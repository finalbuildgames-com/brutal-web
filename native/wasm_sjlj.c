/* .NET 10 wasm workload LLVM19 / Emscripten3.1.56 SjLj ABI bridge.
 * Helpers adapted from Emscripten 3.1.57 system/lib/compiler-rt/emscripten_setjmp.c.
 * Copyright 2020 The Emscripten Authors. MIT license; see licenses/EMSCRIPTEN-LICENSE.txt.
 * https://github.com/dotnet/runtime/issues/129266
 * __wasm_longjmp remains supplied by the workload runtime archive.
 */
#include <stdint.h>
struct br_jmp_buf { void *invocation; uint32_t label; };
void __wasm_setjmp(void *env, uint32_t label, void *invocation) {
    struct br_jmp_buf *buffer = env;
    buffer->invocation = invocation;
    buffer->label = label;
}
uint32_t __wasm_setjmp_test(void *env, void *invocation) {
    struct br_jmp_buf *buffer = env;
    return buffer->invocation == invocation ? buffer->label : 0;
}
