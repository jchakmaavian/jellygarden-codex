const fs=require('node:fs');
const path=require('node:path');
const crypto=require('node:crypto');
const sharp=require('sharp');
const concept=__dirname;
const project=path.resolve(concept,'../../..');
const destination=process.argv[2];
if(!destination)throw Error('Pass the absolute destination for the inline fragment.');
const source=JSON.parse(fs.readFileSync(path.join(concept,'generation-record.json'),'utf8'));
const sha=buffer=>crypto.createHash('sha256').update(buffer).digest('hex');
(async()=>{
  fs.mkdirSync(path.join(concept,'assets','preview'),{recursive:true});
  const records=[...source.records,{key:'logo-locked',source:path.join(project,'Design','Branding','royal-throne-splash-logo-v1.png'),prompt:'Existing locked logo reused unchanged.'}];
  let html=fs.readFileSync(path.join(concept,'home-preview.template.html'),'utf8');
  const manifest=[];
  for(const record of records){
    if(!record.source)throw Error('Missing selected source for '+record.key);
    const localMaster=path.join(concept,'assets',record.key+'.png');
    const png=fs.readFileSync(fs.existsSync(localMaster)?localMaster:record.source);
    const metadata=await sharp(png).metadata();
    const stats=await sharp(png).stats();
    const transparent=metadata.hasAlpha&&stats.channels[3].min===0;
    if(record.key!=='home-castle'&&!transparent)throw Error('Asset needs real transparent background: '+record.key);
    fs.writeFileSync(path.join(concept,'assets',record.key+'.png'),png);
    const width=record.key==='home-castle'?800:record.key==='button-crowned-blank'?800:record.key==='logo-locked'?360:240;
    // Delivery resizing and transparent-margin trimming only. PNG masters remain byte-identical.
    let pipeline=sharp(png);
    if(record.key==='button-crowned-blank')pipeline=pipeline.trim({threshold:2});
    const {data,info}=await pipeline.resize({width,withoutEnlargement:true}).webp({quality:88,alphaQuality:100}).toBuffer({resolveWithObject:true});
    fs.writeFileSync(path.join(concept,'assets','preview',record.key+'.webp'),data);
    html=html.replaceAll('__ASSET_'+record.key+'__','data:image/webp;base64,'+data.toString('base64'));
    manifest.push({name:record.key,file:'assets/'+record.key+'.png',width:metadata.width,height:metadata.height,hasTransparency:!!transparent,sha256:sha(png),source:record.source,preview:{file:'assets/preview/'+record.key+'.webp',width:info.width,height:info.height,bytes:data.length},role:record.key==='home-castle'?'Opaque environment plate; fountain and castle are baked into this concept image.':'Separate sprite'});
  }
  const localFont=path.join(concept,'assets','BorisBlackBloxx.ttf');
  const font=fs.readFileSync(fs.existsSync(localFont)?localFont:path.join(project,'Assets','JellyGarden','Fonts','BorisBlackBloxx.ttf'));
  fs.writeFileSync(path.join(concept,'assets','BorisBlackBloxx.ttf'),font);
  html=html.replace('__FONT__','data:font/ttf;base64,'+font.toString('base64'));
  if(/__ASSET_|__FONT__/.test(html))throw Error('Unresolved preview asset');
  if(Buffer.byteLength(html)>=1000000)throw Error('Fragment exceeds 1 MB');
  fs.writeFileSync(destination,html);
  fs.writeFileSync(path.join(concept,'asset-manifest.json'),JSON.stringify({status:'Concept only, not imported into Unity',generatedWith:'Built-in image_gen',assets:manifest,font:{file:'assets/BorisBlackBloxx.ttf',sha256:sha(font),provenance:'Existing project font; original license remains applicable'},fragmentBytes:Buffer.byteLength(html)},null,2)+'\n');
  console.log(JSON.stringify({assets:manifest.length,fragmentBytes:Buffer.byteLength(html),destination},null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});
