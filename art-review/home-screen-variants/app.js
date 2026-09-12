const A = 'assets/';
const img = (name, cls = '', alt = '') => `<img class="${cls}" src="${A}${name}.png" alt="${alt}" draggable="false">`;
const glyph = (name, cls = '') => {
  const paths = {
    arrow: '<path d="m9 5 7 7-7 7"/>',
    hammer: '<path d="m14 5 5 5M12 3l3 3-9 9-3 6 6-3 9-9 3 3M11 4l3-2 8 8-2 3"/>',
    flag: '<path d="M5 21V3m0 1c5-4 9 4 15 0v10c-6 4-10-4-15 0"/>',
    lamp: '<path d="M9 4a3 3 0 0 1 6 0m-8 4h10l2 11H5L7 8Zm-1-3h12M4 22h16M12 11v5"/>',
    crown: '<path d="m3 6 5 5 4-8 4 8 5-5-2 13H5L3 6Zm3 10h12"/>',
    check: '<path d="m5 12 4 4L19 6"/>',
    heart: '<path d="M20.8 4.6a5.4 5.4 0 0 0-7.6 0L12 5.8l-1.2-1.2a5.4 5.4 0 0 0-7.6 7.6L12 21l8.8-8.8a5.4 5.4 0 0 0 0-7.6Z"/>'
  };
  return `<svg class="glyph ${cls}" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round">${paths[name] || paths.crown}</svg>`;
};
const hud = (cls = '') => `<div class="hud ${cls}"><div class="wallet"><span class="currency coin" aria-label="5 coins">${img('icon_coin')}<b>5</b></span><span class="currency star" aria-label="1 star available">${img('icon_star')}<b data-stars>1</b></span></div><button class="settings" data-action="settings" aria-label="Settings">${img('icon_gear')}</button></div>`;
const progress = (cls = '') => `<div class="progress-block ${cls}"><div class="progress-text"><span>Area 1</span><span><b data-done>0</b> / 3 repairs</span></div><div class="progress-track"><i data-progress></i></div></div>`;
const nav = (cls = '') => `<nav class="game-nav ${cls}" aria-label="Kingdom navigation">${[['areas','Areas'],['leaderboard','Ranks'],['home','Home'],['cards','Cards']].map(([key,label])=>`<button data-action="nav" data-destination="${key}" ${key === 'home' ? 'class="current" aria-current="page"' : ''} aria-label="${key==='leaderboard'?'Leaderboard':label}">${img('nav_'+key)}<span>${label}</span></button>`).join('')}</nav>`;
const play = (label = 'PLAY', cls = '') => `<button class="play-button ${cls}" data-action="play"><span><small>${label}</small><b>Level 2</b></span>${glyph('arrow')}</button>`;
const repair = (text = 'Fix the gate', cls = '') => `<button class="repair-button ${cls}" data-action="repair">${glyph('hammer')}<span data-repair-label>${text}</span><span class="star-cost">${img('icon_star')}1</span></button>`;
const area = (cls = '') => `<div class="area-title ${cls}"><small>AREA 01</small><h3>Castle Gates</h3></div>`;
const pips = () => '<span class="repair-pips"><i data-pip="0"></i><i data-pip="1"></i><i data-pip="2"></i></span>';
const variants = [
  { id:1, name:'Royal Welcome', group:'royal', tag:'START HERE', note:'The familiar royal style, with a much clearer next move.', layout:'A tidy top bar, a compact repair card, and one large green Play button.', strength:'A strong fit with the victory screen you already approved.', tradeoff:'More decorated than the simpler options.', render:()=>`${hud()}${area('ribbon-title')}<div class="bottom-cluster"><div class="royal-task"><button class="chest-button" data-action="repair" aria-label="View castle repairs">${img('icon_chest')}</button><div><strong>A grand entrance awaits!</strong>${progress()}</div><button class="task-arrow" data-action="repair" aria-label="View repairs">${glyph('arrow')}</button></div>${play()}</div>${nav('nav-royal')}` },
  { id:2, name:'The Storybook', group:'royal', tag:'WARM & CHARMING', note:'Your castle feels like a page in a beautifully illustrated tale.', layout:'A floating chapter title and an ivory storybook at the bottom.', strength:'Makes restoration feel like a story worth following.', tradeoff:'The book covers more of the bottom of the scene.', render:()=>`${hud()}<div class="chapter-title"><span>CHAPTER ONE</span><h3>Castle Gates</h3><div>✦ &nbsp; A royal beginning &nbsp; ✦</div></div><div class="storybook"><div class="book-seal">${glyph('crown')}</div><small>EVERY KINGDOM HAS A STORY</small><h3>Let’s open a new chapter.</h3><div class="book-progress">${pips()}<span><b data-done>0</b> of 3 repairs complete</span></div>${repair()}${play('THE ADVENTURE CONTINUES')}</div>${nav('nav-paper')}` },
  { id:3, name:'Castle First', group:'minimal', tag:'ROOM TO BREATHE', note:'Almost all castle. The controls gently float above the world.', layout:'Small translucent controls, a repair marker on the gate, and a floating Play pill.', strength:'The artwork becomes the clear focus of the screen.', tradeoff:'The subtle controls need careful contrast over future area art.', render:()=>`${hud('hud-light')}<div class="scene-label">${area()}<span class="quiet-progress"><b data-done>0</b> / 3 restored</span></div><button class="gate-hotspot" data-action="repair">${glyph('hammer')}<span data-repair-label>Fix the gate</span><em>${img('icon_star')}1</em></button><div class="float-play">${play('NEXT PUZZLE')}</div>${nav('nav-glass')}` },
  { id:4, name:'Little Kingdom', group:'playful', tag:'BRIGHT & BOUNCY', note:'Big friendly shapes, bright colors, and buttons made for little hands.', layout:'Chunky counters, a cloud-like lower panel, and a large green Play button.', strength:'Clear, cheerful, and easy to understand at a glance.', tradeoff:'A deliberately playful look with larger controls.', render:()=>`${hud()}<div class="welcome-cloud"><small>YOUR LITTLE KINGDOM</small><h3>Welcome home!</h3></div><button class="ready-sticker" data-action="repair">${glyph('hammer')}<span data-ready>1 repair<br>ready!</span></button><div class="cloud-dock"><div class="cloud-area"><div><small>AREA 1</small><strong>Castle Gates</strong></div><button data-action="repair" class="bubble-chest" aria-label="Castle repairs">${img('icon_chest')}<span data-done>0</span><small>/3</small></button></div>${play('LET’S PLAY!')}${nav('nav-bubbles')}</div>` },
  { id:5, name:'A King’s Welcome', group:'playful', tag:'CHARACTER FIRST', note:'Leo greets you and gives the next repair a little personality.', layout:'A friendly king portrait and speech card, with a compact action dock below.', strength:'Makes the home screen feel personal and alive.', tradeoff:'The welcome message takes some space from the sky.', render:()=>`${hud()}<div class="leo-welcome">${img('avatar_leo_00','leo-avatar','King Leo')}<div><span>GOOD TO SEE YOU!</span><h3>Your castle<br>needs you.</h3><p>Shall we fix that gate?</p></div></div><div class="concierge-dock"><div class="concierge-title"><span>${glyph('crown')} Castle Gates</span><small>AREA 1</small></div>${repair('Let’s fix the gate')}${progress()}${play('A LITTLE PUZZLE. A LITTLE MAGIC.')}</div>${nav('nav-concierge')}` },
  { id:6, name:'The Royal Workshop', group:'playful', tag:'REPAIR FIRST', note:'Put the satisfying part—bringing the castle back to life—up front.', layout:'A small workshop panel presents the next repair before the level button.', strength:'Makes it clear what the player’s star can do right now.', tradeoff:'Repair becomes the first action when a star is available.', render:()=>`${hud()}${area('workshop-title')}<div class="workshop"><div class="workshop-heading">${glyph('hammer')}<span>Your next little project</span></div><div class="workshop-task"><div class="gate-thumb"></div><div><small>LET’S MAKE IT BEAUTIFUL</small><h3 data-repair-label>Fix the broken gate</h3><span>One star makes a difference.</span></div></div>${repair('Restore now')}${progress()}${play('PLAY FOR MORE STARS')}</div>${nav('nav-workshop')}` },
  { id:7, name:'Crown & Velvet', group:'royal', tag:'A ROYAL OCCASION', note:'A little theatrical, with rich red banners and warm gold accents.', layout:'A crest above the castle and a framed velvet action panel at the bottom.', strength:'Creates a distinctive regal look with a strong visual identity.', tradeoff:'The most ornate direction in the collection.', render:()=>`<div class="velvet-drapes"></div>${hud()}<div class="royal-crest">${glyph('crown')}<h3>Castle Gates</h3><span>YOUR KINGDOM AWAITS</span></div><div class="velvet-dock"><button class="crest-chest" data-action="repair" aria-label="View castle restoration">${img('icon_chest')}</button><h3>A kingdom worth restoring.</h3>${progress()}${play('CONTINUE YOUR ADVENTURE')}</div>${nav('nav-velvet')}` },
  { id:8, name:'The Garden Postcard', group:'minimal', tag:'SOFT & SUNNY', note:'A warm illustrated postcard with a clean, calm place for every control.', layout:'The castle sits in an inset picture above a cream and sage action area.', strength:'Very clear text and a pleasantly simple navigation bar.', tradeoff:'The scene feels framed instead of fully immersive.', render:()=>`${hud()}<div class="postcard-stamp">${glyph('crown')}<span>WITH LOVE,<br>YOUR KINGDOM</span></div><div class="postcard-caption"><small>AREA 01 · A NEW BEGINNING</small><h3>Castle Gates</h3><p>A little care goes a long way.</p><button class="postcard-repairs" data-action="repair"><span>${pips()}<b data-done>0</b> of 3 repairs</span><strong>Restore ${glyph('arrow')}</strong></button>${play('YOUR NEXT ADVENTURE')}</div>${nav('nav-postcard')}` },
  { id:9, name:'The Adventure Trail', group:'playful', tag:'PROGRESS YOU CAN SEE', note:'Three little milestones turn restoration into a journey.', layout:'A visible gate–flag–lamps trail sits directly above a large Play button.', strength:'Shows what comes next and makes each repair feel tangible.', tradeoff:'The milestone row is an extra element over the scene.', render:()=>`${hud()}<div class="adventure-title"><span>YOUR NEXT ADVENTURE</span><h3>A grand entrance!</h3><small>AREA 1 · CASTLE GATES</small></div><button class="trail-pin" data-action="repair" aria-label="Fix the gate">${glyph('hammer')}<i>1</i></button><div class="adventure-dock"><div class="trail-heading"><b>Bring your castle to life</b><span><b data-done>0</b> / 3</span></div><div class="milestone-trail">${[['hammer','Gate'],['flag','Flag'],['lamp','Lamps']].map(([g,label],i)=>`<button class="milestone ${i===0?'next':''}" data-action="repair" data-milestone="${i}"><span>${glyph(g)}</span><b>${label}</b></button>`).join('')}</div>${play('THE NEXT STAR IS WAITING')}</div>${nav('nav-adventure')}` },
  { id:10, name:'Golden Hour', group:'minimal', tag:'LESS, BUT LOVELIER', note:'Warm light, quiet cream panels, and a confident amber Play button.', layout:'A small left-aligned title, a slim repair row, and an uncluttered bottom dock.', strength:'Balances a beautiful scene with very readable controls.', tradeoff:'Uses less of the game’s traditional blue-and-gold decoration.', render:()=>`${hud('hud-joined')}<div class="golden-title"><span>GOOD TO BE HOME</span><h3>Castle<br>Gates.</h3><small>YOUR KINGDOM, ONE STAR AT A TIME.</small></div><div class="golden-dock"><button class="golden-repair" data-action="repair"><div class="mini-star">${img('icon_star')}</div><span><b data-ready>One repair ready</b><small>Area 1 · <b data-done>0</b> of 3 complete</small></span>${glyph('arrow')}</button>${play('LET’S MAKE SOME MAGIC')}</div>${nav('nav-golden')}` }
];

