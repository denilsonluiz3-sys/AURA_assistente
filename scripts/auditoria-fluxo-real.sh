#!/data/data/com.termux/files/usr/bin/bash

ROOT="$HOME/AURA"
cd "$ROOT" || exit 1

OUT="$ROOT/.aura/auditoria/fluxo-$(date +%Y%m%d-%H%M%S)"
mkdir -p "$OUT"

echo "AURA - AUDITORIA DO FLUXO REAL"
echo "================================"
echo "Saida: $OUT"
echo

echo "[1] AgentSession"
grep -RniE \
'new AgentSession|AgentSession\(' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
> "$OUT/01-agentsession.txt" 2>/dev/null

echo "[2] SolutionStore"
grep -RniE \
'SolutionStore|SolutionRule|TryGetKnownSolution' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
> "$OUT/02-solutionstore.txt" 2>/dev/null

echo "[3] MemoryStore"
grep -RniE \
'MemoryStore|MemoryEntry|RequestContext' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
> "$OUT/03-memory.txt" 2>/dev/null

echo "[4] Ferramentas"
grep -RniE \
'AgentTool|read_file|write_file|list_dir|run_shell|ShellAgentTool|WorkspaceAgentTool' \
src/AURA.AI \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
> "$OUT/04-tools.txt" 2>/dev/null

echo "[5] Referencias entre projetos"
grep -Rni \
'<ProjectReference' \
src \
--include='*.csproj' \
> "$OUT/05-project-references.txt" 2>/dev/null

echo "[6] Resumo"
{
    echo "# AUDITORIA DO FLUXO REAL"
    echo
    echo "Data: $(date)"
    echo "Branch: $(git branch --show-current)"
    echo "Commit: $(git rev-parse --short HEAD)"
    echo
    echo "Arquivos:"
    ls -1 "$OUT"
    echo
    echo "Objetivo:"
    echo "Descobrir o que realmente conecta:"
    echo "- usuário"
    echo "- AgentSession"
    echo "- ferramentas"
    echo "- memória"
    echo "- SolutionStore"
    echo "- executores"
} > "$OUT/00-RESUMO.md"

echo
echo "================================"
echo "AUDITORIA CONCLUIDA"
echo "================================"
echo
echo "$OUT"
echo
cat "$OUT/00-RESUMO.md"
