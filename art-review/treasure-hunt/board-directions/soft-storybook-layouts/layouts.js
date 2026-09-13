'use strict';
const project='../../../../';
const ui=project+'Assets/JellyGarden/Resources/Story/TreasureHunt/UI/Layers/';
const boosters=project+'art-review/king-leo-board/candidates/v1/';
const pieces='../v1/art/soft-storybook/';
const pieceNames=['Heart','Droplet','Leaf','Star','Butterfly','Gift'];
const directions=[
 {id:'classic-cards',name:'Classic Cards',mood:'CLOSEST TO YOUR CHOSEN DESIGN',description:'The familiar target card, circular moves counter and lilac booster tray. The character strip becomes space for the board.',detail:'The original feeling, with a protected centre.'},
 {id:'compact-bar',name:'Compact Bar',mood:'MORE ROOM FOR TALL BOARDS',description:'A slimmer score bar and shorter booster dock give tall boards more height, especially on smaller phones.',detail:'My pick: the best balance of space and storybook charm.'},
 {id:'floating-controls',name:'Floating Controls',mood:'LIGHTER, WITH LESS FRAMING',description:'Small floating counters and individual booster buttons. The tiles follow the board shape without a surrounding tray.',detail:'An airy alternative with the board shape in focus.'}
];
// Real board outlines from Resources/Levels/{1,3,4}.txt. Obstacles and token colours are illustrative.
const boards={
 short:{name:'Short',level:1,cols:9,rows:5,moves:20,target:100,holes:[]},
 medium:{name:'Medium',level:3,cols:8,rows:7,moves:24,target:500,holes:[[0,0],[0,7],[6,0],[6,7]]},
 tall:{name:'Tall',level:4,cols:9,rows:11,moves:36,target:1000,holes:[[0,1],[0,7],[5,0],[5,4],[5,8],[10,1],[10,7]]}
};
let shape='tall',phone='tall',enlargedIndex=0,opener;
const star=`<img class="star-icon" src="${ui}victory-star.png" alt="">`;
function levelHeader(b,sub){return `<div class="game-top"><span class="pause-icon" aria-hidden="true">Ⅱ</span><div class="level-label">${sub?'<small>CASTLE GATES</small>':''}Level ${b.level}</div><span class="gear-icon" aria-hidden="true">⚙</span></div>`;}
function moves(b){return `<div class="moves"><small>Moves</small><strong>${b.moves}</strong></div>`;}
function score(b){return `<div class="goal-title">Target</div><div class="scoreline">${star}<span>0</span><small>/ ${b.target.toLocaleString()}</small></div><div class="progress-track"><div class="progress-fill"></div></div>`;}
function topMarkup(d,b){if(d.id==='classic-cards')return levelHeader(b,true)+`<div class="hud"><div class="goal">${score(b)}</div>${moves(b)}</div>`;
 if(d.id==='compact-bar')return levelHeader(b,false)+`<div class="compact-hud"><div class="score-stack">${score(b)}</div>${moves(b)}</div>`;
 return levelHeader(b,false)+`<div class="floating-hud"><div class="score-pill">${star}<div><small>Target</small><strong>0 <em>/ ${b.target.toLocaleString()}</em></strong></div></div>${moves(b)}</div>`;
}
function boardMarkup(b){const holes=new Set(b.holes.map(h=>h.join(',')));let content='';for(let r=0;r<b.rows;r++)for(let c=0;c<b.cols;c++){const n=(r*2+c*5+Math.floor(c/3)+Math.floor(r/2))%6,missing=holes.has(r+','+c);content+=`<div class="cell ${(r+c)%2?'checker':''} ${missing?'missing':''}" data-row="${r}" data-col="${c}">${missing?'':`<img class="piece" src="${pieces}${n}.png" alt="${pieceNames[n]}" draggable="false">`}</div>`;}return content;}
function phoneMarkup(d){const b=boards[shape];return `<div class="phone"><div class="screen soft-storybook dynamic-screen ${d.id}" data-layout="${d.id}" role="img" aria-label="${d.name}: ${b.cols} columns and ${b.rows} rows, with separate header, board and booster regions"><div class="scenery"></div><div class="top-zone">${topMarkup(d,b)}</div><div class="playfield"><div class="board-shell"><div class="board" style="--cols:${b.cols};--rows:${b.rows}">${boardMarkup(b)}</div></div></div><div class="booster-area"><p class="booster-label">A little royal help</p><div class="boosters">${[['rocket-v',3,'Rocket'],['bomb',2,'Bomb'],['orb',1,'Colour orb'],['glove',2,'Glove']].map(([id,count,name])=>`<div class="booster"><img src="${boosters}${id}.png" alt="${name} booster"><b>${count}</b></div>`).join('')}</div></div><div class="home-indicator" aria-hidden="true"></div></div></div>`;}
function fitBoard(screen){if(!screen.isConnected||!screen.getBoundingClientRect().width)return;const b=boards[shape],field=screen.querySelector('.playfield'),shell=screen.querySelector('.board-shell'),width=screen.getBoundingClientRect().width,scale=width/(phone==='compact'?320:393),unit=width/393;
 const edge=9*unit,pad=(screen.classList.contains('floating-controls')?2:9)*unit,gap=(screen.classList.contains('floating-controls')?2.5:1.7)*unit;
 const availableWidth=field.clientWidth-edge*2,availableHeight=field.clientHeight-edge*2;
 const cell=Math.max(0,Math.min(44*unit,(availableWidth-pad*2-gap*(b.cols-1))/b.cols,(availableHeight-pad*2-gap*(b.rows-1))/b.rows));
 shell.style.setProperty('--tile-size',cell+'px');shell.style.setProperty('--grid-gap',gap+'px');shell.style.setProperty('--shell-pad',pad+'px');shell.style.width=(cell*b.cols+gap*(b.cols-1)+pad*2)+'px';shell.style.height=(cell*b.rows+gap*(b.rows-1)+pad*2)+'px';
 screen.dataset.pieceSize=(cell/scale).toFixed(1);screen.dataset.boardSpace=(field.clientHeight/scale).toFixed(0);
 const card=screen.closest('.layout-card');if(card){card.querySelector('[data-size]').textContent=Math.round(cell/scale)+' px tiles';card.querySelector('[data-fit]').textContent='Board stays clear';}
}
const observer=new ResizeObserver(entries=>{for(const entry of entries)fitBoard(entry.target.closest('.screen'));});
function observeScreens(){observer.disconnect();document.querySelectorAll('.dynamic-screen').forEach(s=>{observer.observe(s.querySelector('.playfield'));fitBoard(s);});}
function render(){const b=boards[shape];document.body.classList.toggle('compact',phone==='compact');document.querySelector('#layout-gallery').innerHTML=directions.map((d,i)=>`<article class="layout-card" data-card="${i}"><div class="card-heading"><span class="number">0${i+1}</span><h2>${d.name}</h2>${i===1?'<span class="pick">My pick</span>':''}</div>${phoneMarkup(d)}<div class="design-caption"><p class="mood">${d.mood}</p><p>${d.description}</p><button class="explore" data-open="${i}">Enlarge design ↗</button><div class="fit-note"><span data-size></span><b data-fit></b></div></div></article>`).join('');document.querySelector('#shape-note').textContent=`${b.name} board · Level ${b.level} outline · ${b.cols} columns × ${b.rows} rows`;
 document.querySelectorAll('[data-shape]').forEach(btn=>btn.setAttribute('aria-pressed',String(btn.dataset.shape===shape)));document.querySelectorAll('[data-phone]').forEach(btn=>btn.setAttribute('aria-pressed',String(btn.dataset.phone===phone)));if(document.querySelector('#enlarged').open)renderEnlarged();observeScreens();document.querySelector('#announcement').textContent=`Showing ${b.name.toLowerCase()} boards on ${phone==='compact'?'small':'standard'} phones.`;
}
function renderEnlarged(){const d=directions[enlargedIndex];document.querySelector('#enlarged-number').textContent='SOFT STORYBOOK · 0'+(enlargedIndex+1);document.querySelector('#enlarged-title').textContent=d.name;document.querySelector('#enlarged-note').textContent=d.detail;document.querySelector('#enlarged-phone').innerHTML=phoneMarkup(d);}
const dialog=document.querySelector('#enlarged');document.addEventListener('click',e=>{const shapeButton=e.target.closest('[data-shape]'),phoneButton=e.target.closest('[data-phone]'),openButton=e.target.closest('[data-open]');if(shapeButton){shape=shapeButton.dataset.shape;render();}if(phoneButton){phone=phoneButton.dataset.phone;render();}if(openButton){enlargedIndex=Number(openButton.dataset.open);opener=openButton;renderEnlarged();dialog.showModal();observeScreens();}});document.querySelector('#show-space').addEventListener('change',e=>document.body.classList.toggle('show-space',e.target.checked));document.querySelector('.close-dialog').onclick=()=>dialog.close();dialog.addEventListener('close',()=>{document.querySelector('#enlarged-phone').innerHTML='';observeScreens();opener?.focus();});dialog.addEventListener('click',e=>{if(e.target!==dialog)return;const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();});
render();document.fonts.ready.then(()=>document.querySelectorAll('.dynamic-screen').forEach(fitBoard));
window.softLayouts={ready:true,directions,boards,select(nextShape,nextPhone){shape=nextShape;phone=nextPhone;render();},fitAll(){document.querySelectorAll('.dynamic-screen').forEach(fitBoard);}};
