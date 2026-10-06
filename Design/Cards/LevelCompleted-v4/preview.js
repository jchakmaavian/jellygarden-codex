(() => {
  'use strict';
  const C=window.ROYAL_WIN, $=s=>document.querySelector(s), q=s=>[...document.querySelectorAll(s)];
  const card=$('.card'), star=$('.star'), cushion=$('.cushion'), shadow=$('.star-shadow'), contact=$('.contact-shadow'), heading=$('.heading'), halo=$('.halo'), rays=$('.rays'), flash=$('.impact-flash'), badge=$('.reward');
  const clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v)), lerp=(a,b,t)=>a+(b-a)*t, ease=v=>1-Math.pow(1-clamp(v),3), smooth=v=>{v=clamp(v);return v*v*(3-2*v)}, frac=v=>v-Math.floor(v);
  let playing=false, reduced=false, raf=0, start=0, time=0, collected=false;
  const params=new URLSearchParams(location.search);
  if(params.has('capture'))document.body.classList.add('capture');
  function fit(){ $('.scene').style.transform=`scale(${$('.viewport').clientWidth/C.canvasWidth})`; }
  new ResizeObserver(fit).observe($('.viewport')); fit();
  card.style.height=C.cardHeight+'px';card.style.top=(C.canvasHeight/2-C.cardOffsetY-C.cardHeight/2)+'px';
  heading.style.top=(C.titleRow-C.titleSize*.7)+'px';heading.style.height=C.titleSize*1.4+'px';heading.style.fontSize=C.titleSize+'px';
  star.style.width=star.style.height=C.starSize+'px';star.style.left=(C.cardWidth-C.starSize)/2+'px';star.style.top=C.starRow-C.starSize/2+'px';
  cushion.style.left=(C.cardWidth-C.cushionWidth)/2+'px';cushion.style.top=C.cushionTop+'px';cushion.style.width=C.cushionWidth+'px';cushion.style.height=C.cushionHeight+'px';
  for(const [el,size] of [[halo,530],[rays,580],[flash,520]]){el.style.top=(C.starRow-size/2+(el===halo?8:0))+'px';el.style.left=(C.cardWidth-size)/2+'px';el.style.width=el.style.height=size+'px'}
  const seat=C.cushionTop+C.cushionHeight*.36;
  shadow.style.width='352px';shadow.style.height='84px';shadow.style.left=(C.cardWidth/2+9-176)+'px';shadow.style.top=(C.starRow+C.starSize*.36-42)+'px';shadow.style.objectFit='fill';shadow.style.transformOrigin='50% 50%';
  contact.style.width='260px';contact.style.height='56px';contact.style.left=(C.cardWidth/2+3-130)+'px';contact.style.top=(C.starRow+C.starSize*.35-28)+'px';
  $('.pillow-light').style.width='360px';$('.pillow-light').style.height='124px';$('.pillow-light').style.left=(C.cardWidth/2-180)+'px';$('.pillow-light').style.top=(C.starRow+C.starSize*.34-62)+'px';
  badge.style.top=C.rewardRow-37+'px';$('.continue').style.top=C.actionRow-60+'px';$('.continue').style.left=(C.cardWidth-C.actionWidth)/2+'px';$('.continue').style.width=C.actionWidth+'px';
  $('.level-number').textContent=C.level;$('.reward span').textContent='+'+C.coins;$('.balance span').textContent=C.balanceBefore;
  function layer(parent,inset,r,top,bottom,extra=''){
    const el=document.createElement('i');el.style.cssText=`inset:${inset}px;border-radius:${r}px;background:linear-gradient(${top},${bottom});${extra}`;parent.appendChild(el);return el;
  }
  const surface=$('.surface');
  layer(surface,1.5,36,'#f9c643','#efa925');
  layer(surface,1.5,36,'#fff6c8f2','#fff6c800','bottom:auto;height:80px');
  layer(surface,1.5,36,'#ffe17a00','#ffe17a8c','top:auto;height:80px');
  layer(surface,9.6,27.9,'#a8591a','#7a3913');
  layer(surface,13.5,24,'#04256e','#021745');
  layer(surface,15.6,21.9,'#0b5ac6','#0851b8');
  layer(surface,17.6,19.9,'#102d6b','#061536');
  layer(surface,17.6,19.9,'#2955a438','#16387400','bottom:auto;height:370.4px');
  layer(surface,17.6,19.9,'transparent','transparent').className='face-glow';
  const close=$('.close');
  for(const a of [[.9,35.85,'#c9681a','#a65226'],[2.4,34.35,'#ffeea0','#f0b43a'],[7.2,29.55,'#c65b12','#a44c10'],[8.6,28.15,'#1d4ea8','#04194a']])layer(close,...a);
  layer(close,8.6,28.15,'#2d62c08c','#2d62c000');
  const action=$('.continue');
  for(const a of [[.8,46.6,'#d0680e','#b85a0c'],[2.6,44.8,'#ffe06a','#e19a1c'],[2.6,44.8,'#fffbd8bf','#fffbd800'],[8.8,38.6,'#c7650f','#a54c05'],[10.3,37.1,'#16241a','#0c2a1e']])layer(action,...a);
  heading.textContent='';const letters=[...'Well Done!'].map(char=>{const span=document.createElement('span');span.textContent=char;heading.appendChild(span);return span});
  function spark(parent,hero=false){const el=document.createElement('i');el.className='spark'+(hero?' hero':'');parent.appendChild(el);return el}
  const burst=Array.from({length:24},()=>spark($('.sparkles'))), glints=Array.from({length:14},()=>spark($('.sparkles'),true)), orbit=Array.from({length:12},()=>spark($('.halo-sparkles')));
  // Exact procedural alpha from SparkleTextureFactory.FourPointSparkle, including its soft halo and gamma lift.
  const sparkleCanvas=document.createElement('canvas');sparkleCanvas.width=sparkleCanvas.height=64;
  const sparkleContext=sparkleCanvas.getContext('2d'), sparklePixels=sparkleContext.createImageData(64,64);
  for(let y=0;y<64;y++)for(let x=0;x<64;x++){
    const u=x*2/63-1,v=y*2/63-1,r2=u*u+v*v,tu=Math.max(0,1-Math.abs(u)),tv=Math.max(0,1-Math.abs(v));
    const alpha=r2>1?0:Math.pow(clamp(Math.exp(-r2*14)+.95*(Math.exp(-v*v*320)*tu*tu*tu+Math.exp(-u*u*320)*tv*tv*tv)+Math.exp(-r2*5)*.16),.85),at=(y*64+x)*4;
    sparklePixels.data[at]=255;sparklePixels.data[at+1]=247;sparklePixels.data[at+2]=210;sparklePixels.data[at+3]=Math.floor(alpha*255);
  }
  sparkleContext.putImageData(sparklePixels,0,0);
  [...burst,...glints,...orbit].forEach(el=>el.style.backgroundImage=`url(${sparkleCanvas.toDataURL()})`);
  // The mesh has ten triangular wedges. Its thirteen vertices along each side sample the same soft radial falloff.
  const rayCanvas=document.createElement('canvas');rayCanvas.width=rayCanvas.height=580;rays.appendChild(rayCanvas);
  const rayContext=rayCanvas.getContext('2d'),rayGradient=rayContext.createRadialGradient(290,290,0,290,290,290);
  for(let ring=0;ring<=12;ring++){const u=ring/12;rayGradient.addColorStop(u,`rgba(255,224,133,${.32*Math.sin(Math.PI*u)*(1-u)})`)}
  rayContext.fillStyle=rayGradient;
  for(let i=0;i<10;i++){const a=i*Math.PI*2/10;rayContext.beginPath();rayContext.moveTo(290,290);rayContext.lineTo(290+Math.cos(a-.095)*290,290+Math.sin(a-.095)*290);rayContext.lineTo(290+Math.cos(a+.095)*290,290+Math.sin(a+.095)*290);rayContext.closePath();rayContext.fill()}
  const hash=n=>{let x=Math.imul(n,2654435761)>>>0;x=(x^(x>>>15))>>>0;x=Math.imul(x,2246822519)>>>0;x=(x^(x>>>13))>>>0;return (x&0xffffff)/0x1000000};
  const burstSettings=burst.map((_,i)=>({start:C.impact+.36*hash(i*3+1),life:.70+.30*hash(i*3+2),angle:i*2.399963,radius:Math.min(235,C.starSize*(.43+.13*hash(i*7+5))),size:(i%4===0?35:20)+15*hash(i*5+7),spin:.22+.24*hash(i*11+3)}));
  const outerRadius=Math.min(235,C.starSize*.57),innerRadius=Math.min(outerRadius-8,C.starSize*.48);
  const orbitSettings=orbit.map((_,i)=>({phase:i*Math.PI*2/12,speed:.18+.04*hash(i*11+7),radius:lerp(innerRadius,outerRadius,hash(i*13+8)),aspect:.82+.08*hash(i*7+2),size:i%4===0?38:15+16*hash(i*5+9)}));
  const glintSchedule=[];let nextTwinkle=1.16;
  for(let index=0;index<600;index++){const phase=index%15,hero=phase<5||phase===14;glintSchedule.push({index,phase,hero,start:nextTwinkle,life:hero?.78:.62,size:hero?(phase===0||phase===14?56:46):24+6*hash(index*13+1)});nextTwinkle+=phase===14?.95:hero?.14:.09}
  const sparkleTop=C.titleRow+C.titleSize*.6+8;
  const coinNames=['coin__16660','coin.02__21355','coin.03__22083','coin.04__17107','coin.05__17096','coin.10__21779','coin.11__22228'];
  const coinFrames=coinNames.map(n=>'assets/Items-CoinItem-'+n+'.png'), coinKeys=[0,1,2,3,4,3,2,1,0,6,5,6];
  const coins=Array.from({length:10},()=>{const el=document.createElement('img');el.className='flying-coin';el.src=coinFrames[0];el.alt='';$('.flying-coins').appendChild(el);return el});
  const readyImages=coinFrames.map(src=>{const img=new Image();img.src=src;return img.decode().catch(()=>{})});
  function glintPose(el,x,y,size,alpha,angle=0){el.style.left=(x-size/2)+'px';el.style.top=(y-size/2)+'px';el.style.width=el.style.height=size+'px';el.style.opacity=clamp(alpha);el.style.transform=`rotate(${angle}deg)`}
  function pose(t){
    time=Math.max(0,t);$('#time').textContent=time.toFixed(2)+' s';$('#scrub').value=Math.min(time,6);card.style.visibility=collected?'hidden':'visible';
    const intro=reduced?clamp(t/.20):ease(t/.42);
    card.style.opacity=intro;card.style.transform=reduced?'none':`translateY(${(1-intro)*18}px) scale(${lerp(.96,1,intro)})`;
    let scale=1,lift=0,rotate=0;
    if(!reduced&&t<C.impact){const u=smooth((t-.10)/(C.impact-.10));scale=lerp(C.entranceScale,.96,u);const revealLift=clamp(C.starRow-sparkleTop-C.starSize*.44*C.entranceScale,0,20);lift=lerp(revealLift,-4,u);rotate=lerp(-4,.65,u)}
    else if(!reduced&&t<.68){const u=ease((t-C.impact)/(.68-C.impact));scale=lerp(.96,1.025,u);lift=lerp(-4,3,u);rotate=lerp(.65,-.35,u)}
    else if(!reduced){const u=ease((t-.68)/(C.settle-.68));scale=lerp(1.025,1,u);lift=lerp(3,0,u);rotate=lerp(-.35,0,u)}
    star.style.transform=`translateY(${-lift}px) rotate(${rotate}deg) scale(${scale})`;star.style.opacity=reduced?clamp(t/.2):ease(t/.16);
    let cushionX=1,cushionY=1;
    if(!reduced&&t>=C.impact&&t<C.impact+.09){const u=ease((t-C.impact)/.09);cushionX=lerp(1,1.025,u);cushionY=lerp(1,.93,u)}
    else if(!reduced&&t>=C.impact+.09&&t<C.impact+.24){const u=ease((t-C.impact-.09)/.15);cushionX=lerp(1.025,.998,u);cushionY=lerp(.93,1.012,u)}
    else if(!reduced&&t>=C.impact+.24&&t<C.settle){const u=ease((t-C.impact-.24)/(C.settle-C.impact-.24));cushionX=lerp(.998,1,u);cushionY=lerp(1.012,1,u)}
    cushion.style.transform=`scale(${cushionX},${cushionY})`;cushion.style.opacity=intro;
    const approach=ease(t/C.impact),settle=ease((t-C.impact)/(C.settle-C.impact));
    shadow.style.opacity=reduced?.28*intro:t<C.impact?lerp(.07,.32,approach)*clamp(t/.09):lerp(.32,.28,settle);
    shadow.style.transform=reduced?'none':`scale(${t<C.impact?lerp(1.15,1.035,approach):lerp(1.035,1,settle)},${t<C.impact?lerp(1.1,.96,approach):lerp(.96,1,settle)})`;
    contact.style.opacity=reduced?.25*intro:.25*ease((t-.28)/(C.impact-.28));
    letters.forEach((el,i)=>{const u=reduced?clamp(t/.20):clamp((t-.56)/.047-i);el.style.opacity=u;el.style.transform=`scale(${reduced?1:lerp(1.7,1,ease(u))})`});
    const rise=reduced?clamp(t/.2):ease((t-C.impact)/.46),breath=reduced?0:Math.sin((t-C.impact)*1.30);
    halo.style.opacity=rise*(.37+.035*breath);halo.style.transform='none';$('.pillow-light').style.opacity=rise*(.12+.018*breath);
    rays.style.opacity=reduced?0:rise*.38;rays.style.transform=`rotate(${-Math.max(0,t-C.impact)*1.5}deg)`;
    const flashIntensity=t<C.impact?clamp((t-C.impact+.09)/.09):1-clamp((t-C.impact)/.30);flash.style.opacity=reduced?0:.24*flashIntensity*flashIntensity;flash.style.transform=`scale(${lerp(.88,1.04,clamp((t-C.impact+.09)/.39))})`;
    burst.forEach((el,i)=>{const p=burstSettings[i],u=(t-p.start)/p.life,a=p.angle+p.spin*clamp(u),radius=p.radius*(.86+.14*ease(u)),height=Math.min(radius*.84,Math.max(30,C.starRow-sparkleTop-p.size*.85)),alpha=!reduced&&u>=0&&u<=1?Math.sin(Math.PI*u):0;glintPose(el,C.cardWidth/2+Math.cos(a)*radius,C.starRow-Math.sin(a)*height+8-8*clamp(u),p.size*(.65+.50*alpha),alpha,-a*20)});
    orbit.forEach((el,i)=>{const p=orbitSettings[i],age=Math.max(0,t-C.impact),show=ease((age-.04)/.60),a=p.phase+age*p.speed,pulse=.5+.5*Math.sin(age*1.9+i*1.7),alpha=show*(.08+.82*pulse*pulse*pulse),height=Math.min(p.radius*p.aspect,Math.max(30,C.starRow-sparkleTop+12-p.size*.85));glintPose(el,C.cardWidth/2+Math.cos(a)*p.radius,C.starRow-Math.sin(a)*height+12,p.size*(.65+.50*pulse),reduced?0:alpha,-a*180/Math.PI*.15)});
    glints.forEach(el=>el.style.opacity=0);
    const tips=[[0,-.40],[.34,-.10],[.22,.29],[-.22,.29],[-.34,-.10]],braid=along=>({x:C.cardWidth/2+lerp(-.4,.4,along)*C.cushionWidth,y:C.cushionTop+C.cushionHeight*(1-(.345+.085*Math.pow(Math.abs(along*2-1),2)))});
    if(!reduced)for(const p of glintSchedule){const u=(t-p.start)/p.life;if(u<0||u>1)continue;const envelope=Math.sin(Math.PI*u),alpha=envelope*(p.hero?.84+.16*Math.cos(4*Math.PI*u):1);let x,y;
      if(p.hero){const tip=tips[p.phase===14?0:p.phase];x=C.cardWidth/2+tip[0]*C.starSize;y=C.starRow+tip[1]*C.starSize}
      else{const along=(p.phase-5)/8,a=braid(along),b=braid(Math.min(1,along+.07));x=lerp(a.x,b.x,ease(u));y=lerp(a.y,b.y,ease(u))}
      glintPose(glints[p.index%14],x,y,p.size*(.40+.80*envelope),alpha,-16*Math.sin(u*Math.PI));
    }
    const rewardU=reduced?clamp(t/.2):ease((t-C.rewardStart)/.32);
    badge.style.opacity=rewardU;badge.style.transform=`translateY(${reduced?0:12*(1-rewardU)}px)`;$('.reward span').textContent='+'+Math.round(C.coins*(reduced?1:ease(Math.round(clamp((t-C.rewardStart)/.62)*64)/64)));
    let landed=0,flightVisible=0,minMargin=Infinity;
    const badgeCoin={x:C.cardWidth/2-67,y:C.rewardRow},from={x:badgeCoin.x-50,y:badgeCoin.y-123};
    coins.forEach((el,i)=>{
      const age=t-(C.rewardReady+.12)-i*.055,burst={x:badgeCoin.x-127+(i%5-2)*12,y:badgeCoin.y-112-i%3*14},control={x:badgeCoin.x-113-i%3*7,y:badgeCoin.y+10-i%2*12};
      if(reduced||age<0||age>=1.15){el.style.opacity=0;if(age>=1.15)landed++;return}
      let x,y,s;
      if(age<.55){const u=ease(age/.45);x=lerp(from.x,burst.x,u);y=lerp(from.y,burst.y,u);s=lerp(.55,1,u)}
      else{const u=clamp((age-.55)/.6),w=u*u;x=(1-w)*(1-w)*burst.x+2*(1-w)*w*control.x+w*w*badgeCoin.x;y=(1-w)*(1-w)*burst.y+2*(1-w)*w*control.y+w*w*badgeCoin.y;s=lerp(1,.72,u)}
      const key=Math.floor((age%.2)*60)%12,src=coinFrames[coinKeys[key]],flip=key>=5&&key<10?-1:1;el.src=src;
      const half=23*s;x=clamp(x,24+half,C.cardWidth-24-half);y=clamp(y,24+half,C.cardHeight-24-half);
      el.style.left=(x-24-23)+'px';el.style.top=(y-24-23)+'px';el.style.transform=`scale(${s*flip},${s})`;el.style.opacity=clamp(age/.06);flightVisible++;minMargin=Math.min(minMargin,x-half,y-half,C.cardWidth-x-half,C.cardHeight-y-half);
    });
    $('.balance').style.opacity=ease((t-.15)/.25);$('.balance span').textContent=C.balanceBefore+(reduced?C.coins:landed===10?C.coins:Math.floor(C.coins/10)*landed);
    const lastArrival=C.rewardReady+.12+.055*(Math.max(0,landed-1))+1.15,pa=t-lastArrival,pulse=landed>0&&pa>=0&&pa<.27?Math.sin(pa/.27*Math.PI)*.05:0;
    $('.reward img').style.transform=`scale(${1+pulse})`;
    const shineAge=(t-2.9)%4;$('.button-sheen').style.opacity=!reduced&&shineAge>=0&&shineAge<.9?.58:0;$('.button-sheen').style.left=(-100+clamp(shineAge/.9)*(C.actionWidth+200))+'px';
    window.previewState={time:t,reduced,starScale:scale,starLift:lift,starVisibleTop:C.starRow-lift-C.starSize*scale*(.5-17/256),flightVisible,minCoinMargin:minMargin===Infinity?null:minMargin,landed,cardWidth:C.cardWidth,cardHeight:C.cardHeight};
  }
  function tick(now){if(!playing)return;pose((now-start)/1000);raf=requestAnimationFrame(tick)}
  function play(){cancelAnimationFrame(raf);collected=false;playing=true;start=performance.now();$('#status').textContent='Playing';raf=requestAnimationFrame(tick)}
  function seek(t){playing=false;cancelAnimationFrame(raf);collected=false;pose(Number(t));$('#status').textContent='Paused';return window.previewState}
  function setReduced(value){reduced=!!value;$('#reduced').checked=reduced;pose(time)}
  function continuePreview(){playing=false;cancelAnimationFrame(raf);collected=true;card.style.visibility='hidden';$('.balance span').textContent=C.balanceBefore+C.coins;$('#status').textContent='Collected'}
  $('#replay').addEventListener('click',play);$('#reduced').addEventListener('change',e=>{setReduced(e.target.checked);play()});$('#scrub').addEventListener('input',e=>seek(e.target.value));$('.continue').addEventListener('click',continuePreview);$('.close').addEventListener('click',continuePreview);
  window.preview={seek,play,setReduced,state:()=>window.previewState,config:C};
  window.previewReady=Promise.all([document.fonts.ready,...q('img').map(i=>i.decode().catch(()=>{})),...readyImages]).then(()=>{fit();if(params.has('t'))seek(Number(params.get('t')));else play();return true});
})();
