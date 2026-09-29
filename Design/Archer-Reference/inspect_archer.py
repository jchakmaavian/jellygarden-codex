from pathlib import Path
import sys,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy
env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data/resources.assets'))
objs={o.path_id:o for o in env.objects}
found=[]
for o in env.objects:
    if o.type.name not in ('AnimationClip','AnimatorController','GameObject','Material','Texture2D','Sprite','AudioClip','TextAsset'):continue
    t=o.read_typetree();name=t.get('m_Name','')
    if 'arrow' not in name.lower():continue
    found.append(dict(id=o.path_id,type=o.type.name,name=name))
    if o.type.name not in ('Texture2D','Sprite'):
        (OUT/f'{o.type.name}-{o.path_id}.json').write_text(json.dumps(t,indent=2),encoding='utf8')
(OUT/'inventory.json').write_text(json.dumps(found,indent=2))
print(json.dumps(found,indent=2))

def walk(gid,path=''):
    go=objs[gid].read_typetree()
    comps={objs[c['component']['m_PathID']].type.name:objs[c['component']['m_PathID']].read_typetree() for c in go['m_Component'] if objs[c['component']['m_PathID']].type.name != 'MonoBehaviour'}
    tr=comps.get('Transform',comps.get('RectTransform'))
    nodes.append(dict(id=gid,name=go['m_Name'],path=path,active=go['m_IsActive'],components=comps))
    for ref in tr['m_Children']:
        ct=objs[ref['m_PathID']].read_typetree();cg=ct['m_GameObject']['m_PathID'];name=objs[cg].read_typetree()['m_Name']
        walk(cg,(path+'/' if path else '')+name)
for gid in [46037]:
    nodes=[];walk(gid)
    (OUT/f'hierarchy-{gid}.json').write_text(json.dumps(nodes,indent=2))
    print('HIERARCHY',gid,[(n['path'],list(n['components'])) for n in nodes])

