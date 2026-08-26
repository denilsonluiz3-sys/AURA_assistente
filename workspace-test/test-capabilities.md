# Teste de Capacidades do Agente AURA

Data: 2026-08-26
Propósito: verificar o que o agente consegue ver, ler, criar, editar, executar.

## 1. Leitura de arquivo
Se você está lendo isto, a tool `read_file` funciona.

## 2. Estrutura do workspace
```
workspace/
├── test-capabilities.md    ← este arquivo
├── test-script.sh          ← script shell para testar run_shell
├── test-config.json        ← arquivo JSON para testar edit_file
└── memory-notes.md         ← suas notas existentes
```

## 3. Comandos shell para testar
O agente deve conseguir executar via `run_shell`:
- `pwd` → mostra diretório atual
- `ls -la` → lista arquivos
- `cat test-capabilities.md` → lê este arquivo
- `getprop ro.product.model` → modelo do aparelho
- `df -h` → espaço em disco
- `ps | head -10` → processos rodando

## 4. Memória executável
Se o agente já resolveu algo antes, `search_memory` deve encontrar.
Exemplo: "listar workspace" → deve retornar bloco `aura-sh` com `pwd` + `ls -la`.

## 5. Criação de arquivo
Peça ao agente: "crie um arquivo chamado test-output.txt com a data de hoje"
→ deve usar `write_file`

## 6. Edição de arquivo
Peça ao agente: "adicione a linha 'editado pelo agente' no final de test-config.json"
→ deve usar `edit_file`

## 7. Web
Peça ao agente: "busque o conteúdo de https://httpbin.org/get"
→ deve usar `web_fetch`

## 8. Navegador
Peça ao agente: "abra https://google.com no navegador"
→ deve usar `open_browser`
