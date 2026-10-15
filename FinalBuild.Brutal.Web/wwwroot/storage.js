// Fetch readers and atomic IndexedDB transactions. No retained wasm heap views.
export function createStorage(runtime,manifest,gameId){
 const records=new Map(manifest.map(a=>[a.path,a]));const readers=new Map();let next=1;
 const stats={opens:0,reads:0,bytes:0,active:0,commits:0,saves:0,loads:0};
 const check=name=>{if(typeof name!=='string'||!name||name.startsWith('/')||name.includes('..')||name.includes('\\')||name.includes(':')||name.split('/').some(x=>x===''||x==='.')||/[\x00-\x1f]/.test(name))throw Error('Invalid asset/save path');return name;};
 const assets={
  manifest:()=>JSON.stringify(manifest),
  async open(name){check(name);if(!records.has(name))throw Error('Asset not in manifest');const controller=new AbortController();const response=await fetch('./assets/'+name.split('/').map(encodeURIComponent).join('/'),{signal:controller.signal});if(!response.ok||!response.body)throw Error('Asset fetch failed: '+name);const id=next++;readers.set(id,{reader:response.body.getReader(),controller,pending:new Uint8Array(),received:0,max:records.get(name).bytes});stats.opens++;stats.active++;return id;},
  async read(id,count){const entry=readers.get(id);if(!entry)throw Error('Asset stream closed');if(!Number.isInteger(count)||count<1||count>16384)throw Error('Invalid chunk size');
   if(!entry.pending.length){const {done,value}=await entry.reader.read();if(done){entry.delivered=new Uint8Array();return 0;}entry.received+=value.length;if(entry.received>entry.max)throw Error('Oversize asset stream');entry.pending=value;}
   const out=entry.pending.slice(0,count);entry.pending=entry.pending.subarray(out.length);stats.reads++;stats.bytes+=out.length;entry.delivered=out;return out.length;
  },
  take(id){const e=readers.get(id);if(!e)throw Error("Asset stream closed");const out=e.delivered??new Uint8Array();e.delivered=null;return out;},
  close(id){const e=readers.get(id);if(!e)return;readers.delete(id);stats.active--;e.controller.abort();void e.reader.cancel().catch(()=>{});},
  commit(name,bytes){check(name);if(!records.has(name))throw Error('Asset not in manifest');const fs=runtime.Module.FS;fs.mkdirTree('/assets/'+name.split('/').slice(0,-1).join('/'));fs.writeFile('/assets/'+name+'.part',new Uint8Array(bytes));fs.rename('/assets/'+name+'.part','/assets/'+name);stats.commits++;}
 };
 if(!/^[a-z0-9-]{1,64}$/.test(gameId))throw Error('Invalid game ID');
 let database;
 const openDb=()=>database??=new Promise((resolve,reject)=>{const request=indexedDB.open('brutal-jam-'+gameId,1);request.onupgradeneeded=()=>request.result.createObjectStore('saves');request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error);request.onblocked=()=>reject(Error('Save database upgrade blocked'));});
 const transact=async(mode,key,json)=>{check(key);const db=await openDb();return new Promise((resolve,reject)=>{const tx=db.transaction('saves',mode),store=tx.objectStore('saves');const request=mode==='readwrite'?store.put(json,key):store.get(key);let value;request.onsuccess=()=>{value=request.result;};tx.oncomplete=()=>resolve(value??null);tx.onerror=()=>reject(tx.error??request.error);tx.onabort=()=>reject(tx.error??Error('Save transaction aborted'));});};
 const saves={async write(key,json){if(json.length>1500000)throw Error('Save too large');await transact('readwrite',key,json);stats.saves++;},async read(key){const json=await transact('readonly',key);stats.loads++;return json;}};
 return {assets,saves,stats,close(){for(const id of [...readers.keys()])assets.close(id);if(database)database.then(db=>db.close());}};
}
