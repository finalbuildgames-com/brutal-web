#include <setjmp.h>
/* Exercise the real compiler/runtime round trip before loading any font. */
int br_sjlj_roundtrip(void) {
    jmp_buf buffer;
    int result = setjmp(buffer);
    if (result == 0) longjmp(buffer, 42);
    return result;
}
