# Measurements

Measured 2026-10-15. Sizes include both web runtimes and assets.

| Payload / run | Measured result |
|---|---:|
| Both web runtimes and assets, raw | 9.673 MiB |
| Per-file gzip-9 total | 3.436 MiB |
| SDK Brotli sidecars plus uncompressed remaining files | 3.084 MiB |
| itch ZIP upload | 3.445 MiB |
| Chrome cold first frame, serial jobs | 1.879 s |
| Chrome cold first frame, worker jobs | 1.943 s |
| Chrome steady FPS, serial / workers | 60.00 / 60.03 |
| Linux first frame / steady FPS | 0.536 s / 59.73 |

Browser: Chrome 150, headless software rendering, disabled HTTP cache, local HTTP. Cold start includes the 1.5-second branding splash. Linux uses software rendering. Figures are single-run measurements; internet latency and hardware GPU performance will differ.
