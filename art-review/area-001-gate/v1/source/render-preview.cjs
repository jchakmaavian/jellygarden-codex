// Offline, deterministic preview export. Start Chrome with remote debugging on 9228 first.
const fs = require('node:fs');
const path = require('node:path');
const { once } = require('node:events');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '..');
const mode = process.argv[2] || 'stills';
async function main() {
  const pages = await (await fetch('http://127.0.0.1:9228/json/list')).json();
  const page = pages.find(p=>p.type==='page');
  const ws = new WebSocket(page.webSocketDebuggerUrl);
  await once(ws,'open');
  let id=0;const pending=new Map();const errors=[];
  ws.addEventListener('message',e=>{
    const r=JSON.parse(e.data);
    if(r.id){const p=pending.get(r.id);pending.delete(r.id);r.error?p.reject(r.error):p.resolve(r.result);}
    if(r.method==='Runtime.exceptionThrown')errors.push(r.params.exceptionDetails);
  });
  const call=(method,params={})=>new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});ws.send(JSON.stringify({id:key,method,params}));});
  const evaluate=async expression=>{
    const r=await call('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true});
    if(r.exceptionDetails)throw new Error(JSON.stringify(r.exceptionDetails));return r.result.value;
  };
  await call('Runtime.enable');await call('Page.enable');
  await call('Emulation.setDeviceMetricsOverride',{width:720,height:1280,deviceScaleFactor:1,mobile:false});
  await call('Page.navigate',{url:`file://${__dirname}/scene.html?render`});
  await new Promise(r=>setTimeout(r,2500));
  const ready=await evaluate('!!window.gatePreview?.ready');
  if(!ready||errors.length)throw new Error(JSON.stringify({ready,errors}));
  console.log(JSON.stringify(await evaluate('({renderer:gatePreview.renderer.info.render,geometries:gatePreview.renderer.info.memory.geometries})')));
  const renderFrame=async t=>{
    const data=await evaluate(`gatePreview.setTime(${t});document.querySelector('canvas').toDataURL('image/png').split(',')[1]`);
    return Buffer.from(data,'base64');
  };
  if(mode==='stills') {
    for(const [name,t] of [['00-broken',0],['01-timber-moving',3],['02-timber-fixed',6],['03-banner-moving',8.7],['04-banner-hung',12],['05-lanterns-moving',14],['06-complete',18]]) {
      fs.writeFileSync(path.join(root,'animations',`${name}-motion-preview.png`),await renderFrame(t));console.log(name);
    }
  } else if(mode==='video') {
    const fps=24,duration=18,frames=fps*duration;
    const dest=path.join(root,'animations','00-full-restoration.mp4');
    const ff=spawn('ffmpeg',['-hide_banner','-loglevel','error','-y','-f','image2pipe','-vcodec','png','-framerate',String(fps),'-i','pipe:0','-an','-c:v','libx264','-preset','medium','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',dest],{stdio:['pipe','inherit','inherit']});
    let ffError;ff.on('error',e=>ffError=e);ff.stdin.on('error',e=>ffError=e);
    const completion=once(ff,'exit');
    for(let i=0;i<frames;i++){
      if(ffError)throw ffError;
      if(!ff.stdin.write(await renderFrame(i/fps)))await once(ff.stdin,'drain');
      if(i%24===0)console.log(`Rendered ${i/24}/${duration}s`);
    }
    ff.stdin.end();const [code]=await completion;if(code!==0)throw new Error(`ffmpeg exit ${code}`);
    console.log(`Saved ${dest}`);
  }
  fs.writeFileSync(path.join(root,'source','render-validation.json'),JSON.stringify({status:'passed',mode,resolution:[720,1280],fps:24,duration:18,browserErrors:errors},null,2)+'\n');
  ws.close();
}
main().catch(e=>{console.error(e);process.exit(1);});
