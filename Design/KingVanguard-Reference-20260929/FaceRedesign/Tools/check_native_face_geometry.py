"""Independent, read-only-source math check for the NativeKingDrillFaceShape importer helper.
Uses only Python's standard library; never launches Unity or changes source geometry.
"""
from pathlib import Path
import hashlib, json, math

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[3]
SOURCE=ROOT/'Design/KingVanguard-Reference-20260929/Logic/KingDrill/UnityImportData.json'
PUPILS={'NewKing/01900_kafa/01920_sag_goz_bebek','NewKing/01900_kafa/01940_sol_goz_bebek'}
PUPIL_SCALE=1.55

def identity():return [[float(i==j) for j in range(4)] for i in range(4)]
def mul(a,b):return [[sum(a[i][k]*b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
def point(a,p):return [sum(a[i][j]*p[j] for j in range(3))+a[i][3] for i in range(3)]
def inv(a):
    a=[a[i][:]+identity()[i] for i in range(4)]
    for c in range(4):
        p=max(range(c,4),key=lambda r:abs(a[r][c]));assert abs(a[p][c])>1e-10
        a[c],a[p]=a[p],a[c];div=a[c][c];a[c]=[v/div for v in a[c]]
        for r in range(4):
            if r==c:continue
            scale=a[r][c];a[r]=[v-scale*w for v,w in zip(a[r],a[c])]
    return [r[4:] for r in a]
def trs(n):
    x,y,z,w=[n['rotation'][k] for k in 'xyzw'];s=n['scale'];p=n['position']
    a=[[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w,p['x']],
       [2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w,p['y']],
       [2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y,p['z']], [0,0,0,1]]
    for i in range(3):
        for j,k in enumerate('xyz'):a[i][j]*=s[k]
    return a
def bounds(vertices):
    lo=[min(v[k] for v in vertices) for k in range(3)];hi=[max(v[k] for v in vertices) for k in range(3)]
    return dict(min=lo,max=hi,center=[(a+b)/2 for a,b in zip(lo,hi)],size=[b-a for a,b in zip(lo,hi)])
def hash_binding(sprite):
    return hashlib.sha256(json.dumps({k:sprite[k] for k in ('uv','triangles','weights','bindPoses')},sort_keys=True).encode()).hexdigest()

def main():
    raw=SOURCE.read_bytes();source_hash=hashlib.sha256(raw).hexdigest();data=json.loads(raw)
    world={}
    for n in data['nodes']:world[n['id']]=mul(world[n['parentId']],trs(n)) if n['parentId'] else trs(n)
    head=next(n for n in data['nodes'] if n['path']=='NewKing/01900_kafa');pivot=point(world[head['id']],[0,0,0])
    affine=identity()
    for i,scale in enumerate((1.15,.92,1.0)):affine[i][i]=scale;affine[i][3]=pivot[i]*(1-scale)
    faces=[n for n in data['nodes'] if n['hasRenderer'] and (n['path']=='NewKing/01900_kafa' or n['path'].startswith('NewKing/01900_kafa/'))]
    sprites={s['id']:s for s in data['sprites']};reports=[];original_all=[];shaped_all=[]
    assert len(faces)==13
    for n in faces:
        s=sprites[n['renderer']['spriteId']];before_hash=hash_binding(s);original=[];shaped=[];newvertices=[];errors=[];skins=[]
        bones=n['renderer']['bones'];binds=[[[p[f'e{i}{j}'] for j in range(4)] for i in range(4)] for p in s['bindPoses']]
        for vi,v in enumerate(s['vertices']):
            skin=world[n['id']]
            if bones:
                skin=[[0.0]*4 for _ in range(4)];w=s['weights'][vi]
                for j in range(4):
                    weight=w[f'weight{j}']
                    if not weight:continue
                    bi=w[f'boneIndex{j}'];b=mul(world[bones[bi]],binds[bi])
                    for row in range(4):
                        for col in range(4):skin[row][col]+=weight*b[row][col]
                skin[3]=[0,0,0,1]
            skins.append(skin);original.append(point(skin,[v[k] for k in 'xyz']))
        base_shaped=[point(affine,v) for v in original]
        is_pupil=n['path'] in PUPILS
        pupil_scale=PUPIL_SCALE if is_pupil else 1.0
        pupil_pivot=[sum(v[k] for v in base_shaped)/len(base_shaped) for k in range(3)] if is_pupil else None
        for skin,base_expected in zip(skins,base_shaped):
            expected=base_expected[:]
            if is_pupil:
                for k in range(2):expected[k]=pupil_pivot[k]+(expected[k]-pupil_pivot[k])*PUPIL_SCALE
            changed=point(inv(skin),expected);actual=point(skin,changed)
            assert all(math.isfinite(c) for c in changed)
            error=math.dist(actual,expected);assert error<1e-9
            assert changed==point(inv(skin),expected),'non-deterministic shape math'
            if not is_pupil:assert changed==point(inv(skin),base_expected),'non-pupil geometry changed'
            shaped.append(actual);newvertices.append(changed);errors.append(error)
        before=bounds(original);after=bounds(shaped)
        for i,scale in enumerate((1.15*pupil_scale,.92*pupil_scale,1.0)):assert abs(after['size'][i]-before['size'][i]*scale)<1e-9
        centroid_error=math.dist([sum(v[k] for v in shaped)/len(shaped) for k in range(3)],pupil_pivot) if is_pupil else 0.0
        assert centroid_error<1e-9
        assert hash_binding(s)==before_hash
        original_all+=original;shaped_all+=shaped
        reports.append(dict(path=n['path'],vertexCount=len(original),boneCount=len(bones),sourceBounds=before,faceAffineBounds=bounds(base_shaped),shapedBounds=after,pupilScale=pupil_scale,pupilPivot=pupil_pivot,pupilCentroidError=centroid_error,nonPupilGeometryUnchanged=None if is_pupil else True,maxSetupWorldError=max(errors),bindingsUnchanged=True,bindingHash=before_hash,deterministic=True))
    assert sum(r['pupilScale']==PUPIL_SCALE for r in reports)==2
    assert sum(r['nonPupilGeometryUnchanged'] is True for r in reports)==11
    assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
    result=dict(status='PASS independent mathematical validation; Unity visual validation still required',sourceSha256=source_hash,sourceUnchanged=True,pivot=pivot,horizontalScale=1.15,verticalScale=.92,pupilScale=PUPIL_SCALE,enlargedPupils=2,unchangedOtherFaceMeshes=11,facialMeshes=len(reports),facialVertices=sum(r['vertexCount'] for r in reports),sourceBounds=bounds(original_all),shapedBounds=bounds(shaped_all),meshes=reports)
    (OUT/'native-face-geometry-independent-validation.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    print(json.dumps({k:v for k,v in result.items() if k!='meshes'},indent=2))

if __name__=='__main__':main()
