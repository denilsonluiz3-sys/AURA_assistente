# AURA native local-model bridge

A biblioteca Android deverá expor, para a ABI `arm64-v8a`, estes símbolos C:

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
- o wrapper não deve fazer rede, autenticação ou executar comandos;
- o wrapper deve ser compilado com o Android NDK e com `arm64-v8a`;
- a biblioteca não deve ser incluída no APK até que o build nativo seja validado.

A implementação concreta usará a interface C do llama.cpp e será adicionada
separadamente do código C# da AURA. O contrato acima evita acoplamento do
`AgentSession` à biblioteca nativa.
