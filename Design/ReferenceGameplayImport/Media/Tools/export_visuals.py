from extract_media import *

ROOTS={'Rocket':84786,'Propeller':80397,'TNT':80398,'ColorBall':80401,'TNTTNTCombo':80399,'ColorBallColorBallCombo':80400}
OUTFILE=OUT/'NativeVisuals.json'
def refs(v):
    if isinstance(v,dict):
        if 'm_PathID'in v:yield v
        else:
            for x in v.values():yield from refs(x)
    elif isinstance(v,list):
        for x in v:yield from refs(x)

PROPS=['m_IsActive','m_Enabled','m_SortingOrder','m_Size.x','m_Size.y']+['m_Color.'+x for x in 'rgba']
HASHES={zlib.crc32(s.encode()):s for s in PROPS}
def decode_curves(t,paths):
    m=t['m_MuscleClip']; c=m['m_Clip']['data']; stream=c['m_StreamedClip']; channels=[[] for _ in range(stream['curveCount'])]
    raw=struct.pack('<'+'I'*len(stream['data']),*stream['data']);p=0
    while p<len(raw):
        time,count=struct.unpack_from('<fI',raw,p);p+=8
        for _ in range(count):
            ix,a,b,slope,v=struct.unpack_from('<I4f',raw,p);p+=20
            channels[ix].append((time,a,b,slope,v))
    duration=m['m_StopTime']; fps=t['m_SampleRate']; allkeys=[]
    for records in channels:
        keys=[];prev=None
        for time,a,b,slope,v in records:
            if time < -1e10:prev=(time,a,b,slope,v);continue
            if not math.isfinite(time) or time>duration+.001:continue
            inslope=slope
            if prev and prev[0]>-1e10:
                dt=time-prev[0];inslope=3*prev[1]*dt*dt+2*prev[2]*dt+prev[3]
            keys.append(dict(time=max(0,time),value=v,inSlope=inslope,outSlope=slope))
            prev=(time,a,b,slope,v)
        if not keys and prev:keys=[dict(time=0,value=prev[4],inSlope=0,outSlope=0)]
        allkeys.append(keys)
    dense=c['m_DenseClip'];nc=dense['m_CurveCount']; nf=dense['m_FrameCount']; vals=dense['m_SampleArray']
    for j in range(nc):
        keys=[]
        for i in range(nf):
            value=vals[i*nc+j];pre=vals[max(0,i-1)*nc+j];post=vals[min(nf-1,i+1)*nc+j]
            keys.append(dict(time=dense['m_BeginTime']+i/dense['m_SampleRate'],value=value,inSlope=(post-pre)*dense['m_SampleRate']*.5,outSlope=(post-pre)*dense['m_SampleRate']*.5))
        allkeys.append(keys)
    for v in c['m_ConstantClip']['data']:allkeys.append([dict(time=0,value=v,inSlope=0,outSlope=0),dict(time=duration,value=v,inSlope=0,outSlope=0)])
    curves=[];missing=[];index=0;ptrmap=t['m_ClipBindingConstant']['pptrCurveMapping']
    for binding in t['m_ClipBindingConstant']['genericBindings']:
        ty=binding['typeID'];a=binding['attribute'];ct=binding['customType'];isptr=binding['isPPtrCurve']!=0
        n=1;props=[]
        if ty==4:
            if a in [1,3,4]:n=3;prefix={1:'m_LocalPosition',3:'m_LocalScale',4:'localEulerAnglesRaw'}[a];props=[prefix+'.'+k for k in 'xyz']
            elif a==2:n=4;props=['m_LocalRotation.'+k for k in 'xyzw']
        elif ty==212 and isptr:props=['m_Sprite']
        elif a in HASHES:props=[HASHES[a]]
        path=paths.get(binding['path'])
        if path is None or not props:missing.append(binding)
        else:
            for j,prop in enumerate(props):
                keys=allkeys[index+j]
                for key in keys:
                    if isptr:
                        v=int(round(key['value'])); key['objectPathId']=ptrmap[v]['m_PathID'] if 0<=v<len(ptrmap) else 0
                    for f in ['inSlope','outSlope']:
                        if not math.isfinite(key[f]):key[f]=0
                curves.append(dict(path=path,typeID=ty,property=prop,isPPtr=isptr,keys=keys))
        index+=n
    assert index==len(allkeys),(t['m_Name'],index,len(allkeys))
    return dict(name=t['m_Name'],duration=duration,frameRate=fps,loop=m['m_LoopTime'],curves=curves,missingBindings=missing)

