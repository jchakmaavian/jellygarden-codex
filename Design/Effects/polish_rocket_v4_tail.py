from pathlib import Path
import shutil
r=Path(__file__).resolve().parents[2];o=r/'Design/Effects/Rocket-review-v4'
shutil.copy2(o/'checks.txt',o/'pre-tail-polish-checks.txt')
p=r/'Assets/JellyGarden/Shaders/RoyalRocketFire.shader';s=p.read_text();s=s.replace('alpha=smoothstep(edge+.19,edge-.13,side)*tailFade;','''alpha=smoothstep(edge+.19,edge-.13,side)*tailFade;
                    // Even a short plume that exits early needs a soft cap at its birth point.
                    alpha*=smoothstep(0,.08,i.uv.x*_TravelFraction);''');p.write_text(s,newline='\r\n')
p=r/'Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs';s=p.read_text()
s=s.replace('public MeshRenderer exhaust; public readonly','public MeshRenderer exhaust; public Gradient preRocketGradient; public readonly')
s=s.replace('foreach(var f in flyers){f.used=false;','foreach(var f in flyers){if(f.rocket&&f.preRocketGradient!=null)f.trail.colorGradient=f.preRocketGradient;f.used=false;')
s=s.replace('f.go.SetActive(false);f.used=false;f.sprite.enabled=true;','if(f.rocket&&f.preRocketGradient!=null)f.trail.colorGradient=f.preRocketGradient;\n            f.go.SetActive(false);f.used=false;f.sprite.enabled=true;')
p.write_text(s,newline='\r\n')
p=r/'Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs';s=p.read_text();s=s.replace('f.trail.sharedMaterial=rocketTrailMaterial;','f.preRocketGradient=f.trail.colorGradient;\n                    f.trail.sharedMaterial=rocketTrailMaterial;');p.write_text(s,newline='\r\n')
p=r/'Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs';s=p.read_text()
s=s.replace('int exitStart=fx.RocketScreenExits;','''var poolGradients=fx.GetComponentsInChildren<TrailRenderer>(true).Where(t=>t.gameObject.name.StartsWith("Projectile")).ToDictionary(t=>t,t=>t.colorGradient);
                int exitStart=fx.RocketScreenExits;''')
s=s.replace('check(CandyShatterService.Instance.ActiveFragments==0,name+', '''check(poolGradients.All(pair=>new[]{0f,.25f,.5f,.75f,1f}.All(t=>Vector4.Distance(pair.Key.colorGradient.Evaluate(t),pair.Value.Evaluate(t))<.001f)),name+": rocket returns the original trail gradient before another booster borrows the pool");
                check(CandyShatterService.Instance.ActiveFragments==0,name+''')
p.write_text(s,newline='\r\n')
print('Softened the short startup tail and restored borrowed trail gradients on release/cancel, with pool-state regression coverage.')
