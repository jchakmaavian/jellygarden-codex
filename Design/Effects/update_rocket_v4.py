from pathlib import Path
import shutil, hashlib, json, uuid
root=Path(__file__).resolve().parents[2]
out=root/'Design/Effects/Rocket-review-v4'
files=['Assets/JellyGarden/Scripts/Juice/RoyalRocketSequence.cs','Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs','Assets/JellyGarden/Shaders/RoyalRocketFire.shader','Assets/JellyGarden/Scripts/Editor/RoyalRocketChecks.cs','Assets/JellyGarden/Scripts/Editor/RoyalEffectsChecks.cs']
out.mkdir(parents=True,exist_ok=True)
for name in files:
    backup=out/'before'/name
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists(): shutil.copy2(root/name,backup)
preserved={str(p.relative_to(root)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'Assets').rglob('*') if p.is_file() and str(p.relative_to(root)).replace('\\','/') not in files}
(out/'preserved-sha256.json').write_text(json.dumps(preserved,indent=2))
(out/'INSPECTION.md').write_text('''# Rocket exhaust inspection — 15 September 2026

The supplied crop shows compact gold flame at the nozzle, a brilliant white middle with four-point stars, then soft white light fading into transparency, over approximately five gem pitches.

The live rocket is built once in RoyalPowerupDirector from EffectsRig.prefab into a 32-projectile pool. It uses cached half-sprite views of the original horizontal/vertical rocket textures. There is no separate RocketLineClear prefab. Each head has a core TrailRenderer, broad glow TrailRenderer, custom flame mesh and LineRenderer. Four fixed ParticleSystems supply launch/contact rings, lights, stars and embers. All use the custom unlit RoyalThrone/RocketFire shader. The ribbon uses premultiplied blending; the light layers are additive.

The previous ribbon stays anchored at launch and uses tiled UVs without longitudinal colour or opacity changes. It grows for the entire flight, and old sections receive new spark emissions. Core/glow times are fixed at .32/.70 seconds regardless of measured rocket speed. Those are the causes of excessive length and the uniform orange line.

Movement uses actual grid-axis pitch, .22 seconds anticipation, and constant speed derived from the existing .8–1.2 second board sweep. Each nose crossing runs the existing local impact, .055 second contact delay, then destruction service. Camera-derived endpoints already carry both heads beyond the viewport. Those paths will remain unchanged.

The runtime GameplayVFX container is unmasked, on the Default sorting layer. The overlay gameplay HUD and UI popup sorting remain above it. No prefab, art, mask, canvas, sorting, UI, level, rules, other-booster or break-service changes are needed.

Change plan: bounded five-pitch plume, normalized distance UVs, speed-derived core/glow/particle lifetimes and spatial emission; white smoke plus dedicated exhaust stars in fixed pools. Compare 4.5 / 5 / 5.5 at .5× and 1× through the existing disposable Lab regression.
''')
def edit(name,fn):
    p=root/name;p.write_text(fn(p.read_text()),newline='\r\n')
def director(s):
    s=s.replace('public MeshRenderer exhaust; public bool','public MeshRenderer exhaust; public readonly MaterialPropertyBlock flameProperties=new MaterialPropertyBlock(); public bool')
    s=s.replace('flame.textureMode=LineTextureMode.Tile','flame.textureMode=LineTextureMode.Stretch')
    s=s.replace('// Keep the entire travelled path fed until exit; then extinguish it smoothly.','// Let the bounded plume and its speed-derived trails finish before pool reuse.')
    s=s.replace('float elapsed=0;\n                while(elapsed<.74f)','float elapsed=0, lifetime=Mathf.Max(f.trail.time,f.core.time)+.08f;\n                while(elapsed<lifetime)')
    s=s.replace('elapsed/.68f','elapsed/Mathf.Max(.1f,lifetime-.08f)')
    return s
