"""Technical packed-component comparison only; no runtime asset writes."""
from pathlib import Path
import json,sys
TASK=Path(__file__).resolve().parents[1]
ROOT=TASK.parents[1]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw,ImageFont
layout=json.loads((TASK/'Inspection/FaceRedesign/Source/TeethShadow-layout.json').read_text(encoding='utf-8'))
old=Image.open(TASK/'Before/Atlases/King_Vanguard.png').convert('RGBA')
new=Image.open(TASK/'FaceRedesign/Packed/King_Vanguard.png').convert('RGBA')
cw,rh=390,192
sheet=Image.new('RGB',(cw*3,85+7*rh),(237,233,225));d=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
for col,label in enumerate(('Original source component','Before: stored atlas art','Packed: new paint + source alpha')):
    d.text((col*cw+8,12),label,font=font,fill=(24,32,48))
d.text((8,43),'Shadows shown at source alpha; runtime opacity is applied separately. These are extracted components, not a game render.',font=small,fill=(54,48,40))
def crop(atlas,c):
    r=c['region'];out=atlas.crop((r['x'],r['y'],r['x']+r['PhysicalWidth'],r['y']+r['PhysicalHeight']))
    if r['rotation']==90:out=out.transpose(Image.Transpose.ROTATE_270)
    elif r['rotation']==270:out=out.transpose(Image.Transpose.ROTATE_90)
    mask=Image.open(TASK/'Inspection/FaceRedesign/Source'/c['maskPath']).getchannel('A')
    values=list(out.getdata());alpha=list(mask.getdata())
    out.putdata([(p[0],p[1],p[2],p[3] if alpha[i] else 0) for i,p in enumerate(values)])
    return out
for c in layout['cells']:
    idx=c['index'];y=85+(idx-1)*rh
    source=Image.open(TASK/'Inspection/FaceRedesign/Source/Parts'/f'{idx:02d}.png').convert('RGBA')
    for col,part in enumerate((source,crop(old,c),crop(new,c))):
        x=col*cw;d.text((x+7,y+3),f'{idx:02d} {c["label"]}',font=small,fill=(37,26,23))
        d.rectangle((x+4,y+26,x+cw-5,y+rh-7),fill=(232,193,168))
        scale=min((cw-30)/part.width,145/part.height)
        resized=part.resize((round(part.width*scale),round(part.height*scale)),Image.Resampling.NEAREST)
        sheet.paste(resized,(x+(cw-resized.width)//2,y+30+(150-resized.height)//2),resized)
out=TASK/'FaceRedesign/Review';out.mkdir(exist_ok=True)
sheet.save(out/'teeth-shadow-packed-comparison.png')
print(out/'teeth-shadow-packed-comparison.png')
