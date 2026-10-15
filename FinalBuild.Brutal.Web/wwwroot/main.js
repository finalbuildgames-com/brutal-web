import {createAudio} from './audio.js';
import {createStorage} from './storage.js';
const threaded=globalThis.crossOriginIsolated&&typeof SharedArrayBuffer==='function';
const {default:createDotnetRuntime}=await import('./single/_framework/dotnet.js');
const canvas = document.querySelector('#canvas');
const status = document.querySelector('#status');
window.fbs = { firstFrameMs:null, fps:[], inputs:[], errors:[], assets:0 };
try {
 let jobRuntime,jobExports;
 if(threaded){const {default:createJobsRuntime}=await import('./threaded/_framework/dotnet.js');jobRuntime=await createJobsRuntime({print:message=>console.log(message),printErr:message=>{window.fbs.errors.push(String(message));console.error(message);}});jobExports=await jobRuntime.getAssemblyExports(jobRuntime.getConfig().mainAssemblyName);}
 const jobs={available:()=>threaded,run:json=>jobExports.FinalBuild.Brutal.Program.RunJobs(json)};
 const runtime = await createDotnetRuntime({ canvas,
  print(message) {
   console.log(message);
   if(message.startsWith('input:'))window.fbs.inputs.push(message);
   if(message.startsWith('first-frame')){window.fbs.firstFrameMs=performance.now();status.textContent='';}
   if(message.startsWith('fps='))window.fbs.fps.push(Number(message.match(/fps=([\d.]+)/)[1]));
  },
  printErr(message){window.fbs.errors.push(String(message));console.error(message);}
 });
 const manifestResponse=await fetch('./assets.json');
 if(!manifestResponse.ok)throw Error('Asset manifest failed');
 const manifest=await manifestResponse.json();
 const config=await (await fetch('./game.json')).json();document.title=config.title;
 const storage=createStorage(runtime,manifest,config.id);window.fbs.storage=storage.stats;window.fbs.threaded=threaded;
 runtime.setModuleImports('brutal-host',{assets:storage.assets,saves:storage.saves,jobs});
 const exports=await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
 window.fbs.metrics=()=>JSON.parse(exports.FinalBuild.Brutal.Program.Metrics());
 const audio=createAudio(runtime);window.fbs.audio=audio.stats;
 window.fbs.shutdown=async()=>{await exports.FinalBuild.Brutal.Program.FlushSaves();await audio.close();storage.close();const result=exports.FinalBuild.Brutal.Program.Shutdown();if(jobRuntime)jobRuntime.exit(0);return result;};
 // Context creation/resume begins in the first real gesture; the worklet loads asynchronously.
 const unlock=()=>{canvas.focus();audio.unlock().catch(error=>{window.fbs.errors.push(String(error));console.error(error);});};
 canvas.addEventListener('pointerdown',unlock,{once:true});
 canvas.addEventListener('keydown',unlock,{once:true});
 const keyCode=e=>({Escape:256,Enter:257,Tab:258,Backspace:259,F1:290,ArrowRight:262,ArrowLeft:263,ArrowDown:264,ArrowUp:265}[e.code]??e.keyCode);
 canvas.addEventListener('pointerdown',e=>runtime.Module._br_input_edge([0,2,1][e.button]??-1,1,-1));
 window.addEventListener('pointerup',e=>runtime.Module._br_input_edge([0,2,1][e.button]??-1,0,-1));
 canvas.addEventListener('keydown',e=>{if(!e.repeat)runtime.Module._br_input_edge(-1,1,keyCode(e));});
 canvas.addEventListener('wheel',event=>event.preventDefault(),{passive:false});
 await new Promise(resolve=>setTimeout(resolve,Math.max(0,1500-performance.now())));
 document.querySelector('#branding').remove();
 await runtime.runMain();
 exports.FinalBuild.Brutal.Program.StartHost();
} catch(error) {window.fbs.errors.push(String(error));status.textContent=String(error);console.error(error);}
