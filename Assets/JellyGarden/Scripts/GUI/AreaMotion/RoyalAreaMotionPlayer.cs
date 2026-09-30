using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation.AreaMotion
{
    /// <summary>Native layered restoration rendered into the existing Home UI. Owns no economy or save state.</summary>
    [DisallowMultipleComponent]
    public sealed class RoyalAreaMotionPlayer : MonoBehaviour
    {
        public const string Folder="RoyalAreas/NativeMotion/";
        static int stageSequence;
        sealed class Pose
        {
            public Vector3 position,scale,euler;public Quaternion rotation;public Color color;
            public int sprite,order;public bool active,enabled,flipX,flipY,eulerChanged;
        }
        sealed class Layer
        {
            public int node,sprite=-1;public Mesh mesh;public MeshRenderer renderer;public MeshFilter filter;
            public MaterialPropertyBlock properties=new MaterialPropertyBlock();public Vector3[] work;public Matrix4x4[] bones;
        }
        SceneData data;
        Transform stage;
        Transform[] nodes;
        Pose[] pose;
        Mesh[] meshes;
        Texture2D[] textures;
        Material[] materials;
        readonly List<Layer> layers=new List<Layer>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        Camera cameraView;
        RawImage surface;
        RenderTexture front,back;
        RoyalAreaParticles effects;
        FlagData flags;Mesh flagMesh;Material flagMaterial;
        AudioSource revealAudio,ambientAudio,fountainAudio;
        int doneMask,currentTask=-1,epoch;
        float taskAge,clock;
        bool effectFired,paused,dirty=true,fountainVisible;
        public bool MotionEnabled {get;set;}=true;
        public bool SoundEnabled {get;set;}=true;
        public bool Visible {get;private set;}=true;
        public bool Loaded=>data!=null;
        public int Area=>data==null?0:data.area;
        public int TaskCount=>data==null?0:data.tasks.Length;
        public bool Revealing=>currentTask>=0;
        public int CurrentTask=>currentTask;
        public int CompletedMask=>doneMask;
        public float Clock=>clock;
        public Camera StageCamera=>cameraView;
        public RawImage Surface=>surface;
        public int ActiveParticleCount=>effects==null?0:effects.ActiveParticleCount;
        public static bool Available(int area)=>area>=1&&area<=3&&Resources.Load<TextAsset>(Folder+"Area"+area.ToString("00")+"/scene");

        public bool Load(int area,int completed)
        {
            if(Area==area){SetProgress(completed);return true;}
            var source=Resources.Load<TextAsset>(Folder+"Area"+area.ToString("00")+"/scene");if(!source)return false;
            Clear();data=JsonUtility.FromJson<SceneData>(source.text);epoch++;doneMask=completed;clock=0;
            if(!surface){var go=new GameObject("Animated native area",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.layer=5;go.transform.SetParent(transform,false);surface=go.GetComponent<RawImage>();surface.raycastTarget=false;var r=surface.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;go.transform.SetAsFirstSibling();}
            surface.enabled=true;
            stage=new GameObject("Royal Throne area stage "+area).transform;stage.gameObject.hideFlags=HideFlags.DontSave;stage.gameObject.layer=31;stage.position=new Vector3(20000+(++stageSequence%50)*100,20000,0);
            var cameraObject=new GameObject("Area render camera",typeof(Camera));cameraObject.layer=31;cameraObject.transform.SetParent(stage,false);cameraView=cameraObject.GetComponent<Camera>();cameraView.orthographic=true;cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.045f,.09f,.15f,1);cameraView.nearClipPlane=.1f;cameraView.farClipPlane=2000;cameraView.cullingMask=1<<31;cameraView.depth=-1000;cameraView.allowHDR=false;cameraView.allowMSAA=false;cameraView.useOcclusionCulling=false;
            var shader=Resources.Load<Shader>(Folder+"Shaders/RoyalAreaLayer");if(!shader)throw new InvalidOperationException("Area motion shader is missing.");
            meshes=new Mesh[data.sprites.Length];textures=new Texture2D[meshes.Length];materials=new Material[meshes.Length];
            var noise=Resources.Load<Texture2D>(Folder+"VFX/distortion");
            for(int i=0;i<meshes.Length;i++){
                var s=data.sprites[i];textures[i]=Resources.Load<Texture2D>(Folder+s.texture);if(!textures[i])throw new InvalidOperationException("Missing area sprite "+s.texture);
                meshes[i]=NewMesh(s.positions,s.uv,s.indices,s.name);materials[i]=new Material(shader){name=s.name,mainTexture=textures[i]};materials[i].SetFloat("_Water",s.water?1:0);materials[i].SetFloat("_FlowKind",s.flow);materials[i].SetTexture("_NoiseTex",noise);
            }
            nodes=new Transform[data.nodes.Length];pose=new Pose[nodes.Length];
            for(int i=0;i<nodes.Length;i++){
                var n=data.nodes[i];var go=new GameObject(n.name);go.layer=31;nodes[i]=go.transform;nodes[i].SetParent(n.parent>=0?nodes[n.parent]:stage,false);pose[i]=new Pose();
                if(n.sortingGroupOrder!=int.MinValue)go.AddComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder=n.sortingGroupOrder/4;
                if(n.sprite>=0){var layer=new Layer{node=i,filter=go.AddComponent<MeshFilter>(),renderer=go.AddComponent<MeshRenderer>()};layer.renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;layer.renderer.receiveShadows=false;layer.renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;layer.renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;layers.Add(layer);AssignSprite(layer,n.sprite);}
            }
            if(area==1){flags=JsonUtility.FromJson<FlagData>(Resources.Load<TextAsset>(Folder+"flags").text);var f=flags.frames[0].meshes[0];flagMesh=NewMesh(f.positions,f.uv,f.indices,"Native waving flags");flagMaterial=new Material(shader){mainTexture=Resources.Load<Texture2D>(Folder+"VFX/flags")};for(int i=0;i<nodes.Length;i++)if(data.nodes[i].flag){nodes[i].gameObject.AddComponent<MeshFilter>().sharedMesh=flagMesh;var r=nodes[i].gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=flagMaterial;r.sortingOrder=29000;}}
            effects=new RoyalAreaParticles(stage);
            foreach(var p in data.particles)effects.Attach(nodes[p.node],p.data);
            revealAudio=Audio("Area reveal sound",false,.8f);ambientAudio=Audio("Garden ambience",true,.15f);fountainAudio=Audio("Fountain ambience",true,.3f);
            if(area==1)ambientAudio.clip=Resources.Load<AudioClip>(Folder+"Audio/area_1_and_4_ambient_noise");
            fountainAudio.clip=Resources.Load<AudioClip>(Folder+"Audio/area_1_fountain_idle");
            Resize();ApplyPose();UpdateEffects();stage.gameObject.SetActive(Visible);UpdateAudio();dirty=true;return true;
        }
        Mesh NewMesh(Vector3[] positions,Vector2[] uv,int[] indices,string name)
        {
            var m=new Mesh{name=name};m.MarkDynamic();m.vertices=positions;m.uv=uv;m.triangles=indices;
            var colors=new Color32[positions.Length];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(255,255,255,255);m.colors32=colors;m.RecalculateBounds();ownedMeshes.Add(m);return m;
        }
        void AssignSprite(Layer l,int index)
        {
            if(index<0||index>=meshes.Length){l.renderer.enabled=false;return;}
            if(l.sprite==index)return;l.sprite=index;var s=data.sprites[index];var n=data.nodes[l.node];
            if(n.bones!=null&&n.bones.Length>0&&s.weights.Length==s.positions.Length&&s.bindposes.Length>0){
                if(l.mesh)Destroy(l.mesh);l.mesh=NewMesh(s.positions,s.uv,s.indices,s.name+" skinned");l.filter.sharedMesh=l.mesh;l.work=new Vector3[s.positions.Length];l.bones=new Matrix4x4[n.bones.Length];
            }else{l.filter.sharedMesh=meshes[index];l.work=null;l.bones=null;}
            l.renderer.sharedMaterial=materials[index];
        }
        AudioSource Audio(string name,bool loop,float volume){var go=new GameObject(name);go.transform.SetParent(stage,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.loop=loop;s.volume=volume;return s;}
        public void SetProgress(int mask){doneMask=mask&((1<<TaskCount)-1);currentTask=-1;taskAge=0;epoch++;if(revealAudio)revealAudio.Stop();dirty=true;if(Loaded){ApplyPose();UpdateEffects();}}
        public void SetVisible(bool visible)
        {
            if(Visible==visible)return;Visible=visible;
            if(!visible){epoch++;if(currentTask>=0)doneMask|=1<<currentTask;currentTask=-1;if(revealAudio){revealAudio.Stop();ambientAudio.Stop();fountainAudio.Stop();}}
            if(stage)stage.gameObject.SetActive(visible);dirty=true;
        }
        public float Duration(int task)=>Loaded&&task>=0&&task<TaskCount?data.tasks[task].duration:0;
        public IEnumerator Reveal(int task,bool motion)
        {
            if(!Loaded||task<0||task>=TaskCount)yield break;
            int token=++epoch;doneMask&=~(1<<task);currentTask=task;taskAge=0;effectFired=false;dirty=true;
            if(SoundEnabled){revealAudio.clip=Resources.Load<AudioClip>(Folder+data.tasks[task].audio);revealAudio.Play();}
            float duration=motion?data.tasks[task].duration+.35f:.12f;
            try{
                float elapsed=0;
                while(elapsed<duration&&token==epoch){if(!paused){elapsed+=Time.unscaledDeltaTime;taskAge=motion?elapsed:data.tasks[task].duration;dirty=true;}yield return null;}
            }finally{
                if(token==epoch){doneMask|=1<<task;currentTask=-1;dirty=true;ApplyPose();UpdateEffects();if(SoundEnabled&&revealAudio)revealAudio.PlayOneShot(Resources.Load<AudioClip>(Folder+"Audio/royal_throne_restore_chime"),.7f);}
            }
        }
        public void SeekForReview(int task,float time){currentTask=task;taskAge=time;doneMask=task<0?(1<<TaskCount)-1:(1<<task)-1;effectFired=true;ApplyPose();UpdateEffects();dirty=true;}
        void ApplyClip(ClipData clip,float time)
        {
            foreach(var c in clip.curves){var p=pose[c.node];float v=MotionCurves.Sample(c.keys,time,c.property>=15&&c.property<=20);
                switch(c.property){case 0:p.position.x=v;break;case 1:p.position.y=v;break;case 2:p.position.z=v;break;case 3:p.rotation.x=v;break;case 4:p.rotation.y=v;break;case 5:p.rotation.z=v;break;case 6:p.rotation.w=v;break;case 7:p.scale.x=v;break;case 8:p.scale.y=v;break;case 9:p.scale.z=v;break;case 10:p.euler.z=v;p.eulerChanged=true;break;case 11:p.color.r=v;break;case 12:p.color.g=v;break;case 13:p.color.b=v;break;case 14:p.color.a=v;break;case 15:p.active=v>.5f;break;case 16:p.enabled=v>.5f;break;case 17:p.order=Mathf.RoundToInt(v);break;case 18:p.flipX=v>.5f;break;case 19:p.flipY=v>.5f;break;case 20:p.sprite=Mathf.RoundToInt(v);break;case 21:p.euler.x=v;p.eulerChanged=true;break;case 22:p.euler.y=v;p.eulerChanged=true;break;}
            }
        }
        void ApplyPose()
        {
            if(!Loaded)return;
            for(int i=0;i<pose.Length;i++){var n=data.nodes[i];var p=pose[i];p.position=n.position;p.rotation=n.rotation;p.scale=n.scale;p.euler=n.rotation.eulerAngles;p.eulerChanged=false;p.color=n.color;p.active=n.active;p.enabled=n.enabled;p.sprite=n.sprite;p.order=n.order;p.flipX=n.flipX;p.flipY=n.flipY;}
            for(int i=0;i<TaskCount;i++){
                var t=data.tasks[i];bool current=i==currentTask,done=(doneMask&(1<<i))!=0;
                if(!current&&!done){pose[t.root].active=false;continue;}
                float tm=current?taskAge:t.duration;
                for(int j=0;j<t.clips.Length;j++){var c=data.clips[t.clips[j]];ApplyClip(c,Mathf.Clamp(tm-t.offsets[j],0,c.duration));}
                if(t.reconstructed&&current){int count=0;for(int j=0;j<pose.Length;j++)if(data.nodes[j].parent==t.root){float u=Mathf.Clamp01((tm-count++*.11f)/.7f);float spring=1-Mathf.Exp(-u*6)*Mathf.Cos(u*8);pose[j].active=u>0;pose[j].scale*=Mathf.Max(.001f,spring);pose[j].position.y+=(1-u)*.55f;}}
            }
            foreach(var c in data.clips)if(c.loop)ApplyClip(c,clock%c.duration);
            for(int i=0;i<nodes.Length;i++){var p=pose[i];nodes[i].localPosition=p.position;nodes[i].localRotation=p.eulerChanged?Quaternion.Euler(p.euler):p.rotation;nodes[i].localScale=new Vector3(p.scale.x*(p.flipX?-1:1),p.scale.y*(p.flipY?-1:1),p.scale.z);if(nodes[i].gameObject.activeSelf!=p.active)nodes[i].gameObject.SetActive(p.active);}
            foreach(var l in layers){var p=pose[l.node];AssignSprite(l,p.sprite);l.renderer.enabled=p.enabled&&p.sprite>=0;if(p.sprite<0)continue;l.renderer.sortingOrder=Mathf.Clamp(p.order/4,-32700,32700);var s=data.sprites[p.sprite];var n=data.nodes[l.node];
                if(l.work!=null&&nodes[l.node].gameObject.activeInHierarchy){for(int j=0;j<l.bones.Length;j++){int b=n.bones[j];l.bones[j]=b>=0&&j<s.bindposes.Length?nodes[l.node].worldToLocalMatrix*nodes[b].localToWorldMatrix*s.bindposes[j].Matrix:Matrix4x4.identity;}
                    for(int j=0;j<l.work.Length;j++){Vector3 v=Vector3.zero;for(int k=0;k<4;k++){float w=s.weights[j][k];int b=(int)s.boneIndices[j][k];if(w!=0&&b>=0&&b<l.bones.Length)v+=l.bones[b].MultiplyPoint3x4(s.positions[j])*w;}l.work[j]=v;}l.mesh.vertices=l.work;l.mesh.RecalculateBounds();}
                l.properties.Clear();l.properties.SetColor("_Color",p.color);l.properties.SetFloat("_Clock",clock);l.properties.SetFloat("_MaskMode",0);
                if(n.maskInteraction>0)for(int j=0;j<data.nodes.Length;j++){var mask=data.nodes[j];if(mask.maskSprite<0||!nodes[j].gameObject.activeInHierarchy||p.order<mask.maskBack||p.order>mask.maskFront)continue;var b=meshes[mask.maskSprite].bounds;l.properties.SetFloat("_MaskMode",n.maskInteraction);l.properties.SetVector("_MaskRect",new Vector4(b.min.x,b.min.y,b.max.x,b.max.y));l.properties.SetMatrix("_WorldToMask",nodes[j].worldToLocalMatrix);break;}
                if(s.water)l.properties.SetFloat("_WaterLine",cameraView.WorldToViewportPoint(l.renderer.bounds.max).y);
                l.renderer.SetPropertyBlock(l.properties);
            }
            if(flagMesh&&flags.frames.Length>0){int frame=Mathf.FloorToInt(clock%flags.duration*flags.fps)%flags.frames.Length;flagMesh.vertices=flags.frames[frame].meshes[0].positions;flagMesh.RecalculateBounds();}
        }
        void Resize()
        {
            if(!cameraView||!surface)return;var rect=surface.rectTransform.rect;float aspect=rect.height>1?Mathf.Clamp(rect.width/rect.height,.4f,1f):.5625f;
            int w=Mathf.Clamp(Screen.width,360,720),h=Mathf.RoundToInt(w/aspect);
            if(!front||front.width!=w||front.height!=h){ReleaseTargets();front=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32){name="Area motion A"};back=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32){name="Area motion B"};front.Create();back.Create();dirty=true;}
            cameraView.aspect=aspect;cameraView.orthographicSize=Mathf.Min((data.camera[3]-data.camera[1])*.5f,(data.camera[2]-data.camera[0])/(aspect*2));cameraView.transform.localPosition=new Vector3((data.camera[0]+data.camera[2])*.5f,(data.camera[1]+data.camera[3])*.5f,-500);
        }
        void UpdateEffects()
        {
            bool fountain=Area==1&&(doneMask&2)!=0;if(fountain!=fountainVisible){fountainVisible=fountain;effects.SetFountain(fountain);}
            if(currentTask>=0&&!effectFired&&taskAge>=data.tasks[currentTask].duration-.45f){effects.Play(data.tasks[currentTask].effect,clock);effectFired=true;}
        }
        void UpdateAudio()
        {
            if(!revealAudio)return;bool audible=SoundEnabled&&Visible&&!paused&&isActiveAndEnabled;
            revealAudio.mute=!audible;ambientAudio.mute=!audible;fountainAudio.mute=!audible;
            if(audible&&ambientAudio.clip&&!ambientAudio.isPlaying)ambientAudio.Play();
            if(audible&&fountainVisible&&fountainAudio.clip&&!fountainAudio.isPlaying)fountainAudio.Play();
            if(!fountainVisible&&fountainAudio.isPlaying)fountainAudio.Stop();
        }
        void LateUpdate()
        {
            if(!Loaded||paused||!Visible)return;Resize();bool animate=MotionEnabled||Revealing;
            if(animate)clock+=Time.unscaledDeltaTime;
            UpdateEffects();effects.Tick(clock,animate);UpdateAudio();cameraView.enabled=animate||dirty;
            if(!cameraView.enabled)return;ApplyPose();
            var next=back;back=front;front=next;cameraView.targetTexture=front;surface.texture=front;
            foreach(var m in materials)m.SetTexture("_ReflectionTex",back);dirty=false;
        }
        public void RenderForReview(float time,bool fountainCloseup=false){clock=time;Resize();if(fountainCloseup&&Area==1){cameraView.orthographicSize*=.34f;var p=cameraView.transform.localPosition;p.y=-2.98f;cameraView.transform.localPosition=p;}ApplyPose();UpdateEffects();var next=back;back=front;front=next;cameraView.targetTexture=front;surface.texture=front;foreach(var m in materials)m.SetTexture("_ReflectionTex",back);cameraView.Render();}
        void OnApplicationPause(bool value){paused=value;if(cameraView)cameraView.enabled=!value;if(revealAudio){if(value){revealAudio.Pause();ambientAudio.Pause();fountainAudio.Pause();}else{revealAudio.UnPause();dirty=true;}}}
        void OnEnable(){if(stage)stage.gameObject.SetActive(Visible);dirty=true;}
        void OnDisable(){epoch++;currentTask=-1;if(stage)stage.gameObject.SetActive(false);if(revealAudio){revealAudio.Stop();ambientAudio.Stop();fountainAudio.Stop();}}
        void ReleaseTargets(){if(cameraView)cameraView.targetTexture=null;if(surface)surface.texture=null;foreach(var t in new[]{front,back})if(t){t.Release();Destroy(t);}front=back=null;}
        void Clear()
        {
            epoch++;currentTask=-1;fountainVisible=false;
            if(stage){stage.gameObject.SetActive(false);Destroy(stage.gameObject);}stage=null;effects?.Dispose();effects=null;
            foreach(var m in ownedMeshes)if(m)Destroy(m);ownedMeshes.Clear();
            if(materials!=null)foreach(var m in materials)if(m)Destroy(m);if(flagMaterial)Destroy(flagMaterial);
            ReleaseTargets();nodes=null;pose=null;meshes=null;textures=null;materials=null;data=null;flags=null;flagMesh=null;layers.Clear();
        }
        void OnDestroy(){Clear();}
    }
}
