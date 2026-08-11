#!/data/data/com.termux/files/usr/bin/bash
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="$ROOT/.aura/auditoria/auditoria-$STAMP"
mkdir -p "$OUT"

REPORT="$OUT/RELATORIO.md"

{
echo "# AUDITORIA COMPLETA AURA"
echo
echo "Data: $(date)"
echo "Branch: $(git branch --show-current 2>/dev/null || echo desconhecida)"
echo "Commit: $(git rev-parse --short HEAD 2>/dev/null || echo desconhecido)"
echo

echo "## 1. Estado Git"
echo '```'
git status --short
echo '```'
echo

echo "## 2. Projetos"
echo '```'
find src -name '*.csproj' -print | sort
echo '```'
echo

echo "## 3. Arquivos C#"
echo '```'
find src -name '*.cs' \
  -not -path '*/bin/*' \
  -not -path '*/obj/*' \
  | sort
echo '```'
echo

echo "## 4. Estatísticas"
echo '```'
printf 'Projetos: '
find src -name '*.csproj' | wc -l

printf 'Arquivos C#: '
find src -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' | wc -l

printf 'Classes: '
grep -RhoE '\b(class|record class|struct)\s+[A-Za-z_][A-Za-z0-9_]*' \
 src --include='*.cs' --exclude-dir=bin --exclude-dir=obj \
 | wc -l

printf 'Interfaces: '
grep -RhoE '\binterface\s+[A-Za-z_][A-Za-z0-9_]*' \
 src --include='*.cs' --exclude-dir=bin --exclude-dir=obj \
 | wc -l

printf 'Métodos aproximados: '
grep -RhoE \
'\b(public|private|protected|internal|internal\s+protected|private\s+protected|static|async|virtual|override|Task|void|int|string|bool)[^;{}]*[^;{}]*[^{;]*\{' \
 src --include='*.cs' --exclude-dir=bin --exclude-dir=obj \
 | wc -l
echo '```'
echo

echo "## 5. Referências entre projetos"
echo '```'
grep -RniE \
'<ProjectReference|ProjectReference Include=' \
src --include='*.csproj' \
| sort
echo '```'
echo

echo "## 6. AgentSession"
echo '```'
grep -RniE \
'AgentSession|RunAsync|ChatToolsAsync|MaxRounds|_messages|ExecuteToolAsync|TryGetKnownSolution' \
src/AURA.AI \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 7. IA / contexto / tokens"
echo '```'
grep -RniE \
'MaxTokens|max_tokens|num_ctx|context|messages|ChatToolsAsync|tool_calls' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 8. Memória"
echo '```'
grep -RniE \
'MemoryStore|SolutionStore|SolutionRule|RequestContext|Memory' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 9. Ferramentas"
echo '```'
grep -RniE \
'AgentTool|ToolDefinition|ToolCall|read_file|write_file|edit_file|list_dir|run_shell|exec' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 10. Executores"
echo '```'
grep -RniE \
'Executor|ExecuteAsync|ProcessStartInfo|Shell|Command' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 11. CLI"
echo '```'
grep -RniE \
'case "|Comandos:|run |cells|diagnostico|internet|agents|ask |chat |agent |exec |modulos|config|plugins' \
src/AURA.CLI \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 12. Runtime / módulos / plugins"
echo '```'
grep -RniE \
'Plugin|Module|Runtime|LoadFrom|Assembly|DependencyAnalyzer|PluginWatcher' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 13. Installer"
echo '```'
grep -RniE \
'Installer|PythonStdlib|Dependency|Install|Runtime' \
src/AURA.Installer \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sort
echo '```'
echo

echo "## 14. Plataformas"
echo '```'
find src -maxdepth 2 -type d \
  -iname '*Mobile*' -o -iname '*Windows*' -o -iname '*Android*' \
  -print | sort
echo '```'
echo

echo "## 15. Possíveis pontos desconectados"
echo '```'

for name in \
  AgentSession \
  MemoryStore \
  SolutionStore \
  AgentManager \
  PluginWatcher \
  ModuleManager \
  DependencyAnalyzer \
  OpenRouterClient
do
    count=$(grep -R \
      --include='*.cs' \
      --exclude-dir=bin \
      --exclude-dir=obj \
      -l "$name" src 2>/dev/null | wc -l)

    printf '%-30s %s arquivos\n' "$name" "$count"
done

echo '```'
echo

echo "## 16. TODO / FIXME / NOT IMPLEMENTED"
echo '```'
grep -RniE \
'TODO|FIXME|NOT IMPLEMENTED|NotImplementedException|throw new NotImplemented' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
|| true
echo '```'
echo

echo "## 17. Código potencialmente duplicado por nomes"
echo '```'
grep -RhoE \
'\b(class|interface|record)\s+[A-Za-z_][A-Za-z0-9_]*' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
| sed -E 's/^(class|interface|record) //' \
| sort \
| uniq -d
echo '```'
echo

echo "## 18. Projetos da solução"
echo '```'
if [ -f AURA.sln ]; then
    dotnet sln AURA.sln list
elif [ -f AURA.slnx ]; then
    dotnet sln AURA.slnx list
else
    echo "Solution não encontrada."
fi
echo '```'
echo

echo "## 19. Build de diagnóstico"
echo '```'
dotnet build src/AURA.CLI/AURA.CLI.csproj --nologo
BUILD=$?
echo "BUILD_EXIT_CODE=$BUILD"
echo '```'

echo
echo "## 20. Classificação inicial"
echo
echo "### Provável núcleo"
echo "- AURA.Core"
echo "- AURA.AI"
echo "- AURA.Agents"
echo "- AURA.Memory"
echo "- AURA.CLI"
echo "- AURA.Abstractions"
echo
echo "### Infraestrutura a auditar"
echo "- AURA.Modules"
echo "- AURA.Installer"
echo "- AURA.SystemInfo"
echo "- AURA.Network"
echo "- AURA.Windows"
echo
echo "### Componentes a verificar como módulos"
echo "- AURA.Mobile"
echo "- Browser"
echo "- VPN/Tor"
echo "- funcionalidades específicas de plataforma"
echo
echo "### Regra"
echo "Nenhum componente será removido somente com base nesta classificação."
echo "A decisão final deve usar referências e fluxo de runtime."
} > "$REPORT"

echo
echo "=============================================="
echo " AUDITORIA CONCLUÍDA"
echo "=============================================="
echo
echo "Relatório:"
echo "$REPORT"
echo
echo "Diretório:"
echo "$OUT"
echo
echo "Nenhum arquivo-fonte foi alterado."
echo
echo "Resumo:"
wc -l "$REPORT"
echo "=============================================="