edit(files[1],director)
def sequence(s):
    s=s.replace('ParticleSystem rocketImpact, rocketGlow, rocketStars, rocketEmbers;','ParticleSystem rocketImpact, rocketGlow, rocketStars, rocketEmbers, rocketSmoke, rocketExhaustStars;\n        [SerializeField,Range(4.5f,5.5f)] float rocketExhaustCells=5f;\n        public float RocketExhaustLengthCells { get=>rocketExhaustCells; set=>rocketExhaustCells=Mathf.Clamp(value,4.5f,5.5f); }')
    s=s.replace('rocketSystems.Length==4','rocketSystems.Length==6')
    s=s.replace('rocketSystems=new[]{rocketImpact,rocketGlow,rocketStars,rocketEmbers};','''rocketExhaustStars=CreateRocketParticles("StarSparkles - rocket exhaust","Star",2048,false);
            rocketSmoke=CreateRocketParticles("WhiteSmokeTail - rocket exhaust","WhiteSmoke",512,false);
            var smokeColour=rocketSmoke.colorOverLifetime;var smokeFade=new Gradient();
            smokeFade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.7f,.18f),new GradientAlphaKey(.35f,.55f),new GradientAlphaKey(0,1)});smokeColour.color=smokeFade;
            var smokeSize=rocketSmoke.sizeOverLifetime;smokeSize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.65f,1,1.35f));
            var starColour=rocketExhaustStars.colorOverLifetime;var starFade=new Gradient();
            starFade.SetKeys(new[]{new GradientColorKey(new Color(1,.87f,.48f),0),new GradientColorKey(Color.white,.55f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(1,.3f),new GradientAlphaKey(.8f,.55f),new GradientAlphaKey(0,1)});starColour.color=starFade;
            rocketSystems=new[]{rocketImpact,rocketGlow,rocketStars,rocketEmbers,rocketSmoke,rocketExhaustStars};''')
    start=s.index('        void RocketExhaustSparks(');end=s.index('        IEnumerator RocketCell',start)
    s=s[:start]+'''        // Spatial samples make density independent of frame rate and the Lab time scale.
        // Lifetimes include rearward drift: no particle can survive into a 7-cell tail.
        void RocketExhaustSparks(Vector3 nozzle,Vector3 direction,float pitch,float speed,float length,float travelled)
        {
            Vector3 across=new Vector3(-direction.y,direction.x,0);
            int count=RoyalThrone.Testing.RoyalSave.GetInt("RoyalThrone.HomeMotion",1)==0?1:4;
            for(int i=0;i<count;i++)
            {
                float behind=Mathf.Min(travelled,Range(.05f,.30f)),drift=Range(.08f,.28f);
                float lifetime=(length-behind-.16f)/(speed+drift)*Range(.80f,1f);
                var e=new ParticleSystem.EmitParams{
                    position=nozzle-direction*pitch*behind+across*pitch*Range(-.28f,.28f)+Vector3.back*.5f,
                    velocity=(-direction*drift+across*Range(-.10f,.10f))*pitch,
                    startSize=pitch*(i==0&&Range(0,1)<.15f?Range(.27f,.34f):Range(.10f,.23f)),
                    startLifetime=lifetime,rotation=Range(0,180),
                    startColor=new Color(1,1,1,Range(.75f,1f)),applyShapeToPosition=false};
                rocketExhaustStars.Emit(e,1);RocketSparkParticlesEmitted++;
            }
            for(int i=0;i<2;i++)
            {
                var e=new ParticleSystem.EmitParams{position=nozzle-direction*pitch*Mathf.Min(travelled,.18f)+across*pitch*Range(-.26f,.26f)+Vector3.back*.5f,
                    velocity=(-direction*.18f+across*Range(-.15f,.15f))*pitch,startSize=pitch*Range(.055f,.12f),
                    startLifetime=length*.58f/(speed+.18f),startColor=new Color(1,.86f,.36f,.75f),applyShapeToPosition=false};
                rocketEmbers.Emit(e,1);
            }
            // Soft white light belongs only to the last 1–1.5 cells of the moving plume.
            if(travelled>length*.70f)
            {
                float behind=Range(length*.70f,Mathf.Min(travelled,length*.88f));
                float drift=.08f;
                var e=new ParticleSystem.EmitParams{position=nozzle-direction*behind*pitch+across*pitch*Range(-.13f,.13f)+Vector3.back*.5f,
                    velocity=(-direction*drift+across*Range(-.10f,.10f))*pitch,startSize=pitch*Range(.28f,.48f),
                    startLifetime=Mathf.Max(.04f,(length-behind-.22f)/(speed+drift)),rotation=Range(0,360),
                    startColor=new Color(1,1,.98f,.32f),applyShapeToPosition=false};rocketSmoke.Emit(e,1);
            }
            if(!rocketExhaustStars.isPlaying)rocketExhaustStars.Play(false);
            if(!rocketEmbers.isPlaying)rocketEmbers.Play(false);
            if(!rocketSmoke.isPlaying)rocketSmoke.Play(false);
        }
        static Gradient RocketExhaustGradient(bool core)
        {
            var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(core?new Color(1,.99f,.85f):new Color(1,.73f,.23f),0),
                new GradientColorKey(core?Color.white:new Color(1,.96f,.78f),.48f),new GradientColorKey(Color.white,.74f),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(core?.95f:.55f,0),new GradientAlphaKey(core?.95f:.38f,.48f),new GradientAlphaKey(core?.6f:.16f,.73f),new GradientAlphaKey(0,1)});return g;
        }
''' + s[end:]
    s=s.replace('float speed=reach/duration,maxExit=0;','float speed=reach/duration,maxExit=0;\n            float exhaustLength=RocketExhaustLengthCells,exhaustLife=exhaustLength/speed;')
    s=s.replace('f.trail.time=.70f;f.trail.widthMultiplier=1.22f*pitch','f.trail.time=exhaustLife;f.trail.widthMultiplier=1.12f*pitch')
    s=s.replace('f.trail.widthCurve=AnimationCurve.Linear(0,1,1,1);','f.trail.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.45f,.85f),new Keyframe(1,.15f));')
    s=s.replace('RocketTrailGradient(new Color(1,.40f,.035f,.58f),.62f)','RocketExhaustGradient(false)')
    s=s.replace('f.flame.widthMultiplier=.94f*pitch;f.flame.enabled=false;','f.flame.widthMultiplier=.94f*pitch;f.flame.enabled=false;\n                    f.flameProperties.SetFloat("_TravelFraction",0);f.flameProperties.SetFloat("_Phase",RocketLaunches*.73f+d);f.flame.SetPropertyBlock(f.flameProperties);')
    s=s.replace('f.core.time=.32f;f.core.widthMultiplier=.30f*pitch;','f.core.time=exhaustLife;f.core.widthMultiplier=.28f*pitch;')
    s=s.replace('f.core.widthCurve=AnimationCurve.Linear(0,1,1,.60f);','f.core.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.5f,.95f),new Keyframe(1,.05f));')
    s=s.replace('RocketTrailGradient(new Color(1,.98f,.78f,.88f),.30f)','RocketExhaustGradient(true)')
    s=s.replace('float distance=0,afterburn=0;','float distance=0;')
    s=s.replace('                afterburn+=Time.deltaTime;bool feedPath=afterburn>=.06f;if(feedPath)afterburn%=.06f;\n','')
    old='''                        f.go.transform.position=head+Vector3.back*.6f;f.flame.SetPosition(1,head+Vector3.back*.6f);
                        // Keep stars and burning flecks alive in the older cleared section too.
                        if(feedPath&&distance>.6f)
                            for(int n=0;n<Mathf.Clamp(Mathf.CeilToInt(distance/3f),1,3);n++)
                                RocketExhaustSparks(p+across*lane*crossPitch+axis*d*Range(.5f,Mathf.Min(distance,exit))*pitch,axis*d);'''
    new='''                        f.go.transform.position=head+Vector3.back*.6f;
                        float visibleCells=Mathf.Min(distance,exit,exhaustLength);
                        f.flame.SetPosition(0,head-axis*d*visibleCells*pitch+Vector3.back*.6f);
                        f.flame.SetPosition(1,head+Vector3.back*.6f);
                        f.flameProperties.SetFloat("_TravelFraction",visibleCells/exhaustLength);f.flame.SetPropertyBlock(f.flameProperties);'''
    assert old in s;s=s.replace(old,new)
    s=s.replace('RocketExhaustSparks(p+across*lane*crossPitch+axis*d*(sample*.10f)*pitch,axis*d);','RocketExhaustSparks(p+across*lane*crossPitch+axis*d*(sample*.10f)*pitch,axis*d,pitch,speed,exhaustLength,sample*.10f);')
    return s
