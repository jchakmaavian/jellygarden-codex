exec(__import__('pathlib').Path(r'G:/Game/JellyGarden-Codex/Design/DualColorBall-Reference/inspect_combo.py').read_text().split('roots=[]')[0])

import math
out=ROOT/'Design/DualCombo-Reference'
def refs(v):
 if isinstance(v,dict):
  if 'm_PathID' in v:yield v
  else:
   for x in v.values():yield from refs(x)
 elif isinstance(v,list):
  for x in v:yield from refs(x)
effects=[]; dependencies=set(); source_ids=set()
for gid in (37221,42889,51482,51485,51486,51487,51488,53567):
 nodes=[]
 for tid,g,path in walk(tr_id(tree(gid))):
  t=tree(tid);components=[]
  for c in g['m_Component']:
   p=pid(c['component']);typ=objs[p].type.name
   if typ=='MonoBehaviour':continue
   v=old.editor_value(tree(p),objs[p]._get_typetree_node())
   dependencies.update((r['m_FileID'],pid(r)) for r in refs(v) if pid(r))
   components.append(dict(pathId=p,type=typ,json=json.dumps(v,separators=(',',':'))));source_ids.add(p)
  nodes.append(dict(pathId=pid(t['m_GameObject']),transformId=tid,parentTransformId=pid(t['m_Father']) if len(nodes) else 0,name=g['m_Name'],active=g['m_IsActive'],localPosition=t['m_LocalPosition'],localRotation=t['m_LocalRotation'],localScale=t['m_LocalScale'],components=components))
  source_ids.update((tid,pid(t['m_GameObject'])))
 effects.append(dict(name=tree(gid)['m_Name'],nodes=nodes))
(out/'NativeSupportingParticles-source.json').write_text(json.dumps(dict(prefabs=effects,materials=[],textures=[],sprites=[]),separators=(',',':')))
old_names={o.peek_name():o for o in old.objects.values() if o.type.name=='Sprite'}
art=[]
for f,p in sorted(dependencies):
 if f or p in source_ids:continue
 o=objs[p];n=o.peek_name()
 print('DEPENDENCY',p,o.type.name,n)
 if o.type.name!='Sprite':continue
 image=o.read().image.convert('RGBA');prev=old_names.get(n);other=prev.read().image.convert('RGBA') if prev else None
 art.append(dict(sourcePathId=p,name=n,size=image.size,identicalOlderPixels=other is not None and image.size==other.size and image.tobytes()==other.tobytes()))
(out/'supporting-art-comparison.json').write_text(json.dumps(art,indent=2))
# Decode both dedicated dual-body clips from the exact package.
clips=[]
for rootid,clipid in ((54127,17030),(54128,17017)):
 seq=list(walk(tr_id(tree(rootid))));animnode=next(p for _,g,p in seq if any(objs[pid(c['component'])].type.name=='Animator' for c in g['m_Component']))
 paths={0:''}
 for _,g,p in seq:
  if p.startswith(animnode+'/'):
   rel=p[len(animnode)+1:];paths[zlib.crc32(rel.encode())]=rel
 decoded=old.decode_curves(tree(clipid),paths);decoded['pathId']=clipid;clips.append(decoded)
(out/'decoded-combo-clips.json').write_text(json.dumps(clips,indent=2))
print('ART',len(art),'different',[x['name'] for x in art if not x['identicalOlderPixels']])
