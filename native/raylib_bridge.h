#ifndef BRUTAL_RAYLIB_H
#define BRUTAL_RAYLIB_H
#include <stdint.h>
#if defined(_WIN32)
#define BR_API __declspec(dllexport)
#else
#define BR_API __attribute__((visibility("default")))
#endif
#ifdef __cplusplus
extern "C" {
#endif
/* Scalar/pointer ABI, same sizes on wasm32 and Linux x64. Handles never cross as raylib structs. */
typedef struct br_input {
 float dt, mouse_x, mouse_y, wheel, axis_x, axis_y;
 int width, height, pressed, down, key, audio_unlocked;
} br_input;
BR_API void br_box(const float* pose_half,uint32_t tint);
BR_API void br_quad(const float* rect_rotation,uint32_t tint);
BR_API int br_init(int width, int height);
BR_API void br_close(void);
BR_API int br_should_close(void);
BR_API void br_input_read(br_input* out);
BR_API void br_begin(uint32_t color);
BR_API void br_end(void);
BR_API int br_texture_load(const char* path);
BR_API void br_texture_free(int handle);
BR_API void br_sprite(int texture, const float* src_dst_rotation, uint32_t tint);
BR_API void br_text(const char* text, float x, float y, int size, uint32_t color);
BR_API int br_model_load(const char* path);
BR_API void br_model_free(int handle);
BR_API int br_model_meshes(int handle);
BR_API void br_model_draw(int handle, const float* position_rotation_scale, uint32_t tint, int shader);
BR_API void br_camera2_begin(const float* camera);
BR_API void br_camera2_end(void);
BR_API void br_camera3_begin(const float* camera);
BR_API void br_camera3_end(void);
BR_API int br_shader_load(const char* vertex, const char* fragment);
BR_API void br_shader_free(int handle);
BR_API void br_shader_float(int handle, const char* name, float value);
BR_API void br_grid(int slices, float spacing);
BR_API const char* br_error(void);
BR_API int br_live_resources(void);
BR_API void br_unlock_audio(void);
BR_API int br_audio_ready(void);
typedef void (*br_audio_renderer)(void*,float*,uint32_t);
BR_API void br_set_audio_render(br_audio_renderer render,void* user);
BR_API float* br_audio_pull(int frames);
BR_API void br_audio_ack(int frames);
BR_API uint64_t br_audio_consumed(void);
BR_API int br_sound_load(const char* path);
BR_API void br_sound_play(int handle, float volume);
BR_API void br_sound_free(int handle);
BR_API int br_screenshot(const char* path);
BR_API int br_pixel_witness(void);
/* Browser events retain taps that begin and end between two render frames. */
BR_API void br_input_edge(int button,int pressed,int key);
BR_API int br_mouse_edge(int button,int released);
BR_API int br_key_edge(int key);

#ifdef __cplusplus
}
#endif
#endif
