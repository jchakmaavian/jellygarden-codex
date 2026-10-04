"""Register the approved PNG to the existing facial topology. No raster edits."""
from pathlib import Path
import json
import numpy as np

root = Path(__file__).resolve().parents[1]
data = json.loads((root.parent/'KingVanguard-Lively-20261004/Review/face-landmarks.json').read_text())
neutral = np.array(data['poses']['Neutral'], dtype=float)
controls = []
def pair(a,b): controls.append((a,b))
# Image boundaries and stable torso establish an invertible whole-portrait warp.
for y in [0,1254]:
    for x in [0,300,627,950,1254]: pair((x,y),(x,y))
for y in [300,650,950]:
    for x in [0,1254]: pair((x,y),(x,y))
for a,b in [((543,59),(607,98)),((310,205),(319,246)),((237,330),(251,320)),
            ((828,137),(979,222)),((899,269),(1027,299)),((570,188),(615,326)),
            ((543,123),(603,245)),((517,190),(552,339)),((575,244),(618,407)),((617,175),(670,333)),
            ((313,473),(324,510)),((864,391),(958,489)),((589,276),(636,444)),
            ((338,553),(338,645)),((874,514),(950,620)),
            ((290,680),(338,810)),((905,661),(958,814)),
            ((393,939),(382,967)),((886,937),(935,973)),
            ((640,1157),(653,1184)),((116,932),(105,924)),((1140,926),(1140,911)),
            ((627,930),(648,1104))]: pair(a,b)
# Register round crown finials and brooch contours, not just their centers.
for a,b,r1,r2 in [((543,59),(607,98),53,75),((310,205),(319,246),43,55),
                  ((237,330),(251,320),31,46),((828,137),(979,222),42,55),
                  ((899,269),(1027,299),32,44),((393,939),(382,967),70,71),
                  ((886,937),(935,973),68,71)]:
    for angle in np.linspace(0,2*np.pi,17)[:-1]:
        direction=np.array([np.cos(angle),np.sin(angle)])
        pair(np.array(a)+direction*r1,np.array(b)+direction*r2)
groups = {
 'browL':((500,361),(519,549),1.0,.88,18),
 'browR':((699,316),(750,526),1.03,.86,-12),
 'eyeL':((511,443),(545,650),1.08,1.02,12),
 'eyeR':((700,415),(751,632),1.08,1.00,10),
 'nose':((620,488),(647,735),1.04,.96,10),
 'moustacheL':((516,561),(526,789),.98,.85,10),
 'moustacheR':((737,529),(755,778),.98,.85,10),
 'mouth':((627,589),(650,837),.95,.75,10),
 'chin':((627,707),(644,963),1.0,.95,10),
}
for prefix,(a,b,sx,sy,angle) in groups.items():
    # Register feature centroids and axes, allowing TPS to keep fine contours smooth.
    radians=np.radians(angle); rotation=np.array([[np.cos(radians),-np.sin(radians)],[np.sin(radians),np.cos(radians)]])
    a=np.array(a);b=np.array(b)
    for offset in [(-35,0),(35,0),(0,-12),(0,12)]:
        offset=np.array(offset); pair(a+offset,b+rotation@(offset*np.array([sx,sy])))

src=np.array([a for a,b in controls])/1254
dst=np.array([b for a,b in controls])/1254
def kernel(a,b):
    r2=((a[:,None,:]-b[None,:,:])**2).sum(axis=2)
    return r2*np.log(np.maximum(r2,1e-20))
