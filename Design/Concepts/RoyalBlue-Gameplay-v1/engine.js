(function(global){
'use strict';
const COLORS=['ruby','shield','crown','leaf','lily'];
const POWER=['rocket','tnt','helicopter','orb'];
const copy=x=>JSON.parse(JSON.stringify(x));
class RoyalEngine{
 constructor(seed=713){this.seed=seed>>>0;this.serial=0;this.reset();}
 rand(){this.seed=(Math.imul(1664525,this.seed)+1013904223)>>>0;return this.seed/4294967296;}
 piece(kind,hp=0,axis='h'){return{id:++this.serial,kind,hp,axis};}
 normal(p){return p&&COLORS.includes(p.kind);}
 power(p){return p&&POWER.includes(p.kind);}
 blocker(p){return p&&p.hp>0;}
 adjacent(a,b){return Math.abs(a%8-b%8)+Math.abs(Math.floor(a/8)-Math.floor(b/8))===1;}
 neighbors(i){return [i-8,i+8,i%8?i-1:-1,i%8<7?i+1:-1].filter(n=>n>=0&&n<64);}
 snapshot(){return{board:copy(this.board),goals:copy(this.goals),moves:this.moves,status:this.status,tools:copy(this.tools)};}
 record(type,details={}){this.steps.push({type,...details,...this.snapshot()});}
 reset(){
  this.moves=24;this.status='playing';this.goals={crate:12,vase:4};this.tools={hammer:3,row:1,column:1,shuffle:1};this.steps=[];
  this.board=Array.from({length:64},()=>this.piece(COLORS[Math.floor(this.rand()*5)]));
  for(let r=6;r<8;r++)for(let c=1;c<7;c++)this.board[r*8+c]=this.piece('crate',(r+c)%2?1:2);
  for(const i of [32,39,40,47])this.board[i]=this.piece('vase',1);
  this.removeInitialMatches();this.ensurePlayable();return this.snapshot();
 }
 groups(){
  const matches=[];
  for(let direction=0;direction<2;direction++)for(let line=0;line<8;line++){
   let start=0;while(start<8){const a=direction?start*8+line:line*8+start,p=this.board[a];let end=start+1;
    if(this.normal(p))while(end<8){const b=direction?end*8+line:line*8+end;if(this.board[b]?.kind!==p.kind)break;end++;}
    if(end-start>=3)matches.push({cells:Array.from({length:end-start},(_,k)=>direction?(start+k)*8+line:line*8+start+k),runs:[{length:end-start,axis:direction?'v':'h'}],square:false});
    start=end;
   }
  }
  for(let r=0;r<7;r++)for(let c=0;c<7;c++){const i=r*8+c,p=this.board[i],cells=[i,i+1,i+8,i+9];if(this.normal(p)&&cells.every(n=>this.board[n]?.kind===p.kind))matches.push({cells,runs:[],square:true});}
  const merged=[];for(const g of matches){let current=g;for(let j=merged.length-1;j>=0;j--)if(merged[j].cells.some(i=>current.cells.includes(i))){const o=merged.splice(j,1)[0];current={cells:[...new Set([...current.cells,...o.cells])],runs:[...current.runs,...o.runs],square:current.square||o.square};j=merged.length; }merged.push(current);}
  return merged;
 }
 creation(g){
  if(g.runs.some(r=>r.length>=5))return{kind:'orb'};
  if(g.runs.some(r=>r.axis==='h')&&g.runs.some(r=>r.axis==='v'))return{kind:'tnt'};
  const line=g.runs.find(r=>r.length>=4);if(line)return{kind:'rocket',axis:line.axis};
  if(g.square)return{kind:'helicopter'};return null;
 }
 removeInitialMatches(){for(let t=0;t<1000;t++){const groups=this.groups();if(!groups.length)return;for(const g of groups)for(const i of g.cells)this.board[i]=this.piece(COLORS[Math.floor(this.rand()*5)]);}throw Error('Unable to initialize stable board');}
 legalMoves(){
  const valid=[];for(let a=0;a<64;a++){if(this.blocker(this.board[a]))continue;if(this.power(this.board[a]))valid.push([a,a]);for(const b of [a%8<7?a+1:-1,a+8<64?a+8:-1]){if(b<0||this.blocker(this.board[b]))continue;if(this.power(this.board[a])||this.power(this.board[b])){valid.push([a,b]);continue;}[this.board[a],this.board[b]]=[this.board[b],this.board[a]];const match=this.groups().some(g=>g.cells.includes(a)||g.cells.includes(b));[this.board[a],this.board[b]]=[this.board[b],this.board[a]];if(match)valid.push([a,b]);}}return valid;
 }
 shuffle(){
  const indices=this.board.map((p,i)=>this.normal(p)?i:-1).filter(i=>i>=0),pieces=indices.map(i=>this.board[i]);
  for(let attempt=0;attempt<500;attempt++){for(let i=pieces.length-1;i>0;i--){const j=Math.floor(this.rand()*(i+1));[pieces[i],pieces[j]]=[pieces[j],pieces[i]];}indices.forEach((i,k)=>this.board[i]=pieces[k]);if(!this.groups().length&&this.legalMoves().length)return;}
  this.removeInitialMatches();if(!this.legalMoves().length)throw Error('No playable shuffle');
 }
 ensurePlayable(){if(this.status==='playing'&&!this.legalMoves().length){this.shuffle();this.record('shuffle');}}
 area(i,radius){const r=Math.floor(i/8),c=i%8,out=[];for(let y=Math.max(0,r-radius);y<=Math.min(7,r+radius);y++)for(let x=Math.max(0,c-radius);x<=Math.min(7,c+radius);x++)out.push(y*8+x);return out;}
 line(i,axis){return Array.from({length:8},(_,n)=>axis==='h'?Math.floor(i/8)*8+n:n*8+i%8);}
 commonColor(){const counts=COLORS.map(k=>this.board.filter(p=>p?.kind===k).length);return COLORS[counts.indexOf(Math.max(...counts))];}
 targets(excluded=new Set(),count=1){
  const ranked=this.board.map((p,i)=>({p,i,rank:this.blocker(p)?(p.hp===1?100:80)+this.neighbors(i).filter(n=>this.blocker(this.board[n])).length:0})).filter(x=>x.p&&!excluded.has(x.i)&&!this.power(x.p)).sort((a,b)=>b.rank-a.rank||a.i-b.i);
  return ranked.slice(0,count).map(x=>x.i);
 }
 expand(initial,forced=[],orbColor=null){
  const clear=new Set(initial),fired=new Set(),effects=[];const queue=[...forced];
  for(const i of clear)if(this.power(this.board[i]))queue.push({i,p:copy(this.board[i])});
  while(queue.length){const {i,p,payload,targetCount}=queue.shift();if(fired.has(i))continue;fired.add(i);let cells=[];let targets=[];
   if(p.kind==='rocket')cells=this.line(i,p.axis);
   else if(p.kind==='tnt')cells=this.area(i,2);
   else if(p.kind==='orb'){const kind=orbColor||this.commonColor();cells=this.board.map((q,j)=>q?.kind===kind?j:-1).filter(j=>j>=0);}
   else if(p.kind==='helicopter'){targets=this.targets(new Set([...clear,i,...this.neighbors(i)]),targetCount||1);cells=[i,...this.neighbors(i)];for(const target of targets){if(payload?.kind==='rocket')cells.push(...this.line(target,payload.axis));else if(payload?.kind==='tnt')cells.push(...this.area(target,2));else cells.push(target);}}
   cells.push(i);effects.push({kind:p.kind,i,axis:p.axis,targets,payload});
   for(const n of cells){clear.add(n);if(this.power(this.board[n])&&!fired.has(n))queue.push({i:n,p:copy(this.board[n])});}
  }
  return{clear:[...clear],effects};
 }
 damage(indices,adjacent=false,protect=[]){
  const clear=new Set(indices),protectedSet=new Set(protect),hits=new Set();
  if(adjacent)for(const i of clear)for(const n of this.neighbors(i))if(this.blocker(this.board[n]))hits.add(n);
  for(const i of clear)hits.add(i);const removed=[],damaged=[],collected=[];
  for(const i of hits){const p=this.board[i];if(!p||protectedSet.has(i))continue;
   if(this.blocker(p)){p.hp--;if(p.hp>0){damaged.push(i);continue;}if(this.goals[p.kind]>0)this.goals[p.kind]--;collected.push({i,kind:p.kind});}
   else if(!clear.has(i))continue;
   removed.push({i,p:copy(p)});this.board[i]=null;
  }
  this.record('clear',{removed,damaged,collected});
 }
 fall(){
  for(let c=0;c<8;c++){let bottom=7;while(bottom>=0){if(this.blocker(this.board[bottom*8+c])){bottom--;continue;}let top=bottom;while(top>0&&!this.blocker(this.board[(top-1)*8+c]))top--;
   const items=[];for(let r=bottom;r>=top;r--)if(this.board[r*8+c])items.push(this.board[r*8+c]);let k=0;for(let r=bottom;r>=top;r--)this.board[r*8+c]=items[k++]||this.piece(COLORS[Math.floor(this.rand()*5)]);bottom=top-1;
  }}this.record('fall');
 }
 resolve(preferred=[]){
  for(let cascade=0;cascade<100;cascade++){
   const groups=this.groups();if(!groups.length)return;
   const creations=[],clear=[];
   for(const g of groups){const special=this.creation(g);let at=preferred.find(i=>g.cells.includes(i));if(at===undefined)at=g.cells[Math.floor(g.cells.length/2)];clear.push(...g.cells);if(special)creations.push({i:at,...special});}
   this.record('match',{cells:clear,creations,cascade});this.damage(clear,true,creations.map(x=>x.i));
   for(const s of creations)this.board[s.i]=this.piece(s.kind,0,s.axis||'h');if(creations.length)this.record('create',{creations});
   this.fall();preferred=[];
  }throw Error('Cascade safety limit reached');
 }
 combo(a,b,pa,pb){
  const origin=b;let initial=[a,b],forced=[],effects=[];const kinds=[pa.kind,pb.kind];
  if(kinds.every(k=>k==='orb')){initial=Array.from({length:64},(_,i)=>i);effects=[{kind:'double-orb',i:origin}];}
  else if(kinds.includes('orb')){const payload=pa.kind==='orb'?pb:pa,color=this.commonColor(),targets=[];for(let i=0;i<64;i++)if(this.board[i]?.kind===color){this.board[i]=this.piece(payload.kind,0,i%2?'v':'h');targets.push(i);forced.push({i,p:copy(this.board[i])});}effects=[{kind:'transform',i:origin,targets,payload}];this.record('transform',{effects});}
  else if(kinds.includes('helicopter')){const payload=pa.kind==='helicopter'?pb:pa;forced=[{i:origin,p:{kind:'helicopter'},targetCount:payload.kind==='helicopter'?3:1,payload:payload.kind==='helicopter'?null:payload}];}
  else if(kinds.every(k=>k==='tnt')){initial.push(...this.area(origin,4));effects=[{kind:'big-tnt',i:origin}];}
  else if(kinds.includes('tnt')){for(let o=-1;o<=1;o++){if(Math.floor(origin/8)+o>=0&&Math.floor(origin/8)+o<8)initial.push(...this.line(origin+o*8,'h'));if(origin%8+o>=0&&origin%8+o<8)initial.push(...this.line(origin+o,'v'));}effects=[{kind:'cross3',i:origin}];}
  else{initial.push(...this.line(origin,'h'),...this.line(origin,'v'));effects=[{kind:'cross',i:origin}];}
  this.board[a]=null;this.board[b]=null;
  const expanded=this.expand(initial,forced);this.record('blast',{effects:[...effects,...expanded.effects],cells:expanded.clear,combo:kinds});this.damage(expanded.clear);
 }
 act(a,b=a){
  this.steps=[];if(this.status!=='playing'||a<0||b<0||a>=64||b>=64||this.blocker(this.board[a])||this.blocker(this.board[b])||(a!==b&&!this.adjacent(a,b)))return{accepted:false,steps:[]};
  const pa=copy(this.board[a]),pb=copy(this.board[b]);if(a===b&&!this.power(pa))return{accepted:false,steps:[]};
  if(a!==b){[this.board[a],this.board[b]]=[this.board[b],this.board[a]];this.record('swap',{a,b});}
  if(!this.power(pa)&&!this.power(pb)&&!this.groups().some(g=>g.cells.includes(a)||g.cells.includes(b))){[this.board[a],this.board[b]]=[this.board[b],this.board[a]];this.record('invalid',{a,b});return{accepted:false,steps:this.steps};}
  this.moves--;this.record('spend');
  if(a!==b&&this.power(pa)&&this.power(pb)){this.combo(a,b,pa,pb);this.fall();this.resolve();}
  else if(this.power(pa)||this.power(pb)){
   const i=this.power(pa)?b:a,p=this.power(pa)?pa:pb,other=this.power(pa)?pb:pa;const expand=this.expand([i],[],p.kind==='orb'&&this.normal(other)?other.kind:null);this.record('blast',{effects:expand.effects,cells:expand.clear});this.damage(expand.clear);this.fall();this.resolve();
  }else this.resolve([b,a]);
  this.finish();return{accepted:true,steps:this.steps};
 }
 finish(){if(Object.values(this.goals).every(n=>n<=0))this.status='won';else if(this.moves<=0)this.status='lost';else this.ensurePlayable();this.record('end');}
 useTool(tool,i){
  this.steps=[];if(this.status!=='playing'||!this.tools[tool]||i<0||i>=64)return{accepted:false,steps:[]};this.tools[tool]--;
  if(tool==='shuffle'){this.shuffle();this.record('shuffle');}
  else{const initial=tool==='row'?this.line(i,'h'):tool==='column'?this.line(i,'v'):[i],expand=this.expand(initial);this.record('blast',{effects:[{kind:tool,i},...expand.effects],cells:expand.clear});this.damage(expand.clear);this.fall();this.resolve();}
  this.finish();return{accepted:true,steps:this.steps};
 }
 scenario(name){
  this.reset();this.steps=[];this.moves=24;this.status='playing';
  const map={rocket:['rocket'],tnt:['tnt'],helicopter:['helicopter'],orb:['orb'],'rocket-rocket':['rocket','rocket'],'rocket-tnt':['rocket','tnt'],'tnt-tnt':['tnt','tnt'],'helicopter-tnt':['helicopter','tnt'],'helicopter-rocket':['helicopter','rocket'],'helicopter-helicopter':['helicopter','helicopter'],'orb-tnt':['orb','tnt'],'orb-rocket':['orb','rocket'],'orb-helicopter':['orb','helicopter'],'orb-orb':['orb','orb']};
  if(map[name]){map[name].forEach((kind,n)=>this.board[27+n]=this.piece(kind,0,n?'v':'h'));return{focus:map[name].length===1?[27]:[27,28]};}
  if(name==='low-moves')this.moves=3;
  return{focus:[]};
 }
}
global.RoyalEngine=RoyalEngine;if(typeof module!=='undefined')module.exports={RoyalEngine,COLORS,POWER};
})(typeof globalThis!=='undefined'?globalThis:this);
