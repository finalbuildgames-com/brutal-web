using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace FinalBuild.Brutal;
internal sealed class HostServices {
 public byte[]? Progress {get;private set;}
 public IAssetStore Assets {get;}
 public ISaveStore Saves {get;}
 public IJobLayer Jobs {get;}=new HostJobs();
 public readonly ConcurrentQueue<Action> MainThread=[];
 int[] workerIds=[];long sum;bool previousSave;int chunks,bytes;
 public string Metrics=>$"{{\"threaded\":{Jobs.Threaded.ToString().ToLowerInvariant()},\"workers\":[{string.Join(',',workerIds)}],\"checksum\":{sum},\"streamChunks\":{chunks},\"streamBytes\":{bytes},\"previousSave\":{previousSave.ToString().ToLowerInvariant()}}}";
 public HostServices(){
#if BRUTAL_BROWSER
  Assets=new BrowserAssets();Saves=new BrowserSaves();
#else
  Assets=new DesktopAssets(AppContext.BaseDirectory);
  string id="brutal-jam-sample",config=Path.Combine(AppContext.BaseDirectory,"game.json");
  if(File.Exists(config)){using var doc=JsonDocument.Parse(File.ReadAllText(config));id=doc.RootElement.GetProperty("id").GetString()!;}
  if(!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-z0-9-]{1,64}$"))throw new InvalidDataException("Invalid game ID");
  string root=Environment.GetEnvironmentVariable("FBS_SAVE_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FinalBuildGames",id);
  Saves=new DesktopSaves(root);
#endif
 }
 public async Task InitializeAsync(){
  var results=await Jobs.RunAsync(Enumerable.Range(0,8).Select(_=>new JobRequest("sum","10000")).ToArray());
  sum=results.Sum(result=>long.Parse(result.Payload,System.Globalization.CultureInfo.InvariantCulture));workerIds=results.Select(r=>r.ThreadId).Distinct().Order().ToArray();
  if(sum!=399960000)throw new Exception("Job checksum failed");
  bool rejected=false;try{await Jobs.RunAsync(new[]{new JobRequest("fixture-failure","")});}catch(Exception){rejected=true;}if(!rejected)throw new Exception("Job failure was lost");
  using(var cancellation=new CancellationTokenSource()){cancellation.Cancel();try{await Jobs.RunAsync(Array.Empty<JobRequest>(),cancellation.Token);throw new Exception("Job cancellation was lost");}catch(OperationCanceledException){}}
  foreach(string path in Assets.StartupAssets)await Assets.StageAsync(path);
  using(var stream=await Assets.OpenReadAsync("stream.bin")){
   var chunk=new byte[4096];int n;
   while((n=await stream.ReadAsync(chunk))!=0){for(int i=0;i<n;i++)if(chunk[i]!=(byte)((bytes+i)%251))throw new InvalidDataException("Stream content mismatch");bytes+=n;chunks++;}
  }
  if(bytes!=131072||chunks<2)throw new Exception("Stream did not load incrementally");
  var expected=Encoding.UTF8.GetBytes("phase3-save-v1");var previous=await Saves.LoadAsync("probe");previousSave=previous!=null;
  if(previous!=null&&!previous.SequenceEqual(expected))throw new InvalidDataException("Persisted fixture changed");
  Progress=await Saves.LoadAsync("run");
  await Saves.SaveAsync("probe",expected);if(!(await Saves.LoadAsync("probe"))!.SequenceEqual(expected))throw new Exception("Save round trip failed");
 }
 public void Pump(){while(MainThread.TryDequeue(out var action))action();}
}
