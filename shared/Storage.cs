using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace FinalBuild.Brutal;
public interface IAssetStore {
 ValueTask<Stream> OpenReadAsync(string path,CancellationToken cancellation=default);
 Task StageAsync(string path,CancellationToken cancellation=default);
 IReadOnlyList<string> StartupAssets {get;}
}
public interface ISaveStore {
 Task SaveAsync(string key,ReadOnlyMemory<byte> payload,CancellationToken cancellation=default);
 Task<byte[]?> LoadAsync(string key,CancellationToken cancellation=default);
}
internal sealed record SaveEnvelope(int Version,string Sha256,string Data);
[JsonSerializable(typeof(SaveEnvelope))]
internal partial class SaveJson:JsonSerializerContext {}
public static class StorageNames {
 public static string Check(string name){AssetPath.Resolve(name);if(name.Length>1024||name.Split('/').Any(x=>x.Length==0||x=="."||System.Text.Encoding.UTF8.GetByteCount(x)>190)||name.Any(char.IsControl))throw new ArgumentException("Invalid storage name");return name;}
}
internal abstract class AssetStore:IAssetStore {
 protected readonly Dictionary<string,(int Size,string Hash)> Manifest=[];
 readonly System.Collections.Concurrent.ConcurrentDictionary<string,bool> staged=[];
 readonly System.Collections.Concurrent.ConcurrentDictionary<string,SemaphoreSlim> stageGates=[];
 public IReadOnlyList<string> StartupAssets {get;}
 protected AssetStore(string manifest){
  using var json=JsonDocument.Parse(manifest);
  foreach(var item in json.RootElement.EnumerateArray()){
   string name=StorageNames.Check(item.GetProperty("path").GetString()!);int size=item.GetProperty("bytes").GetInt32();
   if(size<0||size>64*1024*1024)throw new InvalidDataException("Asset exceeds 64 MiB limit");
   Manifest.Add(name,(size,item.GetProperty("sha256").GetString()!));
  }
  StartupAssets=Manifest.Keys.Where(x=>x!="beacon.gltf"&&x!="stream.bin").ToArray();
 }
 public abstract ValueTask<Stream> OpenReadAsync(string path,CancellationToken cancellation=default);
 protected abstract Task CommitAsync(string path,byte[] bytes,CancellationToken cancellation);
 public async Task StageAsync(string path,CancellationToken cancellation=default){
  StorageNames.Check(path);var gate=stageGates.GetOrAdd(path,_=>new SemaphoreSlim(1,1));await gate.WaitAsync(cancellation);
  try{if(staged.ContainsKey(path))return;
  if(!Manifest.TryGetValue(path,out var expected))throw new FileNotFoundException("Asset not in manifest",path);
  using var stream=await OpenReadAsync(path,cancellation);using var output=new MemoryStream(expected.Size);
  var chunk=new byte[16384];using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
  while(true){int n=await stream.ReadAsync(chunk,cancellation);if(n==0)break;if(output.Length+n>expected.Size)throw new InvalidDataException("Oversize asset");hash.AppendData(chunk,0,n);await output.WriteAsync(chunk.AsMemory(0,n),cancellation);}
  if(output.Length!=expected.Size||!Convert.ToHexString(hash.GetHashAndReset()).Equals(expected.Hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Asset checksum failed: "+path);
  cancellation.ThrowIfCancellationRequested();await CommitAsync(path,output.ToArray(),cancellation);staged.TryAdd(path,true);
  }finally{gate.Release();}
 }
}
internal sealed class DesktopAssets:AssetStore {
 readonly string root;
 public DesktopAssets(string root):base(File.ReadAllText(Path.Combine(root,"assets.json"))){this.root=Path.Combine(root,"assets");}
 public override ValueTask<Stream> OpenReadAsync(string path,CancellationToken cancellation=default){cancellation.ThrowIfCancellationRequested();StorageNames.Check(path);return ValueTask.FromResult<Stream>(new FileStream(Path.Combine(root,path),FileMode.Open,FileAccess.Read,FileShare.Read,16384,FileOptions.Asynchronous|FileOptions.SequentialScan));}
 protected override Task CommitAsync(string path,byte[] bytes,CancellationToken cancellation)=>Task.CompletedTask; // Published immutable file is already present; Stage verified it.
}
internal abstract class SaveStore:ISaveStore {
 protected abstract Task WriteAsync(string key,string json,CancellationToken cancellation);
 protected abstract Task<string?> ReadAsync(string key,CancellationToken cancellation);
 public async Task SaveAsync(string key,ReadOnlyMemory<byte> payload,CancellationToken cancellation=default){
  StorageNames.Check(key);if(payload.Length>1024*1024)throw new ArgumentException("Save exceeds 1 MiB");
  var envelope=new SaveEnvelope(1,Convert.ToHexString(SHA256.HashData(payload.Span)),Convert.ToBase64String(payload.Span));
  await WriteAsync(key,JsonSerializer.Serialize(envelope,SaveJson.Default.SaveEnvelope),cancellation);
 }
 public async Task<byte[]?> LoadAsync(string key,CancellationToken cancellation=default){
  StorageNames.Check(key);string? json=await ReadAsync(key,cancellation);if(json==null)return null;
  if(json.Length>1500000)throw new InvalidDataException("Save too large");
  var value=JsonSerializer.Deserialize(json,SaveJson.Default.SaveEnvelope)??throw new InvalidDataException("Save is empty");
  if(value.Version!=1)throw new InvalidDataException("Unsupported save version");var bytes=Convert.FromBase64String(value.Data);
  if(bytes.Length>1024*1024||Convert.ToHexString(SHA256.HashData(bytes))!=value.Sha256)throw new InvalidDataException("Save checksum failed");return bytes;
 }
}
internal sealed class DesktopSaves(string root):SaveStore {
 protected override async Task WriteAsync(string key,string json,CancellationToken cancellation){
  string file=Path.Combine(root,key),temp=file+"."+Guid.NewGuid().ToString("N")+".part";Directory.CreateDirectory(Path.GetDirectoryName(file)!);
  try{await File.WriteAllTextAsync(temp,json,cancellation);using(var stream=new FileStream(temp,FileMode.Open,FileAccess.Write,FileShare.None))stream.Flush(true);cancellation.ThrowIfCancellationRequested();File.Move(temp,file,true);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 protected override async Task<string?> ReadAsync(string key,CancellationToken cancellation){string file=Path.Combine(root,key);return File.Exists(file)?await File.ReadAllTextAsync(file,cancellation):null;}
}
