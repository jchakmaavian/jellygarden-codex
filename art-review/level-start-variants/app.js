const asset = (name, cls = '', alt = '') => `<img class="${cls}" src="assets/${name}.png" alt="${alt}" draggable="false">`;
const svg = (name) => {
  const paths = {
    arrow: '<path d="m9 5 7 7-7 7"/>',
    heart: '<path d="M20.8 4.6a5.4 5.4 0 0 0-7.6 0L12 5.8l-1.2-1.2a5.4 5.4 0 0 0-7.6 7.6L12 21l8.8-8.8a5.4 5.4 0 0 0 0-7.6Z"/>',
    crown: '<path d="m3 6 5 5 4-8 4 8 5-5-2 13H5L3 6Zm3 10h12"/>',
    check: '<path d="m5 12 4 4L19 6"/>',
    close: '<path d="m6 6 12 12M6 18 18 6"/>'
  };
  return `<svg viewBox="0 0 24 24" class="glyph" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${paths[name]}</svg>`;
};
const boosters = [
  {name:'Rocket', art:'booster_stripes', count:1},
  {name:'Bomb', art:'booster_packages', count:2},
  {name:'Color ball', art:'booster_colorbomb', count:3}
];
const closeButton = () => `<button class="popup-close" data-action="close" aria-label="Close level popup">${svg('close')}</button>`;
const boosterRow = () => `<div class="booster-heading"><h4>Choose boosters</h4><span>OPTIONAL</span></div><div class="booster-row" role="group" aria-label="Optional starting boosters">${boosters.map((b,i)=>`<button class="booster b${i}" data-booster="${i}" aria-pressed="false" aria-label="${b.name}, ${b.count} available"><span class="booster-tile">${asset(b.art)}<b class="stock">${b.count}</b><i class="selected-mark">${svg('check')}</i></span><span class="booster-name">${b.name}</span></button>`).join('')}</div><p class="selection-hint" data-selection>Boosters are optional</p>`;
const play = (label='Play') => `<button class="play" data-action="play"><span>${label}</span>${svg('arrow')}</button>`;
const goal = () => `<div class="goal"><div class="goal-art">${asset('star')}<i class="spark s1">✦</i><i class="spark s2">✧</i></div><div class="goal-copy"><small>YOUR GOAL</small><strong>500 <span>points</span></strong></div></div>`;
const variants = [
  {
    id:1, name:'Soft Royal', tag:'THE NATURAL FIT',
    note:'Warm cream, soft corners, and a single clear next step.',
    layout:'A compact centered card with a floating level badge and a large, calm goal.',
    why:'The closest match to your new home screen. Familiar, friendly, and easy to read.',
    consider:'Keeps the familiar popup layout, with a much lighter visual treatment.',
    render:()=>`<div class="level-popup soft-card">${closeButton()}<div class="level-cap">${svg('crown')}<h3>Level 3</h3></div><p class="popup-kicker">A LITTLE CHALLENGE. A LITTLE MAGIC.</p>${goal()}<div class="fine-divider"></div>${boosterRow()}${play()}<span class="card-footnote">Your next star is waiting</span></div>`
  },
  {
    id:2, name:'Castle Drawer', tag:'MORE OF YOUR WORLD',
    note:'The castle stays in view. Everything you need slides up from below.',
    layout:'A rounded bottom sheet with an inline goal and a tidy booster row.',
    why:'Carries Castle First’s focus on the artwork into the level-start moment.',
    consider:'Feels more like a mobile sheet than a traditional game popup.',
    render:()=>`<div class="drawer-caption"><span>CASTLE GATES</span><p>One more step<br>toward something grand.</p></div><div class="level-popup drawer"><div class="grab-handle"></div>${closeButton()}<header class="drawer-header"><span>NEXT ADVENTURE</span><h3>Level 3</h3><p>Ready when you are.</p></header>${goal()}${boosterRow()}${play('Let’s play')}</div>`
  },
  {
    id:3, name:'The Royal Ticket', tag:'A LITTLE OCCASION',
    note:'A playful invitation to the next puzzle, signed by your kingdom.',
    layout:'A paper ticket with a blue header, a bold score goal, and a perforated booster section.',
    why:'Gives every level a memorable shape while keeping the information simple.',
    consider:'The paper details add character and a little more decoration.',
    render:()=>`<div class="level-popup ticket">${closeButton()}<header class="ticket-header">${svg('crown')}<span>YOUR NEXT ADVENTURE</span><h3>Level 3</h3><small>CASTLE GATES</small></header><div class="ticket-body">${goal()}<div class="ticket-tear"><i></i><span>MAKE A GRAND ENTRANCE</span><i></i></div>${boosterRow()}${play('Play now')}<div class="ticket-footer"><span>✦</span><small>ONE PUZZLE CLOSER TO A BEAUTIFUL KINGDOM</small><span>✦</span></div></div></div>`
  },
  {
    id:4, name:'Emerald & Gold', tag:'QUIETLY REGAL',
    note:'Deep emerald, warm gold, and an illuminated goal at the center.',
    layout:'A dark floating panel, a gold crown medallion, and an amber Play button.',
    why:'Creates strong contrast and gives the start of a level a more regal mood.',
    consider:'A darker direction than the cream controls on your home screen.',
    render:()=>`<div class="level-popup emerald">${closeButton()}<div class="crest">${svg('crown')}</div><span class="emerald-kicker">THE KINGDOM IS COUNTING ON YOU</span><h3>Level 3</h3>${goal()}<div class="fine-divider"></div>${boosterRow()}${play()}<span class="card-footnote">A small challenge. A golden opportunity.</span></div>`
  },
  {
    id:5, name:'A Little Leo Luck', tag:'WARM & PLAYFUL',
    note:'Leo brings the encouragement. You bring the next brilliant move.',
    layout:'A character-led card with a peeking Leo portrait, a speech bubble, and colorful booster tiles.',
    why:'Adds personality and makes the game feel more welcoming.',
    consider:'The character takes more space than the simpler directions.',
    render:()=>`<div class="level-popup leo-card"><div class="leo-peek"><div class="leo-halo"></div>${asset('avatar_leo_00','leo-portrait')}<span class="leo-bubble">You’ve got this!</span><i class="leo-spark">✦</i></div>${closeButton()}<div class="leo-level">${svg('crown')}<h3>Level 3</h3></div><p class="leo-subtitle">Let’s make it shine.</p>${goal()}${boosterRow()}${play()}<span class="card-footnote">A little help can make a little magic</span></div>`
  }
];

