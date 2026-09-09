(() => {
  'use strict';
  const data = window.KING_LEO_AREAS;
  const byId = id => document.getElementById(id);
  const params = new URLSearchParams(location.hash.slice(1));
  let active = data.areas.find(a => a.id === Number(params.get('area'))) || data.areas[0];
  let stage = Math.max(0, Math.min(Number(params.get('stage')) || 0, active.tasks.length));
  let playback;
  const previousFrame = document.createElement('img');
  previousFrame.className = 'previous-stage'; previousFrame.alt = ''; previousFrame.hidden = true;
  byId('after').before(previousFrame);
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
  const buttons = new Map();
  byId('readyCount').textContent = data.areas.reduce((n, a) => n + a.states.length, 0);
  for (const area of data.areas) {
    const button = document.createElement('button');
    const number = document.createElement('small'); number.textContent = String(area.id).padStart(2, '0');
    const label = document.createElement('span'); label.textContent = area.name;
    button.append(number, label);
    button.onclick = () => selectArea(area);
    byId('areaList').append(button); buttons.set(area.id, button);
    const card = document.createElement('button');
    const thumb = document.createElement('div'); thumb.className = 'thumb';
    const image = document.createElement('img'); image.loading = 'lazy'; image.alt = area.name;
    image.src = area.states.length ? area.states[area.states.length - 1].path : area.before;
    thumb.append(image);
    const title = document.createElement('strong'); title.textContent = String(area.id).padStart(2, '0') + ' · ' + area.name;
    const count = document.createElement('small'); count.textContent = area.states.length + ' / ' + (area.tasks.length + 1) + ' stages';
    card.append(thumb, title, count); card.onclick = () => { selectArea(area); document.querySelector('.reviewer').scrollIntoView({block:'start'}); };
    byId('overview').append(card);
  }
  function stopPlayback() {
    clearInterval(playback); playback = null;
    byId('play').textContent = 'Play repairs'; byId('play').setAttribute('aria-pressed', 'false');
  }
  function selectArea(area) { stopPlayback(); active = area; stage = 0; byId('after').removeAttribute('src'); render(); }
  function render() {
    byId('areaNumber').textContent = 'AREA ' + String(active.id).padStart(2, '0');
    byId('areaTitle').textContent = active.name;
    byId('areaGoal').textContent = active.goal;
    for (const [id, button] of buttons) button.setAttribute('aria-current', String(id === active.id));
    byId('before').src = active.before;
    byId('before').alt = active.name + ' previous artwork';
    byId('beforeNote').textContent = active.beforeLabel;
    const current = active.states.find(s => s.state === stage);
    byId('pending').hidden = !!current;
    byId('after').hidden = !current;
    previousFrame.hidden = true;
    if (current) {
      const after = byId('after'), oldPath = after.getAttribute('src');
      if (oldPath !== current.path) {
        after.getAnimations().forEach(animation => animation.cancel());
        if (oldPath && !reducedMotion) { previousFrame.src = oldPath; previousFrame.hidden = false; }
        after.onload = () => { if (!previousFrame.hidden) after.animate([{opacity:0},{opacity:1}], {duration:650}); };
        after.src = current.path;
      }
      byId('fullImage').href = current.path;
      const next = active.states.find(s => s.state === stage + 1);
      if (next) { const preload = new Image(); preload.src = next.path; }
    }
    else { byId('after').removeAttribute('src'); byId('fullImage').removeAttribute('href'); }
    byId('after').alt = active.name + ' — ' + (stage ? active.tasks[stage - 1].label : 'before repairs');
    byId('stageNumber').textContent = stage ? 'REPAIR ' + stage + ' OF ' + active.tasks.length : 'THE STARTING SCENE';
    byId('stageTitle').textContent = stage ? active.tasks[stage - 1].label : 'Before repairs';
    byId('previous').disabled = stage === 0; byId('next').disabled = stage === active.tasks.length;
    byId('play').disabled = active.states.length < 2;
    byId('stages').replaceChildren();
    for (let i = 0; i <= active.tasks.length; i++) {
      const button = document.createElement('button');
      button.textContent = i ? String(i) : 'Before';
      button.title = i ? active.tasks[i - 1].label : 'Before repairs';
      button.setAttribute('aria-pressed', String(i === stage));
      if (!active.states.some(s => s.state === i)) button.className = 'pending';
      button.onclick = () => { stopPlayback(); stage = i; render(); };
      byId('stages').append(button);
    }
    history.replaceState(null, '', '#area=' + active.id + '&stage=' + stage);
  }
  byId('previous').onclick = () => { stopPlayback(); if (stage > 0) { stage--; render(); } };
  byId('next').onclick = () => { stopPlayback(); if (stage < active.tasks.length) { stage++; render(); } };
  byId('play').onclick = () => {
    if (playback) { stopPlayback(); return; }
    stage = 0; render();
    byId('play').textContent = 'Pause'; byId('play').setAttribute('aria-pressed', 'true');
    playback = setInterval(() => {
      if (!active.states.some(s => s.state === stage + 1)) { stopPlayback(); return; }
      stage++; render();
      if (stage === active.tasks.length) stopPlayback();
    }, 2600);
  };
  document.addEventListener('visibilitychange', () => { if (document.hidden) stopPlayback(); });
  byId('compare').onclick = () => {
    const solo = byId('pictures').classList.toggle('solo');
    byId('compare').setAttribute('aria-pressed', String(!solo));
    byId('compare').textContent = solo ? 'Compare original' : 'Focus on new art';
  };
  byId('areaSearch').oninput = event => {
    const query = event.target.value.toLowerCase().trim();
    for (const area of data.areas) buttons.get(area.id).hidden = !(area.name.toLowerCase().includes(query) || String(area.id).includes(query));
  };
  render();
})();
