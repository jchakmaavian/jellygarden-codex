// SFX only. This command never opens or rewrites castle-theme.mp3.
const cp=require('child_process'),fs=require('fs'),path=require('path');
const ff=process.env.ROYAL_THRONE_FFMPEG||'ffmpeg';
const source=process.argv[2];if(!source)throw Error('Provide the folder of five generated SFX MP3s.');
const dest=path.resolve(__dirname,'../../Assets/JellyGarden/Resources/RoyalGameplay/Audio');
const clips={'match-a':.32,'match-b':.34,tnt:.60,rocket:.65,helicopter:.74};
for(const [name,length] of Object.entries(clips)){
 const raw=cp.execFileSync(ff,['-v','error','-i',path.join(source,name+'.mp3'),'-f','f32le','-ac','1','-ar','44100','-']);
 const samples=new Float32Array(raw.buffer,raw.byteOffset,raw.length/4);let peak=0;for(const x of samples)peak=Math.max(peak,Math.abs(x));
 let start=0;while(start<samples.length&&Math.abs(samples[start])<Math.max(.007,peak*.065))start++;start=Math.max(0,start/44100-.005);
 const gain=Math.min(5,.72/Math.max(.01,peak));
 const filter=`atrim=start=${start}:duration=${length},asetpts=PTS-STARTPTS,highpass=f=${name==='tnt'?65:140},equalizer=f=2600:t=q:w=0.8:g=2,volume=${gain},afade=t=in:d=0.003,afade=t=out:st=${length-.11}:d=0.11,alimiter=limit=0.80:level=false`;
 cp.execFileSync(ff,['-v','error','-y','-i',path.join(source,name+'.mp3'),'-af',filter,'-ar','32000','-ac','1','-c:a','pcm_s16le',path.join(dest,name+'.wav')]);
 console.log(name,'onset',start.toFixed(3),'seconds',length,'source peak',peak.toFixed(3));
}
// Quiet short material contact for settling and UI; no sustained ringing in a cascade.
for(const [name,duration,gain,pitch] of [['land',.09,.55,.82],['click',.065,.72,1.2]])
 cp.execFileSync(ff,['-v','error','-y','-i',path.join(dest,'match-b.wav'),'-af',`atrim=duration=${duration},asetrate=${32000*pitch},aresample=32000,volume=${gain},afade=t=out:st=0.035:d=0.04`,'-c:a','pcm_s16le',path.join(dest,name+'.wav')]);
