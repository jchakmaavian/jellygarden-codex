exec((Path if False else __import__('pathlib').Path)(r'G:/Game/JellyGarden-Codex/Design/DualColorBall-Reference/inspect_source.py').read_text().split('found=[]')[0])
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools'))
import export_visuals as old
import zlib
cache={}
def tree(p):
    if p not in cache:cache[p]=objs[p].read_typetree()
    return cache[p]
def pid(r):return r['m_PathID']
def tr_id(g):return next(pid(c['component']) for c in g['m_Component'] if objs[pid(c['component'])].type.name=='Transform')
def walk(tid,path=''):
    t=tree(tid);g=tree(pid(t['m_GameObject']));path=path+'/'+g['m_Name'] if path else g['m_Name']
    yield tid,g,path
    for r in t['m_Children']:yield from walk(pid(r),path)
roots=[]
for o in objs.values():
    if o.type.name!='GameObject':continue
    n=o.peek_name() or ''
    if 'ballball' in n.lower() or ('combo' in n.lower() and 'ball' in n.lower()):roots.append((o.path_id,n))
print('ROOTS',roots)
for gid in (48774,51492):
    seq=list(walk(tr_id(tree(gid))));print('VFX',gid,[(p,[(objs[pid(c['component'])].type.name,pid(c['component'])) for c in g['m_Component']]) for _,g,p in seq])
    (ROOT/'Design/DualColorBall-Reference'/f'hierarchy-{gid}.json').write_text(json.dumps([dict(path=p,components={objs[pid(c['component'])].type.name:tree(pid(c['component'])) for c in g['m_Component'] if objs[pid(c['component'])].type.name!='MonoBehaviour'}) for _,g,p in seq],indent=2))
# Find the view that owns the native dual-ball controller.
for o in objs.values():
    if o.type.name=='Animator' and pid(tree(o.path_id)['m_Controller'])==18166:
        tid=tr_id(tree(pid(tree(o.path_id)['m_GameObject'])));t=tree(tid)
        while pid(t['m_Father']):tid=pid(t['m_Father']);t=tree(tid)
        print('DUAL_ROOT',pid(t['m_GameObject']),tree(pid(t['m_GameObject']))['m_Name'])
        seq=list(walk(tid)); paths={0:''};apath=next(p for _,g,p in seq if any(pid(c['component'])==o.path_id for c in g['m_Component']))
        for _,g,p in seq:
            if p.startswith(apath+'/'):rel=p[len(apath)+1:];paths[zlib.crc32(rel.encode())]=rel
        clip=old.decode_curves(tree(17017),paths);clip['pathId']=17017
        (ROOT/'Design/DualColorBall-Reference/decoded-clip.json').write_text(json.dumps(clip,indent=2))
        def norm(c,objects):
            c=json.loads(json.dumps(c));c.pop('pathId',None)
            for curve in c['curves']:
                for k in curve['keys']:
                    if 'objectPathId' in k:p=k.pop('objectPathId');k['objectName']=objects[p].peek_name() if p else None
            return c
        prev=next(c for c in json.loads((ROOT/'Design/ReferenceGameplayImport/Media/NativeVisuals.json').read_text())['clips'] if c['name']=='BallBallExplodeAnimation')
        same=norm(clip,objs)==norm(prev,old.objects)
        print('CLIP',clip['duration'],len(clip['curves']),'IDENTICAL',same)
        print('POS',[(c['path'],c['property'],[(round(k['time'],4),round(k['value'],4)) for k in c['keys']]) for c in clip['curves'] if c['path'] in ('Ball_1','Ball_2') and c['property'] in ('m_LocalPosition.x','m_LocalPosition.y','m_LocalScale.x','m_Enabled')])
        ctrl=tree(18166);sm=ctrl['m_Controller']['m_StateMachineArray'][0]['data'];print('STATE',sm['m_StateConstantArray'])
        (ROOT/'Design/DualColorBall-Reference/clip-verification.json').write_text(json.dumps(dict(sourcePathId=17017,existingPathId=15090,normalizedCurvesIdentical=same,duration=clip['duration'],curveCount=len(clip['curves']),missingBindings=clip['missingBindings']),indent=2))

print('FULL VIEW',[(p,[(objs[pid(c['component'])].type.name,pid(c['component'])) for c in g['m_Component']]) for _,g,p in walk(tr_id(tree(54128)))])
for gid in (48774,51492):
    for _,g,path in walk(tr_id(tree(gid))):
        for c in g['m_Component']:
            cp=pid(c['component'])
            if objs[cp].type.name=='ParticleSystemRenderer': print('MAT',path,tree(cp)['m_Materials'])

clip=json.loads((ROOT/'Design/DualColorBall-Reference/decoded-clip.json').read_text())
previous=next(c for c in json.loads((ROOT/'Design/ReferenceGameplayImport/Media/NativeVisuals.json').read_text())['clips'] if c['name']=='BallBallExplodeAnimation')
old_names={o.peek_name():o for o in old.objects.values() if o.type.name=='Sprite'}
source_sprites=sorted({k['objectPathId'] for c in clip['curves'] for k in c['keys'] if k.get('objectPathId')})
checks=[]
for p in source_sprites:
    o=objs[p];n=o.peek_name();image=o.read().image.convert('RGBA');older=old_names.get(n);oi=older.read().image.convert('RGBA') if older else None
    checks.append(dict(sourcePathId=p,name=n,identicalImportedPixels=oi is not None and image.size==oi.size and image.tobytes()==oi.tobytes()))
(ROOT/'Design/DualColorBall-Reference/body-art-verification.json').write_text(json.dumps(checks,indent=2))
print('BODY ART',len(checks),'identical',sum(c['identicalImportedPixels'] for c in checks))
