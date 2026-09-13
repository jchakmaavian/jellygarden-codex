// Crop and resize the generated crown, preserving its original transparent pixels.
const fs=require('node:fs');
const base=__dirname;
(async()=>{const c=await require('../soft-storybook-targets/pack-browser.cjs')();try{
  await c.call('Page.navigate',{url:'http://127.0.0.1:8766/art-review/treasure-hunt/board-directions/asset-styles/index.html'});
  await new Promise(r=>setTimeout(r,200));
  const result=await c.ev(`(async()=>{
    const im=new Image();im.src='art/bold-crown-master.png';await im.decode();
    const source=document.createElement('canvas');source.width=im.naturalWidth;source.height=im.naturalHeight;
    const ctx=source.getContext('2d');ctx.drawImage(im,0,0);
    const W=source.width,H=source.height,data=ctx.getImageData(0,0,W,H).data;
    let l=W,t=H,r=-1,b=-1,transparent=0;
    for(let y=0;y<H;y++)for(let x=0;x<W;x++){
      const a=data[(y*W+x)*4+3];if(a<16)transparent++;
      if(a>128){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
    }
    if(transparent<W*H*.12||r<l)throw new Error('Crown must have real transparency.');
    l=Math.max(0,l-2);t=Math.max(0,t-2);r=Math.min(W-1,r+2);b=Math.min(H-1,b+2);
    const w=r-l+1,h=b-t+1,scale=232/Math.max(w,h),out=document.createElement('canvas');out.width=out.height=256;
    const g=out.getContext('2d');g.imageSmoothingQuality='high';g.drawImage(source,l,t,w,h,(256-w*scale)/2,(256-h*scale)/2,w*scale,h*scale);
    return {crop:[l,t,w,h],sourceSize:[W,H],outputSize:[256,256],transparentFraction:transparent/(W*H),png:out.toDataURL('image/png').split(',')[1]};
  })()`);
  fs.writeFileSync(base+'/art/bold/crown-v2.png',Buffer.from(result.png,'base64'));delete result.png;
  fs.writeFileSync(base+'/art/bold-crown-crop.json',JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result));
}finally{c.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
