#include "raylib_bridge.h"
#include "raylib.h"
#include "rlgl.h"
#include "raymath.h"
#include <stdio.h>
#include <string.h>
#ifdef __EMSCRIPTEN__
#include <emscripten.h>
#include <emscripten/html5.h>
#endif
#define SLOTS 128
_Static_assert(sizeof(br_input)==48,"Managed/native input ABI must remain 48 bytes");
static Texture2D textures[SLOTS];
static Model models[SLOTS];
static Shader shaders[SLOTS];
#ifdef __EMSCRIPTEN__
static Wave sound_waves[SLOTS];
static unsigned sound_positions[SLOTS];
static float sound_volumes[SLOTS];
static unsigned char sound_playing[SLOTS];
#else
static Sound sounds[SLOTS];
#endif
static br_audio_renderer audio_renderer;
static void* audio_user;
static float audio_buffer[4096];
static uint64_t audio_consumed;
void br_set_audio_render(br_audio_renderer render,void* user){audio_renderer=render;audio_user=user;}
void br_audio_ack(int frames){if(frames>0)audio_consumed+=(uint64_t)frames;}
uint64_t br_audio_consumed(void){return audio_consumed;}
float* br_audio_pull(int frames){
 if(frames<0||frames>2048)return NULL;
 memset(audio_buffer,0,(size_t)frames*2*sizeof(float));
 if(audio_renderer)audio_renderer(audio_user,audio_buffer,(uint32_t)frames);
#ifdef __EMSCRIPTEN__
 else for(int h=1;h<SLOTS;h++)if(sound_playing[h]){
  float* pcm=sound_waves[h].data;
  for(int f=0;f<frames&&sound_positions[h]<sound_waves[h].frameCount;f++){
   unsigned p=sound_positions[h]++;audio_buffer[2*f]+=pcm[2*p]*sound_volumes[h];audio_buffer[2*f+1]+=pcm[2*p+1]*sound_volumes[h];
  }
  if(sound_positions[h]>=sound_waves[h].frameCount)sound_playing[h]=0;
 }
#endif
 return audio_buffer;
}
static unsigned generations[4][SLOTS];
static unsigned char active[4][SLOTS];
static char error[256];
static int audio_started;
static Color color(uint32_t c) { return (Color){c>>24,c>>16,c>>8,c}; }
static int fail(const char* message) { snprintf(error,sizeof error,"%s",message); return 0; }
static int slot(int kind) {
 for(int i=1;i<SLOTS;i++) if(!active[kind][i]) {
  active[kind][i]=1;
  if(!generations[kind][i]) generations[kind][i]=1;
  return i;
 }
 return fail("Resource capacity exceeded");
}
static int id(int k,int i) { return (int)(generations[k][i]*SLOTS+(unsigned)i); }
static int index_of(int k,int handle) {
 unsigned i=(unsigned)handle%SLOTS, g=(unsigned)handle/SLOTS;
 if(i==0 || !active[k][i] || generations[k][i]!=g) return fail("Invalid or released resource handle");
 return (int)i;
}
static void release(int k,int i) { active[k][i]=0; generations[k][i]++; }
const char* br_error(void) { return error; }
static unsigned char mouse_edges[3][2],key_edges[512];
void br_input_edge(int button,int pressed,int key) {
 if(button>=0&&button<3)mouse_edges[button][pressed?0:1]=1;
 if(pressed&&key>=0&&key<512)key_edges[key]=1;
}
int br_mouse_edge(int b,int released){return b>=0&&b<3?mouse_edges[b][released?1:0]:0;}
int br_key_edge(int key){return key>=0&&key<512?key_edges[key]:0;}
int br_init(int width,int height) {
 error[0]=0; SetTraceLogLevel(LOG_WARNING); 
#ifndef __EMSCRIPTEN__
 SetConfigFlags(FLAG_WINDOW_RESIZABLE);
#endif
 InitWindow(width,height,"RELAY / BRUTAL + raylib"); SetExitKey(KEY_NULL);
#ifndef __EMSCRIPTEN__
 SetTargetFPS(60);
#endif
 return IsWindowReady()?1:fail("raylib window failed");
}
int br_should_close(void) { return WindowShouldClose(); }
void br_input_read(br_input* out) {
 if(!out) return;
#ifdef __EMSCRIPTEN__
 double css_width=0,css_height=0;
 if(emscripten_get_element_css_size("#canvas",&css_width,&css_height)==EMSCRIPTEN_RESULT_SUCCESS&&css_width>0&&css_height>0)
  SetMouseScale((float)(GetScreenWidth()/css_width),(float)(GetScreenHeight()/css_height));
#endif
 Vector2 m=GetMousePosition();
 *out=(br_input){GetFrameTime(),m.x,m.y,GetMouseWheelMove(),
  (float)((IsKeyDown(KEY_D)||IsKeyDown(KEY_RIGHT))-(IsKeyDown(KEY_A)||IsKeyDown(KEY_LEFT))),
  (float)((IsKeyDown(KEY_S)||IsKeyDown(KEY_DOWN))-(IsKeyDown(KEY_W)||IsKeyDown(KEY_UP))),
  GetScreenWidth(),GetScreenHeight(),
  ((IsMouseButtonPressed(MOUSE_BUTTON_LEFT)||br_mouse_edge(0,0))?1:0)|((IsKeyPressed(KEY_SPACE)||br_key_edge(KEY_SPACE))?2:0)|((IsKeyPressed(KEY_ESCAPE)||br_key_edge(KEY_ESCAPE))?4:0)|(IsKeyPressed(KEY_F1)?8:0)|((IsKeyPressed(KEY_R)||br_key_edge(KEY_R))?16:0),
  IsMouseButtonDown(MOUSE_BUTTON_LEFT)?1:0,GetKeyPressed(),audio_started};
}
void br_begin(uint32_t c) { BeginDrawing(); ClearBackground(color(c)); }
void br_end(void) { EndDrawing(); memset(mouse_edges,0,sizeof mouse_edges);memset(key_edges,0,sizeof key_edges); }
int br_texture_load(const char* path) {
 Texture2D t=LoadTexture(path); if(!t.id) return fail("Texture load failed");
 int i=slot(0); if(!i){UnloadTexture(t);return 0;} textures[i]=t;SetTextureFilter(t,TEXTURE_FILTER_POINT);return id(0,i);
}
void br_texture_free(int h) { int i=index_of(0,h); if(i){UnloadTexture(textures[i]);release(0,i);} }
void br_sprite(int h,const float* p,uint32_t tint) {
 int i=index_of(0,h);if(!i||!p)return;
 DrawTexturePro(textures[i],(Rectangle){p[0],p[1],p[2],p[3]},(Rectangle){p[4],p[5],p[6],p[7]},(Vector2){p[6]*.5f,p[7]*.5f},p[8],color(tint));
}
void br_text(const char* text,float x,float y,int size,uint32_t c) { DrawText(text,(int)x,(int)y,size,color(c)); }
int br_model_load(const char* path) {
 Model m=LoadModel(path);if(!m.meshCount||!m.meshes||!m.meshes[0].vaoId){UnloadModel(m);return fail("glTF model load failed");}
 int i=slot(1);if(!i){UnloadModel(m);return 0;}models[i]=m;return id(1,i);
}
int br_model_meshes(int h){int i=index_of(1,h);return i?models[i].meshCount:0;}
void br_model_free(int h){int i=index_of(1,h);if(i){UnloadModel(models[i]);release(1,i);}}
void br_model_draw(int h,const float* p,uint32_t tint,int shader) {
 int i=index_of(1,h);if(!i||!p)return;
 int j=shader?index_of(2,shader):0;
 // Borrow custom shader for this draw; model destruction still owns only its original materials.
 if(j) for(int m=0;m<models[i].materialCount;m++) models[i].materials[m].shader=shaders[j];
 DrawModelEx(models[i],(Vector3){p[0],p[1],p[2]},(Vector3){0,1,0},p[3],(Vector3){p[4],p[5],p[6]},color(tint));
 if(j) for(int m=0;m<models[i].materialCount;m++) models[i].materials[m].shader=(Shader){rlGetShaderIdDefault(),rlGetShaderLocsDefault()};
}
void br_camera2_begin(const float* p){BeginMode2D((Camera2D){{p[0],p[1]},{p[2],p[3]},p[4],p[5]});}
void br_camera2_end(void){EndMode2D();}
void br_camera3_begin(const float* p){BeginMode3D((Camera3D){{p[0],p[1],p[2]},{p[3],p[4],p[5]},{p[6],p[7],p[8]},p[9],(int)p[10]});}
void br_camera3_end(void){EndMode3D();}
int br_shader_load(const char* vs,const char* fs){
 Shader s=LoadShader(vs,fs);if(!s.id||s.id==rlGetShaderIdDefault())return fail("Shader compilation failed");
 int i=slot(2);if(!i){UnloadShader(s);return 0;}shaders[i]=s;return id(2,i);
}
void br_shader_free(int h){int i=index_of(2,h);if(i){UnloadShader(shaders[i]);release(2,i);}}
void br_shader_float(int h,const char* name,float value){int i=index_of(2,h);if(i){int loc=GetShaderLocation(shaders[i],name);if(loc>=0)SetShaderValue(shaders[i],loc,&value,SHADER_UNIFORM_FLOAT);}}
void br_grid(int slices,float spacing){DrawGrid(slices,spacing);}
void br_unlock_audio(void){if(audio_started)return;
#ifdef __EMSCRIPTEN__
 audio_started=1; // The JS host has created and resumed its AudioWorklet device.
#else
 InitAudioDevice();audio_started=IsAudioDeviceReady();
#endif
}
int br_audio_ready(void){return audio_started;}
int br_sound_load(const char* path){if(!audio_started)return fail("Audio locked");
#ifdef __EMSCRIPTEN__
 Wave wave=LoadWave(path);if(!wave.data)return fail("Sound load failed");WaveFormat(&wave,48000,32,2);int i=slot(3);if(!i){UnloadWave(wave);return 0;}sound_waves[i]=wave;
#else
 Sound sound=LoadSound(path);if(!IsSoundValid(sound))return fail("Sound load failed");int i=slot(3);if(!i){UnloadSound(sound);return 0;}sounds[i]=sound;
#endif
 return id(3,i);
}
void br_sound_play(int h,float volume){int i=index_of(3,h);if(!i)return;
#ifdef __EMSCRIPTEN__
 sound_positions[i]=0;sound_volumes[i]=volume;sound_playing[i]=1;
#else
 SetSoundVolume(sounds[i],volume);PlaySound(sounds[i]);
#endif
}
void br_sound_free(int h){int i=index_of(3,h);if(!i)return;
#ifdef __EMSCRIPTEN__
 sound_playing[i]=0;UnloadWave(sound_waves[i]);
#else
 UnloadSound(sounds[i]);
#endif
 release(3,i);
}
int br_screenshot(const char* path){rlDrawRenderBatchActive();Image image=LoadImageFromScreen();if(!image.data)return fail("Screenshot readback failed");int ok=ExportImage(image,path);UnloadImage(image);return ok?1:fail("Screenshot export failed");}
int br_pixel_witness(void){rlDrawRenderBatchActive();Image i=LoadImageFromScreen();if(!i.data)return 0;Color* c=LoadImageColors(i);int n=0;for(int p=0;p<i.width*i.height;p++)if((int)c[p].r+(int)c[p].g+(int)c[p].b>160)n++;UnloadImageColors(c);UnloadImage(i);return n;}
int br_live_resources(void){int n=0;for(int k=0;k<4;k++)for(int i=1;i<SLOTS;i++)n+=active[k][i];return n;}
void br_close(void){
 for(int i=1;i<SLOTS;i++){
  if(active[0][i])br_texture_free(id(0,i));
  if(active[1][i])br_model_free(id(1,i));
  if(active[2][i])br_shader_free(id(2,i));
  if(active[3][i])br_sound_free(id(3,i));
 }
 if(audio_started){
#ifndef __EMSCRIPTEN__
 CloseAudioDevice();
#endif
 audio_started=0;}CloseWindow();
}

void br_quad(const float* p,uint32_t c){DrawRectanglePro((Rectangle){p[0],p[1],p[2],p[3]},(Vector2){p[2]/2,p[3]/2},p[4],color(c));}
void br_box(const float* p,uint32_t c){Vector3 axis;float angle;QuaternionToAxisAngle((Quaternion){p[6],p[7],p[8],p[9]},&axis,&angle);rlPushMatrix();rlTranslatef(p[0],p[1],p[2]);rlRotatef(angle*RAD2DEG,axis.x,axis.y,axis.z);DrawCubeV((Vector3){0},(Vector3){2*p[3],2*p[4],2*p[5]},color(c));DrawCubeWiresV((Vector3){0},(Vector3){2*p[3],2*p[4],2*p[5]},(Color){24,36,52,255});rlPopMatrix();}