def main():
    imports=[];nativeids=set();clips=[];controllers=[];clipseen=set();needs=set()
    for label,root in ROOTS.items():
        nodes=[];animpid=None;paths={};animatorPath=''
        for tid,go,path in traverse(root):
            tr=tree(tid);rel=path.split('/',1)[1] if '/'in path else ''
            comps=[]
            for c in go['m_Component']:
                cid=pid(c['component']);ob=objects[cid];typ=ob.type.name
                if typ=='Animator':animpid=cid;animatorPath=rel
                if typ=='MonoBehaviour':continue
                nativeids.add(cid)
                comps.append(dict(pathId=cid,type=typ,json=json.dumps(editor_tree(cid),separators=(',',':'))))
                if typ in ['SpriteRenderer','SpriteMask']:needs.add(pid(tree(cid)['m_Sprite']))
            nodes.append(dict(pathId=pid(tr['m_GameObject']),transformId=tid,parentTransformId=pid(tr['m_Father']) if tid!=root else 0,name=go['m_Name'],active=go['m_IsActive'],localPosition=tr['m_LocalPosition'],localRotation=tr['m_LocalRotation'],localScale=tr['m_LocalScale'],components=comps))
        for tid,go,path in traverse(root):
            rel=path.split('/',1)[1] if '/'in path else ''
            if rel==animatorPath:paths[0]=''
            elif rel.startswith(animatorPath+'/'):
                rel=rel[len(animatorPath)+1:];paths[zlib.crc32(rel.encode())]=rel
        controller=tree(pid(tree(animpid)['m_Controller']));cid=pid(tree(animpid)['m_Controller']);controllerclips=[]
        for ptr in controller['m_AnimationClips']:
            cp=pid(ptr);native=tree(cp);clip=decode_curves(native,paths);clip['pathId']=cp;controllerclips.append(cp)
            if cp not in clipseen:clips.append(clip);clipseen.add(cp)
            needs.update(pid(p) for p in native['m_ClipBindingConstant']['pptrCurveMapping'] if p['m_FileID']==0)
            print(label,clip['name'],clip['duration'],'curves',len(clip['curves']),'missing',clip['missingBindings'])
        states=[];tos=dict(controller['m_TOS']);sm=controller['m_Controller']['m_StateMachineArray'][0]['data']
        for st in sm['m_StateConstantArray']:
            st=st['data'];node=st['m_BlendTreeConstantArray'][0]['data']['m_NodeArray'][0]['data'];states.append(dict(name=tos.get(st['m_NameID'],str(st['m_NameID'])),clipPathId=controllerclips[node['m_ClipID']],speed=st['m_Speed'],transitions=st['m_TransitionConstantArray']))
        controllers.append(dict(name=controller['m_Name'],pathId=cid,defaultState=sm['m_DefaultState'],states=states))
        imports.append(dict(name=label,nodes=nodes,controllerPathId=cid,animatorPath=animatorPath))
    manifest=json.loads((ROOT/'Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json').read_text());available={x['sourcePathId'] for x in manifest['sprites']}
    missing=sorted(needs-available-{0});print('MISSING SPRITES',missing)
    (OUT/'NativeSpriteDependencies.json').write_text(json.dumps(sorted(needs)))
    OUTFILE.write_text(json.dumps(dict(source='resources.assets',prefabs=imports,clips=clips,controllers=controllers,missingSprites=missing),separators=(',',':')),encoding='utf8')
    # Keep full native dictionaries as editable provenance, outside imported runtime assets.
    original=OUT/'OriginalAnimationData';original.mkdir(exist_ok=True)
    for c in clips:(original/(str(c['pathId'])+'-'+c['name']+'.json')).write_text(json.dumps(tree(c['pathId'])))
    for c in controllers:(original/(str(c['pathId'])+'-'+c['name']+'.json')).write_text(json.dumps(tree(c['pathId'])))

if __name__=='__main__':main()
