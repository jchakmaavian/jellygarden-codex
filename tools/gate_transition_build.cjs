// Bake the approved canvas layers into one small atlas. Start a dedicated
// local Chrome with remote debugging on 9232. No original painting is edited.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {once}=require('node:events');
const root=path.resolve(__dirname,'..');
const preview=path.join(root,'art-review/treasure-hunt/area-001/remaining-task-previews/index.html');
const output=path.join(root,'Assets/JellyGarden/Resources/Story/TreasureHunt/Transitions');
async function main(){
 const source=fs.readFileSync(preview,'utf8');
 const needle='window.areaPreview={ready:true,';
 if(!source.includes(needle))throw new Error('Approved preview export hook changed');
 const temporary='/private/tmp/king-leo-gate-layer-export.html';
 fs.writeFileSync(temporary,source.replace(needle,'window.areaPreview={layers:{doorFrame,doorWood,flagBase,flagLayers,flagPoles,images,hardware},ready:true,'));
 const pages=await(await fetch('http://127.0.0.1:9232/json/list')).json();
 const ws=new WebSocket(pages.find(p=>p.type==='page').webSocketDebuggerUrl);await once(ws,'open');
 let id=0;const pending=new Map();ws.addEventListener('message',e=>{const r=JSON.parse(e.data);if(r.id){const p=pending.get(r.id);pending.delete(r.id);r.error?p.reject(r.error):p.resolve(r.result);}});
 const call=(method,params={})=>new Promise((resolve,reject)=>{const key=++id;pending.set(key,{resolve,reject});ws.send(JSON.stringify({id:key,method,params}));});
 const ev=async expression=>{const r=await call('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true});if(r.exceptionDetails)throw new Error(JSON.stringify(r.exceptionDetails));return r.result.value;};
 try{
  await call('Page.enable');await call('Page.navigate',{url:'file://'+temporary+'?render'});
  for(let i=0;i<100;i++){if(await ev('!!window.areaPreview?.ready'))break;await new Promise(r=>setTimeout(r,100));}
  const baked=await ev(`(()=>{
   const L=areaPreview.layers,S=720/941,parts=[];
   const canvas=(w,h)=>{const c=document.createElement('canvas');c.width=w;c.height=h;return c;};
   function add(key,c,rect){
    if(!rect){const p=c.getContext('2d').getImageData(0,0,c.width,c.height).data;let left=c.width,top=c.height,right=-1,bottom=-1;for(let y=0;y<c.height;y++)for(let x=0;x<c.width;x++)if(p[(y*c.width+x)*4+3]>0){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);}rect=[left,top,right-left+1,bottom-top+1];}
    const [x,y,width,height]=rect;parts.push({key,x,y,width,height,canvas:c});
   }
   add('doorFrame',L.doorFrame);add('doorWood',L.doorWood);
   const final=canvas(720,1280);final.getContext('2d').drawImage(L.images[1],0,0,720,1280);
   L.hardware.forEach((h,i)=>add('hardware'+i,final,[Math.floor(h.x*S),Math.floor(h.y*S),Math.ceil(h.w*S)+1,Math.ceil(h.h*S)+1]));
   L.flagLayers.forEach((c,i)=>add('flag'+i,c));L.flagPoles.forEach((c,i)=>add('pole'+i,c));
   [39,727].forEach((x,i)=>add('stone'+i,L.flagBase,[Math.floor(x*S),Math.floor(548*S),Math.ceil(181*S)+1,Math.ceil(59*S)+1]));
   const white=canvas(4,4);white.getContext('2d').fillStyle='#fff';white.getContext('2d').fillRect(0,0,4,4);add('white',white,[0,0,4,4]);
   parts.sort((a,b)=>b.height-a.height||a.key.localeCompare(b.key));let x=2,y=2,row=0;
   for(const p of parts){if(x+p.width+2>1024){x=2;y+=row+4;row=0;}p.atlasX=x;p.atlasY=y;x+=p.width+4;row=Math.max(row,p.height);}
   const height=2**Math.ceil(Math.log2(y+row+2)),atlas=canvas(1024,height),g=atlas.getContext('2d');
   for(const p of parts){g.drawImage(p.canvas,p.x,p.y,p.width,p.height,p.atlasX,p.atlasY,p.width,p.height);delete p.canvas;}
   return {png:atlas.toDataURL('image/png').split(',')[1],manifest:{width:1024,height,parts}};
  })()`);
  const stem=path.join(output,'area_001_parts');fs.mkdirSync(output,{recursive:true});
  const inputs=[1,2,3,4].map(s=>path.join(root,`Assets/JellyGarden/Resources/Story/TreasureHunt/Art/area_001_state${s}.png`));
  baked.manifest.sourceHashes=inputs.map(p=>({file:path.basename(p),sha256:crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')}));
  fs.writeFileSync(stem+'.png',Buffer.from(baked.png,'base64'));
  fs.writeFileSync(stem+'_layout.json',JSON.stringify(baked.manifest,null,2)+'\n');
  if(!fs.existsSync(stem+'.png.meta')){
   let meta=fs.readFileSync(inputs[0]+'.meta','utf8');
   const guid=crypto.createHash('md5').update('Story/TreasureHunt/Transitions/area_001_parts').digest('hex');
   meta=meta.replace(/^guid:.*$/m,'guid: '+guid).replace(/textureType: 8/g,'textureType: 0').replace(/spriteMode: 1/g,'spriteMode: 0').replace(/maxTextureSize: 2048/g,'maxTextureSize: 1024').replace(/textureCompression: 1/g,'textureCompression: 0');
   fs.writeFileSync(stem+'.png.meta',meta);
  }
  console.log(`Baked ${baked.manifest.parts.length} parts into ${baked.manifest.width} × ${baked.manifest.height} atlas.`);
 }finally{ws.close();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
