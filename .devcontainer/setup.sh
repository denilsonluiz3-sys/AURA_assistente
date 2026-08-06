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
  # apk v3 buga com pacote unico; passar sempre >=2 pacotes.
  apk add --no-cache gcompat libstdc++ zlib icu-libs tzdata unzip curl 2>/dev/null || true

  if [ ! -x /opt/dotnet-glibc/dotnet ]; then
    echo "Instalando .NET SDK glibc (linux-x64) em /opt/dotnet-glibc..."
    # Tarball glibc direto (dotnet-install.sh pega a build musl no Alpine).
    curl -fsSL -o /tmp/dotnet-sdk.tar.gz \
      https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.302/dotnet-sdk-10.0.302-linux-x64.tar.gz
    mkdir -p /opt/dotnet-glibc
    tar -xzf /tmp/dotnet-sdk.tar.gz -C /opt/dotnet-glibc
    rm -f /tmp/dotnet-sdk.tar.gz
  fi

  if [ ! -x /opt/jdk17/bin/java ]; then
    echo "Instalando JDK 17 (Temurin glibc) em /opt/jdk17..."
    curl -fsSL -o /tmp/jdk17.tar.gz \
      "https://api.adoptium.net/v3/binary/latest/17/ga/linux/x64/jdk/hotspot/normal/eclipse"
    mkdir -p /opt/jdk17
    tar -xzf /tmp/jdk17.tar.gz -C /opt/jdk17 --strip-components=1
    rm -f /tmp/jdk17.tar.gz
  fi

  export DOTNET_ROOT=/opt/dotnet-glibc
  export PATH="/opt/dotnet-glibc:/opt/jdk17/bin:$PATH"
  export LD_LIBRARY_PATH=/lib
  export JAVA_HOME=/opt/jdk17
  {
    echo "export DOTNET_ROOT=/opt/dotnet-glibc"
    echo 'export PATH="/opt/dotnet-glibc:/opt/jdk17/bin:$PATH"'
    echo 'export LD_LIBRARY_PATH=/lib'
    echo "export JAVA_HOME=/opt/jdk17"
  } > /etc/profile.d/aura-dotnet.sh
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
go version 2>&1 | head -1

echo "=== Pronto. Para gerar o APK: bash scripts/build-apk-codespaces.sh ==="
