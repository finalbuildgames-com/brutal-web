#if BRUTAL_BROWSER
using System.Runtime.InteropServices.JavaScript;
namespace FinalBuild.Brutal;
internal static partial class BrowserStorage {
 [JSImport("assets.manifest","brutal-host")] internal static partial string Manifest();
 [JSImport("assets.open","brutal-host")] internal static partial Task<int> Open(string path);
 [JSImport("assets.read","brutal-host")]
 internal static partial Task<int> Read(int id,int count);
 [JSImport("assets.take","brutal-host")]
 [return:JSMarshalAs<JSType.Array<JSType.Number>>]
 internal static partial byte[] Take(int id);
 [JSImport("assets.close","brutal-host")] internal static partial void Close(int id);
 [JSImport("assets.commit","brutal-host")] internal static partial void Commit(string path,[JSMarshalAs<JSType.Array<JSType.Number>>]byte[] bytes);
 [JSImport("saves.write","brutal-host")] internal static partial Task Write(string key,string json);
 [JSImport("saves.read","brutal-host")] internal static partial Task<string?> Load(string key);
}
internal sealed class BrowserAssets():AssetStore(BrowserStorage.Manifest()) {
 public override async ValueTask<Stream> OpenReadAsync(string path,CancellationToken cancellation=default){StorageNames.Check(path);cancellation.ThrowIfCancellationRequested();int id=await BrowserStorage.Open(path);if(cancellation.IsCancellationRequested){BrowserStorage.Close(id);cancellation.ThrowIfCancellationRequested();}return new BrowserStream(id);}
 protected override Task CommitAsync(string path,byte[] bytes,CancellationToken cancellation){cancellation.ThrowIfCancellationRequested();BrowserStorage.Commit(path,bytes);return Task.CompletedTask;}
 sealed class BrowserStream(int id):Stream {
  bool disposed;long position;
  public override bool CanRead=>!disposed;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>position;set=>throw new NotSupportedException();}
  public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellation=default){ObjectDisposedException.ThrowIf(disposed,this);if(buffer.Length==0)return 0;cancellation.ThrowIfCancellationRequested();var pending=BrowserStorage.Read(id,Math.Min(buffer.Length,16384));try{await pending.WaitAsync(cancellation);}catch(OperationCanceledException){BrowserStorage.Close(id);try{await pending;}catch{}throw;}var bytes=BrowserStorage.Take(id);cancellation.ThrowIfCancellationRequested();bytes.CopyTo(buffer);position+=bytes.Length;return bytes.Length;}
  public override int Read(byte[] buffer,int offset,int count)=>throw new NotSupportedException("Browser reads are asynchronous");
  protected override void Dispose(bool disposing){if(!disposed){disposed=true;BrowserStorage.Close(id);}base.Dispose(disposing);}
  public override void Flush(){}public override long Seek(long o,SeekOrigin origin)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int n)=>throw new NotSupportedException();
 }
}
internal sealed class BrowserSaves:SaveStore {
 protected override Task WriteAsync(string key,string json,CancellationToken cancellation){cancellation.ThrowIfCancellationRequested();return BrowserStorage.Write(key,json);}
 protected override Task<string?> ReadAsync(string key,CancellationToken cancellation){cancellation.ThrowIfCancellationRequested();return BrowserStorage.Load(key);}
}
#endif
