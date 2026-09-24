"""Offline geometry check of native static poses; Unity verification remains separate."""
from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[3];HERE=Path(__file__).parent
data=json.loads((ROOT/'Assets/ReferenceGameplay/Art/BlockerNativeData.json').read_text())
cat=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text());sprites={s['sourcePathId']:s for s in cat['sprites']};images={}
def mat(n,root=False,offset=None):
 q=n['localRotation'];x,y,z,w=[q[k] for k in 'xyzw'];p=n['localPosition'];s=n['localScale'];m=np.eye(3)
 m[:2,:2]=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w],[2*x*y+2*z*w,1-2*x*x-2*z*z]])@np.diag([s['x'],s['y']])
 m[:2,2]=[-offset['x'],-offset['y']] if root else [p['x'],p['y']]
 return m
def drawtri(canvas,v,uv,tex,col,pma):
 x0=max(0,math.floor(v[:,0].min()));x1=min(canvas.shape[1]-1,math.ceil(v[:,0].max()));y0=max(0,math.floor(v[:,1].min()));y1=min(canvas.shape[0]-1,math.ceil(v[:,1].max()))
 if x1<x0 or y1<y0:return
 xs,ys=np.meshgrid(np.arange(x0,x1+1)+.5,np.arange(y0,y1+1)+.5)
 a,b,c=v;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
 if abs(den)<1e-8:return
 wa=((b[1]-c[1])*(xs-c[0])+(c[0]-b[0])*(ys-c[1]))/den;wb=((c[1]-a[1])*(xs-c[0])+(a[0]-c[0])*(ys-c[1]))/den;wc=1-wa-wb
 mask=(wa>=0)&(wb>=0)&(wc>=0)
 u=wa*uv[0,0]+wb*uv[1,0]+wc*uv[2,0];vv=wa*uv[0,1]+wb*uv[1,1]+wc*uv[2,1]
 tx=np.clip((u*(tex.shape[1]-1)).round().astype(int),0,tex.shape[1]-1);ty=np.clip(((1-vv)*(tex.shape[0]-1)).round().astype(int),0,tex.shape[0]-1)
 pix=tex[ty,tx]/255.;pix=pix*np.array(col);alpha=pix[:,:,3:4];rgb=pix[:,:,:3]*col[3] if pma else pix[:,:,:3]*alpha
 dst=canvas[y0:y1+1,x0:x1+1];dst[mask]=(rgb+dst*(1-alpha))[mask]
tiles=[]
for p in data['prefabs']:
 matrices={};actives={};layers=[]
 for n in p['nodes']:
  par=n['parentTransformId'];mm=mat(n,par==0,p['centerOffset']);m=matrices[par]@mm if par else mm;matrices[n['transformId']]=m;active=n['active'] and (actives[par] if par else True);actives[n['transformId']]=active
  for r in n['renderers']:
   if not active or not r['enabled'] or not r['spritePathId']:continue
   s=sprites[r['spritePathId']];path=ROOT/s['assetPath']
   if str(path) not in images:images[str(path)]=np.array(Image.open(path).convert('RGBA'))
   w=s['width']/s['pixelsPerUnit'];h=s['height']/s['pixelsPerUnit'];px=s['pivot']['x'];py=s['pivot']['y'];verts=np.array([[-px*w,-py*h],[-px*w,(1-py)*h],[(1-px)*w,(1-py)*h],[(1-px)*w,-py*h]])
   if r['flipX']:verts[:,0]*=-1
   if r['flipY']:verts[:,1]*=-1
   verts=np.c_[verts,np.ones(4)]@m.T
   layers.append((r['sortingOrder'],verts[:,:2],np.array([[0,0],[0,1],[1,1],[1,0]]),[0,1,2,0,2,3],images[str(path)],[r['color'][k] for k in 'rgba'],False))
 if p['meshLayers']:
  m=matrices[p['meshTransformId']];tex=np.array(Image.open(ROOT/p['meshTextureAssetPath']).convert('RGBA'))
  for i,l in enumerate(p['meshLayers']):
   vs=np.array([[v['x'],v['y'],1] for v in l['vertices']])@m.T
   layers.append((i,vs[:,:2],np.array([[v['x'],v['y']] for v in l['uv']]),l['triangles'],tex,[l['color'][k] for k in 'rgba'],True))
 canvas=np.empty((360,320,3));canvas[:]=[.27,.30,.41]
 for _,verts,uv,tris,tex,col,pma in sorted(layers,key=lambda l:l[0]):
  vs=verts.copy();vs[:,0]=160+vs[:,0]*130;vs[:,1]=166-vs[:,1]*130
  for i in range(0,len(tris),3):ix=tris[i:i+3];drawtri(canvas,vs[ix],uv[ix],tex,col,pma)
 im=Image.fromarray(np.uint8(np.clip(canvas,0,1)*255));dd=ImageDraw.Draw(im);dd.text((12,332),p['key']+' | native static geometry',fill='white');tiles.append(im)
sheet=Image.new('RGB',(320*3,360*2))
for i,im in enumerate(tiles):sheet.paste(im,((i%3)*320,(i//3)*360))
sheet.save(HERE/'Native-Blocker-Static-Review.png')
print('Offline static pose review ready.')
