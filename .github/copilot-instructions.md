# AURA Agent Contract

Fluxo obrigatório:
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW -> COMMIT -> PUSH

Ownership:
- Architect: arquitetura/roadmap
- Code Auditor: auditoria/duplicação
- AI Provider: LLM/providers
- Agent Engine: AgentSession/tools/loop
- Android: MAUI/Android/APK
- Testing: testes/CI/E2E
- Security: segurança/isolamento

Roadmap:
- F4/F5/F6: concluídos; somente manutenção/regressão
- F7: CLI/API
- F8: E2E/Smoke

Anti-duplicação:
- pesquisar antes de criar;
- reutilizar implementação existente;
- não criar segundo Provider/AgentSession/Executor/Memory;
- não assumir que algo falta sem pesquisar.

Git:
- nunca reset --hard;
- nunca clean -fd;
- nunca push --force;
- nunca sobrescrever alterações locais;
- revisar diff antes do commit.

Build:
dotnet build
dotnet test

Android, quando aplicável:
dotnet build src/AURA.Mobile/AURA.Mobile.csproj

Conflitos:
não resolver automaticamente.
Parar -> analisar -> resolver -> build -> test -> revisar.

Commit:
git status
git diff
git add <arquivos-intencionais>
git diff --cached
git commit

Push:
git push -u origin <branch>
