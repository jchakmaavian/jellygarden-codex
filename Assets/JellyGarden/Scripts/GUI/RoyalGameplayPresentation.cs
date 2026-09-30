using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System.Collections;
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
        SpriteRenderer backdrop; Sprite idle,cheer,think;float excitement,lastRefresh;bool built,ready;
        float hudFade,backdropFade;
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
        RectTransform goalArea;
        Transform framedField;Square[] framedSquares;Vector2 framedScreen;Rect framedSafeArea;
        Transform prePlay;Square[] styledSquares;float framedSize;Vector3 framedCamera;
        static Color Navy=>new Color(.025f,.12f,.32f);
        static readonly Color BackdropNavy=new Color(.047f,.208f,.49f);
        // The bottom bar's round buttons in canvas reference units (720x1280): the gameplay reference's size and spacing,
        // held constant across aspect ratios.
        const float BoostSize=RoyalGameplayHud.ButtonSize,BoostStride=RoyalGameplayHud.ButtonStride;
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
        // Under the startup loading screen this build runs in frames of its own after the scene's first frame
        // (RoyalTitleScreen.QueueStartupWork); otherwise all at once, here, as before. LateUpdate waits until it is complete.
        void Awake(){var steps=Build();if(!RoyalTitleScreen.QueueStartupWork(this,steps))while(steps.MoveNext()){}}
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
        // Each yield only marks a frame boundary for the startup loading screen; run straight through, it is the same build.
        IEnumerator Build()
        {
            if(built)yield break;built=true;
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
            header.SetAsLastSibling();
            // The top row follows the gameplay reference (Design/GameplayHud): the Goal panel, the king's arched portrait and
            // the Moves panel over the royal-blue backdrop, drawn by RoyalGameplayHud. The goals, the number and the king are
            // the live objects placed in them below.
            var goalPanel=RoyalGameplayHud.BuildPanel(header,"Royal HUD goal panel",-RoyalGameplayHud.PanelX,"Goal",display);goalPanel.heading.name="Goal heading";
            // The goals sit in the cream body, a little below its centre as in the reference; LayoutGoals places them.
            goalArea=Rect("Goal contents",goalPanel.body,0,0,1,1);goalArea.offsetMin=new Vector2(5,4);goalArea.offsetMax=new Vector2(-5,-18);
            for(int i=0;i<4;i++){
                var icon=Rect("Goal icon "+i,goalArea,0,.4f,1,.94f).gameObject.AddComponent<Image>();
                icon.preserveAspect=true;icon.raycastTarget=false;goalIcons.Add(icon);
                var number=Label("Goal number "+i,goalArea,"",30,0,0,1,.42f);number.resizeTextMinSize=11;number.color=Color.white;number.GetComponent<Shadow>().enabled=false;goalNumbers.Add(number);
                RoyalGameplayHud.Style(number,new Color(.09f,.24f,.53f),new Color(.045f,.14f,.36f),Color.clear,0,Color.clear,0,new Color(1,1,1,.35f),new Vector2(0,-1.2f));
            }
            var movesPanel=RoyalGameplayHud.BuildPanel(header,"Royal HUD moves panel",RoyalGameplayHud.PanelX,"Moves",display);
            movesHeading=movesPanel.heading;movesHeading.name="Moves heading";
            // The number is the panel's strongest text: dark blue, its capitals just above the body's centre.
            moves=Label("Moves remaining",movesPanel.body,"24",74,0,0,1,1);moves.color=Color.white;moves.GetComponent<Shadow>().enabled=false;
            moves.rectTransform.offsetMin=new Vector2(4,10.8f);moves.rectTransform.offsetMax=new Vector2(-4,10.8f);
            RoyalGameplayHud.Style(moves,new Color(.09f,.24f,.53f),new Color(.045f,.14f,.36f),Color.clear,0,Color.clear,0,new Color(1,1,1,.35f),new Vector2(0,-1.6f));
            var portrait=RoyalGameplayHud.BuildPortrait(header);
            king=Rect("King reactions",portrait.slot,.5f,.5f,.5f,.5f);king.sizeDelta=new Vector2(196,217);king.anchoredPosition=new Vector2(0,5);
            kingImage=king.gameObject.AddComponent<Image>();kingImage.sprite=idle;kingImage.preserveAspect=true;kingImage.raycastTarget=false;
            yield return null;   // the king's rig below is the build's largest single piece: a frame of its own
            yield return null;
            // Keep the live level label available to existing diagnostics, without adding
            // a heading that is absent from the source gameplay hierarchy.
            level=Label("Level",header,"",22,0,0,0,0);level.gameObject.SetActive(false);
            // One rounded bar of five evenly spaced round buttons: the four tools and settings (RoyalGameplayHud).
            var rail=Fixed("Imported booster rail",content,new Vector2(.5f,0),new Vector2(0,RoyalGameplayHud.RailY),new Vector2(720,RoyalGameplayHud.BarHeight));
            RoyalGameplayHud.BuildBar(rail);
            var pause=Fixed("Pause game",rail,new Vector2(.5f,.5f),new Vector2(2*BoostStride,0),new Vector2(BoostSize,BoostSize));
            var pauseFace=Rect("Royal HUD settings face",pause,0,0,1,1);RoyalGameplayHud.BuildButton(pauseFace,true);
            Pair("Settings gear",pauseFace,"Game-settings_button_gear",.22f,.22f,.78f,.78f);
            // Pause only while the player is actually playing: not during the win celebration, the fail offer or over
            // another popup (the legacy pause button had the same Playing check).
            var image=pause.gameObject.AddComponent<Image>();image.color=Color.clear;var button=pause.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
            pause.gameObject.AddComponent<RoyalHudPress>().parts=new[]{pauseFace};
            button.onClick.AddListener(()=>{var state=LevelManager.THIS;if(state&&state.gameStatus==GameState.Playing&&!MenuManager.Instance.BlocksBoardInput)MenuManager.Instance.ShowMenuPause(true);});
            var tools=gameObject.AddComponent<RoyalThrone.ReferenceGameplay.ReferenceBoosterBar>();
            tools.Attach(content,display);
            var lm=GetComponent<LevelManager>();
            if(lm)
            {
                var preplay=lm.GameField.parent.Find("Canvas/PrePlay");prePlay=preplay;
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
                    boost.anchoredPosition=new Vector2((i-2)*BoostStride,0);
                    boost.localScale=Vector3.one;
                    var graphic=boost.GetComponent<Image>();if(graphic)graphic.color=Color.clear;
                    var face=Rect("Imported booster face",boost,0,0,1,1);face.SetAsFirstSibling();
                    RoyalGameplayHud.BuildButton(face,false);
                    var art=Rect("Existing booster action",face,.16f,.16f,.84f,.84f).gameObject.AddComponent<Image>();
                    art.sprite=RoyalThrone.ReferenceGameplay.ReferenceBoosterBar.Art(i);art.preserveAspect=true;art.raycastTarget=false;
                    foreach(var text in boost.GetComponentsInChildren<Text>(true)){text.font=display;text.color=new Color(1,.96f,.8f);}
                    tools.Bind(boost.GetComponent<BoostIcon>(),i);railTools.Add(boost.gameObject);
                    // The badges in the reference's style, over the ones Bind placed: the red count, and the green + once used up.
                    var indicator=boost.Find("Indicator");var plate=indicator?indicator.Find("Image") as RectTransform:null;var plus=indicator?indicator.Find("Plus") as RectTransform:null;
                    if(indicator)RoyalMenuSkin.Place(indicator,0,0,BoostSize,BoostSize);
                    foreach(var part in new[]{plate,plus})if(part)foreach(var old in part.GetComponentsInChildren<Image>(true))if(old.name.StartsWith("Native "))old.enabled=false;
                    if(plate)RoyalGameplayHud.BuildBadge(plate,false);if(plus)RoyalGameplayHud.BuildBadge(plus,true);
                    var boostIcon=boost.GetComponent<BoostIcon>();if(boostIcon&&boostIcon.boostCount)RoyalGameplayHud.StyleCount(boostIcon.boostCount,display);
                    var press=boost.GetComponent<Button>();if(press)press.transition=Selectable.Transition.None;
                    boost.gameObject.AddComponent<RoyalHudPress>().parts=indicator?new[]{face,(RectTransform)indicator}:new[]{face};
                }
            }
            // Retain the real boost buttons and their inventory/payment behaviour.
            var background=new GameObject("Royal gameplay backdrop");background.transform.SetParent(transform);backdrop=background.AddComponent<SpriteRenderer>();backdrop.sortingOrder=-50;
            var tex=new Texture2D(1,1);tex.SetPixel(0,0,Color.white);tex.Apply();tileSprite=Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1,0,SpriteMeshType.FullRect);
            // A clean royal-blue gradient behind the board (the gameplay reference): no scenery.
            backdrop.sprite=RoyalGameplayHud.Backdrop();backdrop.color=Color.white;
            group.alpha=0;group.blocksRaycasts=false;
            ready=true;
        }
        void LateUpdate()
        {
            var lm=LevelManager.THIS;if(!lm||!ready)return;
            // The board appears with the HUD, the backdrop and its styled cells from the moment it exists, its entrance slide
            // (WaitForPopup) included; only the goals card, which this overlay would cover, keeps them back.
            bool show=lm.GameField&&lm.gameStatus!=GameState.Map&&lm.GameField.gameObject.activeInHierarchy&&!(prePlay&&prePlay.gameObject.activeInHierarchy);
            if(lm.gameStatus==GameState.Map){framedField=null;framedSquares=null;}
            // The HUD and the backdrop fade over 0.25 s (UI polish, 2026-09-30): they popped in at once after the goals card, and the
            // HUD vanished at once when the pause menu took effect. Taps follow the state at once; the camera's navy stays behind.
            bool hudShown=show&&!(lm.gameStatus==GameState.Pause||lm.gameStatus==GameState.GameOver||lm.gameStatus==GameState.Win||lm.gameStatus==GameState.PreFailed);
            hudFade=Mathf.MoveTowards(hudFade,hudShown?1:0,Time.unscaledDeltaTime/.25f);backdropFade=Mathf.MoveTowards(backdropFade,show?1:0,Time.unscaledDeltaTime/.25f);
            group.alpha=hudFade;group.blocksRaycasts=hudShown;
            backdrop.enabled=backdropFade>0;if(backdrop.color.a!=backdropFade)backdrop.color=new Color(1,1,1,backdropFade);
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
                int shown=lm.targetObject==null?0:Mathf.Min(goalIcons.Count,lm.targetObject.Length);
                for(int i=0;i<goalIcons.Count;i++){goalIcons[i].gameObject.SetActive(i<shown);goalNumbers[i].gameObject.SetActive(i<shown);}
                if(shown>0)LayoutGoals(lm,shown);
            }
            excitement=Mathf.MoveTowards(excitement,0,Time.deltaTime*.8f);
            kingImage.sprite=excitement>.5f?cheer:lm.Limit<=5?think:idle;
            bool motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1)!=0;
            king.localRotation=Quaternion.Euler(0,0,motion?Mathf.Sin(Time.time*5)*Mathf.Min(excitement,1)*3:0);king.localScale=Vector3.one*(1+(motion?Mathf.Sin(Time.time*4)*.006f+Mathf.Sin(excitement*Mathf.PI)*.022f:0));
            var cam=Camera.main;if(cam){if(cam.backgroundColor!=BackdropNavy)cam.backgroundColor=BackdropNavy;FrameBoard(lm,cam);backdrop.transform.position=cam.ViewportToWorldPoint(new Vector3(.5f,.5f,18));var bounds=backdrop.sprite.bounds.size;backdrop.transform.localScale=new Vector3(cam.orthographicSize*2*cam.aspect/bounds.x,cam.orthographicSize*2/bounds.y,1);}
            // A new board is styled in its first frame; after that every 0.3 s.
            if(lm.squaresArray!=styledSquares){styledSquares=lm.squaresArray;lastRefresh=0;}
            if(Time.unscaledTime>lastRefresh){lastRefresh=Time.unscaledTime+.3f;StyleBoard(lm);}
        }
        // The Goals card: one column per goal. The icons share one square size, larger when there are fewer goals (the score
        // star's art fills only 80% x 75% of its square, so its box is larger); the numbers share one font size that fits the
        // longest; the icon-over-number group is centred in the panel, like the Moves number beside it.
        void LayoutGoals(LevelManager lm,int shown)
        {
            float width=goalArea.rect.width,height=goalArea.rect.height,pitch=width/shown;
            float size=Mathf.Min(shown==1?70:shown==2?64:shown==3?50:40,pitch-8);
            int font=shown<=2?30:27;
            for(int i=0;i<shown;i++){
                var target=lm.targetObject[i];var number=goalNumbers[i];
                goalIcons[i].sprite=RoyalVisualCatalog.Goal(target,true);number.text=Mathf.Max(0,target.GetCount()).ToString();
                // Best fit stays off so every number shares one size; a box shorter than the font's line height would then
                // truncate the text to nothing, so the numbers overflow vertically.
                number.resizeTextForBestFit=false;number.horizontalOverflow=HorizontalWrapMode.Overflow;number.verticalOverflow=VerticalWrapMode.Overflow;number.fontSize=30;
                float at30=number.preferredWidth;
                while(font>16&&at30*font/30f>pitch-6)font--;
            }
            float textHeight=font*1.1f,gap=13,group=size+gap+textHeight,bottom=(height-group)*.5f;   // the digits' visual height
            var star=RoyalPieceSkin.Art("star");
            for(int i=0;i<shown;i++){
                float x=(i+.5f)*pitch;var icon=goalIcons[i];
                float box=icon.sprite&&icon.sprite==star?size*1.2f:size;
                Place(icon.rectTransform,x,bottom+textHeight+gap+size*.5f,box,box);
                goalNumbers[i].fontSize=font;Place(goalNumbers[i].rectTransform,x,bottom+textHeight*.5f,pitch-4,textHeight);
            }
        }
        static void Place(RectTransform r,float x,float y,float w,float h)
        {r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        public static Rect PlayableViewport(Rect safeNormalized)
        {
            float scale=Mathf.Min(Screen.width/720f,Screen.height/1280f);
            float safeHeight=Mathf.Max(1,Screen.height*safeNormalized.height);
            // Below the top row (its lower edge 269 units down) and above the bar (170 units up), with small gaps; the board's
            // frame keeps the reference's 3.9% side margins.
            float bottom=179*scale/safeHeight,top=280*scale/safeHeight;
            return UnityEngine.Rect.MinMaxRect(Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.039f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,bottom),
                Mathf.Lerp(safeNormalized.xMin,safeNormalized.xMax,.961f),Mathf.Lerp(safeNormalized.yMin,safeNormalized.yMax,1-top));
        }
        public static float FitSize(Bounds bounds,float aspect,Rect viewport)
        {return Mathf.Max(bounds.extents.x/(Mathf.Max(.01f,aspect)*viewport.width),bounds.extents.y/viewport.height);}
        void FrameBoard(LevelManager lm,Camera cam)
        {
            var screen=new Vector2(Screen.width,Screen.height);var safe=Screen.safeArea;
            if(framedField==lm.GameField&&framedSquares==lm.squaresArray&&framedScreen==screen&&framedSafeArea==safe)
            {
                // The legacy AspectCamera sets its own zoom once the pieces exist, which is during the entrance slide: keep this
                // board's framing (nothing else in the game moves or zooms the camera during a level).
                if(cam.orthographicSize!=framedSize||cam.transform.position!=framedCamera){cam.orthographicSize=framedSize;cam.transform.position=framedCamera;}
                return;
            }
            if(lm.squaresArray==null||lm.squaresArray.Length==0)return;
            // During the entrance slide (LevelManager.AnimateField moves the field in from the right to x = -maxCols/2.75),
            // frame for the resting place, so the board slides into the finished screen instead of the view jumping as it lands.
            var entrance=lm.GameField.GetComponent<Animation>();
            float slide=entrance&&entrance.isPlaying?-lm.maxCols/2.75f-lm.GameField.localPosition.x:0;
            bool hasBounds=false;var bounds=new Bounds();
            foreach(var sq in lm.squaresArray){if(!sq||sq.type==SquareTypes.NONE)continue;
                var cell=new Bounds(sq.transform.position+new Vector3(slide,0,0),new Vector3(lm.squareWidth,lm.squareHeight,0));
                if(!hasBounds){bounds=cell;hasBounds=true;}else bounds.Encapsulate(cell);
            }
            if(!hasBounds)return;
            // Room for the gold frame, a tenth of a cell outside the cells on every side (RoyalBoardSurface).
            bounds.Expand(new Vector3(lm.squareWidth*.2f,lm.squareHeight*.2f,0));
            bool ingredients=false;if(lm.targetObject!=null)foreach(var target in lm.targetObject)if(target.type==Target.INGREDIENT)ingredients=true;
            // The key exits' arrows hang to about 0.86 of a cell below the bottom row (0.1 of it is the frame's, already in the
            // bounds): keep the whole of them above the bar.
            if(ingredients)bounds.Encapsulate(new Vector3(bounds.center.x,bounds.min.y-lm.squareHeight*.8f,bounds.center.z));
            var viewport=PlayableViewport(new Rect(safe.position/screen,safe.size/screen));
            float size=FitSize(bounds,cam.aspect,viewport);cam.orthographicSize=size;
            Vector2 offset=(viewport.center-new Vector2(.5f,.5f))*new Vector2(size*2*cam.aspect,size*2);
            cam.transform.position=new Vector3(bounds.center.x-offset.x,bounds.center.y-offset.y,cam.transform.position.z);
            framedField=lm.GameField;framedSquares=lm.squaresArray;framedScreen=screen;framedSafeArea=safe;framedSize=size;framedCamera=cam.transform.position;styled.Clear();
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
