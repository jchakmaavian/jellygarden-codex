// Area 1 approval preview only. This file is not consumed by Unity.
// Three.js 0.180.0. Run build-preview.cjs to rebuild the offline bundle.
import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

const canvas = document.querySelector('#scene');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true, preserveDrawingBuffer: true });
renderer.setPixelRatio(1);
renderer.setSize(720, 1280, false);
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.0;
const scene = new THREE.Scene();
scene.background = new THREE.Color('#b9d8e6');
scene.fog = new THREE.Fog('#d8dbc5', 17, 40);
const camera = new THREE.PerspectiveCamera(35, 9 / 16, .1, 100);
camera.position.set(0, 4.5, 17.25);
camera.lookAt(0, 3.15, 0);
scene.add(new THREE.HemisphereLight('#e0f2ff', '#917454', 1.45));
const sun = new THREE.DirectionalLight('#ffdb9c', 3.1);
sun.position.set(-6, 9, 7);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, {left:-7,right:7,top:9,bottom:-5,near:.1,far:30});
sun.shadow.normalBias = .025;
sun.shadow.bias = -.00025;
sun.shadow.radius = 3;
scene.add(sun);
const bounce = new THREE.DirectionalLight('#9bb7e0', .8);
bounce.position.set(5, 4, -1); scene.add(bounce);

let seed = 251;
const random = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
const clamp = v => THREE.MathUtils.clamp(v, 0, 1);
const smooth = v => { v = clamp(v); return v*v*(3-2*v); };
const out = v => 1 - Math.pow(1-clamp(v), 3);
const ease = (t,a,b) => smooth((t-a)/(b-a));
const mat = (color, metalness = 0, roughness = .65) => new THREE.MeshStandardMaterial({ color, metalness, roughness });
const stone = Array.from({length:8}, (_,i) => mat(new THREE.Color().setHSL(.099+i*.001,.32,.55+i*.018)));
const trim = mat('#d9b783');
const mortar = mat('#a88a68');
const blue = mat('#154caa', .22, .36);
const gold = mat('#d69b29', .7, .25);
const iron = mat('#3a3a43', .72, .39);
const dark = mat('#191a1b');
const leaves = [mat('#657b26'),mat('#879c36'),mat('#495e28'),mat('#718629')];
const flowerMaterial = mat('#edc855');
function mesh(geometry, material, x=0, y=0, z=0, parent=scene) {
  const m = new THREE.Mesh(geometry, material); m.position.set(x,y,z);
  m.castShadow = true; m.receiveShadow = true; parent.add(m); return m;
}
function box(w,h,d,material,x=0,y=0,z=0,parent=scene,r=.025) {
  return mesh(new RoundedBoxGeometry(w,h,d,2,Math.min(r,w/4,h/4,d/4)),material,x,y,z,parent);
}
function cylinder(rt,rb,h,material,x,y,z,parent=scene,n=40) {
  return mesh(new THREE.CylinderGeometry(rt,rb,h,n),material,x,y,z,parent);
}
function sphere(r,material,x,y,z,parent=scene) {
  return mesh(new THREE.SphereGeometry(r,16,12),material,x,y,z,parent);
}
function rod(a,b,r,material,parent=scene) {
  const p = new THREE.Vector3(...a), q = new THREE.Vector3(...b);
  const m = cylinder(r,r,p.distanceTo(q),material,0,0,0,parent,12);
  m.position.copy(p).add(q).multiplyScalar(.5);
  m.quaternion.setFromUnitVectors(new THREE.Vector3(0,1,0),q.sub(p).normalize()); return m;
}
function texture(draw,w=512,h=512) {
  const c = document.createElement('canvas'); c.width=w;c.height=h;draw(c.getContext('2d'),w,h);
  const t=new THREE.CanvasTexture(c);t.colorSpace=THREE.SRGBColorSpace;t.anisotropy=4;return t;
}
function woodTexture(aged=false) {
  return texture((ctx,w,h)=>{
    ctx.fillStyle=aged?'#6b5237':'#bd7839';ctx.fillRect(0,0,w,h);
    for(let i=0;i<52;i++) {
      const x=random()*w;ctx.beginPath();ctx.moveTo(x,0);
      for(let y=0;y<=h;y+=6) ctx.lineTo(x+Math.sin(y*.019+i*2)*3+Math.sin(y*.047+i)*1.5,y);
      ctx.strokeStyle=i%3===0?(aged?'#3b3228':'#74401f'):(aged?'#927351':'#d69956');
      ctx.lineWidth=.6+random()*2;ctx.globalAlpha=.2+random()*.30;ctx.stroke();
    }
    ctx.globalAlpha=.6;
    for(let i=0;i<4;i++) {
      const x=random()*w,y=random()*h;ctx.strokeStyle=aged?'#493a2b':'#824721';
      for(let j=0;j<5;j++){ctx.beginPath();ctx.ellipse(x,y,2+j*1.8,9+j*8,.05,0,Math.PI*2);ctx.stroke();}
    }
  },128,768);
}
const wood = new THREE.MeshStandardMaterial({map:woodTexture(),roughness:.53});
const oldWood = new THREE.MeshStandardMaterial({map:woodTexture(true),roughness:.92,color:'#acaa97'});
scene.background=texture((ctx,w,h)=>{
  const sky=ctx.createLinearGradient(0,0,0,h);sky.addColorStop(0,'#82b6dc');sky.addColorStop(.55,'#cfdbcf');sky.addColorStop(1,'#f1d8a9');ctx.fillStyle=sky;ctx.fillRect(0,0,w,h);
  const glow=ctx.createRadialGradient(0,h*.18,0,0,h*.18,w*.65);glow.addColorStop(0,'#ffecbcbf');glow.addColorStop(1,'#ffdb9500');ctx.fillStyle=glow;ctx.fillRect(0,0,w,h);
  for(const [cx,cy,rx] of [[.09,.23,.15],[.40,.105,.11],[.72,.30,.17],[.95,.13,.12]]){
    for(let i=0;i<5;i++){
      const x=(cx+(i-2)*rx*.43)*w,y=(cy+Math.sin(i*2)*.012)*h,r=rx*w*(.38+.12*Math.sin(i));
      const cloud=ctx.createRadialGradient(x,y,0,x,y,r);cloud.addColorStop(0,'#fff1d1a0');cloud.addColorStop(.65,'#fff0d880');cloud.addColorStop(1,'#fff0d800');ctx.fillStyle=cloud;ctx.fillRect(x-r,y-r,r*2,r*2);
    }
  }
},720,1280);

