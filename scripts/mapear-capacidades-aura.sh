#!/data/data/com.termux/files/usr/bin/bash
set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

STAMP="$(date +%Y%m%d-%H%M%S)"
OUT="$ROOT/.aura/auditoria/capacidades-$STAMP"
mkdir -p "$OUT"

REPORT="$OUT/RELATORIO-CAPACIDADES.md"
CSFILES="$OUT/csharp-files.txt"

find src -type f -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' | sort > "$CSFILES"

cat > "$REPORT" <<EOF2
# AURA — MAPA DE CAPACIDADES REAIS

Data: $(date)
Branch: $(git branch --show-current 2>/dev/null || echo desconhecida)
Commit: $(git rev-parse --short HEAD 2>/dev/null || echo desconhecido)

Objetivo: identificar capacidades existentes, pontos de entrada, dependências e sinais de integração antes de remover ou duplicar código.

Este relatório é análise estática. Uma referência encontrada não prova que o fluxo foi executado com sucesso.
EOF2

section() {
  printf '\n## %s\n\n' "$1" >> "$REPORT"
}

scan() {
  local title="$1"
  local pattern="$2"
  printf '### %s\n' "$title" >> "$REPORT"
  grep -RniE "$pattern" src --include='*.cs' --exclude-dir=bin --exclude-dir=obj 2>/dev/null |
    head -n 150 >> "$REPORT" || true
  printf '\n' >> "$REPORT"
}

section "1. Entradas e orquestração"
scan "AgentSession e loop" 'class AgentSession|new AgentSession|RunAsync\(|ChatToolsAsync'
scan "Pontos que criam AgentSession" 'new AgentSession'

section "2. Ferramentas"
scan "AgentTool" 'class .*Tool|: AgentTool|ExecuteAsync\('
scan "Ferramentas básicas" 'Name = "(list_dir|read_file|write_file|edit_file|run_shell)"'

section "3. Memória"
scan "MemoryStore" 'class MemoryStore|new MemoryStore|\.Append\(|\.Read\(|\.Clear\('
scan "SolutionStore" 'class SolutionStore|new SolutionStore|\.Find\(|SaveValidated|TryGetKnownSolution'
scan "RequestContext" 'class RequestContext|RequestContext\('

section "4. Execução"
scan "Executores" 'class .*Executor|: .*Executor|ShellExecutor|GitExecutor|PythonExecutor|NodeExecutor|ProcessStartInfo|Process\.Start'
scan "Launchers e Runtime" 'class .*Launcher|class Runner|class .*Runtime|CellStore|SimulationRuntime'

section "5. Análise e diagnóstico"
scan "Diagnóstico" 'Diagnostics|SystemAnalyzer|SystemDiagnosticsResult|ProjectAccessService|SearchCatalog|FixProposal'
scan "Sistema/rede/Windows" 'NetworkManager|SystemAnalyzer|AURA.Windows|class .*Windows'

section "6. Módulos e extensibilidade"
scan "Módulos" 'ModuleManager|ModuleCatalog|ModuleInfo|LoadModule|RuntimeManager|RuntimeResolver'
scan "Interfaces/extensibilidade" 'IPlugin|PluginWatcher|IModule|IService|IAgent'

section "7. IA e pesquisa"
scan "Provedores IA" 'OpenRouterClient|ProviderCatalog|AiAssistant|AiAssistantService'
scan "Pesquisa/Web" 'BrowserPage|ImageSearchPage|HttpClient|WebView|SearchCatalog'

section "8. Mobile/DI"
scan "DI" 'MauiProgram|AddSingleton|AddTransient|GetRequiredService'
scan "Páginas" 'class .*Page'

section "9. Dependências entre projetos"
grep -RniE '<ProjectReference[^>]*Include=' src --include='*.csproj' 2>/dev/null |
  sort >> "$REPORT" || true

section "10. Classes e interfaces"
grep -RniE '^\s*(public|internal|private|protected)?\s*(sealed\s+|abstract\s+|static\s+)?(class|interface|record|enum)\s+[A-Za-z_][A-Za-z0-9_]*' \
  src --include='*.cs' --exclude-dir=bin --exclude-dir=obj 2>/dev/null |
  sort >> "$REPORT" || true

section "11. Índice inicial de componentes"
python3 - "$CSFILES" "$REPORT" <<'PY'
import re, sys
from pathlib import Path

files = Path(sys.argv[1]).read_text(errors="ignore").splitlines()
report = Path(sys.argv[2])

groups = [
    ("AI/orquestração", r"AgentSession|OpenRouterClient|AiAssistant|AgentTool"),
    ("Memória", r"MemoryStore|MemoryEntry|SolutionStore|SolutionRule|RequestContext"),
    ("Ferramentas", r"AgentTools|ShellAgentTool|FileTools|WorkspaceAgentTool"),
    ("Execução", r"Executor|Launcher|Runner|Runtime"),
    ("Módulos", r"Module|Catalog|Dependency|Compatibility"),
    ("Diagnóstico", r"Diagnostic|SystemAnalyzer|ProjectAccess|FixProposal|SearchCatalog"),
    ("Mobile", r"AURA.Mobile|Page|MauiProgram"),
]

with report.open("a", encoding="utf-8") as out:
    for name, pattern in groups:
        n = sum(bool(re.search(pattern, f)) for f in files)
        out.write(f"- **{name}**: {n} arquivos candidatos\n")
PY

section "12. Classificação para decisão"
cat >> "$REPORT" <<'EOF2'
| Categoria | Critério | Ação |
|---|---|---|
| 🟢 Núcleo | Participa do fluxo usuário → AgentSession → ferramenta/execução | manter |
| 🟡 Capacidade | Existe e possui referências, mas ainda não foi validada em execução | testar |
| 🔵 Infraestrutura | Runtime, DI, logging, módulos, configuração ou plataforma | manter até provar redundância |
| 🟠 Isolado | Não foram encontradas chamadas/referências claras | investigar |
| 🔴 Redundante | Outra implementação comprovadamente substitui a capacidade | candidato a remoção |

## Próxima arquitetura

A AURA deve evoluir para:

usuário
→ AgentSession
→ memória/soluções conhecidas
→ capacidades/ferramentas
→ execução
→ verificação
→ solução validada

A IA decide e raciocina quando necessário; a AURA fornece as capacidades e executa.

## Regra

Não remover código apenas porque parece não utilizado. Primeiro provar por busca estática + teste mínimo que a capacidade é redundante.
EOF2

echo
echo "============================================="
echo "AURA — MAPA DE CAPACIDADES GERADO"
echo "============================================="
echo "Relatório: $REPORT"
echo
echo "Para visualizar:"
echo "cat \"$REPORT\""
