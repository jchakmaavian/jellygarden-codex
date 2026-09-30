"""Read-only pixel attribution for green spill; writes inspection evidence only."""
import json,sys
from pathlib import Path
TASK=Path(__file__).resolve().parents[1];ROOT=TASK.parents[1]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw,ImageFont
REF=ROOT/'Design/KingVanguard-Reference-20260929'
layout_path=REF/'FaceRedesign/Portrait/Portrait-layout.json'
layout=json.loads(layout_path.read_text(encoding='utf-8'))
clean_mode='--clean' in sys.argv
images={k:Image.open(p).convert('RGBA') for k,p in {
    'before_teeth':TASK/'Before/Atlases/King_Vanguard.png',
    'current':TASK/'FaceRedesign/PackedClean/King_Vanguard.png' if clean_mode else ROOT/'Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png',
    'original':REF/'Original-rfv387/King.png'}.items()}
def suspicious(p):
    r,g,b,a=p
    return a>=16 and g-r>=-8 and g-b>=20
def crop(im,c):
    r=c['region'];out=im.crop((r['x'],r['y'],r['x']+r['PhysicalWidth'],r['y']+r['PhysicalHeight']))
    if r['rotation']==90:out=out.transpose(Image.Transpose.ROTATE_270)
    mask=Image.open(layout_path.parent/c['maskPath']).getchannel('A');values=list(out.getdata());alpha=list(mask.getdata())
    out.putdata([(p[0],p[1],p[2],p[3] if alpha[i] else 0) for i,p in enumerate(values)])
    return out
records=[]
for c in layout['cells']:
    current=crop(images['current'],c);old=crop(images['before_teeth'],c)
    cur=list(current.getdata());prev=list(old.getdata())
    points=[{'local':[i%current.width,i//current.width],'rgba':list(p),'wasIdenticalBeforeTeeth':p==prev[i]} for i,p in enumerate(cur) if suspicious(p)]
    records.append({'cell':c['index'],'name':c['name'],'suspiciousCurrentPixels':len(points),'suspiciousBeforePixels':sum(suspicious(p) for p in prev),
        'rgbaChangedFromBeforeTeeth':sum(a!=b and (a[3]>0 or b[3]>0) for a,b in zip(cur,prev)),
        'strongGreenDominancePixels':sum(p[3]>=16 and p[1]-max(p[0],p[2])>12 for p in cur),'examples':points[:15]})
out=TASK/'Inspection';out.mkdir(exist_ok=True)
(out/('green-fringe-attribution-clean.json' if clean_mode else 'green-fringe-attribution.json')).write_text(json.dumps({'rule':'Visible alpha>=16, G-R>=-8 and G-B>=20. This flags yellow-green matte spill, not only pure saturated green. Exact before/current byte comparison attributes changes.', 'records':records},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
ids=[20,19,21,3,2,16,17,8,12]
cw,rh=460,165;sheet=Image.new('RGB',(cw*2,60+len(ids)*rh),(228,231,235));d=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
d.text((10,12),'Current installed component',font=font,fill=(20,25,40));d.text((cw+10,12),'Same component: suspicious texels marked blue',font=font,fill=(20,25,40))
for row,idx in enumerate(ids):
    c=next(c for c in layout['cells'] if c['index']==idx);part=crop(images['current'],c);marked=part.copy()
    marked.putdata([(10,50,255,255) if suspicious(p) else p for p in marked.getdata()])
    y=60+row*rh
    for col,im in enumerate((part,marked)):
        x=col*cw;d.text((x+8,y+3),f'{idx:02d} {c["label"]}',font=small,fill=(20,25,40));d.rectangle((x+5,y+25,x+cw-5,y+rh-5),fill=(234,190,161))
        scale=min((cw-25)/im.width,130/im.height);resized=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.NEAREST)
        sheet.paste(resized,(x+(cw-resized.width)//2,y+28+(132-resized.height)//2),resized)
sheet.save(out/('green-fringe-components-clean.png' if clean_mode else 'green-fringe-components.png'))
for r in records:
    if r['suspiciousCurrentPixels'] or r['cell'] in [19,20,21,8,12]:print(r['cell'],r['name'].encode('ascii','replace').decode(),'current',r['suspiciousCurrentPixels'],'before',r['suspiciousBeforePixels'],'changed',r['rgbaChangedFromBeforeTeeth'],'strong',r['strongGreenDominancePixels'])
