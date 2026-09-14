// Reproduce the tactile SFX master. Never rewrites castle-theme.mp3.
const cp=require('child_process'),fs=require('fs'),path=require('path');
const ff=process.env.ROYAL_THRONE_FFMPEG||'ffmpeg';
const source=process.argv[2];if(!source)throw Error('Provide the folder of seven generated SFX MP3s.');
const dest=path.resolve(__dirname,'../../Assets/JellyGarden/Resources/RoyalGameplay/Audio');
const clips={'match-a':[.46,120],'match-b':[.43,120],tnt:[1.18,48],rocket:[.86,95],helicopter:[.8,90],firework:[1.35,90],celebrate:[1.4,100]};
const report=[];
for(const [name,[length,cutoff]] of Object.entries(clips)){
 const raw=cp.execFileSync(ff,['-v','error','-i',path.join(source,name+'.mp3'),'-f','f32le','-ac','1','-ar','44100','-']);
 const samples=new Float32Array(raw.buffer,raw.byteOffset,raw.length/4);let peak=0;for(const x of samples)peak=Math.max(peak,Math.abs(x));
 let onset=0;while(onset<samples.length&&Math.abs(samples[onset])<Math.max(.002,peak*.04))onset++;
 const start=Math.max(0,onset/44100-.008),gain=Math.min(5,.74/Math.max(.01,peak)),tail=name.startsWith('match')?.10:.18;
 const filter=`atrim=start=${start}:duration=${length},asetpts=PTS-STARTPTS,highpass=f=${cutoff},lowpass=f=11500,volume=${gain},afade=t=in:d=0.002,afade=t=out:st=${length-tail}:d=${tail},alimiter=limit=0.78:level=false`;
 cp.execFileSync(ff,['-v','error','-y','-i',path.join(source,name+'.mp3'),'-af',filter,'-ar','32000','-ac','1','-c:a','pcm_s16le',path.join(dest,name+'.wav')]);
 report.push({name,start,length,sourcePeak:peak});
}
// Material contacts and the formation shimmer share the same real-world sound palette.
for(const [name,input,start,length,target] of [['land','match-b',0,.12,.35],['click','match-a',.025,.10,.40],['create','firework',.16,.55,.60]]) {
 const raw=cp.execFileSync(ff,['-v','error','-i',path.join(dest,input+'.wav'),'-af',`atrim=start=${start}:duration=${length}`,'-f','f32le','-ac','1','-']);
 const samples=new Float32Array(raw.buffer,raw.byteOffset,raw.length/4);let peak=0;for(const x of samples)peak=Math.max(peak,Math.abs(x));
 const gain=target/Math.max(.001,peak);
 cp.execFileSync(ff,['-v','error','-y','-i',path.join(dest,input+'.wav'),'-af',`atrim=start=${start}:duration=${length},asetpts=PTS-STARTPTS,volume=${gain},afade=t=in:d=0.002,afade=t=out:st=${Math.max(.02,length-.04)}:d=0.04,alimiter=limit=0.78:level=false`,'-c:a','pcm_s16le',path.join(dest,name+'.wav')]);
}
console.log(JSON.stringify(report,null,2));