const state = new Map(variants.map(v=>[v.id,{done:0,stars:1}]));
let saved;
try { saved = new Set(JSON.parse(localStorage.getItem('king-leo-home-shortlist') || '[]').filter(x=>variants.some(v=>v.id===x))); } catch { saved = new Set(); }
let filter = 'all';
let current = 1;
let motion = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const gallery = document.querySelector('#gallery');
const dialog = document.querySelector('#inspector');
const timer = new WeakMap();
const sizeObserver = new ResizeObserver(entries=>entries.forEach(entry=>{
  const screen = entry.target.querySelector('.game-screen');
  if (screen) screen.style.setProperty('--scale', entry.contentRect.width / 360);
}));

function screenHTML(v) {
  return `<div class="phone-stage"><section class="game-screen v${v.id}" data-concept="${v.id}" aria-label="${v.name} interactive game mockup"><img class="scene" data-scene src="${A}castle-${state.get(v.id).done}.jpg" alt="The castle gates" draggable="false"><div class="scene-shade"></div>${v.render()}<div class="game-toast" role="status" aria-live="polite"></div><div class="sheet-veil" hidden></div><div class="game-sheet" hidden></div></section></div>`;
}
function renderGallery() {
  sizeObserver.disconnect();
  gallery.innerHTML = variants.map(v=>`<article class="concept-card" data-card="${v.id}"><header class="card-heading"><span class="concept-number">${String(v.id).padStart(2,'0')}</span><div><span class="concept-tag">${v.tag}</span><h2>${v.name}</h2></div><button class="save-button" data-save="${v.id}" aria-label="Shortlist ${v.name}" aria-pressed="${saved.has(v.id)}">${glyph('heart')}</button></header>${screenHTML(v)}<div class="card-info"><p>${v.note}</p><button class="inspect-button" data-inspect="${v.id}">Open preview <span>↗</span></button></div></article>`).join('');
  observeStages(); updateSaved(); applyFilter(); variants.forEach(v=>sync(v.id));
}
function observeStages() { document.querySelectorAll('.phone-stage').forEach(s=>sizeObserver.observe(s)); }
function applyFilter() {
  let count=0;
  document.querySelectorAll('[data-card]').forEach(card=>{
    const v=variants[Number(card.dataset.card)-1];
    const shown = filter==='all' || (filter==='saved' ? saved.has(v.id) : v.group===filter);
    card.hidden=!shown; if(shown)count++;
  });
  document.querySelector('#empty').hidden = count>0;
  document.querySelectorAll('.filter').forEach(button=>{
    button.classList.toggle('active',button.dataset.filter===filter);
    button.setAttribute('aria-pressed',button.dataset.filter===filter);
  });
}
function updateSaved() {
  document.querySelector('#saved-count').textContent=saved.size;
  document.querySelectorAll('[data-save]').forEach(button=>{
    const active=saved.has(Number(button.dataset.save));
    button.classList.toggle('saved',active);button.setAttribute('aria-pressed',active);
  });
  const button=document.querySelector('#inspector-save');
  button.innerHTML=`${glyph('heart')} ${saved.has(current)?'Saved to your shortlist':'Add to shortlist'}`;
  button.classList.toggle('saved',saved.has(current));
  button.setAttribute('aria-pressed',saved.has(current));
}
function toggleSaved(id) {
  saved.has(id)?saved.delete(id):saved.add(id);
  try {localStorage.setItem('king-leo-home-shortlist',JSON.stringify([...saved]));} catch {}
  updateSaved(); applyFilter();
  announce(saved.has(id)?`Concept ${String(id).padStart(2,'0')} added to your shortlist.`:`Concept ${String(id).padStart(2,'0')} removed from your shortlist.`);
}
function openPreview(id) {
  current=id; const v=variants[id-1];
  document.querySelector('#inspector-number').textContent=String(id).padStart(2,'0');
  document.querySelector('#inspector-title').textContent=v.name;
  document.querySelector('#inspector-description').textContent=v.note;
  document.querySelector('#inspector-details').innerHTML=`<dl><dt>The layout</dt><dd>${v.layout}</dd><dt>Why choose it</dt><dd>${v.strength}</dd><dt>Consider</dt><dd>${v.tradeoff}</dd></dl>`;
  const oldStage=document.querySelector('#inspector-phone .phone-stage');if(oldStage)sizeObserver.unobserve(oldStage);
  document.querySelector('#inspector-phone').innerHTML=screenHTML(v);
  document.querySelector('#position').textContent=`${id} / 10`;
  if(!dialog.open)dialog.showModal();
  document.body.classList.add('preview-open');
  observeStages();sync(id);updateSaved();
  history.replaceState(null,'',`#concept-${String(id).padStart(2,'0')}`);
}
function closePreview() {dialog.close();document.body.classList.remove('preview-open');history.replaceState(null,'',location.pathname+location.search);}
function announce(text) { const el=document.querySelector('#global-toast');el.textContent=text;el.classList.add('visible');clearTimeout(timer.get(el));timer.set(el,setTimeout(()=>el.classList.remove('visible'),2400)); }
function toast(screen,text) {const el=screen.querySelector('.game-toast');el.textContent=text;el.classList.add('visible');clearTimeout(timer.get(el));timer.set(el,setTimeout(()=>el.classList.remove('visible'),2600));}
const tasks=['Fix the broken gate','Hang the king’s flag','Light the lamps'];
function sync(id) {
  const s=state.get(id);
  document.querySelectorAll(`.game-screen[data-concept="${id}"]`).forEach(screen=>{
    screen.querySelector('[data-scene]').src=`${A}castle-${s.done}.jpg`;
    screen.querySelectorAll('[data-stars]').forEach(el=>el.textContent=s.stars);
    screen.querySelectorAll('.currency.star').forEach(el=>el.setAttribute('aria-label',`${s.stars} stars available`));
    screen.querySelectorAll('[data-done]').forEach(el=>el.textContent=s.done);
    screen.querySelectorAll('[data-progress]').forEach(el=>el.style.width=`${s.done/3*100}%`);
    screen.querySelectorAll('[data-pip]').forEach(el=>el.classList.toggle('done',Number(el.dataset.pip)<s.done));
    screen.querySelectorAll('[data-milestone]').forEach(el=>{const n=Number(el.dataset.milestone);el.classList.toggle('done',n<s.done);el.classList.toggle('next',n===s.done);});
    if(s.done)screen.querySelectorAll('[data-repair-label]').forEach(el=>el.textContent=tasks[s.done]||'Castle restored!');
    screen.querySelectorAll('[data-ready]').forEach(el=>el.textContent=s.stars?'One repair ready':'Play for a star');
  });
}
function showSheet(screen,title,content) {
  screen.querySelector('.sheet-veil').hidden=false;
  const sheet=screen.querySelector('.game-sheet');
  sheet.innerHTML=`<header><h3>${title}</h3><button data-action="close-sheet" aria-label="Close panel">×</button></header>${content}`;
  sheet.hidden=false;
  sheet.querySelector('button')?.focus({preventScroll:true});
}
function closeSheet(screen) {screen.querySelector('.sheet-veil').hidden=true;screen.querySelector('.game-sheet').hidden=true;}
function repairs(screen) {
  const s=state.get(Number(screen.dataset.concept));
  showSheet(screen,'A little love for your castle',`<p class="sheet-copy">Three small repairs. One beautiful welcome.</p><div class="repair-list">${tasks.map((text,i)=>`<div class="repair-item ${i<s.done?'complete':''}"><span class="task-symbol">${glyph(['hammer','flag','lamp'][i])}</span><span>${text}<small>${i<s.done?'Beautifully done':i===s.done?'Your next repair':'Coming next'}</small></span><button data-action="restore" ${i!==s.done||!s.stars?'disabled':''}>${i<s.done?'✓':img('icon_star')+' 1'}</button></div>`).join('')}</div><p class="sheet-foot">${s.stars?'You have 1 star ready to use.':'Win your next level to earn another star.'}</p>`);
}
function action(screen,key,button) {
  const id=Number(screen.dataset.concept);
  if(key==='play') {
    showSheet(screen,'Level 2',`${img('icon_star','level-star')}<h4 class="level-greeting">Your next star awaits!</h4><p class="sheet-copy">Play a puzzle and bring a little more life to your kingdom.</p><button class="sheet-primary" data-action="start-level">Let’s play ${glyph('arrow')}</button>`);
  } else if(key==='start-level') {closeSheet(screen);toast(screen,'Your next adventure: Level 2!');}
  else if(key==='repair') repairs(screen);
  else if(key==='restore') {
    const s=state.get(id);if(!s.stars||s.done>=3)return;
    s.stars--;s.done++;closeSheet(screen);sync(id);screen.classList.add('repaired');setTimeout(()=>screen.classList.remove('repaired'),1500);toast(screen,'Lovely! Your first repair is complete.');
  } else if(key==='close-sheet') closeSheet(screen);
  else if(key==='settings') showSheet(screen,'A little peace & quiet',`<p class="sheet-copy">Make yourself at home.</p><label class="setting-row">Music <input type="checkbox" checked role="switch"></label><label class="setting-row">Sound effects <input type="checkbox" checked role="switch"></label>`);
  else if(key==='nav') {
    const target=button.dataset.destination;
    if(target==='home'){closeSheet(screen);toast(screen,'Welcome home, Your Majesty.');return;}
    const s=state.get(id);
    if(target==='areas')showSheet(screen,'Your kingdom',`<div class="area-preview"><img src="${A}castle-${s.done}.jpg" alt="Castle Gates"><div><small>AREA 01</small><h4>Castle Gates</h4><p>${s.done} of 3 repairs complete</p></div></div><div class="locked-area">${glyph('flag')}<span>Outer Courtyard<small>Restore Castle Gates to unlock</small></span></div><button class="sheet-primary" data-action="repair">View repairs ${glyph('arrow')}</button>`);
    if(target==='leaderboard')showSheet(screen,'The royal ranks',`${img('nav_leaderboard','sheet-hero')}<h4 class="level-greeting">A place among the greats.</h4><p class="sheet-copy">Your best scores belong here. Keep playing and watch your kingdom rise.</p>`);
    if(target==='cards')showSheet(screen,'Your story collection',`<div class="card-collection">${img('nav_cards')}${img('nav_cards')}${img('nav_cards')}</div><h4 class="level-greeting">Every castle has a secret.</h4><p class="sheet-copy">Restore your kingdom to discover more of the story.</p>`);
  }
}
document.addEventListener('click',event=>{
  const button=event.target.closest('button');
  if(button?.dataset.filter){filter=button.dataset.filter;applyFilter();}
  if(button?.dataset.save)toggleSaved(Number(button.dataset.save));
  if(button?.dataset.inspect)openPreview(Number(button.dataset.inspect));
  const screen=event.target.closest('.game-screen');
  if(screen&&button?.dataset.action)action(screen,button.dataset.action,button);
  if(screen&&event.target.classList.contains('sheet-veil'))closeSheet(screen);
});
document.querySelector('#close-preview').addEventListener('click',closePreview);
dialog.addEventListener('click',event=>{if(event.target===dialog)closePreview();});
dialog.addEventListener('cancel',event=>{event.preventDefault();closePreview();});
document.querySelector('#previous').addEventListener('click',()=>openPreview(current===1?10:current-1));
document.querySelector('#next').addEventListener('click',()=>openPreview(current===10?1:current+1));
document.querySelector('#inspector-save').addEventListener('click',()=>toggleSaved(current));
document.addEventListener('keydown',event=>{if(dialog.open && !['INPUT','SELECT','TEXTAREA'].includes(event.target.tagName)){if(event.key==='ArrowLeft')openPreview(current===1?10:current-1);if(event.key==='ArrowRight')openPreview(current===10?1:current+1);}});
function setMotion() {document.documentElement.classList.toggle('no-motion',!motion);const b=document.querySelector('#motion');b.setAttribute('aria-pressed',motion);b.querySelector('span').textContent=motion?'on':'off';}
document.querySelector('#motion').addEventListener('click',()=>{motion=!motion;setMotion();});
document.querySelector('#reset').addEventListener('click',()=>{variants.forEach(v=>state.set(v.id,{done:0,stars:1}));renderGallery();if(dialog.open)openPreview(current);announce('All castle demos reset. Your shortlist is saved.');});
setMotion();renderGallery();
const linked=location.hash.match(/^#concept-(\d{1,2})$/);if(linked&&Number(linked[1])>=1&&Number(linked[1])<=10)openPreview(Number(linked[1]));
