#!/usr/bin/env bash
set -euo pipefail

# AURA local helper for PRoot Ubuntu.
# Default: diagnose only. --apply performs only safe, deterministic repairs.
# The official Android build remains on GitHub Actions.

ROOT="${AURA_ROOT:-}"
if [[ -z "$ROOT" ]]; then
  if [[ -d "$HOME/AURA_assistente/.git" ]]; then
    ROOT="$HOME/AURA_assistente"
  elif [[ -d "$(pwd)/.git" ]]; then
    ROOT="$(pwd)"
  else
    echo "ERRO: não encontrei ~/AURA_assistente. Use AURA_ROOT=/caminho/para/AURA_assistente." >&2
    exit 2
  fi
fi

cd "$ROOT"

MODE="diagnose"
if [[ "${1:-}" == "--apply" ]]; then
  MODE="apply"
elif [[ "${1:-}" == "--diagnose" || -z "${1:-}" ]]; then
  MODE="diagnose"
else
  echo "Uso: $0 [--diagnose|--apply]" >&2
  exit 2
fi

section() {
  printf '\n== %s ==\n' "$1"
}

section "AURA PRoot diagnóstico"
echo "Projeto: $ROOT"
echo "Modo: $MODE"

test -f AURA.sln || { echo "ERRO: AURA.sln ausente"; exit 1; }
test -f global.json || { echo "ERRO: global.json ausente"; exit 1; }

echo "SDK esperado: $(grep -E '"version"' global.json | head -1 | tr -d ' ' || true)"

for cmd in git bash; do
  command -v "$cmd" >/dev/null 2>&1 || echo "AVISO: $cmd não encontrado"
done

if command -v dotnet >/dev/null 2>&1; then
  echo "dotnet: $(dotnet --version)"
  dotnet --info | sed -n '1,80p' || true
else
  echo "AVISO: dotnet não está disponível no PRoot. O build deve continuar sendo feito pelo GitHub Actions."
fi

section "Arquivos críticos"
for path in \
  AURA.sln \
  global.json \
  src/AURA.Mobile/AURA.Mobile.csproj \
  src/AURA.Agents/AURA.Agents.csproj \
  src/AURA.Abstractions/AURA.Abstractions.csproj \
  .github/workflows/build-android-apk.yml \
  .github/workflows/ai-failure-diagnostics.yml; do
  if [[ -f "$path" ]]; then
    echo "OK  $path"
  else
    echo "MISS $path"
  fi
done

section "Diagnóstico de logs locais"
LOG_FILE="${AURA_LOG_FILE:-}"
if [[ -n "$LOG_FILE" && -f "$LOG_FILE" ]]; then
  echo "Log: $LOG_FILE"
  grep -E -i 'NETSDK[0-9]+|CS[0-9]{4}|NU[0-9]{4}|XA[0-9]+|error|failed|exception|fatal' "$LOG_FILE" | tail -n 120 || true
elif [[ -f docs/ai/CI_FAILURE_LATEST.md ]]; then
  echo "Último diagnóstico publicado: docs/ai/CI_FAILURE_LATEST.md"
  grep -E -i 'NETSDK[0-9]+|CS[0-9]{4}|NU[0-9]{4}|XA[0-9]+|categoria|workflow|commit' docs/ai/CI_FAILURE_LATEST.md | head -n 80 || true
else
  echo "Nenhum log local ou relatório CI disponível."
fi

if [[ "$MODE" == "diagnose" ]]; then
  section "Próxima ação"
  cat <<'EOF'
Nenhuma alteração foi feita.

Para uma correção determinística:
  bash scripts/ai/proot/aura-repair.sh --apply

Para falhas do GitHub Actions, use primeiro o relatório:
  docs/ai/CI_FAILURE_LATEST.md

O build Android oficial continua no GitHub Actions; este script não executa build Android automaticamente.
EOF
  exit 0
fi

section "Correções seguras"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Não é possível aplicar correções .NET: dotnet não está instalado."
  echo "Use o GitHub Actions ou instale o SDK conforme global.json."
  exit 3
fi

# Remove apenas artefatos de build conhecidos. Não toca em código, git ou dados do usuário.
find src tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +

echo "Artefatos bin/obj removidos."

dotnet restore src/AURA.Mobile/AURA.Mobile.csproj

echo "Restore concluído."

# Deterministic workload repair only when the installed SDK supports the command.
if dotnet workload list >/dev/null 2>&1; then
  if ! dotnet workload list 2>/dev/null | grep -qi 'maui-android'; then
    echo "maui-android não aparece instalado. Instalando o workload solicitado pelo CI..."
    dotnet workload install maui-android
  else
    echo "maui-android já está disponível."
  fi
fi

section "Validação"
dotnet build src/AURA.Agents/AURA.Agents.csproj --no-restore

echo
echo "Correção local concluída. O build Android completo deve ser validado no GitHub Actions."
