import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {chromium} from 'playwright-core';
import {serve} from './serve.mjs';
const here=path.dirname(fileURLToPath(import.meta.url));
const root=process.argv[2]??path.join(here,'build/itch');
const evidence=process.env.FBS_EVIDENCE_DIR??path.join(here,'build/evidence');
const prefix=process.env.FBS_EVIDENCE_PREFIX??'phase3-';
assert.match(prefix,/^[a-z0-9-]*$/);fs.mkdirSync(evidence,{recursive:true});
const server=serve(root,process.env.FBS_HTTP_ENCODING??'identity');
await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
const browser=await chromium.launch({executablePath:process.env.CHROME??'/usr/bin/google-chrome',headless:true,
 args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--no-sandbox','--mute-audio']});
const errors=[],logs=[],responses=[];
let page,complete=false;
try {
 const context=await browser.newContext({viewport:{width:1100,height:850},serviceWorkers:'block'});
 page=await context.newPage();page.setDefaultTimeout(15000);
 await page.addInitScript(()=>{
  window.audioProbe={contexts:[],analysers:[],peak:0,scriptProcessors:0};
  const Original=window.AudioContext??window.webkitAudioContext;
  class ObservedContext extends Original {constructor(...args){super(...args);window.audioProbe.contexts.push(this);}}
  Original.prototype.createScriptProcessor=function(){window.audioProbe.scriptProcessors++;throw Error("ScriptProcessor is forbidden");};
  window.AudioContext=ObservedContext;if(window.webkitAudioContext)window.webkitAudioContext=ObservedContext;
  const connect=AudioNode.prototype.connect;
  AudioNode.prototype.connect=function(destination,...args){
   if(destination instanceof AudioDestinationNode){
    const analyser=this.context.createAnalyser();analyser.fftSize=2048;
    window.audioProbe.analysers.push(analyser);
    connect.call(this,analyser);connect.call(analyser,destination,...args);return destination;
   }
   return connect.call(this,destination,...args);
  };
  setInterval(()=>{for(const a of window.audioProbe.analysers){const pcm=new Float32Array(a.fftSize);a.getFloatTimeDomainData(pcm);for(const value of pcm)window.audioProbe.peak=Math.max(window.audioProbe.peak,Math.abs(value));}},10);
 });
 const cdp=await context.newCDPSession(page);await cdp.send('Network.enable');await cdp.send('Network.setCacheDisabled',{cacheDisabled:true});
 page.on('console',m=>{logs.push(`${m.type()}: ${m.text()}`);if(logs.length<35)console.log(logs.at(-1));if(m.type()==='error')errors.push(m.text());});
 page.on('pageerror',e=>errors.push(String(e)));
 page.on('response',r=>responses.push({url:r.url(),status:r.status(),type:r.headers()['content-type']}));
 await page.goto(`http://127.0.0.1:${server.address().port}/`);
 await page.waitForFunction(()=>window.fbs?.firstFrameMs!=null||window.fbs?.errors.length,null,{timeout:90000});
 assert.deepEqual(await page.evaluate(()=>window.fbs.errors),[]);
 const before=await page.evaluate(()=>({metrics:window.fbs.metrics(),contexts:audioProbe.contexts.length}));
 assert.equal(before.contexts,0,'WebAudio starts only on input');
 assert.ok(before.metrics.physics.top2y>6&&before.metrics.physics.top3y>6,'Both native stacks begin upright');
 await page.screenshot({path:path.join(evidence,`${prefix}stacks-before.png`)});
 const canvas=page.locator('canvas');
 const click=async(x,y)=>{const box=await canvas.boundingBox();await canvas.click({position:{x:x*box.width/960,y:y*box.height/600},delay:0});};
 await click(300,350);
 await page.waitForFunction(()=>window.fbs.audio?.ready);
 await page.keyboard.down('Space');await page.waitForTimeout(90);await page.keyboard.up('Space');
 await page.waitForFunction(()=>window.audioProbe.peak>.001,null,{timeout:10000});
 await page.keyboard.down('KeyD');await page.waitForTimeout(560);await page.keyboard.up('KeyD');
 await page.waitForFunction(()=>window.fbs.metrics().score>=1);
 const afterMove=await page.evaluate(()=>window.fbs.metrics());
 assert.equal(afterMove.services.checksum,399960000);assert.equal(afterMove.services.streamBytes,131072);assert.ok(afterMove.services.streamChunks>1);
 assert.equal(afterMove.services.threaded,process.env.FBS_ISOLATE==='1');
 assert.ok(afterMove.services.workers.length>=1);
 if(process.env.FBS_ISOLATE==='1')assert.ok(afterMove.services.workers.every(id=>id>1),'Jobs ran on actual managed worker threads');

 assert.ok(afterMove.playerX>before.metrics.playerX+45);
 if(afterMove.ui.backend==='RmlUi'){
  await click(100,146);
  await page.waitForFunction(()=>!window.fbs.metrics().playing);
  await page.screenshot({path:path.join(evidence,`${prefix}menu.png`)});
  await click(465,250);
  await page.waitForFunction(()=>window.fbs.metrics().playing);
  await click(100,207);
  await page.waitForFunction(()=>window.fbs.metrics().muted);
  await click(56,290);
  await page.waitForFunction(()=>window.fbs.metrics().ui.help);
  await page.screenshot({path:path.join(evidence,`${prefix}help.png`)});
  await click(56,290);
  await page.waitForFunction(()=>!window.fbs.metrics().ui.help);
  // CSS-resized embed must retain matching render/input coordinates.
  await page.setViewportSize({width:800,height:800});
  await click(100,146);await page.waitForFunction(()=>!window.fbs.metrics().playing);
  await click(465,250);await page.waitForFunction(()=>window.fbs.metrics().playing);
  await page.setViewportSize({width:1100,height:850});
 }
 await page.mouse.move(1000,800);
 await page.waitForFunction(()=>window.fbs.fps.length>=2&&window.fbs.metrics().modelReady&&window.fbs.metrics().physics.top2y<5&&window.fbs.metrics().physics.top3y<5);
 const result=await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>{
  const canvas=document.querySelector('canvas'),gl=canvas.getContext('webgl2');
  const copy=document.createElement('canvas');copy.width=canvas.width;copy.height=canvas.height;
  const ctx=copy.getContext('2d');ctx.drawImage(canvas,0,0);const bytes=ctx.getImageData(0,0,copy.width,copy.height).data;
  let nonBlack=0,colourful=0;for(let i=0;i<bytes.length;i+=4){if(bytes[i]+bytes[i+1]+bytes[i+2]>30)nonBlack++;if(Math.max(bytes[i],bytes[i+1],bytes[i+2])-Math.min(bytes[i],bytes[i+1],bytes[i+2])>50)colourful++;}
  resolve({firstFrameMs:window.fbs.firstFrameMs,fps:window.fbs.fps,assets:window.fbs.assets,metrics:window.fbs.metrics(),nonBlack,colourful,
   audioPeak:audioProbe.peak,scriptProcessors:audioProbe.scriptProcessors,worklet:window.fbs.audio,storage:window.fbs.storage,audioStates:audioProbe.contexts.map(c=>c.state),webgl2:gl instanceof WebGL2RenderingContext,crossOriginIsolated,
   resources:performance.getEntriesByType('resource').map(r=>({name:r.name.split('/').pop(),encodedBodySize:r.encodedBodySize,decodedBodySize:r.decodedBodySize}))});
 })));
 await page.screenshot({path:path.join(evidence,`${prefix}chrome-web.png`)});
 assert.ok(result.webgl2&&result.nonBlack>10000&&result.colourful>1000);
 assert.ok(result.metrics.physics.top2y>=0&&result.metrics.physics.top3y>=0,'Native stacks stay above the floor');
 assert.equal(result.scriptProcessors,0);assert.equal(result.worklet.backend,"AudioWorklet");assert.ok(result.worklet.consumed>0);assert.equal(result.storage.active,0);
 assert.ok(result.audioPeak>.001&&result.audioStates.includes('running'));
 assert.equal(result.metrics.failed,0);
 if(result.metrics.audio.started!=null){assert.ok(result.metrics.audio.started>=1);assert.ok(result.metrics.audio.consumed>0);assert.ok(result.metrics.audio.peak>0);}
 assert.deepEqual(errors,[]);assert.ok(!logs.some(m=>/\b(?:WARNING|ERROR):/.test(m)),'Native engine warning or error');assert.ok(responses.every(r=>r.status===200));
 assert.ok(responses.filter(r=>r.url.endsWith('.wasm')).every(r=>r.type==='application/wasm'));
 const released=await page.evaluate(()=>window.fbs.shutdown());assert.equal(released,0);
 await page.reload();await page.waitForFunction(()=>window.fbs?.firstFrameMs!=null,null,{timeout:90000});
 const restored=await page.evaluate(()=>window.fbs.metrics());
 assert.equal(restored.services.previousSave,true,'IndexedDB save survives a page reload');assert.equal(restored.score,result.metrics.score);assert.equal(restored.muted,result.metrics.muted);assert.ok(Math.abs(restored.playerX-result.metrics.playerX)<.01,'Whole game position persists');
 assert.equal(await page.evaluate(()=>window.fbs.shutdown()),0);assert.deepEqual(errors,[]);

 fs.writeFileSync(path.join(evidence,`${prefix}browser.json`),JSON.stringify({chrome:browser.version(),mode:'headless SwiftShader; HTTP cache disabled; localhost; real gesture; AudioWorklet PCM; physics; streamed assets; reload persistence',before,...result,restored,released,errors,responses},null,2)+'\n');
 complete=true;
 console.log(JSON.stringify({firstFrameMs:result.firstFrameMs,fps:result.fps,audioPeak:result.audioPeak,metrics:result.metrics,released,errors}));
} finally {
 if(!complete&&page)await page.screenshot({path:path.join(evidence,`${prefix}failure.png`)}).catch(()=>{});
 fs.writeFileSync(path.join(evidence,`${prefix}browser-console.log`),logs.join('\n')+'\n');
 await browser.close();await new Promise(resolve=>server.close(resolve));
}
