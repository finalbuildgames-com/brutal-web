// Bounded PCM queue, works with and without SharedArrayBuffer. No wasm heap view
// is retained here; the host transfers complete stereo Float32Array chunks.
class BrutalAudio extends AudioWorkletProcessor {
 constructor(){super();this.queue=[];this.offset=0;this.consumed=0;this.underruns=0;this.started=false;this.pending=3;
  this.port.onmessage=event=>{const pcm=event.data.pcm;if(!(pcm instanceof Float32Array)||pcm.length!==4096||this.queue.length>=8)return;this.pending--;this.queue.push(pcm);};
  this.port.postMessage({type:'need',count:3});
 }
 process(_inputs,outputs){const out=outputs[0];if(!out?.length)return true;
  let missing=0;
  for(let i=0;i<out[0].length;i++){
   const pcm=this.queue[0];if(pcm){this.started=true;out[0][i]=pcm[this.offset++];out[1][i]=pcm[this.offset++];this.consumed++;
    if(this.offset===pcm.length){this.queue.shift();this.offset=0;this.pending++;this.port.postMessage({type:'need',count:1});this.port.postMessage({type:'stats',consumed:this.consumed,underruns:this.underruns});}
   }else if(this.started)missing++;
  }
  this.underruns+=missing;return true;
 }
}
registerProcessor('brutal-audio',BrutalAudio);