edit(files[0],sequence)
def shader(s):
    s=s.replace('5 flame ribbon','5 flame ribbon, 6 white smoke')
    s=s.replace('        _Phase (','        _TravelFraction ("Visible fraction of five-cell plume", Float) = 1\n        _Phase (')
    s=s.replace('float _Shape, _Boost, _FxTime, _Phase;','float _Shape, _Boost, _FxTime, _Phase, _TravelFraction;')
    s=s.replace('float3(1,.19,.015),float3(1,.68,.045)','float3(1,.55,.09),float3(1,.84,.24)')
    start=s.index('                if(_Shape>4.5)');end=s.index('                float3 rgb=',start)
    s=s[:start]+'''                if(_Shape>4.5 && _Shape<5.5)
                {
                    // UV distance is measured behind the nozzle, not elapsed flight length.
                    // Short startup plumes show just the hot section; a mature plume spans five cells.
                    float age=(1-i.uv.x)*_TravelFraction;
                    float clock=_FxTime+_Phase;
                    float taper=lerp(.96,.32,smoothstep(.20,1,age));
                    float waves=.07*sin(age*61-clock*23)+.045*sin(age*113+clock*31);
                    float edge=(.77+waves)*taper;
                    float side=abs(p.y+.025*sin(age*79-clock*34)*taper);
                    float tailFade=1-smoothstep(.69,1,age);
                    alpha=smoothstep(edge+.19,edge-.13,side)*tailFade;
                    float tongues=.045*sin(age*91-clock*35)+.025*sin(age*173+clock*41);
                    float gold=smoothstep(edge,.27*taper,side+tongues);
                    float hot=smoothstep(.34*taper,.07*taper,side+tongues);
                    float3 fire=lerp(float3(1,.57,.10),float3(1,.89,.30),gold);
                    fire=lerp(fire,float3(1,1,.93),hot);
                    // Yellow burns away before the final 1.5-cell soft white light tail.
                    tint=lerp(fire,float3(1,1,.98),smoothstep(.25,.73,age));
                }
                else if(_Shape>5.5)
                {
                    // Textureless soft white smoke/light, never dark smoke or a square quad.
                    float r=length(p);
                    alpha=exp(-r*r*4.8)*smoothstep(1,.55,r);
                }
''' + s[end:]
    return s
edit(files[2],shader)
mat=root/'Assets/JellyGarden/Resources/RoyalRocketFX/WhiteSmoke.mat'
mat.write_text((root/'Assets/JellyGarden/Resources/RoyalRocketFX/FlameTrail.mat').read_text().replace('m_Name: FlameTrail','m_Name: WhiteSmoke').replace('_Shape: 5','_Shape: 6').replace('_DstBlend: 10','_DstBlend: 1'),newline='\r\n')
Path(str(mat)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',newline='\r\n')
print('Updated only rocket exhaust runtime/shader and added pooled smoke material; baseline preserved in',out)
