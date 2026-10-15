export function createAudio(runtime){
 let context,node,starting,closed=false,lastAck=0;
 const stats={backend:'AudioWorklet',ready:false,consumed:0,underruns:0,blocks:0};
 const unlock=()=>starting??=(async()=>{
  // Context creation and resume begin synchronously in the real input handler.
  context=new AudioContext({sampleRate:48000,latencyHint:'interactive'});const resumed=context.resume();
  await context.audioWorklet.addModule(new URL('./audio-worklet.js',import.meta.url));await resumed;
  if(closed){await context.close();return;}
  node=new AudioWorkletNode(context,'brutal-audio',{numberOfInputs:0,numberOfOutputs:1,outputChannelCount:[2]});
  node.port.onmessage=event=>{
   if(closed)return;
   const message=event.data;
   if(message.type==='need')for(let i=0;i<message.count;i++){
    const pointer=runtime.Module._br_audio_pull(2048);
    const pcm=runtime.Module.HEAPF32.slice(pointer/4,pointer/4+4096);
    node.port.postMessage({pcm},[pcm.buffer]);stats.blocks++;
   }
   if(message.type==='stats'){stats.consumed=message.consumed;stats.underruns=message.underruns;runtime.Module._br_audio_ack(stats.consumed-lastAck);lastAck=stats.consumed;}
  };
  runtime.Module._br_unlock_audio();stats.ready=true;node.connect(context.destination);
 })();
 return {stats,unlock,async close(){closed=true;if(node){node.port.onmessage=null;node.disconnect();node.port.close();}if(context&&context.state!=='closed')await context.close();stats.ready=false;}};
}
