#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$here"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
mkdir -p build
exec 9>build/.build.lock
flock 9
python3 native-build.py desktop
python3 prepare-assets.py
out="$here/FinalBuild.Brutal.Desktop/bin/Release/net10.0/linux-x64/publish"
[[ ! -L "$out" ]] || { echo 'Refusing symlinked publish directory' >&2; exit 1; }
rm -rf "$out"
nice -n 19 "$DOTNET_ROOT/dotnet" publish FinalBuild.Brutal.Desktop -c Release -r linux-x64 --self-contained true -m:1 -p:UseSharedCompilation=false -p:RestoreLockedMode=true
cp -a build/assets "$out/"
cp build/assets.json "$out/assets.json"
cp game.json "$out/game.json"
find "$(cat build/NativeDesktopPath.txt)" -name '*.so*' -exec cp -t "$out" {} +
python3 notices.py "$out"
printf 'Run: %s/FinalBuild.Brutal.Desktop\n' "$out"
