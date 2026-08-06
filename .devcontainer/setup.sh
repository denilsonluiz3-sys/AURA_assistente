#!/usr/bin/env bash
# AURA — setup do Codespaces/devcontainer: workload MAUI + Android SDK.
set -euo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit=1C0000000 DOTNET_GCHeapCount=2

ANDROID_HOME=/usr/local/android
SDKMANAGER="$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager"

# No Codespaces o container pode vir Alpine (musl). O .NET Android usa libs
# glibc (libZipSharp). Instalamos gcompat + libstdc++ + zlib e um SDK .NET
# glibc dedicado para o build do Android rodar sobre o musl.
if grep -qi 'alpine\|musl' /etc/os-release 2>/dev/null; then
  echo "Container musl/Alpine detectado; preparando compatibilidade glibc..."
  apk add --no-cache gcompat libstdc++ zlib unzip curl 2>/dev/null || \
    apk add gcompat libstdc++ zlib unzip curl 2>/dev/null || true

  if [ ! -x /opt/dotnet-glibc/dotnet ]; then
    echo "Instalando .NET SDK glibc (linux-x64) em /opt/dotnet-glibc..."
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet-glibc
    rm -f /tmp/dotnet-install.sh
  fi

  export DOTNET_ROOT=/opt/dotnet-glibc
  export PATH="/opt/dotnet-glibc:$PATH"
  export LD_LIBRARY_PATH=/lib
  echo "export DOTNET_ROOT=/opt/dotnet-glibc" > /etc/profile.d/aura-dotnet.sh
  echo 'export PATH="/opt/dotnet-glibc:$PATH"' >> /etc/profile.d/aura-dotnet.sh
  echo 'export LD_LIBRARY_PATH=/lib' >> /etc/profile.d/aura-dotnet.sh
fi

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
