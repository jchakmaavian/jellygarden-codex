using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Presentation
{
    public sealed class RoyalGameplayPresentation : MonoBehaviour
    {
        CanvasGroup group; RectTransform content,king; Image kingImage;Text moves,level,goals;
        SpriteRenderer backdrop; Sprite idle,cheer,think;float excitement,lastRefresh;bool built;
        readonly HashSet<Square> styled=new HashSet<Square>();
        readonly List<CanvasGroup> hidden=new List<CanvasGroup>();
        readonly List<string> goalParts=new List<string>();float nextHudUpdate;
        Font display;
        Sprite tileSprite;
        readonly List<Image> goalIcons=new List<Image>();readonly List<Text> goalNumbers=new List<Text>();
        static Color Navy=>new Color(.025f,.12f,.32f);
        static Color Gold=>new Color(.96f,.74f,.22f);
        RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RoyalPanelGraphic Panel(string name,Transform parent,Color a,Color b,float x0,float y0,float x1,float y1,float radius=25)
        {var r=Rect(name,parent,x0,y0,x1,y1);var g=r.gameObject.AddComponent<RoyalPanelGraphic>();g.topColor=a;g.bottomColor=b;g.cornerRadius=radius;g.raycastTarget=false;return g;}
        Text Label(string name,Transform parent,string text,int size,float x0,float y0,float x1,float y1)
        {var r=Rect(name,parent,x0,y0,x1,y1);var t=r.gameObject.AddComponent<Text>();t.font=display;t.fontSize=size;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.96f,.8f);t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=size;var s=r.gameObject.AddComponent<Shadow>();s.effectColor=Navy;s.effectDistance=new Vector2(0,-2);return t;}
        void Awake(){Build();}
        void OnEnable(){RoyalPowerupDirector.Reaction+=React;}
        void OnDisable(){RoyalPowerupDirector.Reaction-=React;}
        void React(float value){excitement=Mathf.Max(excitement,value);}
        void Build()
        {
            if(built)return;built=true;
            display=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var font=Resources.Load<Font>("RoyalGameplay/Display");if(font)display=font;
            var menus=GameObject.Find("CanvasGlobal");if(menus)RoyalMenuSkin.Apply(menus.transform,display);
            idle=RoyalPieceSkin.Art("king-idle");cheer=RoyalPieceSkin.Art("king-cheer");think=RoyalPieceSkin.Art("king-think");
            var root=Rect("Royal gameplay HUD",transform,0,0,1,1);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();group=root.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            content=Rect("Safe area",root,0,0,1,1);
            Panel("Header gold edge",content,Gold,new Color(.66f,.36f,.08f),0,.824f,1,1,0);
            Panel("Header royal blue",content,new Color(.08f,.32f,.65f),Navy,0,.83f,1,1,0);
            Panel("Goal inset",content,new Color(.03f,.17f,.4f),Navy,.025f,.845f,.34f,.962f,20);
            Label("Goal heading",content,"GOALS",23,.03f,.926f,.33f,.957f);
            goals=Label("Live goals",content,"",32,.025f,.853f,.34f,.925f);
            goals.enabled=false;
            for(int i=0;i<3;i++){
                float left=.035f+i*.098f;var r=Rect("Goal icon "+i,content,left,.891f,left+.087f,.927f);var icon=r.gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;goalIcons.Add(icon);
                var number=Label("Goal number "+i,content,"",26,left,.853f,left+.087f,.893f);number.resizeTextMinSize=10;number.horizontalOverflow=HorizontalWrapMode.Overflow;goalNumbers.Add(number);
            }
            Panel("Moves gold",content,Gold,Gold,.68f,.845f,.9f,.963f,22);
            Panel("Moves blue",content,new Color(.14f,.43f,.78f),Navy,.687f,.85f,.893f,.96f,19);
            Label("Moves heading",content,"MOVES",22,.69f,.928f,.89f,.96f);moves=Label("Moves remaining",content,"24",54,.68f,.849f,.9f,.934f);
            king=Rect("King reactions",content,.35f,.833f,.675f,.99f);kingImage=king.gameObject.AddComponent<Image>();kingImage.sprite=idle;kingImage.preserveAspect=true;kingImage.raycastTarget=false;
            var pause=Rect("Pause game",content,.909f,.917f,.994f,.97f);var image=pause.gameObject.AddComponent<Image>();image.color=Navy;var button=pause.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{MenuManager.Instance.ShowMenuPause(true);});RoyalMenuSkin.Surface(pause,true);Label("Pause icon",pause,"Ⅱ",28,0,0,1,1);
            level=Label("Level",content,"LEVEL 1",22,.35f,.795f,.68f,.828f);
            Panel("Booster rail",content,new Color(.045f,.24f,.53f),Navy,0,0,1,.119f,0);
            Panel("Rail gold",content,Gold,Gold,0,.116f,1,.12f,0);
            var lm=GetComponent<LevelManager>();
            if(lm)
            {
                var preplay=lm.GameField.parent.Find("Canvas/PrePlay");
                if(preplay){var banner=preplay.Find("Banner");if(banner){RoyalMenuSkin.Surface(banner);RoyalMenuSkin.Label(banner,"Royal heading","YOUR GOALS",0,88,560,54,32);}preplay.gameObject.AddComponent<RoyalMenuPresenter>();}
                foreach(string path in new[]{"Canvas/Panel","Canvas/BottomPanel"})
                {var old=lm.GameField.parent.Find(path);if(old){var cg=old.gameObject.GetComponent<CanvasGroup>();if(!cg)cg=old.gameObject.AddComponent<CanvasGroup>();if(cg){cg.alpha=0;cg.blocksRaycasts=false;hidden.Add(cg);}}}
                var back=lm.GameField.parent.Find("CanvasBack");if(back)back.gameObject.SetActive(false);
                string[] boostNames={"BoostExtraMoves","BoostHand","BoostRandom_color","BoostBomb"};
                string[] artNames={"crown","hammer","orb","tnt"};
                for(int i=0;i<4;i++)
                {
                    var boost=lm.GameField.parent.Find("Canvas/BottomPanel/Panel/"+boostNames[i]) as RectTransform;
                    if(!boost)continue;boost.SetParent(content,false);boost.anchorMin=new Vector2(.06f+i*.235f,.009f);boost.anchorMax=new Vector2(.235f+i*.235f,.087f);boost.offsetMin=boost.offsetMax=Vector2.zero;boost.localScale=Vector3.one;
                    var graphic=boost.GetComponent<Image>();if(graphic){graphic.sprite=RoyalPieceSkin.Art(artNames[i]);graphic.preserveAspect=true;graphic.color=Color.white;}
                    foreach(var text in boost.GetComponentsInChildren<Text>(true)){text.font=display;text.color=new Color(1,.96f,.8f);}
                    RoyalMenuSkin.Boost(boost,true);
                }
            }
            // Retain the real boost buttons and their inventory/payment behaviour.
            var background=new GameObject("Royal gameplay backdrop");background.transform.SetParent(transform);backdrop=background.AddComponent<SpriteRenderer>();backdrop.sortingOrder=-50;
            var tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();tileSprite=Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);backdrop.sprite=tileSprite;backdrop.color=new Color(.06f,.24f,.47f);
            var castle=RoyalPieceSkin.Art("home-castle");if(castle){backdrop.sprite=castle;backdrop.color=new Color(.28f,.4f,.59f);}
            group.alpha=0;group.blocksRaycasts=false;
        }
        void LateUpdate()
        {
            var lm=LevelManager.THIS;if(!lm)return;
            bool show=lm.gameStatus!=GameState.Map&&lm.gameStatus!=GameState.WaitForPopup&&lm.GameField.gameObject.activeInHierarchy;
            group.alpha=show?1:0;group.blocksRaycasts=show;backdrop.enabled=show;
            if(lm.gameStatus==GameState.Pause||lm.gameStatus==GameState.GameOver||lm.gameStatus==GameState.Win||lm.gameStatus==GameState.PreFailed){group.alpha=0;group.blocksRaycasts=false;}
            if(!show)return;
            foreach(var old in hidden)if(old){old.alpha=0;old.blocksRaycasts=false;}
            var safe=Screen.safeArea;content.anchorMin=safe.min/new Vector2(Screen.width,Screen.height);content.anchorMax=safe.max/new Vector2(Screen.width,Screen.height);
            if(Time.unscaledTime>=nextHudUpdate)
            {
                nextHudUpdate=Time.unscaledTime+.1f;moves.text=Mathf.Max(0,lm.Limit).ToString();level.text="LEVEL "+lm.currentLevel;
                for(int i=0;i<goalIcons.Count;i++){
                    bool active=lm.targetObject!=null&&i<lm.targetObject.Length;goalIcons[i].gameObject.SetActive(active);goalNumbers[i].gameObject.SetActive(active);if(!active)continue;
                    var target=lm.targetObject[i];goalIcons[i].sprite=RoyalVisualCatalog.Goal(target);goalNumbers[i].text=Mathf.Max(0,target.GetCount()).ToString();
                }
            }
            excitement=Mathf.MoveTowards(excitement,0,Time.deltaTime*.8f);
            kingImage.sprite=excitement>.5f?cheer:lm.Limit<=5?think:idle;
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            king.localRotation=Quaternion.Euler(0,0,motion?Mathf.Sin(Time.time*5)*Mathf.Min(excitement,1)*3:0);king.localScale=Vector3.one*(1+(motion?Mathf.Sin(Time.time*4)*.006f+Mathf.Sin(excitement*Mathf.PI)*.022f:0));
            var cam=Camera.main;if(cam){backdrop.transform.position=cam.ViewportToWorldPoint(new Vector3(.5f,.5f,18));var bounds=backdrop.sprite.bounds.size;float scale=Mathf.Max(cam.orthographicSize*2*cam.aspect/bounds.x,cam.orthographicSize*2/bounds.y);backdrop.transform.localScale=Vector3.one*scale;}
            if(Time.unscaledTime>lastRefresh){lastRefresh=Time.unscaledTime+.3f;StyleBoard(lm);}
        }
        void StyleBoard(LevelManager lm)
        {
            if(lm.squaresArray!=null)foreach(var sq in lm.squaresArray)
            {
                if(!sq||!styled.Add(sq))continue;
                var sr=sq.GetComponent<SpriteRenderer>();if(sr){var size=sr.sprite.bounds.size;sr.sprite=tileSprite;sr.drawMode=SpriteDrawMode.Sliced;sr.size=size;sr.color=(sq.row+sq.col)%2==0?new Color(.23f,.59f,.9f):new Color(.16f,.48f,.8f);}
                foreach(Transform tr in sq.transform)if(tr.name.ToLowerInvariant().Contains("outline")){var edge=tr.GetComponent<SpriteRenderer>();if(edge)edge.color=Gold;}
            }
        }
    }
}
