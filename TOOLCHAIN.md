# Toolchain pins

Verified on Linux x86-64, 2026-10-15. Everything installed for this port is user-local.

| Component | Pin | Location |
|---|---|---|
| .NET SDK | **10.0.302** (`global.json`, no roll-forward) | `~/.dotnet` |
| `wasm-tools` workload | manifest **10.0.110 / 10.0.100**, installed with SDK manifest set `10.0.300-manifests.1641d827` | `~/.dotnet/packs` |
| .NET browser runtime, native build packs | **10.0.10** | `~/.dotnet/packs` |
| Emscripten | **3.1.56**, release `9d106be887796484c4aaffc9dc45f48a8810f336` | `~/emsdk` |
| BRUTAL source | upstream `b30520d784f71bc3b3fa7907a75e985d6690e326` | upstream checkout |
| BRUTAL interop generator | **2026.10.1**, analyzer asset only | NuGet; no BRUTAL runtime packages |
| CommunityToolkit.HighPerformance | **8.4.2** | NuGet |
| Microsoft.Extensions.Logging.Console / ObjectPool | **10.0.0** | NuGet |
| Playwright Core | **1.61.0** | local `npm ci` |

All phase 2 native wasm code is compiled and linked with the **.NET workload's own**
Emscripten 3.1.56 pack, whose Clang is .NET-patched LLVM **19.1.0**. The standalone
`~/emsdk` release also says 3.1.56 but has LLVM **19.0.0git**: do not mix their C++
archives. `native-build.py` explicitly selects the workload compiler and a separate
writable cache (`build/dotnet-emscripten-cache`); it does not patch the installed SDK.

The .NET 10 workload has a known SjLj ABI mismatch: LLVM emits `__wasm_setjmp` helpers
missing from its bundled Emscripten compiler-rt. `native/wasm_sjlj.c` supplies the two
helpers adapted from Emscripten **3.1.57**; the runtime still supplies longjmp.
`sjlj_probe.c` executes a real 42-valued round trip before startup. This is necessary
for RmlUi/FreeType. See [dotnet/runtime#129266](https://github.com/dotnet/runtime/issues/129266)
and [upstream helpers](https://github.com/emscripten-core/emscripten/blob/3.1.57/system/lib/compiler-rt/emscripten_setjmp.c).
Compile the probe separately from the helpers to avoid LLVM lowering name collisions.
`-fwasm-exceptions` and `-sSUPPORT_LONGJMP=wasm` are consistent across C++/link steps.

Additional phase 2 pins: raylib **6.0**, commit
`dbc56a87da87d973a9c5baa4e7438a9d20121d28`, SHA256 tarball
`81b06ce7c19cf3b634b0271c23c361ba6ad8bf45fb8b036abbfeb4260ec1e126`.
Optional adapters own additional dependencies, such as RmlUi/FreeType. The default kit does not build them.

Install/reproduce (no FBS checkout needed):

```sh
./bootstrap-toolchain.sh
```

The script installs SDK 10.0.302 via Microsoft's user-local installer and then `wasm-tools` with
`--skip-manifest-update`. On a new installation it clones Emsdk bootstrap commit
`5eb0bde7585670252e8ba05e9d361627bffd08b5`, and installs/
activates compiler 3.1.56. It preserves an existing `~/emsdk` directory. `DOTNET_ROOT` and `EMSDK` can
point to other user-owned installations; no system packages are installed. Git, curl, Python 3, `rg`,
Node 18+ (verification/server), and standard Linux build prerequisites must already be present.

`dotnet workload list`, `dotnet --info`, and `python3 ~/emsdk/upstream/emscripten/emcc.py --version`
show the resolved versions. The install downloaded SDK/compiler packs; no Git LFS assets were needed.
Desktop builds bundled GLFW 3.4 from raylib and needs existing X11/OpenGL development
libraries and a Mesa/display. CMake 3.25+, Ninja and Python 3.12+ are required by native builds. Automated verification uses Xvfb/Mesa software rendering and headless Chrome/SwiftShader.

## Phase 3

- Box2D **3.1.1**, archive SHA256 `fb6ef914b50f4312d7d921a600eabc12318bb3c55a0b8c0b90608fa4488ef2e4`.
- Jolt **5.6.0**, exact revision used by BRUTAL's generated bindings: `e77f175595e64cb44218cc9d9d56fc365ad0e36a`; archive SHA256 `1f32328fb763135de10a244568d6ccb2ed9b1e6593fafe6dc6db5b2719d330bd`. CPU-only; DX12/Vulkan/Metal shader builds disabled; fixed precision, RTTI/EH enabled, cross-platform deterministic option, no x86-only instruction extensions.
- `Microsoft.NET.WebAssembly.Threading` **10.0.10** and `Microsoft.NETCore.App.Runtime.Mono.multithread.browser-wasm` **10.0.10**, restored user-locally through NuGet. `packages-threaded.lock.json` pins the threaded references. No additional system-wide workload is installed.
- Page/game runtime: `WasmEnableThreads=false`. Data-only job companion: `WasmEnableThreads=true`, all native objects built with `-pthread` in a separate build tree. Both are packaged; the companion loads only with isolation/SAB. No worker is used for drawing.
- AudioWorklet uses transferred PCM queues and requires HTTPS/localhost, but no SAB. The deprecated miniaudio ScriptProcessor device is never initialized on web.

Updating these pins requires rebuilding all native variants and rerunning serial/isolated browser and template witnesses. .NET 10 worker support remains experimental; the host avoids its synchronous-export/deputy-thread canvas limitation by keeping game/render execution in the serial runtime.
