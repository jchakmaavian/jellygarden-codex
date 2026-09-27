// Asset packaging only: generation is from the built-in image tool; no gameplay edits.
const sharp=require('C:/Users/chakm/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../..');
const folder=__dirname;
const spriteDir='Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Sprites';
const variants=['coin.10__21779','coin.11__22228','coin__16660','coin.02__21355','coin.03__22083','coin.04__17107','coin.05__17096','coin.10_g__16629','coin.11_g__17115'];
const aliases=['Assets/ReferenceGameplay/Home/Resources/ReferenceGameplay/Home/22081.png','Assets/JellyGarden/Textures/RoyalHome/icon-coin.png','Assets/JellyGarden/Resources/RoyalCampaignArt/coin.png','Assets/JellyGarden/Resources/RoyalGameplay/coin.webp','Assets/JellyGarden/Resources/RoyalGameplayV4/06_optional_extras/reward_coin.png','Assets/GameRedesign/Resources/Coin_Gold.png','Assets/GameRedesign/Reward/Coin_Gold.png'];
async function bbox(file){const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});let x0=info.width,y0=info.height,x1=0,y1=0;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>32){x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);}return {width:info.width,height:info.height,x:x0,y:y0,w:x1-x0+1,h:y1-y0+1};}
(async()=>{
  // Remove the generated neutral preview matte using the saturated gold silhouette.
  // A convex scanline fill retains white specular highlights inside the coin.
  const crops=[{left:0,top:50,width:395,height:440},{left:400,top:50,width:370,height:440},{left:780,top:50,width:360,height:440},{left:1170,top:50,width:340,height:440},{left:60,top:510,width:290,height:440},{left:480,top:510,width:190,height:440},{left:870,top:510,width:145,height:440}];
  for(let k=0;k<crops.length;k++){
    const {data,info}=await sharp(path.join(folder,'Source/Generated-views.png')).extract(crops[k]).ensureAlpha().raw().toBuffer({resolveWithObject:true});
    for(let y=0;y<info.height;y++){
      let lo=info.width,hi=-1;for(let x=0;x<info.width;x++){const i=(y*info.width+x)*4;if(data[i]-data[i+2]>42&&data[i+1]-data[i+2]>18){lo=Math.min(lo,x);hi=Math.max(hi,x);}}
      for(let x=0;x<info.width;x++){const i=(y*info.width+x)*4;data[i+3]=(x>=lo&&x<=hi)?255:0;if(!data[i+3]){data[i]=255;data[i+1]=192;data[i+2]=12;}}
    }
    const buf=await sharp(data,{raw:info}).png().toBuffer();const b=await bbox(buf);
    await sharp(buf).extract({left:b.x,top:b.y,width:b.w,height:b.h}).png().toFile(path.join(folder,`Frames/pose-${k}.png`));
  }
  const files=variants.map((v,i)=>({file:`${spriteDir}/Items-CoinItem-${v}.png`,pose:i<7?i:(i===7?0:1)})).concat(aliases.map(file=>({file,pose:2})));
  const preview=[];
  for(const [i,item] of files.entries()){
    const full=path.join(root,item.file);if(!fs.existsSync(full))throw Error('Missing '+full);
    const b=await bbox(full);item.layout=b;
    const scaled=await sharp(path.join(folder,`Frames/pose-${item.pose}.png`)).resize(b.w,b.h,{fit:'fill',kernel:'lanczos3'}).toBuffer();
    const dst=path.join(folder,'Prepared',item.file);fs.mkdirSync(path.dirname(dst),{recursive:true});
    let img=sharp({create:{width:b.width,height:b.height,channels:4,background:{r:255,g:185,b:14,alpha:0}}}).composite([{input:scaled,left:b.x,top:b.y}]);
    if(dst.endsWith('.webp'))await img.webp({lossless:true}).toFile(dst);else await img.png().toFile(dst);
    const thumb=await sharp(dst).resize(140,140,{fit:'contain',background:'#203d70'}).png().toBuffer();
    preview.push({input:thumb,left:20+(i%8)*155,top:20+Math.floor(i/8)*160});
  }
  fs.writeFileSync(path.join(folder,'manifest.json'),JSON.stringify(files,null,2));
  await sharp({create:{width:1260,height:345,channels:4,background:'#101827'}}).composite(preview).png().toFile(path.join(folder,'Prepared-coins.png'));
  console.log(JSON.stringify(files.map(x=>({file:x.file,size:[x.layout.width,x.layout.height]}))));
})();
