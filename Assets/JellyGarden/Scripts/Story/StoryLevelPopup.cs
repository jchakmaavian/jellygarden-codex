// Sapphire Trim: the selected Soft Royal layout in a blue-and-gold frame.
// Goals still come from the in-level HUD reader; booster choices still pass
// through StoryBoosters to the kit when the board is created.
using System;
using System.Collections.Generic;
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryLevelPopup : MonoBehaviour
    {
        public const string HostName = "StoryLevelPopup";
        private const int MaxGoals = 4;
        private static readonly Color32 Cream = new Color32(255,253,236,255);
        private static readonly Color32 Ink = new Color32(64,82,62,255);
        private static readonly Color32 Muted = new Color32(125,138,105,255);
        private static readonly Color32 Gold = new Color32(246,216,146,255);
        private static readonly Color32 Blue = new Color32(35,76,102,255);
        private static StoryLevelPopup _instance;
        public static StoryLevelPopup Instance => _instance;
        public static bool IsOpen => _instance!=null && _instance.gameObject.activeSelf;

        /// <summary>Null launches the real board. Tests can observe the level and area instead.</summary>
        public static Action<int,int> Launcher;
        private int _level, _areaId;
        private Action _onClose;
        private Canvas _canvas;
        private StoryModalBackdrop _backdrop;
        private RectTransform _card, _goalRow;
        private Text _title, _goalHeading, _hint;
        private Button _play, _close;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<BoosterSlot> _boosters = new List<BoosterSlot>();
        private sealed class Slot
        {
            public RectTransform Root;
            public Image Icon, Backing;
            public Text Count, Unit;
            public StoryLevelGoalVfx Vfx;
        }
        private sealed class BoosterSlot
        {
            public BoostType Boost;
            public Button Button;
            public Image Border, Face, Icon;
            public Text Count;
            public GameObject Selected, Locked;
        }
        public int Level => _level;
        public int AreaId => _areaId;
        public Button PlayButton => _play;
        public Button CloseButton => _close;
        public string TitleText => _title==null?null:_title.text;
        public int BoosterCount => _boosters.Count;
        public BoostType BoosterAt(int index) => _boosters[index].Boost;
        public Button BoosterButton(int index) => _boosters[index].Button;
        public bool BoosterIsLocked(int index) => _boosters[index].Locked.activeSelf;
        public bool BoosterIsSelected(int index) => _boosters[index].Selected.activeSelf;
        public List<string> GoalTexts()
        {
            var result=new List<string>();
            foreach(Slot slot in _slots) if(slot.Root.gameObject.activeSelf) result.Add(slot.Count.text);
            return result;
        }
        public List<string> BoosterCounts()
        {
            var result=new List<string>();
            foreach(BoosterSlot slot in _boosters) result.Add(slot.Count.text);
            return result;
        }

        public static void Open(int levelNumber,int areaId,Action onClose)
        {
            if(levelNumber<=0){Debug.LogWarning("[Story] No level to offer.");return;}
            if(_instance==null)
            {
                var go=new GameObject(HostName);
                StoryObjects.Persist(go);
                _instance=go.AddComponent<StoryLevelPopup>();
                _instance.Build();
            }
            StoryBoosters.ClearSelection();
            _instance._level=levelNumber;
            _instance._areaId=areaId;
            _instance._onClose=onClose;
            _instance.gameObject.SetActive(true);
            _instance.Refresh();
            _instance._backdrop.Capture();
        }
        public static void Close()
        {
            if(_instance!=null) _instance.gameObject.SetActive(false);
        }
        private void OnDisable() { _onClose=null; }
        private void OnDestroy() { if(_instance==this) _instance=null; }

        private void Build()
        {
            _canvas=StoryUi.CreateOverlayCanvas("LevelPopupCanvas",600);
            _canvas.transform.SetParent(transform,false);
            _canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(360,720);
            _backdrop=_canvas.gameObject.AddComponent<StoryModalBackdrop>();
            _backdrop.Initialize();
            StoryUi.CreateFullScreenButton("Scrim",_canvas.transform,new Color32(19,38,36,128),null);
            RectTransform safe=StoryCastleFrame.Create(_canvas.transform);
            _card=StoryUi.CreateRect("SapphireCard",safe);
            _card.anchorMin=_card.anchorMax=_card.pivot=new Vector2(.5f,.5f);
            _card.anchoredPosition=new Vector2(0,-10);
            _card.sizeDelta=new Vector2(300,504);

            Plate("Shadow",_card,25,new Color32(15,33,28,50),-7,-9,314,521);
            Plate("Depth",_card,23,new Color32(110,85,47,255),0,-9,300,504);
            Image cream=Plate("CreamPanel",_card,23,Color.white,0,0,300,504);
            StoryCastleUi.Gradient(cream,Cream,new Color32(247,243,219,255));
            Plate("InnerLight",_card,20,new Color32(255,255,255,85),4,-4,292,496);
            Image face=Plate("InnerCream",_card,19,Color.white,6,-6,288,492);
            StoryCastleUi.Gradient(face,Cream,new Color32(247,243,219,255));
            RectTransform frame=StoryUi.CreateRect("SapphireFrame",_card);
            StoryCastleUi.Place(frame,-12,12,324,528);
            frame.gameObject.AddComponent<StorySapphireFrame>().raycastTarget=false;

            BuildHeader();
            Label("Kicker",_card,"A LITTLE CHALLENGE. A LITTLE MAGIC.",7,Muted,24,-43,252,11);
            _goalRow=StoryUi.CreateRect("GoalRow",_card);
            StoryCastleUi.Place(_goalRow,24,-68,252,145);
            _goalHeading=Label("GoalHeading",_card,"YOUR GOAL",8,Muted,24,-155,252,12);
            Image divider=StoryUi.CreateImage("Divider",_card,null,new Color32(218,222,199,140));
            StoryCastleUi.Place(divider.rectTransform,24,-229,252,1);
            var dividerShadow=divider.gameObject.AddComponent<Shadow>();
            dividerShadow.effectDistance=new Vector2(0,-1);
            dividerShadow.effectColor=new Color(1,1,1,.45f);
            BuildBoosterRow();
            BuildPlay();
            Label("Footnote",_card,"Your next star is waiting",8,new Color32(150,156,124,255),24,-480,252,12);
        }
        private void BuildHeader()
        {
            Plate("LevelDepth",_card,14,new Color32(22,55,71,255),48,21,204,49);
            Image rim=Plate("LevelBadge",_card,14,Gold,48,24,204,49);
            Image fill=Plate("Sapphire",rim.transform,12,Color.white,2,-2,200,45);
            StoryCastleUi.Gradient(fill,new Color32(56,103,126,255),Blue);
            RectTransform crown=StoryUi.CreateRect("Crown",rim.transform);
            StoryCastleUi.Place(crown,23,-13,24,24);
            Color gold=new Color32(237,202,124,255);
            Vector2[] points={new Vector2(3,6),new Vector2(8,11),new Vector2(12,3),new Vector2(16,11),new Vector2(21,6),new Vector2(19,19),new Vector2(5,19),new Vector2(3,6)};
            for(int i=1;i<points.Length;i++) Line(crown,"CrownEdge",points[i-1],points[i],1.7f,gold);
            Line(crown,"CrownBand",new Vector2(6,16),new Vector2(18,16),1.7f,gold);
            _title=Label("Title",rim.transform,"Level",25,Cream,53,-4,137,39,true);
            StoryUi.FitOneLine(_title,16,25);

            Image hit=StoryUi.CreateImage("Close",_card,null,Color.clear);
            StoryCastleUi.Place(hit.rectTransform,269,19,44,44);
            _close=StoryCastleUi.Clickable(hit,OnClose);
            Plate("Depth",hit.transform,18,new Color32(115,83,37,255),4.5f,-7.5f,35,35);
            Plate("GoldRim",hit.transform,18,Gold,4.5f,-4.5f,35,35);
            Image blue=Plate("Face",hit.transform,16,Blue,6.5f,-6.5f,31,31);
            _close.targetGraphic=blue;
            Line(hit.transform,"Cross",new Vector2(17,17),new Vector2(27,27),1.8f,Cream);
            Line(hit.transform,"Cross",new Vector2(17,27),new Vector2(27,17),1.8f,Cream);
        }
        private void BuildBoosterRow()
        {
            Label("BoosterHeading",_card,"Choose boosters",13,Ink,24,-251,166,18,false,TextAnchor.MiddleLeft);
            Label("Optional",_card,"OPTIONAL",7,Muted,191,-253,85,15,false,TextAnchor.MiddleRight);
            string[] names={"Rocket","Bomb","Color ball"};
            for(int i=0;i<StoryBoosters.PreLevel.Length;i++)
            {
                BoostType boost=StoryBoosters.PreLevel[i];
                float x=26+i*80;
                var slot=new BoosterSlot{Boost=boost};
                RectTransform root=StoryUi.CreateRect("Booster"+boost,_card);
                StoryCastleUi.Place(root,x,-282,67,92);
                Plate("Depth",root,19,new Color32(232,225,199,255),0,-3,67,65);
                slot.Border=Plate("Tile",root,19,new Color32(227,222,197,255),0,0,67,65);
                slot.Face=Plate("Face",root,18,new Color32(255,252,240,255),1.5f,-1.5f,64,62);
                slot.Button=StoryCastleUi.Clickable(slot.Border,()=>OnBooster(boost));
                slot.Button.targetGraphic=slot.Face;
                slot.Icon=StoryUi.CreateImage("Icon",root,StoryBoosters.IconOf(boost),Color.white);
                slot.Icon.preserveAspect=true;
                StoryCastleUi.Place(slot.Icon.rectTransform,9,-8,49,49);
                Label("Name",root,names[i],9,Muted,-5,-76,77,14);
                Image countRim=Plate("CountRim",root,11,Cream,50,-49,23,23);
                Plate("Gold",countRim.transform,10,new Color32(217,169,85,255),2,-2,19,19);
                slot.Count=Label("Count",countRim.transform,"0",10,Cream,2,-2,19,19);
                StoryUi.FitOneLine(slot.Count,6,10);
                // Zero inventory keeps the item visible and clearly unavailable.
                slot.Locked=Label("Empty",root,"EMPTY",7,Muted,6,-1,55,12).gameObject;
                Image chosen=Plate("Selected",root,10,Cream,52,6,22,22);
                slot.Selected=chosen.gameObject;
                Plate("Green",chosen.transform,9,new Color32(80,141,88,255),2,-2,18,18);
                Line(chosen.transform,"Check",new Vector2(6,11),new Vector2(9,14),1.8f,Color.white);
                Line(chosen.transform,"Check",new Vector2(9,14),new Vector2(16,7),1.8f,Color.white);
                slot.Selected.SetActive(false);
                _boosters.Add(slot);
            }
            _hint=Label("SelectionHint",_card,"Boosters are optional",9,Muted,24,-380,252,14);
        }
        private void BuildPlay()
        {
            Plate("PlayDepth",_card,29,new Color32(49,100,57,255),24,-414,252,56);
            Image rim=Plate("Play",_card,29,new Color32(231,237,179,255),24,-410,252,56);
            Image face=Plate("Green",rim.transform,28,Color.white,1.5f,-1.5f,249,53);
            StoryCastleUi.Gradient(face,new Color32(101,169,98,255),new Color32(65,134,79,255));
            _play=StoryCastleUi.Clickable(rim,OnPlay);
            _play.targetGraphic=face;
            Label("Label",rim.transform,"Play",26,Cream,20,-4,212,46,true);
            Line(rim.transform,"Arrow",new Vector2(224,22),new Vector2(230,28),2.2f,Cream);
            Line(rim.transform,"Arrow",new Vector2(230,28),new Vector2(224,34),2.2f,Cream);
        }
        private Slot BuildSlot()
        {
            var slot=new Slot();
            slot.Root=StoryUi.CreateRect("Goal"+_slots.Count,_goalRow);
            RectTransform glow=StoryUi.CreateRect("StarGlow",slot.Root);
            slot.Vfx=glow.gameObject.AddComponent<StoryLevelGoalVfx>();
            slot.Vfx.raycastTarget=false;
            slot.Backing=StoryUi.CreateImage("IconBacking",slot.Root,null,Color.white);
            slot.Backing.preserveAspect=true;
            slot.Icon=StoryUi.CreateImage("Icon",slot.Root,null,Color.white);
            slot.Icon.preserveAspect=true;
            slot.Count=Label("Count",slot.Root,"0",31,Ink,0,-105,252,40,true);
            slot.Unit=Label("Unit",slot.Root,"points",14,Muted,0,-111,50,28);
            _slots.Add(slot);
            return slot;
        }
        private void Refresh()
        {
            _title.text="Level "+_level;
            _play.interactable=true;
            StoryLevelHud.HudModel model=StoryLevelHud.ReadLevel(_level,0);
            List<StoryLevelHud.HudTarget> targets=model==null?new List<StoryLevelHud.HudTarget>():model.Targets;
            int count=Mathf.Min(targets.Count,MaxGoals);
            _goalHeading.text=count>1?"YOUR GOALS":"YOUR GOAL";
            while(_slots.Count<count) BuildSlot();
            for(int i=0;i<_slots.Count;i++)
            {
                Slot slot=_slots[i];
                slot.Vfx.SetStar(null);
                slot.Root.gameObject.SetActive(i<count);
                if(i>=count) continue;
                StoryLevelHud.HudTarget target=targets[i];
                float width=252f/count;
                StoryCastleUi.Place(slot.Root,i*width,0,width,145);
                float icon=count==1?70f:Mathf.Min(58,width-10);
                StoryCastleUi.Place(slot.Icon.rectTransform,(width-icon)*.5f,-(76-icon)*.5f,icon,icon);
                slot.Icon.rectTransform.pivot=new Vector2(.5f,.5f);
                slot.Icon.rectTransform.anchoredPosition=new Vector2(width*.5f,-38);
                StoryCastleUi.Place(slot.Backing.rectTransform,(width-icon)*.5f,-(76-icon)*.5f,icon,icon);
                StoryCastleUi.Place(slot.Vfx.rectTransform,(width-Mathf.Min(110,width))*.5f,0,Mathf.Min(110,width),76);
                slot.Icon.sprite=target.IsScore?StoryUi.StarSprite:(target.Icon!=null?target.Icon:StoryUi.StarSprite);
                slot.Icon.enabled=slot.Icon.sprite!=null;
                slot.Backing.sprite=target.Backing;
                slot.Backing.enabled=target.Backing!=null;
                slot.Count.text=Mathf.Max(0,target.Remaining).ToString();
                int size=count==1?31:count==2?27:count==3?23:20;
                StoryUi.FitOneLine(slot.Count,14,size);
                slot.Unit.gameObject.SetActive(target.IsScore);
                if(count==1 && target.IsScore)
                {
                    slot.Count.fontSize=size;
                    float digits=Mathf.Min(174,slot.Count.preferredWidth+4);
                    float left=(width-digits-51)*.5f;
                    StoryCastleUi.Place(slot.Count.rectTransform,left,-105,digits,40);
                    StoryCastleUi.Place(slot.Unit.rectTransform,left+digits+5,-111,46,28);
                    slot.Unit.fontSize=14;
                    StoryUi.FitOneLine(slot.Unit,14,14);
                }
                else
                {
                    StoryCastleUi.Place(slot.Count.rectTransform,2,-105,width-4,34);
                    StoryCastleUi.Place(slot.Unit.rectTransform,0,-136,width,12);
                    StoryUi.FitOneLine(slot.Unit,8,9);
                }
                if(target.IsScore) slot.Vfx.SetStar(slot.Icon.rectTransform);
            }
            RefreshBoosters();
            Debug.Log(string.Format("[Story] Level popup: Level {0} for area {1}, goal [{2}], boosters [{3}].",
                _level,_areaId,string.Join(" ",GoalTexts().ToArray()),string.Join(" ",BoosterCounts().ToArray())));
        }
        private void RefreshBoosters()
        {
            int selectedCount=0;
            foreach(BoosterSlot slot in _boosters)
            {
                int owned=Mathf.Max(0,StoryBoosters.Owned(slot.Boost));
                bool selected=owned>0 && StoryBoosters.IsSelected(slot.Boost);
                slot.Count.text=owned.ToString();
                slot.Locked.SetActive(owned<=0);
                slot.Button.interactable=owned>0;
                slot.Icon.color=new Color(1,1,1,owned>0?1:.35f);
                slot.Selected.SetActive(selected);
                slot.Border.color=selected?new Color32(109,155,96,255):new Color32(227,222,197,255);
                slot.Face.color=selected?new Color32(233,240,206,255):new Color32(255,252,240,255);
                if(selected) selectedCount++;
            }
            _hint.text=selectedCount==0?"Boosters are optional":selectedCount+" booster"+(selectedCount==1?"":"s")+" ready to go";
        }
        private void OnBooster(BoostType boost)
        {
            if(!isActiveAndEnabled) return;
            StoryBoosters.Toggle(boost);
            RefreshBoosters();
        }
        private void OnPlay()
        {
            if(!isActiveAndEnabled||!_play.interactable) return;
            _play.interactable=false;
            int level=_level,area=_areaId;
            Close();
            if(Launcher!=null) Launcher(level,area);
            else StoryFlow.LaunchLevel(level,area);
        }
        private void OnClose()
        {
            if(!isActiveAndEnabled) return;
            Action back=_onClose;
            StoryBoosters.ClearSelection();
            Close();
            back?.Invoke();
            StoryShell.EnsureFloor();
        }

        private static Image Plate(string name,Transform parent,int radius,Color tint,float x,float y,float w,float h)
        {
            Image image=StoryCastleUi.Plate(name,parent,radius,tint);
            StoryCastleUi.Place(image.rectTransform,x,y,w,h);
            return image;
        }
        private static Text Label(string name,Transform parent,string value,int size,Color tint,float x,float y,float w,float h,
            bool display=false,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            Text label=StoryCastleUi.Label(name,parent,value,size,tint,display,alignment);
            StoryCastleUi.Place(label.rectTransform,x,y,w,h);
            return label;
        }
        private static void Line(Transform parent,string name,Vector2 from,Vector2 to,float width,Color tint)
        {
            Vector2 delta=new Vector2(to.x-from.x,from.y-to.y);
            Image image=StoryUi.CreateImage(name,parent,null,tint);
            RectTransform r=image.rectTransform;
            r.anchorMin=r.anchorMax=new Vector2(0,1);
            r.pivot=new Vector2(.5f,.5f);
            r.anchoredPosition=new Vector2((from.x+to.x)*.5f,-(from.y+to.y)*.5f);
            r.sizeDelta=new Vector2(delta.magnitude,width);
            r.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
