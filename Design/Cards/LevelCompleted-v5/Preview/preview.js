(() => {
  'use strict';
  const C=window.ROYAL_WIN, $=s=>document.querySelector(s), q=s=>[...document.querySelectorAll(s)];
  const card=$('.card'), star=$('.star'), cushion=$('.cushion'), shadow=$('.star-shadow'), contact=$('.contact-shadow'), heading=$('.heading'), halo=$('.halo'), rays=$('.rays'), flash=$('.impact-flash');
  const clamp=(v,a=0,b=1)=>Math.max(a,Math.min(b,v)), lerp=(a,b,t)=>a+(b-a)*t, ease=v=>1-Math.pow(1-clamp(v),3), smooth=v=>{v=clamp(v);return v*v*(3-2*v)}, frac=v=>v-Math.floor(v);
  let playing=false, reduced=false, raf=0, start=0, time=0;
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
  $('.continue').style.top=C.actionRow-C.actionHeight/2+'px';$('.continue').style.left=(C.cardWidth-C.actionWidth)/2+'px';$('.continue').style.width=C.actionWidth+'px';$('.continue').style.height=C.actionHeight+'px';
  $('.level-number').textContent=C.level;$('.home-plus').textContent='+'+C.coins;$('.home-coin-value').textContent=C.balanceBefore;$('.home-star-value').textContent=C.starsBefore;$('.home-level span').textContent='Level '+(C.level+1);
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
  const burstSettings=burst.map((_,i)=>({start:C.impact+.18*hash(i*3+1),life:.70+.30*hash(i*3+2),angle:i*2.399963,radius:Math.min(235,C.starSize*(.43+.13*hash(i*7+5))),size:(i%4===0?35:20)+15*hash(i*5+7),spin:.22+.24*hash(i*11+3)}));
  const outerRadius=Math.min(235,C.starSize*.57),innerRadius=Math.min(outerRadius-8,C.starSize*.48);
  const orbitSettings=orbit.map((_,i)=>({phase:i*Math.PI*2/12,speed:.18+.04*hash(i*11+7),radius:lerp(innerRadius,outerRadius,hash(i*13+8)),aspect:.82+.08*hash(i*7+2),size:i%4===0?38:15+16*hash(i*5+9)}));
  const glintSchedule=[];let nextTwinkle=C.idleGlintStart;
  for(let index=0;index<600;index++){const phase=index%15,hero=phase<5||phase===14;glintSchedule.push({index,phase,hero,start:nextTwinkle,life:hero?.78:.62,size:hero?(phase===0||phase===14?56:46):24+6*hash(index*13+1)});nextTwinkle+=phase===14?.95:hero?.14:.09}
  const sparkleTop=C.titleRow+C.titleSize*.6+8;
  const coinNames=['coin__16660','coin.02__21355','coin.03__22083','coin.04__17107','coin.05__17096','coin.10__21779','coin.11__22228'];
  const coinFrames=coinNames.map(n=>'assets/Items-CoinItem-'+n+'.png'), coinKeys=[0,1,2,3,4,3,2,1,0,6,5,6];
  const coins=Array.from({length:10},()=>{const el=document.createElement('img');el.className='flying-coin';el.src=coinFrames[0];el.alt='';$('.home-coins-layer').appendChild(el);return el});
  const readyImages=coinFrames.map(src=>{const img=new Image();img.src=src;return img.decode().catch(()=>{})});
  function glintPose(el,x,y,size,alpha,angle=0){el.style.left=(x-size/2)+'px';el.style.top=(y-size/2)+'px';el.style.width=el.style.height=size+'px';el.style.opacity=clamp(alpha);el.style.transform=`rotate(${angle}deg)`}
  function pose(t){
    time=clamp(t,0,C.duration);t=time;$('#time').textContent=time.toFixed(2)+' s';$('#scrub').value=time;
    const home=t>=C.continueAt;
    card.style.visibility=home?'hidden':'visible';$('.dim').style.visibility=home?'hidden':'visible';$('.scene').classList.toggle('home',home);
    const intro=reduced?ease(t/.22):1;
    card.style.opacity=intro;card.style.transform='none';$('.dim').style.opacity=intro;
    let scale=1,lift=0,rotate=0;
    if(!reduced&&t<C.impact){const u=clamp(t/C.impact);scale=t<.06?lerp(2.4,1.50,ease(t/.06)):t<.15?lerp(1.50,1.30,(t-.06)/.09):lerp(1.30,.76,(t-.15)/.05);rotate=28*Math.cos(u*Math.PI*3)*Math.pow(1-u,1.2)}
    else if(!reduced&&t<C.rebound){const u=ease((t-C.impact)/(C.rebound-C.impact));scale=lerp(.76,1.08,u);lift=lerp(0,7,u);rotate=lerp(0,-2,u)}
    else if(!reduced){const u=ease((t-C.rebound)/(C.settle-C.rebound));scale=lerp(1.08,1,u);lift=lerp(7,0,u);rotate=lerp(-2,0,u)}
    star.style.transform=`translateY(${-lift}px) rotate(${-rotate}deg) scale(${scale})`;star.style.opacity=reduced?clamp(t/.2):1;
    let cushionX=1,cushionY=1;
    if(!reduced&&t>=C.impact&&t<C.impact+.04){const u=ease((t-C.impact)/.04);cushionX=lerp(1,1.055,u);cushionY=lerp(1,.88,u)}
    else if(!reduced&&t>=C.impact+.04&&t<C.impact+.16){const u=ease((t-C.impact-.04)/.12);cushionX=lerp(1.055,.96,u);cushionY=lerp(.88,1.30,u)}
    else if(!reduced&&t>=C.impact+.16&&t<C.impact+.40){const u=ease((t-C.impact-.16)/.24);cushionX=lerp(.96,1.035,u);cushionY=lerp(1.30,.965,u)}
    else if(!reduced&&t>=C.impact+.40&&t<C.cushionSettle){const u=ease((t-C.impact-.40)/(C.cushionSettle-C.impact-.40));cushionX=lerp(1.035,1,u);cushionY=lerp(.965,1,u)}
    cushion.style.transform=`scale(${cushionX},${cushionY})`;cushion.style.opacity=intro;
    const approach=ease(t/C.impact),settle=ease((t-C.impact)/(C.settle-C.impact));
    shadow.style.opacity=reduced?.28*intro:t<C.impact?lerp(.07,.32,approach)*clamp(t/.09):lerp(.32,.28,settle);
    shadow.style.transform=reduced?'none':`scale(${t<C.impact?lerp(1.15,1.035,approach):lerp(1.035,1,settle)},${t<C.impact?lerp(1.1,.96,approach):lerp(.96,1,settle)})`;
    contact.style.opacity=reduced?.25*clamp(t/.2):.25*ease((t-.12)/(C.impact-.12));
    letters.forEach((el,i)=>{const u=reduced?clamp(t/.20):clamp((t-C.writeStart)/C.perLetter-i);el.style.opacity=u;el.style.transform=`scale(${reduced?1:lerp(1.7,1,ease(u))})`});
    const rise=reduced?clamp(t/.2):ease((t-C.impact)/.34),breath=reduced?0:Math.sin((t-C.impact)*1.30);
    const flashIntensity=t<C.impact?clamp((t-C.impact+.025)/.025):1-clamp((t-C.impact)/.30);
    halo.style.opacity=rise*(.28+.03*breath);halo.style.transform='none';$('.pillow-light').style.opacity=rise*(.12+.018*breath)+(reduced?0:.26*flashIntensity*flashIntensity);
    rays.style.opacity=reduced?0:rise*.38;rays.style.transform=`rotate(${-Math.max(0,t-C.impact)*1.5}deg)`;
    flash.style.opacity=reduced?0:.52*flashIntensity*flashIntensity;flash.style.transform=`scale(${lerp(.70,1.08,clamp((t-C.impact+.025)/.325))})`;
    burst.forEach((el,i)=>{const p=burstSettings[i],u=(t-p.start)/p.life,a=p.angle+p.spin*clamp(u),radius=p.radius*(.86+.14*ease(u)),height=Math.min(radius*.84,Math.max(30,C.starRow-sparkleTop-p.size*.85)),alpha=!reduced&&u>=0&&u<=1?Math.sin(Math.PI*u):0;glintPose(el,C.cardWidth/2+Math.cos(a)*radius,C.starRow-Math.sin(a)*height+8-8*clamp(u),p.size*(.65+.50*alpha),alpha,-a*20)});
    orbit.forEach((el,i)=>{const p=orbitSettings[i],age=Math.max(0,t-C.impact),show=ease((age-.04)/.60),a=p.phase+age*p.speed,pulse=.5+.5*Math.sin(age*1.9+i*1.7),alpha=show*(.08+.82*pulse*pulse*pulse),height=Math.min(p.radius*p.aspect,Math.max(30,C.starRow-sparkleTop+12-p.size*.85));glintPose(el,C.cardWidth/2+Math.cos(a)*p.radius,C.starRow-Math.sin(a)*height+12,p.size*(.65+.50*pulse),reduced?0:alpha,-a*180/Math.PI*.15)});
    glints.forEach(el=>el.style.opacity=0);
    const tips=[[0,-.40],[.34,-.10],[.22,.29],[-.22,.29],[-.34,-.10]],braid=along=>({x:C.cardWidth/2+lerp(-.4,.4,along)*C.cushionWidth,y:C.cushionTop+C.cushionHeight*(1-(.345+.085*Math.pow(Math.abs(along*2-1),2)))});
    if(!reduced)for(const p of glintSchedule){const u=(t-p.start)/p.life;if(u<0||u>1)continue;const envelope=Math.sin(Math.PI*u),alpha=envelope*(p.hero?.84+.16*Math.cos(4*Math.PI*u):1);let x,y;
      if(p.hero){const tip=tips[p.phase===14?0:p.phase];x=C.cardWidth/2+tip[0]*C.starSize;y=C.starRow+tip[1]*C.starSize}
      else{const along=(p.phase-5)/8,a=braid(along),b=braid(Math.min(1,along+.07));x=lerp(a.x,b.x,ease(u));y=lerp(a.y,b.y,ease(u))}
      glintPose(glints[p.index%14],x,y,p.size*(.40+.80*envelope),alpha,-16*Math.sin(u*Math.PI));
    }
    const homeState=poseHome(home?t-C.continueAt:-1);
    window.previewState={time:t,reduced,phase:home?'Home':'Level complete',starScale:scale,starLift:lift,starVisibleTop:C.starRow-lift-C.starSize*scale*(.5-17/256),cardVisible:!home,popupCoinFlights:0,cardWidth:C.cardWidth,cardHeight:C.cardHeight,...homeState};
  }
  // Native Home uses random burst/control offsets. Fixed seeds make this offline reconstruction seekable.
  const homeFrom={x:C.canvasWidth/2,y:C.canvasHeight*.59},starTo={x:565.18,y:82.59},coinTo={x:170,y:82.22};
  const homeBurst={x:homeFrom.x-35,y:homeFrom.y-29},homeControl={x:lerp(homeBurst.x,starTo.x,.2)+95,y:lerp(homeBurst.y,starTo.y,.55)};
  const coinPaths=coins.map((_,i)=>{const a=hash(i*17+4)*Math.PI*2,r=lerp(48*.4,48,hash(i*23+9)),b={x:homeFrom.x+Math.cos(a)*r,y:homeFrom.y-Math.sin(a)*r*.75};return {burst:b,control:{x:lerp(b.x,coinTo.x,.24)-Math.sign(b.x-coinTo.x)*55,y:lerp(b.y,coinTo.y,.35)}}});
  const curve=(a,b,c,u)=>{const w=clamp(u)**2;return {x:(1-w)**2*a.x+2*(1-w)*w*b.x+w*w*c.x,y:(1-w)**2*a.y+2*(1-w)*w*b.y+w*w*c.y}};
  function place(el,p,size,scale,rotation=0,flip=1){el.style.left=(p.x-size/2)+'px';el.style.top=(p.y-size/2)+'px';el.style.transform=`rotate(${rotation}deg) scale(${scale*flip},${scale})`}
  function pulseAt(age){return age<0||age>=.27?1:age<.08?lerp(1,1.05,ease(age/.08)):age<.21?lerp(1.05,.98,smooth((age-.08)/.13)):lerp(.98,1,smooth((age-.21)/.06))}
  function poseHome(h){
    const visible=h>=0,animated=visible&&!reduced;
    $('.home-ui').style.opacity=visible?1:0;$('.home-rewards').style.opacity=visible?1:0;
    const homeStar=$('.home-reward-star'),age=h-C.homeStarDelay,burstTime=C.homeStarPop+C.homeStarHold,arrival=C.homeStarDelay+burstTime+C.homeStarFlight;
    const starVisible=animated&&age>=0&&h<arrival,starLanded=visible&&(reduced||h>=arrival);
    homeStar.style.opacity=starVisible?clamp(age/.06):0;
    if(starVisible){let p,s,rock=0;
      if(age<burstTime){const u=ease(age/C.homeStarPop);p={x:lerp(homeFrom.x,homeBurst.x,u),y:lerp(homeFrom.y,homeBurst.y,u)};s=lerp(.55,1,u);rock=-5*Math.sin(clamp((age-C.homeStarPop)/C.homeStarHold)*Math.PI*2)}
      else{const u=clamp((age-burstTime)/C.homeStarFlight);p=curve(homeBurst,homeControl,starTo,u);s=lerp(1,.72,u)}
      place(homeStar,p,110,s,rock);
    }
    let landed=0,flightVisible=0,minMargin=Infinity,lastArrival=-Infinity;
    coins.forEach((el,i)=>{
      const delay=C.homeCoinStart+C.homeCoinDelay+i*C.homeCoinStagger,e=h-delay,pop=C.homeCoinPop+i*C.homeCoinHoldStep,arrivalTime=delay+pop+C.homeCoinFlight;
      const arrived=visible&&(reduced||h>=arrivalTime);if(arrived){landed++;lastArrival=Math.max(lastArrival,arrivalTime)}
      if(!animated||e<0||arrived){el.style.opacity=0;return}
      const path=coinPaths[i];let p,s;
      if(e<pop){const u=ease(e/.45);p={x:lerp(homeFrom.x,path.burst.x,u),y:lerp(homeFrom.y,path.burst.y,u)};s=lerp(.55,1,u)}
      else{const u=clamp((e-pop)/C.homeCoinFlight);p=curve(path.burst,path.control,coinTo,u);s=lerp(1,.72,u)}
      const key=Math.floor((e%.2)*60)%12,src=coinFrames[coinKeys[key]],flip=key>=5&&key<10?-1:1;
      if(el.getAttribute('src')!==src)el.src=src;
      place(el,p,70,s,0,flip);el.style.opacity=clamp(e/.06);flightVisible++;
      const half=35*s;minMargin=Math.min(minMargin,p.x-half,p.y-half,C.canvasWidth-p.x-half,C.canvasHeight-p.y-half);
    });
    const plusAge=h-C.homeCoinStart;
    $('.home-plus').style.opacity=animated&&plusAge>=0?1-clamp((plusAge-1.8)/.5):0;
    $('.home-plus').style.top=(homeFrom.y-55-45-18*ease(plusAge/.8))+'px';
    const balance=C.balanceBefore+(landed===10?C.coins:Math.floor(C.coins/10)*landed),stars=C.starsBefore+(starLanded?1:0);
    $('.home-coin-value').textContent=balance;$('.home-star-value').textContent=stars;
    $('.home-coin-icon').style.transform=`scale(${animated?pulseAt(h-lastArrival):1})`;
    return {homeTime:visible?h:null,homeVisible:visible,starInFlight:starVisible,starLanded,flightVisible,landed,balance,stars,minCoinMargin:minMargin===Infinity?null:minMargin};
  }
  function tick(now){if(!playing)return;pose((now-start)/1000);if(time>=C.duration){playing=false;$('#status').textContent='Complete';return}raf=requestAnimationFrame(tick)}
  function play(from=0){cancelAnimationFrame(raf);playing=true;start=performance.now()-clamp(Number(from)||0,0,C.duration)*1000;$('#status').textContent='Playing';pose((performance.now()-start)/1000);raf=requestAnimationFrame(tick)}
  function seek(t){playing=false;cancelAnimationFrame(raf);pose(Number(t)||0);$('#status').textContent='Paused';return window.previewState}
  function setReduced(value){reduced=!!value;$('#reduced').checked=reduced;pose(time);return window.previewState}
  function continuePreview(){if(time<C.continueAt)play(C.continueAt);return window.previewState}
  $('#replay').addEventListener('click',()=>play());$('#reduced').addEventListener('change',e=>setReduced(e.target.checked));$('#scrub').addEventListener('input',e=>seek(e.target.value));$('.continue').addEventListener('click',continuePreview);$('.close').addEventListener('click',continuePreview);
  window.preview={seek,play,setReduced,continue:continuePreview,state:()=>window.previewState,config:C};
  const backgroundImage=new Image();backgroundImage.src='assets/castle-background.webp';
  window.previewReady=Promise.all([document.fonts.ready,backgroundImage.decode(),...q('img').map(i=>i.decode()),...readyImages]).then(()=>{fit();reduced=params.get('reduced')==='1';$('#reduced').checked=reduced;if(params.has('t'))seek(Number(params.get('t')));else play();return true});
})();
