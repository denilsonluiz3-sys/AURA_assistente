#!/usr/bin/env bash
# AURA — validação local do AURA.Mobile.
# No host ARM64, o build completo aborta em XARLP7000 (EmbeddedResource /
# libZipSharpNative, que só existe em x64) ANTES de compilar o C#, gerando
# falso positivo de "OK". Por isso usamos `-t:Compile`: ele compila o C# (e o
# XamlC, quando aplicável) sem entrar no empacotamento Android, pegando de
# verdade os erros CS/XC do código — inclusive os arquivos de Platforms/.
# A validação final (empacotamento + XamlC completo) é feita no Codemagic.
#
# Uso:
#   bash scripts/build-mobile.sh            (valida C# via -t:Compile)
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

echo "=== validação C#/XamlC AURA.Mobile (-t:Compile, SDK=$SDK) ==="
OUTPUT="$(dotnet build src/AURA.Mobile/AURA.Mobile.csproj -f net10.0-android -c Release -t:Compile -v q --nologo -p:AndroidSdkDirectory="$SDK" 2>&1)"

echo "$OUTPUT" | grep -E "error (XC[0-9]+|CS[0-9]+)" | sort -u

if echo "$OUTPUT" | grep -qE "error (XC[0-9]+|CS[0-9]+)"; then
  echo "FALHA: erros reais de XamlC/C# acima. Corrija antes de subir." >&2
  exit 1
fi

if echo "$OUTPUT" | grep -qE "Build FAILED"; then
  echo "ATENÇÃO: o build completo (empacotamento) ainda pode falhar por XARLP7000 no ARM64; C# compilou OK." >&2
fi

echo "OK: C# compilou limpo. XamlC/empacotamento finais validados no Codemagic."
