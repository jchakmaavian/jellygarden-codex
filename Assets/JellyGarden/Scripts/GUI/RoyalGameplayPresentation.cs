using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
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
        readonly Dictionary<string,Sprite> cellSprites=new Dictionary<string,Sprite>();
        Vector2 cellSpriteSize;
        readonly List<Image> goalIcons=new List<Image>();readonly List<Text> goalNumbers=new List<Text>();
        Transform framedField;Square[] framedSquares;Vector2 framedScreen;Rect framedSafeArea;
        static Color Navy=>new Color(.025f,.12f,.32f);
        // Booster rail metrics in canvas reference units (720x1280): the width and spacing the
        // fractional anchors produced on a phone, now held constant across aspect ratios.
        const float BoostSize=126f,BoostStride=169f;
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
            Panel("Header gold edge",content,Gold,new Color(.66f,.36f,.08f),0,.85f,1,1,0);
            Panel("Header royal blue",content,new Color(.08f,.32f,.65f),Navy,0,.855f,1,1,0);
            Panel("Goal gold",content,Gold,Gold,.025f,.866f,.395f,.982f,20);
            Panel("Goal ivory",content,new Color(1,.98f,.88f),new Color(.91f,.85f,.66f),.032f,.871f,.388f,.977f,17);
            Panel("Goal title bar",content,new Color(.075f,.32f,.64f),Navy,.033f,.944f,.388f,.977f,13);
            Label("Goal heading",content,"GOALS",22,.04f,.945f,.38f,.976f);
            goals=Label("Live goals",content,"",32,.025f,.871f,.395f,.944f);
            goals.enabled=false;
            for(int i=0;i<3;i++){
                float left=.043f+i*.114f;var r=Rect("Goal icon "+i,content,left,.910f,left+.106f,.944f);var icon=r.gameObject.AddComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;goalIcons.Add(icon);
                var number=Label("Goal number "+i,content,"",27,left,.873f,left+.106f,.911f);number.resizeTextMinSize=11;number.color=Navy;number.GetComponent<Shadow>().enabled=false;goalNumbers.Add(number);
            }
            Panel("Moves gold",content,Gold,Gold,.631f,.866f,.886f,.982f,20);
            Panel("Moves ivory",content,new Color(1,.98f,.88f),new Color(.91f,.85f,.66f),.638f,.871f,.879f,.977f,17);
            Panel("Moves title bar",content,new Color(.075f,.32f,.64f),Navy,.638f,.944f,.879f,.977f,13);
            Label("Moves heading",content,"MOVES",22,.64f,.945f,.877f,.976f);moves=Label("Moves remaining",content,"24",58,.642f,.874f,.875f,.947f);moves.color=Navy;moves.GetComponent<Shadow>().enabled=false;
            king=Rect("King reactions",content,.405f,.861f,.619f,.986f);kingImage=king.gameObject.AddComponent<Image>();kingImage.sprite=idle;kingImage.preserveAspect=true;kingImage.raycastTarget=false;
            var pause=Rect("Pause game",content,.901f,.933f,.99f,.985f);var image=pause.gameObject.AddComponent<Image>();image.color=Navy;var button=pause.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{MenuManager.Instance.ShowMenuPause(true);});RoyalMenuSkin.Surface(pause,true);Label("Pause icon",pause,"II",28,0,0,1,1);
            level=Label("Level",content,"LEVEL 1",22,.35f,.818f,.68f,.85f);
            Panel("Booster rail",content,new Color(.045f,.24f,.53f),Navy,0,0,1,.106f,0);
            Panel("Rail gold",content,Gold,Gold,0,.103f,1,.107f,0);
            var lm=GetComponent<LevelManager>();
            if(lm)
            {
                var preplay=lm.GameField.parent.Find("Canvas/PrePlay");
                if(preplay)preplay.gameObject.AddComponent<RoyalMenuPresenter>().ConfigurePrePlay();
                foreach(string path in new[]{"Canvas/Panel","Canvas/BottomPanel"})
                {var old=lm.GameField.parent.Find(path);if(old){var cg=old.gameObject.GetComponent<CanvasGroup>();if(!cg)cg=old.gameObject.AddComponent<CanvasGroup>();if(cg){cg.alpha=0;cg.blocksRaycasts=false;hidden.Add(cg);}}}
                var back=lm.GameField.parent.Find("CanvasBack");if(back)back.gameObject.SetActive(false);
                string[] boostNames={"BoostExtraMoves","BoostHand","BoostRandom_color","BoostBomb"};
                string[] artNames={"crown","hammer","orb","tnt"};
                for(int i=0;i<4;i++)
                {
                    var boost=lm.GameField.parent.Find("Canvas/BottomPanel/Panel/"+boostNames[i]) as RectTransform;
                    if(!boost)continue;boost.SetParent(content,false);
                    // Fractional anchors made each booster both wider and further from its
                    // neighbours as the screen widened, so the rail sprawled on a tablet. Pin the
                    // row to the bottom centre at the size it already had on the 720x1280
                    // reference, and it keeps that shape on every aspect.
                    boost.anchorMin=boost.anchorMax=new Vector2(.5f,.048f);
                    boost.sizeDelta=new Vector2(BoostSize,BoostSize*.8f);
                    boost.anchoredPosition=new Vector2((i-1.5f)*BoostStride,0);
                    boost.localScale=Vector3.one;
                    var graphic=boost.GetComponent<Image>();if(graphic){graphic.sprite=RoyalGameplayArt.Art(artNames[i])??RoyalPieceSkin.Art(artNames[i]);graphic.preserveAspect=true;graphic.color=Color.white;}
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
            bool show=lm.GameField&&lm.gameStatus!=GameState.Map&&lm.gameStatus!=GameState.WaitForPopup&&lm.GameField.gameObject.activeInHierarchy;
            if(lm.gameStatus==GameState.Map){framedField=null;framedSquares=null;}
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
                    var target=lm.targetObject[i];goalIcons[i].sprite=RoyalVisualCatalog.Goal(target,true);goalNumbers[i].text=Mathf.Max(0,target.GetCount()).ToString();
                    float step=.342f/Mathf.Min(3,lm.targetObject.Length),left=.04f+i*step;
                    var ir=goalIcons[i].rectTransform;ir.anchorMin=new Vector2(left,.909f);ir.anchorMax=new Vector2(left+step,.944f);
                    var nr=goalNumbers[i].rectTransform;nr.anchorMin=new Vector2(left+.002f,.873f);nr.anchorMax=new Vector2(left+step-.002f,.911f);
                }
            }
            excitement=Mathf.MoveTowards(excitement,0,Time.deltaTime*.8f);
            kingImage.sprite=excitement>.5f?cheer:lm.Limit<=5?think:idle;
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            king.localRotation=Quaternion.Euler(0,0,motion?Mathf.Sin(Time.time*5)*Mathf.Min(excitement,1)*3:0);king.localScale=Vector3.one*(1+(motion?Mathf.Sin(Time.time*4)*.006f+Mathf.Sin(excitement*Mathf.PI)*.022f:0));
            var cam=Camera.main;if(cam){FrameBoard(lm,cam);backdrop.transform.position=cam.ViewportToWorldPoint(new Vector3(.5f,.5f,18));var bounds=backdrop.sprite.bounds.size;float scale=Mathf.Max(cam.orthographicSize*2*cam.aspect/bounds.x,cam.orthographicSize*2/bounds.y);backdrop.transform.localScale=Vector3.one*scale;}
            if(Time.unscaledTime>lastRefresh){lastRefresh=Time.unscaledTime+.3f;StyleBoard(lm);}
        }
        public static Rect PlayableViewport(Rect safeNormalized)
        {
            return UnityEngine.Rect.MinMaxRect(Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.02f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,.145f),
                Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.98f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,.808f));
        }
        public static float FitSize(Bounds bounds,float aspect,Rect viewport)
        {return Mathf.Max(bounds.extents.x/(Mathf.Max(.01f,aspect)*viewport.width),bounds.extents.y/viewport.height);}
        void FrameBoard(LevelManager lm,Camera cam)
        {
            var screen=new Vector2(Screen.width,Screen.height);var safe=Screen.safeArea;
            if(framedField==lm.GameField&&framedSquares==lm.squaresArray&&framedScreen==screen&&framedSafeArea==safe)return;
            if(lm.squaresArray==null||lm.squaresArray.Length==0)return;
            var entrance=lm.GameField.GetComponent<Animation>();if(entrance&&entrance.isPlaying)return;
            bool hasBounds=false;var bounds=new Bounds();
            foreach(var sq in lm.squaresArray){if(!sq||sq.type==SquareTypes.NONE)continue;
                var cell=new Bounds(sq.transform.position,new Vector3(lm.squareWidth,lm.squareHeight,0));
                if(!hasBounds){bounds=cell;hasBounds=true;}else bounds.Encapsulate(cell);
            }
            if(!hasBounds)return;
            bounds.Expand(new Vector3(lm.squareWidth*.06f,lm.squareHeight*.06f,0));
            bool ingredients=false;if(lm.targetObject!=null)foreach(var target in lm.targetObject)if(target.type==Target.INGREDIENT)ingredients=true;
            if(ingredients)bounds.Encapsulate(new Vector3(bounds.center.x,bounds.min.y-lm.squareHeight*.45f,bounds.center.z));
            var viewport=PlayableViewport(new Rect(safe.position/screen,safe.size/screen));
            float size=FitSize(bounds,cam.aspect,viewport);cam.orthographicSize=size;
            Vector2 offset=(viewport.center-new Vector2(.5f,.5f))*new Vector2(size*2*cam.aspect,size*2);
            cam.transform.position=new Vector3(bounds.center.x-offset.x,bounds.center.y-offset.y,cam.transform.position.z);
            framedField=lm.GameField;framedSquares=lm.squaresArray;framedScreen=screen;framedSafeArea=safe;styled.Clear();
        }
        Sprite CellSprite(string key,Vector2 size)
        {
            if(cellSpriteSize!=size)
            {
                foreach(var sprite in cellSprites.Values)if(sprite)Destroy(sprite);
                cellSprites.Clear();styled.Clear();cellSpriteSize=size;
            }
            if(cellSprites.TryGetValue(key,out var cached)&&cached)return cached;
            var source=RoyalGameplayArt.Art(key)??tileSprite;
            if(!source)return null;
            // The pack's full 512px cells have no slicing borders and are 5.12
            // units at the catalog PPU. Give this renderer a native cell-sized
            // view instead of relying on Sliced to shrink an unsliced sprite.
            float ppu=source.rect.width/Mathf.Max(.001f,size.x);
            float border=Mathf.Min(1,Mathf.Min(source.rect.width,source.rect.height)*.25f);
            var pivot=source.pivot/new Vector2(source.rect.width,source.rect.height);
            var result=Sprite.Create(source.texture,source.rect,pivot,ppu,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));
            result.name="Royal cell - "+key;result.hideFlags=HideFlags.DontSave;cellSprites[key]=result;return result;
        }
        public void StyleBoard(LevelManager lm)
        {
            if(lm.GameField){var surface=lm.GameField.GetComponent<RoyalBoardSurface>();if(!surface)surface=lm.GameField.gameObject.AddComponent<RoyalBoardSurface>();surface.RebuildIfNeeded(lm);}
            if(lm.squaresArray!=null)foreach(var sq in lm.squaresArray)
            {
                if(!sq)continue;var sr=sq.GetComponent<SpriteRenderer>();if(!sr)continue;
                if(sq.type==SquareTypes.NONE){sr.enabled=false;styled.Remove(sq);continue;}
                var size=new Vector2(lm.squareWidth,lm.squareHeight);
                var sprite=CellSprite((sq.row+sq.col)%2==0?"cell-light":"cell-dark",size);
                if(!styled.Add(sq)&&sr.sprite==sprite)continue;
                sr.sprite=sprite;sr.drawMode=Mathf.Approximately(size.x,size.y)?SpriteDrawMode.Simple:SpriteDrawMode.Sliced;
                sr.size=size;sr.color=Color.white;sr.enabled=true;
            }
        }
        void OnDestroy(){foreach(var sprite in cellSprites.Values)if(sprite)Destroy(sprite);}
    }
}
