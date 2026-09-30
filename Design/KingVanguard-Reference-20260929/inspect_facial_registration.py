"""Reference-only face geometry inventory. Does not edit any runtime asset/artwork."""
from pathlib import Path
import json,sys,math,collections,subprocess
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
from PIL import Image

def atlas(path):
    result={};region=None;size=None
    for line in path.read_text(encoding='utf8').splitlines():
        if line.startswith('size:'):size=[int(x) for x in line.split(':',1)[1].split(',')]
        elif line and not line[0].isspace() and ':' not in line and not line.endswith('.png'):
            region={};result[line]=region
        elif region is not None and ':' in line:
            k,v=line.strip().split(':',1);region[k]=[int(x.strip()) for x in v.split(',')] if k in ['xy','size','orig','offset'] else v.strip()
    return size,result

def fingerprint(j,attachment):
    result={k:v for k,v in attachment.items() if k not in ['name','path','color','edges','width','height']}
    if result.get('vertices',{}).get('weighted'):
        result['vertices']=dict(weighted=True,values=[[[j['bones'][w[0]]['name'],*w[1:]] for w in ws] for ws in result['vertices']['values']])
    return result

rigs={};face_bones=['yuz','on_yuz','biyik','biyik_sag_1','biyik_sag_2','biyik_sag_3','biyik_sol_1','biyik_sol_2','biyik_sol_3','burun','sag_goz_aki','sag_goz_bebegi','sag_kas_1','sag_kas_2','sag_yanak','sol_goz_aki','sol_goz_bebegi','sol_kas_1','sol_kas_2','sol_yanak','sag_kulak','sol_kulak','tac','ust_dudak','alt_dudak','yuz_arka','yuz_kontrol']
for stem in ['King','AreaCompleted']:
    j=json.loads((OUT/(stem+'.skel.spine38.json')).read_text(encoding='utf8'));size,regions=atlas(OUT/'Original-rfv387'/(stem+'.atlas'))
    bones=[];slots=[]
    for i,b in enumerate(j['bones']):
        if b['name'] not in face_bones:continue
        bones.append(dict(index=i,name=b['name'],parent=j['bones'][b['parent']]['name'] if b['parent']>=0 else None,rotation=b['transform'][0],x=b['transform'][1],y=b['transform'][2],scale_x=b['transform'][3],scale_y=b['transform'][4],shear_x=b['transform'][5],shear_y=b['transform'][6],length=b['transform'][7],mode=b['mode']))
    for si,s in enumerate(j['slots']):
        for n,a in j['skins'][0]['attachments'].get(str(si),{}).items():
            if s['name'] in ['Frame Background3','Pelerin','sol frame','sag frame','frame_clipping','sag','sol'] or not (s['name'] in regions):continue
            bone_name=j['bones'][s['bone']]['name']
            # Face/background parts are attached to named facial bones; exclude other torso/scenery slots.
            if bone_name not in face_bones:continue
            mesh=a['type']==2;v=a.get('vertices',{});weights=v.get('values',[]) if v.get('weighted') else []
            names=sorted({j['bones'][w[0]]['name'] for ws in weights for w in ws})
            rec=dict(slot_index=si,slot=s['name'],setup_attachment=s['attachment'],slot_bone=bone_name,name=n,type='mesh' if mesh else 'region',weighted=v.get('weighted',False),influence_bones=names,vertex_count=len(a.get('uvs',[]))//2 if mesh else 4,triangle_count=len(a.get('triangles',[]))//3 if mesh else 2,mesh_dimensions=[a.get('width'),a.get('height')] if mesh else None,region_transform=a.get('transform'),atlas=regions.get(a.get('path',n)),deform_animations=[name for name,anim in j['animations'].items() if any(d['slot']==si and d['name']==n for d in anim['deform'])])
            slots.append(rec)
    rigs[stem]=dict(atlas_size=size,bones=bones,slots=slots,constraints=j['transformConstraints'])

ja=json.loads((OUT/'King.skel.spine38.json').read_text(encoding='utf8'));jb=json.loads((OUT/'AreaCompleted.skel.spine38.json').read_text(encoding='utf8'))
def by_name(j):return {n:a for ats in j['skins'][0]['attachments'].values() for n,a in ats.items()}
aa=by_name(ja);bb=by_name(jb);shared=[]
for s in rigs['King']['slots']:
    name=s['name']
    t=next((v for v in rigs['AreaCompleted']['slots'] if v['name']==name),None)
    if not t:continue
    shared.append(dict(name=name,same_type=aa[name]['type']==bb[name]['type'],same_atlas_trim_dimensions=s['atlas']['size']==t['atlas']['size'],same_original_dimensions=s['atlas']['orig']==t['atlas']['orig'],same_uvs=aa[name].get('uvs')==bb[name].get('uvs'),same_triangles=aa[name].get('triangles')==bb[name].get('triangles'),same_geometry_after_bone_name_mapping=fingerprint(ja,aa[name])==fingerprint(jb,bb[name])))

# The actual attachment sheet is 1536x1024. Search candidates by dimensions only;
# V1 art is explicitly excluded and is never read as a redesign reference.
candidates=[];scanned=0
paths=subprocess.check_output(['rg','--files','Design','-g','*.png','-g','*.jpg','-g','*.jpeg','-g','*.webp'],cwd=ROOT,text=True,encoding='utf8').splitlines()
for rel in paths:
    if 'Characters\\KingVenguard\\V1' in rel or 'Characters/KingVenguard/V1' in rel:continue
    p=ROOT/rel
    try:
        with Image.open(p) as im:
            scanned+=1
            if im.size==(1536,1024):candidates.append(rel)
    except Exception:pass
data=dict(transform_order='rotation,x,y,scaleX,scaleY,shearX,shearY,length',rigs=rigs,shared_regions=shared,reference_sheet_search=dict(dimensions=[1536,1024],scanned_non_v1_images=scanned,candidates=candidates))
(OUT/'facial-rig-registration.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
print('SHARED',json.dumps(shared,ensure_ascii=False));print('SHEET CANDIDATES',candidates)
for stem,rig in rigs.items():
    print(stem,'KEY BONES',[(b['name'],b['parent'],b['rotation'],b['x'],b['y'],b['scale_x'],b['scale_y']) for b in rig['bones'] if b['name'] in ['yuz','on_yuz','burun','biyik','yuz_arka','yuz_kontrol','sag_goz_aki','sol_goz_aki','sag_kas_1','sol_kas_1']])
    print(stem,'FACE REGIONS',[(s['name'],s['type'],s['weighted'],s['vertex_count'],s['atlas']['size'],s['atlas']['orig'],s['slot_bone'],s['influence_bones']) for s in rig['slots']])
