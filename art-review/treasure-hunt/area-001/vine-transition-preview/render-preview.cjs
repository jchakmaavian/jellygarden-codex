// Export the standalone canvas with a dedicated Chrome debug instance on 9230.
const fs=require('node:fs');
const path=require('node:path');
const {once}=require('node:events');
const {spawn}=require('node:child_process');
async function main(){
  const pages=await (await fetch('http://127.0.0.1:9230/json/list')).json();
  const page=pages.find(p=>p.type==='page');
  if(!page)throw new Error('No preview browser page');
  const ws=new WebSocket(page.webSocketDebuggerUrl);await once(ws,'open');
  let id=0;const pending=new Map(),errors=[];
  ws.addEventListener('message',e=>{const r=JSON.parse(e.data);if(r.id){const p=pending.get(r.id);pending.delete(r.id);r.error?p.reject(r.error):p.resolve(r.result);}if(r.method==='Runtime.exceptionThrown')errors.push(r.params.exceptionDetails);});
  const call=(method,params={})=>new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});ws.send(JSON.stringify({id:key,method,params}));});
  const evaluate=async expression=>{const r=await call('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true});if(r.exceptionDetails)throw new Error(JSON.stringify(r.exceptionDetails));return r.result.value;};
  try{
    await call('Runtime.enable');await call('Page.enable');
    await call('Emulation.setDeviceMetricsOverride',{width:720,height:1280,deviceScaleFactor:1,mobile:false});
    await call('Page.navigate',{url:`file://${path.join(__dirname,'index.html')}?render`});
    for(let i=0;i<50;i++){if(await evaluate('!!window.vinePreview?.ready'))break;await new Promise(r=>setTimeout(r,100));}
    const info=await evaluate('({ready:vinePreview.ready,width:vinePreview.width,height:vinePreview.height,duration:vinePreview.duration,particles:vinePreview.particles})');
    if(!info.ready||errors.length)throw new Error(JSON.stringify({info,errors}));
    console.log(JSON.stringify(info));
    const frame=async t=>Buffer.from(await evaluate(`vinePreview.setTime(${t});document.getElementById('scene').toDataURL('image/png').split(',')[1]`),'base64');
    const mode=process.argv[2]||'stills';
    if(mode==='stills'){
      for(const [name,t] of [['before',0],['clearing',2.2],['upper-vines',3.05],['after',5.6]])fs.writeFileSync(path.join(__dirname,`${name}.png`),await frame(t));
    }else if(mode==='video'){
      const fps=30,total=Math.round(info.duration*fps);
      const dest=path.join(__dirname,'area-1-clear-vines-preview.mp4');
      const ff=spawn('ffmpeg',['-hide_banner','-loglevel','error','-y','-f','image2pipe','-vcodec','png','-framerate',String(fps),'-i','pipe:0','-an','-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',dest],{stdio:['pipe','inherit','inherit']});
      let failure;ff.on('error',e=>failure=e);ff.stdin.on('error',e=>failure=e);const completion=once(ff,'exit');
      for(let i=0;i<total;i++){if(failure)throw failure;if(!ff.stdin.write(await frame(i/fps)))await once(ff.stdin,'drain');if(i%30===0)console.log(`Rendered ${i/fps}/${info.duration}s`);}
      ff.stdin.end();const [code]=await completion;if(code!==0)throw new Error(`ffmpeg exit ${code}`);console.log(dest);
    }
    // Check replay and timeline seek against the exact endpoint renders.
    const first=await frame(0),last=await frame(info.duration);
    const firstAgain=await frame(0),lastAgain=await frame(info.duration);
    const checks={firstReplayMatches:first.equals(firstAgain),lastReplayMatches:last.equals(lastAgain)};
    if(errors.length||Object.values(checks).some(v=>!v))throw new Error(JSON.stringify({errors,checks}));
    fs.writeFileSync(path.join(__dirname,'preview-validation.json'),JSON.stringify({scope:'Area 1 Clear the vines; standalone preview only',...info,mode,browserErrors:errors,...checks},null,2)+'\n');
  }finally{ws.close();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
