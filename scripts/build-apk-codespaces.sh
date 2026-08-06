#!/usr/bin/env bash
# AURA — build do APK Android dentro do Codespaces/devcontainer.
# Uso: bash scripts/build-apk-codespaces.sh
set -euo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit="${DOTNET_GCHeapHardLimit:-1C0000000}"
export DOTNET_GCHeapCount="${DOTNET_GCHeapCount:-2}"

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Restore ==="
dotnet restore src/AURA.Mobile/AURA.Mobile.csproj

echo "=== Build APK (Release) ==="
dotnet build src/AURA.Mobile/AURA.Mobile.csproj -c Release --no-restore \
  -p:AndroidPackageFormats=apk \
  -p:AcceptAndroidSDKLicenses=true

APK="$(ls -1 src/AURA.Mobile/bin/Release/net10.0-android/*-Signed.apk 2>/dev/null | head -1 || true)"
if [ -z "$APK" ]; then
  APK="$(ls -1 src/AURA.Mobile/bin/Release/net10.0-android/*.apk 2>/dev/null | head -1 || true)"
fi

if [ -z "$APK" ]; then
  echo "ERRO: nenhum APK foi gerado em src/AURA.Mobile/bin/Release/net10.0-android/." >&2
  exit 1
fi

echo
echo "================================================================"
echo "APK pronto: $ROOT/$APK"
echo "Baixe via Explorer/terminal: gh codespace cp ou botão direito no arquivo."
echo "================================================================"
