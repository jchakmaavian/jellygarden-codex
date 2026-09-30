"""Produce source/current component evidence, with no runtime writes."""
from pathlib import Path
import hashlib,json,statistics,sys
from datetime import datetime,timezone
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
REF=ROOT/'Design/KingVanguard-Reference-20260929'
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw,ImageFont
source_dir=OUT/'FaceRedesign/Source'
current_dir=OUT/'FaceRedesign/Current'
layout=json.loads((source_dir/'TeethShadow-layout.json').read_text(encoding='utf-8'))
inventory=json.loads((REF/'facial-rig-registration.json').read_text(encoding='utf-8'))
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',19)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
width,row_height=1024,200
board=Image.new('RGB',(width,90+row_height*7),(237,231,224))
draw=ImageDraw.Draw(board)
draw.text((15,12),'Original APK: actual component alpha + RGB',font=font,fill=(37,36,40))
draw.text((525,12),'Current atlas: stored artwork (some hidden)',font=font,fill=(37,36,40))
draw.text((15,43),'Technical comparison on warm skin-colored background; not replacement artwork.',font=small,fill=(70,60,52))
records=[]
for cell in layout['cells']:
    idx=cell['index'];src=Image.open(source_dir/'Parts'/f'{idx:02d}.png').convert('RGBA')
    cur=Image.open(current_dir/'Parts'/f'{idx:02d}.png').convert('RGBA')
    y=90+(idx-1)*row_height
    for col,part in enumerate((src,cur)):
        x=col*512
        draw.rectangle((x+5,y+25,x+507,y+row_height-8),fill=(232,193,168))
        draw.text((x+10,y+2),f'{idx:02d} {cell["label"]}',font=small,fill=(45,32,27))
        scale=min(450/part.width,148/part.height)
        enlarged=part.resize((round(part.width*scale),round(part.height*scale)),Image.Resampling.NEAREST)
        board.paste(enlarged,(x+(512-enlarged.width)//2,y+32+(155-enlarged.height)//2),enlarged)
    stats={}
    for label,part in [('original',src),('current',cur)]:
        pixels=list(part.getdata());visible=[p for p in pixels if p[3]>=64]
        stats[label]={'alphaSum':sum(p[3] for p in pixels),'maxAlpha':max(p[3] for p in pixels),
                      'medianVisibleRgb':[statistics.median(p[c] for p in visible) for c in range(3)]}
    slot=next(s for s in inventory['rigs']['King']['slots'] if s['name']==cell['name'])
    records.append({'workboardCell':idx,'name':cell['name'],'logicalSize':[src.width,src.height],
        'atlas':cell['region'],'colorAndAlpha':stats,'sourceSlot':slot,
        'suppressedInCurrentRuntime':idx in [3,4,5]})
board.save(OUT/'teeth-and-shadows-source-current.png')
inputs=[ROOT/'Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png',ROOT/'Assets/Characters/KingVenguard/V2/Spine/AreaCompleted_Vanguard.png',
        REF/'Original-rfv387/King.png',REF/'Original-rfv387/King.atlas']
report={'generatedUtc':datetime.now(timezone.utc).isoformat(),'runtimeAssetsModified':False,
        'inputs':[{'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in inputs],
        'records':records,'note':'Source masks are decoded directly from the APK atlas. Recolored source technical masks preserve original authored edge softness and deform motion without restoring original facial RGB. Current comparisons include suppressed texture layers.'}
(OUT/'teeth-shadow-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Wrote comparison and audit for',len(records),'components')
