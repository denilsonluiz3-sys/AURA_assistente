# AURA — Próximos passos (roadmap — continuação)

Este documento resume e organiza os próximos passos priorizados para o projeto AURA, partindo do ponto onde o roadmap anterior parou. Ele foi criado na branch `feat/rename-angela-to-aura` como parte da renomeação/avalização solicitada.

Resumo rápido
- As fases F0–F4 históricas estão implementadas no `main` e são tratadas como concluídas.
- Prioridade imediata: fechar PR #23 (ToolRegistry) para desbloquear a camada cognitiva.
- Alvo final (médio prazo): ToolResult tipado, loja de módulos local/remota, daemon+API HTTP, agentes concretos (IAgent).

P0 — Fechos imediatos (1–2 sprints)
- P0.1 Merge PR #23 — ToolRegistry (Fase A)
  - Critério: `build-and-test` verde + smoke tests.
  - Saída: documentação curta em `docs/tool-registry.md`.
- P0.2 Consolidar docs históricos (marcar `docs/roadmap-4-itens.md` como histórico)
  - Critério: README/docs apontando `docs/roadmap-completo.md` como fonte de verdade.

P1 — Camada cognitiva (depende de ToolRegistry)
- P1.1 ToolResult interno (Fase B)
  - Implementar tipo rico: Success, ExitCode, Stdout, Stderr, Duration, Metadata.
  - Atualizar `IToolExecutor`/implementações (Shell/Git/Python/Node) para retornar o tipo.
  - Critério: testes e integração com AgentSession.
- P1.2 `search_files` (executor/grep)
  - Fornecer RAG local: buscar arquivos e retornar snippets ao agente.
- P1.3 Expandir `MemoryKind` com ToolCall, ErrorEvent, ProceduralExperience
  - Permitir memória episódica e procedural para agentes.

P2 — Loja de módulos (F4)
- P2.1 Loja local (`~/AURA/loja`) + `aura update` (local)
  - Reaproveitar ModuleManager/ModuleCatalog.
  - Critério: `ModuleFlowTests` passa com o fluxo local.
- P2.2 Loja remota (HTTPS) + validação/assinatura (opcional)

P3 — Daemon + API HTTP (F5)
- P3.1 Daemon (termux-services / systemd --user)
  - `aura daemon start/stop/status` + service templates.
- P3.2 API HTTP (REST) — endpoints básicos (/cells, /run, /log, /modules, /agents/ask)
  - Segurança: unix-socket + token por padrão em Termux.

P4 — Agentes (IAgent concretos)
- P4.1 MemoryAgent, AutomationAgent, AIAgent wrapper (integração com ToolRegistry e MemoryStore).
- P4.2 Polimento do ToolRegistry/AgentTool (schema, validações).

Checklist de PRs sugeridos (pequenos, reversíveis)
- feat/toolregistry — concluir e testar PR #23
- feat/toolresult — implementar ExecutionResult/ToolResult tipado
- feat/search_files — adicionar executor de busca RAG
- feat/module-store-local — loja local e comandos `aura update`
- feat/daemon-api-prototype — daemon + endpoints básicos
- docs/tool-consolidation-plan.md — descrever integração entre ToolRegistry, ToolResult, AgentTool, AgentSession

QA e critérios de aceitação
- CI verde (build-and-test em Actions)
- Testes unitários cobrindo os novos comportamentos
- Smoke scripts em `scripts/` para validação manual
- Documentação curta em `docs/` para cada recurso

O que eu já verifiquei
- Busquei por ocorrências exatas do nome “Angela” e não encontrei correspondências no repositório atual. Assim, não foi necessário aplicar substituições de texto naquele nome.

Próximos passos que posso executar automaticamente
- Commitar outras alterações pequenas (ex.: ajustes de README, atualizar badges) nesta mesma branch e abrir um PR para revisão.
- Criar issues correspondentes ao checklist acima (um por tarefa).

Quer que eu:
- (A) Abra um PR com apenas este arquivo `docs/roadmap-next-steps.md` na branch `feat/rename-angela-to-aura`? (recomendado)
- (B) Além de A, crie issues para cada item do checklist? (requer confirmação)
- (C) Procure por outras variações do nome/strings adicionais onde a substituição seria necessária (ex.: imagens, binários) e proponha patches?