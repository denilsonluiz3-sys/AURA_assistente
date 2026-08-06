#!/usr/bin/env bash
# AURA — setup do Codespaces/devcontainer: workload MAUI + Android SDK.
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit=1C0000000 DOTNET_GCHeapCount=2

ANDROID_HOME=/usr/local/android
SDKMANAGER="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"

echo "=== .NET workload MAUI Android ==="
dotnet workload install maui-android

echo "=== Android SDK (cmdline-tools) ==="
if [ ! -x "$SDKMANAGER" ]; then
  mkdir -p "$ANDROID_HOME/cmdline-tools"
  curl -fsSL -o /tmp/cmdline-tools.zip \
    https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip
  unzip -q /tmp/cmdline-tools.zip -d "$ANDROID_HOME/cmdline-tools"
  mv "$ANDROID_HOME/cmdline-tools/cmdline-tools" "$ANDROID_HOME/cmdline-tools/latest"
  rm -f /tmp/cmdline-tools.zip
fi

echo "=== Android SDK (licenças + componentes) ==="
yes | "$SDKMANAGER" --licenses >/dev/null 2>&1 || true
"$SDKMANAGER" "platform-tools" "platforms;android-36" "build-tools;36.0.0"

echo "=== Runtimes disponíveis ==="
dotnet --version
java -version 2>&1 | head -1
node --version
python3 --version
go version

echo "=== Pronto. Para gerar o APK: bash scripts/build-apk-codespaces.sh ==="
