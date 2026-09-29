from pathlib import Path
import sys,json,math,hashlib,ast
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1];OUT=HERE
TOOLS=ROOT/'Design/ReferenceGameplayImport/Media/Tools'
sys.path.insert(0,str(TOOLS/'Python'))
import UnityPy
module=ast.parse((TOOLS/'extract_media.py').read_text())
exec(compile(ast.Module(body=[n for n in module.body if isinstance(n,ast.FunctionDef) and n.name=='editor_value'],type_ignores=[]),'<existing native converter>','exec'))
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'));objs={o.path_id:o for o in env.objects};cache={}
def tree(p):
    if p not in cache:cache[p]=objs[p].read_typetree()
    return cache[p]
def pid(p):return p['m_PathID']
def refs(v):
    if isinstance(v,dict):
        if 'm_PathID' in v:yield v
        else:
            for x in v.values():yield from refs(x)
    elif isinstance(v,list):
        for x in v:yield from refs(x)
def clean(v):
    if isinstance(v,float) and not math.isfinite(v):return 0.
    if isinstance(v,list):return [clean(x) for x in v]
    if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
    return v
dest=ROOT/'Assets/ReferenceGameplay/Tools/Archer/VFXTextures';dest.mkdir(parents=True,exist_ok=True)
prefabs=[];needed=set();owned=set()
def walk(gid,nodes,parent=0):
    g=tree(gid);tid=next(pid(c['component']) for c in g['m_Component'] if objs[pid(c['component'])].type.name=='Transform');t=tree(tid);comps=[]
    owned.update([gid,tid])
    for c in g['m_Component']:
        cp=pid(c['component']);o=objs[cp]
        if o.type.name not in ['Transform','ParticleSystem','ParticleSystemRenderer','SpriteRenderer','SortingGroup']:continue
        owned.add(cp);needed.update((r['m_FileID'],pid(r)) for r in refs(tree(cp)) if pid(r))
        comps.append(dict(pathId=cp,type=o.type.name,json=json.dumps(clean(editor_value(tree(cp),o._get_typetree_node())),separators=(',',':'))))
    nodes.append(dict(pathId=gid,transformId=tid,parentTransformId=parent,name=g['m_Name'],active=g['m_IsActive'],localPosition=t['m_LocalPosition'],localRotation=t['m_LocalRotation'],localScale=t['m_LocalScale'],components=comps))
    for ch in t['m_Children']:walk(pid(tree(pid(ch))['m_GameObject']),nodes,tid)
# Impact belongs to the archer action; reward particles are kept separately for reuse.
for gid,name in [(27388,'ArcherTrailRfv387'),(57173,'ArcherSmokeRfv387')]:
    nodes=[];walk(gid,nodes);prefabs.append(dict(name=name,nodes=nodes))
genv=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/data.unity3d_data/globalgamemanagers.assets'));globs={o.path_id:o for o in genv.objects}
mats=[];textures=set();sprites=[];external=[]
for f,p in sorted(needed):
    if f:external.append([f,p]);continue
    if p in owned:continue
    o=objs[p];t=tree(p)
    if o.type.name=='Material':
        sh=t['m_Shader'];shader=(tree(pid(sh)) if sh['m_FileID']==0 else globs[pid(sh)].read_typetree())['m_ParsedForm']['m_Name']
        m=dict(pathId=p,name=t['m_Name'],shaderName=shader,queue=t['m_CustomRenderQueue'],keywords=t.get('m_ShaderKeywords',''),textures=[],floats=[],colors=[])
        for k,v in t['m_SavedProperties']['m_TexEnvs']:
            tp=pid(v['m_Texture']);m['textures'].append(dict(name=k,pathId=tp,scale=v['m_Scale'],offset=v['m_Offset']))
            if tp:textures.add(tp)
        m['floats']=[dict(name=k,value=v) for k,v in t['m_SavedProperties']['m_Floats']]
        m['colors']=[dict(name=k,color=clean(v)) for k,v in t['m_SavedProperties']['m_Colors']];mats.append(m)
    elif o.type.name=='Sprite':
        rd=t['m_RD'];tp=pid(rd['texture']);textures.add(tp)
        sprites.append(dict(sourcePathId=p,sourceTexturePathId=tp,name=t['m_Name'],rect=rd['textureRect'],pivot=t['m_Pivot'],border=t['m_Border'],pixelsPerUnit=t['m_PixelsToUnits']))
    elif o.type.name=='Transform':pass # Parent outside the extracted effect root.
    else:raise ValueError((p,o.type.name))
texdata=[]
for p in sorted(textures):
    tx=objs[p].read();path=dest/(str(p)+'-'+tx.m_Name+'.png');tx.image.save(path)
    texdata.append(dict(pathId=p,name=tx.m_Name,assetPath=path.relative_to(ROOT).as_posix()))
OFFSET=390000000
def remap(v):
    if isinstance(v,list):return [remap(x) for x in v]
    if isinstance(v,dict):
        if 'm_PathID' in v:return {k:(x+OFFSET if k=='m_PathID' and x and v['m_FileID']==0 else x) for k,x in v.items()}
        return {k:(x+OFFSET if k in ('pathId','transformId','parentTransformId','sourcePathId','sourceTexturePathId') and x else json.dumps(remap(json.loads(x)),separators=(',',':')) if k=='json' else remap(x)) for k,x in v.items()}
    return v
(OUT/'NativeArcherParticles.json').write_text(json.dumps(remap(dict(prefabs=prefabs,materials=mats,textures=texdata,sprites=sprites)),separators=(',',':')))
report=dict(prefabs=[p['name'] for p in prefabs],systems=sum(c['type']=='ParticleSystem' for p in prefabs for n in p['nodes'] for c in n['components']),materials=[(m['name'],m['shaderName']) for m in mats],sprites=len(sprites),textures=len(textures),external=external)
(OUT/'extraction-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))

