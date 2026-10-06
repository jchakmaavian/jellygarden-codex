// Offline API/timeline smoke check. This does not substitute for browser or Unity visual review.
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const root=__dirname,html=fs.readFileSync(path.join(root,'preview.html'),'utf8'),css=fs.readFileSync(path.join(root,'preview.css'),'utf8');
for(const match of [...html.matchAll(/(?:src|href)="([^"]+)"/g),...css.matchAll(/url\('([^']+)'\)/g)])assert(fs.existsSync(path.join(root,match[1])),`Missing asset: ${match[1]}`);
assert(!/class="(?:reward|balance|flying-coins)"/.test(html),'Popup must not contain a reward badge, top balance, or flights');
const elements=new Map();
function element(){return {style:{},children:[],classList:{add(){},toggle(){}},clientWidth:450,appendChild(e){this.children.push(e)},addEventListener(){},getAttribute(name){return this[name]},getContext(){return {createImageData(w,h){return {data:new Uint8ClampedArray(w*h*4)}},putImageData(){},createRadialGradient(){return {addColorStop(){}}},beginPath(){},moveTo(){},lineTo(){},closePath(){},fill(){}}},toDataURL(){return 'data:image/png;base64,'},decode(){return Promise.resolve()}}}
global.window=global;global.location={search:'?t=0'};global.ResizeObserver=class{observe(){}};
global.Image=class{decode(){return Promise.resolve()}};global.requestAnimationFrame=()=>1;global.cancelAnimationFrame=()=>{};
global.document={fonts:{ready:Promise.resolve()},body:element(),createElement:element,querySelectorAll:()=>[],querySelector(s){
  if(!elements.has(s)){const token=s.slice(1).split(' ')[0];assert(s.startsWith('.')?new RegExp(`class="[^"]*\\b${token}\\b`).test(html):html.includes(`id="${token}"`),`Missing DOM target ${s}`);elements.set(s,element())}return elements.get(s)
}};
vm.runInThisContext(fs.readFileSync(path.join(root,'config.js'),'utf8'));
vm.runInThisContext(fs.readFileSync(path.join(root,'preview.js'),'utf8'));
previewReady.then(()=>{
  assert.equal(preview.seek(0).cardVisible,true);assert.equal(preview.state().starScale,2.4);
  assert.equal(preview.seek(4.49).homeVisible,false);assert.equal(preview.state().popupCoinFlights,0);
  assert.equal(preview.seek(4.5).homeVisible,true);assert.equal(preview.state().balance,70);
  assert.equal(preview.seek(4.9).starInFlight,true);assert.equal(preview.state().flightVisible,0);
  assert.equal(preview.seek(5.74).starInFlight,true);assert(preview.state().flightVisible>0,'Star and coins overlap');
  assert.equal(preview.seek(5.84).stars,5);assert.equal(preview.state().landed,0);
  assert.equal(preview.seek(6.86).landed,1);assert.equal(preview.state().balance,76);
  assert.equal(preview.seek(7.85).landed,10);assert.equal(preview.state().balance,135);
  for(let i=0;i<=600;i++){const s=preview.seek(i/60);assert(s.minCoinMargin===null||s.minCoinMargin>=0,`Coin out of frame at ${s.time}`)}
  preview.setReduced(true);const reduced=preview.seek(4.5);assert.equal(reduced.balance,135);assert.equal(reduced.stars,5);assert.equal(reduced.flightVisible,0);assert.equal(reduced.starInFlight,false);
  preview.setReduced(false);preview.seek(0);assert.equal(preview.state().balance,70);preview.continue();assert.equal(preview.state().homeVisible,true);preview.seek(0);
  console.log('PASS: assets, DOM targets, full timeline, overlapping Home rewards, exact count-up, reduced motion, replay, Continue, and 601 containment samples.');
}).catch(error=>{console.error(error);process.exitCode=1});
