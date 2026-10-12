const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('sharp');
const output=path.join(__dirname,'assets');fs.mkdirSync(output,{recursive:true});
const generated='C:/Users/chakm/.codex/generated_images/01a07f2d-f2b5-7123-a1e6-e3dba88553a7';
const home=path.resolve(__dirname,'../RoyalBlue-Home-v1/assets');
const names=['ruby','shield','crown','leaf','lily','rocket','tnt','helicopter','orb','crate','vase','hammer'];
(async()=>{
 const source=path.join(generated,'exec-9addacfe-3b98-46a8-9521-c8254d8fed81.png');const m=await sharp(source).metadata();console.log({atlas:m.width,height:m.height,alpha:m.hasAlpha});
 const manifest=[];
 for(let n=0;n<names.length;n++){
  // The generated sheet has unequal visual gutters. Keep neighboring row silhouettes out of each cell.
  const rows=[0,356,709,m.height];
  const left=Math.round(n%4*m.width/4),top=rows[Math.floor(n/4)],right=Math.round((n%4+1)*m.width/4),bottom=rows[Math.floor(n/4)+1];
  const cut=await sharp(source).extract({left,top,width:right-left,height:bottom-top}).toBuffer();
  const buf=await sharp(cut).trim({threshold:8}).toBuffer();
  await sharp(buf).resize(160,160,{fit:'contain',background:'#00000000'}).webp({quality:84,alphaQuality:90,effort:6}).toFile(path.join(output,names[n]+'.webp'));
 }
 const chars=[['king-idle',path.join(home,'avatar-king.png')],['king-cheer',path.join(generated,'exec-2f7b1d87-7f93-4674-9eec-278440928cd0.png')],['king-think',path.join(generated,'exec-0e45a2bf-28bd-4265-a382-c5c0bda99eff.png')]];
 for(const [name,src]of chars)await sharp(src).resize(320,340,{fit:'contain',background:'#00000000'}).webp({quality:84,alphaQuality:90,effort:6}).toFile(path.join(output,name+'.webp'));
 for(const name of ['home-castle','logo-locked','icon-star','icon-settings'])fs.copyFileSync(path.join(home,'preview',name+'.webp'),path.join(output,name+'.webp'));
 for(const name of fs.readdirSync(output)){
  const buf=fs.readFileSync(path.join(output,name)),meta=await sharp(buf).metadata();manifest.push({file:name,bytes:buf.length,width:meta.width,height:meta.height,alpha:meta.hasAlpha,rgbaBytes:meta.width*meta.height*4,sha256:crypto.createHash('sha256').update(buf).digest('hex')});
 }
 fs.writeFileSync(path.join(__dirname,'asset-manifest.json'),JSON.stringify({format:'WebP',totalBytes:manifest.reduce((a,x)=>a+x.bytes,0),decodedRGBABytes:manifest.reduce((a,x)=>a+x.rgbaBytes,0),assets:manifest},null,2));console.log({totalBytes:manifest.reduce((a,x)=>a+x.bytes,0),count:manifest.length});
})().catch(e=>{console.error(e);process.exit(1)});
