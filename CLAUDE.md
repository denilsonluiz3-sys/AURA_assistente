# AURA — Contexto do Projeto

## Sobre
Orquestrador de aplicativos em C#/.NET (user-space sobre Linux/Termux).
Roda células isoladas (processos), com launchers por extensão (.py, .jar, .dll).

## Regras de trabalho (ECONOMIA DE TOKENS)
- NÃO leia o repositório inteiro a cada pergunta. Leia só os arquivos relevantes à tarefa.
- Antes de editar, mostre um resumo curto do plano (3-5 linhas), não o raciocínio completo.
- Prefira diffs/patches a reescrever arquivos inteiros.
- Não rode `dotnet build` completo repetidamente — só quando pedir para validar.
- Não gere explicações longas depois de cada ação; só confirme o que foi feito em 1-2 linhas.
- Se a tarefa for grande, quebre em etapas e pergunte antes de continuar para a próxima.
- Evite reler arquivos que não mudaram desde a última leitura na mesma sessão.

## Estrutura relevante
- src/AURA.Core/Runtime — células isoladas, watcher, reciclagem
- src/AURA.Core/Launchers — resolução por extensão
- AURA.CLI — comandos run/cells/cell
- scripts/ — auditoria_memoria.sh, fix-agent-memory.sh, etc.

## Prioridades atuais
1. Diagnosticar problema de memória (auditoria_memoria.sh / fix-agent-memory.sh)
2. Fase F3 do roadmap: célula "assistente"
