"""Inspect original mouth/brow roles, setup placement and color; no asset writes."""
import json,math,sys,statistics
from pathlib import Path
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
REF=ROOT/'Design/KingVanguard-Reference-20260929'
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image
j=json.loads((REF/'King.skel.spine38.json').read_text(encoding='utf-8'))
layout=json.loads((REF/'FaceRedesign/Portrait/Portrait-layout.json').read_text(encoding='utf-8'))
atlas=Image.open(ROOT/'Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png').convert('RGBA')
matrices=[]
for b in j['bones']:
    angle,x,y,sx,sy,shearx,sheary,length=b['transform']
    assert b['mode']==0, 'This setup audit requires normal bone inheritance.'
    radians=math.pi/180
    a=math.cos((angle+shearx)*radians)*sx;c=math.sin((angle+shearx)*radians)*sx
    bb=math.cos((angle+90+sheary)*radians)*sy;d=math.sin((angle+90+sheary)*radians)*sy
    if b['parent']>=0:
        pa,pb,pc,pd,px,py=matrices[b['parent']]
        a,bb,c,d,x,y=pa*a+pb*c,pa*bb+pb*d,pc*a+pd*c,pc*bb+pd*d,pa*x+pb*y+px,pc*x+pd*y+py
    matrices.append((a,bb,c,d,x,y))

def point(bi,x,y):
    a,b,c,d,px,py=matrices[bi];return [a*x+b*y+px,c*x+d*y+py]

def color_stats(image, mask):
    rgb=[p[:3] for p,m in zip(image.getdata(),mask.getdata()) if m>=128 and p[3]>=64]
    return {'sampleCount':len(rgb),'medianRgb':[statistics.median(p[c] for p in rgb) for c in range(3)],
            'meanGreenMinusRed':round(sum(p[1]-p[0] for p in rgb)/len(rgb),3)}

records=[]
for idx in [3,19,20,21,22,24,25,26,27]:
    cell=layout['cells'][idx-1];name=cell['name'];r=cell['region']
    source=Image.open(REF/'FaceRedesign/Portrait/Parts'/f'{idx:02d}.png').convert('RGBA')
    mask=Image.open(REF/'FaceRedesign/Portrait'/cell['maskPath']).getchannel('A')
    current=atlas.crop((r['x'],r['y'],r['x']+r['PhysicalWidth'],r['y']+r['PhysicalHeight']))
    if r['rotation']==90:current=current.transpose(Image.Transpose.ROTATE_270)
    si=next(i for i,s in enumerate(j['slots']) if s['name']==name);slot=j['slots'][si]
    attachment=j['skins'][0]['attachments'][str(si)][name]
    verts=attachment['vertices'];points=[]
    if verts['weighted']:
        for vertex in verts['values']:
            xx=yy=0
            for bi,x,y,w in vertex:
                p=point(bi,x,y);xx+=p[0]*w;yy+=p[1]*w
            points.append([xx,yy])
    else:
        values=verts['values']
        points=[point(slot['bone'],values[i],values[i+1]) for i in range(0,len(values),2)]
    records.append({'cell':idx,'name':name,'slotIndex':si,'slotBone':j['bones'][slot['bone']]['name'],
        'slotColorHex':f"{slot['color']:08x}",'attachmentColorHex':f"{attachment['color']:08x}",'blend':slot['blend'],
        'setupWorldBounds':[round(min(p[0] for p in points),3),round(min(p[1] for p in points),3),round(max(p[0] for p in points),3),round(max(p[1] for p in points),3)],
        'sourceStraightAlphaColor':color_stats(source,mask),'installedStraightAlphaColor':color_stats(current,mask)})
result={'note':'Original setup-world bounds computed with normal bone inheritance, before animation. Color uses only source-mask alpha>=128 and corresponding image alpha>=64; this is visible texel color, not transparent edge RGB.',
        'drawOrderSetup':[s['name'] for s in j['slots'][18:32]],'records':records}
(OUT/'mouth-shadow-semantics.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for r in records:print(r['cell'],r['setupWorldBounds'],r['sourceStraightAlphaColor']['medianRgb'],r['installedStraightAlphaColor']['medianRgb'])
