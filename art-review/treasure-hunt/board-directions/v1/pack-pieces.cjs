const fs=require('node:fs'),path=require('node:path');
const root=__dirname;
(async()=>{const c=await require('./pack-browser.cjs')();try{
 await c.call('Page.navigate',{url:'http://127.0.0.1:8766/art-review/treasure-hunt/board-directions/v1/'});
 await new Promise(r=>setTimeout(r,500));
 const data=await c.ev(`(async()=>{
 const ids=['royal-gems','soft-storybook','enchanted-jewels','royal-keepsakes','garden-magic'],out=[];
 for(const id of ids){const im=new Image();im.src='art/'+id+'-sheet.png';await im.decode();
 for(let i=0;i<6;i++){
  const w=im.naturalWidth/3,h=im.naturalHeight/2,cell=document.createElement('canvas');cell.width=w;cell.height=h;
  const g=cell.getContext('2d');g.drawImage(im,(i%3)*w,Math.floor(i/3)*h,w,h,0,0,w,h);
  const px=g.getImageData(0,0,w,h).data;let x0=w,y0=h,x1=0,y1=0;
  for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(px[(y*w+x)*4+3]>32){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  x0=Math.max(0,x0-3);y0=Math.max(0,y0-3);x1=Math.min(w-1,x1+3);y1=Math.min(h-1,y1+3);
  const sw=x1-x0+1,sh=y1-y0+1,scale=232/Math.max(sw,sh),o=document.createElement('canvas');o.width=o.height=256;
  const og=o.getContext('2d');og.imageSmoothingQuality='high';og.drawImage(cell,x0,y0,sw,sh,(256-sw*scale)/2,(256-sh*scale)/2,sw*scale,sh*scale);
  out.push({id,index:i,sourceCell:[i%3,Math.floor(i/3)],crop:[x0,y0,sw,sh],png:o.toDataURL('image/png').split(',')[1]});
 }}return out;})()`);
 for(const p of data){fs.mkdirSync(path.join(root,'art',p.id),{recursive:true});fs.writeFileSync(path.join(root,'art',p.id,p.index+'.png'),Buffer.from(p.png,'base64'));delete p.png;}
 fs.writeFileSync(path.join(root,'sprite-layout.json'),JSON.stringify({size:256,padding:12,pieces:data},null,2)+'\n');
 console.log('Packed '+data.length+' transparent sprites from original sheets.');
 }finally{c.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
