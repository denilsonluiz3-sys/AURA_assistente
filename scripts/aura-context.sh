#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT_DIR="$ROOT/reports/aura-context"
REPORT="$REPORT_DIR/latest.md"

mkdir -p "$REPORT_DIR"

timestamp="$(date '+%Y-%m-%d %H:%M:%S %z')"

section() {
    echo
    echo "## $1"
    echo
}

safe_cmd() {
    "$@" 2>&1 || true
}

{
    echo "# AURA — Contexto Técnico"
    echo
    echo "> Relatório gerado automaticamente em: $timestamp"
    echo "> Raiz: $ROOT"
    echo

    section "Estado Git"

    echo '```text'
    safe_cmd git -C "$ROOT" status --short --branch
    echo '```'

    echo
    echo "**Branch:**"
    echo '```text'
    safe_cmd git -C "$ROOT" branch --show-current
    echo '```'

    section "Últimos commits"

    echo '```text'
    safe_cmd git -C "$ROOT" log -5 --oneline --decorate
    echo '```'

    section "Estrutura de projetos"

    echo '```text'
    find "$ROOT/src" \
        -type f \
        -name '*.csproj' -o -name '*.sln' -o -name '*.slnx' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
    echo '```'

    section "Arquivos C#"

    echo '```text'
    find "$ROOT/src" \
        -type f \
        -name '*.cs' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
    echo '```'

    section "Arquitetura AURA.AI"

    echo '```text'
    find "$ROOT/src/AURA.AI" \
        -type f \
        -name '*.cs' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
    echo '```'

    section "Símbolos principais"

    echo '```text'
    grep -RniE \
        '^[[:space:]]*(public|private|protected|internal|internal protected)[[:space:]].*(class|interface|record|struct|enum|[A-Za-z0-9_]+[[:space:]]*)' \
        "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 1000
    echo '```'

    section "Fluxo de IA"

    echo '```text'
    grep -RniE \
        'OpenRouterClient|AgentSession|ChatToolsAsync|ChatAsync|ProviderCatalog|AiAssistantService|AiAssistant|AgentTool|AgentToolDefinition|AgentToolCall|OPENAI_API_KEY|Ollama|OpenRouter|tool_calls' \
        "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 1500
    echo '```'

    section "Referências entre componentes"

    echo '```text'
    grep -RniE \
        'new[[:space:]]+OpenRouterClient|new[[:space:]]+AgentSession|new[[:space:]]+AgentTool|OpenRouterOptions|ChatToolsAsync|ChatAsync' \
        "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 1000
    echo '```'

    section "Providers"

    echo '```text'
    grep -RniE \
        'Provider[[:space:]]*=|BaseUrl[[:space:]]*=|Model[[:space:]]*=|ApiKey|openai|openrouter|ollama|groq|google' \
        "$ROOT/src/AURA.AI" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 1000
    echo '```'

    section "Ferramentas do agente"

    echo '```text'
    find "$ROOT/src" \
        -type f \
        \( -iname '*Tool*.cs' -o -iname '*Agent*.cs' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
    echo '```'

    section "Configuração"

    echo '```text'
    find "$ROOT" \
        -maxdepth 4 \
        -type f \
        -name '*.json' -o -name '*.yaml' -o -name '*.yml' -o -name '*.xml' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        ! -path '*/.git/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort |
        head -n 500
    echo '```'

    section "Testes"

    echo '```text'
    find "$ROOT" \
        -type f \
        -name '*Tests.cs' -o -name '*Test.cs' -o -name '*.Tests.csproj' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        ! -path '*/.git/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
    echo '```'

    section "Resumo de tamanho"

    echo '```text'
    echo "Projetos:"
    find "$ROOT/src" -type f -name '*.csproj' 2>/dev/null | wc -l

    echo "Arquivos C#:"
    find "$ROOT/src" -type f -name '*.cs' \
        ! -path '*/bin/*' ! -path '*/obj/*' 2>/dev/null | wc -l

    echo "Linhas C#:"
    find "$ROOT/src" -type f -name '*.cs' \
        ! -path '*/bin/*' ! -path '*/obj/*' \
        -exec cat {} + 2>/dev/null | wc -l
    echo '```'

    section "Observações"

    echo "- Este relatório não inclui valores de `OPENAI_API_KEY`."
    echo "- Não deve conter tokens, senhas ou chaves privadas."
    echo "- `latest.md` é um artefato temporário de análise."
    echo "- A coleta não modifica o código-fonte."

} > "$REPORT"

echo "========================================"
echo " AURA CONTEXT"
echo "========================================"
echo
echo "Relatório criado:"
echo "$REPORT"
echo
echo "Tamanho:"
wc -c < "$REPORT" | awk '{print $1 " bytes"}'
echo
echo "Para visualizar:"
echo "cat \"$REPORT\""
