#include <emscripten.h>
#include <GLFW/glfw3.h>
// This archive's basename maps BRUTAL's LibraryImport("glfw") to the static
// pinvoke table. Emscripten supplies the actual glfw* JS port symbols.
EMSCRIPTEN_KEEPALIVE void fbs_glfw_link_anchor(void) {}
