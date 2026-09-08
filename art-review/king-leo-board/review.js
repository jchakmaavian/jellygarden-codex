(() => {
  'use strict';
  const {assets, themes, version} = window.REVIEW_DATA;
  const byId = Object.fromEntries(assets.map(a => [a.id, a]));
  const $ = selector => document.querySelector(selector);
  const storageKey = 'king-leo-board-review-v1';
  let notes = {assets: {}, overall: ''};
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey));
    if (saved && typeof saved === 'object' && saved.assets && typeof saved.assets === 'object') {
      for (const a of assets) {
        const value = saved.assets[a.id];
        if (!value || typeof value !== 'object') continue;
        notes.assets[a.id] = {
          verdict: ['Keep', 'Change', 'Undecided'].includes(value.verdict) ? value.verdict : 'Undecided',
          note: typeof value.note === 'string' ? value.note : ''
        };
      }
      notes.overall = typeof saved.overall === 'string' ? saved.overall : '';
    }
  } catch (_) {
    $('#storage-status').textContent = 'Browser storage is unavailable. Export your notes before closing this page.';
  }
  const pathFor = (id, proposed) => proposed ? `candidates/${version}/${id}.png` : `current/${id}.png`;
  const entryFor = id => notes.assets[id] ||= {verdict: 'Undecided', note: ''};
  const updateProgress = () => {
    const reviewed = assets.filter(a => ['Keep', 'Change'].includes(entryFor(a.id).verdict)).length;
    $('#review-progress').textContent = `${reviewed} of ${assets.length} candidates reviewed`;
  };
  function save() {
    try { localStorage.setItem(storageKey, JSON.stringify(notes)); }
    catch (_) { $('#storage-status').textContent = 'Notes are held in this tab only. Export them before closing.'; }
    updateProgress();
  }

  for (const theme of themes) {
    const option = document.createElement('option');
    option.value = theme.themeKey;
    option.textContent = theme.themeKey.charAt(0).toUpperCase() + theme.themeKey.slice(1);
    $('#arc').append(option);
  }
  function boardCells(proposed) {
    const cols = $('#layout').value === 'dense' ? 9 : 7;
    const normals = ['book', 'shield', 'crown', 'gem', 'leaf', 'coin'];
    const special = cols === 9 ? {13:'rocket-h',30:'bomb',51:'orb',64:'rocket-v'} : {9:'rocket-h',24:'bomb',32:'orb',38:'rocket-v'};
    const blockers = cols === 9 ? {22:'wood',46:'block',48:'wire',65:'stone'} : {16:'wood',28:'block',29:'wire',40:'stone'};
    const holes = $('#layout').value === 'irregular' ? new Set([0,1,5,6,7,13,35,41,42,43,47,48]) : new Set();
    let cells = '';
    let seed = 1709;
    const identities = [];
    for (let row = 0; row < cols; row++) {
      for (let col = 0; col < cols; col++) {
        const index = row * cols + col;
        if (holes.has(index)) { cells += '<div class="cell hole" aria-hidden="true"></div>'; continue; }
        seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
        let normal = seed % normals.length;
        while ((col >= 2 && identities[index-1] === normals[normal] && identities[index-2] === normals[normal]) ||
               (row >= 2 && identities[index-cols] === normals[normal] && identities[index-cols*2] === normals[normal])) {
          normal = (normal + 1) % normals.length;
        }
        const id = special[index] || normals[normal];
        identities[index] = id;
        const blocker = blockers[index];
        cells += `<div class="cell">${['wood','stone'].includes(blocker) ? '' : `<img src="${pathFor(id,proposed)}" alt="${byId[id].name}">`}${blocker ? `<img class="obstacle" src="context/${blocker}.png" alt="Existing ${blocker} blocker">` : ''}</div>`;
      }
    }
    return `<div class="board-grid" style="--cols:${cols}" aria-label="${cols} by ${cols} sample board">${cells}</div>`;
  }
  function hud(proposed) {
    return `<div class="hud"><img class="hud-frame" src="context/frame.png" alt="King Leo HUD frame"><div class="hud-portrait"><img src="context/leo.png" alt="King Leo"></div><div class="hud-left"><div class="hud-title">TARGETS</div><div class="hud-targets"><div><img src="${pathFor('crown',proposed)}" alt="Crown target"><span>12</span></div><div><img src="${pathFor('book',proposed)}" alt="Book target"><span>18</span></div></div></div><div class="hud-right"><div class="hud-title">MOVES</div><div class="hud-moves">24</div></div></div>`;
  }
  function renderBoards() {
    $('#board-pair').innerHTML = [false,true].map(proposed => `<article class="board-panel"><div class="panel-label"><strong>${proposed ? 'King Leo · installed' : 'Before installation'}</strong><span class="pill">${proposed ? 'INSTALLED V1' : 'ORIGINAL SNAPSHOT'}</span></div><div class="phone-wrap"><div class="phone">${hud(proposed)}${boardCells(proposed)}<div class="phone-bottom"><div class="tool-preview"><img src="${pathFor('glove',proposed)}" alt="${proposed?'Proposed royal swap glove':'Current glove button'}"></div></div></div></div><div class="panel-foot">${proposed ? 'Warm gold · softened forms · jewel colours' : 'Existing pieces, power-ups, and glove button'}</div></article>`).join('');
    updateContext();
  }
  function updateContext() {
    const theme = themes.find(t => t.themeKey === $('#arc').value) || themes[0];
    document.body.dataset.surface = $('#surface').value;
    document.body.classList.toggle('is-grayscale', $('#grayscale').checked);
    const fill = theme.boardFill || [0.85,0.74,0.55,0.85];
    document.documentElement.style.setProperty('--arc-fill', `rgba(${fill.slice(0,3).map(c => Math.round(c*255)).join(',')},${fill[3] ?? 1})`);
    for (const phone of document.querySelectorAll('.phone')) phone.style.backgroundImage = `url('context/${theme.themeKey}.jpg')`;
  }
  function renderCards() {
    $('#asset-grid').innerHTML = assets.map((a,index) => `<article class="asset-card" data-id="${a.id}" data-category="${a.category}"><div class="card-top"><span class="card-id">${String(index+1).padStart(2,'0')} / ${a.category.toUpperCase()}</span><h3>${a.name}</h3><p class="card-role">${a.role}</p></div><div class="image-pair"><div class="asset-stage"><img src="${pathFor(a.id,false)}" alt="Current ${a.name}"><span>Before${a.id==='glove'?' · button':''}</span></div><button class="asset-stage candidate" data-detail="${a.id}" aria-label="Enlarge candidate ${a.name}"><img src="${pathFor(a.id,true)}" alt="Candidate ${a.name}"><span>Approved v1 ↗</span></button></div><div class="size-strip" aria-label="Candidate at phone sizes">${[24,32,48,64].map(size=>`<div class="size-item"><img src="${pathFor(a.id,true)}" width="${size}" height="${size}" alt="${a.name} at ${size} pixels"><small>${size} px</small></div>`).join('')}</div><div class="card-footer"><div class="verdict" role="group" aria-label="Review ${a.name}">${['Keep','Change','Undecided'].map(v=>`<button data-verdict="${v}" aria-pressed="false">${v==='Keep'?'✓ ':v==='Change'?'↺ ':''}${v}</button>`).join('')}</div><textarea aria-label="Notes for ${a.name}" placeholder="What would you keep or change?"></textarea><details><summary>Asset files & role</summary><p>${a.role}${a.id==='glove'?'. Current image includes its button plate; the candidate is a separate icon.':''}</p><code>Assets/JellyGarden/${a.current}</code><a class="file-link" href="${pathFor(a.id,true)}" download="${a.id}-v1.png">Candidate PNG ↓</a><a class="file-link" href="candidates/v1/exports-128/${a.id}.png" download="${a.id}-128.png">128 px preview export ↓</a></details></div></article>`).join('');
    for (const card of document.querySelectorAll('.asset-card')) {
      const id = card.dataset.id;
      const textarea = card.querySelector('textarea');
      textarea.value = entryFor(id).note;
      textarea.addEventListener('input',()=>{entryFor(id).note=textarea.value;save();});
      for (const button of card.querySelectorAll('[data-verdict]')) {
        button.addEventListener('click',()=>{entryFor(id).verdict=button.dataset.verdict;paintVerdicts(card);save();});
      }
      paintVerdicts(card);
    }
  }
  function paintVerdicts(card) {
    for (const button of card.querySelectorAll('[data-verdict]')) {
      const selected = button.dataset.verdict === entryFor(card.dataset.id).verdict;
      button.classList.toggle('selected',selected);
      button.setAttribute('aria-pressed',String(selected));
    }
  }
  function renderBoosters() {
    $('#booster-comparison').innerHTML = [['rocket-h','stripes','Rocket'],['bomb','packages','Area bomb'],['orb','colorbomb','Prism orb']].map(([id,current,title])=>`<article class="booster-card"><h3>${title}</h3><div class="booster-pair"><img src="context/booster-${current}.png" alt="Current ${title} pre-level icon"><span aria-hidden="true">→</span><img src="${pathFor(id,true)}" alt="Proposed ${title} icon"></div><p>Current pre-level badge → proposed icon</p></article>`).join('');
  }
  $('#arc').addEventListener('change',updateContext);
  $('#surface').addEventListener('change',updateContext);
  $('#grayscale').addEventListener('change',updateContext);
  $('#layout').addEventListener('change',renderBoards);
  for (const button of document.querySelectorAll('.tabs button')) {
    button.addEventListener('click',()=>{
      for(const tab of document.querySelectorAll('.tabs button')){tab.classList.toggle('active',tab===button);tab.setAttribute('aria-pressed',String(tab===button));}
      for(const card of document.querySelectorAll('.asset-card'))card.hidden=button.dataset.category!=='All'&&card.dataset.category!==button.dataset.category;
    });
  }
  $('#asset-grid').addEventListener('click',event=>{
    const button=event.target.closest('[data-detail]');if(!button)return;
    const a=byId[button.dataset.detail];
    $('#dialog-title').textContent=a.name+' · v1';
    $('#dialog-image').src=pathFor(a.id,true);$('#dialog-image').alt=a.name+' candidate master';
    $('#download-master').href=pathFor(a.id,true);$('#download-master').download=a.id+'-v1.png';
    $('#asset-dialog').showModal();
  });
  $('#close-dialog').addEventListener('click',()=>$('#asset-dialog').close());
  $('#asset-dialog').addEventListener('click',e=>{if(e.target===$('#asset-dialog')){const r=e.target.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)e.target.close();}});
  $('#overall-notes').value=notes.overall;
  $('#overall-notes').addEventListener('input',()=>{notes.overall=$('#overall-notes').value;save();});
  $('#export-review').addEventListener('click',()=>{
    const lines=['# King Leo board artwork review',`Version: ${version}`,`Exported: ${new Date().toISOString()}`,'','## Overall direction',notes.overall||'No overall notes yet.',''];
    for(const a of assets){const e=entryFor(a.id);lines.push(`## ${a.name} (${a.id})`,`Choice: ${e.verdict}`,`Role: ${a.role}`,`Candidate: candidates/${version}/${a.id}.png`,e.note||'No notes.','');}
    lines.push('These are owner review notes. Exporting does not install or approve artwork automatically.');
    const url=URL.createObjectURL(new Blob([lines.join('\n')],{type:'text/markdown;charset=utf-8'}));
    const link=document.createElement('a');link.href=url;link.download=`king-leo-art-review-${version}.md`;document.body.append(link);link.click();link.remove();setTimeout(()=>URL.revokeObjectURL(url),1000);
  });
  $('#reset-review').addEventListener('click',()=>{
    if(!confirm('Clear all review choices and notes in this browser? Export a copy first if you need to keep them.'))return;
    notes={assets:{},overall:''};$('#overall-notes').value='';
    for(const card of document.querySelectorAll('.asset-card')){card.querySelector('textarea').value='';paintVerdicts(card);}save();
  });
  renderBoards();renderCards();renderBoosters();updateProgress();
})();
