# AURA native local-model bridge

A biblioteca Android expõe, para a ABI `arm64-v8a`, estes símbolos C:

```c
void* aura_llama_open(const char* model_path, int context_size, int threads);
char* aura_llama_generate(void* context, const char* prompt, int max_tokens);
void aura_llama_free_text(char* output);
void aura_llama_close(void* context);
```

Regras da ponte:

- `model_path` aponta somente para um modelo previamente registrado pelo `LocalModelStore`;
- o retorno de `aura_llama_generate` deve ser UTF-8 alocado pela própria biblioteca;
- `aura_llama_free_text` libera o retorno da geração;
- qualquer falha deve retornar ponteiro nulo;
- o wrapper não faz rede, autenticação ou execução de comandos;
- o wrapper é compilado com o Android NDK e com `arm64-v8a`;
- a biblioteca não deve ser incluída no APK até que o build nativo seja validado.

## Build nativo

O `CMakeLists.txt` espera uma cópia do llama.cpp em `LLAMA_CPP_ROOT` e gera a
biblioteca compartilhada `libaura_llama.so`.

```bash
cmake -S native/aura-llama -B native/aura-llama/build \
  -DLLAMA_CPP_ROOT="$PWD/native/llama.cpp" \
  -DCMAKE_TOOLCHAIN_FILE="$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake" \
  -DANDROID_ABI=arm64-v8a \
  -DANDROID_PLATFORM=28 \
  -DGGML_NATIVE=OFF \
  -DGGML_OPENMP=OFF \
  -DGGML_LLAMAFILE=OFF \
  -DLLAMA_OPENSSL=OFF
cmake --build native/aura-llama/build --target aura_llama
```

A biblioteca gerada deve ser colocada em
`src/AURA.Mobile/Platforms/Android/NativeLibs/arm64-v8a/` antes do publish do
APK. O código C# não ativa o runtime enquanto essa biblioteca não estiver
presente.
