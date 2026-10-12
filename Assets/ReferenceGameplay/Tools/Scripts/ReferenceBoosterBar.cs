using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using RoyalThrone.Testing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RoyalThrone.ReferenceGameplay
{
    // Native icon artwork + verified packaged tool descriptions. The APK's IL2CPP
    // implementation is not imported: effects are dispatched through this game's cells.
    [DefaultExecutionOrder(-50)]
    public sealed class ReferenceBoosterBar : MonoBehaviour
    {
        public static ReferenceBoosterBar Instance {get;private set;}
        public static readonly BoostType[] Types={BoostType.RoyalHammer,BoostType.RoyalArrow,BoostType.RoyalCannon,BoostType.RoyalJester};
        static readonly int[] Icons={16599,21610,21411,21543};
        static readonly string[] Titles={"Royal Hammer","Arrow","Cannon","Jester Hat"};
        public static bool IsTool(BoostType type)=>Array.IndexOf(Types,type)>=0;
        public static bool OwnsInput=>Instance&&(Instance.Selected>=0||Instance.Busy);
        // Levels 1-5 keep the in-level tools padlocked, as the reference build does (Design/RoyalMatchReference/OBSERVATIONS.md),
        // keyed to King Leo's level number (D8: no reference campaign). The QA practice board, which tests the tools, stays unlocked.
        public const int OpeningLevels=5;
        public static bool Locked=>LevelManager.THIS&&LevelManager.THIS.currentLevel>=1&&LevelManager.THIS.currentLevel<=OpeningLevels&&!QaPractice;
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
        static bool QaPractice=>RoyalQaService.Instance&&RoyalQaService.Instance.Practising;
#else
        static bool QaPractice=>false;   // the QA service exists only in editor and development builds
#endif
        public int Selected {get;private set;}=-1;
        public bool Busy {get;private set;}
        // D5: nothing is for sale, so there is no tool purchase panel. Kept for callers that ask.
        public bool ShopOpen=>false;
        // Back that cancelled a chosen tool this frame; LevelManager then does not also open the pause menu.
        static int backConsumedFrame=-1;
        public static bool BackConsumedThisFrame=>backConsumedFrame==Time.frameCount;
        public int LastHitCount {get;private set;}
        public string Status {get;private set;}="";
        RectTransform hud,prompt;Text message;
        RectTransform rail;CanvasGroup railInput;Vector2 railRest;bool railMoving,railBlocks,railInteractable;float railDrop;
        Font font;readonly List<BoostIcon> buttons=new List<BoostIcon>();
        // UI polish (2026-09-30): the chosen tool eases up to 1.06x over 0.12 s with a soft gold halo behind it, and a tap on a
        // padlocked tool says when tools unlock, in the tool prompt, for about two seconds.
        readonly float[] chosen=new float[4];readonly RoyalThrone.Presentation.RoyalCardShape[] halos=new RoyalThrone.Presentation.RoyalCardShape[4];float lockedHintUntil;
        readonly Dictionary<BoostIcon,GameObject> padlocks=new Dictionary<BoostIcon,GameObject>();static Sprite padlockArt,discArt;
        ReferenceHammerSequence hammer;ReferenceArcherSequence archer;ReferenceCannonSequence cannon;Square[] currentBoard;Coroutine operation;bool shuffled;
        // The King cheers the tools (2026-10-04): ToolUsed when a tool is committed, ToolStruck at its first hit (the
        // Jester's when the shuffled pieces settle). Presentation only; the avatar listens on AvatarEvents (D2).
        public static event Action<int> ToolUsed,ToolStruck;
        int usingTool=-1;bool struck;
        static void Notify(Action<int> handlers,int tool,bool hit)
        {
            try{handlers?.Invoke(tool);}catch(Exception e){Debug.LogException(e);}
            try{if(hit)AvatarEvents.NotifyToolStruck(tool);else AvatarEvents.NotifyToolUsed(tool);}catch(Exception e){Debug.LogException(e);}
        }
        LevelManager LM=>LevelManager.THIS;
        public static Sprite Art(int index)=>Resources.Load<Sprite>("ReferenceGameplay/Tools/"+Icons[index]);
        static Sprite Sprite(int id)=>Resources.Load<Sprite>("ReferenceGameplay/Tools/"+id);
        void Awake(){Instance=this;}
        void OnDestroy(){if(Instance==this)Instance=null;}
        public void Attach(RectTransform parent,Font display){hud=parent;font=display;rail=parent.Find("Imported booster rail") as RectTransform;BuildPrompt();}
        void PresentCannon(float elapsed)
        {
            if(!rail)return;
            if(elapsed<0){
                if(railMoving){rail.anchoredPosition=railRest;if(railInput){railInput.blocksRaycasts=railBlocks;railInput.interactable=railInteractable;}}
                railMoving=false;return;
            }
            if(!railMoving){
                railMoving=true;railRest=rail.anchoredPosition;
                railInput=rail.GetComponent<CanvasGroup>();if(!railInput)railInput=rail.gameObject.AddComponent<CanvasGroup>();
                railBlocks=railInput.blocksRaycasts;railInteractable=railInput.interactable;
                railInput.blocksRaycasts=false;railInput.interactable=false;
            }
            // Measure against the actual screen bottom, including the safe-area inset.
            // Recalculate while running so a resolution/safe-area change cannot expose the bar.
            var canvas=rail.GetComponentInParent<Canvas>();var cam=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(hud,Vector2.zero,cam,out var screenBottom))
                railDrop=Mathf.Max(0,hud.rect.yMin+railRest.y+rail.rect.yMax-screenBottom.y)+16;
            else railDrop=railRest.y+rail.rect.height+32;
            float hidden=elapsed<ReferenceCannonSequence.RecoilEnd
                ?Mathf.SmoothStep(0,1,elapsed/.22f)
                :1-Mathf.SmoothStep(0,1,(elapsed-ReferenceCannonSequence.RecoilEnd)/ReferenceCannonSequence.BarReturn);
            rail.anchoredPosition=railRest+Vector2.down*(railDrop*hidden);
        }
        static RectTransform Box(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static Image Picture(Transform parent,string name,Sprite sprite,float x,float y,float w,float h,bool slice=false)
        {
            var r=Box(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;
            im.type=slice?Image.Type.Sliced:Image.Type.Simple;im.pixelsPerUnitMultiplier=1.5f;return im;
        }
        static void Halves(Transform parent,int id,float w,float h,bool slice=false)
        {
            Picture(parent,"Native left",Sprite(id),-w/4,0,w/2,h,slice);
            Picture(parent,"Native right",Sprite(id),w/4,0,w/2,h,slice).rectTransform.localScale=new Vector3(-1,1,1);
        }
        Text Label(Transform parent,string text,float x,float y,float w,float h,int size=26)
        {
            var t=Box("Label",parent,x,y,w,h).gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;
            t.color=new Color(1,.97f,.84f);t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=14;t.resizeTextMaxSize=size;return t;
        }
        Button Clickable(RectTransform r,Action click)
        {
            var im=r.gameObject.AddComponent<Image>();im.color=Color.clear;var b=r.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>click());return b;
        }
        public void Bind(BoostIcon icon,int index)
        {
            icon.type=Types[index];buttons.Add(icon);
            var indicator=icon.transform.Find("Indicator");
            RoyalGameplayHud.Place(indicator,0,0,114,114);
            var plate=indicator.Find("Image");RoyalGameplayHud.Place(plate,36,-31,34,34);
            foreach(var g in plate.GetComponentsInChildren<Graphic>(true))if(!(g is Text))g.enabled=false;
            Halves(plate,16397,34,34);
            // The old stock Text belongs to the legacy HUD and carries its layout/mask
            // configuration. Bind a clean label to the same stock updater inside this badge.
            if(icon.boostCount)icon.boostCount.enabled=false;
            var count=Label(plate,"0",0,0,34,34,23);count.color=Color.white;icon.boostCount=count;

            var plus=indicator.Find("Plus");RoyalGameplayHud.Place(plus,36,-31,34,34);
            foreach(var g in plus.GetComponentsInChildren<Graphic>(true))g.enabled=false;
            var hit=plus.GetComponent<Image>();if(hit){hit.enabled=true;hit.color=Color.clear;}
            Halves(plus,16891,34,34);var symbol=Box("Native plus",plus,0,0,21,21);Halves(symbol,21167,21,21);
            // The opening's padlock: a green disc over the tool art with a gold padlock on it, drawn here (no image asset).
            var cover=new GameObject("Opening padlock",typeof(RectTransform)).GetComponent<RectTransform>();cover.SetParent(icon.transform,false);
            cover.anchorMin=new Vector2(.14f,.14f);cover.anchorMax=new Vector2(.86f,.86f);cover.offsetMin=cover.offsetMax=Vector2.zero;
            var disc=cover.gameObject.AddComponent<Image>();disc.sprite=discArt?discArt:(discArt=Disc());disc.raycastTarget=false;disc.preserveAspect=true;
            var lockImage=Box("Padlock",cover,0,2,0,0);lockImage.anchorMin=new Vector2(.2f,.18f);lockImage.anchorMax=new Vector2(.8f,.84f);lockImage.offsetMin=lockImage.offsetMax=Vector2.zero;
            var pad=lockImage.gameObject.AddComponent<Image>();pad.sprite=padlockArt?padlockArt:(padlockArt=Padlock());pad.raycastTarget=false;pad.preserveAspect=true;
            cover.gameObject.SetActive(false);padlocks[icon]=cover.gameObject;padlocksShown=null;
        }
        // The padlocks change only when the lock state does. While locked, the stock badge stays hidden every frame:
        // LevelManager.UnLockBoosts (after each move) calls BoostIcon.UnLockBoost, which shows the Indicator again.
        bool? padlocksShown;
        void ShowPadlocks()
        {
            bool locked=Locked;
            if(padlocksShown!=locked)
            {
                padlocksShown=locked;if(locked)CancelSelection();
                foreach(var pair in padlocks)
                {
                    if(!pair.Key||!pair.Value)continue;
                    pair.Value.SetActive(locked);if(locked)pair.Value.transform.SetAsLastSibling();
                    var badge=pair.Key.transform.Find("Indicator");if(badge)badge.gameObject.SetActive(!locked);
                }
            }
            else if(locked)
                foreach(var pair in padlocks)
                {
                    if(!pair.Key)continue;
                    var badge=pair.Key.transform.Find("Indicator");if(badge&&badge.gameObject.activeSelf)badge.gameObject.SetActive(false);
                }
        }
        // A green disc with a soft darker rim, like the tool buttons' own face.
        static Sprite Disc()
        {
            const int N=96;var t=new Texture2D(N,N,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var c=new Color[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                float dx=(x+.5f)/N*2-1,dy=(y+.5f)/N*2-1,r=Mathf.Sqrt(dx*dx+dy*dy);
                var col=Color.Lerp(new Color(.36f,.72f,.26f),new Color(.14f,.46f,.14f),Mathf.Clamp01(.5f-dy*.5f));
                col=Color.Lerp(col,new Color(.09f,.32f,.10f),Mathf.Clamp01((r-.82f)/.14f));
                col.a=Mathf.Clamp01((1-r)*N*.5f);c[y*N+x]=col;
            }
            t.SetPixels(c);t.Apply();return UnityEngine.Sprite.Create(t,new Rect(0,0,N,N),new Vector2(.5f,.5f),N);
        }
        // A gold padlock: a rounded body with a keyhole under a shackle, with a dark outline.
        static Sprite Padlock()
        {
            const int N=128;var t=new Texture2D(N,N,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};var c=new Color[N*N];
            Color gold=new Color(1f,.84f,.30f),deep=new Color(.84f,.56f,.12f),edge=new Color(.36f,.20f,.04f),hole=new Color(.30f,.16f,.04f);
            for(int y=0;y<N;y++)for(int x=0;x<N;x++)
            {
                float px=x+.5f,py=y+.5f;
                // body: a rounded rectangle 26..102 x 14..74
                float bx=Mathf.Max(Mathf.Abs(px-64)-38+12,0),by=Mathf.Max(Mathf.Abs(py-44)-30+12,0),body=Mathf.Sqrt(bx*bx+by*by)-12;
                // shackle: a half ring (outer 30, inner 19) over the body, with its legs down into it
                float sx=px-64,sy=Mathf.Max(py-80,0),ring=Mathf.Abs(Mathf.Sqrt(sx*sx+sy*sy)-24.5f)-5.5f;
                if(py<70)ring=1e3f;
                float d=Mathf.Min(body,ring);
                Color col;float a=Mathf.Clamp01(1.5f-d);
                if(d>-2.5f)col=edge;
                else if(body<=d+.01f){col=Color.Lerp(deep,gold,Mathf.Clamp01((py-14)/60f));float k=Mathf.Sqrt((px-64)*(px-64)+(py-50)*(py-50));if(k<8||(Mathf.Abs(px-64)<3.5f&&py>28&&py<50))col=hole;}
                else col=Color.Lerp(deep,gold,.55f+.45f*Mathf.Clamp01(-sx/30f));
                col.a=a;c[y*N+x]=col;
            }
            t.SetPixels(c);t.Apply();return UnityEngine.Sprite.Create(t,new Rect(0,0,N,N),new Vector2(.5f,.5f),N);
        }
        void BuildPrompt()
        {
            prompt=Box("Tool selection",hud,0,0,580,108);prompt.anchorMin=prompt.anchorMax=new Vector2(.5f,1);prompt.anchoredPosition=new Vector2(0,-328);
            Picture(prompt,"Instruction plate",Sprite(16286),0,0,580,108,true);
            message=Label(prompt,"",-25,0,485,90,24);message.color=new Color(.025f,.12f,.3f);
            var close=Box("Cancel tool",prompt,252,0,48,48);Halves(close,21805,48,48);Clickable(close,CancelSelection);prompt.gameObject.SetActive(false);
        }
        // D5: the count StoryFreeGame grants, read and written through StoryBoosters (RoyalSave, keyed by BoostType name).
        public int Stock(int index)=>JellyGarden.Scripts.Story.StoryBoosters.Owned(Types[index]);
        void SetStock(int index,int count)=>JellyGarden.Scripts.Story.StoryBoosters.SetOwned(Types[index],count);
        public void Choose(BoostType type)
        {
            int index=Array.IndexOf(Types,type);if(index<0||Busy||!LM||LM.gameStatus!=GameState.Playing)return;
            if(Locked){LockedHint(index);return;}
            if(Selected==index){CancelSelection();return;}
            CancelSelection();if(Stock(index)<=0){EmptyHint(index);return;}
            Selected=index;currentBoard=LM.squaresArray;AI.THIS?.CancelVisibleHint();
            message.text=Titles[index]+"\n"+(index==3?"Tap Shuffle to rearrange the board.":index==0?"Tap an object to clear it.":index==1?"Tap a cell in the row to clear.":"Tap a cell in the column to clear.");
            prompt.gameObject.SetActive(true);prompt.SetAsLastSibling();
            if(index==3){message.text="Jester Hat — tap here to Shuffle";var b=prompt.GetComponent<Button>();if(!b){b=prompt.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>{if(Selected==3)TryUse(null);});}var im=prompt.GetComponent<Image>();if(!im)im=prompt.gameObject.AddComponent<Image>();im.color=Color.clear;im.raycastTarget=true;}
        }
        public void CancelSelection(){Selected=-1;lockedHintUntil=0;if(prompt)prompt.gameObject.SetActive(false);}
        // Tools open on the level after the opening ones (OpeningLevels).
        void LockedHint(int index)=>Hint(index,"Tools unlock at Level "+(OpeningLevels+1));
        // D5: a tool with no stock is not offered for sale; the prompt says how more are earned.
        void EmptyHint(int index)=>Hint(index,"None left. Win levels to earn more.");
        void Hint(int index,string line)
        {
            if(!prompt||!message)return;CancelSelection();
            message.text=Titles[index]+"\n"+line;
            prompt.gameObject.SetActive(true);prompt.SetAsLastSibling();lockedHintUntil=Time.unscaledTime+2.2f;
            if(SoundBase.Instance)SoundBase.Instance.PlaySound(SoundBase.Instance.click);
        }
        // The halo behind a tool's face: made the first time it is needed, sized to the button.
        RoyalThrone.Presentation.RoyalCardShape Halo(BoostIcon b,int index)
        {
            if(halos[index]||!b)return halos[index];
            var go=new GameObject("Royal selected halo",typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(b.transform,false);r.SetAsFirstSibling();
            float d=((RectTransform)b.transform).rect.width+22;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(d,d);r.anchoredPosition=Vector2.zero;
            var halo=go.AddComponent<RoyalThrone.Presentation.RoyalCardShape>();halo.raycastTarget=false;halo.radius=d*.5f;halo.band=8;halo.softness=7;
            halo.from=new Color(1,.97f,.78f);halo.to=new Color(1,.8f,.3f);halo.color=new Color(1,1,1,0);
            return halos[index]=halo;
        }
        // BoostShop.SetBoost routes the tools here. D5: there is no purchase panel (StoryFreeGame.ShopsAllowed); the tap explains instead.
        public void OpenShop(BoostType type)
        {
            int index=Array.IndexOf(Types,type);if(index<0||Busy)return;
            if(Locked)LockedHint(index);else if(Stock(index)<=0)EmptyHint(index);else Choose(type);
        }
        bool Stable()=>LM&&LM.gameStatus==GameState.Playing&&!LM.IsResolvingMatches&&LM.InputSwaps==0&&!RoyalPowerupDirector.Busy&&
            LM.GetItems().Where(i=>i).All(i=>!i.falling&&!i.destroying)&&!MenuManager.Instance.BlocksBoardInput;
        void Update()
        {
            if(!LM)return;
            ShowPadlocks();
            if(LM.gameStatus!=GameState.Playing||(currentBoard!=null&&currentBoard!=LM.squaresArray))
            {CancelSelection();if(Busy&&(LM.gameStatus==GameState.Map||LM.gameStatus==GameState.PrepareGame||currentBoard!=LM.squaresArray))CancelOperation();currentBoard=LM.squaresArray;return;}
            if(lockedHintUntil>0&&Time.unscaledTime>=lockedHintUntil){lockedHintUntil=0;if(Selected<0&&prompt)prompt.gameObject.SetActive(false);}
            // Eased once per tool per frame (a tool's button can be bound more than once), then applied to its buttons.
            for(int ix=0;ix<chosen.Length;ix++)chosen[ix]=Mathf.MoveTowards(chosen[ix],Selected==ix?1:0,Time.unscaledDeltaTime/.12f);
            foreach(var b in buttons)
            {
                if(!b)continue;int ix=Array.IndexOf(Types,b.type);if(ix<0)continue;
                float u=1-chosen[ix],e=1-u*u*u;
                b.transform.localScale=Vector3.one*(1+.06f*e);
                var halo=chosen[ix]>0||halos[ix]?Halo(b,ix):null;if(halo&&!Mathf.Approximately(halo.color.a,e))halo.color=new Color(1,1,1,e);
            }
            // Back first cancels a chosen tool, instead of also opening the pause menu (BackConsumedThisFrame).
            if(InputManager.Instance&&InputManager.Instance.Gameplay.Cancel.WasReleasedThisFrame()&&!BackConsumedThisFrame)
            {
                if(Selected>=0&&!Busy){CancelSelection();backConsumedFrame=Time.frameCount;return;}
            }
            if(Selected<0||Busy)return;
            if(MenuManager.Instance.BlocksBoardInput){CancelSelection();return;}
            if(InputManager.Instance&&InputManager.Instance.WasTouchPressed)
            {
                var data=new PointerEventData(EventSystem.current){position=InputManager.Instance.TouchPosition};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);if(hits.Count>0)return;
                Vector3 p=Camera.main.ScreenToWorldPoint(InputManager.Instance.TouchPosition);
                var sq=LM.squaresArray.FirstOrDefault(s=>s&&!s.IsNone()&&Mathf.Abs(s.transform.position.x-p.x)<LM.squareWidth*.5f&&Mathf.Abs(s.transform.position.y-p.y)<LM.squareHeight*.5f);
                if(sq)TryUse(sq);
            }
        }
        public bool TryUse(Square target)
        {
            if(Selected<0||Busy||!Stable()||Stock(Selected)<=0)return false;
            if(Selected!=3&&(!target||target.IsNone()||target.type==SquareTypes.UNDESTROYABLE||(target.item&&target.item.currentType==ItemsTypes.INGREDIENT&&target.block.Count==0)))return false;
            int type=Selected;var affected=type==3?new List<Square>():LM.squaresArray.Where(s=>s&&!s.IsNone()&&(type==0?s==target:type==1?s.row==target.row:s.col==target.col)).OrderBy(s=>type==2?-s.row:s.col).ToList();
            if(type==3&&Movable().Count<2)return false;
            if(type==0&&!ReferenceHammerSequence.Available){Status="Hammer animation assets are unavailable.";return false;}
            if(type==1&&!ReferenceArcherSequence.Available){Status="Archer animation assets are unavailable.";return false;}
            SetStock(type,Stock(type)-1);CancelSelection();Busy=true;LM.DragBlocked=true;currentBoard=LM.squaresArray;LastHitCount=0;
            usingTool=type;struck=false;Notify(ToolUsed,type,false);
            operation=StartCoroutine(Use(type,affected));return true;
        }
        List<Item> Movable()=>LM.GetItems().Where(i=>i&&!i.destroying&&!i.falling&&i.square&&i.square.CanGoOut()&&i.currentType!=ItemsTypes.INGREDIENT).ToList();
        IEnumerator Use(int type,List<Square> affected)
        {
            try
            {
                AI.THIS?.CancelVisibleHint();
                if(type==3){
                    shuffled=false;yield return Shuffle();
                    if(!shuffled){SetStock(type,Stock(type)+1);Status="No safe shuffle available; hat returned.";yield break;}
                    struck=true;Notify(ToolStruck,type,true);
                }
                else
                {
                    if(type==0){
                        if(!hammer)hammer=gameObject.AddComponent<ReferenceHammerSequence>();
                        var target=affected[0];float cell=LM.squareWidth*Mathf.Abs(LM.GameField.lossyScale.x);
                        yield return hammer.Play(target.transform.position,HammerOrigin(target.transform.position),cell,()=>HitCannonCell(target));
                    }else if(type==1){
                        if(!archer)archer=gameObject.AddComponent<ReferenceArcherSequence>();
                        yield return archer.Play(affected,HitCannonCell);
                    }else if(type==2&&ReferenceCannonSequence.Available){
                        if(!cannon)cannon=gameObject.AddComponent<ReferenceCannonSequence>();
                        yield return cannon.Play(affected,HitCannonCell,PresentCannon);
                    }else{
                        foreach(var s in affected){HitCell(s);if(type!=0)yield return new WaitForSeconds(.045f);}
                    }
                }
                yield return new WaitForSeconds(.12f);LM.FindMatches();
                float deadline=Time.realtimeSinceStartup+30;
                while((LM.IsResolvingMatches||RoyalPowerupDirector.Busy||LM.GetItems().Where(i=>i).Any(i=>i.falling||i.destroying))&&Time.realtimeSinceStartup<deadline)yield return null;
                Status="Completed "+Titles[type];
            }
            finally{Busy=false;usingTool=-1;if(LM&&!LM.IsResolvingMatches&&LM.InputSwaps==0)LM.DragBlocked=false;if(hammer)hammer.Cancel();if(archer)archer.Cancel();if(cannon)cannon.Cancel();operation=null;}
        }
        void HitCell(Square s)
        {
            if(!s||s.type==SquareTypes.UNDESTROYABLE)return;
            if(!struck&&usingTool>=0){struck=true;Notify(ToolStruck,usingTool,true);}
            if(s.item&&s.item.currentType!=ItemsTypes.INGREDIENT){s.item.royalBlastClear=true;s.item.DestroyItem(true);LastHitCount++;}
            else if(s.block.Count>0){s.DestroyBlock();LastHitCount++;}
        }
        void HitCannonCell(Square s)
        {
            // Share the existing immediate projectile-impact presentation. The ball has
            // already arrived, so the generic match anticipation would make the hit lag.
            if(s&&s.type!=SquareTypes.UNDESTROYABLE&&s.item&&s.item.currentType!=ItemsTypes.INGREDIENT)s.item.royalRocketClear=true;
            HitCell(s);
        }
        Vector3 HammerOrigin(Vector3 target)
        {
            var button=buttons.FirstOrDefault(b=>b&&b.type==BoostType.RoyalHammer);var camera=Camera.main;
            if(!button||!camera)return target+Vector3.down*LM.squareHeight*5;
            var canvas=button.GetComponentInParent<Canvas>();var uiCamera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            var screen=RectTransformUtility.WorldToScreenPoint(uiCamera,button.transform.position);
            return camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,Mathf.Abs(camera.transform.position.z-target.z)));
        }
        IEnumerator Shuffle()
        {
            var pieces=Movable();var cells=pieces.Select(i=>i.square).ToArray();var start=pieces.Select(i=>i.transform.position).ToArray();var order=Enumerable.Range(0,pieces.Count).ToArray();
            int minX=cells.Min(c=>c.col),maxX=cells.Max(c=>c.col),minY=cells.Min(c=>c.row),maxY=cells.Max(c=>c.row);
            for(int attempt=0;attempt<48;attempt++){
                // Symmetric permutations preserve a stable two-colour board where a
                // purely random shuffle is unlikely to avoid automatic matches.
                if(attempt<3){
                    bool fits=true;
                    for(int n=0;n<cells.Length;n++){
                        int x=attempt==1?cells[n].col:minX+maxX-cells[n].col;
                        int y=attempt==0?cells[n].row:minY+maxY-cells[n].row;
                        order[n]=Array.FindIndex(cells,c=>c.col==x&&c.row==y);if(order[n]<0)fits=false;
                    }
                    if(!fits){for(int n=0;n<order.Length;n++)order[n]=n;continue;}
                }else for(int n=order.Length-1;n>0;n--){int j=UnityEngine.Random.Range(0,n+1);int old=order[n];order[n]=order[j];order[j]=old;}
                for(int n=0;n<pieces.Count;n++){pieces[n].square=cells[order[n]];cells[order[n]].item=pieces[n];}
                if(order.Where((v,n)=>v!=n).Any()&&LM.GetMatches().Count==0&&RoyalHintSolver.Find(LM)!=null){shuffled=true;break;}
            }
            if(!shuffled){for(int n=0;n<pieces.Count;n++){pieces[n].square=cells[n];cells[n].item=pieces[n];}yield break;}
            for(float t=0;t<.5f;t+=Time.deltaTime){float u=Mathf.SmoothStep(0,1,t/.5f);for(int n=0;n<pieces.Count;n++)if(pieces[n])pieces[n].transform.position=Vector3.Lerp(start[n],pieces[n].square.transform.position,u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.18f;yield return null;}
            foreach(var p in pieces)if(p)p.transform.position=p.square.transform.position;LastHitCount=0;
        }
        void CancelOperation(){if(operation!=null)StopCoroutine(operation);operation=null;Busy=false;usingTool=-1;if(LM&&!LM.IsResolvingMatches&&LM.InputSwaps==0)LM.DragBlocked=false;if(hammer)hammer.Cancel();if(archer)archer.Cancel();if(cannon)cannon.Cancel();}
        void OnDisable(){CancelSelection();CancelOperation();}
    }
}
