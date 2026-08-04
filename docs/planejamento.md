# AURA — Planejamento do projeto

Fonte de verdade do estado, próximos passos e **delegação de tarefas** entre
as camadas disponíveis (opencode / aichat / termux-ai / GitHub).

## 1. O que já foi feito (concluído e validado)

| Item | Detalhe | Validação |
|---|---|---|
| F0 | Migração net48→net10.0; runtime de células (processo isolado + reciclagem) | build 0 erros |
| F1 | `cells.json` persistência + restauração de estado, adoção de órfãos, hot-reload de plugins (ALC coletável + FileSystemWatcher) | smoke-test OK |
| F2 | `prlimit` por célula (`--mem`, `--cpu`, `--files`, `--procs`) | SIGXCPU / OOM ~100MB validados |
| CLI | `run`, `cells`, `cell start/stop/pause/resume/delete/log/limits`, `persist`, `diagnostico`, `internet`, `modulos`, `launchers`, `plugins`, `ajuda` | smoke-test OK |
| Scripts | `setup.sh`, `aura.sh`, `smoke-test.sh`, `check-env.sh`, `migrar-ferramentas.sh` | smoke-test OK |
| Docs | `docs/README.md` (roadmap), `docs/termux-ai-spec.md`, `docs/ferramentas.md`, `docs/legacy/*` | — |
| Git/GitHub | Repo público `denilsonluiz3-sys/AURA_assistente`, SSH, `.gitignore` | push OK |
| CI | `.github/workflows/build-test.yml`: Restore→Build→tests→smoke no runner x64 | 4 runs success |
| CI++ | Cache NuGet, `global.json` (SDK 10.0.110), schedule diário, badge | success |
| aichat | Config OpenRouter `qwen/qwen-plus`, `use_tools` ativo, 10 tools llm-functions, `argc` instalado, role `aura-review` | build functions OK |
| termux-ai | Provider `openai` + api_url OpenRouter (patch `openai.py`) | respondeu "4" |

## 2. O que falta concluir

### F3 — Célula assistente (PRÓXIMA)
- `AgentManager` que orquestra aichat/termux-ai como app comum.
- `aura run aichat --cell chat` (aichat já tem launcher? — verificar).
- `aura ask "pergunta"` → responde e loga na célula.
- Implementar `IAgent` (stub existe em `AURA.Core/Abstractions`).

### F4 — Loja de módulos
- Loja local `~/AURA/loja` + `aura update`; depois HTTPS.
- Reaproveita `PluginWatcher` para hot-reload de módulos baixados.

### F5 — Daemon + API HTTP
- Termux: `termux-services` (runit); Linux: `systemd --user`.
- API via célula dedicada (ex.: `AURA.API`).

### F6 (opcional) — Isolamento forte
- `proot`/firejail só sob demanda para células suspeitas (`.jar` da net).

### F7 (estudo) — KVM/qcow2
- Backend em Linux real; impossível no celular (sem `/dev/kvm`).

### Infra pendente
- Migrar `aichat`+`jq` → `~/bin` (script pronto; **aguarda mount do Termux**).
- Testar function calling do aichat (estava 429 upstream; aichat offline).
- Botão/check de status do repo em `docs/ferramentas.md` com o badge.

## 3. Ordem de execução (anti-conflito)

Princípio: **nunca tocar em arquivo/processo ainda em uso por tarefa anterior**.
Cada linha libera o recurso antes da próxima começar.

```
[1] Sincronizar workspace local ←── clone do GitHub (fonte de verdade)
        │  (repo local /root/AURA está com inodes quebrados → usar /tmp/opencode/aura_clone)
        ▼
[2] F3: implementação (opencode) — cria NOVOS arquivos, não altera F1/F2
        │   valida: build + smoke via CI (não depende do mount)
        ▼
[3] CI aprova F3 → push → run (fire-and-forget, ~30s)
        │   enquanto CI roda: [4] migrar aichat/jq p/ ~/bin (se mount voltou)
        ▼
[4] Migração de ferramentas (script pronto) + testar function calling aichat
        │   NÃO conflita: só escreve ~/bin/aichat, ~/bin/jq
        ▼
[5] F4: loja local + aura update (depende de F3 pronta p/ o fluxo de módulos)
        │
[6] F5: daemon + API (depende de F4, pois a loja alimenta a API)
        │
[7] F6/F7: isolamento e KVM (opcionais, só quando o resto estiver estável)
```

**Regra de conflito:** F3 e [4] podem rodar em paralelo (arquivos distintos).
F4/F5 não começam antes de F3 aprovada pelo CI. Migração de ferramentas só
toca `~/bin/*`, nunca `src/`.

## 4. Distribuição de tarefas (quem faz o quê)

| Tarefa | Ferramenta | Justificativa |
|---|---|---|
| F3: código `AgentManager`, `aura ask` | **opencode** | Núcleo, precisa de contexto do runtime |
| F3: revisão de código | **aichat** (`-r aura-review`) | Achou bug real do CellStore antes |
| F3: testes unitários rascunho | **aichat** | Gera rápido; opencode valida/adapta |
| F4/F5: análise de arquitetura | **opencode** + aichat | Decisão opencode, rascunho aichat |
| Docs F3-F5 | **aichat** (rascunho) + opencode (revisão) | Padrão já usado |
| Consultas rápidas/conceitos | **termux-ai** | Leve, no celular, fallback |
| Build + testes + smoke | **GitHub Actions** | Runner x64 limpo, ~30s |
| Histórico, issues, releases | **GitHub** | Fonte de verdade |
| Migrar aichat/jq p/ ~/bin | **script** `migrar-ferramentas.sh` | Idempotente, blindado |
| Status das ferramentas | **check-env.sh** | Detecta sumiços pós-remontagem |

## 5. Estado atual do ambiente (impedimentos)

| Recurso | Estado |
|---|---|
| GitHub repo + CI | ✅ estável, 4 runs success |
| `/root` (argc, functions, configs) | ✅ estável |
| Mount `/data/data/com.termux` (aichat, jq, termux-ai) | ❌ **sumiu** (proot remontou) |
| Repo git local `/root/AURA/.git` | ❌ inodes quebrados → usar clone `/tmp/opencode/aura_clone` |
| Build local | ⚠️ instável (deps.json incompleto, VSTest ARM64) → **usar CI** |

## 6. Próximo passo imediato

1. **Agora:** implementar F3 (AgentManager + `aura ask` + célula para aichat),
   sem esperar o mount. Validação via CI.
2. **Em paralelo:** se o mount voltar, rodar `migrar-ferramentas.sh` e testar
   o function calling do aichat.
3. **Quando F3 passar no CI:** abrir issue F4 no GitHub e iniciar a loja local.
