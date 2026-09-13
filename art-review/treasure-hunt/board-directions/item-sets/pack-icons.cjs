// Non-destructive cropping and resizing only; generated RGBA pixels retain their alpha.
const fs = require('node:fs');
const base = __dirname;
const ids = ['royal-keepsakes','storybook-magic','castle-garden','royal-sweets'];
const names = ['red','orange','purple','blue','green','yellow'];
(async()=>{
  const c=await require('../soft-storybook-targets/pack-browser.cjs')();
  try {
    await c.call('Page.navigate',{url:'http://127.0.0.1:8766/art-review/treasure-hunt/board-directions/item-sets/index.html'});
    await new Promise(r=>setTimeout(r,300));
    const packed=await c.ev(`(async()=>{
      const output=[];
      for(const style of ${JSON.stringify(ids.filter(id=>fs.existsSync(base+'/art/'+id+'-sheet.png')))}) {
        const im=new Image(); im.src='art/'+style+'-sheet.png'; await im.decode();
        const source=document.createElement('canvas'); source.width=im.naturalWidth; source.height=im.naturalHeight;
        const ctx=source.getContext('2d');ctx.drawImage(im,0,0);
        const W=source.width,H=source.height,pixels=ctx.getImageData(0,0,W,H).data;
        // Find quiet gutter lines near each nominal boundary, avoiding clipped tips.
        function gutters(size,other,cells,vertical){
          const out=[0];
          for(let k=1;k<cells;k++){
            const center=Math.round(k*size/cells),radius=Math.round(size/cells*.12);
            let best=center,bestCost=Infinity;
            for(let p=center-radius;p<=center+radius;p++){
              let count=0;for(let q=0;q<other;q++){const x=vertical?p:q,y=vertical?q:p;if(pixels[(y*W+x)*4+3]>128)count++;}
              const cost=count+Math.abs(p-center)*.015;
              if(cost<bestCost){bestCost=cost;best=p;}
            }
            out.push(best);
          }
          out.push(size);return out;
        }
        const xs=gutters(W,H,3,true),ys=gutters(H,W,2,false);
        for(const [i,name] of ${JSON.stringify(names)}.entries()){
          const col=i%3,row=Math.floor(i/3),x0=xs[col],y0=ys[row],cw=xs[col+1]-x0,ch=ys[row+1]-y0;
          let l=x0+cw,t=y0+ch,r=-1,b=-1,transparent=0;
          for(let y=y0;y<y0+ch;y++)for(let x=x0;x<x0+cw;x++){
            const a=pixels[(y*W+x)*4+3];if(a<16)transparent++;
            if(a>128){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
          }
          if(transparent<cw*ch*.1||r<l)throw new Error('Unusable sprite alpha: '+style+'/'+name);
          l=Math.max(x0,l-2);t=Math.max(y0,t-2);r=Math.min(x0+cw-1,r+2);b=Math.min(y0+ch-1,b+2);
          const w=r-l+1,h=b-t+1,scale=232/Math.max(w,h),o=document.createElement('canvas');o.width=o.height=256;
          const og=o.getContext('2d');og.imageSmoothingQuality='high';og.drawImage(source,l,t,w,h,(256-w*scale)/2,(256-h*scale)/2,w*scale,h*scale);
          output.push({style,name,cell:i,crop:[l,t,w,h],png:o.toDataURL('image/png').split(',')[1]});
        }
      }
      return output;
    })()`);
    for(const p of packed){fs.mkdirSync(base+'/art/'+p.style,{recursive:true});fs.writeFileSync(base+'/art/'+p.style+'/'+p.name+'.png',Buffer.from(p.png,'base64'));delete p.png;}
    const reuse={red:'../asset-styles/art/bold/piece-0.png',orange:'../asset-styles/art/bold/piece-5.png',purple:'art/royal-keepsakes/purple.png',blue:'../asset-styles/art/bold/piece-1.png',green:'../asset-styles/art/bold/piece-2.png',yellow:'../asset-styles/art/bold/piece-3.png'};
    fs.mkdirSync(base+'/art/playful-classics',{recursive:true});
    for(const [name,source] of Object.entries(reuse)){fs.copyFileSync(base+'/'+source,base+'/art/playful-classics/'+name+'.png');packed.push({style:'playful-classics',name,reusedFrom:source});}
    fs.writeFileSync(base+'/art/sprite-layout.json',JSON.stringify(packed,null,2)+'\n');
    console.log('Packed '+packed.length+' transparent sprites.');
  } finally {c.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
