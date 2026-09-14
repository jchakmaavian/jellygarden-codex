using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>A single reusable, non-blocking reward banner with a fixed sparkle pool.</summary>
    public sealed class RoyalCelebration : MonoBehaviour
    {
        static RoyalCelebration instance;
        const int SparkleCount = 32;
        sealed class Sparkle { public Image image; public RectTransform rect; public Vector2 origin, velocity; public float spin, size; }
        readonly Sparkle[] sparkles = new Sparkle[SparkleCount];
        CanvasGroup group;
        RectTransform root, safe, banner, glow;
        Image halo;
        Text title;
        Sprite sparkleSprite, glowSprite;
        float age = 10, nextAllowed;
        bool reduced;
        public bool IsShowing => age < 1.25f;
        public int PoolSize => SparkleCount;
        public int Presentations { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        public static bool Play(int achievement)
        {
            var level = LevelManager.THIS;
            if (!level || level.gameStatus != GameState.Playing || !JuiceConfig.Instance.EnableJuice) return false;
            if (!instance) instance = level.gameObject.AddComponent<RoyalCelebration>();
            return instance.Present(achievement);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = size;
            return rect;
        }
        static Sprite Shape(Texture2D texture) => Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(.5f,.5f), texture.width);

        void Awake()
        {
            instance = this;
            root = Rect("Royal celebration - no input blocking", transform, Vector2.zero);
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 24;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720,1280); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false; group.alpha = 0;
            safe = Rect("Safe area", root, Vector2.zero); safe.offsetMin = safe.offsetMax = Vector2.zero;
            banner = Rect("Celebration", safe, new Vector2(650,150)); banner.anchorMin = banner.anchorMax = new Vector2(.5f,.735f);
            sparkleSprite = Shape(SparkleTextureFactory.FourPointSparkle); glowSprite = Shape(SparkleTextureFactory.SoftGlow);
            glow = Rect("Warm halo", banner, new Vector2(550,145)); halo = glow.gameObject.AddComponent<Image>(); halo.sprite = glowSprite; halo.raycastTarget = false;
            for (int i=0;i<SparkleCount;i++)
            {
                var rect = Rect("Celebration sparkle " + i, banner, Vector2.one*12); var image = rect.gameObject.AddComponent<Image>();
                image.sprite = sparkleSprite; image.raycastTarget = false;
                sparkles[i] = new Sparkle { image=image, rect=rect };
            }
            var textRect = Rect("Achievement", banner, new Vector2(620,112));
            title = textRect.gameObject.AddComponent<Text>(); title.font = Resources.Load<Font>("RoyalGameplay/Display") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 78; title.resizeTextForBestFit = true; title.resizeTextMinSize=42; title.resizeTextMaxSize=78;
            title.alignment=TextAnchor.MiddleCenter; title.color=new Color(1,.92f,.58f); title.raycastTarget=false;
            var shadow=textRect.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.06f,.13f,.32f,.95f); shadow.effectDistance=new Vector2(0,-6);
            var outline=textRect.gameObject.AddComponent<Outline>(); outline.effectColor=new Color(.12f,.20f,.41f); outline.effectDistance=new Vector2(2.5f,-2.5f);
            textRect.gameObject.AddComponent<RoyalCelebrationGradient>();
        }

        bool Present(int achievement)
        {
            if (Time.unscaledTime < nextAllowed) return false;
            nextAllowed = Time.unscaledTime + 2.4f; age=0; Presentations++;
            reduced=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)==0;
            title.text=achievement>=6?"BRILLIANT!":achievement>=5?"FANTASTIC!":achievement>=4?"BRAVO!":"GREAT!";
            // Deterministic decoration never consumes the board's random sequence.
            for (int i=0;i<SparkleCount;i++)
            {
                var spark=sparkles[i]; float a=(i+.3f)*Mathf.PI*2/SparkleCount;
                float speed=95+(i*47%140); spark.origin=new Vector2(Mathf.Cos(a)*220,Mathf.Sin(a)*48);
                spark.velocity=new Vector2(Mathf.Cos(a)*speed,Mathf.Sin(a)*speed*.7f+35);
                spark.spin=(i%2==0?1:-1)*(55+i*11); spark.size=10+i%4*5;
                spark.image.enabled=!reduced||i%4==0;
            }
            RoyalAudioDirector.Get?.Play(RoyalCue.Celebrate);
            Render(); return true;
        }

        public void Cancel() { age=10; if(group)group.alpha=0; }
        void Update()
        {
            var level=LevelManager.THIS;
            if(!level) { Cancel(); return; }
            if(level.gameStatus==GameState.Pause) { group.alpha=0; return; }
            if(level.gameStatus!=GameState.Playing) { Cancel(); return; }
            if(!IsShowing) { group.alpha=0; return; }
            age+=Time.deltaTime; Render();
        }
        void Render()
        {
            var area=Screen.safeArea;
            safe.anchorMin=area.min/new Vector2(Screen.width,Screen.height); safe.anchorMax=area.max/new Vector2(Screen.width,Screen.height);
            safe.offsetMin=safe.offsetMax=Vector2.zero;
            float entrance=Mathf.Clamp01(age/.18f), exit=Mathf.Clamp01((age-.93f)/.32f);
            group.alpha=(reduced?entrance:1)* (1-exit);
            float spring=1+Mathf.Sin(entrance*Mathf.PI)*.12f;
            banner.localScale=Vector3.one*(reduced?1:Mathf.Lerp(.65f,spring,1-Mathf.Pow(1-entrance,3)));
            banner.anchoredPosition=new Vector2(0,reduced?0:Mathf.Lerp(-12,9,entrance)+exit*22);
            banner.localRotation=Quaternion.Euler(0,0,reduced?0:Mathf.Sin(entrance*Mathf.PI)*-2);
            halo.color=new Color(1,.75f,.23f,(reduced?.13f:.24f)*(1-exit)); glow.localScale=Vector3.one*(1+entrance*.14f);
            for(int i=0;i<SparkleCount;i++)
            {
                var spark=sparkles[i]; if(!spark.image.enabled)continue;
                float t=Mathf.Max(0,age-.055f), amount=reduced?.25f:1;
                spark.rect.anchoredPosition=spark.origin+spark.velocity*t*amount+Vector2.down*(95*t*t*amount);
                spark.rect.localRotation=Quaternion.Euler(0,0,spark.spin*t*amount);
                float pulse=.6f+.4f*Mathf.Sin(t*12+i); spark.rect.sizeDelta=Vector2.one*spark.size*pulse;
                spark.image.color=i%3==0?new Color(.65f,.88f,1,1-exit):new Color(1,.9f,.55f,1-exit);
            }
        }
        void OnDestroy()
        {
            if(instance==this)instance=null;
            if(root)Destroy(root.gameObject);
            if(sparkleSprite)Destroy(sparkleSprite); if(glowSprite)Destroy(glowSprite);
        }
    }

    // Keep the lettering crisp at every mobile size; no raster word atlas is needed.
    public sealed class RoyalCelebrationGradient : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vertices)
        {
            if(!IsActive()||vertices.currentVertCount==0)return;
            var vertex=new UIVertex(); float min=float.PositiveInfinity,max=float.NegativeInfinity;
            for(int i=0;i<vertices.currentVertCount;i++){vertices.PopulateUIVertex(ref vertex,i);min=Mathf.Min(min,vertex.position.y);max=Mathf.Max(max,vertex.position.y);}
            for(int i=0;i<vertices.currentVertCount;i++)
            {
                vertices.PopulateUIVertex(ref vertex,i);
                // Outline/shadow vertices retain their dark colour.
                if(vertex.color.r>180&&vertex.color.g>150)vertex.color=Color.Lerp(new Color(1,.70f,.17f),new Color(1,1,.9f),Mathf.InverseLerp(min,max,vertex.position.y));
                vertices.SetUIVertex(vertex,i);
            }
        }
    }
}
