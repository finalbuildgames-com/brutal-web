using System.Runtime.InteropServices;
namespace FinalBuild.Brutal;
public readonly record struct TextureRef(int Id);
public readonly record struct ModelRef(int Id);
public readonly record struct ShaderRef(int Id);
public readonly record struct Rect(float X,float Y,float Width,float Height);
public readonly record struct Vec3(float X,float Y,float Z);
public readonly record struct Camera2(float OffsetX,float OffsetY,float TargetX,float TargetY,float Rotation,float Zoom);
public readonly record struct Camera3(Vec3 Position,Vec3 Target,float Fov=45,bool Orthographic=false);
public readonly record struct Material(ShaderRef Shader,uint Tint);
[StructLayout(LayoutKind.Sequential)]
public struct InputState {
 public float Delta,MouseX,MouseY,Wheel,AxisX,AxisY;
 public int Width,Height,Pressed,Down,Key,AudioUnlocked;
 public bool PointerCaptured,KeyboardCaptured;
}
public interface IRenderLayer : IDisposable {
 TextureRef LoadTexture(string path);
 ModelRef LoadModel(string path);
 ShaderRef LoadShader(string path);
 void ShaderFloat(ShaderRef shader,string name,float value);
 void Clear(uint color);
 void Begin2D(Camera2 camera);
 void End2D();
 void Sprite(TextureRef texture,Rect source,Rect destination,float rotation,uint tint);
 void Text(string text,float x,float y,int size,uint color);
 void Begin3D(Camera3 camera);
 void End3D();
 void Model(ModelRef model,Vec3 position,float rotation,float scale,Material material);
 void Grid(int slices,float spacing);
 void Box(Pose pose,Vec3 half,uint tint);
 void Quad(Rect destination,float rotation,uint tint);
}
public interface IAudioLayer : IDisposable {
 bool Ready {get;}
 void Play(string path,float volumeDb=-12);
 void Update();
 string Metrics {get;}
}
[Flags] public enum PanelActions { None=0,Play=1,Pause=2,Sound=4,Help=8,Reset=16 }
public readonly record struct HudState(int Score,int Goal,bool Playing,bool Muted,bool AudioReady);
public interface IPanelLayer : IDisposable {
 PanelActions Poll(ref InputState input);
 void Set(HudState state);
 void Draw();
 string Metrics {get;}
}
public static class AssetPath {
 public static string Resolve(string name) {
  if(string.IsNullOrWhiteSpace(name)||name.StartsWith('/')||name.Contains("..")||name.Contains(':')||name.Contains('\\'))
   throw new ArgumentException("Asset path must be relative to assets/",nameof(name));
  return "assets/"+name;
 }
}
public readonly record struct Pose(Vec3 Position,float Qx=0,float Qy=0,float Qz=0,float Qw=1);
public interface IPhysicsLayer:IDisposable {
 void Reset();
 int Box2(Vec3 position,Vec3 half,bool dynamic);
 int Box3(Vec3 position,Vec3 half,bool dynamic);
 Pose Read2(int body);
 Pose Read3(int body);
 void Impulse2(int body,Vec3 impulse);
 void Impulse3(int body,Vec3 impulse);
 void Step(float delta);
 string Metrics {get;}
}

public sealed record JobRequest(string Kernel,string Payload);
public sealed record JobResult(string Payload,int ThreadId);
public interface IJobLayer { bool Threaded {get;} Task<JobResult[]> RunAsync(IReadOnlyList<JobRequest> work,CancellationToken cancellation=default); }
