from pathlib import Path
import json,math,sys
ROOT=Path(__file__).resolve().parents[4]; OUT=ROOT/'Design/ReferenceGameplayImport/Media'
source=json.loads((OUT/'NativeParticles.json').read_text())
dump=Path(sys.argv[1]) if len(sys.argv)>1 else OUT/'ImportedNativeModules.json'
actual=json.loads(dump.read_text())
lookup={c['pathId']:(p['name'],n['name'],c['type'],json.loads(c['json'])) for p in source['prefabs'] for n in p['nodes'] for c in n['components']}
issues=[]; comparisons=0
def compare(a,b,path,identity):
    global comparisons
    if isinstance(a,dict):
        if 'm_PathID'in a:return # GUID/local file IDs are verified by the editor material/sprite-reference checks.
        if not isinstance(b,dict):issues.append(dict(component=identity,path=path,expected=a,actual=b));return
        keys=a.keys()
        if a.get('enabled') is False:keys=['enabled']
        elif 'minMaxState'in a and 'maxCurve'in a:
            mode=a['minMaxState'];keys=['minMaxState','scalar']
            if mode in [2,3]:keys+=['minScalar']
            if mode in [1,2]:keys+=['maxCurve']
            if mode==2:keys+=['minCurve']
        elif 'minMaxState'in a and 'maxGradient'in a:
            mode=a['minMaxState'];keys=['minMaxState']+{0:['maxColor'],1:['maxGradient'],2:['minColor','maxColor'],3:['minGradient','maxGradient'],4:['maxGradient']}.get(mode,[])
        for k in keys:
            v=a[k]
            if k in ['m_GameObject','stopAction','serializedVersion','m_SortingLayerID']:continue
            if k not in b:issues.append(dict(component=identity,path=path+'/'+k,expected='native serialized field',actual='missing'));continue
            compare(v,b[k],path+'/'+k,identity)
    elif isinstance(a,list):
        if not isinstance(b,list) or len(a)!=len(b):issues.append(dict(component=identity,path=path,expectedCount=len(a),actualCount=len(b) if isinstance(b,list) else None));return
        for i,(x,y) in enumerate(zip(a,b)):compare(x,y,path+'/'+str(i),identity)
    else:
        comparisons+=1
        equal=a==b
        if isinstance(a,(int,float)) and isinstance(b,(int,float)):equal=math.isclose(a,b,rel_tol=1e-5,abs_tol=1e-6)
        if not equal:issues.append(dict(component=identity,path=path,expected=a,actual=b))
for entry in actual['components']:
    p,n,typ,native=lookup[entry['sourcePathId']]; restored=json.loads(entry['json']);restored=restored.get(typ,restored)
    if typ in ['ParticleSystem','LineRenderer']:compare(native,restored,'',str(entry['sourcePathId'])+' '+p+'/'+entry['path'])
def known_difference(issue):
    component=issue['component'];path=issue['path']
    if 'LightballRayPrefab/' in component:
        if path in ['/m_LightmapIndex','/m_LightmapIndexDynamic','/m_LightmapTilingOffset','/m_LightmapTilingOffsetDynamic'] and issue['actual']=='missing':
            return 'Obsolete Unity 2021 renderer lightmap field; native unlit beam shader does not use lightmaps.'
        if path=='/m_SortingLayer' and issue['actual']==0:
            return 'Native sorting-layer index remapped to current project; relative sorting order retained.'
    if component.startswith('94786 ') and path in ['/EmissionModule/enabled','/UVModule/enabled'] and issue['actual'] is False:
        return 'Packaged Potion single-hit template has a null sprite. Only this incomplete emitter is disabled; valid child effects remain.'
    return None
for issue in issues:
    reason=known_difference(issue)
    if reason:issue['compatibilityReason']=reason
unexpected=[i for i in issues if 'compatibilityReason' not in i]
result=dict(source='NativeParticles.json',restored=str(dump),components=len(actual['components']),particleSystems=sum(x['type']=='ParticleSystem' for x in actual['components']),scalarChecks=comparisons,differences=issues,unexpectedDifferences=unexpected,activeModuleValuesMatch=not unexpected)
(OUT/'NativeModuleValidation.json').write_text(json.dumps(result,indent=2))
print('Components',result['components'],'ParticleSystems',result['particleSystems'],'scalarChecks',comparisons,'differences',len(issues),'unexpected',len(unexpected))
print(json.dumps(issues[:25],indent=2))
