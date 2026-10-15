namespace FinalBuild.Brutal;
// CPU acceptance probe: executes the real shared game and native engines, no window.
internal static class PhysicsCheck {
 public static void Run(){
  string metrics;
  using(var physics=new BrutalPhysics()){
   var game=new SampleGame(new NullRenderer(),new NullAudio(),new NullPanels(),physics);
   float initial2=game.Top2.Position.Y,initial3=game.Top3.Position.Y;
   for(int frame=0;frame<360;frame++)game.Update(new InputState{Delta=1f/60,Width=960,Height=600},PanelActions.None);
   Console.WriteLine("PHYSICS "+game.PhysicsMetrics);
   if(game.Top2.Position.Y>initial2-2||game.Top3.Position.Y>initial3-2)throw new Exception("Physics stacks did not topple");
   if(game.Top2.Position.Y<0||game.Top3.Position.Y<0)throw new Exception("Physics stack left the ground arena");
   metrics=game.PhysicsMetrics;
   game.Update(new InputState{Delta=0},PanelActions.Reset);
   if(game.Top2.Position.Y<6.5f||game.Top3.Position.Y<6.5f)throw new Exception("Physics reset failed");
  }
  Console.WriteLine("PHYSICS "+metrics);Console.WriteLine("PASS Box2D/Jolt gravity, toppling, floor collision and reset");
 }
 sealed class NullAudio:IAudioLayer {public bool Ready=>false;public string Metrics=>"{}";public void Play(string p,float db=-12){}public void Update(){}public void Dispose(){}}
 sealed class NullPanels:IPanelLayer {public string Metrics=>"{}";public PanelActions Poll(ref InputState i)=>0;public void Set(HudState s){}public void Draw(){}public void Dispose(){}}
 sealed class NullRenderer:IRenderLayer {
  public TextureRef LoadTexture(string p)=>new(1);public ModelRef LoadModel(string p)=>new(1);public ShaderRef LoadShader(string p)=>new(1);
  public void ShaderFloat(ShaderRef s,string n,float v){}public void Clear(uint c){}public void Begin2D(Camera2 c){}public void End2D(){}public void Sprite(TextureRef t,Rect s,Rect d,float r,uint c){}public void Text(string t,float x,float y,int s,uint c){}public void Begin3D(Camera3 c){}public void End3D(){}public void Model(ModelRef m,Vec3 p,float r,float s,Material a){}public void Grid(int s,float p){}public void Box(Pose p,Vec3 h,uint t){}public void Quad(Rect r,float a,uint t){}public void Dispose(){}
 }
}
