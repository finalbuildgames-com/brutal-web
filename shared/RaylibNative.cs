using System.Runtime.InteropServices;
using Brutal.Interop;
namespace FinalBuild.Brutal;
internal static unsafe partial class RaylibNative
{
    [LibraryImport("brutal_raylib", EntryPoint="br_box")] internal static partial void Box(float* pose,uint tint);
    [LibraryImport("brutal_raylib", EntryPoint="br_quad")] internal static partial void Quad(float* rect,uint tint);
    [LibraryImport("brutal_raylib", EntryPoint = "br_init")]
    internal static partial int Init(int width, int height);
    [LibraryImport("brutal_raylib", EntryPoint = "br_close")]
    internal static partial void Close();
    [LibraryImport("brutal_raylib", EntryPoint = "br_should_close")]
    internal static partial int ShouldClose();
    [LibraryImport("brutal_raylib", EntryPoint = "br_input_read")]
    internal static partial void ReadInput(InputState* input);
    [LibraryImport("brutal_raylib", EntryPoint = "br_begin")]
    internal static partial void Begin(uint color);
    [LibraryImport("brutal_raylib", EntryPoint = "br_end")]
    internal static partial void End();
    [LibraryImport("brutal_raylib", EntryPoint = "br_texture_load", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial int TextureLoad(string path);
    [LibraryImport("brutal_raylib", EntryPoint = "br_texture_free")]
    internal static partial void TextureFree(int handle);
    [LibraryImport("brutal_raylib", EntryPoint = "br_sprite")]
    internal static partial void Sprite(int texture, float* rectangles, uint tint);
    [LibraryImport("brutal_raylib", EntryPoint = "br_text", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial void Text(string text, float x, float y, int size, uint color);
    [LibraryImport("brutal_raylib", EntryPoint = "br_model_load", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial int ModelLoad(string path);
    [LibraryImport("brutal_raylib", EntryPoint = "br_model_free")]
    internal static partial void ModelFree(int handle);
    [LibraryImport("brutal_raylib", EntryPoint = "br_model_meshes")]
    internal static partial int ModelMeshes(int handle);
    [LibraryImport("brutal_raylib", EntryPoint = "br_model_draw")]
    internal static partial void ModelDraw(int handle, float* transform, uint tint, int shader);
    [LibraryImport("brutal_raylib", EntryPoint = "br_camera2_begin")]
    internal static partial void Camera2Begin(float* camera);
    [LibraryImport("brutal_raylib", EntryPoint = "br_camera2_end")]
    internal static partial void Camera2End();
    [LibraryImport("brutal_raylib", EntryPoint = "br_camera3_begin")]
    internal static partial void Camera3Begin(float* camera);
    [LibraryImport("brutal_raylib", EntryPoint = "br_camera3_end")]
    internal static partial void Camera3End();
    [LibraryImport("brutal_raylib", EntryPoint = "br_shader_load", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial int ShaderLoad(string vertex, string fragment);
    [LibraryImport("brutal_raylib", EntryPoint = "br_shader_free")]
    internal static partial void ShaderFree(int handle);
    [LibraryImport("brutal_raylib", EntryPoint = "br_shader_float", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial void ShaderFloat(int handle, string name, float value);
    [LibraryImport("brutal_raylib", EntryPoint = "br_grid")]
    internal static partial void Grid(int slices, float spacing);
    [LibraryImport("brutal_raylib", EntryPoint = "br_error")]
    internal static partial nint Error();
    [LibraryImport("brutal_raylib", EntryPoint = "br_live_resources")]
    internal static partial int LiveResources();
    [LibraryImport("brutal_raylib", EntryPoint = "br_unlock_audio")]
    internal static partial void UnlockAudio();
    [LibraryImport("brutal_raylib", EntryPoint = "br_audio_ready")]
    internal static partial int AudioReady();
    [LibraryImport("brutal_raylib", EntryPoint = "br_sound_load", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial int SoundLoad(string path);
    [LibraryImport("brutal_raylib", EntryPoint = "br_sound_play")]
    internal static partial void SoundPlay(int sound, float volume);
    [LibraryImport("brutal_raylib", EntryPoint = "br_sound_free")]
    internal static partial void SoundFree(int sound);
    [LibraryImport("brutal_raylib", EntryPoint = "br_screenshot", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(InteropStringMarshaller))]
    internal static partial int Screenshot(string path);
    [LibraryImport("brutal_raylib", EntryPoint = "br_pixel_witness")]
    internal static partial int PixelWitness();
}
