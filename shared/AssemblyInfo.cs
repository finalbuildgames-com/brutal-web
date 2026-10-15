using System.Runtime.CompilerServices;
// BRUTAL OpenGl's calli signatures pass blittable handle structs by value.
// WASM cannot generate runtime marshalling IL for those calls.
[assembly: DisableRuntimeMarshalling]
