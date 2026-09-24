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
        // Timed levels: the limit header reads "Time" (m:ss) instead of "Moves"; the four rebound rail tools stay
        // visible (the legacy code hides the old extra-moves booster on timed levels, which is now the Hammer slot).
        Text movesHeading;readonly List<GameObject> railTools=new List<GameObject>();
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
        const float BoostSize=114f,BoostStride=141f;
        static Color Gold=>new Color(.96f,.74f,.22f);
        RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        RoyalPanelGraphic Panel(string name,Transform parent,Color a,Color b,float x0,float y0,float x1,float y1,float radius=25)
        {var r=Rect(name,parent,x0,y0,x1,y1);var g=r.gameObject.AddComponent<RoyalPanelGraphic>();g.topColor=a;g.bottomColor=b;g.cornerRadius=radius;g.raycastTarget=false;return g;}
        Text Label(string name,Transform parent,string text,int size,float x0,float y0,float x1,float y1)
        {var r=Rect(name,parent,x0,y0,x1,y1);var t=r.gameObject.AddComponent<Text>();t.font=display;t.fontSize=size;t.text=text;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(1,.96f,.8f);t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=size;var s=r.gameObject.AddComponent<Shadow>();s.effectColor=Navy;s.effectDistance=new Vector2(0,-2);return t;}
        // APK level2 UI sprites are authored as mirrored halves. Preserve their source
        // artwork and slicing while adapting only anchoring to the existing overlay canvas.
        Image Source(string name, Transform parent, string key, float x0,float y0,float x1,float y1, bool sliced=false)
        {
            var r=Rect(name,parent,x0,y0,x1,y1);var im=r.gameObject.AddComponent<Image>();
            im.sprite=Resources.Load<Sprite>("ReferenceGameplay/Layout/"+key);im.raycastTarget=false;
            im.type=sliced?Image.Type.Sliced:Image.Type.Simple;im.pixelsPerUnitMultiplier=1.3f;return im;
        }
        void Pair(string name,Transform parent,string key,float x0,float y0,float x1,float y1,bool sliced=false)
        {
            float mid=(x0+x1)*.5f;
            Source(name+" left",parent,key,x0,y0,mid,y1,sliced);
            var right=Source(name+" right",parent,key,mid,y0,x1,y1,sliced);right.rectTransform.localScale=new Vector3(-1,1,1);
        }
        RectTransform Fixed(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var r=Rect(name,parent,anchor.x,anchor.y,anchor.x,anchor.y);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        void Awake(){Build();}
        void OnEnable(){RoyalPowerupDirector.Reaction+=React;}
        // A level interrupted by the OS (app sent to the background; on devices also focus loss, e.g. the notification
        // shade) comes back to the pause menu instead of straight into live play. Only while actually playing with no
        // popup open, so the win celebration, the fail offer and result cards are left alone.
        void OnApplicationPause(bool paused){if(paused)PauseForInterruption();}
        void OnApplicationFocus(bool focused){if(!focused&&!Application.isEditor)PauseForInterruption();}
        void PauseForInterruption()
        {
            var lm=LevelManager.THIS;
            if(lm&&lm.gameStatus==GameState.Playing&&MenuManager.Instance&&!MenuManager.Instance.BlocksBoardInput)MenuManager.Instance.ShowMenuPause(true);
        }
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
            var header=Fixed("Imported gameplay header",content,new Vector2(.5f,1),new Vector2(0,-135),new Vector2(720,270));
            var headerBack=Source("Packaged header background",content,"Game-top_background",0,1,1,1,true).rectTransform;
            headerBack.sizeDelta=new Vector2(0,270);headerBack.anchoredPosition=new Vector2(0,-135);
            header.SetAsLastSibling();
            Pair("Goal container",header,"Game-goal_panel_container",.027f,.07f,.315f,.85f,true);
            Pair("Goal banner",header,"Game-goal_panel_banner",.021f,.67f,.321f,.96f);
            Label("Goal heading",header,"Goals",28,.03f,.744f,.312f,.92f);
            var goalArea=Rect("Goal contents",header,.045f,.11f,.297f,.70f);
            for(int i=0;i<4;i++){
                var icon=Rect("Goal icon "+i,goalArea,0,.4f,1,.94f).gameObject.AddComponent<Image>();
                icon.preserveAspect=true;icon.raycastTarget=false;goalIcons.Add(icon);
                var number=Label("Goal number "+i,goalArea,"",30,0,0,1,.42f);number.resizeTextMinSize=11;number.color=Navy;number.GetComponent<Shadow>().enabled=false;goalNumbers.Add(number);
            }
            Pair("Moves container",header,"Game-goal_panel_container",.685f,.07f,.973f,.85f,true);
            Pair("Moves banner",header,"Game-goal_panel_banner",.679f,.67f,.979f,.96f);
            movesHeading=Label("Moves heading",header,"Moves",28,.685f,.744f,.973f,.92f);
            moves=Label("Moves remaining",header,"24",65,.707f,.16f,.955f,.68f);moves.color=Navy;moves.GetComponent<Shadow>().enabled=false;
            Source("Portrait background",header,"King-Frame",.348f,.08f,.652f,.865f);
            king=Rect("King reactions",header,.364f,.095f,.636f,.9f);kingImage=king.gameObject.AddComponent<Image>();kingImage.sprite=idle;kingImage.preserveAspect=true;kingImage.raycastTarget=false;
            Pair("Portrait frame",header,"King-Frame-Edge",.335f,.055f,.665f,.9f);
            // Keep the live level label available to existing diagnostics, without adding
            // a heading that is absent from the source gameplay hierarchy.
            level=Label("Level",header,"",22,0,0,0,0);level.gameObject.SetActive(false);
            var rail=Fixed("Imported booster rail",content,new Vector2(.5f,0),new Vector2(0,70),new Vector2(720,170));
            for(int i=0;i<5;i++){
                var socket=Fixed("Packaged rail slot "+i,rail,new Vector2(.5f,.5f),new Vector2((i-2)*BoostStride,0),new Vector2(BoostStride+1,170));
                Pair("Rail background",socket,"Game-bottom_background_empty",0,-.63f,1,.73f,true);
            }
            var pause=Fixed("Pause game",rail,new Vector2(.5f,.5f),new Vector2(2*BoostStride,5),new Vector2(114,114));
            Pair("Settings blue",pause,"Game-blue_circle_button",.07f,.07f,.93f,.93f);
            Pair("Settings frame",pause,"Game-booster_button_frame",0,0,1,1);
            Pair("Settings gear",pause,"Game-settings_button_gear",.22f,.22f,.78f,.78f);
            // Pause only while the player is actually playing: not during the win celebration, the fail offer or over
            // another popup (the legacy pause button had the same Playing check).
            var image=pause.gameObject.AddComponent<Image>();image.color=Color.clear;var button=pause.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{var state=LevelManager.THIS;if(state&&state.gameStatus==GameState.Playing&&!MenuManager.Instance.BlocksBoardInput)MenuManager.Instance.ShowMenuPause(true);});
            var tools=gameObject.AddComponent<RoyalThrone.ReferenceGameplay.ReferenceBoosterBar>();
            tools.Attach(content,display);
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
                    if(!boost)continue;boost.SetParent(rail,false);
                    boost.anchorMin=boost.anchorMax=new Vector2(.5f,.5f);
                    boost.sizeDelta=new Vector2(BoostSize,BoostSize);
                    boost.anchoredPosition=new Vector2((i-2)*BoostStride,5);
                    boost.localScale=Vector3.one;
                    var graphic=boost.GetComponent<Image>();if(graphic)graphic.color=Color.clear;
                    var face=Rect("Imported booster face",boost,0,0,1,1);face.SetAsFirstSibling();
                    Pair("Green button",face,"Game-booster_button",.07f,.07f,.93f,.93f);
                    Pair("Gold frame",face,"Game-booster_button_frame",0,0,1,1);
                    var art=Rect("Existing booster action",face,.21f,.21f,.79f,.79f).gameObject.AddComponent<Image>();
                    art.sprite=RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Art(i);art.preserveAspect=true;art.raycastTarget=false;
                    foreach(var text in boost.GetComponentsInChildren<Text>(true)){text.font=display;text.color=new Color(1,.96f,.8f);}
                    tools.Bind(boost.GetComponent<BoostIcon>(),i);railTools.Add(boost.gameObject);
                }
            }
            // Retain the real boost buttons and their inventory/payment behaviour.
            var background=new GameObject("Royal gameplay backdrop");background.transform.SetParent(transform);backdrop=background.AddComponent<SpriteRenderer>();backdrop.sortingOrder=-50;
            var tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();tileSprite=Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);backdrop.sprite=tileSprite;backdrop.color=new Color(.06f,.24f,.47f);
            // Packaged gameplay camera clear colour; do not reuse the unrelated homepage illustration.
            backdrop.color=new Color(.19215687f,.3019608f,.4745098f);
            group.alpha=0;group.blocksRaycasts=false;
        }
        void LateUpdate()
        {
            var lm=LevelManager.THIS;if(!lm)return;
            bool show=lm.GameField&&lm.gameStatus!=GameState.Map&&lm.gameStatus!=GameState.WaitForPopup&&lm.GameField.gameObject.activeInHierarchy;
            if(lm.gameStatus==GameState.Map){framedField=null;framedSquares=null;}
            group.alpha=show?1:0;group.blocksRaycasts=show;backdrop.enabled=show;
            if(lm.gameStatus==GameState.Pause||lm.gameStatus==GameState.GameOver||lm.gameStatus==GameState.Win||lm.gameStatus==GameState.PreFailed){group.alpha=0;group.blocksRaycasts=false;}
            // This overlay canvas sits above the popup canvas, so while any popup is open the HUD must not take its taps.
            if(MenuManager.Instance&&MenuManager.Instance.BlocksBoardInput)group.blocksRaycasts=false;
            if(!show)return;
            foreach(var old in hidden)if(old){old.alpha=0;old.blocksRaycasts=false;}
            var safe=Screen.safeArea;content.anchorMin=safe.min/new Vector2(Screen.width,Screen.height);content.anchorMax=safe.max/new Vector2(Screen.width,Screen.height);
            if(Time.unscaledTime>=nextHudUpdate)
            {
                nextHudUpdate=Time.unscaledTime+.1f;level.text="LEVEL "+lm.currentLevel;
                bool timed=lm.limitType==LIMIT.TIME;int limit=Mathf.Max(0,lm.Limit);
                if(movesHeading)movesHeading.text=timed?"Time":"Moves";
                moves.text=timed?limit/60+":"+(limit%60).ToString("00"):limit.ToString();
                foreach(var tool in railTools)if(tool&&!tool.activeSelf)tool.SetActive(true);
                for(int i=0;i<goalIcons.Count;i++){
                    bool active=lm.targetObject!=null&&i<lm.targetObject.Length;goalIcons[i].gameObject.SetActive(active);goalNumbers[i].gameObject.SetActive(active);if(!active)continue;
                    var target=lm.targetObject[i];goalIcons[i].sprite=RoyalVisualCatalog.Goal(target,true);goalNumbers[i].text=Mathf.Max(0,target.GetCount()).ToString();
                    float step=1f/Mathf.Min(goalIcons.Count,lm.targetObject.Length),left=i*step;
                    var ir=goalIcons[i].rectTransform;ir.anchorMin=new Vector2(left,.4f);ir.anchorMax=new Vector2(left+step,.94f);
                    var nr=goalNumbers[i].rectTransform;nr.anchorMin=new Vector2(left+.002f,0);nr.anchorMax=new Vector2(left+step-.002f,.42f);
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
            float scale=Mathf.Min(Screen.width/720f,Screen.height/1280f);
            float safeHeight=Mathf.Max(1,Screen.height*safeNormalized.height);
            float bottom=155*scale/safeHeight,top=285*scale/safeHeight;
            return UnityEngine.Rect.MinMaxRect(Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.025f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,bottom),
                Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.975f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,1-top));
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