const state = new Map(variants.map(v=>[v.id,{selected:new Set(),closed:false}]));
let saved;
try {
  const value=JSON.parse(localStorage.getItem('king-leo-level-start-shortlist')||'[]');
  saved=new Set(Array.isArray(value)?value.filter(id=>variants.some(v=>v.id===id)):[]);
} catch { saved=new Set(); }
let current=1, filter='all', motion=!matchMedia('(prefers-reduced-motion: reduce)').matches;
const dialog=document.querySelector('#inspector');
const gallery=document.querySelector('#gallery');
const timers=new WeakMap();
const observer=new ResizeObserver(entries=>entries.forEach(({target,contentRect})=>{
  target.querySelector('.game-screen')?.style.setProperty('--scale',contentRect.width/360);
}));

function homeBackdrop() {
  return `<div class="home-scene" aria-hidden="true"><div class="wallet"><span>${asset('icon_coin')}<b>5</b></span><span>${asset('icon_star')}<b>3</b></span></div><div class="gear">${asset('icon_gear')}</div><div class="home-title"><small>AREA 01</small><strong>Castle Gates</strong><span>1 / 3 restored</span></div><div class="home-play"><small>NEXT PUZZLE</small><b>Level 3</b></div><div class="home-nav">${[['nav_areas','Areas'],['nav_leaderboard','Ranks'],['nav_home','Home'],['nav_cards','Cards']].map(([a,l])=>`<span class="${l==='Home'?'active':''}">${asset(a)}<b>${l}</b></span>`).join('')}</div></div>`;
}
function screenHTML(v) {
  return `<div class="phone-stage"><section class="game-screen v${v.id}" data-concept="${v.id}" aria-label="${v.name} level-start mockup">${homeBackdrop()}<div class="scrim"></div>${v.render()}<button class="reopen" data-action="reopen">Open Level 3 ${svg('arrow')}</button><div class="demo-toast" role="status" aria-live="polite"></div></section></div>`;
}
function observeStages() { document.querySelectorAll('.phone-stage').forEach(s=>observer.observe(s)); }
function renderGallery() {
  gallery.innerHTML=variants.map(v=>`<article class="concept-card" data-card="${v.id}"><header class="card-header"><span class="concept-number">0${v.id}</span><div><span class="concept-tag">${v.tag}</span><h2>${v.name}</h2></div><button class="heart-button" data-save="${v.id}" aria-label="Shortlist ${v.name}" aria-pressed="false">${svg('heart')}</button></header>${screenHTML(v)}<div class="card-description"><p>${v.note}</p><button class="inspect-button" data-inspect="${v.id}">Explore design <span>↗</span></button></div></article>`).join('');
  observeStages(); syncAll();
}
function sync(id) {
  const s=state.get(id);
  document.querySelectorAll(`.game-screen[data-concept="${id}"]`).forEach(screen=>{
    screen.classList.toggle('is-closed',s.closed);
    screen.querySelector('.level-popup').inert=s.closed;
    screen.querySelector('.reopen').tabIndex=s.closed?0:-1;
    if(s.closed){const toast=screen.querySelector('.demo-toast');clearTimeout(timers.get(toast));toast.classList.remove('visible');}
    screen.querySelectorAll('[data-booster]').forEach(button=>button.setAttribute('aria-pressed',s.selected.has(Number(button.dataset.booster))));
    const count=s.selected.size;
    screen.querySelector('[data-selection]').textContent=count?`${count} booster${count===1?'':'s'} ready to go`:'Boosters are optional';
  });
}
function syncSaved() {
  document.querySelectorAll('[data-save]').forEach(b=>b.setAttribute('aria-pressed',saved.has(Number(b.dataset.save))));
  document.querySelector('#saved-count').textContent=saved.size;
  const button=document.querySelector('#inspector-save');
  button.setAttribute('aria-pressed',saved.has(current));
  button.innerHTML=`${svg('heart')} ${saved.has(current)?'Saved to your shortlist':'Add to shortlist'}`;
  let visible=0;
  document.querySelectorAll('[data-card]').forEach(card=>{card.hidden=filter==='saved'&&!saved.has(Number(card.dataset.card));if(!card.hidden)visible++;});
  document.querySelector('#empty').hidden=visible>0;
  document.querySelectorAll('.filter').forEach(b=>{b.classList.toggle('active',b.dataset.filter===filter);b.setAttribute('aria-pressed',b.dataset.filter===filter);});
}
function syncMotion() {
  document.body.classList.toggle('motion-off',!motion);
  const b=document.querySelector('#motion');b.setAttribute('aria-pressed',motion);b.querySelector('span').textContent=motion?'on':'off';
}
function syncAll() {variants.forEach(v=>sync(v.id));syncSaved();syncMotion();}
function announce(message) {
  const el=document.querySelector('#announcement');el.textContent=message;el.classList.add('visible');
  clearTimeout(timers.get(el));timers.set(el,setTimeout(()=>el.classList.remove('visible'),2200));
}
function save(id) {
  saved.has(id)?saved.delete(id):saved.add(id);
  try {localStorage.setItem('king-leo-level-start-shortlist',JSON.stringify([...saved]));} catch {}
  syncSaved();announce(`Design 0${id} ${saved.has(id)?'added to':'removed from'} your shortlist.`);
}
function openPreview(id) {
  current=id;const v=variants[id-1];
  const old=document.querySelector('#inspector-phone .phone-stage');if(old)observer.unobserve(old);
  document.querySelector('#inspector-phone').innerHTML=screenHTML(v);
  document.querySelector('#inspector-number').textContent=`0${id}`;
  document.querySelector('#inspector-title').textContent=v.name;
  document.querySelector('#inspector-description').textContent=v.note;
  document.querySelector('#inspector-details').innerHTML=`<dt>The layout</dt><dd>${v.layout}</dd><dt>Why choose it</dt><dd>${v.why}</dd><dt>Consider</dt><dd>${v.consider}</dd>`;
  document.querySelector('#position').textContent=`${id} / 5`;
  if(!dialog.open)dialog.showModal();
  document.body.classList.add('preview-open');
  observeStages();sync(id);syncSaved();
  history.replaceState(null,'',`#design-0${id}`);
}
function closePreview() {dialog.close();}
dialog.addEventListener('close',()=>{document.body.classList.remove('preview-open');history.replaceState(null,'',location.pathname+location.search);});
dialog.addEventListener('click',e=>{if(e.target===dialog){const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)closePreview();}});
function demoPlay(screen,id) {
  const count=state.get(id).selected.size;
  const el=screen.querySelector('.demo-toast');
  el.innerHTML=`<span class="toast-check">${svg('check')}</span><b>Ready for Level 3</b><span>${count?`${count} booster${count===1?'':'s'} selected`:'No boosters selected'} · Preview only</span>`;
  el.classList.add('visible');clearTimeout(timers.get(el));timers.set(el,setTimeout(()=>el.classList.remove('visible'),2300));
}
document.addEventListener('click',e=>{
  const b=e.target.closest('button');if(!b)return;
  if(b.dataset.save){save(Number(b.dataset.save));return;}
  if(b.dataset.inspect){openPreview(Number(b.dataset.inspect));return;}
  if(b.dataset.filter){filter=b.dataset.filter;syncSaved();return;}
  const screen=b.closest('[data-concept]');if(!screen)return;
  const id=Number(screen.dataset.concept),s=state.get(id);
  if(b.hasAttribute('data-booster')){
    const booster=Number(b.dataset.booster);s.selected.has(booster)?s.selected.delete(booster):s.selected.add(booster);sync(id);
  } else if(b.dataset.action==='play') demoPlay(screen,id);
  else if(b.dataset.action==='close'){s.closed=true;s.selected.clear();sync(id);screen.querySelector('.reopen').focus();}
  else if(b.dataset.action==='reopen'){s.closed=false;sync(id);screen.querySelector('.popup-close').focus();}
});
document.querySelector('#close-preview').addEventListener('click',closePreview);
document.querySelector('#inspector-save').addEventListener('click',()=>save(current));
document.querySelector('#previous').addEventListener('click',()=>openPreview(current===1?5:current-1));
document.querySelector('#next').addEventListener('click',()=>openPreview(current===5?1:current+1));
document.querySelector('#motion').addEventListener('click',()=>{motion=!motion;syncMotion();});
document.querySelector('#reset').addEventListener('click',()=>{
  state.forEach(s=>{s.selected.clear();s.closed=false;});
  document.querySelectorAll('.demo-toast').forEach(el=>{clearTimeout(timers.get(el));el.classList.remove('visible');});
  syncAll();announce('All five demos are reset.');
});
document.addEventListener('keydown',e=>{if(!dialog.open)return;if(e.key==='ArrowRight'){e.preventDefault();openPreview(current===5?1:current+1);}if(e.key==='ArrowLeft'){e.preventDefault();openPreview(current===1?5:current-1);}});
renderGallery();
const match=location.hash.match(/^#design-0?([1-5])$/);if(match)openPreview(Number(match[1]));
