'use strict';
const colours=['red','orange','purple','blue','green','yellow'];
const colourHex=['#ef4850','#f4972e','#a865e5','#3999e1','#69b846','#ebc745'];
const approvedSet='storybook-magic';
let activeStyle=approvedSet;
const pieceByColour=[0,1,2,3,4,5];
const art=(id,set=activeStyle)=>id.startsWith('piece-')?`art/${set}/${colours[Number(id.slice(6))]}.png`:`../asset-styles/art/bold/${id}.png`;
const pieceName=(index,set=activeStyle)=>itemSets.find(s=>s.id===set).items[index];
const cases=[{id:'score',name:'Reach the score',tag:'SCORE',text:'Earn the required points. A star represents score, with progress shown as points earned / needed.'},{id:'collect',name:'Collect pieces',tag:'COLOURED PIECES + SCORE',text:'Match the requested colours. The target icons use the same new pieces as the board.'},{id:'treasure',name:'Bring treasures down',tag:'TREASURES + SCORE',text:'Bring the blue locket and red ring to their column exits. Each treasure has its own count.'},{id:'ice',name:'Clear the ice',tag:'ICE / BLOCKS + SCORE',text:'Clear the required frozen cells. The target shows a piece under ice, matching the board.'}];
const powers=[
 {id:'moves',name:'Extra moves',effect:'+5 moves',description:'Adds five moves to the current level.',group:'During a level',type:'ExtraMoves'},
 {id:'glove',name:'Swap glove',description:'Swap adjacent pieces without spending a move.',group:'During a level',type:'Hand'},
 {id:'hammer',name:'Royal hammer',description:'Removes one chosen piece.',group:'During a level',type:'Bomb'},
 {id:'recolour',name:'Colour brush',description:'Recolours nearby normal pieces around the chosen spot.',group:'During a level',type:'Random_color'},
 {id:'rocket',name:'Rocket',description:'Clears a row or column. Available as a pre-level booster and a board special.',group:'Board / before a level',type:'Stripes'},
 {id:'area-bomb',name:'Area bomb',description:'Clears a nearby area. Available on the board and before a level.',group:'Board / before a level',type:'Packages'},
 {id:'rainbow-orb',name:'Rainbow ball',description:'Clears pieces of one colour. Also available before a level.',group:'Board / before a level',type:'Colorful_bomb'},
 {id:'time',name:'Extra time',effect:'+30 seconds',description:'Adds thirty seconds on timed levels. Replaces extra moves in the tray.',group:'Timed levels',type:'ExtraTime'}
];
let layout='compact-bar',phone='tall',progress=false,stress=false,timed=false,modal=null,opener;
const format=n=>n.toLocaleString('en-US');
function example(id){return targetExamples.find(e=>e.id===id);}
function spriteForTarget(t){if(t.type===0)return art('score');if(t.type===1)return art('piece-'+pieceByColour[t.color]);if(t.type===2)return art(t.iconGuid==='5b36c94fa40177248bd1894a25cbd3b8'?'ring':'locket');return art('piece-0');}
function nameForTarget(t){return t.type===0?'Score':t.type===1?'Collect '+pieceName(pieceByColour[t.color]).toLowerCase():t.type===2?(t.iconGuid==='5b36c94fa40177248bd1894a25cbd3b8'?'Bring down rings':'Bring down lockets'):'Clear ice';}
function currentGoal(t,index,single){if(!progress)return t.type===0&&single?`0 / ${format(t.count)}`:format(t.count);if(t.type===0)return single?`${format(Math.floor(t.count*.64))} / ${format(t.count)}`:format(Math.floor(t.count*.36));if((t.type===1&&t.color===1)||(t.type===2&&t.count===1))return '✓';return format(Math.max(1,Math.ceil(t.count*.45)));}
function targetMarkup(targets){const single=targets.length===1;return `<div class="targets-panel"><p class="targets-title">${single&&targets[0].type===0?'Score':'Targets'}</p><div class="target-slots ${single?'single':''}" style="--count:${targets.length}">${targets.map((t,i)=>{const text=currentGoal(t,i,single);return `<div class="goal-chip ${text==='✓'?'done':''}" title="${nameForTarget(t)}" aria-label="${nameForTarget(t)}: ${text==='✓'?'complete':text}"><div class="goal-icon ${t.type===0?'score-medal':t.type===3?'ice':''}"><img src="${spriteForTarget(t)}" alt="">${t.type===3?`<img class="ice-cover" src="${art('ice')}" alt="">`:''}</div><strong>${text}${t.type===0&&!single?'<small>pts</small>':''}</strong></div>`;}).join('')}</div>${single&&targets[0].type===0?`<div class="single-progress"><span style="width:${progress?64:0}%"></span></div>`:''}</div>`;}
function limitMarkup(e){return `<div class="moves"><small>${timed?'Time':'Moves'}</small><strong>${timed?(progress?'0:29':'0:45'):Math.max(1,e.moves-(progress?4:0))}</strong></div>`;}
function header(e,only=false){return `${only?'':`<div class="game-top"><span class="pause-icon" aria-hidden="true">Ⅱ</span><div class="level-label">${layout==='classic-cards'?'<small>CASTLE GATES</small>':''}Level ${e.level}</div><span class="gear-icon" aria-hidden="true">⚙</span></div>`}<div class="target-hud ${e.targets.length===4?'has-four':''}">${targetMarkup(e.targets)}${limitMarkup(e)}</div>`;}
function shapeOf(e){if(!stress)return e;return {cols:9,rows:11,grid:Array.from({length:11},(_,r)=>Array.from({length:9},(_,c)=>[[0,1],[0,7],[5,0],[5,4],[5,8],[10,1],[10,7]].some(p=>p[0]===r&&p[1]===c)?'00':'10'))};}
function boardMarkup(e){const s=shapeOf(e),valid=[];for(let r=0;r<s.rows;r++)for(let c=0;c<s.cols;c++)if(s.grid[r][c][0]!=='0')valid.push([r,c]);const specials=new Map(),middle=Math.floor(valid.length*.55);['rocket','area-bomb','rainbow-orb'].forEach((id,i)=>{const p=valid[Math.min(valid.length-1,middle+i*3)];if(p)specials.set(p.join(','),id);});const treasures=new Map();if(e.targets.some(t=>t.type===2)){const ts=e.targets.filter(t=>t.type===2);ts.forEach((t,i)=>{const p=valid[Math.min(valid.length-1,Math.floor(valid.length*.22)+i*4)];if(p)treasures.set(p.join(','),spriteForTarget(t));});}let html='';for(let r=0;r<s.rows;r++)for(let c=0;c<s.cols;c++){const cell=s.grid[r][c],missing=cell[0]==='0',n=(r*2+c*5+Math.floor(c/3)+Math.floor(r/2))%6,key=r+','+c;let src=art('piece-'+n),cls='',alt=pieceName(n);if(specials.has(key)){src=art(specials.get(key));cls='special-piece';alt=specials.get(key);}if(treasures.has(key)){src=treasures.get(key);cls='treasure-piece';alt='Treasure';}const frozen=e.id==='ice'&&(stress?(r===3||r===4)&&(c===3||c===4):cell[0]==='2'||cell[0]==='5');const exit=e.targets.some(t=>t.type===2)&&!missing&&!s.grid.slice(r+1).some(row=>row[c][0]!=='0');html+=`<div class="cell ${(r+c)%2?'checker':''} ${missing?'missing':''}">${missing?'':`<img class="piece ${cls}" src="${src}" alt="${alt}">${frozen?`<img class="ice-cover-board" src="${art('ice')}" alt="Ice layer">`:''}${exit?'<span class="ingredient-exit" aria-hidden="true">▼</span>':''}`}</div>`;}return html;}
function tray(){return `<div class="booster-area tool-tray"><p class="booster-label">A little royal help</p><div class="boosters">${[timed?'time':'moves','glove','hammer','recolour'].map((id,i)=>`<div class="booster" title="${powers.find(p=>p.id===id).name}"><img src="${art(id)}" alt="${powers.find(p=>p.id===id).name}">${i===0?`<em class="effect-tag">${timed?'+30s':'+5'}</em>`:''}<b>${[2,3,2,1][i]}</b></div>`).join('')}</div></div>`;}
function phoneMarkup(e,style){activeStyle=style;const s=shapeOf(e);return `<div class="phone"><div class="screen soft-storybook dynamic-screen ${layout}" data-style="${style}" data-case="${e.id}" data-cols="${s.cols}" data-rows="${s.rows}" role="img" aria-label="${e.id} target preview, level ${e.level}"><div class="scenery"></div><div class="top-zone">${header(e)}</div><div class="playfield"><div class="board-shell"><div class="board" style="--cols:${s.cols};--rows:${s.rows}">${boardMarkup(e)}</div></div></div>${tray()}<div class="home-indicator" aria-hidden="true"></div></div></div>`;}
function fitBoard(s){if(!s.isConnected||!s.getBoundingClientRect().width)return;const f=s.querySelector('.playfield'),shell=s.querySelector('.board-shell'),unit=s.getBoundingClientRect().width/393,cols=+s.dataset.cols,rows=+s.dataset.rows,pad=(layout==='floating-controls'?2:9)*unit,gap=(layout==='floating-controls'?2.5:1.7)*unit,edge=10*unit,cell=Math.max(0,Math.min(44*unit,(f.clientWidth-edge*2-pad*2-gap*(cols-1))/cols,(f.clientHeight-edge*2-pad*2-gap*(rows-1))/rows));shell.style.setProperty('--tile-size',cell+'px');shell.style.setProperty('--grid-gap',gap+'px');shell.style.setProperty('--shell-pad',pad+'px');shell.style.width=(cell*cols+gap*(cols-1)+pad*2)+'px';shell.style.height=(cell*rows+gap*(rows-1)+pad*2)+'px';}
const resize=new ResizeObserver(entries=>entries.forEach(entry=>fitBoard(entry.target.closest('.screen'))));function fitAll(){resize.disconnect();document.querySelectorAll('.dynamic-screen').forEach(s=>{resize.observe(s.querySelector('.playfield'));fitBoard(s);});}
let selectedSet=approvedSet,target='collect',smallIcons=false;
const getSet=id=>itemSets.find(s=>s.id===id);
function galleryMarkup(){
  return [...itemSets.filter(s=>s.id===approvedSet),...itemSets.filter(s=>s.id!==approvedSet)].map((s,i)=>`<article class="set-card ${s.id===selectedSet?'selected':''}" id="${s.id}">
    <div class="set-heading"><div><div class="card-heading"><span class="set-number">0${i+1}</span><h2>${s.name}</h2></div><p>${s.description}</p></div><button class="try-set" data-set="${s.id}" aria-pressed="${s.id===selectedSet}">${s.id===selectedSet?'On the board ✓':'Try on board ↗'}</button></div>
    <div class="set-items">${s.items.map((name,n)=>`<button class="item-card" data-item="${n}" data-collection="${s.id}" aria-label="Enlarge ${colours[n]} ${name}, ${s.name}"><span class="enlarge-mark" aria-hidden="true">↗</span><span class="item-art"><img src="${art('piece-'+n,s.id)}" alt=""></span><strong>${name}</strong><small><i class="colour-dot" style="--colour:${colourHex[n]}" aria-hidden="true"></i>${colours[n]}</small></button>`).join('')}</div>
    <p class="set-caption">${s.tag}<span>${s.id===approvedSet?'CHOSEN FOR THE GAME':'ALTERNATIVE COLLECTION'}</span></p>
  </article>`).join('');
}
function render(){
  document.body.classList.toggle('compact',phone==='compact');
  document.body.classList.toggle('small-icons',smallIcons);
  document.querySelector('#target').value=target;
  document.querySelector('#phone').value=phone;
  document.querySelector('#stress').checked=stress;
  document.querySelector('#small-icons').checked=smallIcons;
  document.querySelector('#set-gallery').innerHTML=galleryMarkup();
  document.querySelector('#board-title').textContent=getSet(selectedSet).name;
  document.querySelector('#board-phone').innerHTML=phoneMarkup(example(target),selectedSet);
  if(modal)renderModal();
  fitAll();
  document.querySelector('#announcement').textContent=getSet(selectedSet).name+' selected for the board.';
}
function renderModal(){
  const s=getSet(modal.set);
  if(modal.kind==='board'){
    document.querySelector('#detail-content').innerHTML=`<p class="eyebrow">SOFT STORYBOOK · BOARD PREVIEW</p><h2>${s.name}</h2><p>${s.description}</p><div class="detail-phone">${phoneMarkup(example(target),s.id)}</div>`;
  }else{
    const n=modal.item,name=s.items[n];
    document.querySelector('#detail-content').innerHTML=`<p class="eyebrow">${s.name.toUpperCase()}</p><h2>${name}</h2><img class="large-item" src="${art('piece-'+n,s.id)}" alt="${colours[n]} ${name}"><div class="detail-meta"><i class="colour-dot" style="--colour:${colourHex[n]}" aria-hidden="true"></i>${colours[n][0].toUpperCase()+colours[n].slice(1)} match piece · Bold Cartoon</div>`;
  }
}
const dialog=document.querySelector('#detail');
function openModal(next,trigger){opener=trigger;modal=next;renderModal();dialog.showModal();fitAll();}
document.addEventListener('click',e=>{
  const choose=e.target.closest('[data-set]'),item=e.target.closest('[data-item]');
  if(choose){
    selectedSet=choose.dataset.set;render();const button=document.querySelector(`[data-set="${selectedSet}"]`);
    if(matchMedia('(max-width:1000px)').matches)openModal({kind:'board',set:selectedSet},button);else button.focus({preventScroll:true});
  }else if(item){openModal({kind:'item',set:item.dataset.collection,item:Number(item.dataset.item)},item);}
});
document.querySelector('#target').onchange=e=>{target=e.target.value;render();};
document.querySelector('#phone').onchange=e=>{phone=e.target.value;render();};
document.querySelector('#stress').onchange=e=>{stress=e.target.checked;render();};
document.querySelector('#small-icons').onchange=e=>{smallIcons=e.target.checked;document.body.classList.toggle('small-icons',smallIcons);};
document.querySelector('#open-board').onclick=e=>openModal({kind:'board',set:selectedSet},e.currentTarget);
document.querySelector('.close-dialog').onclick=()=>dialog.close();
dialog.addEventListener('close',()=>{modal=null;document.querySelector('#detail-content').innerHTML='';fitAll();opener?.focus({preventScroll:true});});
dialog.addEventListener('click',e=>{if(e.target!==dialog)return;const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();});
render();document.fonts.ready.then(fitAll);
window.itemPreview={ready:true,sets:itemSets,select(options){if(options.set)selectedSet=options.set;if(options.target)target=options.target;if(options.phone)phone=options.phone;if(options.stress!==undefined)stress=options.stress;if(options.smallIcons!==undefined)smallIcons=options.smallIcons;render();},fitAll};
