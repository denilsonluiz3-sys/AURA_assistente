#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

echo "=== AURA AGENTS SETUP ==="

mkdir -p .github/agents

cat > .github/copilot-instructions.md <<'DOC'
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
DOC

make_agent() {
    local file="$1"
    local role="$2"
    local owner="$3"
    local order="$4"
    local forbidden="$5"

    cat > ".github/agents/$file" <<DOC
# $role

## Ownership
$owner

## Ordem de pesquisa
$order

## Roadmap
F7 — CLI/API
F8 — E2E/Smoke

F4/F5/F6 somente manutenção e regressão.

## Proibido
$forbidden

## Regra
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW

Nunca criar implementação paralela sem provar que a existente não pode ser reutilizada.
DOC
}

make_agent \
"aura-architect.agent.md" \
"AURA Architect" \
"Arquitetura, roadmap, integração e distribuição de ownership." \
"docs/roadmap* -> docs/architecture -> src/AURA.Core -> src/AURA.AI -> src/AURA.Mobile -> tests -> workflows" \
"criar segunda arquitetura, segunda sessão, segundo executor, segundo provider ou reimplementar fases concluídas."

make_agent \
"aura-code-auditor.agent.md" \
"AURA Code Auditor" \
"Descoberta de código, dependências, duplicação e comparação entre branches." \
"docs -> src -> tests -> workflows -> histórico Git" \
"implementar antes da auditoria ou duplicar código existente."

make_agent \
"aura-ai-provider.agent.md" \
"AURA AI Provider" \
"Providers LLM, resolução, autenticação, tool calling, streaming e respostas." \
"src/AURA.AI -> src/AURA.Core -> tests -> configuração -> histórico" \
"UI, Android, shell executor, memória própria ou provider duplicado."

make_agent \
"aura-agent-engine.agent.md" \
"AURA Agent Engine" \
"AgentSession, loop de execução, contexto, tool calls e resultados." \
"AgentSession.cs -> AgentChat.cs -> AgentTool.cs -> AgentToolResult.cs -> AgentTools -> tests" \
"segunda AgentSession, segundo AgentTool, provider HTTP, UI, Android ou memória paralela."

make_agent \
"aura-android.agent.md" \
"AURA Android" \
"AURA.Mobile, MAUI, Android, permissões, filesystem, UI e APK." \
"src/AURA.Mobile -> Platforms/Android -> MauiProgram -> Pages -> Services -> workflows" \
"colocar Android em Core, provider em Page, AgentSession na UI ou duplicar serviços Core."

make_agent \
"aura-testing.agent.md" \
"AURA Testing" \
"Unit, integration, smoke, E2E, regressão e CI." \
"tests -> workflows -> roadmap -> código afetado" \
"apagar/enfraquecer testes para obter PASS ou esconder regressões."

make_agent \
"aura-security.agent.md" \
"AURA Security" \
"Shell, filesystem, módulos, reflection, secrets, permissões e isolamento." \
"AgentTools -> Modules -> Mobile/Android -> CLI -> config -> workflows -> tests" \
"remover validações, expor secrets, permitir shell arbitrário ou reduzir segurança para corrigir build."

echo "[PASS] 7 agentes + contrato criados."

echo
echo "=== VALIDANDO ==="
test "$(find .github/agents -name '*.agent.md' | wc -l)" -eq 7
test -f .github/copilot-instructions.md
echo "[PASS] estrutura válida"

echo
echo "=== GIT ==="
git status --short .github

echo
echo "=== BUILD ==="
if dotnet build --no-restore; then
    echo "[PASS] build"
else
    echo "[FAIL] build"
    exit 1
fi

echo
echo "=== TEST ==="
if dotnet test --no-restore --no-build; then
    echo "[PASS] tests"
else
    echo "[FAIL] tests"
    exit 1
fi

echo
echo "=== CONFLITOS ==="
if git diff --check; then
    echo "[PASS] sem erros de whitespace/conflitos"
else
    echo "[FAIL] revisar conflitos"
    exit 1
fi

if git ls-files -u | grep -q .; then
    echo "[FAIL] existem conflitos Git não resolvidos"
    exit 1
fi

echo
echo "=== ALTERAÇÕES ==="
git status --short .github
git diff -- .github

echo
echo "=== PRÓXIMO PASSO ==="
echo "Revisar os arquivos acima."
echo
echo "Depois:"
echo "  git add .github/agents .github/copilot-instructions.md"
echo "  git diff --cached"
echo "  git commit -m 'chore(agents): define AURA agent ownership'"
echo "  git push -u origin \$(git branch --show-current)"
