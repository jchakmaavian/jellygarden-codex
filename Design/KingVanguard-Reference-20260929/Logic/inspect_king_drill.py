"""Normalize native KingDrill rig and preserve mesh/curve evidence without Unity."""
from pathlib import Path
import sys, json, zlib, collections, math
OUT=Path(__file__).resolve().parent/'KingDrill'
PROJECT=OUT.parents[3]
REF=OUT.parents[1]
sys.path.insert(0,str(PROJECT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
sys.path.insert(0,str(PROJECT/'Design/ReferenceGameplayImport/Media/Tools'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
from export_visuals import decode_curves

def main():
    OUT.mkdir(exist_ok=True)
    env=UnityPy.load(str(REF/'EmbeddedKingBundles/kingdrill_v3'))
    objects={o.path_id:o for o in env.objects};trees={o.path_id:o.read_typetree() for o in env.objects}
    rootgo=4002648559900736485
    def tid(go):return next(p['component']['m_PathID'] for p in trees[go]['m_Component'] if objects[p['component']['m_PathID']].type.name=='Transform')
    root=tid(rootgo);nodes=[];paths={0:''};spriteids=set();skins=[];renderers=[]
    def descend(t,path):
        tr=trees[t];go=trees[tr['m_GameObject']['m_PathID']]
        paths[zlib.crc32(path.encode())]=path
        comps=[(p['component']['m_PathID'],objects[p['component']['m_PathID']].type.name) for p in go['m_Component']]
        nodes.append(dict(transformId=t,gameObjectId=tr['m_GameObject']['m_PathID'],path=path,name=go['m_Name'],active=go['m_IsActive'],transform=tr,components=comps))
        for cid,ctype in comps:
            if ctype=='SpriteRenderer':
                renderer=trees[cid];spriteids.add(renderer['m_Sprite']['m_PathID']);renderers.append(dict(path=path,pathId=cid,data=renderer))
            if ctype=='MonoBehaviour' and 'm_BoneTransforms' in trees[cid]:skins.append(dict(path=path,pathId=cid,data=trees[cid]))
        for c in tr['m_Children']:
            child=trees[c['m_PathID']];childname=trees[child['m_GameObject']['m_PathID']]['m_Name']
            descend(c['m_PathID'],path+'/'+childname if path else childname)
    descend(root,'')
    animid=next(p['component']['m_PathID'] for p in trees[rootgo]['m_Component'] if objects[p['component']['m_PathID']].type.name=='Animator')
    animator=trees[animid];controller=trees[animator['m_Controller']['m_PathID']];clips=[];unresolved=[]
    for ptr in controller['m_AnimationClips']:
        t=trees[ptr['m_PathID']];unknown=[]
        for binding in t['m_ClipBindingConstant']['genericBindings']:
            h=binding['path']
            if h not in paths:
                paths[h]='__UNRESOLVED_HASH_'+str(h);unknown.append(h)
        clip=decode_curves(t,paths);clip['sourcePathId']=ptr['m_PathID'];clip['unresolvedPathHashes']=unknown
        assert not clip['missingBindings'],clip['missingBindings']
        clips.append(clip)
        for binding in clip['curves']:
            if binding['path'].startswith('__UNRESOLVED'):
                unresolved.append(dict(clip=clip['name'],curve=binding))
            if binding['isPPtr']:
                spriteids.update(key.get('objectPathId',0) for key in binding['keys'])
    sprites=[];errors=[]
    for sid in sorted(spriteids-{0}):
        try:
            o=objects[sid];s=o.read();t=trees[sid];mesh=MeshHandler(s.m_RD,o.version);mesh.process()
            sprites.append(dict(sourcePathId=sid,name=s.m_Name,rect=t['m_Rect'],pixelsToUnits=t['m_PixelsToUnits'],pivot=t['m_Pivot'],
                                vertices=mesh.m_Vertices,uv=mesh.m_UV0,indices=mesh.m_IndexBuffer,
                                boneIndices=mesh.m_BoneIndices,boneWeights=mesh.m_BoneWeights,
                                bindPoses=t['m_RD'].get('m_Bindpose',[]),bones=t.get('m_Bones',[]),renderData=t['m_RD']))
        except Exception as ex:errors.append({'sprite':sid,'error':repr(ex)})
    skinchecks=[]
    nodesbyid={n['transformId']:n for n in nodes}
    spritesbyid={s['sourcePathId']:s for s in sprites}
    for skin in skins:
        renderer=next(r for r in renderers if r['path']==skin['path']);sp=spritesbyid[renderer['data']['m_Sprite']['m_PathID']]
        boneptrs=skin['data']['m_BoneTransforms'];missing=[p['m_PathID'] for p in boneptrs if p['m_PathID'] not in nodesbyid]
        valid=not missing and len(sp['bindPoses'])==len(boneptrs) and len(sp['boneWeights'])==len(sp['vertices'])
        skinchecks.append(dict(path=skin['path'],vertices=len(sp['vertices']),bones=len(boneptrs),bindPoses=len(sp['bindPoses']),missingBones=missing,valid=valid))
    atlas=[]
    for o in env.objects:
        if o.type.name=='SpriteAtlas':atlas.append(trees[o.path_id])
    result=dict(sourceBundle='EmbeddedKingBundles/kingdrill_v3',rootGameObjectId=rootgo,rootTransformId=root,nodes=nodes,renderers=renderers,skins=skins,sprites=sprites,animator=animator,controller=controller,clips=clips,atlas=atlas)
    (OUT/'native-king-drill-import-data.json').write_text(json.dumps(result,ensure_ascii=False,default=lambda value:list(value) if isinstance(value,(bytes,bytearray)) else value.tolist()),encoding='utf8')
    names=dict(controller['m_TOS']);sm=controller['m_Controller']['m_StateMachineArray'][0]['data']
    states=[]
    for i,st in enumerate(sm['m_StateConstantArray']):
        st=st['data'];states.append(dict(index=i,name=names.get(st['m_NameID']),speed=st['m_Speed'],loop=st['m_Loop'],transitions=st['m_TransitionConstantArray']))
    summary=dict(nodeCount=len(nodes),rendererCount=len(renderers),skinCount=len(skins),spriteCount=len(sprites),controllerStates=states,defaultState=sm['m_DefaultState'],
                 clips=[dict(name=c['name'],duration=c['duration'],loop=c['loop'],curveCount=len(c['curves'])) for c in clips],
                 unresolvedPathHashes=sorted(set(int(x['curve']['path'].split('_')[-1]) for x in unresolved)),unresolvedCurves=unresolved,
                 spriteErrors=errors,skinChecks=skinchecks,completeRigAndBindings=not errors and not unresolved and all(s['valid'] for s in skinchecks))
    (OUT/'native-king-drill-feasibility.json').write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf8')
    print(json.dumps({k:v for k,v in summary.items() if k!='unresolvedCurves'},indent=2,ensure_ascii=True))

if __name__=='__main__':main()
