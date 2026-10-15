using System.Text.Json;
using System.Text.Json.Serialization;
namespace FinalBuild.Brutal;

// The exact same file runs on desktop and web. Platform work belongs to host interfaces.
public sealed class SampleGame {
 readonly IRenderLayer renderer;
 readonly IAudioLayer audio;
 readonly IPanelLayer panels;
 readonly IPhysicsLayer? physics;
 readonly ISaveStore? saves;
 Task save=Task.CompletedTask;
 public Task FlushSavesAsync(){Save();return save;}
 readonly int[] boxes2=new int[7],boxes3=new int[7];
 bool kicked;
 public string PhysicsMetrics=>physics==null?"{}":System.FormattableString.Invariant($"{{\"worlds\":{physics.Metrics},\"top2x\":{Top2.Position.X},\"top2y\":{Top2.Position.Y},\"top3x\":{Top3.Position.X},\"top3y\":{Top3.Position.Y}}}");
 public Pose Top2=>physics?.Read2(boxes2[^1])??default;
 public Pose Top3=>physics?.Read3(boxes3[^1])??default;
 readonly TextureRef atlas;
 ModelRef beacon;
 public bool ModelReady=>beacon.Id!=0;
 public void LoadBeacon()=>beacon=renderer.LoadModel("beacon.gltf");
 readonly ShaderRef shader;
 readonly (float X,float Y)[] cells=[(390,350),(650,430),(760,250),(440,170),(830,470),(620,150)];
 readonly bool[] collected=new bool[6];
 float x=300,y=350,time,zoom=1;
 bool playing=true,muted;
 int score;
 public int Score=>score;
 public float PlayerX=>x;
 public bool Playing=>playing;
 public bool Muted=>muted;
 public SampleGame(IRenderLayer renderer,IAudioLayer audio,IPanelLayer panels,IPhysicsLayer? physics=null,ISaveStore? saves=null){
  this.saves=saves;
  this.physics=physics;
  SpawnPhysics();
  this.renderer=renderer;this.audio=audio;this.panels=panels;
  atlas=renderer.LoadTexture("atlas.png");

  shader=renderer.LoadShader("beacon");
  Publish();
 }
 void SpawnPhysics(){
  if(physics!=null){physics.Box2(new(0,-.5f,0),new(12,.5f,1),false);physics.Box3(new(0,-.5f,0),new(12,.5f,12),false);for(int i=0;i<7;i++){boxes2[i]=physics.Box2(new(i*.15f,.6f+i*1.03f,0),new(.48f,.48f,.48f),true);boxes3[i]=physics.Box3(new((i%2)*.06f,.6f+i*1.03f,0),new(.48f,.48f,.48f),true);}}
 }
 public void Restore(byte[]? bytes){
  if(bytes==null)return;var state=JsonSerializer.Deserialize(bytes,ProgressJson.Default.JamProgress)??throw new InvalidDataException("Empty game save");
  if(state.Version!=1||!float.IsFinite(state.X)||!float.IsFinite(state.Y)||!float.IsFinite(state.Zoom)||state.X<240||state.X>920||state.Y<130||state.Y>550||state.Zoom<.75f||state.Zoom>1.3f||state.Mask<0||state.Mask>63)throw new InvalidDataException("Invalid game save");
  x=state.X;y=state.Y;zoom=state.Zoom;muted=state.Muted;score=0;for(int i=0;i<6;i++){collected[i]=(state.Mask&(1<<i))!=0;if(collected[i])score++;}Publish();
 }
 void Save(){
  if(saves==null)return;int mask=0;for(int i=0;i<6;i++)if(collected[i])mask|=1<<i;
  var bytes=JsonSerializer.SerializeToUtf8Bytes(new JamProgress(1,x,y,mask,muted,zoom),ProgressJson.Default.JamProgress);var prior=save;
  save=WriteAsync(prior,bytes);
 }
 async Task WriteAsync(Task prior,byte[] bytes){await prior;await saves!.SaveAsync("run",bytes);}
 void Publish()=>panels.Set(new(score,cells.Length,playing,muted,audio.Ready));
 public void Update(InputState input,PanelActions action){
  if(save.IsFaulted)save.GetAwaiter().GetResult();
  if((input.Pressed&16)!=0&&!input.KeyboardCaptured)action|=PanelActions.Reset;
  if((input.Pressed&4)!=0)action|=PanelActions.Pause;
  if((action&PanelActions.Play)!=0)playing=true;
  if((action&PanelActions.Pause)!=0)playing=!playing;
  if((action&PanelActions.Sound)!=0)muted=!muted;
  if((action&PanelActions.Reset)!=0){score=0;x=300;y=350;Array.Fill(collected,false);time=0;kicked=false;if(physics!=null){physics.Reset();SpawnPhysics();}Save();}
  float dt=Math.Clamp(input.Delta,0,.05f);
  time+=dt;
  if(!input.PointerCaptured)zoom=Math.Clamp(zoom+input.Wheel*.05f,.75f,1.3f);
  if(playing){
   if(!input.KeyboardCaptured){
    float length=MathF.Sqrt(input.AxisX*input.AxisX+input.AxisY*input.AxisY);
    if(length>0){x=Math.Clamp(x+input.AxisX/length*210*dt,240,920);y=Math.Clamp(y+input.AxisY/length*210*dt,130,550);}
    if((input.Pressed&2)!=0&&!muted)audio.Play("collect.wav");
   }
   for(int i=0;i<cells.Length;i++)if(!collected[i]&&MathF.Abs(x-cells[i].X)<25&&MathF.Abs(y-cells[i].Y)<25){
    collected[i]=true;score++;Save();if(!muted)audio.Play("collect.wav");
   }
  }
  if(playing&&physics!=null){physics.Step(dt);if(!kicked&&time>1){for(int i=1;i<boxes2.Length;i++){physics.Impulse2(boxes2[i],new(i*.2f,0,0));physics.Impulse3(boxes3[i],new(i*.2f,0,(i%2)*.2f));}kicked=true;}}
  if((action&(PanelActions.Pause|PanelActions.Sound))!=0)Save();
  Publish();
 }
 public void Draw(InputState input){
  renderer.Clear(0x101B2BFF);
  renderer.ShaderFloat(shader,"uPulse",.75f+.25f*MathF.Sin(time*2));
  renderer.Begin3D(new(new(11,8,13),new(-1,2,0),48));
  renderer.Grid(24,1);
  if(beacon.Id!=0)renderer.Model(beacon,new(4,.9f,-3),time*24,1.1f,new(shader,0xFFCC72FF));
  if(physics!=null)for(int i=0;i<boxes3.Length;i++)renderer.Box(physics.Read3(boxes3[i]),new(.48f,.48f,.48f),i%2==0?0xE8C170FF:0xC59951FF);
  renderer.End3D();
  if(physics!=null){renderer.Quad(new(410,516,672,12),0,0x354B61FF);for(int i=0;i<boxes2.Length;i++){var p=physics.Read2(boxes2[i]);renderer.Quad(new(410+p.Position.X*28,510-p.Position.Y*28,26,26),-2*MathF.Atan2(p.Qz,p.Qw)*180/MathF.PI,i%2==0?0x55DECEFFU:0x399D9AFFU);}}
  // A real Camera2D transforms atlas sprites; game coordinates remain stable.
  renderer.Begin2D(new(580,320,580,320,0,zoom));
  for(int i=0;i<cells.Length;i++)if(!collected[i])
   renderer.Sprite(atlas,new(32,0,32,32),new(cells[i].X,cells[i].Y,36,36),time*40,0xFFFFFFFF);
  renderer.Sprite(atlas,new(((int)(time*8)%2)*32,32,32,32),new(x,y,52,52),input.AxisX*8,0xFFFFFFFF);
  renderer.End2D();
  renderer.Text("RELAY",input.Width-155,26,30,0xE9C27AFF);
  renderer.Text("06",input.Width-66,input.Height-55,28,0x56677AFF);
  if(score==cells.Length)renderer.Text("COMPLETE",input.Width/2-80,input.Height-80,28,0x8AD1B0FF);
  panels.Draw();
 }
}

public sealed record JamProgress(int Version,float X,float Y,int Mask,bool Muted,float Zoom);
[JsonSerializable(typeof(JamProgress))]
internal partial class ProgressJson:JsonSerializerContext {}
