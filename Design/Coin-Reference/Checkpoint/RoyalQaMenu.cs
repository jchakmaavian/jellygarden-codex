#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalThrone.Testing
{
    // Entire menu is runtime-created. No scene, prefab or player Settings edits.
    public sealed class RoyalQaMenu : MonoBehaviour
    {
        /// <summary>Set by the Mod Menu, which replaces this screen but still needs CoinReward.</summary>
        public static bool SuppressUi;
        RoyalQaService service;RectTransform root,panel,content,quick;Text status,metrics;Font font;
        readonly List<Button> commandButtons=new List<Button>();readonly HashSet<int> attached=new HashSet<int>();
        string category="Gems";float scanAt;bool shown;Text loopLabel;
        readonly List<Text> debugLabels=new List<Text>();
        readonly List<Image> rewardCoins=new List<Image>();
        readonly List<Selectable> parameters=new List<Selectable>();
        readonly List<RectTransform> cellBorders=new List<RectTransform>();
        public IEnumerator CoinReward()
        {
            var home=FindAnyObjectByType<RoyalHomeScreen>();
            var sprite=home&&home.icons.Length>1?home.icons[1]:RoyalPieceSkin.Art("gem");
            while(rewardCoins.Count<8){var rect=Rect("QA reward coin",root);rect.sizeDelta=new Vector2(64,64);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;rewardCoins.Add(image);}
            float age=0;
            try
            {
                while(age<1.35f)
                {
                    age+=Time.deltaTime;
                    for(int i=0;i<rewardCoins.Count;i++)
                    {
                        var coin=rewardCoins[i];float t=Mathf.Clamp01((age-i*.04f)/.95f);coin.gameObject.SetActive(t<1);
                        float e=t*t*(3-2*t);var r=coin.rectTransform;r.anchorMin=r.anchorMax=Vector2.Lerp(new Vector2(.5f+(i-3.5f)*.035f,.48f),new Vector2(.27f,.966f),e);
                        r.anchoredPosition=new Vector2(Mathf.Sin(t*Mathf.PI)*(i-3.5f)*12,Mathf.Sin(t*Mathf.PI)*65);r.localScale=Vector3.one*Mathf.Lerp(1,.45f,e);
                    }
                    yield return null;
                }
            }
            finally{foreach(var coin in rewardCoins)if(coin)coin.gameObject.SetActive(false);}
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded-=OnScene;SceneManager.sceneLoaded+=OnScene;OnScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        }
        static void OnScene(Scene scene,LoadSceneMode mode)
        {
            // RoyalModMenu installs the shared object now. This stays only so the legacy screen can be
            // restored by flipping SuppressUi, and so the service can still reach CoinReward.
        }
        void Awake()
        {
            service=GetComponent<RoyalQaService>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root=Rect("QA canvas",transform);
            if(SuppressUi){var c=root.gameObject.AddComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=1000;root.gameObject.AddComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;return;}var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=.5f;root.gameObject.AddComponent<GraphicRaycaster>();
            panel=Rect("Game testing mode",root);Bounds(panel,.025f,.07f,.975f,.95f);panel.gameObject.AddComponent<Image>().color=new Color(.025f,.065f,.14f,.985f);
            var heading=Label(panel,"GAME TESTING MODE",30);Bounds(heading.rectTransform,.04f,.935f,.96f,.985f);
            var note=Label(panel,"INTERNAL QA  •  ISOLATED TEST PROFILE",18);note.color=new Color(1,.79f,.29f);Bounds(note.rectTransform,.04f,.899f,.96f,.935f);
            status=Label(panel,service.Status,19);Bounds(status.rectTransform,.04f,.80f,.96f,.895f);
            for(int i=0;i<9;i++){string name=RoyalQaService.Categories[i];var b=Button(panel,name,()=>{category=name;Populate();});int x=i%3,y=i/3;Bounds((RectTransform)b.transform,.035f+x*.315f,.744f-y*.05f,.335f+x*.315f,.785f-y*.05f);}
            Field("Amount",.035f,.595f,()=>service.Amount,v=>service.Amount=v);Field("Level",.355f,.595f,()=>service.Level,v=>service.Level=v);Field("Gem 0–4",.675f,.595f,()=>service.Gem,v=>service.Gem=Mathf.Clamp(v,0,4));
            Field("Column",.035f,.535f,()=>service.Column,v=>service.Column=v);Field("Row",.355f,.535f,()=>service.Row,v=>service.Row=v);
            var powers=Button(panel,"A: TNT",null);Bounds((RectTransform)powers.transform,.675f,.53f,.965f,.575f);
            parameters.Add(powers);
            powers.onClick.AddListener(()=>{service.PowerA=(service.PowerA+1)%5;powers.GetComponentInChildren<Text>().text="A: "+RoyalQaService.PowerNames[service.PowerA];});
            var pb=Button(panel,"B: Rocket H",null);Bounds((RectTransform)pb.transform,.675f,.486f,.965f,.526f);pb.onClick.AddListener(()=>{service.PowerB=(service.PowerB+1)%5;pb.GetComponentInChildren<Text>().text="B: "+RoyalQaService.PowerNames[service.PowerB];});
            parameters.Add(pb);
            var guide=Label(panel,"Zero-based cell coordinates. Scenarios use a disposable practice board.",16);Bounds(guide.rectTransform,.035f,.475f,.65f,.53f);
            var scroll=Rect("Test controls",panel);Bounds(scroll,.035f,.178f,.965f,.467f);var scrollRect=scroll.gameObject.AddComponent<ScrollRect>();scrollRect.horizontal=false;scrollRect.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Rect("Viewport",scroll);Bounds(viewport,0,0,1,1);viewport.gameObject.AddComponent<Image>().color=new Color(.04f,.10f,.20f);viewport.gameObject.AddComponent<RectMask2D>();
            content=Rect("Commands",viewport);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(300,62);grid.spacing=new Vector2(10,10);grid.padding=new RectOffset(8,8,8,8);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scrollRect.content=content;scrollRect.viewport=viewport;
            float[] speeds={.25f,.5f,1,1.5f};for(int i=0;i<4;i++){float speed=speeds[i];var b=Button(panel,speed+"×",()=>{service.Speed=speed;service.Status="Game speed: "+speed+"×";});Bounds((RectTransform)b.transform,.035f+i*.155f,.115f,.18f+i*.155f,.162f);}
            var loop=Button(panel,"Loop: OFF",()=>{service.Loop=!service.Loop;loopLabel.text="Loop: "+(service.Loop?"ON":"OFF");});loopLabel=loop.GetComponentInChildren<Text>();Bounds((RectTransform)loop.transform,.675f,.115f,.965f,.162f);
            var start=Button(panel,"Start test profile",()=>{if(!RoyalSave.Testing)service.StartSession();else{service.Loop=false;StartCoroutine(service.EndSession());}});Bounds((RectTransform)start.transform,.035f,.048f,.65f,.10f);
            var close=Button(panel,"View game",()=>{service.Visible=false;});Bounds((RectTransform)close.transform,.675f,.048f,.965f,.10f);
            quick=Rect("QA replay toolbar",root);Bounds(quick,.035f,.008f,.965f,.055f);
            var open=Button(quick,"QA MENU",()=>{service.Visible=true;service.Loop=false;});Bounds((RectTransform)open.transform,0,0,.30f,1);
            var replay=Button(quick,"Replay",()=>{if(service.LastReplay.Length>0)service.Request(service.LastReplay);});Bounds((RectTransform)replay.transform,.32f,0,.62f,1);
            var stop=Button(quick,"Stop loop",()=>{service.Loop=false;service.Speed=1;});Bounds((RectTransform)stop.transform,.64f,0,1,1);
            metrics=Label(root,"",16);metrics.alignment=TextAnchor.UpperLeft;metrics.color=Color.yellow;Bounds(metrics.rectTransform,.04f,.952f,.98f,.99f);
            service.Changed+=()=>{status.text=service.Status;start.GetComponentInChildren<Text>().text=RoyalSave.Testing?"End test profile / restore player":"Start test profile";};
            service.Visible=false;Populate();panel.gameObject.SetActive(false);
        }
        void Field(string caption,float x,float y,Func<int> get,Action<int> set)
        {
            var label=Label(panel,caption,16);Bounds(label.rectTransform,x,y+.018f,x+.28f,y+.045f);
            var r=Rect(caption+" input",panel);Bounds(r,x,y-.02f,x+.28f,y+.017f);r.gameObject.AddComponent<Image>().color=new Color(.13f,.22f,.34f);
            var input=r.gameObject.AddComponent<InputField>();var text=Label(r,get().ToString(),21);Bounds(text.rectTransform,.04f,0,.96f,1);input.textComponent=text;input.contentType=InputField.ContentType.IntegerNumber;input.characterLimit=7;input.text=get().ToString();input.onEndEdit.AddListener(s=>{if(int.TryParse(s,out int value))set(value);input.text=get().ToString();});
            parameters.Add(input);
        }
        void Populate()
        {
            foreach(Transform child in content)Destroy(child.gameObject);commandButtons.Clear();
            foreach(string command in service.Commands[category]){string key=command;var b=Button(content,key,()=>service.Request(key));b.name="QA control: "+key;commandButtons.Add(b);}content.anchoredPosition=Vector2.zero;
        }
        void Update()
        {
            if(SuppressUi)return;
            bool visible=service.Visible;if(shown!=visible){shown=visible;panel.gameObject.SetActive(visible);}
            quick.gameObject.SetActive(RoyalSave.Testing&&!visible);status.text=service.Status;
            foreach(var b in commandButtons)if(b)b.interactable=RoyalSave.Testing&&!service.Busy;
            foreach(var field in parameters)if(field)field.interactable=!service.Busy;
            if(Time.unscaledTime<scanAt)return;scanAt=Time.unscaledTime+.3f;
            AttachSettings();RefreshDebug();
        }
        void AttachSettings()
        {
            foreach(var home in FindObjectsByType<RoyalHomeScreen>(FindObjectsInactive.Include))
            {
                if(!home.settingsGroup||!attached.Add(home.GetHashCode()))continue;
                var b=Button(home.settingsGroup.transform,"GAME TESTING MODE",()=>{service.Visible=true;home.ClosePanel();});
                // Use the existing gap above Back to home. Below the dialog,
                // the raised Home navigation icon intercepts the center hit area.
                var rt=(RectTransform)b.transform;rt.SetParent(home.dialogRect,false);Bounds(rt,.12f,.145f,.88f,.225f);
                var gate=b.gameObject.AddComponent<RoyalQaSettingsGate>();gate.Settings=home.settingsGroup;
            }
            if(!MenuManager.Instance)return;
            foreach(var menu in MenuManager.Instance.GetComponentsInChildren<AnimationManager>(true))
            {
                if(menu.name!="Settings"&&menu.name!="MenuPause"||!attached.Add(menu.GetHashCode()))continue;
                var face=menu.transform.Find("Image");if(!face)continue;
                var b=Button(face,"GAME TESTING MODE",()=>{service.Visible=true;});var rect=(RectTransform)b.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.sizeDelta=new Vector2(456,55);rect.anchoredPosition=new Vector2(0,-39);
            }
        }
        void RefreshDebug()
        {
            var lm=LevelManager.THIS;bool active=RoyalSave.Testing&&lm;
            metrics.gameObject.SetActive(active&&service.Counters);
            if(active&&service.Counters){int particles=FindObjectsByType<ParticleSystem>().Sum(p=>p.particleCount);metrics.text=$"QA • {1/Mathf.Max(.001f,Time.unscaledDeltaTime):0} FPS • particles {particles} • fragments {CandyShatterService.Instance.ActiveFragments}\nCoins/Gems {InitScript.Gems} • Lives {InitScript.lifes} • Speed {service.Speed:0.##}×";}
            int used=0;
            if(active&&lm.gameStatus==GameState.Playing&&(service.Grid||service.Ids||service.ValidMoves||service.Matches||service.Candidate))
            {
                var marked=new HashSet<Square>();var matched=service.Matches?service.ReadDetectedMatches():new HashSet<Square>();var hint=RoyalHintSolver.Find(lm);
                foreach(var sq in lm.squaresArray.Where(s=>s&&s.item))
                {
                    if(service.ValidMoves)foreach(var neighbor in new[]{sq.GetNeighborRight(),sq.GetNeighborBottom()})if(neighbor&&neighbor.item&&RoyalHintSolver.EvaluateSwap(lm,sq.item,neighbor.item)!=null){marked.Add(sq);marked.Add(neighbor);}
                }
                foreach(var sq in lm.squaresArray.Where(s=>s&&s.type!=SquareTypes.NONE))
                {
                    if(used==debugLabels.Count){var label=Label(root,"",15);label.rectTransform.sizeDelta=new Vector2(78,48);debugLabels.Add(label);}
                    var t=debugLabels[used++];t.gameObject.SetActive(true);string flags=(marked.Contains(sq)?" V":"")+(matched.Contains(sq)?" M":"")+(service.Candidate&&hint!=null&&hint.MatchCells.Contains(sq)?" H":"");
                    t.text=(service.Grid?$"[{sq.col},{sq.row}]":"")+(service.Ids&&sq.item?$"\n{sq.item.color}:{sq.item.currentType}":"")+flags;
                    var point=Camera.main.WorldToScreenPoint(sq.transform.position);RectTransformUtility.ScreenPointToLocalPointInRectangle(root,point,null,out var local);t.rectTransform.anchoredPosition=local;t.color=flags.Length>0?Color.yellow:Color.white;
                    if(used>cellBorders.Count)
                    {
                        var border=Rect("QA cell border",root);cellBorders.Add(border);
                        for(int side=0;side<4;side++)
                        {
                            var edge=Rect("Edge",border);var line=edge.gameObject.AddComponent<Image>();line.color=new Color(0,1,1,.65f);line.raycastTarget=false;
                            if(side<2){Bounds(edge,0,side,1,side);edge.sizeDelta=new Vector2(0,1.5f);}
                            else{Bounds(edge,side-2,0,side-2,1);edge.sizeDelta=new Vector2(1.5f,0);}
                        }
                    }
                    var frame=cellBorders[used-1];frame.gameObject.SetActive(service.Grid);frame.anchoredPosition=local;
                    var corner=Camera.main.WorldToScreenPoint(sq.transform.position+new Vector3(lm.squareWidth,lm.squareHeight));
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root,corner,null,out var cornerLocal);frame.sizeDelta=new Vector2(Mathf.Abs(cornerLocal.x-local.x),Mathf.Abs(cornerLocal.y-local.y));
                }
            }
            for(int i=used;i<debugLabels.Count;i++)debugLabels[i].gameObject.SetActive(false);
            for(int i=used;i<cellBorders.Count;i++)cellBorders[i].gameObject.SetActive(false);
        }
        RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);return r;}
        Text Label(Transform parent,string text,int size){var r=Rect(text,parent);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        Button Button(Transform parent,string text,UnityEngine.Events.UnityAction action)
        {var r=Rect(text,parent);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.13f,.32f,.57f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;if(action!=null)b.onClick.AddListener(action);var label=Label(r,text,20);Bounds(label.rectTransform,.025f,.02f,.975f,.98f);return b;}
        static void Bounds(RectTransform r,float x,float y,float xx,float yy){r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(xx,yy);r.offsetMin=r.offsetMax=Vector2.zero;}
    }
    public sealed class RoyalQaSettingsGate : MonoBehaviour
    {
        public GameObject Settings;CanvasGroup group;
        void Awake(){group=gameObject.AddComponent<CanvasGroup>();}
        void Update(){bool on=Settings&&Settings.activeInHierarchy;group.alpha=on?1:0;group.interactable=group.blocksRaycasts=on;}
    }
}
#endif

