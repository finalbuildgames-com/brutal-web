#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export EMSDK="${EMSDK:-$HOME/emsdk}"
if [[ ! -x "$DOTNET_ROOT/dotnet" ]] || ! "$DOTNET_ROOT/dotnet" --list-sdks | rg -q '^10\.0\.302 '; then
    installer="$(mktemp)"
    trap 'rm -f "$installer"' EXIT
    curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$installer"
    nice -n 19 bash "$installer" --version 10.0.302 --install-dir "$DOTNET_ROOT" --no-path
fi
nice -n 19 "$DOTNET_ROOT/dotnet" workload install wasm-tools --skip-manifest-update
if [[ ! -e "$EMSDK" ]]; then
    git clone --no-checkout https://github.com/emscripten-core/emsdk.git "$EMSDK"
    git -C "$EMSDK" checkout 5eb0bde7585670252e8ba05e9d361627bffd08b5
fi
nice -n 19 python3 "$EMSDK/emsdk.py" install 3.1.56
python3 "$EMSDK/emsdk.py" activate 3.1.56
