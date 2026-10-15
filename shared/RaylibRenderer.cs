using System.Runtime.InteropServices;
namespace FinalBuild.Brutal;
internal sealed unsafe class RaylibRenderer : IRenderLayer {
 readonly List<int> textures=[],models=[],shaders=[];
 bool disposed;
 internal static int Check(int id) => id!=0?id:throw new InvalidOperationException(Marshal.PtrToStringUTF8(RaylibNative.Error()));
 public TextureRef LoadTexture(string path){int id=Check(RaylibNative.TextureLoad(AssetPath.Resolve(path)));textures.Add(id);return new(id);}
 public ModelRef LoadModel(string path){int id=Check(RaylibNative.ModelLoad(AssetPath.Resolve(path)));models.Add(id);if(RaylibNative.ModelMeshes(id)<=0)throw new Exception("glTF has no uploaded meshes");return new(id);}
 public ShaderRef LoadShader(string path){
  string profile=OperatingSystem.IsBrowser()?"es300":"gl330";
  int id=Check(RaylibNative.ShaderLoad(AssetPath.Resolve($"{path}.{profile}.vs"),AssetPath.Resolve($"{path}.{profile}.fs")));
  shaders.Add(id);return new(id);
 }
 public void ShaderFloat(ShaderRef h,string name,float value)=>RaylibNative.ShaderFloat(h.Id,name,value);
 public void Clear(uint color)=>RaylibNative.Begin(color);
 public void Begin2D(Camera2 c){float* p=stackalloc float[]{c.OffsetX,c.OffsetY,c.TargetX,c.TargetY,c.Rotation,c.Zoom};RaylibNative.Camera2Begin(p);}
 public void End2D()=>RaylibNative.Camera2End();
 public void Sprite(TextureRef t,Rect s,Rect d,float rotation,uint tint){float* p=stackalloc float[]{s.X,s.Y,s.Width,s.Height,d.X,d.Y,d.Width,d.Height,rotation};RaylibNative.Sprite(t.Id,p,tint);}
 public void Text(string text,float x,float y,int size,uint color)=>RaylibNative.Text(text,x,y,size,color);
 public void Begin3D(Camera3 c){float* p=stackalloc float[]{c.Position.X,c.Position.Y,c.Position.Z,c.Target.X,c.Target.Y,c.Target.Z,0,1,0,c.Fov,c.Orthographic?1:0};RaylibNative.Camera3Begin(p);}
 public void End3D()=>RaylibNative.Camera3End();
 public void Model(ModelRef m,Vec3 at,float rotation,float scale,Material material){float* p=stackalloc float[]{at.X,at.Y,at.Z,rotation,scale,scale,scale};RaylibNative.ModelDraw(m.Id,p,material.Tint,material.Shader.Id);}
 public void Box(Pose pose,Vec3 half,uint tint){float* p=stackalloc float[]{pose.Position.X,pose.Position.Y,pose.Position.Z,half.X,half.Y,half.Z,pose.Qx,pose.Qy,pose.Qz,pose.Qw};RaylibNative.Box(p,tint);}
 public void Quad(Rect r,float rotation,uint tint){float* p=stackalloc float[]{r.X,r.Y,r.Width,r.Height,rotation};RaylibNative.Quad(p,tint);}
 public void Grid(int slices,float spacing)=>RaylibNative.Grid(slices,spacing);
 public void Dispose(){if(disposed)return;disposed=true;foreach(int id in models)RaylibNative.ModelFree(id);foreach(int id in textures)RaylibNative.TextureFree(id);foreach(int id in shaders)RaylibNative.ShaderFree(id);}
}
internal sealed class RaylibAudio : IAudioLayer {
 readonly Dictionary<string,int> sounds=[];
 public bool Ready=>RaylibNative.AudioReady()!=0;
 public void Play(string path,float volumeDb=-12){if(!Ready)return;if(!sounds.TryGetValue(path,out int id)){id=RaylibRenderer.Check(RaylibNative.SoundLoad(AssetPath.Resolve(path)));sounds.Add(path,id);}RaylibNative.SoundPlay(id,MathF.Pow(10,Math.Clamp(volumeDb,-60,0)/20));}
 public void Update(){}
 public string Metrics=>"{\"backend\":\"raylib\"}";
 public void Dispose(){foreach(int id in sounds.Values)RaylibNative.SoundFree(id);sounds.Clear();}
}
// Public fallback. The FBS jam adapter replaces this with real RmlUi panels.
internal sealed class RaylibPanels(IRenderLayer renderer) : IPanelLayer {
 HudState state;
 public PanelActions Poll(ref InputState input)=>PanelActions.None;
 public void Set(HudState value)=>state=value;
 public void Draw(){renderer.Text($"+ {state.Score}/{state.Goal}",24,24,24,0xE5B65BFF);renderer.Text(state.Playing?"> RUN":"|| PAUSE",24,58,18,0xC7D9E5FF);}
 public string Metrics=>"{\"backend\":\"raylib\"}";
 public void Dispose(){}
}
public static partial class HostAdapters {
 static partial void Configure(ref IAudioLayer audio,ref IPanelLayer panels);
 public static void Create(IRenderLayer renderer,out IAudioLayer audio,out IPanelLayer panels){audio=new RaylibAudio();panels=new RaylibPanels(renderer);Configure(ref audio,ref panels);}
}
