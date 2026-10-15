#include <emscripten.h>
EMSCRIPTEN_KEEPALIVE void fbs_run_loop(void (*tick)(void)) {
    // Return to the browser; no blocking loop, pthreads, or Asyncify.
    emscripten_set_main_loop(tick, 0, 0);
}
EMSCRIPTEN_KEEPALIVE void fbs_cancel_loop(void) { emscripten_cancel_main_loop(); }
