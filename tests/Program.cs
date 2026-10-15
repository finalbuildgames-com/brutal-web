using FinalBuild.Brutal;
int checks=0;
void Check(bool ok,string what){checks++;if(!ok)throw new Exception(what);}
Check(System.Runtime.InteropServices.Marshal.OffsetOf<InputState>(nameof(InputState.PointerCaptured)).ToInt32()==48,"Managed input prefix matches scalar native ABI");
var r=new Renderer();var a=new Audio();var p=new Panels();var game=new SampleGame(r,a,p);game.LoadBeacon();
var input=new InputState{Delta=.05f,Width=960,Height=600,AxisX=1};
for(int i=0;i<9;i++)game.Update(input,PanelActions.None);
Check(game.Score==1,"Walking onto a cell collects it");Check(a.Plays==1,"Collection plays exactly one sound");
for(int i=0;i<5;i++)game.Update(input with{AxisX=0},PanelActions.None);
Check(game.Score==1&&a.Plays==1,"Collected cell cannot repeat");
float previous=game.PlayerX;
game.Update(input,PanelActions.Pause);game.Update(input,PanelActions.None);
Check(!game.Playing&&game.PlayerX==previous,"Pause stops movement");
game.Update(input with{AxisX=0,Pressed=2},PanelActions.Play|PanelActions.Sound);
Check(game.Playing&&game.Muted&&a.Plays==1,"Muted actions do not enqueue sound");
game.Update(input with{AxisX=0},PanelActions.Reset);
Check(game.Score==0&&game.PlayerX==300,"Reset restores run state");
game.Update(input with{KeyboardCaptured=true},PanelActions.None);
Check(game.PlayerX==300,"UI keyboard capture blocks game motion");
game.Draw(input);
Check(r.Models==1&&r.Sprites==7&&r.Camera2==1&&r.Camera3==1,"Scene uses 3D glTF and 2D atlas cameras");
Check(AssetPath.Resolve("model/file.gltf")=="assets/model/file.gltf","Relative asset path");
foreach(string bad in new[]{"../private","/tmp/file","https://host/file","a\\b"}){
 bool rejected=false;try{AssetPath.Resolve(bad);}catch(ArgumentException){rejected=true;}Check(rejected,"Reject escaped asset path");
}
var source=Enumerable.Range(0,100).Select(i=>(byte)i).ToArray();
var assets=new Assets(source,source);await Task.WhenAll(assets.StageAsync("fixture"),assets.StageAsync("fixture"));Check(assets.Commits==1,"Concurrent staging commits one validated asset");Check(assets.Committed!.SequenceEqual(source),"Asset staged after digest validation");Check(assets.Reads>2,"Asset reads are incremental");
var broken=new Assets(source,source[..90]);bool corrupt=false;try{await broken.StageAsync("fixture");}catch(InvalidDataException){corrupt=true;}Check(corrupt&&broken.Committed==null,"Truncated asset never commits");
var memory=new Saves();Check(await memory.LoadAsync("missing")==null,"Missing save remains null");await memory.SaveAsync("run",source);Check((await memory.LoadAsync("run"))!.SequenceEqual(source),"Save envelope round trip");
memory.Tamper();corrupt=false;try{await memory.LoadAsync("run");}catch(InvalidDataException){corrupt=true;}Check(corrupt,"Save corruption is rejected");
string root=Path.Combine(AppContext.BaseDirectory,"storage-"+Guid.NewGuid().ToString("N"));
try{var files=new DesktopSaves(root);await files.SaveAsync("run",source);var restarted=new DesktopSaves(root);Check((await restarted.LoadAsync("run"))!.SequenceEqual(source),"Desktop save survives a new store");
 using var cancelled=new CancellationTokenSource();cancelled.Cancel();bool stopped=false;try{await files.SaveAsync("run",new byte[]{99},cancelled.Token);}catch(OperationCanceledException){stopped=true;}Check(stopped,"Cancelled write propagates cancellation");Check((await restarted.LoadAsync("run"))!.SequenceEqual(source),"Cancelled write retains previous save");Check(!Directory.GetFiles(root,"*.part").Any(),"Temporary save files are cleaned up");}
finally{if(Directory.Exists(root))Directory.Delete(root,true);}
var saved=new Saves();var original=new SampleGame(new Renderer(),new Audio(),new Panels(),saves:saved);
for(int i=0;i<9;i++)original.Update(input with{AxisX=1},PanelActions.None);await original.FlushSavesAsync();
var restored=new SampleGame(new Renderer(),new Audio(),new Panels());restored.Restore(await saved.LoadAsync("run"));Check(restored.Score==1&&restored.PlayerX==original.PlayerX,"Flush persists current whole-game position after collection");
Console.WriteLine($"PASS {checks} game/host contract checks");
sealed class Audio:IAudioLayer{public int Plays;public bool Ready=>true;public void Play(string path,float volumeDb=-12)=>Plays++;public void Update(){}public string Metrics=>"{}";public void Dispose(){}}
sealed class Panels:IPanelLayer{public PanelActions Poll(ref InputState i)=>PanelActions.None;public void Set(HudState s){}public void Draw(){}public string Metrics=>"{}";public void Dispose(){}}
sealed class Renderer:IRenderLayer{
 public void Box(Pose p,Vec3 h,uint t){} public void Quad(Rect r,float a,uint t){}
 public int Models,Sprites,Camera2,Camera3;
 public TextureRef LoadTexture(string p)=>new(1);public ModelRef LoadModel(string p)=>new(1);public ShaderRef LoadShader(string p)=>new(1);
 public void ShaderFloat(ShaderRef s,string n,float v){}public void Clear(uint c){}public void Begin2D(Camera2 c)=>Camera2++;public void End2D(){}
 public void Sprite(TextureRef t,Rect s,Rect d,float r,uint c)=>Sprites++;public void Text(string t,float x,float y,int s,uint c){}
 public void Begin3D(Camera3 c)=>Camera3++;public void End3D(){}public void Model(ModelRef m,Vec3 p,float r,float s,Material a)=>Models++;public void Grid(int s,float p){}public void Dispose(){}
}

sealed class Assets:AssetStore {
 readonly byte[] content;public byte[]? Committed;public int Reads,Commits;
 public Assets(byte[] expected,byte[] content):base("[{\"path\":\"fixture\",\"bytes\":"+expected.Length+",\"sha256\":\""+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(expected))+"\"}]"){this.content=content;}
 public override ValueTask<Stream> OpenReadAsync(string p,CancellationToken c=default)=>ValueTask.FromResult<Stream>(new Chunks(content,()=>Reads++));
 protected override async Task CommitAsync(string p,byte[] b,CancellationToken c){await Task.Yield();Committed=b;Interlocked.Increment(ref Commits);}
 sealed class Chunks(byte[] bytes,Action read):MemoryStream(bytes){public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken c=default){read();return base.ReadAsync(buffer[..Math.Min(7,buffer.Length)],c);}}
}
sealed class Saves:SaveStore {
 readonly Dictionary<string,string> values=[];
 protected override Task WriteAsync(string key,string json,CancellationToken c){c.ThrowIfCancellationRequested();values[key]=json;return Task.CompletedTask;}
 protected override Task<string?> ReadAsync(string key,CancellationToken c)=>Task.FromResult(values.GetValueOrDefault(key));
 public void Tamper(){values["run"]=values["run"].Replace("\"Sha256\":\"","\"Sha256\":\"BAD");}
}
