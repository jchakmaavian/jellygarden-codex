from pathlib import Path
r=Path(__file__).resolve().parents[2]
p=r/'Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs'
s=p.read_text().replace('Rocket-review-v3','Rocket-review-v4')
s=s.replace('var fx=RoyalPowerupDirector.Get;\n            for(int scenario','var fx=RoyalPowerupDirector.Get;\n            yield return CompareLengths(lm,seed,colours,check,speed);\n            for(int scenario')
s=s.replace('originHeld=true','bounded=true')
s=s.replace('originHeld&=ribbon.startColor.a>=.85f;','bounded&=Vector3.Distance(start,end)<=fx.RocketExhaustLengthCells*Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x)+.01f;')
s=s.replace('check(originHeld,name+": flame at launch area stays fed until head exits");','check(bounded,name+": exhaust remains within the five-cell budget throughout flight");')
s=s.replace('custom rocket shader and four fixed emitters replace toolkit effects','custom rocket shader and six fixed pooled emitters')
at=s.index('        static IEnumerator AtSpeed(')
method='''        static IEnumerator CompareLengths(LevelManager lm,Action seed,Action colours,Action<bool,string> check,float speed)
        {
            var fx=RoyalPowerupDirector.Get;float original=fx.RocketExhaustLengthCells;
            try
            {
                foreach(float length in new[]{4.5f,5f,5.5f})for(int direction=0;direction<2;direction++)
                {
                    fx.Cancel();lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.allowShowTip=false;lm.DragBlocked=true;
                    CandyShatterService.Instance.Clear();seed();yield return null;colours();yield return null;
                    fx.RocketExhaustLengthCells=length;
                    bool vertical=direction==1;var origin=lm.GetSquare(vertical?3:0,vertical?7:3).item;
                    origin.currentType=vertical?ItemsTypes.VERTICAL_STRIPPED:ItemsTypes.HORIZONTAL_STRIPPED;
                    yield return null;
                    string name="length-"+length.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+(vertical?"-vertical-":"-horizontal-")+speed;
                    float pitch=Mathf.Abs(vertical?lm.squareHeight*lm.GameField.lossyScale.y:lm.squareWidth*lm.GameField.lossyScale.x);
                    float started=Time.time,nextCapture=.15f,maxLength=0;int frame=0,objects=fx.GetComponentsInChildren<Transform>(true).Length;
                    bool bounded=true,attached=true,lifeMatches=true,validShader=true;
                    var lastPositions=new Dictionary<SpriteRenderer,Vector3>();var lastTimes=new Dictionary<SpriteRenderer,float>();
                    origin.DestroyItem(true);
                    do
                    {
                        yield return new WaitForEndOfFrame();
                        foreach(var head in fx.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if(!head.enabled||!head.gameObject.name.StartsWith("Projectile"))continue;
                            var ribbon=head.transform.Find("Continuous flame ribbon").GetComponent<LineRenderer>();
                            if(!ribbon.enabled||ribbon.positionCount!=2)continue;
                            float cells=Vector3.Distance(ribbon.GetPosition(0),ribbon.GetPosition(1))/pitch;maxLength=Mathf.Max(maxLength,cells);
                            bounded&=cells<=length+.015f;attached&=Vector3.Distance(ribbon.GetPosition(1),head.transform.position)<.001f;
                            validShader&=ribbon.sharedMaterial.shader.isSupported&&ribbon.textureMode==LineTextureMode.Stretch;
                            if(lastPositions.TryGetValue(head,out var prev)&&Time.time>lastTimes[head])
                            {
                                float velocity=Vector3.Distance(prev,head.transform.position)/(Time.time-lastTimes[head])/pitch;
                                if(velocity>.1f)
                                {
                                    var core=head.transform.Find("White-hot rocket core").GetComponent<TrailRenderer>();
                                    lifeMatches&=Mathf.Abs(core.time*velocity-length)<.12f&&Mathf.Abs(head.GetComponent<TrailRenderer>().time*velocity-length)<.12f;
                                }
                            }
                            lastPositions[head]=head.transform.position;lastTimes[head]=Time.time;
                        }
                        if(Time.time-started>=nextCapture&&frame<30)
                        {
                            var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+"-"+frame.ToString("D2")+".png",tex.EncodeToPNG());Object.Destroy(tex);
                            frame++;nextCapture+=.08f;
                        }
                        if(!RoyalPowerupDirector.Busy){lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();lm.DragBlocked=true;}
                    }while((RoyalPowerupDirector.Busy||fx.RocketVisualsActive)&&Time.time-started<8);
                    check(bounded&&Mathf.Abs(maxLength-length)<.02f,name+": measured mature ribbon "+maxLength.ToString("F3")+" actual gem cells, never exceeds budget");
                    check(attached,name+": flame stays attached to nozzle every frame");
                    check(lifeMatches,name+": measured speed times core/glow lifetime equals cell-length target");
                    check(validShader,name+": normalized plume shader supported");
                    check(!fx.RocketVisualsActive,name+": all trails and soft-tail particles fade and return to pool");
                    check(fx.GetComponentsInChildren<Transform>(true).Length==objects,name+": no VFX object growth");
                }
            }
            finally {fx.Cancel();fx.RocketExhaustLengthCells=original;}
        }
'''
s=s[:at]+method+s[at:]
p.write_text(s,newline='\r\n')
p=r/'Assets/JellyGarden/Scripts/Editor/RoyalEffectsChecks.cs';p.write_text(p.read_text().replace('Rocket-review-v3','Rocket-review-v4'),newline='\r\n')
print('Added 12 controlled length/direction/speed comparisons before existing rocket regression.')
