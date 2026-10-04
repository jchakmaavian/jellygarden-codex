"""Author correspondence geometry; source raster artwork is never modified."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
names = ['Neutral','Worried','Sad','Happy','Excited','Laugh']
points = [[] for _ in names]
labels = []
def add(label, neutral, **poses):
    labels.append(label)
    for i,name in enumerate(names): points[i].append(poses.get(name,neutral))
def group(label, neutral, **poses):
    for k,point in enumerate(neutral): add(label+str(k),point,**{n:v[k] for n,v in poses.items()})

# Stable outside silhouette and a mesh around the deforming facial features.
for y in [0,180,270,780,940,1080,1254]:
    for x in [0,180,340,440,627,820,950,1080,1254]: add('anchor', (x,y))
for y in [380,520,650]:
    for x in [0,180,330,890,1080,1254]: add('side',(x,y))

group('crownContact',[(380,446),(400,416),(440,377),(480,347),(520,320),(560,297),(600,281),(640,273),(680,282),(720,297),(760,321),(800,357),(820,387)])

group('browL',[(434,408),(471,366),(513,339),(553,327),(565,345),(520,358),(480,386)],
 Worried=[(433,415),(471,374),(514,343),(551,310),(560,333),(522,371),(480,397)],
 Sad=[(436,427),(473,386),(515,359),(557,320),(565,342),(524,387),(482,409)],
 Laugh=[(433,404),(470,362),(512,336),(549,309),(557,338),(516,357),(477,383)])
group('browR',[(633,302),(664,288),(705,302),(747,328),(760,346),(707,328),(664,313)],
 Worried=[(633,289),(651,301),(697,319),(744,342),(760,365),(702,343),(650,322)],
 Sad=[(627,309),(649,319),(694,337),(744,354),(760,372),(700,359),(646,343)],
 Laugh=[(634,291),(664,281),(704,298),(744,328),(757,350),(704,326),(661,310)])
group('eyeL',[(459,455),(473,426),(511,413),(549,423),(561,446),(545,465),(510,470),(476,469)],
 Sad=[(458,470),(475,444),(513,431),(551,440),(563,457),(545,479),(513,484),(477,482)],
 Laugh=[(451,456),(465,431),(500,418),(537,428),(557,447),(532,437),(500,427),(470,442)])
group('eyeR',[(650,421),(665,390),(697,377),(729,384),(751,409),(733,433),(695,444),(664,439)],
 Sad=[(653,430),(667,405),(698,395),(730,399),(753,419),(734,444),(696,455),(664,449)],
 Laugh=[(656,423),(672,402),(708,390),(741,402),(762,419),(737,411),(708,399),(677,411)])
group('nose',[(549,487),(578,451),(618,439),(666,452),(693,487),(674,513),(624,531),(572,521)],
 Laugh=[(543,469),(572,435),(610,420),(656,434),(690,469),(669,491),(616,506),(565,496)])
group('moustacheL',[(420,558),(438,516),(454,542),(508,535),(565,527),(623,539),(625,571),(577,586),(516,612),(457,606)],
 Worried=[(430,573),(446,540),(460,563),(514,551),(570,534),(626,540),(625,577),(578,595),(515,625),(459,616)],
 Sad=[(430,581),(446,551),(460,572),(514,559),(570,538),(626,541),(625,580),(578,600),(515,632),(459,624)],
 Laugh=[(414,557),(435,516),(451,538),(508,514),(563,503),(621,510),(618,551),(567,568),(510,592),(451,594)])
group('moustacheR',[(632,539),(679,522),(744,505),(790,504),(809,477),(826,512),(803,552),(752,576),(688,574),(636,569)],
 Worried=[(632,540),(679,530),(744,516),(790,513),(809,488),(826,523),(803,566),(752,585),(688,581),(636,576)],
 Sad=[(632,541),(679,536),(744,528),(790,526),(809,498),(826,537),(803,578),(752,591),(688,587),(636,580)],
 Laugh=[(627,510),(676,487),(741,477),(790,486),(810,466),(828,505),(803,545),(750,557),(683,548),(631,548)])
group('mouth',[(541,587),(581,591),(627,581),(679,571),(716,559),(708,578),(676,598),(627,610),(581,608),(550,598)],
 Worried=[(542,607),(581,590),(627,580),(679,577),(718,581),(711,592),(676,591),(627,596),(581,603),(550,614)],
 Sad=[(542,617),(581,596),(627,583),(679,581),(718,587),(711,598),(676,595),(627,600),(581,610),(550,624)],
 Happy=[(538,589),(579,591),(627,583),(680,574),(718,559),(706,596),(676,619),(627,631),(579,621),(548,603)],
 Excited=[(536,590),(579,590),(627,582),(680,572),(718,558),(710,608),(678,642),(627,658),(579,642),(548,615)],
 Laugh=[(491,590),(548,565),(623,557),(684,547),(741,550),(730,620),(690,676),(624,699),(558,682),(521,643)])
# Chin/beard roots follow the lip opening, rather than allowing a face cutout seam.
group('chin',[(495,643),(558,650),(627,632),(696,638),(751,610),(790,683),(712,746),(627,800),(532,751),(460,704)],
 Laugh=[(492,672),(556,713),(628,712),(696,700),(765,654),(802,711),(717,767),(627,810),(529,774),(461,728)])

# Keep the lip/beard support topology positive across very different openings.
def correct(pose, label, point): points[names.index(pose)][labels.index(label)] = point
for pose in ['Worried','Sad']:
 correct(pose,'mouth1',(581,610 if pose=='Sad' else 604))
for pose,offset in [('Happy',15),('Excited',50)]:
 for label in ['chin1','chin2','chin3']:
  i=labels.index(label);x,y=points[names.index(pose)][i];correct(pose,label,(x,y+offset))
for label,point in {'nose1':(572,458),'nose2':(610,444),'moustacheR1':(676,500),'mouth0':(515,607),'mouth1':(554,588),'mouth2':(623,567),'mouth3':(684,565),'mouth9':(533,661)}.items():correct('Laugh',label,point)

correct('Sad','nose3',(666,455))
correct('Laugh','mouth0',(518,607))
correct('Laugh','chin1',(556,740))
# Redundant interior lip controls can cross in a closed mouth; the surrounding
# lip contour remains sufficient and keeps the mesh valid.
for label in ['mouth1','mouth3','moustacheL7','nose2']:
 i=labels.index(label);labels.pop(i)
 for pose in points:pose.pop(i)

# Bowyer-Watson Delaunay in source space, no external geometry dependency.
base=points[0]; n=len(base); cloud=base+[(-10000,-10000),(20000,-10000),(0,20000)]
triangles=[(n,n+1,n+2)]
def circle(tri,p):
    a,b,c=[cloud[i] for i in tri]
    ax,ay=a[0]-p[0],a[1]-p[1];bx,by=b[0]-p[0],b[1]-p[1];cx,cy=c[0]-p[0],c[1]-p[1]
    d=(ax*ax+ay*ay)*(bx*cy-cx*by)-(bx*bx+by*by)*(ax*cy-cx*ay)+(cx*cx+cy*cy)*(ax*by-bx*ay)
    orient=(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    return d*orient>1e-7
for i,p in enumerate(base):
    bad=[t for t in triangles if circle(t,p)];edges={}
    for t in bad:
        for a,b in [(t[0],t[1]),(t[1],t[2]),(t[2],t[0])]:
            key=tuple(sorted((a,b)));edges[key]=edges.get(key,0)+1
    triangles=[t for t in triangles if t not in bad]+[(a,b,i) for (a,b),count in edges.items() if count==1]
triangles=[t for t in triangles if max(t)<n]
for i,t in enumerate(triangles):
    a,b,c=[base[k] for k in t]
    if (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])<0: triangles[i]=(t[0],t[2],t[1])

text='using UnityEngine;\nnamespace RoyalThrone.Characters {\n public static class KingVanguardFaceMesh {\n'
for name,coords in zip(names,points):
    text+='  public static readonly Vector2[] '+name+' = {\n'+',\n'.join('   new Vector2(%sf,%sf)'%(x,y) for x,y in coords)+'\n  };\n'
text+='  public static readonly int[] Triangles = {'+','.join(str(i) for t in triangles for i in t)+'};\n }\n}\n'
(root/'Staged/KingVanguardFaceMesh.cs').write_text(text,newline='\n')
(root/'Review/face-landmarks.json').write_text(json.dumps({'labels':labels,'poses':dict(zip(names,points)),'triangles':triangles},indent=2),newline='\n')
print(n,'corresponding vertices;',len(triangles),'triangles')