// Ground and warm courtyard: every repair uses this one scene and one camera.
box(32,.14,38,mat('#899267'),0,-.12,-5,scene,.02);
box(6.2,.1,21,mortar,0,-.03,3);
for(let row=0;row<28;row++) for(let col=0;col<10;col++) {
  const x=(col-4.5)*.59+(row%2)*.26;
  const p=box(.54+random()*.035,.09,.59,stone[(row+col*3)%8],x,.022,8-row*.66,scene,.043);
  p.rotation.y=(random()-.5)*.075;
}
// Side garden walls.
for(const s of [-1,1]) for(let j=0;j<9;j++) {
  for(let y=0;y<2;y++) box(.56,.29,.71,stone[(j+y)%8],s*3.05,.17+y*.29,5.9-j*.74);
  box(.65,.15,.78,trim,s*3.05,.68,5.9-j*.74);
}
for(let i=0;i<140;i++) {
  const s=i%2?1:-1,x=s*(2.9+random()*2.0),z=5-random()*10;
  const g=mesh(new THREE.IcosahedronGeometry(.13+random()*.19,1),leaves[i%4],x,.12+random()*.12,z);
  g.scale.set(1,1.2+random(),.8);
  if(i%5===0) sphere(.04,flowerMaterial,x,.39,z);
}
// Cypress silhouettes and distant castle pieces.
for(let i=0;i<18;i++) {
  const x=(random()-.5)*25,z=-7-random()*13,h=2+random()*5;
  cylinder(.055,.08,h*.55,mat('#776849'),x,h*.275,z);
  mesh(new THREE.ConeGeometry(.35+random()*.3,h,12),leaves[i%4],x,h*.55,z);
}
for(let i=0;i<6;i++) {
  const x=5+i*1.4,z=-10-i*.8,h=5+random()*3;
  cylinder(.52,.58,h,trim,x,h/2,z);
  mesh(new THREE.ConeGeometry(.7,1.9,32),blue,x,h+.93,z);
  sphere(.08,gold,x,h+1.9,z);
}
// Towers use individual radial sandstone blocks, with blue tiled cone roofs.
for(const x of [-2.05,2.05]) {
  cylinder(.705,.705,4.65,mortar,x,2.325,-.05);
  for(let row=0;row<11;row++) for(let k=0;k<14;k++) {
    const theta=k/14*Math.PI*2+(row%2)*Math.PI/14;
    const radius=.73,angle=Math.PI*2/14-.016;
    const m=mesh(new THREE.CylinderGeometry(radius,radius,.405,5,1,false,theta,angle),stone[(row+k*3)%8],x,.22+row*.422,-.05);
  }
  cylinder(.82,.87,.22,trim,x,.16,-.05);
  cylinder(.87,.75,.24,trim,x,3.97,-.05);
  cylinder(.89,.89,.14,trim,x,4.55,-.05);
  for(let k=0;k<12;k++) {
    const theta=k/12*Math.PI*2;
    const corbel=box(.18,.25,.21,trim,x+Math.sin(theta)*.76,3.77,-.05+Math.cos(theta)*.76);
    corbel.rotation.y=theta;
  }
  mesh(new THREE.ConeGeometry(.98,1.66,64),blue,x,5.45,-.05);
  for(let row=1;row<9;row++) {
    const y=4.62+row*1.66/9,r=.98*(1-row/9);
    const ring=mesh(new THREE.TorusGeometry(r,.01,6,64),blue,x,y,-.05);ring.rotation.x=Math.PI/2;
  }
  cylinder(.97,.97,.12,trim,x,4.59,-.05);
  cylinder(.08,.10,.18,gold,x,6.32,-.05);
  sphere(.13,gold,x,6.48,-.05);
  mesh(new THREE.ConeGeometry(.067,.44,24),gold,x,6.77,-.05);
  // Narrow dark window with raised border.
  box(.22,.55,.07,mortar,x,3.03,.7,scene,.075);
  box(.105,.42,.035,dark,x,3.05,.746,scene,.04);
}
// The wall has a true arched opening; it is never swapped between objectives.
const wall = new THREE.Shape();
wall.moveTo(-2,0);wall.lineTo(-2,4.47);wall.lineTo(2,4.47);wall.lineTo(2,0);
wall.lineTo(1.28,0);wall.lineTo(1.28,2.20);
wall.absarc(0,2.20,1.28,0,Math.PI,false);
wall.lineTo(-1.28,0);wall.closePath();
mesh(new THREE.ExtrudeGeometry(wall,{depth:.45,bevelEnabled:false,curveSegments:40}),mortar,0,0,-.1);
for(let row=0;row<10;row++) for(let col=0;col<8;col++) {
  const x=-1.78+col*.51+(row%2)*.23,y=.22+row*.44;
  const openingTop=2.2+Math.sqrt(Math.max(0,1.59*1.59-x*x));
  if(Math.abs(x)<1.74 && y<openingTop+.23)continue;
  box(.47,.405,.12,stone[(row*2+col)%8],x,y,.39);
}
box(3.58,.16,.64,trim,0,4.46,.12);
for(let i=0;i<3;i++) box(.40,.36,.56,stone[i],-.74+i*.74,4.7,.14);
const archR=1.28, archOuter=1.64;
for(let i=0;i<13;i++) {
  const a=i*Math.PI/13+.009,b=(i+1)*Math.PI/13-.009,s=new THREE.Shape();
  s.absarc(0,2.2,archOuter,a,b,false);s.absarc(0,2.2,archR,b,a,true);s.closePath();
  mesh(new THREE.ExtrudeGeometry(s,{depth:.26,bevelEnabled:true,bevelSegments:2,steps:1,bevelSize:.012,bevelThickness:.012,curveSegments:8}),stone[(i+4)%8],0,0,.37);
}
for(const x of [-1.46,1.46]) for(let i=0;i<5;i++) box(.36,.432,.41,stone[(i+3)%8],x,.224+i*.442,.47);
box(2.65,.09,.68,mortar,0,.015,.36);
// A dark recess stays behind the boards, with enough depth to read the damage.
const doorShape=new THREE.Shape();doorShape.moveTo(-1.25,.06);doorShape.lineTo(1.25,.06);
doorShape.lineTo(1.25,2.2);doorShape.absarc(0,2.2,1.25,0,Math.PI,false);doorShape.closePath();
mesh(new THREE.ShapeGeometry(doorShape,40),dark,0,0,.13);
function plankShape(left,right,broken=false,index=0) {
  const s=new THREE.Shape(),top=x=>2.2+Math.sqrt(Math.max(0,1.25*1.25-x*x))-.018;
  const cut=broken?.5+(index%3)*.23:.07;
  s.moveTo(left,cut);s.lineTo(left,top(left));
  for(let j=1;j<=8;j++){const x=left+(right-left)*j/8;s.lineTo(x,top(x));}
  s.lineTo(right,cut);
  if(broken){s.lineTo(right-.027,cut+.22);s.lineTo(right-.07,cut-.15);s.lineTo(left+.05,cut+.09);}
  s.closePath();return s;
}
const oldPlanks=[],newPlanks=[],plankCount=12;
for(let i=0;i<plankCount;i++) {
  const l=-1.244+i*2.488/plankCount+.012,r=l+2.488/plankCount-.024,center=(l+r)/2;
  const geo=new THREE.ExtrudeGeometry(plankShape(l,r),{depth:.12,bevelEnabled:true,bevelSegments:2,bevelThickness:.009,bevelSize:.003,curveSegments:12});
  // Map grain along each plank rather than stretching it over the entire door.
  const p=geo.attributes.position,uv=geo.attributes.uv;
  for(let j=0;j<p.count;j++)uv.setXY(j,(p.getX(j)-l)/(r-l),p.getY(j)/3.48);
  geo.translate(-center,-1.75,0);
  const m=mesh(geo,wood,center,1.75,.265);newPlanks.push({mesh:m,x:center,index:i});
  const og=new THREE.ExtrudeGeometry(plankShape(l,r,i>=3&&i<=8,i),{depth:.12,bevelEnabled:true,bevelSegments:1,bevelThickness:.007,bevelSize:.004});
  const op=og.attributes.position,ou=og.attributes.uv;
  for(let j=0;j<op.count;j++)ou.setXY(j,(op.getX(j)-l)/(r-l),op.getY(j)/3.48);
  og.translate(-center,-1.75,0);
  const o=mesh(og,oldWood,center,1.75,.265);oldPlanks.push({mesh:o,x:center,index:i});
}
const debris=[];
for(let i=0;i<5;i++) {
  const p=box(.13,.045,.64,oldWood,(i-2)*.19,.09,.65+i*.13);
  p.rotation.y=(i-2)*.38;debris.push(p);
}
// Reuse the original ironwork: no door opening or replacement gate.
for(const s of [-1,1]) for(const y of [.75,2.10]) {
  box(1.02,.13,.085,iron,s*.72,y,.444,scene,.03);
  for(const a of [.33,.58,.83,1.08]) {
    const rivet=sphere(.029,iron,s*a,y,.5);rivet.scale.z=.4;
  }
  cylinder(.038,.038,.3,iron,s*1.26,y,.43,scene,12);
  const tip=box(.135,.135,.08,iron,s*.225,y,.445);tip.rotation.z=Math.PI/4;
}
// The royal banner hangs from the existing rod and unrolls as a deformed cloth mesh.
rod([-1.05,4.10,.73],[1.05,4.10,.73],.024,gold);
sphere(.075,gold,-1.10,4.10,.73);sphere(.075,gold,1.10,4.10,.73);
const flagTexture=texture((ctx,w,h)=>{
  const bg=ctx.createLinearGradient(0,0,w,h);bg.addColorStop(0,'#163e92');bg.addColorStop(.48,'#2260c4');bg.addColorStop(1,'#123883');
  ctx.fillStyle=bg;ctx.fillRect(0,0,w,h);ctx.strokeStyle='#efc568';ctx.lineWidth=17;ctx.strokeRect(9,-30,w-18,h+22);
  ctx.strokeStyle='#9d6a24';ctx.lineWidth=3;ctx.strokeRect(20,-30,w-40,h+10);
  ctx.save();ctx.translate(w*.5,h*.51);ctx.scale(1,.94);
  ctx.fillStyle='#efbd48';ctx.strokeStyle='#976420';ctx.lineWidth=5;
  ctx.beginPath();ctx.moveTo(-121,-57);ctx.lineTo(-85,49);ctx.quadraticCurveTo(0,70,85,49);ctx.lineTo(121,-57);ctx.lineTo(60,-6);ctx.lineTo(0,-105);ctx.lineTo(-60,-6);ctx.closePath();ctx.fill();ctx.stroke();
  ctx.fillStyle='#ffe39a';ctx.fillRect(-83,39,166,18);
  for(const [x,y] of [[-121,-57],[0,-105],[121,-57]]){ctx.beginPath();ctx.arc(x,y,12,0,Math.PI*2);ctx.fill();ctx.stroke();}
  ctx.fillStyle='#24509a';for(const x of [-56,0,56]){ctx.beginPath();ctx.ellipse(x,24,7,9,0,0,Math.PI*2);ctx.fill();}
  ctx.restore();
},512,320);
const clothMaterial=new THREE.MeshStandardMaterial({map:flagTexture,roughness:.87,metalness:.05,side:THREE.DoubleSide});
const clothGeo=new THREE.PlaneGeometry(1.84,.76,48,26);
const clothBase=clothGeo.attributes.position.array.slice();
const cloth=mesh(clothGeo,clothMaterial,0,4.065,.775);
const tabs=[];
for(let i=0;i<5;i++) tabs.push(box(.075,.16,.028,blue,-.82+i*.41,4.075,.755));
const roll=cylinder(.055,.055,1.84,blue,0,4.065,.79);roll.rotation.z=Math.PI/2;