p=np.column_stack((np.ones(len(src)),src))
matrix=np.block([[kernel(src,src)+np.eye(len(src))*.000001,p],[p.T,np.zeros((3,3))]])
weights=np.linalg.solve(matrix,np.vstack((dst,np.zeros((3,2)))))
# Subdivide existing triangles at registration controls. Barycentric evaluation
# preserves all original expression geometry while giving rigid ornaments support.
original=json.loads(json.dumps(data)); excluded=set()
for attempt in range(len(controls)+1):
    data=json.loads(json.dumps(original))
    points=list(map(tuple,data['poses']['Neutral']));triangles=list(map(tuple,data['triangles']))
    poses={name:list(map(tuple,pose)) for name,pose in data['poses'].items()}
    support={}
    for ci,(a,b) in enumerate(controls):
        if ci in excluded:continue
        a=np.array(a,dtype=float)
        if min(np.linalg.norm(a-np.array(q)) for q in points)<.1: continue
        matches=[]
        for t in triangles:
            xyz=np.array([points[i] for i in t]); m=np.vstack((xyz.T,np.ones(3)))
            w=np.linalg.solve(m,np.append(a,1))
            if w.min()>=-1e-7:matches.append((t,w))
        if not matches:continue
        t,w=matches[0];index=len(points);points.append(tuple(a));data['labels'].append('bowSupport'+str(index));support[index]=ci
        for name,pose in poses.items():pose.append(tuple(w@np.array([pose[i] for i in t])))
        for t,w in matches:
            triangles.remove(t)
            for j in range(3):
                u,v=t[j],t[(j+1)%3]
                aa,bb=np.array(points[u]),np.array(points[v])
                cross=(bb[0]-aa[0])*(a[1]-aa[1])-(bb[1]-aa[1])*(a[0]-aa[0])
                if cross>1e-5:triangles.append((u,v,index))
    # Delaunay retriangulation avoids near-collinear slivers from repeated splits.
    count=len(points);cloud=points+[(-10000,-10000),(20000,-10000),(0,20000)]
    triangles=[(count,count+1,count+2)]
    def inside_circle(tri,point):
        aa,bb,cc=[cloud[i] for i in tri]
        ax,ay=aa[0]-point[0],aa[1]-point[1];bx,by=bb[0]-point[0],bb[1]-point[1];cx,cy=cc[0]-point[0],cc[1]-point[1]
        d=(ax*ax+ay*ay)*(bx*cy-cx*by)-(bx*bx+by*by)*(ax*cy-cx*ay)+(cx*cx+cy*cy)*(ax*by-bx*ay)
        orient=(bb[0]-aa[0])*(cc[1]-aa[1])-(bb[1]-aa[1])*(cc[0]-aa[0])
        return d*orient>1e-7
    for index,point in enumerate(points):
        bad=[t for t in triangles if inside_circle(t,point)];edges={}
        for t in bad:
            for u,v in [(t[0],t[1]),(t[1],t[2]),(t[2],t[0])]:
                key=tuple(sorted((u,v)));edges[key]=edges.get(key,0)+1
        triangles=[t for t in triangles if t not in bad]+[(u,v,index) for (u,v),c in edges.items() if c==1]
    triangles=[t for t in triangles if max(t)<count]
    for index,t in enumerate(triangles):
        aa,bb,cc=[points[i] for i in t]
        if (bb[0]-aa[0])*(cc[1]-aa[1])-(bb[1]-aa[1])*(cc[0]-aa[0])<0:triangles[index]=(t[0],t[2],t[1])
    neutral=np.array(points);data['poses']={n:np.round(p,5).tolist() for n,p in poses.items()};data['triangles']=triangles
    q=neutral/1254
    bow=(kernel(q,src)@weights[:-3]+np.column_stack((np.ones(len(q)),q))@weights[-3:])*1254
    for i,(x,y) in enumerate(neutral):
        if x in [0,1254]: bow[i,0]=x
        if y in [0,1254]: bow[i,1]=y
    bow=np.round(bow,3);tri=np.array(triangles)
    def area(points):
        a,b,c=points[tri[:,0]],points[tri[:,1]],points[tri[:,2]]
        return (b[:,0]-a[:,0])*(c[:,1]-a[:,1])-(b[:,1]-a[:,1])*(c[:,0]-a[:,0])
    checks=0; worst=1e9;bad=None
    for name,pose in data['poses'].items():
        for w in np.linspace(0,1,101):
            areas=area(np.array(pose)*(1-w)+bow*w);worst=min(worst,float(areas.min()));checks+=len(areas)
            if areas.min()<=0:bad=int(np.argmin(areas));break
        if bad is not None:break
    if bad is None:break
    candidates=[i for i in tri[bad] if i in support]
    if not candidates:raise ValueError(f'Original topology folded: {name} {w} {tri[bad]}')
    excluded.add(support[max(candidates)])
else:raise ValueError('No stable correspondence subdivision')
print('Retained',len(points),'vertices; omitted',len(excluded),'redundant controls from topology')
text='using UnityEngine;\nnamespace RoyalThrone.Characters {\n public static class KingVanguardBowMesh {\n  public static readonly Vector2[] Points = {\n'
text+=',\n'.join(f'   new Vector2({x}f,{y}f)' for x,y in bow)+'\n  };\n }\n}\n'
(root/'Staged/KingVanguardBowMesh.cs').write_text(text)
text='using UnityEngine;\nnamespace RoyalThrone.Characters {\n public static class KingVanguardFaceMesh {\n'
for name,pose in data['poses'].items():
    text+='  public static readonly Vector2[] '+name+' = {\n'+',\n'.join(f'   new Vector2({x}f,{y}f)' for x,y in pose)+'\n  };\n'
text+='  public static readonly int[] Triangles = {'+','.join(str(i) for t in triangles for i in t)+'};\n }\n}\n'
(root/'Staged/KingVanguardFaceMesh.cs').write_text(text)
(root/'Review/bow-landmarks.json').write_text(json.dumps({'neutral':neutral.tolist(),'bow':bow.tolist(),'triangles':data['triangles'],'labels':data['labels']},indent=2))
(root/'Review/face-landmarks.json').write_text(json.dumps(data))
(root/'Review/mesh-checks.txt').write_text(f'PASS {checks} triangle samples, all six expression-to-bow paths; minimum signed double area {worst:.4f}.\n')
print('PASS',checks,'triangle samples; minimum area',worst)
