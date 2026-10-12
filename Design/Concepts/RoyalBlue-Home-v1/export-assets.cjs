const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {chromium}=require('playwright');
const sharp=require('sharp');
const root=__dirname;
const fragment=fs.readFileSync(process.argv[2],'utf8');
const styles=fragment.match(/<style>([\s\S]*?)<\/style>/)[1];
const manifest=JSON.parse(fs.readFileSync(path.join(root,'asset-manifest.json'),'utf8'));
const surfacePath=path.join(root,'assets','surfaces');fs.mkdirSync(surfacePath,{recursive:true});
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 try{
  const page=await browser.newPage({viewport:{width:1100,height:1900},deviceScaleFactor:2});
  await page.setContent(fragment);
  await page.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
  const surfaceRecords=[];
  const specs=[
   {name:'hud-ivory-plate',selector:'.rt-resource-face',w:180,h:46},
   {name:'profile-gold-frame',selector:'.rt-profile',w:72,h:78},
   {name:'area-gold-panel',selector:'.rt-area',w:198,h:90},
   {name:'bottom-nav-blue-gold',selector:'.rt-nav',w:432,h:104},
   {name:'progress-track',selector:'.rt-progress',w:180,h:26},
   {name:'progress-blue-fill',selector:'.rt-progress-fill',w:180,h:26},
   {name:'dialog-ivory-panel',selector:'.rt-dialog',w:360,h:320},
   {name:'dialog-blue-button',selector:'.rt-dialog-cta',w:260,h:56},
   {name:'close-button',selector:'.rt-close',w:44,h:44,text:'×'},
   {name:'add-coins-button',selector:'.rt-plus',w:24,h:24,text:'+'}
  ];
  for(const spec of specs){
   const markup=await page.locator(spec.selector).first().evaluate((el,spec)=>{const node=el.cloneNode(false);node.removeAttribute('id');node.removeAttribute('hidden');node.textContent=spec.text||'';node.style.cssText=`position:relative;inset:auto;margin:0;width:${spec.w}px;height:${spec.h}px;min-height:0;min-width:0;max-width:none;aspect-ratio:auto;flex:none;transform:none;`;return node.outerHTML;},spec);
   const surface=await browser.newPage({viewport:{width:500,height:400},deviceScaleFactor:2.5});
   // Render native UI surfaces from their source CSS; generated artwork is not edited.
   await surface.setContent('<style>'+styles+'</style><style>body{margin:0;background:transparent}#rt-home-study{width:432px;container-type:inline-size}.export{display:inline-block;padding:12px;background:transparent}#rt-home-study .export .rt-progress-fill{display:block}</style><div id="rt-home-study"><div class="export">'+markup+'</div></div>');
   const target=path.join(surfacePath,spec.name+'.png');
   await surface.locator('.export').screenshot({path:target,omitBackground:true});
   const png=fs.readFileSync(target),meta=await sharp(png).metadata();
   surfaceRecords.push({name:spec.name,file:'assets/surfaces/'+spec.name+'.png',width:meta.width,height:meta.height,hasAlpha:!!meta.hasAlpha,sha256:crypto.createHash('sha256').update(png).digest('hex'),source:'Native UI CSS in home-preview.template.html',logicalSize:{width:spec.w,height:spec.h},padding:12,scale:2.5});
   await surface.close();
  }
  fs.writeFileSync(path.join(root,'surface-manifest.json'),JSON.stringify(surfaceRecords,null,2)+'\n');
  const board=await browser.newPage({viewport:{width:1100,height:900},deviceScaleFactor:1});
  await board.setContent(fs.readFileSync(path.join(root,'asset-board.template.html'),'utf8'));
  const records=[...manifest.assets.map(a=>({...a,src:'data:image/webp;base64,'+fs.readFileSync(path.join(root,a.preview.file)).toString('base64'),surface:false})),...surfaceRecords.map(a=>({...a,src:'data:image/png;base64,'+fs.readFileSync(path.join(root,a.file)).toString('base64'),surface:true}))];
  await board.evaluate(records=>{const grid=document.getElementById('asset-grid');let heading=false;for(const r of records){if(r.surface&&!heading){const h=document.createElement('h2');h.textContent='Reusable interface surfaces';grid.append(h);heading=true;}const f=document.createElement('figure');if(r.name==='home-castle')f.className='environment';else if(r.surface)f.className='surface';const img=document.createElement('img');img.src=r.src;img.alt=r.name;const label=document.createElement('figcaption');label.textContent=r.name.replaceAll('-',' ');f.append(img,label);grid.append(f);}},records);
  await board.locator('img').evaluateAll(images=>Promise.all(images.map(i=>i.decode())));
  await board.screenshot({path:path.join(root,'review','asset-contact-sheet.png'),fullPage:true});
  fs.writeFileSync(path.join(root,'review','asset-contact-sheet.html'),await board.content());
  console.log(JSON.stringify({artwork:manifest.assets.length,surfaces:surfaceRecords.length,totalPngAssets:manifest.assets.length+surfaceRecords.length}));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
