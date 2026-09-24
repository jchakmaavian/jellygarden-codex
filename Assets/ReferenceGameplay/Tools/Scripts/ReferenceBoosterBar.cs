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
        static readonly string[] Descriptions={"Clear one object without using a move.","Clear a row without using a move.","Clear a column without using a move.","Shuffle the movable pieces without using a move."};
        public static bool IsTool(BoostType type)=>Array.IndexOf(Types,type)>=0;
        public static bool OwnsInput=>Instance&&(Instance.Selected>=0||Instance.Busy||Instance.ShopOpen);
        public int Selected {get;private set;}=-1;
        public bool Busy {get;private set;}
        public bool ShopOpen=>shop&&shop.gameObject.activeSelf;
        public int LastHitCount {get;private set;}
        public string Status {get;private set;}="";
        RectTransform hud,shop,prompt;Text message,balance,price,title,body;Image shopIcon;
        Font font;int buying=-1;bool purchasePending;readonly List<BoostIcon> buttons=new List<BoostIcon>();
        SpriteRenderer hammer;Square[] currentBoard;Coroutine operation;bool shuffled;
        LevelManager LM=>LevelManager.THIS;
        public static Sprite Art(int index)=>Resources.Load<Sprite>("ReferenceGameplay/Tools/"+Icons[index]);
        static Sprite Sprite(int id)=>Resources.Load<Sprite>("ReferenceGameplay/Tools/"+id);
        void Awake(){Instance=this;}
        void OnDestroy(){if(Instance==this)Instance=null;}
        public void Attach(RectTransform parent,Font display){hud=parent;font=display;BuildPrompt();BuildShop();}
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
            RoyalMenuSkin.Place(indicator,0,0,114,114);
            var plate=indicator.Find("Image");RoyalMenuSkin.Place(plate,36,-31,34,34);
            foreach(var g in plate.GetComponentsInChildren<Graphic>(true))if(!(g is Text))g.enabled=false;
            Halves(plate,16397,34,34);
            // The old stock Text belongs to the legacy HUD and carries its layout/mask
            // configuration. Bind a clean label to the same stock updater inside this badge.
            if(icon.boostCount)icon.boostCount.enabled=false;
            var count=Label(plate,"0",0,0,34,34,23);count.color=Color.white;icon.boostCount=count;

            var plus=indicator.Find("Plus");RoyalMenuSkin.Place(plus,36,-31,34,34);
            foreach(var g in plus.GetComponentsInChildren<Graphic>(true))g.enabled=false;
            var hit=plus.GetComponent<Image>();if(hit){hit.enabled=true;hit.color=Color.clear;}
            Halves(plus,16891,34,34);var symbol=Box("Native plus",plus,0,0,21,21);Halves(symbol,21167,21,21);
        }
        void BuildPrompt()
        {
            prompt=Box("Tool selection",hud,0,0,580,108);prompt.anchorMin=prompt.anchorMax=new Vector2(.5f,1);prompt.anchoredPosition=new Vector2(0,-328);
            Picture(prompt,"Instruction plate",Sprite(16286),0,0,580,108,true);
            message=Label(prompt,"",-25,0,485,90,24);message.color=new Color(.025f,.12f,.3f);
            var close=Box("Cancel tool",prompt,252,0,48,48);Halves(close,21805,48,48);Clickable(close,CancelSelection);prompt.gameObject.SetActive(false);
        }
        void BuildShop()
        {
            shop=Box("Reference tool purchase",hud,0,0,0,0);shop.anchorMin=Vector2.zero;shop.anchorMax=Vector2.one;shop.sizeDelta=Vector2.zero;
            var dim=shop.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.72f);dim.raycastTarget=true;
            var panel=Box("Tool purchase panel",shop,0,0,600,670);var frameBody=Box("Panel body",panel,0,-42.5f,600,585);Halves(frameBody,16782,600,585,true);
            var banner=Box("Title banner",panel,0,257,600,112);
            Picture(banner,"Banner left",Resources.Load<Sprite>("ReferenceGameplay/Layout/Game-goal_panel_banner"),-150,0,300,112);
            Picture(banner,"Banner right",Resources.Load<Sprite>("ReferenceGameplay/Layout/Game-goal_panel_banner"),150,0,300,112).rectTransform.localScale=new Vector3(-1,1,1);
            title=Label(panel,"",0,265,460,70,38);
            var close=Box("Close purchase",panel,272,299,62,62);Halves(close,21805,62,62);Clickable(close,CloseShop);
            shopIcon=Picture(panel,"Tool artwork",null,0,103,155,155);shopIcon.preserveAspect=true;
            body=Label(panel,"",0,-30,500,95,27);
            var buy=Box("Buy tool pack",panel,0,-155,420,92);Halves(buy,21955,420,92,true);Clickable(buy,()=>Buy());
            price=Label(buy,"",-28,0,285,72,32);Picture(buy,"Coin",Sprite(21613),139,0,54,54);
            balance=Label(panel,"",0,-245,500,54,24);shop.gameObject.SetActive(false);
        }
        public int Stock(int index)=>RoyalSave.GetInt(Types[index].ToString(),0);
        public void Choose(BoostType type)
        {
            int index=Array.IndexOf(Types,type);if(index<0||Busy||!LM||LM.gameStatus!=GameState.Playing)return;
            if(ShopOpen)CloseShop();
            if(Selected==index){CancelSelection();return;}
            CancelSelection();if(Stock(index)<=0){OpenShop(type);return;}
            Selected=index;currentBoard=LM.squaresArray;AI.THIS?.CancelVisibleHint();
            message.text=Titles[index]+"\n"+(index==3?"Tap Shuffle to rearrange the board.":index==0?"Tap an object to clear it.":index==1?"Tap a cell in the row to clear.":"Tap a cell in the column to clear.");
            prompt.gameObject.SetActive(true);prompt.SetAsLastSibling();
            if(index==3){message.text="Jester Hat — tap here to Shuffle";var b=prompt.GetComponent<Button>();if(!b){b=prompt.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>{if(Selected==3)TryUse(null);});}var im=prompt.GetComponent<Image>();if(!im)im=prompt.gameObject.AddComponent<Image>();im.color=Color.clear;im.raycastTarget=true;}
        }
        public void CancelSelection(){Selected=-1;if(prompt)prompt.gameObject.SetActive(false);}
        public void OpenShop(BoostType type)
        {
            CancelSelection();buying=Array.IndexOf(Types,type);if(buying<0||Busy)return;
            title.text=Titles[buying];shopIcon.sprite=Art(buying);body.text=Descriptions[buying];
            Offer(buying,out int count,out int cost);price.text="x"+count+"       "+cost;
            balance.text="Your coins: "+InitScript.Gems;shop.gameObject.SetActive(true);shop.SetAsLastSibling();
        }
        // Retain the existing project economy until reference shop pricing is recoverable.
        // Do not invent APK prices or migrate unrelated old booster inventories.
        public void Offer(int index,out int count,out int cost)
        {
            var existing=GameObject.Find("CanvasGlobal").GetComponentInChildren<BoostShop>(true);
            int source=(int)(index==0?BoostType.Bomb:index==3?BoostType.Random_color:BoostType.Stripes);
            var product=existing.boostProducts[source];count=product.count[0];cost=product.GemPrices[0];
        }
        public bool Buy()
        {
            if(!ShopOpen||buying<0||purchasePending)return false;
            Offer(buying,out int count,out int cost);
            if(count<=0||cost<0||InitScript.Gems<cost){balance.text="Not enough coins — you have "+InitScript.Gems;return false;}
            purchasePending=true;
            try{InitScript.Instance.SetGems(InitScript.Gems-cost);InitScript.Instance.BuyBoost(Types[buying],0,Stock(buying)+count);int index=buying;CloseShop();Choose(Types[index]);return true;}
            finally{purchasePending=false;}
        }
        public void CloseShop(){if(shop)shop.gameObject.SetActive(false);buying=-1;}
        bool Stable()=>LM&&LM.gameStatus==GameState.Playing&&!LM.IsResolvingMatches&&LM.InputSwaps==0&&!RoyalPowerupDirector.Busy&&
            LM.GetItems().Where(i=>i).All(i=>!i.falling&&!i.destroying)&&!MenuManager.Instance.BlocksBoardInput;
        void Update()
        {
            if(!LM)return;
            if(LM.gameStatus!=GameState.Playing||(currentBoard!=null&&currentBoard!=LM.squaresArray))
            {CancelSelection();CloseShop();if(Busy&&(LM.gameStatus==GameState.Map||LM.gameStatus==GameState.PrepareGame||currentBoard!=LM.squaresArray))CancelOperation();currentBoard=LM.squaresArray;return;}
            foreach(var b in buttons)if(b)b.transform.localScale=Vector3.one*(Selected==Array.IndexOf(Types,b.type)?1.06f:1);
            // Back first closes this bar's shop or cancels a chosen tool, instead of also opening the pause menu.
            if(InputManager.Instance&&InputManager.Instance.Gameplay.Cancel.WasReleasedThisFrame())
            {
                if(ShopOpen){CloseShop();RoyalThrone.Presentation.RoyalBackButton.Consume();return;}
                if(Selected>=0&&!Busy){CancelSelection();RoyalThrone.Presentation.RoyalBackButton.Consume();return;}
            }
            if(Selected<0||Busy||ShopOpen)return;
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
            InitScript.Instance.SpendBoost(Types[type]);CancelSelection();Busy=true;LM.DragBlocked=true;currentBoard=LM.squaresArray;LastHitCount=0;
            operation=StartCoroutine(Use(type,affected));return true;
        }
        List<Item> Movable()=>LM.GetItems().Where(i=>i&&!i.destroying&&!i.falling&&i.square.CanGoOut()&&i.currentType!=ItemsTypes.INGREDIENT&&!RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)).ToList();
        IEnumerator Use(int type,List<Square> affected)
        {
            try
            {
                AI.THIS?.CancelVisibleHint();
                if(type==3){
                    shuffled=false;yield return Shuffle();
                    if(!shuffled){InitScript.Instance.BuyBoost(Types[type],0,Stock(type)+1);Status="No safe shuffle available; hat returned.";yield break;}
                }
                else
                {
                    if(type==0)yield return Hammer(affected[0].transform.position);
                    foreach(var s in affected){if(!s||s.type==SquareTypes.UNDESTROYABLE)continue;
                        if(s.item&&s.item.currentType!=ItemsTypes.INGREDIENT){s.item.royalBlastClear=true;s.item.DestroyItem(true);LastHitCount++;}
                        else if(s.block.Count>0){s.DestroyBlock();LastHitCount++;}
                        if(type!=0)yield return new WaitForSeconds(.045f);
                    }
                }
                yield return new WaitForSeconds(.12f);LM.FindMatches();
                float deadline=Time.realtimeSinceStartup+30;
                while((LM.IsResolvingMatches||RoyalPowerupDirector.Busy||LM.GetItems().Where(i=>i).Any(i=>i.falling||i.destroying))&&Time.realtimeSinceStartup<deadline)yield return null;
                Status="Completed "+Titles[type];
            }
            finally{Busy=false;if(LM&&!LM.IsResolvingMatches&&LM.InputSwaps==0)LM.DragBlocked=false;if(hammer)hammer.enabled=false;operation=null;}
        }
        IEnumerator Hammer(Vector3 position)
        {
            if(!hammer){var go=new GameObject("Reusable reference hammer");go.transform.SetParent(transform);hammer=go.AddComponent<SpriteRenderer>();hammer.sprite=Art(0);hammer.sortingOrder=34;}
            hammer.enabled=true;float cell=LM.squareWidth;hammer.transform.localScale=Vector3.one*cell*1.35f;
            for(float t=0;t<.24f;t+=Time.deltaTime){float u=t/.24f;hammer.transform.position=position+new Vector3(.15f,Mathf.Lerp(.8f,0,u*u),-.5f)*cell;hammer.transform.rotation=Quaternion.Euler(0,0,Mathf.Lerp(-35,15,u*u));yield return null;}
            hammer.enabled=false;
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
        void CancelOperation(){if(operation!=null)StopCoroutine(operation);operation=null;Busy=false;if(LM&&!LM.IsResolvingMatches&&LM.InputSwaps==0)LM.DragBlocked=false;if(hammer)hammer.enabled=false;}
        void OnDisable(){CancelSelection();CloseShop();CancelOperation();}
    }
}
