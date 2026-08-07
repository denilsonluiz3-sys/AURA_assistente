#!/usr/bin/env bash
# AURA — validação local do AURA.Mobile pegando TODOS os erros de uma vez.
# Roda o build em Release (ativa o XamlC, que valida as propriedades do XAML;
# build Debug/-t:Compile NÃO pega esses erros).
# O empacotamento local falha em ARM64 (libZipSharpNative só existe em x64) —
# isso é tolerado. Qualquer erro XamlC (XCxxxx) ou de C# (CSxxxx) é falha real.
#
# Uso:
#   bash scripts/build-mobile.sh            (build Release com XamlC)
#   ANDROID_SDK_DIR=/caminho bash scripts/build-mobile.sh
set -u

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

SDK="${ANDROID_SDK_DIR:-/opt/android-sdk}"
if [ ! -d "$SDK" ]; then
  echo "ERRO: SDK Android não encontrado em $SDK (defina ANDROID_SDK_DIR)." >&2
  exit 1
fi

export DOTNET_GCHeapHardLimit=0x50000000 DOTNET_GCConserveMemory=9 DOTNET_GCHeapHardLimitPercent=50

echo "=== build Release AURA.Mobile (XamlC ativo, SDK=$SDK) ==="
OUTPUT="$(dotnet build src/AURA.Mobile/AURA.Mobile.csproj -f net10.0-android -c Release -v q --nologo -p:AndroidSdkDirectory="$SDK" 2>&1)"

echo "$OUTPUT" | grep -E "error (XC[0-9]+|CS[0-9]+)" | sort -u

if echo "$OUTPUT" | grep -qE "error (XC[0-9]+|CS[0-9]+)"; then
  echo "FALHA: erros reais de XamlC/C# acima. Corrija antes de subir." >&2
  exit 1
fi

echo "OK: XamlC e C# limpos. Empacotamento local pode falhar por ARM64; use o Codemagic para gerar o APK."
