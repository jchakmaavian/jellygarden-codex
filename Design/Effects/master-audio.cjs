const cp=require('child_process'),path=require('path');
// node master-audio.cjs <folder-containing-original-mp3s> [output-folder]
const ff=process.env.ROYAL_THRONE_FFMPEG||'ffmpeg';
const source=process.argv[2];
if(!source)throw new Error('Provide the folder containing the original generated MP3 files.');
const dest=process.argv[3]||path.resolve(__dirname,'../../Assets/JellyGarden/Resources/RoyalGameplay/Audio');
const edits={
 'match-a':'atrim=start=0.10:end=0.55,asetpts=PTS-STARTPTS,afade=t=out:st=0.27:d=0.18',
 'match-b':'atrim=start=0.10:end=0.55,asetpts=PTS-STARTPTS,afade=t=out:st=0.27:d=0.18',
 'tnt':'atrim=end=0.8,afade=t=out:st=0.55:d=0.25',
 'rocket':'atrim=start=0.06:end=1.3,asetpts=PTS-STARTPTS,atempo=1.7,afade=t=out:st=0.5:d=0.22',
 'helicopter':'atrim=end=1.6,atempo=1.8,volume=-5dB,afade=t=out:st=0.62:d=0.26',
 'create':'atrim=end=0.65,volume=9dB,afade=t=out:st=0.4:d=0.25'};
for(const [n,filter] of Object.entries(edits))cp.execFileSync(ff,['-hide_banner','-loglevel','error','-y','-i',path.join(source,n+'.mp3'),'-af',filter+',alimiter=limit=0.70:level=false','-ar','22050','-ac','1','-c:a','pcm_s16le',path.join(dest,n+'.wav')]);
cp.execFileSync(ff,['-hide_banner','-loglevel','error','-y','-i',path.join(source,'castle-theme.mp3'),'-af','loudnorm=I=-23:TP=-4:LRA=8','-ar','44100','-ac','2','-c:a','libmp3lame','-b:a','128k',path.join(dest,'castle-theme.mp3')]);
console.log('Timed and mastered six SFX plus the original score.');
