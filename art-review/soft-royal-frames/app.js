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
// Each frame is drawn around the same Soft Royal content. Unique paint IDs
// keep gradients correct when a design appears in both the gallery and dialog.
let frameInstance = 0;
function royalFrame(id) {
  const uid = `royal-metal-${++frameInstance}`;
  const metal = `url(#${uid})`;
  const rail = (shape, width=7) => `<path d="${shape}" fill="none" stroke="#775125" stroke-width="${width+2}"/><path d="${shape}" fill="none" stroke="${metal}" stroke-width="${width}"/>`;
  const round = 'M47 8H277Q316 8 316 47V489Q316 528 277 528H47Q8 528 8 489V47Q8 8 47 8Z';
  const inner = '<rect x="17" y="17" width="290" height="502" rx="29" fill="none" stroke="#bc944c" stroke-width="1" opacity=".55"/>';
  const diamond = (x,y,size=5) => `<path d="M${x} ${y-size}l${size} ${size}-${size} ${size}-${size}-${size}Z" fill="${metal}" stroke="#9a6b2d" stroke-width=".6"/>`;
  const corners = art => `<g>${art}</g><g transform="translate(324 0) scale(-1 1)">${art}</g><g transform="translate(0 536) scale(1 -1)">${art}</g><g transform="translate(324 536) scale(-1 -1)">${art}</g>`;
  const leaf = '<path d="M10 59C11 34 26 16 58 10" fill="none" stroke="#f2d287" stroke-width="1.8"/><path d="M13 46C1 39 3 29 8 24C14 29 18 36 13 46ZM20 32C10 23 14 13 20 9C24 18 25 24 20 32ZM31 21C23 12 28 4 34 2C36 10 36 15 31 21ZM17 40C22 27 32 26 38 29C34 35 28 39 17 40ZM27 27C32 16 41 15 47 19C43 23 37 27 27 27ZM42 15C45 6 53 4 59 7C55 13 48 16 42 15Z" fill="'+metal+'" stroke="#a27636" stroke-width=".6"/>';
  const scroll = `<path d="M9 66C-2 50 0 25 16 11C28 0 48 0 63 8C51 7 38 14 41 23C44 31 55 25 50 20C47 17 45 21 47 23M13 53C20 40 34 45 29 54C26 59 19 56 22 52M13 34C19 18 31 15 38 17" fill="none" stroke="#89602e" stroke-width="5"/><path d="M9 66C-2 50 0 25 16 11C28 0 48 0 63 8C51 7 38 14 41 23C44 31 55 25 50 20C47 17 45 21 47 23M13 53C20 40 34 45 29 54C26 59 19 56 22 52M13 34C19 18 31 15 38 17" fill="none" stroke="${metal}" stroke-width="3.4"/><path d="M13 33C6 22 11 10 19 7C24 16 22 24 13 33ZM27 16C26 4 37-2 43 2C39 11 34 15 27 16Z" fill="${metal}" stroke="#ad7a37" stroke-width=".7"/>${diamond(14,15,5)}`;
  let art='';
  if(id===1) {
    art=rail(round,5)+inner+corners(`<path d="M11 58V44Q11 11 44 11H61" fill="none" stroke="#fff8d1" stroke-width="1.2"/>${diamond(20,20,4)}<path d="M24 32Q29 23 38 24" fill="none" stroke="#ceab63" stroke-width="1.2"/>`)+diamond(162,528,4);
  } else if(id===2) {
    const shape='M35 8H289L316 35V501L289 528H35L8 501V35Z';
    art=rail(shape,22)+`<path d="${shape}" fill="none" stroke="#244e67" stroke-width="17"/><path d="${shape}" fill="none" stroke="#4b7790" stroke-width="1"/><path d="M38 21H286L303 38V498L286 515H38L21 498V38Z" fill="none" stroke="#fff0b0" stroke-width="1.2"/>`+corners(`<path d="M3 37L37 3L45 10L10 45Z" fill="${metal}" stroke="#8b5d29" stroke-width="1"/><path d="M10 36L36 10" stroke="#fff0b5" stroke-width="1.3"/>${diamond(22,22,6)}<path d="m22 17 4 5-4 5-4-5Z" fill="#5b91a4" stroke="#fae3a0" stroke-width="1"/>`)+`<path d="M136 528h52" stroke="#edca7c" stroke-width="1.5"/>`+diamond(162,528,5);
  } else if(id===3) {
    art=rail(round,9)+inner+corners(`<path d="M8 58V37Q8 8 37 8H59" fill="none" stroke="${metal}" stroke-width="12"/><path d="M9 55C13 34 36 39 29 23C25 14 16 21 21 27C24 31 29 27 26 24M39 12C47 3 58 3 65 8" fill="none" stroke="#fff0b0" stroke-width="1.7"/>${diamond(13,13,6)}`)+`<path d="M122 529Q141 512 162 527Q183 512 202 529M136 528Q148 539 162 529Q176 539 188 528" fill="none" stroke="${metal}" stroke-width="2.2"/>`+diamond(162,528,7);
  } else if(id===4) {
    art=rail(round,19)+`<path d="${round}" fill="none" stroke="#3e654d" stroke-width="14"/><path d="${round}" fill="none" stroke="#78a37b" stroke-width="1"/><rect x="20" y="20" width="284" height="496" rx="29" fill="none" stroke="#caab67" stroke-width="1"/>`+corners(leaf)+`<path d="M134 528Q148 519 162 529Q176 519 190 528" fill="none" stroke="${metal}" stroke-width="1.5"/>`+diamond(162,528,6);
  } else {
    const shape='M48 8H126Q162 -1 198 8H276Q285 6 288 17Q291 29 308 32Q318 34 316 48V488Q317 503 306 505Q291 508 288 521Q286 531 276 528H198Q162 537 126 528H48Q36 531 34 520Q30 508 17 505Q6 503 8 488V48Q5 35 17 32Q32 29 35 16Q37 6 48 8Z';
    art=rail(shape,13)+`<path d="M48 17H126Q162 9 198 17H276Q280 35 305 42V494Q280 501 276 519H198Q162 527 126 519H48Q41 500 19 494V42Q41 35 48 17Z" fill="none" stroke="#c29b51" stroke-width="1"/>`+corners(scroll)+`<g transform="translate(162 529)"><path d="M-48 0C-38-13-20-11-14-2C-10 5-19 10-23 4C-25 0-21-2-18 0M48 0C38-13 20-11 14-2C10 5 19 10 23 4C25 0 21-2 18 0" fill="none" stroke="${metal}" stroke-width="2.8"/><path d="M0-13C-15-6-12 5 0 13C12 5 15-6 0-13Z" fill="${metal}" stroke="#94632e" stroke-width="1"/><path d="M0-7C-7-3-6 2 0 7C6 2 7-3 0-7Z" fill="#54745a"/></g>`;
  }
  return `<svg class="frame-art" viewBox="0 0 324 536" preserveAspectRatio="none" aria-hidden="true"><defs><linearGradient id="${uid}" x1="0" y1="0" x2=".8" y2="1"><stop stop-color="#a16b29"/><stop offset=".12" stop-color="#ffecab"/><stop offset=".3" stop-color="#d3a451"/><stop offset=".49" stop-color="#f5d58b"/><stop offset=".67" stop-color="#b58236"/><stop offset=".85" stop-color="#ebc675"/><stop offset="1" stop-color="#986127"/></linearGradient></defs><g stroke-linecap="round" stroke-linejoin="round">${art}</g></svg>`;
}
function crownCrest() {
  return `<svg class="royal-crown" viewBox="0 0 90 54" aria-hidden="true"><path d="m10 12 17 11L45 3l18 20 17-11-8 33H18Z" fill="#dcb25e" stroke="#795529" stroke-width="2.5" stroke-linejoin="round"/><path d="m15 16 15 14L45 8l15 22 15-14-6 23H21Z" fill="#ffdf88"/><path d="m45 8 0 31H21l-6-23 15 14Z" fill="#f7ce6d"/><path d="m45 8 0 31h24l6-23-15 14Z" fill="#e4b251"/><path d="M18 39H72V48H18Z" fill="#ecc778" stroke="#8c6330" stroke-width="1.5"/><path d="M21 42H69" stroke="#fff0b6" stroke-width="2"/><path d="m45 23 5 7-5 7-5-7Z" fill="#477459" stroke="#fff0b5" stroke-width="1.3"/><circle cx="10" cy="11" r="4" fill="#fff0a8" stroke="#966e32"/><circle cx="45" cy="4" r="3.5" fill="#fff0a8" stroke="#966e32"/><circle cx="80" cy="11" r="4" fill="#fff0a8" stroke="#966e32"/></svg>`;
}
const softRoyal = id => `<div class="level-popup soft-card">${royalFrame(id)}${id===3?crownCrest():''}${closeButton()}<div class="level-cap">${svg('crown')}<h3>Level 3</h3></div><div class="card-content"><p class="popup-kicker">A LITTLE CHALLENGE. A LITTLE MAGIC.</p>${goal()}<div class="fine-divider"></div>${boosterRow()}${play()}<span class="card-footnote">Your next star is waiting</span></div></div>`;
const variants = [
  {
    id:1, name:'Gilded Silk', tag:'LIGHT & ELEGANT',
    note:'A fine double-gold border, tiny diamond corners, and a soft satin finish.',
    layout:'Slim, rounded gold molding with a delicate inner line. The green level badge floats over the edge.',
    why:'Adds a polished royal touch while keeping the cream panel spacious and quiet.',
    consider:'The lightest frame of the five. Best if you prefer subtle decoration.',
    render:()=>softRoyal(1)
  },
  {
    id:2, name:'Sapphire Trim', tag:'CLASSIC ROYAL',
    note:'Blue enamel, gold corner plates, and a crisp, cut-corner silhouette.',
    layout:'A deep sapphire border between two gold rails, with beveled gold corner clasps.',
    why:'Blue and gold make the popup unmistakably royal and echo the castle’s blue rooftops.',
    consider:'The strongest color contrast. The cream center stays as soft as before.',
    render:()=>softRoyal(2)
  },
  {
    id:3, name:'Crown Crest', tag:'MY PICK · SOFT & REGAL',
    note:'A sculpted crown, curled gold shoulders, and a little jewel at the base.',
    layout:'Rounded gold molding with a crown above the level badge and gentle scrollwork at the corners.',
    why:'The crown gives Soft Royal a clear royal identity. A lovely balance of warmth and character.',
    consider:'The crown makes this the tallest silhouette, with the same space inside for the goal and boosters.',
    render:()=>softRoyal(3)
  },
  {
    id:4, name:'Emerald Laurel', tag:'GARDEN ROYALTY',
    note:'An emerald border wrapped in little gold laurel leaves at every corner.',
    layout:'A rounded emerald enamel band with gold edges and four pairs of laurel branches.',
    why:'The green frame connects naturally to your home controls and the castle gardens.',
    consider:'Feels botanical and welcoming. The leaf details are most visible in the larger preview.',
    render:()=>softRoyal(4)
  },
  {
    id:5, name:'Palace Carving', tag:'THE GRANDEST FRAME',
    note:'Carved gold scrolls, scalloped edges, and an emerald set into the base.',
    layout:'A shaped gilded frame with sculpted corners, fine inner molding, and a small jewel ornament.',
    why:'The richest royal treatment, with the look of a little palace mirror around the cream panel.',
    consider:'The most decorative choice. Open it at phone size to judge the amount of ornament.',
    render:()=>softRoyal(5)
  }
];

const state = new Map(variants.map(v=>[v.id,{selected:new Set(),closed:false}]));
let saved;
try {
  const value=JSON.parse(localStorage.getItem('king-leo-soft-royal-frame-shortlist')||'[]');
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
  try {localStorage.setItem('king-leo-soft-royal-frame-shortlist',JSON.stringify([...saved]));} catch {}
  syncSaved();announce(`Design 0${id} ${saved.has(id)?'added to':'removed from'} your shortlist.`);
}
function openPreview(id) {
  current=id;const v=variants[id-1];
  const old=document.querySelector('#inspector-phone .phone-stage');if(old)observer.unobserve(old);
  document.querySelector('#inspector-phone').innerHTML=screenHTML(v);
  document.querySelector('#inspector-number').textContent=`0${id}`;
  document.querySelector('#inspector-title').textContent=v.name;
  document.querySelector('#inspector-description').textContent=v.note;
  document.querySelector('#inspector-details').innerHTML=`<dt>The frame</dt><dd>${v.layout}</dd><dt>Why choose it</dt><dd>${v.why}</dd><dt>Consider</dt><dd>${v.consider}</dd>`;
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
