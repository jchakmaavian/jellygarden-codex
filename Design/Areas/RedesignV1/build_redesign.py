"""Mechanically slice generated RGBA sheets, place layers, and import the area catalogue.
Artwork is generated with imagegen. This script only crops, scales, and composites it.
"""
from pathlib import Path
import sys, json, math, hashlib, shutil
HERE=Path(__file__).resolve().parent
PROJECT=next((p for p in HERE.parents if (p/'Assets').is_dir() and (p/'ProjectSettings').is_dir()), HERE.parent/'JellyGarden-Codex')
sys.path.insert(0,str(PROJECT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image,ImageDraw,ImageFont
W,H=940,1672
OUT=PROJECT/'Assets/JellyGarden/Resources/RoyalAreas'
CATALOG=OUT/'areas.json'

def obj(cell,name,*instances): return dict(cell=cell,name=name,instances=instances)
# Instances are left,top,right,bottom bounds; sprites preserve their aspect ratio and sit at the bottom.
SPECS={
1:dict(name='Sapphire Castle',subtitle='Raise the crown of the kingdom',outdoor=True,grid=(2,3),tasks=[
 obj(0,'Raise the sapphire castle',(.23,.135,.77,.424)),
 obj(1,'Restore the crown fountain',(.315,.50,.685,.706)),
 obj(2,'Build the royal gazebos',(.045,.408,.30,.591),(.70,.408,.955,.591)),
 obj(3,'Shape the emerald topiaries',(.03,.673,.29,.79),(.71,.673,.97,.79)),
 obj(4,'Place the swan guardians',(.14,.515,.28,.662),(.72,.515,.86,.662)),
 obj(5,'Plant the royal flower beds',(.12,.747,.38,.85),(.62,.747,.88,.85)),
]),
2:dict(name='Crown Throne Room',subtitle='A home for the royal court',outdoor=False,grid=(2,3),cells=[(0,0,512,480),(512,0,1024,480),(0,480,530,1004),(530,480,1024,1004),(0,1004,480,1536),(480,1004,1024,1536)],tasks=[
 obj(0,'Place the sapphire throne',(.31,.275,.69,.485)),
 obj(1,'Roll out the royal carpet',dict(box=(.345,.47,.655,.86),stretch=True)),
 obj(2,'Hang the court banners',(.20,.20,.80,.395)),
 obj(3,'Raise the crystal chandelier',(.33,.09,.67,.265)),
 obj(4,'Stand the golden guardians',dict(box=(.10,.425,.28,.645),source=(0,0,.5,1)),dict(box=(.72,.425,.90,.645),source=(.5,0,1,1))),
 obj(5,'Set the velvet benches',dict(box=(.04,.65,.36,.80),source=(0,0,.5,1)),dict(box=(.64,.65,.96,.80),source=(.5,0,1,1))),
]),
3:dict(name='Royal Dining Salon',subtitle='Set a feast for the kingdom',outdoor=False,grid=(3,3),tasks=[
 obj(0,'Set the royal banquet',(.22,.535,.78,.72)),
 obj(1,'Arrange the dining chairs',(.17,.565,.33,.72),(.67,.565,.83,.72)),
 obj(2,'Fill the china cabinet',(.10,.305,.34,.53)),
 obj(3,'Tune the grand piano',(.65,.43,.96,.60)),
 obj(4,'Light the golden fireplace',(.37,.335,.64,.535)),
 obj(5,'Drape the sapphire curtains',dict(box=(.075,.246,.293,.478),stretch=True),dict(box=(.707,.246,.925,.478),stretch=True)),
 obj(6,'Hang the banquet chandelier',(.345,.115,.655,.305)),
]),
4:dict(name="Queen's Crown Garden",subtitle='Make the royal gardens bloom',outdoor=True,grid=(3,3),cells=[(0,0,438,449),(438,0,825,449),(825,0,1254,449),(0,449,438,799),(438,449,825,799),(825,449,1254,799),(0,799,438,1254),(438,799,825,1254)],tasks=[
 obj(0,'Raise the flowering arch',(.355,.095,.645,.257)),
 obj(1,'Build the garden pavilion',(.035,.23,.34,.436)),
 obj(2,'Restore the garden fountain',(.34,.205,.66,.403)),
 obj(3,'Plant the rose terraces',(.06,.495,.37,.61),(.63,.495,.94,.61)),
 obj(4,'Sculpt the emerald hedges',(.04,.61,.34,.727),(.66,.61,.96,.727)),
 obj(5,'Place the royal garden seats',(.675,.288,.935,.403)),
 obj(6,'Set the swan statues',(.11,.36,.255,.506),(.745,.36,.89,.506)),
 obj(7,'Light the garden lanterns',(.375,.47,.625,.70)),
]),
5:dict(name='Grand Royal Library',subtitle='A treasury of stories and stars',outdoor=False,grid=(3,3),cells=[(0,0,381,471),(381,0,847,471),(847,0,1254,471),(0,471,381,856),(381,471,837,846),(837,471,1254,864),(0,856,448,1254),(448,846,837,1254)],tasks=[
 obj(0,'Fill the western bookcase',(.17,.22,.40,.495)),
 obj(1,'Restore the eastern shelves',(.46,.22,.865,.495)),
 obj(2,'Set the royal reading desk',(.315,.485,.685,.65)),
 obj(3,'Place the velvet reading chair',(.67,.54,.91,.718)),
 obj(4,'Chart the celestial globe',(.055,.455,.295,.655)),
 obj(5,'Raise the library ladder',(.67,.32,.83,.53)),
 obj(6,'Unroll the sapphire rug',(.31,.665,.69,.81)),
 obj(7,'Light the library chandelier',(.33,.065,.67,.265)),
]),
}

def bbox_alpha(im,threshold=8):
    return im.getchannel('A').point(lambda a:255 if a>threshold else 0).getbbox()

def cuts(im,axis,count):
    a=im.getchannel('A').point(lambda v:1 if v>32 else 0)
    if axis==0:a=a.transpose(Image.Transpose.TRANSPOSE)
    raw=a.tobytes();sums=[sum(raw[y*a.width:(y+1)*a.width]) for y in range(a.height)]
    size=im.width if axis==0 else im.height
    result=[0]
    for i in range(1,count):
        expected=round(i*size/count); radius=round(size/count*.055)
        candidates=range(expected-radius,expected+radius+1)
        best=min(candidates,key=lambda p:(sum(sums[max(0,p-1):p+2]),abs(p-expected)))
        result.append(best)
    return result+[size]

def slice_sheet(folder,spec):
    sheet=Image.open(folder/'objects.png').convert('RGBA'); cols,rows=spec['grid']
    xs,ys=cuts(sheet,0,cols),cuts(sheet,1,rows)
    folder.joinpath('Sprites').mkdir(exist_ok=True)
    sprites=[];slices=[]
    for n in range(len(spec['tasks'])):
        col,row=n%cols,n//cols;box=spec.get('cells',[])[n] if 'cells' in spec else (xs[col],ys[row],xs[col+1],ys[row+1]);im=sheet.crop(box)
        override=folder/f'object-{n+1:02}-replacement.png'
        if n==2 and folder.name=='Area03':override=folder/'cabinet-replacement.png'
        if override.exists():im=Image.open(override).convert('RGBA')
        bounds=bbox_alpha(im)
        assert bounds,('empty sprite cell',folder,n)
        x0,y0,x1,y1=bounds;bounds=(max(0,x0-3),max(0,y0-3),min(im.width,x1+3),min(im.height,y1+3))
        im=im.crop(bounds);sprites.append(im)
        im.save(folder/'Sprites'/f'object-{n+1:02}.png')
        slices.append(dict(cell=n,bounds=box,trim=bounds))
    (folder/'atlas-slices.json').write_text(json.dumps(slices,indent=2))
    return sprites

def fit_box(im,box):
    x0,y0,x1,y1=[round(v*s) for v,s in zip(box,[W,H,W,H])]
    size=min((x1-x0)/im.width,(y1-y0)/im.height)
    im=im.resize((max(1,round(im.width*size)),max(1,round(im.height*size))),Image.Resampling.LANCZOS)
    return im,(round((x0+x1-im.width)/2),y1-im.height)

def webp(im,path):
    # Exact RGBA source for replay/final consistency. GPU compression is handled by Unity's existing importer.
    im.save(path,'WEBP',lossless=True,method=6)

def roundbox(box):
    x0,y0,x1,y1=box
    return max(0,x0//4*4),max(0,y0//4*4),min(W,math.ceil(x1/4)*4),min(H,math.ceil(y1/4)*4)

def build(i,spec,entry):
    folder=HERE/f'Area{i:02}';dest=OUT/'RedesignV1'/f'Area{i:02}';dest.mkdir(parents=True,exist_ok=True)
    base=Image.open(folder/'before.png').convert('RGBA').resize((W,H),Image.Resampling.LANCZOS)
    assert base.getchannel('A').getextrema()==(255,255),'background must be opaque'
    sprites=slice_sheet(folder,spec);complete=base.copy();records=[];layers=[]
    old_costs=[t['cost'] for t in entry['tasks']]
    assert len(old_costs)==len(spec['tasks'])
    prefix=f'RedesignV1/Area{i:02}/'
    entry.update({k:spec[k] for k in ['name','subtitle','outdoor']})
    entry.update(placeholder=False,bakedAtmosphere=True,before=prefix+'before',after=prefix+'after',thumbBefore=prefix+'thumb-before',thumbAfter=prefix+'thumb-after')
    for n,t in enumerate(spec['tasks']):
        im=sprites[t['cell']];layer=Image.new('RGBA',(W,H))
        for inst in t['instances']:
            source=im
            stretch=False
            if isinstance(inst,dict):
                if 'source' in inst:
                    r=inst['source'];source=im.crop(tuple(round(v*s) for v,s in zip(r,[im.width,im.height,im.width,im.height])))
                    bb=bbox_alpha(source)
                    if bb:source=source.crop(bb)
                stretch=inst.get('stretch',False);inst=inst['box']
            if stretch:
                b=[round(v*s) for v,s in zip(inst,[W,H,W,H])]
                part=source.resize((b[2]-b[0],b[3]-b[1]),Image.Resampling.LANCZOS);pos=b[:2]
            else:part,pos=fit_box(source,inst)
            layer.alpha_composite(part,pos)
        # Preserve all nonzero alpha, including fine shadow edges, within the padded compressed block rect.
        bounds=roundbox(layer.getbbox());patch=layer.crop(bounds);x0,y0,x1,y1=bounds
        complete.alpha_composite(layer);layers.append((patch,bounds))
        patch.save(folder/f'task-{n+1:02}.png');webp(patch,dest/f'task-{n+1:02}.webp')
        icon=Image.new('RGBA',(256,256));thumb=im.copy();thumb.thumbnail((236,236),Image.Resampling.LANCZOS);icon.alpha_composite(thumb,((256-thumb.width)//2,(256-thumb.height)//2));webp(icon,dest/f'icon-{n+1:02}.webp')
        records.append(dict(name=t['name'],cost=old_costs[n],patch=prefix+f'task-{n+1:02}',icon=prefix+f'icon-{n+1:02}',x0=x0/W,y0=y0/H,x1=x1/W,y1=y1/H))
    entry['tasks']=records
    reconstruction=base.copy()
    for patch,b in layers:reconstruction.alpha_composite(patch,b[:2])
    assert complete.tobytes()==reconstruction.tobytes(),'final differs from task composition'
    complete.save(folder/'after.png');base.save(folder/'before-runtime.png')
    for name,im in [('before',base),('after',complete),('thumb-before',base.resize((240,428),Image.Resampling.LANCZOS)),('thumb-after',complete.resize((240,428),Image.Resampling.LANCZOS))]:webp(im,dest/(name+'.webp'))
    manifest=dict(number=i,name=spec['name'],tasks=len(records),stars=sum(old_costs),source_sheet_sha256=hashlib.sha256((folder/'objects.png').read_bytes()).hexdigest(),source_background_sha256=hashlib.sha256((folder/'before.png').read_bytes()).hexdigest(),exact_composite_verified=True,placements=spec['tasks'])
    (folder/'manifest.json').write_text(json.dumps(manifest,indent=2))
    print(i,spec['name'],len(records),'tasks',sum(old_costs),'stars',flush=True)
    return entry

if __name__=='__main__':
    selected=[int(a) for a in sys.argv[1:] if a.isdigit()] or sorted(SPECS)
    catalog=json.loads(CATALOG.read_text(encoding='utf-8'))
    original=HERE/'Backup-Before-Integration/Assets/JellyGarden/Resources/RoyalAreas/areas.json'
    if not original.exists():original=HERE/'baseline-areas.json'
    baseline=json.loads(original.read_text(encoding='utf-8'))
    for i in selected:
        catalog['areas'][i-1]=build(i,SPECS[i],catalog['areas'][i-1])
        assert [t['cost'] for t in catalog['areas'][i-1]['tasks']]==[t['cost'] for t in baseline['areas'][i-1]['tasks']]
    CATALOG.write_text(json.dumps(catalog,indent=1,ensure_ascii=False)+'\n',encoding='utf-8')
    (HERE/'placements.json').write_text(json.dumps(SPECS,indent=2))
