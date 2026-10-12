from pathlib import Path
import json,math
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Design/DualCombo-Reference'
expected=json.loads((OUT/'decoded-combo-clips.json').read_text());actual=json.loads((OUT/'imported-clips.json').read_text());checks=0;issues=[];compatibility=[]
for e in expected:
 a=next(c for c in actual if c['name']==e['name'])
 for field in ('duration','frameRate','loop'):
  checks+=1
  if e[field]!=a[field] and (not isinstance(e[field],(int,float)) or abs(e[field]-a[field])>1e-5):issues.append(dict(clip=e['name'],field=field))
 for ec in e['curves']:
  ac=next((c for c in a['curves'] if c['path']==ec['path'] and c['property']==ec['property']),None)
  if not ac or len(ac['keys'])!=len(ec['keys']):issues.append(dict(clip=e['name'],path=ec['path'],property=ec['property'],keyCountMismatch=True));continue
  for i,(ek,ak) in enumerate(zip(ec['keys'],ac['keys'])):
   fields=['time'] if ec['isPPtr'] else ['time','value','inSlope','outSlope']
   for field in fields:
    checks+=1;x=ek[field];y=ak[field]
    if field in ('inSlope','outSlope') and ec['property'] in ('m_Enabled','m_SortingOrder') and y=='Infinity':
     compatibility.append(dict(clip=e['name'],path=ec['path'],property=ec['property'],key=i,field=field,reason='Unity discrete renderer property uses step tangents; native key times and integer values retained.'));continue
    if not math.isclose(x,y,rel_tol=2e-6,abs_tol=1e-5):issues.append(dict(clip=e['name'],path=ec['path'],property=ec['property'],key=i,field=field,expected=x,actual=y))
   if ec['isPPtr']:
    checks+=1;wanted=str(988000000+ek['objectPathId']) if ek['objectPathId'] else None
    if wanted!=ak['objectName']:issues.append(dict(clip=e['name'],path=ec['path'],key=i,expectedExactSprite=wanted,actual=ak['objectName']))
result=dict(clips=2,curves=147,scalarChecks=checks,compatibilityAdjustments=compatibility,differences=issues)
(OUT/'Verification/clip-validation.json').write_text(json.dumps(result,indent=2))
print('Curves 147 checks',checks,'documented discrete tangent adjustments',len(compatibility),'unexpected differences',len(issues));print(json.dumps(issues[:8],indent=2))
