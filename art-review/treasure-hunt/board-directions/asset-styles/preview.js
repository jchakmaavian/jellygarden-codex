'use strict';
let activeStyle='bold';
const art=(id,style=activeStyle)=>`art/${style}/${style==='bold'&&id==='piece-4'?'crown-v2':id}.png`;
const pieceByColour=[0,5,4,1,2,3],pieceNames=['Heart','Droplet','Leaf','Star','Butterfly','Gift'];
const pieceName=(index,style=activeStyle)=>style==='bold'&&index===4?'Crown':pieceNames[index];
const assetName=(asset,style)=>style==='bold'&&asset.id==='piece-4'?'Royal crown':asset.name;
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
const styles = [
  {id:'glossy',name:'Glossy Jewels',tag:'FACETED · BRIGHT · POLISHED',description:'Jewel-like pieces with polished tools and warm gold details.'},
  {id:'painted',name:'Painted Storybook',tag:'MATTE · WARM · HAND-PAINTED',description:'Rich brushwork and gentle illustrated shapes, like objects from Leo’s storybook.'},
  {id:'bold',name:'Bold Cartoon',tag:'CLEAR · COLOURFUL · EXPRESSIVE',description:'Your selected style, now with a purple royal crown in place of the butterfly.'}
];
let target = 'collect', libraryStyle = 'bold';
const pieceDescriptions = ['Red match piece','Blue match piece','Green match piece','Yellow match piece','Purple match piece','Orange match piece'];
const groups = [
  {name:'The six match pieces',note:'Six distinct colours. The selected Bold Cartoon set uses a purple crown.',items:pieceNames.map((name,i)=>({id:'piece-'+i,name,description:pieceDescriptions[i]}))},
  {name:'Tools & power-ups',note:'The same eight actions as the game.',items:powers},
  {name:'Treasures & targets',note:'Collectible goals share their artwork with the board.',items:[
    {id:'locket',name:'Blue locket',description:'Bring this treasure down to a column exit.'},
    {id:'ring',name:'Royal ring',description:'Bring this treasure down to a column exit.'},
    {id:'ice',name:'Ice layer',description:'A translucent frozen-cell overlay. The piece remains visible underneath.'},
    {id:'score',name:'Score medal',description:'Marks the points target; its blue medal separates it from the yellow match piece.'}
  ]}
];
const allAssets = groups.flatMap(g=>g.items);
function renderLibrary() {
  document.querySelector('#style-tabs').innerHTML = styles.map(s=>`<button data-library="${s.id}" aria-pressed="${s.id===libraryStyle}">${s.name}</button>`).join('');
  document.querySelector('#library').innerHTML = groups.map(g=>`<section class="library-group"><div class="library-heading"><h3>${g.name}</h3><p>${g.note}</p></div><div class="asset-grid">${g.items.map(a=>`<button class="asset-card" data-art="${a.id}" data-style="${libraryStyle}" aria-label="Enlarge ${assetName(a,libraryStyle)}, ${styles.find(s=>s.id===libraryStyle).name}"><i class="enlarge-mark" aria-hidden="true">↗</i><img class="${a.id==='ice'?'ice-swatch':''}" src="${art(a.id,libraryStyle)}" alt=""><strong>${assetName(a,libraryStyle)}</strong><small>${a.description}</small></button>`).join('')}</div></section>`).join('');
}
function render() {
  document.querySelector('#target').value=target;
  document.querySelector('#phone').value=phone;
  document.querySelector('#stress').checked=stress;
  document.querySelector('#timed').checked=timed;
  document.body.classList.toggle('compact',phone==='compact');
  document.body.classList.toggle('timed',timed);
  document.querySelector('#style-gallery').innerHTML=styles.map((s,i)=>`<article class="style-card" id="style-${s.id}"><div class="card-heading"><span class="number">0${i+1}</span><h2>${s.name}</h2>${s.id==='bold'?'<span class="pick">SELECTED</span>':''}</div>${phoneMarkup(example(target),s.id)}<div class="art-strip" aria-label="Six pieces in ${s.name}">${pieceNames.map((n,i)=>`<img src="${art('piece-'+i,s.id)}" alt="${pieceName(i,s.id)}">`).join('')}</div><div class="design-caption"><p class="mood">${s.tag}</p><p>${s.description}</p><button class="explore" data-phone-open="${s.id}">Enlarge mockup ↗</button></div></article>`).join('');
  const progressButton=document.querySelector('#progress');
  progressButton.setAttribute('aria-pressed',String(progress));
  progressButton.textContent=progress?'Reset progress':'Show progress';
  if(modal)renderModal();
  fitAll();
  document.querySelector('#announcement').textContent=`Showing ${target} targets with ${phone==='compact'?'compact':'tall'} phones. ${progress?'Progress shown.':''}`;
}
function renderModal() {
  const style=styles.find(s=>s.id===modal.style);
  if(modal.kind==='phone') {
    document.querySelector('#detail-content').innerHTML=`<p class="eyebrow">ASSET STYLE PREVIEW</p><h2>${style.name}</h2><p>${style.description}</p><div class="detail-phone">${phoneMarkup(example(target),style.id)}</div>`;
  } else {
    const a=allAssets.find(a=>a.id===modal.id);
    document.querySelector('#detail-content').innerHTML=`<p class="eyebrow">${style.name.toUpperCase()}</p><h2>${assetName(a,style.id)}</h2><img class="large-power ${a.id==='ice'?'ice-swatch':''}" src="${art(a.id,style.id)}" alt="${assetName(a,style.id)}"><p>${a.description}</p>`;
  }
}
document.querySelector('#target').onchange=e=>{target=e.target.value;render();};
document.querySelector('#phone').onchange=e=>{phone=e.target.value;render();};
document.querySelector('#progress').onclick=()=>{progress=!progress;render();};
document.querySelector('#stress').onchange=e=>{stress=e.target.checked;render();};
document.querySelector('#timed').onchange=e=>{timed=e.target.checked;render();};
const dialog=document.querySelector('#detail');
document.addEventListener('click',e=>{
  const tab=e.target.closest('[data-library]');
  if(tab){libraryStyle=tab.dataset.library;renderLibrary();document.querySelector(`[data-library="${libraryStyle}"]`).focus();return;}
  const p=e.target.closest('[data-phone-open]'),a=e.target.closest('[data-art]');
  if(!p&&!a)return;
  opener=p||a;
  modal=p?{kind:'phone',style:p.dataset.phoneOpen}:{kind:'art',style:a.dataset.style,id:a.dataset.art};
  renderModal();dialog.showModal();fitAll();
});
document.querySelector('.close-dialog').onclick=()=>dialog.close();
dialog.addEventListener('close',()=>{modal=null;document.querySelector('#detail-content').innerHTML='';fitAll();opener?.focus();});
dialog.addEventListener('click',e=>{if(e.target!==dialog)return;const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();});
render();renderLibrary();document.fonts.ready.then(fitAll);
window.assetPreview={ready:true,styles,allAssets,select(options){if(options.target)target=options.target;if(options.phone)phone=options.phone;if(options.progress!==undefined)progress=options.progress;if(options.stress!==undefined)stress=options.stress;if(options.timed!==undefined)timed=options.timed;if(options.libraryStyle){libraryStyle=options.libraryStyle;renderLibrary();}render();},fitAll};
