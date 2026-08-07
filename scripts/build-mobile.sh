#!/usr/bin/env bash
# AURA — validação local do AURA.Mobile (compilação sem gerar APK).
# O mobile NÃO está no AURA.sln de propósito: requer workload maui-android,
# SDK Android e, para Release assinado, os segredos do keystore (só no Actions).
# Este script é o jeito canônico de pegar quebra do mobile localmente.
#
# Uso:
#   bash scripts/build-mobile.sh            (compila -t:Compile)
#   ANDROID_SDK_DIR=/caminho bash scripts/build-mobile.sh
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SDK="${ANDROID_SDK_DIR:-/opt/android-sdk}"
if [ ! -d "$SDK" ]; then
  echo "ERRO: SDK Android não encontrado em $SDK" >&2
  echo "Defina ANDROID_SDK_DIR apontando para a pasta do SDK." >&2
  exit 1
fi

echo "=== compilando AURA.Mobile (AndroidSdkDirectory=$SDK) ==="
dotnet build src/AURA.Mobile/AURA.Mobile.csproj -t:Compile -v q --nologo \
  -p:AndroidSdkDirectory="$SDK"

echo "=== OK: AURA.Mobile compila sem erros ==="
