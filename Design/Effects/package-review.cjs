const fs=require('fs'),path=require('path'),cp=require('child_process');
const root=path.resolve(__dirname,'../..');
const capture=path.join(root,'Library/RoyalEffectsCapture');
const output=path.join(__dirname,process.argv[2]||'Unity-review');
fs.mkdirSync(output,{recursive:true});
const ff=process.env.ROYAL_THRONE_FFMPEG||'ffmpeg';
const frames=fs.readFileSync(path.join(capture,'frames.csv'),'utf8').trim().split(/\r?\n/).map(line=>{const [name,time]=line.split(',');return {name,time:Number(time)};});
if(frames.length<2)throw new Error('Run the Unity regression capture first.');
const concat=[];
for(let i=0;i<frames.length;i++){
 concat.push("file '"+frames[i].name+"'");
 if(i<frames.length-1)concat.push('duration '+Math.max(.001,frames[i+1].time-frames[i].time).toFixed(6));
}
fs.writeFileSync(path.join(capture,'timing.txt'),concat.join('\n'));
const encoders=cp.execFileSync(ff,['-hide_banner','-encoders'],{encoding:'utf8',stdio:['ignore','pipe','ignore']});
const video=encoders.includes('libx264')?['-c:v','libx264','-preset','veryfast','-crf','24']:['-c:v','h264_mf','-b:v','3000k'];
// H.264 4:2:0 requires even dimensions; preserve the capture and pad only its edge.
cp.execFileSync(ff,['-hide_banner','-loglevel','error','-y','-f','concat','-safe','0','-i','timing.txt','-i',path.join(capture,'unity-audio.wav'),'-vf','pad=ceil(iw/2)*2:ceil(ih/2)*2','-r','30','-vsync','cfr',...video,'-pix_fmt','yuv420p','-c:a','aac','-b:a','128k','-shortest','-movflags','+faststart',path.join(output,'royal-throne-unity-effects.mp4')],{cwd:capture});
console.log(JSON.stringify({frames:frames.length,seconds:frames.at(-1).time,bytes:fs.statSync(path.join(output,'royal-throne-unity-effects.mp4')).size}));
