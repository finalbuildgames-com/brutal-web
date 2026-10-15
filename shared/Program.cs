using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
namespace FinalBuild.Brutal;

public static partial class Program {
 static readonly Stopwatch Clock=Stopwatch.StartNew();
 static IRenderLayer renderer=null!;
 static IAudioLayer audio=null!;
 static IPanelLayer panels=null!;
 static SampleGame game=null!;
 static IPhysicsLayer physics=null!;
 static HostServices services=null!;
 static int frames,failed,closed;
 static long lastReport;
 static int firstPixels;
 [LibraryImport("brutal_raylib",EntryPoint="fbs_run_loop")]
 private static unsafe partial void RunLoop(delegate* unmanaged[Cdecl]<void> tick);
 [LibraryImport("brutal_raylib",EntryPoint="fbs_cancel_loop")]
 private static partial void CancelLoop();
 [LibraryImport("brutal_raylib",EntryPoint="br_sjlj_roundtrip")]
 private static partial int SjljRoundtrip();
#if BRUTAL_BROWSER && BRUTAL_THREADS
 [JSExport]
 public static async Task<string> RunJobs(string json){
  var requests=JsonSerializer.Deserialize(json,JobJson.Default.JobRequestArray)??throw new ArgumentException("Empty job batch");
  var results=await new HostJobs().RunAsync(requests);
  return JsonSerializer.Serialize(results,JobJson.Default.JobResultArray);
 }
#endif
 public static async Task Main(string[] args){
  if(OperatingSystem.IsBrowser() && SjljRoundtrip()!=42)throw new Exception("Wasm SjLj roundtrip failed");
  if(!OperatingSystem.IsBrowser())Directory.SetCurrentDirectory(AppContext.BaseDirectory);
  if(!OperatingSystem.IsBrowser())NativeLibrary.SetDllImportResolver(typeof(Program).Assembly,(name,_,_)=>{
   var path=Path.Combine(AppContext.BaseDirectory,$"lib{name}.so");
   return NativeLibrary.TryLoad(path,out nint handle)?handle:0;
  });
  if(!OperatingSystem.IsBrowser()&&args.Contains("--physics-check")){PhysicsCheck.Run();return;}
  services=new HostServices();await services.InitializeAsync();
  if(!OperatingSystem.IsBrowser()){RunHost();await game.FlushSavesAsync();}
 }
#if BRUTAL_BROWSER && !BRUTAL_THREADS
 [JSExport]
#endif
 public static void StartHost()=>RunHost();
 static unsafe void RunHost(){
  RaylibRenderer.Check(RaylibNative.Init(960,600));
  if(!OperatingSystem.IsBrowser())RaylibNative.UnlockAudio();
  renderer=new RaylibRenderer();HostAdapters.Create(renderer,out audio,out panels);
  physics=new BrutalPhysics();game=new(renderer,audio,panels,physics,services.Saves);game.Restore(services.Progress);
  _=LoadBeaconAsync();
  Console.WriteLine("ready: raylib 6.0 / shared game");
  if(OperatingSystem.IsBrowser())RunLoop(&BrowserTick);
  else {
   int limit=int.TryParse(Environment.GetEnvironmentVariable("FBS_SAMPLE_FRAMES"),out int n)?n:int.MaxValue;
   try {while(RaylibNative.ShouldClose()==0&&frames<limit&&failed==0)Tick();}
   finally {Console.WriteLine("WITNESS "+Metrics());Shutdown();Console.WriteLine($"resources={RaylibNative.LiveResources()}");}
   if(failed!=0)Environment.ExitCode=1;
  }
 }
 [UnmanagedCallersOnly(CallConvs=[typeof(CallConvCdecl)])]
 static void BrowserTick(){try{Tick();}catch(Exception e){failed=1;Console.Error.WriteLine(e);CancelLoop();}}
 static async Task LoadBeaconAsync(){try{await services.Assets.StageAsync("beacon.gltf");services.MainThread.Enqueue(()=>game.LoadBeacon());}catch(Exception error){services.MainThread.Enqueue(()=>throw new Exception("Async model load failed",error));}}
 static unsafe void Tick(){
  services.Pump();
  InputState input=default;RaylibNative.ReadInput(&input);
  if(!OperatingSystem.IsBrowser()&&Environment.GetEnvironmentVariable("FBS_SAMPLE_FRAMES")!=null){
   if(frames<40){input.AxisX=1;input.Delta=1f/60;}
  }
  var action=panels.Poll(ref input);
  if(input.Key!=0)Console.WriteLine($"input:key key={input.Key}");
  if((input.Pressed&1)!=0)Console.WriteLine($"input:mouse x={input.MouseX} y={input.MouseY}");
  game.Update(input,action);audio.Update();game.Draw(input);
  if(frames==0){firstPixels=RaylibNative.PixelWitness();if(firstPixels<500)throw new Exception("Raylib pixel witness failed");Console.WriteLine($"first-frame ms={Clock.Elapsed.TotalMilliseconds:F2} pixels={firstPixels}");}
  if(!OperatingSystem.IsBrowser()&&frames==(int.TryParse(Environment.GetEnvironmentVariable("FBS_SAMPLE_SCREENSHOT_FRAME"),out int shot)?shot:119)){var path=Environment.GetEnvironmentVariable("FBS_SAMPLE_SCREENSHOT");if(path!=null)RaylibRenderer.Check(RaylibNative.Screenshot(path));}
  RaylibNative.End();frames++;
  if(frames%120==0){long now=Clock.ElapsedMilliseconds;Console.WriteLine($"fps={120000.0/(now-lastReport):F2} frames={frames}");lastReport=now;}
 }
#if BRUTAL_BROWSER && !BRUTAL_THREADS
 [JSExport]
#endif
 public static string Metrics()=>"{"+$"\"frames\":{frames},\"failed\":{failed},\"pixels\":{firstPixels},\"score\":{game.Score},\"playerX\":{game.PlayerX.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"playing\":{game.Playing.ToString().ToLowerInvariant()},\"muted\":{game.Muted.ToString().ToLowerInvariant()},\"services\":{services.Metrics},\"modelReady\":{game.ModelReady.ToString().ToLowerInvariant()},\"physics\":{game.PhysicsMetrics},\"audio\":{audio.Metrics},\"ui\":{panels.Metrics}"+"}";
#if BRUTAL_BROWSER && !BRUTAL_THREADS
 [JSExport]
#endif
 public static Task FlushSaves()=>game.FlushSavesAsync();
#if BRUTAL_BROWSER && !BRUTAL_THREADS
 [JSExport]
#endif
 public static int Shutdown(){
  if(closed!=0)return RaylibNative.LiveResources();closed=1;
  if(OperatingSystem.IsBrowser())CancelLoop();
  panels.Dispose();audio.Dispose();physics.Dispose();renderer.Dispose();
  int leaked=RaylibNative.LiveResources();RaylibNative.Close();
  if(leaked!=0)throw new Exception($"Live raylib resources at shutdown: {leaked}");
  return leaked;
 }
}
