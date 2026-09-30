from inspect_king_sources import *
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
import UnityPy
from export_visuals import decode_curves
import zlib

def main():
    index=[];clips=[];rigs=[]
    base=OUT/'EmbeddedKingBundles'
    for bundle in ['kingdrill_v3','kingdrillcloud','kingnightmarecommon_v3','kingnightmarecommonextra_v4']:
        env=UnityPy.load(str(base/bundle));objects={o.path_id:o for o in env.objects};dest=base/'Recovered'/bundle;dest.mkdir(exist_ok=True,parents=True);trees={};allnodes=[]
        for o in env.objects:
            try:trees[o.path_id]=o.read_typetree()
            except Exception as ex:print('TREE ERR',o.path_id,o.type.name,str(ex))
        for o in env.objects:
            t=trees.get(o.path_id,{});name=t.get('m_Name',str(o.path_id));rec=dict(bundle=bundle,path_id=o.path_id,type=o.type.name,name=name)
            safe=re.sub(r'[^\w. -]+','_',name)
            if o.type.name in ['Texture2D','Sprite']:
                folder=dest/('Textures' if o.type.name=='Texture2D' else 'Sprites');folder.mkdir(exist_ok=True)
                try:
                    im=o.read().image;path=folder/(safe+'__'+str(o.path_id)+'.png');im.save(path);rec.update(image=str(path.relative_to(OUT)),dimensions=im.size)
                except Exception as ex:rec['decode_error']=repr(ex)
            if o.type.name in ['AnimationClip','AnimatorController','MonoBehaviour','Material']:
                folder=dest/'Serialized';folder.mkdir(exist_ok=True);path=folder/(safe+'__'+str(o.path_id)+'.json');path.write_text(json.dumps(t,ensure_ascii=False,indent=2),encoding='utf8');rec['serialized']=str(path.relative_to(OUT))
            if o.type.name=='GameObject':
                comps=[(c['component']['m_PathID'],objects[c['component']['m_PathID']].type.name) for c in t['m_Component']]
                ts=next((trees[i] for i,typ in comps if typ in ['Transform','RectTransform']),None)
                allnodes.append(dict(id=o.path_id,name=name,components=comps,transform=ts))
            if o.type.name=='AnimationClip':clips.append(dict(bundle=bundle,path_id=o.path_id,name=name,duration=t.get('m_MuscleClip',{}).get('m_StopTime'),loop=t.get('m_MuscleClip',{}).get('m_LoopTime'),serialized=rec.get('serialized')))
            index.append(rec)
        (dest/'gameobject-hierarchy.json').write_text(json.dumps(allnodes,ensure_ascii=False,indent=2),encoding='utf8')
        # Resolve every relative transform path from every node to match optimized clip hashes.
        paths={0:''}
        def descend(tid,path=''):
            tr=trees[tid];paths[zlib.crc32(path.encode())]=path
            for c in tr['m_Children']:
                ct=trees[c['m_PathID']];go=trees[ct['m_GameObject']['m_PathID']];descend(c['m_PathID'],path+'/'+go['m_Name'] if path else go['m_Name'])
        for node in allnodes:
            for pid,typ in node['components']:
                if typ in ['Transform','RectTransform']:descend(pid)
        decoded=[]
        for o in env.objects:
            if o.type.name!='AnimationClip':continue
            try:
                clip=decode_curves(trees[o.path_id],paths);clip['path_id']=o.path_id;decoded.append(clip)
                print('CLIP',bundle,clip['name'],clip['duration'],'missing',clip['missingBindings'])
            except Exception as ex:print('CLIP ERR',bundle,o.path_id,repr(ex))
        (dest/'decoded-animation-clips.json').write_text(json.dumps(decoded,ensure_ascii=False,indent=2),encoding='utf8')
        print(bundle,'textures/sprites',len([r for r in index if r['bundle']==bundle and r['type'] in ['Texture2D','Sprite']]),'objects',len(objects))
    save('embedded-king-visual-inventory.json',index);save('embedded-king-unity-clips.json',clips)

if __name__=='__main__':main()
