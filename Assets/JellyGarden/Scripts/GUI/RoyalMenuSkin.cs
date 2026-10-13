using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    // Original button objects, callbacks and numeric fields remain the economy's source of truth.
    public static class RoyalMenuSkin
    {
        public static readonly Color Navy=new Color(.025f,.10f,.27f),Cream=new Color(1,.965f,.83f);
        static Font font;
        public static RectTransform Place(Transform t,float x,float y,float w,float h)
        {
            if(!t)return null;var r=t as RectTransform;if(!r)return null;
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);
            r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;r.localRotation=Quaternion.identity;return r;
        }
        public static void Typography(Text t,int size=28)
        {
            if(!t)return;t.font=font?font:Resources.Load<Font>("RoyalGameplay/Display");t.color=Cream;
            t.fontSize=size;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=size;
            t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
            foreach(var outline in t.GetComponents<Outline>())outline.enabled=false;
            var shadow=t.GetComponent<Shadow>();if(!shadow)shadow=t.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(0,.04f,.14f,.55f);shadow.effectDistance=new Vector2(0,-2);
        }
        public static Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size=28)
        {
            var tr=p.Find(name);if(!tr){tr=new GameObject(name,typeof(RectTransform)).transform;tr.SetParent(p,false);}
            Place(tr,x,y,w,h);var t=tr.GetComponent<Text>();if(!t)t=tr.gameObject.AddComponent<Text>();Typography(t,size);t.text=value;return t;
        }
        public static Image Art(Transform p,string name,string art,float x,float y,float w,float h)
        {
            var tr=p.Find(name);if(!tr){tr=new GameObject(name,typeof(RectTransform)).transform;tr.SetParent(p,false);}
            Place(tr,x,y,w,h);var i=tr.GetComponent<Image>();if(!i)i=tr.gameObject.AddComponent<Image>();i.sprite=RoyalPieceSkin.Art(art);i.color=Color.white;
            // A missing sprite draws a plain white rectangle: hide it and name the art. (An empty name means the caller assigns the sprite.)
            i.enabled=i.sprite||string.IsNullOrEmpty(art);if(!i.enabled)Debug.LogWarning("Royal art missing: "+art);i.preserveAspect=true;i.raycastTarget=false;return i;
        }
        static RoyalPanelGraphic Layer(Transform p,string name,float inset,Color top,Color bottom,float radius)
        {
            var tr=new GameObject(name,typeof(RectTransform)).transform;tr.SetParent(p,false);
            var r=(RectTransform)tr;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;
            var g=tr.gameObject.AddComponent<RoyalPanelGraphic>();g.topColor=top;g.bottomColor=bottom;g.cornerRadius=radius;g.raycastTarget=false;return g;
        }
        public static void Surface(Transform p,bool button=false,bool inset=false)
        {
            if(!p||p.Find("Royal skin"))return;var image=p.GetComponent<Image>();if(image)image.color=Color.clear;
            var go=new GameObject("Royal skin",typeof(RectTransform));go.transform.SetParent(p,false);go.transform.SetAsFirstSibling();
            var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            Layer(r,"Depth",0,new Color(.27f,.14f,.04f),new Color(.12f,.08f,.055f),button?30:34);
            Layer(r,"Gold rim",3,new Color(1,.93f,.6f),new Color(.78f,.43f,.085f),button?28:32);
            Layer(r,"Inner edge",7,new Color(.09f,.30f,.65f),new Color(.025f,.10f,.29f),button?24:28);
            var face=Layer(r,"Enamel",10,inset?new Color(.065f,.24f,.48f):button?new Color(.035f,.52f,.49f):new Color(.07f,.30f,.64f),inset?Navy:button?new Color(.02f,.27f,.30f):new Color(.035f,.15f,.40f),button?21:25);
            var shine=Layer(r,"Top glint",14,new Color(.65f,.88f,1,.27f),new Color(.3f,.55f,1,0),18);
            var sr=(RectTransform)shine.transform;sr.anchorMin=new Vector2(0,.58f);sr.offsetMin=new Vector2(14,0);
            var b=p.GetComponent<Button>();if(b){b.targetGraphic=face;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,1,.94f);colors.pressedColor=new Color(.65f,.76f,.95f);colors.disabledColor=new Color(.5f,.55f,.62f);colors.fadeDuration=.08f;b.colors=colors;}
        }
        public static void Button(Transform t,string caption,float x,float y,float w,float h,int size=30)
        {
            if(!t)return;Place(t,x,y,w,h);Surface(t,true);foreach(var text in t.GetComponentsInChildren<Text>(true))text.enabled=false;
            var label=Label(t,"Royal caption",caption,0,1,w-28,h-15,size);label.enabled=true;
        }
        static void Field(Transform p,string name,float x,float y,float w,float h,int size)
        {var t=p.Find(name);if(!t)return;Place(t,x,y,w,h);Typography(t.GetComponent<Text>(),size);}
        static void HideVisual(Transform t)
        {if(!t)return;var cg=t.GetComponent<CanvasGroup>();if(!cg)cg=t.gameObject.AddComponent<CanvasGroup>();cg.alpha=0;cg.blocksRaycasts=false;}
        static void Base(Transform menu,Transform p,float h,string title)
        {
            foreach(var clamp in menu.GetComponentsInChildren<JellyGarden.Scripts.GUI.KeepUIInScreen>(true))clamp.enabled=false;
            // Some inherited popup roots carry a horizontal authoring offset (the lives and gems
            // shops sat 7 and 19 px left of centre), which reads as a crooked panel. Centre them.
            var root=menu as RectTransform;
            if(root&&Mathf.Abs(root.anchoredPosition.x)>0.01f)root.anchoredPosition=new Vector2(0,root.anchoredPosition.y);
            Place(p,0,0,620,h);Surface(p);Label(p,"Royal heading",title,p.Find("Close")?-24:0,h*.5f-65,430,70,36);Button(p.Find("Close"),"×",257,h*.5f-50,68,68,46);
            var backdrop=menu.GetComponent<Image>();if(backdrop)backdrop.color=new Color(.005f,.015f,.05f,.82f);HideVisual(menu.Find("Leadboard"));
        }
        // The Level Start card (MenuPlay) follows its reference layout: RoyalLevelCard draws it and gives it its own motion.
        static void LevelCard(Transform menu,Transform p){RoyalLevelCard.Build(menu,p);}
        // A goal column on the Level Start card. The presenter calls this, because the goals are created each time the card opens.
        public static void LevelCardGoal(JellyGarden.Scripts.Targets.TargetIcon target,int index,int count){RoyalLevelCard.Goal(target,index,count);}
        // The pause menu's leave question (RoyalCards builds it; AnimationManager shows and hides it by this name).
        public const string LeaveConfirmName="Royal leave confirm";
        public static void Apply(Transform root,Font display)
        {
            font=display;
            foreach(Transform menu in root)
            {
                var p=menu.Find("Image");if(!p||p.Find("Royal skin"))continue;
                foreach(var text in menu.GetComponentsInChildren<Text>(true))Typography(text,32);
                switch(menu.name)
                {
                    case "MenuPlay":LevelCard(menu,p);break;
                    // Every other card is built from the Level Start card's parts (RoyalCardKit) by RoyalCards.
                    case "GemsShop":RoyalCards.Coins(menu,p);break;
                    case "LiveShop":RoyalCards.Lives(menu,p);break;
                    case "MenuPause":case "Settings":RoyalCards.Pause(menu,p,menu.name=="MenuPause");break;
                    case "MenuComplete":case "MenuFailed":
                        bool win=menu.name=="MenuComplete";var royalKing=RoyalCards.Result(menu,p,win);
                        // The newly imported APK rig restarts its win or sad reaction each time its result card opens.
                        if(Application.isPlaying)RoyalThrone.Characters.KingVanguardSlot.Mount(royalKing,RoyalThrone.Characters.KingVanguardCharacter.Framing.Bust,win?RoyalThrone.Characters.KingVanguardSlot.Mode.HappyOnShow:RoyalThrone.Characters.KingVanguardSlot.Mode.SadOnShow);
                        break;
                    case "BoostShop":RoyalCards.Boosters(menu,p);break;
                    case "BoostInfo":RoyalCards.Guide(menu,p);break;
                    case "Reward":RoyalCards.Reward(menu,p);break;
                    case "Tutorial":RoyalCards.Lessons(menu,p);break;
                }
                if(p.Find("Royal skin"))menu.gameObject.AddComponent<RoyalMenuPresenter>();
            }
            var retry=root.Find("PreFailed");if(retry&&!retry.Find("Banner/Royal skin")){
                // The offer of extra moves, in the same card; its own clip (RoyalMenuPresenter.ConfigureRetryMotion) times it.
                RoyalCards.SoClose(retry);retry.gameObject.AddComponent<RoyalMenuPresenter>().ConfigureRetryMotion();
            }
        }
    }
}
