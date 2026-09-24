from export_visuals import *

GOIDS=[22741,41098,33946,47294,47297,47298,47299,47300,47307,49218,39253,47301,47303,47295,47296,47305]
def clean(v):
    if isinstance(v,float) and not math.isfinite(v):return 0.0
    if isinstance(v,list):return [clean(x) for x in v]
    if isinstance(v,dict):return {k:clean(x) for k,x in v.items()}
    return v
def main(goids=None,filename='NativeParticles.json'):
    prefabs=[];needed=set();owned=set();notes=[]
    for gid in (goids or GOIDS):
        go=tree(gid);root=next(pid(c['component']) for c in go['m_Component'] if objects[pid(c['component'])].type.name=='Transform');nodes=[]
        for tid,g,path in traverse(root):
            tr=tree(tid);comps=[];owned.update([tid,pid(tr['m_GameObject'])])
            for c in g['m_Component']:
                cp=pid(c['component']);typ=objects[cp].type.name
                if typ=='MonoBehaviour':continue
                if typ not in ['Transform','ParticleSystem','ParticleSystemRenderer','SpriteRenderer','LineRenderer','SortingGroup']:
                    notes.append(dict(prefab=go['m_Name'],path=path,unsupported=typ,pathId=cp));continue
                t=tree(cp);owned.add(cp);needed.update((r['m_FileID'],pid(r)) for r in refs(t) if pid(r))
                comps.append(dict(pathId=cp,type=typ,json=json.dumps(clean(editor_tree(cp)),separators=(',',':'))))
            nodes.append(dict(pathId=pid(tr['m_GameObject']),transformId=tid,parentTransformId=pid(tr['m_Father']) if tid!=root else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=tr['m_LocalPosition'],localRotation=tr['m_LocalRotation'],localScale=tr['m_LocalScale'],components=comps))
        prefabs.append(dict(name=go['m_Name'],sourcePathId=gid,nodes=nodes))
    mats=[];textures=[];sprites=set();external=set();globalenv=UnityPy.load(str(ROOT/'Design/Inspection-Levels01-50/ReferencePackage/data.unity3d_data/globalgamemanagers.assets'));globals={o.path_id:o for o in globalenv.objects}
    for f,p in needed:
        if f!=0:external.add((f,p));continue
        if p in owned:continue
        o=objects[p]
        if o.type.name=='Sprite':sprites.add(p)
        elif o.type.name=='Material':
            t=tree(p);sh=t['m_Shader'];shader=tree(pid(sh)) if sh['m_FileID']==0 else globals[pid(sh)].read_typetree();shadername=shader['m_ParsedForm']['m_Name']
            mat=dict(pathId=p,name=t['m_Name'],shaderName=shadername,queue=t['m_CustomRenderQueue'],keywords=t.get('m_ShaderKeywords',''),textures=[],floats=[],colors=[])
            for key,v in t['m_SavedProperties']['m_TexEnvs']:
                tp=pid(v['m_Texture']);mat['textures'].append(dict(name=key,pathId=tp,scale=v['m_Scale'],offset=v['m_Offset']));
                if tp and v['m_Texture']['m_FileID']==0:textures.append(tp)
            mat['floats']=[dict(name=k,value=v) for k,v in t['m_SavedProperties']['m_Floats']]
            mat['colors']=[dict(name=k,color=clean(v)) for k,v in t['m_SavedProperties']['m_Colors']]
            mats.append(mat)
        else:notes.append(dict(dependencyPathId=p,unsupported=o.type.name,name=o.peek_name()))
    art=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text()); allsprites={s['sourcePathId']:s for s in art['sprites']}; alltex={s['pathId']:s for s in art['textures']}
    missing=sorted(sprites-allsprites.keys());assert not missing,missing
    for s in sprites:textures.append(allsprites[s]['sourceTexturePathId'])
    dest=ROOT/'Assets/ReferenceGameplay/VFX/Textures';dest.mkdir(parents=True,exist_ok=True)
    texdata=[]
    for tp in sorted(set(textures)):
        if tp in alltex:texdata.append(dict(pathId=tp,name=alltex[tp]['name'],assetPath=alltex[tp]['assetPath']));continue
        ob=objects[tp];tx=ob.read();path=dest/(str(tp)+'-'+tx.m_Name+'.png');tx.image.save(path)
        texdata.append(dict(pathId=tp,name=tx.m_Name,assetPath=path.relative_to(ROOT).as_posix()))
    doc=dict(prefabs=prefabs,materials=mats,textures=texdata,sprites=[allsprites[s] for s in sorted(sprites)],externalReferences=sorted(external),limitations=notes)
    (OUT/filename).write_text(json.dumps(doc,separators=(',',':')))
    print('PREFABS',len(prefabs),'materials',[(m['name'],m['shaderName']) for m in mats],'textures',len(texdata),'sprites',len(sprites),'notes',notes,'external',external)

if __name__=='__main__':
    if '--blockers' in sys.argv:main([22703,22709,22716,22720,22739,22757,22760,38209,38210,38211,38212],'NativeBlockerParticles.json')
    else:main()
