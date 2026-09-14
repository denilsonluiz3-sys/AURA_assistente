#include "llama.h"
#include <algorithm>
#include <cstring>
#include <cstdlib>
#include <memory>
#include <mutex>
#include <string>
#include <vector>

struct AuraLlamaContext {
    llama_model * model;
    llama_context * context;
    const llama_vocab * vocab;
    int context_size;
    std::mutex gate;
};

static char * copy_result(const std::string & text) {
    char * result = static_cast<char *>(std::malloc(text.size() + 1));
    if (!result) return nullptr;
    std::memcpy(result, text.data(), text.size());
    result[text.size()] = '\0';
    return result;
}

extern "C" {

void * aura_llama_open(const char * model_path, int context_size, int threads) {
    if (!model_path || context_size < 512 || threads < 1) return nullptr;
    static std::once_flag backend_once;
    std::call_once(backend_once, [] { llama_backend_init(); });
    llama_model_params model_params = llama_model_default_params();
    model_params.n_gpu_layers = 0;
    llama_model * model = llama_model_load_from_file(model_path, model_params);
    if (!model) return nullptr;
    llama_context_params context_params = llama_context_default_params();
    context_params.n_ctx = static_cast<uint32_t>(context_size);
    context_params.n_batch = static_cast<uint32_t>(context_size);
    context_params.n_threads = threads;
    context_params.n_threads_batch = threads;
    llama_context * context = llama_init_from_model(model, context_params);
    if (!context) { llama_model_free(model); return nullptr; }
    auto * result = new AuraLlamaContext();
    result->model = model;
    result->context = context;
    result->vocab = llama_model_get_vocab(model);
    result->context_size = context_size;
    return result;
}

using aura_progress_callback = int (*)(int phase, int current, int total);

char * aura_llama_generate(void * handle, const char * prompt, int max_tokens, aura_progress_callback progress) {
    if (!handle || !prompt || max_tokens < 1) return nullptr;
    auto * state = static_cast<AuraLlamaContext *>(handle);
    std::lock_guard<std::mutex> lock(state->gate);
    const int prompt_size = -llama_tokenize(state->vocab, prompt, std::strlen(prompt), nullptr, 0, true, true);
    if (prompt_size <= 0 || prompt_size + max_tokens > state->context_size) return nullptr;
    if (progress && !progress(0, 0, prompt_size)) return nullptr;
    std::vector<llama_token> prompt_tokens(static_cast<size_t>(prompt_size));
    if (llama_tokenize(state->vocab, prompt, std::strlen(prompt), prompt_tokens.data(), prompt_size, true, true) < 0) return nullptr;

    llama_sampler_chain_params sampler_params = llama_sampler_chain_default_params();
    llama_sampler * sampler = llama_sampler_chain_init(sampler_params);
    if (!sampler) return nullptr;
    llama_sampler_chain_add(sampler, llama_sampler_init_greedy());

    std::string output;
    int position = 0;
    constexpr int prompt_chunk = 256;
    while (position < prompt_size) {
        int count = std::min(prompt_chunk, prompt_size - position);
        llama_batch batch = llama_batch_get_one(prompt_tokens.data() + position, count);
        if (llama_decode(state->context, batch) != 0) { llama_sampler_free(sampler); return nullptr; }
        position += count;
        if (progress && !progress(0, position, prompt_size)) { llama_sampler_free(sampler); return nullptr; }
    }

    llama_token token = 0;
    for (int generated = 0; generated < max_tokens; ++generated) {
        if (generated > 0) {
            llama_batch batch = llama_batch_get_one(&token, 1);
            if (llama_decode(state->context, batch) != 0) { llama_sampler_free(sampler); return nullptr; }
        }
        token = llama_sampler_sample(sampler, state->context, -1);
        if (llama_vocab_is_eog(state->vocab, token)) break;
        char piece[256];
        int piece_size = llama_token_to_piece(state->vocab, token, piece, sizeof(piece), 0, true);
        if (piece_size < 0) { llama_sampler_free(sampler); return nullptr; }
        output.append(piece, static_cast<size_t>(piece_size));
        if (progress && !progress(1, generated + 1, max_tokens)) { llama_sampler_free(sampler); return nullptr; }
    }
    llama_sampler_free(sampler);
    return copy_result(output);
}

void aura_llama_free_text(char * output) { std::free(output); }
void aura_llama_close(void * handle) {
    auto * state = static_cast<AuraLlamaContext *>(handle);
    if (!state) return;
    llama_free(state->context);
    llama_model_free(state->model);
    delete state;
}
}
