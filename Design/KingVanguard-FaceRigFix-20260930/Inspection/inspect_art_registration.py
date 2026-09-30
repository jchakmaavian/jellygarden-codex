"""Read-only atlas/workboard audit; emits technical evidence, never runtime art."""
from pathlib import Path
import json, hashlib, sys
from datetime import datetime, timezone

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
REF = ROOT / "Design/KingVanguard-Reference-20260929"
sys.path.insert(0, str(ROOT / "Design/ReferenceGameplayImport/Media/Tools/Python"))
from PIL import Image, ImageDraw, ImageFont

layout_path = REF / "FaceRedesign/Portrait/Portrait-layout.json"
layout = json.loads(layout_path.read_text(encoding="utf-8"))
new_path = ROOT / "Assets/Characters/KingVenguard/V2/Spine/King_Vanguard.png"
area_path = ROOT / "Assets/Characters/KingVenguard/V2/Spine/AreaCompleted_Vanguard.png"
board_path = ROOT / "Design/KingVanguard-ActivityFix-20260930/FaceRedesign/Portrait-mouth-repaired-workboard.png"
atlas = Image.open(new_path).convert("RGBA")
board = Image.open(board_path).convert("RGBA")
font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 19)
small = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
cells = {c["index"]: c for c in layout["cells"]}


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def crop_installed(c):
    r = c["region"]
    w, h = r["PhysicalWidth"], r["PhysicalHeight"]
    result = atlas.crop((r["x"], r["y"], r["x"]+w, r["y"]+h))
    if r["rotation"] == 90: result = result.transpose(Image.Transpose.ROTATE_270)
    elif r["rotation"] == 270: result = result.transpose(Image.Transpose.ROTATE_90)
    elif r["rotation"] == 180: result = result.transpose(Image.Transpose.ROTATE_180)
    mask = Image.open(layout_path.parent / c["maskPath"]).convert("RGBA").getchannel("A")
    data = list(result.getdata()); alpha = list(mask.getdata())
    result.putdata([(p[0],p[1],p[2],p[3] if alpha[i] else 0) for i,p in enumerate(data)])
    return result, mask


def crop_paint(c):
    x,y,w,h = c["paintRect"]
    sx,sy = board.width/layout["width"],board.height/layout["height"]
    r = c["region"]
    result = board.transform((r["width"],r["height"]), Image.Transform.EXTENT,
        (x*sx,y*sy,(x+w)*sx,(y+h)*sy), Image.Resampling.BICUBIC)
    return result


def keyed(result):
    out = result.copy()
    out.putdata([(r,g,b,0 if g-max(r,b)>12 else a) for r,g,b,a in result.getdata()])
    return out


def bbox_and_count(values, w, predicate):
    pts = [(i%w,i//w) for i,p in enumerate(values) if predicate(p)]
    return {"pixels":len(pts),"bounds": [min(x for x,y in pts),min(y for x,y in pts),max(x for x,y in pts)+1,max(y for x,y in pts)+1] if pts else None}


metrics=[]
for idx,c in cells.items():
    if c["technicalShadow"]: continue
    original = Image.open(layout_path.parent / "Parts" / f"{idx:02d}.png").convert("RGBA")
    installed,mask = crop_installed(c)
    paint=crop_paint(c)
    source_a=list(mask.getdata()); current_a=list(installed.getchannel("A").getdata())
    pp=list(paint.getdata()); core=[i for i,a in enumerate(source_a) if a>=128]
    fill=[i for i in core if pp[i][1]-max(pp[i][0],pp[i][2])>12 or pp[i][3]<32]
    metrics.append({"cell":idx,"name":c["name"],"label":c["label"],"atlasPhysicalRect":[c["region"][k] for k in ("x","y","PhysicalWidth","PhysicalHeight")],
        "logicalSize":[original.width,original.height],"sourceCorePixels":len(core),
        "estimatedSourceCoreOutsideNewPaint":len(fill),"estimatedSourceCoreOutsidePaintPercent":round(100*len(fill)/len(core),2),
        "sourceAlphaSum":sum(source_a),"installedAlphaSum":sum(current_a),"installedToSourceAlphaRatio":sum(current_a)/sum(source_a),
        "sourceCoreTexelsNoLongerOpaque":sum(current_a[i]<128 for i in core),
        "sourceAndInstalledAlphaDifferences":sum(a!=b for a,b in zip(source_a,current_a)),
        "paintOpaqueFootprint":bbox_and_count(pp,paint.width,lambda p:p[3]>=128 and p[1]-max(p[0],p[2])<=12)})


def sheet(indices, name):
    cw,rh=460,240
    output=Image.new("RGB",(cw*3,70+rh*len(indices)),(229,233,238))
    d=ImageDraw.Draw(output)
    for col,title in enumerate(("Original APK (straight alpha)","New painted cell before packing", "Installed: new alpha clipped to source")):
        d.text((col*cw+12,12),title,font=font,fill=(22,35,60))
    for row,idx in enumerate(indices):
        c=cells[idx]; y=70+row*rh
        original=Image.open(layout_path.parent/"Parts"/f"{idx:02d}.png").convert("RGBA")
        installed,mask=crop_installed(c); paint=keyed(crop_paint(c))
        for col,part in enumerate((original,paint,installed)):
            x=col*cw
            d.rectangle((x+6,y+27,x+cw-6,y+rh-7),fill=(46,57,74))
            d.text((x+12,y+4),f'{idx:02d} {c["label"]}',font=small,fill=(20,32,50))
            scale=min((cw-40)/part.width,(rh-55)/part.height)
            sized=part.resize((round(part.width*scale),round(part.height*scale)),Image.Resampling.NEAREST)
            output.paste(sized,(x+(cw-sized.width)//2,y+32+(rh-45-sized.height)//2),sized)
        d.line((0,y+rh-1,cw*3,y+rh-1),fill=(149,161,176))
    output.save(OUT/name)


sheet([3,18,19,20,21],"mouth-and-moustache-registration.png")
sheet([1,2,25,27,10,14],"face-brows-and-lids-registration.png")

inventory=json.loads((REF/"facial-rig-registration.json").read_text(encoding="utf-8"))
key_names={cells[i]["name"] for i in [3,18,19,20,21,25,27]}
topology={name:[s for s in rig["slots"] if s["name"] in key_names] for name,rig in inventory["rigs"].items()}
record={"generatedUtc":datetime.now(timezone.utc).isoformat(),
    "inputs": [{"path":str(p.relative_to(ROOT)).replace('\\','/'),"sha256":sha(p)} for p in [layout_path,new_path,area_path,board_path]],
    "measurementNote":"Paint-cell sampling uses Pillow BICUBIC and is an estimate of the C# packer's System.Drawing sampling. Alpha comparisons use exact installed texels and original extraction masks. Counts marked estimated are not a reproduction of the original pack report. Final pack uses new keyed paint alpha clipped to the source island alpha, not forced source coverage. Nearest RGB fill also exists in fully transparent texels and is not by itself a visible defect.",
    "runtimeAssetsModified":False,"metrics":metrics,"relevantOriginalTopology":topology}
(OUT/"art-registration-audit.json").write_text(json.dumps(record,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
for m in metrics:
    if m['cell'] in [1,2,3,10,14,18,19,20,21,25,27]:
        print(m['cell'],m['label'],'outsidePaint%',m['estimatedSourceCoreOutsidePaintPercent'],'alphaRatio',round(m['installedToSourceAlphaRatio'],4))
