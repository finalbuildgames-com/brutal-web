#!/usr/bin/env bash
set -euo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$here"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}" EMSDK="${EMSDK:-$HOME/emsdk}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 EMCC_CORES=2 BINARYEN_CORES=2
[[ "$("$DOTNET_ROOT/dotnet" --version)" == 10.0.302 ]]
mkdir -p build
exec 9>build/.build.lock
flock 9
python3 prepare-assets.py
for threads in false true; do
 BRUTAL_THREADS="$threads" python3 native-build.py web
 python3 - "$here/build/publish-$threads" <<'PYCODE'
import sys,shutil
from pathlib import Path
p=Path(sys.argv[1])
if p.is_symlink():raise SystemExit('Refusing symlinked publish directory')
if p.exists():shutil.rmtree(p)
PYCODE
 nice -n 19 "$DOTNET_ROOT/dotnet" publish FinalBuild.Brutal.Web -c Release -m:1 -p:UseSharedCompilation=false -p:RestoreLockedMode="${FBS_RESTORE_LOCKED:-true}" -p:WasmEnableThreads="$threads" -p:BaseIntermediateOutputPath="obj/$threads/" -p:BaseOutputPath="bin/$threads/" -p:WasmCachePath="$here/build/dotnet-emscripten-cache" -o "$here/build/publish-$threads"
done
python3 stage-web.py
python3 package.py
printf 'Local: node %s/serve.mjs\nIsolated: FBS_ISOLATE=1 node %s/serve.mjs\nVerify: cd %s && npm ci && nice -n 19 node verify-browser.mjs\n' "$here" "$here" "$here"
