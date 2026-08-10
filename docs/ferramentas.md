# Orquestração das ferramentas de desenvolvimento da AURA

Este documento define **como distribuir, gerenciar, delegar e executar**
trabalho usando as 4 camadas de assistência disponíveis:

| Camada | Ferramenta | Papel |
|---|---|---|
| 1 | **opencode** (eu) | Núcleo: código C#, arquitetura, testes, decisões; **agente do `AgentManager`** que edita o repo (auto-melhoria) |
| 2 | **aichat** | Assistente LLM poderoso: revisão, geração, análise, tools |
| 3 | **termux-ai** | Assistente leve no celular: consultas rápidas, fallback |
| 4 | **GitHub** | Fonte de verdade: histórico, CI, issues, releases |

> **Consolidação F3**: os três assistentes (opencode, aichat, termux-ai) são
> apps comuns da AURA (`aura ask "..." --assistente <nome>` /
> `aura run <nome> --cell <id>`). Só o **opencode** enxerga/edita o repo —
> `scripts/aura-workspace.sh` cria o clone de auto-melhoria em
> `~/AURA/workspace/AURA_assistente`. Prompts: `prompts/` (formato LobeHub).

## Princípio de delegação

> **Regra de ouro**: o `opencode` decide *o quê* e *por quê*; os assistentes
> LLM executam *como* (revisões, rascunhos, análises); o GitHub garante *onde*
> (histórico, CI, entrega).

```
Quero X ──► opencode (planeja) ──► aichat/termux-ai (executa apoio) ──► git push (valida/CI)
                                        ▲                                      │
                                        └────────── revisão ◄──────────────────│
```

## Matriz de delegação por tarefa

| Tarefa | opencode | aichat | termux-ai | GitHub |
|---|---|---|---|---|
| Implementar feature F3 | **lidera** | revisa código | — | branch + PR |
| Review de código | decisão final | `-r aura-review` | — | CodeQL + PR review |
| Gerar testes unitários | valida/adapta | rascunha | — | CI roda |
| Consulta rápida (conceito, comando) | — | `aichat "..."` | `termux-ai "..."` | — |
| Rodar comandos shell | — | `aichat -e "..."` | — | Actions workflow |
| Documentar | revisa | rascunha | — | wiki/docs no repo |
| Loja de módulos (F4) | desenha formato | — | — | **Releases** servem `.dll` |
| Reportar bug | — | — | `termux-ai` relata | **Issue** com label |
| CI/build/test | — | — | — | **Actions** |

## Como eu (opencode) uso cada uma

### aichat — o "braço de análise"
- **Revisão**: `aichat -r aura-review -f <arquivo.cs> "Revise."` (já configurado;
  achou bug real no CellStore).
- **Consulta com contexto**: `aichat -f docs/README.md "explique o roadmap"`.
- **Execução shell**: `aichat -e "liste células em ~/AURA"`.
- **Sessões**: `aichat -s aura-f3` para manter contexto entre chamadas.
- **Tools (function calling)**: ao configurar `use_tools`, o aichat pode
  executar comandos reais (data, fs, web) em vez de "inventar".

### termux-ai — o "assistente do celular"
- Usar quando o aichat não estiver disponível (Termux puro, sem proot).
- Consultas rápidas e discretas; custo baixo; sem tools.
- Célula AURA: `aura run termux-ai --cell chat`.

### GitHub — o "arquivo e o CI"
- **Repositório**: `denilsonluiz3-sys/AURA_assistente`.
- **Commit**: mudança validada (build + smoke ou CI) antes do push.
- **Actions**: build + testes em Linux real.
- **Issues / Releases**: roadmap e distribuição de plugins.

## Executores de ferramentas (AURA.Abstractions)

Contrato único de **execução de processo**: `IToolExecutor` em
`src/AURA.Abstractions/Execution/`.

| Executor | Binário | Uso (request.Command) |
|---|---|---|
| `ShellExecutor` | `/bin/sh` | Comando shell completo (`sh -c`) |
| `GitExecutor` | `git` | Subcomando git |
| `PythonExecutor` | `python3`/`python` | Script, módulo ou flag |
| `NodeExecutor` | `node` | Script ou flag |

- Base: `ProcessExecutorBase` (stdout/stderr, timeout, env).
- Sem binário: `IsAvailable() == false` e `ExecutionResult` de erro.

### Camada cognitiva (AgentTool)

O agente LLM (`AgentSession`) usa `AgentTool` (schema + string para o modelo).
Tools de **processo** (ex.: `ShellAgentTool` / `run_shell`) são **adaptadores**:

```
LLM → AgentSession → ShellAgentTool → IToolExecutor (ShellExecutor)
                         → FormatForLlm(ExecutionResult) → string
```

- File tools (`list_dir`, `read_file`, …) não passam por `IToolExecutor`.
- Decisão arquitetural: `docs/architecture/tool-consolidation-plan.md`.

Exemplo operacional:

```csharp
var git = new GitExecutor();
var result = await git.ExecuteAsync(new ExecutionRequest
{
    Command = "status",
    WorkingDirectory = "/root/AURA"
});
Console.WriteLine(result.StandardOutput);
```

## Espaço de auto-melhoria (workspace)

```bash
bash scripts/aura-workspace.sh clone
bash scripts/aura-workspace.sh open
bash scripts/aura-workspace.sh status
```

## Fluxo de trabalho recomendado

1. Issue no GitHub.
2. Implementar; revisar; commit pequeno.
3. CI (`build-test` + APK se Mobile).
4. Próximo item: ver `docs/roadmap-completo.md`.

## Estabilidade do ambiente (proot/Termux)

1. Crítico em `/root` (`~/bin`, configs).
2. Após remontagem: `bash scripts/check-env.sh`.
3. Repo git local corrompido → reclonar do GitHub.
4. Builds locais instáveis → **confiar no GitHub Actions**.
5. Fire-and-forget: push → CI ~30s → ler resultado.
