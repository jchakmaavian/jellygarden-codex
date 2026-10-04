from pathlib import Path
import itertools
import json
import math
import random

root = Path(__file__).resolve().parents[1]
data = json.loads((root/'Review/face-landmarks.json').read_text())
poses = list(data['poses'].values())
checked = 0
def check(points):
    global checked
    for triangle in data['triangles']:
        a,b,c = [points[i] for i in triangle]
        area = (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
        assert area > 0, f'Inverted triangle: {triangle}, {area}'
        checked += 1
for a,b in itertools.combinations_with_replacement(poses,2):
    for step in range(21):
        t=step/20
        check([(x[0]*(1-t)+y[0]*t,x[1]*(1-t)+y[1]*t) for x,y in zip(a,b)])
def eye(point,center):
    distance=math.hypot((point[0]-center[0])/72,(point[1]-center[1])/48)
    t=max(0,min(1,(distance-.6)/.6))
    return 1-t*t*(3-2*t)
for pose in poses:
    for blink in [0,.25,.5,.75,1]:
        for wink in [False,True]:
            points=[]
            for i,point in enumerate(pose):
                neutral=poses[0][i];laugh=poses[5][i]
                weight=max(eye(neutral,(516,443))*blink,0 if wink else eye(neutral,(690,415))*blink)
                points.append((point[0]*(1-weight)+laugh[0]*weight,point[1]*(1-weight)+laugh[1]*weight))
            check(points)
random.seed(104)
for _ in range(200):
    weights=[random.random() for _ in poses];total=sum(weights);weights=[w/total for w in weights]
    check([(sum(pose[i][0]*w for pose,w in zip(poses,weights)),sum(pose[i][1]*w for pose,w in zip(poses,weights))) for i in range(len(poses[0]))])
report=f'PASS {checked} triangle checks; zero inverted triangles.\nAll expression pairs, partial blinks, winks, and 200 seeded interrupted blends.\n'
(root/'Review/mesh-checks.txt').write_text(report,newline='\n')
print(report)
