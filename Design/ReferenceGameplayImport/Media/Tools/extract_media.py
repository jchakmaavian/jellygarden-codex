from pathlib import Path
import sys, json, struct, math, zlib, hashlib, wave, re
ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Design/ReferenceGameplayImport/Media'
sys.path.insert(0,str(OUT/'Tools/Python'))
import UnityPy

SOURCE=ROOT/'Design/Inspection-Levels01-50/ReferencePackage/datapack.unity3d_data/resources.assets'
env=UnityPy.load(str(SOURCE)); objects={o.path_id:o for o in env.objects}; cache={}
def tree(pid):
    if pid not in cache:cache[pid]=objects[pid].read_typetree()
    return cache[pid]
def pid(p):return p['m_PathID']
def name(p):return tree(p).get('m_Name',str(p))
def editor_value(value,node):
    """Restore serializedVersion tags absent from UnityPy's data-only dictionary.
    Omitting these invokes pre-2021 enum, gradient and emission migrations in Unity's JSON reader.
    """
    if isinstance(value,dict):
        result={}
        if node.m_Version>1:result['serializedVersion']=node.m_Version
        children={c.m_Name:c for c in node.m_Children}
        for key,v in value.items():result[key]=editor_value(v,children[key]) if key in children else v
        return result
    if isinstance(value,list):
        n=node
        if n.m_Children and n.m_Children[0].m_Type=='Array':n=n.m_Children[0]
        data=next((c for c in n.m_Children if c.m_Name=='data'),None)
        return [editor_value(v,data) for v in value] if data else value
    return value
def editor_tree(path_id):return editor_value(tree(path_id),objects[path_id]._get_typetree_node())
def traverse(tid,path=''):
    tr=tree(tid); go=tree(pid(tr['m_GameObject'])); path=path+'/'+go['m_Name'] if path else go['m_Name']
    yield tid,go,path
    for child in tr['m_Children']:yield from traverse(pid(child),path)

if __name__=='__main__':
    roots={'Rocket':84786,'Propeller':80397,'TNT':80398,'ColorBall':80401}
    for label,tid in roots.items():
        seq=list(traverse(tid)); print(label,'nodes',len(seq),'root',seq[0][1]['m_Name']);
        print('\n'.join(p for _,_,p in seq[:60]))
        comp=[]
        for _,go,_ in seq:
            for c in go['m_Component']:
                oid=pid(c['component']);o=objects[oid];comp.append((oid,o.type.name))
        print('componenttypes',sorted(set(x[1] for x in comp)))
        mats=set()
        for oid,typ in comp:
            if typ.endswith('Renderer'):
                for m in tree(oid).get('m_Materials',[]):mats.add((m['m_FileID'],pid(m)))
        print('materials',[(f,p,name(p) if f==0 else '') for f,p in mats])
    sfx=ROOT/'Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio';sfx.mkdir(parents=True,exist_ok=True)
    data=(OUT/'resources.resource').read_bytes()
    import fmod_toolkit
    selected=[]
    include=re.compile(r'^(rocket_|tnt_|lightball_|propeller_(creation|hit|fly|take_off|tnt_combo)|match_(explode|item)|fall_hit_|box_(layer_break|break)|grass_clear|coin_(collect|appear|counter)|cupboard_|potion_|mailbox_|egg_break)')
    for o in env.objects:
        if o.type.name!='AudioClip':continue
        t=tree(o.path_id);n=t['m_Name']
        if not include.search(n):continue
        res=t['m_Resource'];buf=data[res['m_Offset']:res['m_Offset']+res['m_Size']]
        try:
            waves=fmod_toolkit.raw_to_wav(buf,n,t['m_Channels'],t['m_Frequency'])
            for fn,wav in waves.items():
                path=sfx/(n+'__'+str(o.path_id)+'.wav')
                if not path.exists() or path.read_bytes()!=wav:path.write_bytes(wav)
                selected.append(dict(name=n,pathId=o.path_id,assetPath=path.relative_to(ROOT).as_posix(),resourcesPath='ReferenceGameplay/Audio/'+path.stem,source='resources.assets',sourcePayloadSHA256=hashlib.sha256(buf).hexdigest(),length=t['m_Length'],channels=t['m_Channels'],sampleRate=t['m_Frequency']))
        except Exception as ex:print('SFX FAILED',n,str(ex))
    (OUT/'AudioManifest.json').write_text(json.dumps(selected,indent=2))
    print('AUDIO EXTRACTED',len(selected))
