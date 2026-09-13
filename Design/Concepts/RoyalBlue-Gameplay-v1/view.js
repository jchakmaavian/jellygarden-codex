(function(){
'use strict';
const root=document.getElementById('rt-gameplay-study');if(!root)return;
const q=s=>root.querySelector(s),qa=s=>[...root.querySelectorAll(s)];
const assets=RT_ASSETS,engine=new RoyalEngine(713),phone=q('.rt-phone'),board=q('#rt-board'),pieces=q('#rt-pieces');
const names={ruby:'Ruby',shield:'Shield',crown:'Crown',leaf:'Leaf',lily:'Royal lily',rocket:'Rocket',tnt:'TNT',helicopter:'Helicopter',orb:'Royal orb',crate:'Crate',vase:'Vase',hammer:'Royal hammer'};
const prefs={motion:!matchMedia('(prefers-reduced-motion: reduce)').matches,sound:false,boardTone:'blue',kingScale:1};
let view=engine.snapshot(),selected=-1,armed=null,busy=false,epoch=0,paused=false,previousFocus=null,kingTimer,wordTimer,hintTimer,screen='game',chosenBoosts=new Set(),reviewOutcome=false,seed=713,breath=null,celebrationToken=0;
const animations=new Set(),cache=new Map(),metrics={actions:0,peakParticles:0,frames:[],engineMs:[],effects:[],kingStates:[]};
const images={};for(const [name,src]of Object.entries(assets)){const img=new Image();img.src=src;images[name]=img;}
q('.rt-scenery').style.backgroundImage=`url("${assets['home-castle']}")`;
const img=(name,cls='',alt='')=>`<img class="${cls}" src="${assets[name]}" alt="${alt}">`;
function animate(node,frames,options){if(!prefs.motion)return null;const a=node.animate(frames,options);animations.add(a);a.finished.catch(()=>{}).finally(()=>animations.delete(a));return a;}
function applyPrefs(){root.dataset.motion=prefs.motion?'on':'off';root.style.setProperty('--rt-board',prefs.boardTone==='ivory'?'#b8c6d3':'#8dadd2');if(!prefs.motion){animations.forEach(a=>a.cancel());fx.clear();breath=null;}else if(!breath||breath.playState==='idle'){breath=animate(q('.rt-king-halo'),[{opacity:.6,transform:'scale(.97)'},{opacity:.95,transform:'scale(1.06)'},{opacity:.6,transform:'scale(.97)'}],{duration:3800,iterations:Infinity});}q('#rt-king-sprite').style.width=`${148*prefs.kingScale}%`;}
function react(state,message,duration=1500){
 clearTimeout(kingTimer);const key=state==='cheer'?'king-cheer':state==='think'?'king-think':'king-idle';q('#rt-king-sprite').src=assets[key];q('#rt-king-sprite').alt=state==='cheer'?'King celebrating your move':state==='think'?'King thinking about the next move':'King watching the board';q('#rt-speech').textContent=message;metrics.kingStates.push(state);
 if(state==='cheer')animate(q('#rt-king-sprite'),[{transform:'translateY(0) rotate(0)'},{transform:'translateY(-7px) rotate(-3deg)',offset:.25},{transform:'translateY(1px) rotate(2deg)',offset:.65},{transform:'translateY(0) rotate(0)'}],{duration:650,easing:'ease-out'});
 else if(state==='think')animate(q('#rt-king-sprite'),[{transform:'rotate(0deg)'},{transform:'rotate(-4deg)'},{transform:'rotate(0deg)'}],{duration:650});
 if(duration)kingTimer=setTimeout(()=>{if(screen!=='game')return;react(view.moves<=5?'think':'idle',view.moves<=5?'You have this. Look for a combination.':'Your next brilliant move awaits.',0)},duration);
}
function say(text){q('#rt-hint').textContent=text;}
function word(text){clearTimeout(wordTimer);const el=q('#rt-burst-word');el.textContent=text;el.classList.add('show');wordTimer=setTimeout(()=>el.classList.remove('show'),prefs.motion?720:400);}
function render(snap,mode='normal'){
 view=snap;q('#rt-moves').textContent=view.moves;root.dataset.low=String(view.moves<=5);
 q('#rt-goals').innerHTML=Object.entries(view.goals).map(([kind,count])=>`<div class="rt-goal" data-goal="${kind}">${img(kind)}<span aria-label="${count} ${kind}s remaining">${count===0?'✓':count}</span></div>`).join('');
 const seen=new Set();view.board.forEach((p,i)=>{if(!p)return;seen.add(p.id);let node=cache.get(p.id);
  if(!node){node=document.createElement('div');node.className='rt-piece';node.innerHTML=img(p.kind);node.dataset.id=p.id;pieces.appendChild(node);cache.set(p.id,node);node.style.transform=`translate(${i%8*100}%,${(Math.floor(i/8)-(mode==='fall'?2:0))*100}%)`;if(mode==='fall'&&prefs.motion){node.style.opacity=0;node.getBoundingClientRect();}}
  node.className=`rt-piece ${p.kind}${p.kind==='rocket'&&p.axis==='v'?' vertical':''}${p.hp>1?' layered':''}`;node.style.opacity='1';node.style.transform=`translate(${i%8*100}%,${Math.floor(i/8)*100}%)`;node.dataset.index=i;
  if(mode==='create')animate(node,[{scale:.35},{scale:1.2,offset:.7},{scale:1}],{duration:260,easing:'ease-out'});
 });
 for(const [id,node]of cache)if(!seen.has(id)){node.remove();cache.delete(id);}
 qa('.rt-cell').forEach((b,i)=>{const p=view.board[i];b.setAttribute('aria-label',p?`${names[p.kind]}, row ${Math.floor(i/8)+1}, column ${i%8+1}${p.hp?`, ${p.hp} layer${p.hp>1?'s':''}`:''}`:'Empty space');b.classList.toggle('selected',i===selected);});
 qa('.rt-tool').forEach(b=>{b.querySelector('.rt-count').textContent=view.tools[b.dataset.tool];b.setAttribute('aria-pressed',String(armed===b.dataset.tool));b.disabled=!view.tools[b.dataset.tool]||busy||view.status!=='playing';});
}
for(let i=0;i<64;i++){const b=document.createElement('button');b.type='button';b.className='rt-cell';b.dataset.index=i;b.addEventListener('click',()=>choose(i));q('#rt-cells').appendChild(b);}
const toolInfo=[['hammer','hammer','Hammer'],['row','rocket','Row'],['column','rocket','Column'],['shuffle','orb','Mix']];
q('#rt-tool-row').innerHTML=toolInfo.map(([tool,icon,label])=>`<button class="rt-tool" type="button" data-tool="${tool}" aria-label="${label} booster" aria-pressed="false"><span class="rt-tool-socket">${img(icon,tool==='column'?'rt-column-icon':'')}<span class="rt-count">${view.tools[tool]}</span></span>${label}</button>`).join('');
q('.rt-column-icon').style.transform='rotate(90deg)';
qa('.rt-tool').forEach(b=>b.addEventListener('click',async()=>{if(busy||paused||view.status!=='playing')return;armed=armed===b.dataset.tool?null:b.dataset.tool;selected=-1;render(view);if(armed==='shuffle'){const result=engine.useTool('shuffle',0);armed=null;await play(result);}else say(armed?'Choose a tile. This booster uses no move.':'Swap matching pieces. Tap a power-up to blast.');}));
let pointer=null,suppressClick=false;
q('#rt-cells').addEventListener('pointerdown',e=>{const cell=e.target.closest('.rt-cell');if(!cell||busy||paused)return;pointer={i:+cell.dataset.index,x:e.clientX,y:e.clientY};});
q('#rt-cells').addEventListener('pointerup',e=>{if(!pointer)return;const p=pointer;pointer=null;const dx=e.clientX-p.x,dy=e.clientY-p.y;if(Math.hypot(dx,dy)<14)return;const to=p.i+(Math.abs(dx)>Math.abs(dy)?Math.sign(dx):Math.sign(dy)*8);if(to>=0&&to<64&&engine.adjacent(p.i,to)){suppressClick=true;choosePair(p.i,to);setTimeout(()=>suppressClick=false,100);}});
q('#rt-cells').addEventListener('pointercancel',()=>pointer=null);
q('#rt-cells').addEventListener('pointermove',e=>{if(!prefs.motion||paused)return;const r=board.getBoundingClientRect(),lean=Math.max(-2,Math.min(2,((e.clientX-r.left)/r.width-.5)*4));q('#rt-king').style.transform=`rotate(${lean}deg)`;});
q('#rt-cells').addEventListener('pointerleave',()=>q('#rt-king').style.transform='rotate(0deg)');
q('#rt-cells').addEventListener('keydown',e=>{if(!e.key.startsWith('Arrow'))return;const i=+e.target.dataset.index,n=i+({ArrowUp:-8,ArrowDown:8,ArrowLeft:-1,ArrowRight:1}[e.key]||0);if(n>=0&&n<64&&engine.adjacent(i,n)){e.preventDefault();qa('.rt-cell')[n].focus();}});
function choose(i){if(suppressClick||busy||paused||view.status!=='playing')return;clearHints();if(armed){const result=engine.useTool(armed,i);armed=null;play(result);return;}if(selected>=0&&selected!==i&&engine.adjacent(selected,i)){choosePair(selected,i);return;}if(engine.power(engine.board[i])){choosePair(i,i);return;}if(engine.blocker(engine.board[i])){say('Match beside blockers or use a blast to clear them.');return;}selected=selected===i?-1:i;render(view);}
async function choosePair(a,b){if(busy||paused||view.status!=='playing')return;const t=performance.now();const result=engine.act(a,b);metrics.engineMs.push(performance.now()-t);selected=-1;await play(result);}
function clearHints(){clearTimeout(hintTimer);qa('.hinted').forEach(n=>n.classList.remove('hinted'));}
function scheduleHint(){clearHints();hintTimer=setTimeout(()=>{if(busy||paused||screen!=='game'||view.status!=='playing')return;const move=engine.legalMoves()[0];if(move)move.forEach(i=>qa('.rt-cell')[i].classList.add('hinted'));},6500);}
function wait(ms,token){return new Promise(resolve=>{let remaining=prefs.motion?ms:Math.min(ms,35),last=performance.now();function tick(){if(token!==epoch){resolve(false);return;}const now=performance.now();if(!paused)remaining-=now-last;last=now;if(remaining<=0)resolve(true);else setTimeout(tick,16);}tick();});}
async function play(result){
 if(!result.steps.length)return;busy=true;clearHints();const token=epoch;root.dataset.busy='true';qa('.rt-tool').forEach(b=>b.disabled=true);metrics.actions++;
 for(const step of result.steps){if(token!==epoch)return;
  if(step.type==='swap'){render(step);if(!await wait(145,token))return;}
  else if(step.type==='invalid'){render(step);say('Try a match of three or more. Your move is safe.');react('think','A different move could be splendid.',900);await wait(170,token);}
  else if(step.type==='spend'){render(step);}
  else if(step.type==='match'){
   step.cells.forEach(i=>{const node=pieces.querySelector(`[data-index="${i}"]`);if(node)animate(node.querySelector('img'),[{scale:1},{scale:1.15},{scale:.8}],{duration:180});});
   if(step.cascade>0){word(step.cascade>1?'Magnificent!':'Splendid!');react('cheer','What a wonderful cascade!',1600);}await wait(145,token);
  }else if(step.type==='transform'){
   render(step,'create');fx.effects(step.effects);react('cheer','A little royal magic!',1800);await wait(450,token);
  }else if(step.type==='blast'){
   const big=step.combo||step.cells.length>15;react(big?'cheer':'idle',big?'Now that is a royal combination!':'A splendid shot!',1800);if(step.combo)word('Royal combo!');
   for(const effect of step.effects){const node=pieces.querySelector(`[data-index="${effect.i}"]`);if(node)animate(node,[{scale:1},{scale:1.26},{scale:.9}],{duration:170});}
   await wait(130,token);fx.effects(step.effects);sound(step.combo?'combo':'blast');
   if(big)animate(q('.rt-board-frame'),[{transform:'translateX(0)'},{transform:'translateX(-3px)',offset:.25},{transform:'translateX(3px)',offset:.5},{transform:'translateX(-1px)',offset:.75},{transform:'translateX(0)'}],{duration:210});
   await wait(step.effects.some(e=>e.kind==='helicopter')?860:340,token);
  }else if(step.type==='clear'){
   for(const item of step.removed)fx.pop(item.i,item.p.kind,['crate','vase'].includes(item.p.kind));
   step.collected.slice(0,8).forEach((item,n)=>flyGoal(item,n*35));render(step);sound('match');await wait(110,token);
  }else if(step.type==='create'){render(step,'create');step.creations.forEach(c=>fx.ring(c.i,'#fff2ab'));sound('create');await wait(200,token);}
  else if(step.type==='fall'){render(step,'fall');await wait(270,token);}
  else if(step.type==='shuffle'){say('A fresh arrangement. Your moves are safe.');render(step,'fall');await wait(350,token);}
  else if(step.type==='end'){render(step);}
 }
 if(token!==epoch)return;busy=false;root.dataset.busy='false';render(engine.snapshot());
 if(view.status==='won'){react('cheer','The gardens are glorious!');word('Glorious!');await wait(550,token);if(token!==epoch)return;celebrate(false);}
 else if(view.status==='lost'){react('think','A fresh approach, Your Majesty.',0);showScreen('lost',false);}
 else{if(result.accepted)say('Swap matching pieces. Tap a power-up to blast.');if(view.moves<=5)react('think','You have this. Look for a combination.',0);scheduleHint();}
}
function flyGoal(item,delay){if(!prefs.motion)return;const target=q(`[data-goal="${item.kind}"]`);if(!target)return;const pr=phone.getBoundingClientRect(),br=board.getBoundingClientRect(),tr=target.getBoundingClientRect(),cell=br.width/8;
 const x=br.left-pr.left+(item.i%8+.5)*cell-15,y=br.top-pr.top+(Math.floor(item.i/8)+.5)*cell-15,tx=tr.left-pr.left+tr.width/2-15,ty=tr.top-pr.top+15;
 const el=new Image();el.src=assets[item.kind];el.className='rt-fly';el.style.left=x+'px';el.style.top=y+'px';phone.appendChild(el);
 const a=animate(el,[{transform:'translate(0,0) scale(1)',opacity:1},{transform:`translate(${(tx-x)*.55+25}px,${(ty-y)*.5-25}px) scale(1.1)`,offset:.5},{transform:`translate(${tx-x}px,${ty-y}px) scale(.4)`,opacity:.4}],{duration:620,delay,easing:'cubic-bezier(.35,0,.6,1)',fill:'both'});if(a)a.finished.catch(()=>{}).finally(()=>el.remove());else el.remove();}
let audioCtx;
function sound(kind){if(!prefs.sound)return;try{audioCtx=audioCtx||new (window.AudioContext||window.webkitAudioContext)();if(audioCtx.state==='suspended')audioCtx.resume();const t=audioCtx.currentTime;for(let k=0;k<(kind==='combo'?3:1);k++){const osc=audioCtx.createOscillator(),gain=audioCtx.createGain();osc.type=kind==='blast'?'triangle':'sine';const hz=kind==='blast'?160:kind==='create'?620:kind==='combo'?330*(k+1):480;osc.frequency.setValueAtTime(hz,t+k*.065);osc.frequency.exponentialRampToValueAtTime(hz*.55,t+k*.065+.17);gain.gain.setValueAtTime(.045,t+k*.065);gain.gain.exponentialRampToValueAtTime(.001,t+k*.065+.22);osc.connect(gain);gain.connect(audioCtx.destination);osc.start(t+k*.065);osc.stop(t+k*.065+.25);}}catch{} }
const fx=(()=>{
 const canvas=q('#rt-fx'),ctx=canvas.getContext('2d'),particles=[],effects=[];let frame=0,last=0,w=0,dpr=1,time=0;
 const palette={ruby:'#ff5860',shield:'#5dccff',crown:'#ffd764',leaf:'#86e543',lily:'#d278ff',crate:'#d29b57',vase:'#fff3cc'};
 function resize(){w=board.clientWidth;dpr=Math.min(devicePixelRatio||1,2);canvas.width=Math.round(w*dpr);canvas.height=canvas.width;ctx.setTransform(dpr,0,0,dpr,0,0);}
 new ResizeObserver(resize).observe(board);resize();
 function center(i){return{x:(i%8+.5)*w/8,y:(Math.floor(i/8)+.5)*w/8};}
 function start(){if(!frame){last=performance.now();frame=requestAnimationFrame(draw);}}
 function push(e){if(!prefs.motion)return;effects.push({...e,start:time});start();}
 function particle(p){if(!prefs.motion||particles.length>=160)return;particles.push({...p,start:time});metrics.peakParticles=Math.max(metrics.peakParticles,particles.length);start();}
 function pop(i,kind,block){if(!prefs.motion)return;const {x,y}=center(i),count=block?8:5;for(let k=0;k<count;k++){const a=k/count*Math.PI*2;particle({x,y,vx:Math.cos(a)*(35+Math.random()*65),vy:Math.sin(a)*60-50,life:350+Math.random()*200,color:palette[kind]||'#fff1a4',size:block?4:2.5,chip:block});}push({kind:'ring',i,color:palette[kind]||'#fff4bc',duration:230});}
 function ring(i,color){push({kind:'ring',i,color,duration:360});}
 function addEffects(items){for(const e of items){metrics.effects.push(e.kind);push({...e,duration:e.kind==='helicopter'?900:e.kind==='transform'?600:550});if(['tnt','big-tnt','cross3','double-orb'].includes(e.kind))for(let n=0;n<24;n++){const p=center(e.i),a=n/24*Math.PI*2;particle({x:p.x,y:p.y,vx:Math.cos(a)*150,vy:Math.sin(a)*140,life:400,color:n%2?'#ffe589':'#ffb56c',size:3});}}}
 function line(x1,y1,x2,y2,alpha,width){ctx.save();ctx.globalAlpha=alpha;ctx.lineCap='round';ctx.shadowColor='#ffe488';ctx.shadowBlur=14;ctx.strokeStyle='#ffc54c';ctx.lineWidth=width;ctx.beginPath();ctx.moveTo(x1,y1);ctx.lineTo(x2,y2);ctx.stroke();ctx.shadowBlur=0;ctx.lineWidth=Math.max(2,width*.32);ctx.strokeStyle='#fffbe3';ctx.stroke();ctx.restore();}
 function blast(e,p,t){const axis=e.axis||'h',reach=w*Math.min(1,t*3),a=Math.max(0,1-t);if(e.kind==='rocket'||e.kind==='row'||e.kind==='column'){const horizontal=e.kind==='row'||(e.kind!=='column'&&axis==='h');if(horizontal)line(Math.max(0,p.x-reach),p.y,Math.min(w,p.x+reach),p.y,a,14*(1-t)+2);else line(p.x,Math.max(0,p.y-reach),p.x,Math.min(w,p.y+reach),a,14*(1-t)+2);}
  else if(e.kind==='cross'||e.kind==='cross3'){const bands=e.kind==='cross3'?[-1,0,1]:[0];for(const offset of bands){line(0,p.y+offset*w/8,w,p.y+offset*w/8,a,12);line(p.x+offset*w/8,0,p.x+offset*w/8,w,a,12);}}
  else if(e.kind==='tnt'||e.kind==='big-tnt'||e.kind==='hammer'){const radius=(e.kind==='hammer'?.55:e.kind==='big-tnt'?4.4:2.7)*w/8;ctx.save();ctx.globalAlpha=Math.max(0,.75-t);const r=Math.max(1,radius*t);const grad=ctx.createRadialGradient(p.x,p.y,0,p.x,p.y,r);grad.addColorStop(0,'#fff8d8');grad.addColorStop(.4,'#ffe082');grad.addColorStop(.75,'#ff9b483d');grad.addColorStop(1,'#ffcd6400');ctx.fillStyle=grad;ctx.beginPath();ctx.arc(p.x,p.y,r,0,Math.PI*2);ctx.fill();ctx.globalAlpha=a;ctx.strokeStyle='#ffe08a';ctx.lineWidth=7*(1-t)+1;ctx.beginPath();ctx.arc(p.x,p.y,r,0,Math.PI*2);ctx.stroke();ctx.restore();}
  else if(e.kind==='orb'||e.kind==='transform'||e.kind==='double-orb'){const targets=e.targets||Array.from({length:12},(_,n)=>(n*5+3)%64);for(const i of targets){const target=center(i);line(p.x,p.y,p.x+(target.x-p.x)*Math.min(1,t*2),p.y+(target.y-p.y)*Math.min(1,t*2),a*.75,4);}ringDraw(p,t,'#dca2ff',w*.6);}
 }
 function ringDraw(p,t,color,max){ctx.save();ctx.globalAlpha=1-t;ctx.strokeStyle=color;ctx.lineWidth=3*(1-t)+1;ctx.beginPath();ctx.arc(p.x,p.y,Math.max(1,max*t),0,Math.PI*2);ctx.stroke();ctx.restore();}
 function draw(now){frame=0;const rawDt=now-last,dt=Math.min(50,rawDt);last=now;if(!paused){time+=dt;metrics.frames.push(rawDt);if(metrics.frames.length>5000)metrics.frames.shift();}ctx.clearRect(0,0,w,w);
  for(let n=effects.length-1;n>=0;n--){const e=effects[n],t=(time-e.start)/e.duration,p=center(e.i);if(t>=1){effects.splice(n,1);continue;}if(e.kind==='ring')ringDraw(p,t,e.color,w/9);
   else if(e.kind==='helicopter'){
    const targets=e.targets||[];for(const targetIndex of targets){const target=center(targetIndex),u=Math.min(1,Math.max(0,(t-.17)/.65)),x=p.x+(target.x-p.x)*u,y=p.y+(target.y-p.y)*u-Math.sin(u*Math.PI)*w*.16;
     ctx.save();ctx.globalAlpha=t>.85?Math.max(0,(1-t)/.15):1;line(p.x,p.y,x,y,.17,3);ctx.translate(x,y);ctx.rotate(Math.sin(t*13)*.06);const size=w/7;const art=images.helicopter;if(art.complete&&art.naturalWidth)ctx.drawImage(art,-size/2,-size/2,size,size);ctx.strokeStyle='#fff2bacc';ctx.lineWidth=3;ctx.beginPath();ctx.ellipse(0,-size*.32,size*.36,3,Math.sin(t*60)*.1,0,Math.PI*2);ctx.stroke();if(e.payload&&images[e.payload.kind]?.complete)ctx.drawImage(images[e.payload.kind],-size*.18,size*.14,size*.45,size*.45);ctx.restore();
     if(t>.77){const impact={...e.payload,kind:e.payload?.kind||'tnt'};blast(impact,target,(t-.77)/.23);}
    }
   }else blast(e,p,t);
  }
  for(let n=particles.length-1;n>=0;n--){const p=particles[n],age=(time-p.start)/1000,t=age/(p.life/1000);if(t>=1){particles.splice(n,1);continue;}ctx.save();ctx.globalAlpha=1-t;ctx.fillStyle=p.color;ctx.translate(p.x+p.vx*age,p.y+p.vy*age+120*age*age);ctx.rotate(age*9);if(p.chip)ctx.fillRect(-p.size,-p.size/2,p.size*2,p.size);else{ctx.beginPath();ctx.arc(0,0,p.size*(1-t*.5),0,Math.PI*2);ctx.fill();}ctx.restore();}
  if(effects.length||particles.length)frame=requestAnimationFrame(draw);
 }
 function clear(){effects.length=0;particles.length=0;if(frame)cancelAnimationFrame(frame);frame=0;ctx.clearRect(0,0,w,w);}
 function pause(value){if(value&&frame){cancelAnimationFrame(frame);frame=0;}else if(!value&&(effects.length||particles.length))start();}
 return{pop,ring,effects:addEffects,clear,pause,get active(){return effects.length+particles.length;}};
})();
function pauseAnimations(value){paused=value;animations.forEach(a=>value?a.pause():a.play());fx.pause(value);}
function closeModal(){if(reviewOutcome&&['won','lost'].includes(screen))render(engine.snapshot());q('#rt-modal').hidden=true;pauseAnimations(false);screen='game';q('#rt-screen').value='game';q('#rt-close').hidden=false;previousFocus?.focus();if(!busy)react(view.moves<=5?'think':'idle',view.moves<=5?'You have this. Look for a combination.':'A splendid day for a puzzle!',0);scheduleHint();}
const goalsMarkup=(start=false)=>`<div class="rt-modal-goals">${Object.entries(start?{crate:12,vase:4}:view.goals).map(([kind,count])=>`<span>${img(kind)}${count===0?'✓':count}</span>`).join('')}</div>`;
function modalAction(label,fn,secondary=false){const b=document.createElement('button');b.type='button';b.className=secondary?'rt-secondary':'rt-cta';b.textContent=label;b.addEventListener('click',fn);q('#rt-dialog-content').appendChild(b);return b;}
function showScreen(next,review=true){
 if(screen==='celebrate'&&next!=='celebrate'){busy=false;root.dataset.busy='false';fx.clear();render(engine.snapshot());}
 if(reviewOutcome&&['won','lost'].includes(screen))render(engine.snapshot());
 if(next==='celebrate'){celebrate(true);return;}
 celebrationToken++;q('#rt-skip').hidden=true;
 if(next==='game'){closeModal();return;}clearHints();previousFocus=document.activeElement;screen=next;reviewOutcome=review;pauseAnimations(true);q('#rt-screen').value=next;q('#rt-modal').hidden=false;q('#rt-close').hidden=!review&&['won','lost'].includes(next);
 if(review&&next==='won')render({...engine.snapshot(),goals:{crate:0,vase:0},status:'won'});
 if(review&&next==='lost'){render({...engine.snapshot(),moves:0,status:'lost'});react('think','A fresh approach, Your Majesty.',0);}
 const container=q('#rt-dialog-content');let body='';
 if(next==='start'||next==='goals'){
  body=`<h2 id="rt-dialog-title">${next==='start'?'Level 12':'A royal challenge'}</h2><p class="rt-dialog-sub">THE ROYAL GARDENS · 24 MOVES</p><div class="rt-dialog-ivory"><h3>Clear the gardens</h3>${goalsMarkup(true)}${next==='start'?'<h3>Choose your boosters</h3><div class="rt-picks">'+['rocket','tnt','orb'].map(k=>`<button type="button" class="rt-pick" data-pick="${k}" aria-label="Start with ${names[k]}" aria-pressed="${chosenBoosts.has(k)}">${img(k)}<span>${chosenBoosts.has(k)?'✓':'+'}</span></button>`).join('')+'</div><p>Make a strong first move.</p>':'<p>Match beside crates and vases.<br>Combine power-ups for a bigger blast.</p>'}</div>`;
  container.innerHTML=body;qa('[data-pick]').forEach(b=>b.addEventListener('click',()=>{const k=b.dataset.pick;if(chosenBoosts.has(k))chosenBoosts.delete(k);else chosenBoosts.add(k);b.setAttribute('aria-pressed',String(chosenBoosts.has(k)));b.querySelector('span').textContent=chosenBoosts.has(k)?'✓':'+';}));
  modalAction(next==='start'?'Play':'Let’s play',()=>{if(next==='start'){reset('free');let i=18;for(const kind of chosenBoosts)engine.board[i++]=engine.piece(kind);render(engine.snapshot());showScreen('goals');}else{closeModal();react('idle','Let us make the gardens shine!',1300);}});
 }else if(next==='pause'){
  container.innerHTML=`<h2 id="rt-dialog-title">Take a moment</h2><div class="rt-dialog-ivory">${img('king-idle','rt-modal-king')}<label class="rt-toggle">Sound effects<input id="rt-sound-toggle" type="checkbox" ${prefs.sound?'checked':''}></label><label class="rt-toggle">Gentle motion<input id="rt-motion-toggle" type="checkbox" ${!prefs.motion?'checked':''}></label></div>`;
  q('#rt-sound-toggle').onchange=e=>{prefs.sound=e.target.checked;if(prefs.sound)sound('create');};q('#rt-motion-toggle').onchange=e=>{prefs.motion=!e.target.checked;applyPrefs();};modalAction('Continue',closeModal);modalAction('Restart level',()=>{reset('free');showScreen('start');},true);
 }else if(next==='won'){
  react('cheer','A triumph fit for a king!',0);container.innerHTML=`<h2 id="rt-dialog-title">Gloriously done!</h2><div class="rt-dialog-ivory">${img('king-cheer','rt-modal-king')}<div>${img('icon-star','rt-win-star')}</div><h3>The gardens are restored</h3><p>One more star for your kingdom.</p></div>`;modalAction('Continue',()=>{closeModal();showRewardHome();});const star=q('.rt-win-star');animate(star,[{transform:'scale(.2) rotate(-35deg)'},{transform:'scale(1.22) rotate(10deg)',offset:.65},{transform:'scale(1) rotate(0)'}],{duration:700,easing:'ease-out'});
 }else if(next==='lost'){
  container.innerHTML=`<h2 id="rt-dialog-title">So close!</h2><div class="rt-dialog-ivory">${img('king-think','rt-modal-king')}<h3>Out of moves</h3>${goalsMarkup()}<p>A new approach can make all the difference.</p></div>`;modalAction('Try again',()=>{reset('free');showScreen('start');});modalAction('Review the board',()=>{closeModal();say('Your last board. Restart when you are ready.');},true);
 }else if(next==='guide'){
  const rows=[['rocket','4 in a line','Clears a row or column. Its fins show the direction.'],['tnt','5 in a T or L','Blasts a 5 × 5 area around it.'],['helicopter','4 in a square','Clears neighbors, then flies to an unfinished goal.'],['orb','5 in a line','Clears one color. Combine it to multiply power-ups.']];
  container.innerHTML=`<h2 id="rt-dialog-title">Royal power</h2><div class="rt-dialog-ivory">${rows.map(([k,rule,desc])=>`<div class="rt-guide-row">${img(k)}<div><strong>${names[k]} · ${rule}</strong><p>${desc}</p></div></div>`).join('')}<p>Tap a power-up, or swap two together.<br>One combination uses one move.</p></div>`;modalAction('Wonderful',closeModal);
 }else if(next==='assets'){
  container.innerHTML=`<h2 id="rt-dialog-title">Royal collection</h2><div class="rt-dialog-ivory"><div class="rt-asset-grid">${Object.keys(names).map(k=>`<div>${img(k)}${names[k]}</div>`).join('')}</div></div>`;modalAction('Back to play',closeModal);
 }
 if(prefs.motion){const a=animate(q('.rt-dialog'),[{transform:'translateY(24px) scale(.92)',opacity:0},{transform:'translateY(0) scale(1)',opacity:1}],{duration:240,easing:'cubic-bezier(.2,.9,.2,1)'});if(a)a.play();}
 const target=container.querySelector('.rt-cta')||q('#rt-close');target.focus();
}
async function celebrate(review=true){
 const token=++celebrationToken;epoch++;busy=true;root.dataset.busy='true';clearHints();q('#rt-modal').hidden=true;pauseAnimations(false);screen='celebrate';q('#rt-screen').value='celebrate';q('#rt-skip').hidden=false;
 const finish=()=>{if(token!==celebrationToken)return;celebrationToken++;fx.clear();busy=false;root.dataset.busy='false';q('#rt-skip').hidden=true;render(engine.snapshot());showScreen('won',review);};q('#rt-skip').onclick=finish;
 react('cheer','A triumph fit for a king!',0);word('Glorious!');say('A royal finish for a royal victory.');
 const sequence=[{kind:'rocket',i:17,axis:'h'},{kind:'tnt',i:43},{kind:'rocket',i:38,axis:'v'},{kind:'cross',i:28}];
 for(const effect of sequence){if(token!==celebrationToken)return;fx.effects([effect]);sound('blast');await new Promise(r=>setTimeout(r,prefs.motion?380:65));}
 await new Promise(r=>setTimeout(r,prefs.motion?650:100));finish();
}
function showRewardHome(){
 pauseAnimations(true);screen='reward';q('#rt-modal').hidden=false;q('#rt-close').hidden=false;const c=q('#rt-dialog-content');c.innerHTML=`<h2 id="rt-dialog-title">Your kingdom grows</h2><div class="rt-dialog-ivory">${img('logo-locked','rt-reward-logo')}<div class="rt-home-reward">${img('home-castle')}<span id="rt-reward-counter">★ 1</span>${img('icon-star','rt-reward-star')}</div><h3>The Royal Gardens</h3><p>A star toward your next restoration.</p></div>`;q('.rt-reward-logo').style.cssText='width:110px;height:72px;object-fit:contain';q('.rt-home-reward').style.cssText='position:relative;height:250px;border-radius:12px;overflow:hidden';q('.rt-home-reward>img').style.cssText='width:100%;height:100%;object-fit:cover;object-position:center';q('#rt-reward-counter').style.cssText='position:absolute;top:10px;right:12px;background:#fff1c7;border:2px solid #d6a037;color:#163b75;padding:5px 12px;border-radius:18px;font-weight:bold';const star=q('.rt-reward-star');star.style.cssText='position:absolute;width:50px;height:50px;top:125px;left:44%;object-fit:contain';const a=animate(star,[{transform:'translate(0,0) scale(1.8)',opacity:0},{transform:'translate(0,-15px) scale(1.2)',opacity:1,offset:.2},{transform:'translate(70px,-112px) scale(.3)',opacity:1,offset:.85},{transform:'translate(70px,-112px) scale(.1)',opacity:0}],{duration:1100,fill:'forwards'});if(a)a.play();modalAction('Play another puzzle',()=>{reset('free');showScreen('start');});
}
function reset(demo='free'){
 epoch++;celebrationToken++;q('#rt-skip').hidden=true;paused=false;busy=false;root.dataset.busy='false';selected=-1;armed=null;clearTimeout(kingTimer);clearTimeout(wordTimer);clearHints();animations.forEach(a=>a.cancel());animations.clear();fx.clear();qa('.rt-fly').forEach(e=>e.remove());q('#rt-burst-word').classList.remove('show');q('#rt-modal').hidden=true;q('#rt-close').hidden=false;screen='game';q('#rt-screen').value='game';q('#rt-demo').value=demo;seed+=17;engine.seed=seed;const setup=engine.scenario(demo);render(engine.snapshot());applyPrefs();
 if(setup.focus.length){setup.focus.forEach(i=>qa('.rt-cell')[i].classList.add('hinted'));say(setup.focus.length===1?'Tap the highlighted power-up.':'Swap the two highlighted power-ups together.');react('idle','Shall we try a little royal power?',0);}
 else{say('Swap matching pieces. Tap a power-up to blast.');react(demo==='low-moves'?'think':'idle',demo==='low-moves'?'You have this. Look for a combination.':'A splendid day for a puzzle!',0);scheduleHint();}
}
q('#rt-screen').onchange=e=>showScreen(e.target.value);q('#rt-demo').onchange=e=>reset(e.target.value);q('#rt-reset').onclick=()=>reset(q('#rt-demo').value);q('#rt-pause').onclick=()=>showScreen('pause');q('#rt-help').onclick=()=>showScreen('guide');q('#rt-close').onclick=closeModal;q('#rt-king').onclick=()=>{if(!paused)react('cheer','At your service, Your Majesty!',1800);};
root.addEventListener('keydown',e=>{if(e.key==='Escape'){if(!q('#rt-modal').hidden&&!q('#rt-close').hidden)closeModal();else if(screen==='game')showScreen('pause');}if(e.key==='Tab'&&!q('#rt-modal').hidden){const buttons=[...q('#rt-modal').querySelectorAll('button,input,select')].filter(x=>!x.hidden&&!x.disabled&&x.getClientRects().length);const first=buttons[0],last=buttons[buttons.length-1];if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}}});
document.addEventListener('visibilitychange',()=>{if(document.hidden&&screen==='game'&&busy)showScreen('pause');});
if(globalThis.Tweak){const tweak=new Tweak({container:root,onChange:applyPrefs});tweak.addSelect(prefs,'boardTone',{label:'Board surface',options:[{label:'Royal blue',value:'blue'},{label:'Cool ivory',value:'ivory'}]});tweak.addToggle(prefs,'motion',{label:'Full motion'});tweak.addSlider(prefs,'kingScale',{label:'King size',min:.9,max:1.1,step:.05});}
root.royalPrototype={engine,metrics,get state(){return{busy,paused,screen,view:JSON.parse(JSON.stringify(view)),selected,fxActive:fx.active}},reset,showScreen,choosePair,useTool:async(tool,i)=>play(engine.useTool(tool,i)),setMotion:value=>{prefs.motion=value;applyPrefs();},render:()=>render(engine.snapshot()),whenIdle:async()=>{const start=performance.now();while(busy&&performance.now()-start<30000)await new Promise(r=>setTimeout(r,30));return!busy;}};
render(view);react('idle','A splendid day for a puzzle!',0);applyPrefs();scheduleHint();
})();
