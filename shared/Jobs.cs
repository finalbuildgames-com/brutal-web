using Brutal.Concurrency.Jobs;
using System.Text.Json;
using System.Text.Json.Serialization;
#if BRUTAL_BROWSER && !BRUTAL_THREADS
using System.Runtime.InteropServices.JavaScript;
#endif
namespace FinalBuild.Brutal;
// Kernels exchange data, never render handles, closures or mutable game objects.
public static partial class JobKernels {
 static readonly Dictionary<string,Func<string,CancellationToken,string>> kernels=[];
 static JobKernels(){
  Register("sum",(payload,cancellation)=>{int count=int.Parse(payload,System.Globalization.CultureInfo.InvariantCulture);if(count<0||count>1000000)throw new ArgumentOutOfRangeException(nameof(payload));long sum=0;for(int i=0;i<count;i++){if((i&1023)==0)cancellation.ThrowIfCancellationRequested();sum+=i;}return sum.ToString(System.Globalization.CultureInfo.InvariantCulture);});
  Register("fixture-failure",(_,_)=>throw new InvalidOperationException("job-fixture"));Configure();
 }
 static partial void Configure();
 public static void Register(string name,Func<string,CancellationToken,string> kernel)=>kernels.Add(name,kernel);
 internal static string Execute(JobRequest request,CancellationToken cancellation){
  if(request.Payload.Length>65536||!kernels.TryGetValue(request.Kernel,out var kernel))throw new ArgumentException("Unknown/oversize job");var result=kernel(request.Payload,cancellation);if(result.Length>1024*1024)throw new InvalidOperationException("Job result exceeds 1 MiB");return result;
 }
}
[JsonSerializable(typeof(JobRequest[]))]
[JsonSerializable(typeof(JobResult[]))]
internal partial class JobJson:JsonSerializerContext {}
internal sealed class HostJobs:IJobLayer {
 public bool Threaded {
  get {
#if BRUTAL_BROWSER && !BRUTAL_THREADS
   return BrowserJobs.Available();
#else
   return true;
#endif
  }
 }
 readonly SemaphoreSlim gate=new(1,1);
 public async Task<JobResult[]> RunAsync(IReadOnlyList<JobRequest> work,CancellationToken cancellation=default){
  if(work.Count>64)throw new ArgumentException("At most 64 jobs per batch");await gate.WaitAsync(cancellation);
  try {
#if BRUTAL_BROWSER && !BRUTAL_THREADS
   if(Threaded){string json=JsonSerializer.Serialize(work.ToArray(),JobJson.Default.JobRequestArray);string result=await BrowserJobs.Run(json).WaitAsync(cancellation);return JsonSerializer.Deserialize(result,JobJson.Default.JobResultArray)??throw new InvalidDataException("Empty job result");}
#endif
   var jobs=work.Select(request=>new Work(request,cancellation)).ToArray();
   if(!Threaded){foreach(var job in jobs)job.Run();}
   else{
    var scheduler=new JobScheduler(2,"Jam",threadPriority:ThreadPriority.Normal);
    foreach(var job in jobs)scheduler.Queue(job,1);scheduler.ExecuteJobs();
    while(scheduler.IsExecuting)await Task.Delay(1);
   }
   cancellation.ThrowIfCancellationRequested();var errors=jobs.Where(j=>j.Error!=null).Select(j=>j.Error!).ToArray();if(errors.Length!=0)throw new AggregateException(errors);
   return jobs.Select(j=>j.Result!).ToArray();
  } finally {gate.Release();}
 }
 sealed class Work(JobRequest request,CancellationToken cancellation):IJob {
  public Exception? Error;public JobResult? Result;
  public void Run(){try{cancellation.ThrowIfCancellationRequested();Result=new(JobKernels.Execute(request,cancellation),Environment.CurrentManagedThreadId);}catch(Exception error){Error=error;}}
 }
}
#if BRUTAL_BROWSER && !BRUTAL_THREADS
internal static partial class BrowserJobs {
 [JSImport("jobs.available","brutal-host")]internal static partial bool Available();
 [JSImport("jobs.run","brutal-host")]internal static partial Task<string> Run(string json);
}
#endif
