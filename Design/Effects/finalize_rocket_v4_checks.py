from pathlib import Path
import shutil
r=Path(__file__).resolve().parents[2];o=r/'Design/Effects/Rocket-review-v4'
shutil.copy2(o/'checks.txt',o/'first-run-checks.txt')
p=r/'Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs';p.write_text(p.read_text().replace('Mathf.Min(distance,exit,exhaustLength)','Mathf.Min(Mathf.Min(distance,exit),exhaustLength)'),newline='\r\n')
p=r/'Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs';s=p.read_text()
s=s.replace('        static IEnumerator AtSpeed(','''        static float DistanceToSegment(Vector2 point,Vector2 start,Vector2 end)
        {
            Vector2 delta=end-start;float t=delta.sqrMagnitude>.000001f?Mathf.Clamp01(Vector2.Dot(point-start,delta)/delta.sqrMagnitude):0;
            return Vector2.Distance(point,start+delta*t);
        }
        static Vector3 NoseOffset(SpriteRenderer head,float cell)
        {
            string n=head.sprite.name;
            return (n.EndsWith("Right")?Vector3.right:n.EndsWith("Left")?Vector3.left:n.EndsWith("Up")?Vector3.up:Vector3.down)*cell*.45f;
        }
        static IEnumerator AtSpeed(''')
s=s.replace('var moving=new HashSet<SpriteRenderer>();','var moving=new HashSet<SpriteRenderer>();\n                var sweptStarts=new Dictionary<SpriteRenderer,Vector3>();var sweptEnds=new Dictionary<SpriteRenderer,Vector3>();\n                float maximumSampleStep=0,maximumEndDistance=0;')
s=s.replace('previousPositions[id]=head.transform.position;previousTimes[id]=Time.time;','''sweptStarts[id]=previousPositions.TryGetValue(id,out var old)?old:head.transform.position;
                        sweptEnds[id]=head.transform.position;
                        maximumSampleStep=Mathf.Max(maximumSampleStep,Vector3.Distance(sweptStarts[id],sweptEnds[id]));
                        previousPositions[id]=head.transform.position;previousTimes[id]=Time.time;''')
old='if(scenario<3)leads &= fx.GetComponentsInChildren<SpriteRenderer>().Any(sr=>sr.enabled&&sr.gameObject.name.StartsWith("Projectile")&&Vector2.Distance(sr.transform.position,pos)<cell*.8f);'
new='''if(scenario<3)
                        {
                            // PNG capture can span a substantial part of a fast sweep. Check the
                            // actual segment traversed since the last sample, not only its end.
                            var heads=sweptEnds.Keys.Where(sr=>sr&&sr.enabled).ToArray();
                            maximumEndDistance=Mathf.Max(maximumEndDistance,heads.Length>0?heads.Min(sr=>Vector2.Distance(sr.transform.position,pos))/cell:0);
                            leads&=heads.Any(sr=>DistanceToSegment(pos,sweptStarts[sr]+NoseOffset(sr,cell),sweptEnds[sr]+NoseOffset(sr,cell))<=cell*.55f);
                        }'''
assert old in s;s=s.replace(old,new)
s=s.replace('check(leads,name+": physical rocket head leads every crossed cell");','check(leads,name+": physical rocket nose sweeps every contacted cell (max sample step "+(maximumSampleStep/Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x)).ToString("F3")+" cells; max end distance "+maximumEndDistance.ToString("F3")+")");')
p.write_text(s,newline='\r\n')
print('Kept cell clearing and movement unchanged; fixed discrete-frame contact observation and removed per-frame params allocation.')
