# AURA — Mapa de aprendizagem local (menos IA)

## Objetivo

A AURA **não deve chamar o LLM para tudo**. Ordem preferida:

```text
1. IntentResolver / rotas locais (diagnóstico, navegação)
2. SolutionStore (tarefas que já deram certo)
3. Ferramentas fixas (list_dir, read_file, shell seguro)
4. Só então OpenRouter / AgentSession
```

## SolutionStore (já no código)

- Arquivo: `src/AURA.Memory/SolutionStore.cs`
- Persistência: `aura_agent_memory.json` (procedural)
- API:
  - `FindBestMatch(task)` → reutiliza ação bem-sucedida
  - `Record(task, action, result, success)` → grava após êxito

**ActionTaken** pode ser:
- texto de resposta pronta
- sequência de tools JSON
- script shell (uma linha ou bloco) a rodar no terminal **via PolicyGuard**

## Quando chamar a IA

Só se:
- não houver match local ≥ threshold
- a tarefa exigir raciocínio novo (código/patch desconhecido)
- o usuário pedir explicitamente “pense / planeje / orquestre”

## Protocolo código → terminal (quando a IA for necessária)

Prompt de sistema (resumo):

1. Prefira **ferramentas já registradas** (list/read/write/shell).
2. Se precisar de script: responda com um único bloco:

````text
```aura-sh
# comandos seguros no workspace
ls -la
```
````

3. O runtime extrai `aura-sh` e executa com `ShellAgentTool` / `ShellExecutor` **sem** segundo round de chat, se possível.
4. Proibido: `rm -rf /`, exfiltração de keys, alterar remote git sem pedido.

## Mapa mínimo de capacidades “já prontas”

| Capacidade | Como (sem LLM) |
|------------|----------------|
| Status do device | Capability Lab / SystemAnalyzer |
| Listar workspace | `list_dir` |
| Ler arquivo | `read_file` |
| Memória recente | `MemoryStore.Read` |
| Tarefa repetida | `SolutionStore.FindBestMatch` |
| Git status/commit | `scripts/aura_git.py` (se Python+git) |
| Fala | Android TTS (Kokoro removido) |

## Limpeza de APK

- Sem Kokoro / ONNX / pf_dora
- Sem vídeos lunar/solar
- TTS = `AndroidTtsSpeechService` via `HybridSpeechService`
