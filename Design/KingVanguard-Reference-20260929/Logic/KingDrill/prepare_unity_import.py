"""Create JsonUtility-compatible native rig import data; never edits original data."""
from pathlib import Path
import sys,json,zlib
OUT=Path(__file__).resolve().parent
PROJECT=OUT.parents[3]
sys.path.insert(0,str(PROJECT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy

def vec(a,keys='xyz'):return dict(zip(keys,a))
def main():
    src=json.loads((OUT/'native-king-drill-import-data.json').read_text(encoding='utf8'))
    env=UnityPy.load(str(OUT.parents[1]/'EmbeddedKingBundles/kingdrill_v3'))
    trees={o.path_id:o.read_typetree() for o in env.objects}
    atlas=next(t for t in trees.values() if t.get('m_Name')=='KingDrillAtlas')
    amap={json.dumps(k,sort_keys=True):v for k,v in atlas['m_RenderDataMap']}
    sprites=[]
    for s in src['sprites']:
        packing=amap[json.dumps(trees[s['sourcePathId']]['m_RenderDataKey'],sort_keys=True)]
        rotation=(packing['settingsRaw']>>2)&15
        assert rotation in (0,1,2,3), '90 degree packing needs separate mapping'
        uv=packing['uvTransform']
        rect=packing['textureRect'];packeduv=[]
        for v in s['vertices']:
            x=v[0]*uv['x']+uv['y'];y=v[1]*uv['z']+uv['w']
            if rotation in (1,3):x=2*rect['x']+rect['width']-x
            if rotation in (2,3):y=2*rect['y']+rect['height']-y
            packeduv.append({'x':x/1024,'y':y/2048})
        weights=[]
        if s['boneWeights']:
            for indices,w in zip(s['boneIndices'],s['boneWeights']):
                weights.append({**{f'boneIndex{i}':indices[i] for i in range(4)},**{f'weight{i}':w[i] for i in range(4)}})
        sprites.append(dict(id=s['sourcePathId'],name=s['name'],vertices=[vec(v) for v in s['vertices']],uv=packeduv,triangles=s['indices'],weights=weights,bindPoses=s['bindPoses'],textureRect=packing['textureRect'],pivot=s['pivot'],pixelsPerUnit=s['pixelsToUnits']))
    skinmap={s['path']:s['data'] for s in src['skins']}
    renderermap={r['path']:r['data'] for r in src['renderers']}
    nodes=[]
    for n in src['nodes']:
        tr=n['transform'];r=renderermap.get(n['path']);skin=skinmap.get(n['path'])
        renderer=None
        if r:
            renderer=dict(spriteId=r['m_Sprite']['m_PathID'],enabled=r['m_Enabled'],color=r['m_Color'],sortingOrder=r['m_SortingOrder'],sortingLayerId=r['m_SortingLayerID'],flipX=r['m_FlipX'],flipY=r['m_FlipY'],
                          bones=[p['m_PathID'] for p in skin['m_BoneTransforms']] if skin else [],rootBone=skin['m_RootBone']['m_PathID'] if skin else 0)
        nodes.append(dict(id=n['transformId'],parentId=tr['m_Father']['m_PathID'] if n['transformId']!=src['rootTransformId'] else 0,name=n['name'],path=n['path'],active=bool(n['active']),position=tr['m_LocalPosition'],rotation=tr['m_LocalRotation'],scale=tr['m_LocalScale'],hasRenderer=renderer is not None,renderer=renderer))
    data=dict(source='royal_thron_rfv387.apk / kingdrill_v3',atlasWidth=1024,atlasHeight=2048,rootId=src['rootTransformId'],nodes=nodes,sprites=sprites,clips=src['clips'])
    (OUT/'UnityImportData.json').write_text(json.dumps(data,ensure_ascii=False),encoding='utf8')
    print('Prepared',len(nodes),'nodes',len(sprites),'sprites',len(data['clips']),'clips')

if __name__=='__main__':main()
