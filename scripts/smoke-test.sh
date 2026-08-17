#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

required=(
  "AURA.sln"
  "src/AURA.Mobile/AURA.Mobile.csproj"
  "src/AURA.Core/AURA.Core.csproj"
  "src/AURA.Agents/AURA.Agents.csproj"
  "tests/AURA.Tests/AURA.Tests.csproj"
)

for path in "${required[@]}"; do
  test -f "$path" || { echo "Missing required file: $path" >&2; exit 1; }
done

for artifact in \
  "src/AURA.Core/bin/Release/net10.0/AURA.Core.dll" \
  "src/AURA.Agents/bin/Release/net10.0/AURA.Agents.dll" \
  "tests/AURA.Tests/bin/Release/net10.0/AURA.Tests.dll"; do
  test -f "$artifact" || { echo "Missing build artifact: $artifact" >&2; exit 1; }
done

mobile_dir="src/AURA.Mobile/bin/Release/net10.0-android"
test -d "$mobile_dir" || { echo "Missing Android build output: $mobile_dir" >&2; exit 1; }

apk_count="$(find "$mobile_dir" -type f -name '*.apk' | wc -l)"
test "$apk_count" -gt 0 || { echo "No Android APK produced under $mobile_dir" >&2; exit 1; }

echo "AURA smoke test OK: core artifacts and Android APK are present."
