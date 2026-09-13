// Deterministic local exports; dedicated Chrome preview instance on port 9231.
const fs=require('node:fs'),path=require('node:path');
const {once}=require('node:events'),{spawn}=require('node:child_process');
async function main(){
 const pages=await(await fetch('http://127.0.0.1:9231/json/list')).json();
 const ws=new WebSocket(pages.find(p=>p.type==='page').webSocketDebuggerUrl);await once(ws,'open');
 let id=0;const pending=new Map(),errors=[];
 ws.addEventListener('message',e=>{const r=JSON.parse(e.data);if(r.id){const p=pending.get(r.id);pending.delete(r.id);r.error?p.reject(r.error):p.resolve(r.result);}if(r.method==='Runtime.exceptionThrown')errors.push(r.params.exceptionDetails);});
 const call=(method,params={})=>new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});ws.send(JSON.stringify({id:key,method,params}));});
 const ev=async expression=>{const r=await call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true});if(r.exceptionDetails)throw new Error(JSON.stringify(r.exceptionDetails));return r.result.value;};
 try{
  await call('Runtime.enable');await call('Page.enable');await call('Emulation.setDeviceMetricsOverride',{width:720,height:1280,deviceScaleFactor:1,mobile:false});
  await call('Page.navigate',{url:`file://${path.join(__dirname,'index.html')}?render`});
  for(let i=0;i<100;i++){if(await ev('!!window.areaPreview?.ready'))break;await new Promise(r=>setTimeout(r,100));}
  if(!await ev('!!window.areaPreview?.ready')||errors.length)throw new Error(JSON.stringify(errors));
  const mode=process.argv[2]||'stills',keys=process.argv[3]?[process.argv[3]]:['door','flags','lanterns'];
  const frame=async(key,t)=>Buffer.from(await ev(`areaPreview.setTime('${key}',${t});areaPreview.previews['${key}'].canvas.toDataURL('image/png').split(',')[1]`),'base64');
  const validation={scope:'Area 1 task 2–4 standalone previews only',width:720,height:1280,fps:30,duration:6.6,mode,browserErrors:errors,tasks:{}};
  for(const key of keys){
   console.log(key);
   if(mode==='stills'){
    for(const [name,t] of [['before',0],['working',1.85],['settling',2.85],['after',5.7]])fs.writeFileSync(path.join(__dirname,`${key}-${name}.png`),await frame(key,t));
   }else if(mode==='video'){
    const dest=path.join(__dirname,`area-1-${key}-preview.mp4`);
    const ff=spawn('ffmpeg',['-hide_banner','-loglevel','error','-y','-f','image2pipe','-vcodec','png','-framerate','30','-i','pipe:0','-an','-c:v','libx264','-preset','medium','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',dest],{stdio:['pipe','inherit','inherit']});
    let failure;ff.on('error',e=>failure=e);ff.stdin.on('error',e=>failure=e);const completion=once(ff,'exit');
    for(let i=0;i<198;i++){if(failure)throw failure;if(!ff.stdin.write(await frame(key,i/30)))await once(ff.stdin,'drain');if(i%60===0)console.log(`${key}: ${i/30}/6.6s`);}
    ff.stdin.end();const [code]=await completion;if(code!==0)throw new Error(`ffmpeg: ${code}`);
   }
   const before=await frame(key,0),after=await frame(key,6.6),beforeAgain=await frame(key,0),afterAgain=await frame(key,6.6);
   validation.tasks[key]={replayStartMatches:before.equals(beforeAgain),replayEndMatches:after.equals(afterAgain)};
   if(Object.values(validation.tasks[key]).some(v=>!v))throw new Error('Non-deterministic replay');
  }
  if(errors.length)throw new Error(JSON.stringify(errors));
  fs.writeFileSync(path.join(__dirname,`validation-${mode}.json`),JSON.stringify(validation,null,2)+'\n');
 }finally{ws.close();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