// Two brass lanterns hang on the same hooks. Their arrival and swing are true 3D transforms.
const lanterns=[];
for(const s of [-1,1]) {
  const x=s*1.83,y=2.33;
  rod([x,y+.14,.61],[x,y+.14,.84],.025,gold);
  const hook=mesh(new THREE.TorusGeometry(.075,.019,8,22,Math.PI*1.3),gold,x,y+.085,.84);hook.rotation.z=.2;
  const group=new THREE.Group();scene.add(group);
  const pivotY=y+.01;
  mesh(new THREE.TorusGeometry(.052,.012,8,20),gold,0,-.05,0,group);
  cylinder(.018,.045,.11,gold,0,-.15,0,group);
  mesh(new THREE.ConeGeometry(.205,.12,4),gold,0,-.245,0,group).rotation.y=Math.PI/4;
  box(.295,.035,.23,gold,0,-.304,0,group);
  box(.285,.04,.225,gold,0,-.695,0,group);
  const glassMat=new THREE.MeshStandardMaterial({color:'#fbd38a',transparent:true,opacity:.2,roughness:.16,depthWrite:false,side:THREE.DoubleSide});
  box(.257,.35,.197,glassMat,0,-.495,0,group,.006);
  for(const px of [-.13,.13])for(const pz of [-.10,.10])rod([px,-.315,pz],[px,-.68,pz],.012,gold,group);
  cylinder(.065,.087,.055,gold,0,-.736,0,group);
  sphere(.028,gold,0,-.779,0,group);
  cylinder(.04,.045,.17,mat('#ffe3a0'),0,-.602,0,group);
  const flameMat=new THREE.MeshStandardMaterial({color:'#fff4b8',emissive:'#ffb436',emissiveIntensity:0});
  const flame=mesh(new THREE.SphereGeometry(.038,12,12),flameMat,0,-.475,0,group);flame.scale.set(.7,1.65,.7);
  const light=new THREE.PointLight('#ffb94e',0,2.5,1.6);light.position.set(0,-.48,.13);group.add(light);
  lanterns.push({group,x,y:pivotY,flame,flameMat,light,side:s});
}
const sparks=[];
const sparkMaterial=new THREE.MeshBasicMaterial({color:'#ffe0a0',transparent:true,depthWrite:false});
for(let i=0;i<44;i++) {
  const g=mesh(new THREE.OctahedronGeometry(.017,0),sparkMaterial.clone());g.castShadow=false;
  sparks.push({mesh:g,angle:random()*Math.PI*2,phase:random(),speed:.5+random()*.7});
}
// Batch the unchanging courtyard and stonework for smooth offline playback.
const moving=new Set([...oldPlanks.map(p=>p.mesh),...newPlanks.map(p=>p.mesh),...debris,cloth,roll,...tabs,...sparks.map(p=>p.mesh)]);
const batches=new Map();scene.updateMatrixWorld(true);
for(const m of [...scene.children]) {
  if(!m.isMesh||moving.has(m)||Array.isArray(m.material))continue;
  const key=m.material.uuid;
  if(!batches.has(key))batches.set(key,{material:m.material,geometries:[]});
  const geometry=(m.geometry.index?m.geometry.toNonIndexed():m.geometry.clone()).applyMatrix4(m.matrixWorld);
  batches.get(key).geometries.push(geometry);scene.remove(m);m.geometry.dispose();
}
for(const {material,geometries} of batches.values()) {
  const geometry=mergeGeometries(geometries,false);mesh(geometry,material);
  geometries.forEach(g=>g.dispose());
}
function frame(t) {
  t=THREE.MathUtils.clamp(t,0,18);
  oldPlanks.forEach(({mesh:m,index:i,x})=>{
    const p=ease(t,.9+i*.028,1.65+i*.028);m.visible=p<1;
    m.position.set(x+(i-5.5)*p*.09,1.75-p*3.7,.265+p*.5);
    m.rotation.set(p*.65,0,(i%2?1:-1)*p*.22);
  });
  debris.forEach((d,i)=>{const p=ease(t,1.0+i*.08,1.8+i*.08);d.visible=p<1;d.position.y=.09-p*.4;d.scale.setScalar(1-p);});
  newPlanks.forEach(({mesh:m,index:i,x})=>{
    const start=1.7+i*.18,p=out((t-start)/1.15);m.visible=t>=start;
    m.position.set(x+(i%2?1:-1)*(1-p)*1.9,1.75-(1-p)*3.8,.265+Math.pow(1-p,1.2)*3.1);
    m.rotation.set((1-p)*.55,(i%2?1:-1)*(1-p)*.62,(i-5.5)*(1-p)*.065);
    m.position.z+=t>start+1.0&&t<start+1.65?.035*Math.sin((t-start-1)*22)*Math.exp(-(t-start-1)*10):0;
  });
  const hoist=ease(t,6.75,7.6),unroll=ease(t,7.65,10.05);
  cloth.visible=t>=6.75;roll.visible=t>=6.75&&t<10.15;
  const pos=cloth.geometry.attributes.position;
  for(let i=0;i<pos.count;i++) {
    const x=clothBase[i*3],v=(.38-clothBase[i*3+1])/.76;
    const tip=Math.max(0,1-Math.abs(x)/.92)*.10*v;
    const length=.76*v+tip;
    const wave=Math.sin(x*10+t*2.4+v*2.6)*.024+Math.sin(x*18-t*1.3)*.013;
    const released=.86*unroll,radius=.045*Math.sqrt(1-unroll)+.005;
    if(length<=released)pos.setXYZ(i,x,-length,wave*v*unroll);
    else {
      const angle=(length-released)/radius;
      pos.setXYZ(i,x,-released-Math.sin(angle)*radius,radius*(1-Math.cos(angle)));
    }
  }
  pos.needsUpdate=true;cloth.geometry.computeVertexNormals();
  cloth.position.y=4.065-(1-hoist)*1.8;cloth.position.z=.775+(1-hoist)*1.6;
  roll.position.set(0,cloth.position.y-.86*unroll,cloth.position.z+.035);
  roll.rotation.x=t*3;roll.scale.x=Math.max(.05,1-unroll*.9);roll.scale.z=roll.scale.x;
  tabs.forEach(tab=>{tab.visible=t>=7.6;tab.scale.y=Math.max(.01,ease(t,7.6,7.85));});
  lanterns.forEach((l,i)=>{
    const start=12.75+i*.40,p=out((t-start)/1.45);l.group.visible=t>=start;
    l.group.position.set(l.x+l.side*(1-p)*1.15,l.y-(1-p)*2.6,.875+(1-p)*2.3);
    const settle=Math.max(0,t-start-1.35);
    l.group.rotation.set((1-p)*.27,Math.sin(settle*5)*Math.exp(-settle*2)*.15,(1-p)*l.side*.40+Math.sin(settle*7.2)*Math.exp(-settle*1.8)*.13);
    const lit=ease(t,start+1.65,start+2.30),flicker=.95+.05*Math.sin(t*11+i);
    l.flame.visible=lit>0;l.flameMat.emissiveIntensity=lit*4*flicker;l.light.intensity=lit*3.5*flicker;
    l.flame.scale.y=1.65+.13*Math.sin(t*9+i);
  });
  const moments=[4.8,10.15,16.55];
  let age=9,stage=0;moments.forEach((at,i)=>{if(t>=at&&t-at<age){age=t-at;stage=i;}});
  sparks.forEach((p,i)=>{
    const life=age-p.phase*.24;p.mesh.visible=life>=0&&life<.95;
    p.mesh.material.opacity=Math.max(0,1-life/.95)*.8;
    const cx=stage===2?(i%2?-1.83:1.83):0,cy=stage===0?1.65:stage===1?3.68:1.87;
    p.mesh.position.set(cx+Math.cos(p.angle)*(life*p.speed+.07),cy+Math.sin(p.angle)*life*p.speed+life*.35,.94+life*.45);
    p.mesh.rotation.set(t*2,t*3,t);p.mesh.scale.setScalar(1+.5*Math.sin(life*8));
  });
  renderer.render(scene,camera);
}
window.gatePreview={duration:18,setTime:frame,renderer,scene,camera,ready:true};
let playing=false,currentTime=0,last=performance.now();
const params=new URLSearchParams(location.search);const renderMode=params.has('render');
function loop(now) {
  if(playing){currentTime+=(now-last)/1000;if(currentTime>18)currentTime=0;frame(currentTime);}
  last=now;requestAnimationFrame(loop);
  if(window.parent!==window&&playing)window.parent.postMessage({type:'gate-time',time:currentTime},'*');
}
window.addEventListener('message',e=>{
  if(e.source!==window.parent)return;
  if(e.data?.type==='gate-play'){playing=!!e.data.play;last=performance.now();}
  if(e.data?.type==='gate-seek'){currentTime=Math.max(0,Math.min(18,Number(e.data.time)||0));frame(currentTime);}
});
frame(renderMode?0:18);
if(!renderMode)requestAnimationFrame(loop);
window.parent.postMessage({type:'gate-ready'},'*');
