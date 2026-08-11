#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<USAGE
AURA INSPECT

Uso:
  ./scripts/aura-inspect.sh search TERMO
  ./scripts/aura-inspect.sh file CAMINHO
  ./scripts/aura-inspect.sh symbol TERMO
  ./scripts/aura-inspect.sh ai
  ./scripts/aura-inspect.sh tools
  ./scripts/aura-inspect.sh deps
  ./scripts/aura-inspect.sh git
  ./scripts/aura-inspect.sh tree
USAGE
}

search_code() {
    local term="$*"

    grep -RniF "$term" "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 500
}

inspect_file() {
    local file="$1"

    if [[ "$file" != /* ]]; then
        file="$ROOT/$file"
    fi

    if [[ ! -f "$file" ]]; then
        echo "Arquivo não encontrado: $file"
        return 1
    fi

    echo "===== $file ====="
    nl -ba "$file"
}

inspect_symbol() {
    local term="$*"

    grep -RniE \
        "(class|interface|record|struct|enum)[[:space:]]+$term\\b|[[:space:]]+$term[[:space:]]*\\(" \
        "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 500
}

inspect_ai() {
    grep -RniE \
        'OpenRouterClient|AgentSession|ChatToolsAsync|ChatAsync|ProviderCatalog|AiAssistantService|AiAssistant|AgentTool|AgentToolDefinition|AgentToolCall|OPENAI_API_KEY|Ollama|OpenRouter|tool_calls' \
        "$ROOT/src" \
        --include='*.cs' \
        --exclude-dir=bin \
        --exclude-dir=obj \
        2>/dev/null |
        sed "s#^$ROOT/##" |
        head -n 1000
}

inspect_tools() {
    find "$ROOT/src" \
        -type f \
        \( -iname '*Tool*.cs' -o -iname '*Agent*.cs' \) \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
}

inspect_deps() {
    echo "===== Project references ====="

    find "$ROOT/src" -type f -name '*.csproj' \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        -print0 2>/dev/null |
    while IFS= read -r -d '' file; do
        echo
        echo "### ${file#$ROOT/}"
        grep -nE '<ProjectReference|<PackageReference|<TargetFramework' "$file" 2>/dev/null || true
    done
}

inspect_git() {
    git -C "$ROOT" status --short --branch
    echo
    git -C "$ROOT" log -10 --oneline --decorate
}

inspect_tree() {
    find "$ROOT/src" \
        -type f \
        ! -path '*/bin/*' \
        ! -path '*/obj/*' \
        ! -path '*/.git/*' \
        -print 2>/dev/null |
        sed "s#^$ROOT/##" |
        sort
}

case "${1:-}" in
    search)
        shift
        [[ $# -gt 0 ]] || { usage; exit 1; }
        search_code "$@"
        ;;
    file)
        [[ $# -eq 2 ]] || { usage; exit 1; }
        inspect_file "$2"
        ;;
    symbol)
        shift
        [[ $# -gt 0 ]] || { usage; exit 1; }
        inspect_symbol "$@"
        ;;
    ai)
        inspect_ai
        ;;
    tools)
        inspect_tools
        ;;
    deps)
        inspect_deps
        ;;
    git)
        inspect_git
        ;;
    tree)
        inspect_tree
        ;;
    *)
        usage
        exit 1
        ;;
esac
