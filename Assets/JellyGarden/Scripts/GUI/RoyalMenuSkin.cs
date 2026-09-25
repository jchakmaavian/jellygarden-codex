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
            Place(tr,x,y,w,h);var i=tr.GetComponent<Image>();if(!i)i=tr.gameObject.AddComponent<Image>();i.sprite=RoyalPieceSkin.Art(art);i.color=Color.white;i.preserveAspect=true;i.raycastTarget=false;return i;
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
            var face=Layer(r,"Enamel",10,inset?new Color(.065f,.24f,.48f):new Color(.16f,.46f,.91f),inset?Navy:new Color(.035f,.15f,.40f),button?21:25);
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
        public static void Boost(Transform t,bool compact=false)
        {
            if(!t)return;var indicator=t.Find("Indicator");if(!indicator)return;Place(indicator,0,0,100,100);
            float badgeY=compact?-24:-46;Button(indicator.Find("Plus"),"+",49,badgeY,48,48,33);
            var plate=indicator.Find("Image");if(plate){Place(plate,49,badgeY,48,48);Surface(plate,true);Field(plate,"Count",0,0,42,40,24);var check=plate.Find("Check");if(check){Place(check,0,0,38,38);var im=check.GetComponent<Image>();if(im)im.color=Color.clear;Label(check,"Royal check","✓",0,0,38,38,25);}}
            // The other boosters are only set aside while one is selected. Labelling that state
            // "LOCKED" read as a permanent unlock requirement, even on boosters the player owns,
            // so the dim alone now carries it.
            var locked=t.Find("Lock");if(locked){var im=locked.GetComponent<Image>();if(im)im.color=new Color(.02f,.09f,.23f,.72f);Label(locked,"Royal lock","",0,0,120,40,19);}
        }
        // Pause -> HOME leaves the level and the life spent on it, so the pause menu asks first. The card covers the
        // pause panel (its dimmer blocks the buttons underneath); LEAVE runs the existing BackToMap, STAY returns.
        public const string LeaveConfirmName="Royal leave confirm";
        static void LeaveConfirm(Transform menu,Transform p)
        {
            if(p.Find(LeaveConfirmName))return;
            var dim=new GameObject(LeaveConfirmName,typeof(RectTransform),typeof(Image));dim.transform.SetParent(p,false);
            var dr=(RectTransform)dim.transform;dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;
            var shade=dim.GetComponent<Image>();shade.color=new Color(.005f,.02f,.07f,.78f);shade.raycastTarget=true;
            var card=new GameObject("Card",typeof(RectTransform)).transform;card.SetParent(dim.transform,false);Place(card,0,0,540,340);Surface(card);
            Label(card,"Royal confirm title","LEAVE LEVEL?",0,105,460,62,34);
            Label(card,"Royal confirm text","You will lose the life used for this level.",0,32,470,56,22);
            var manager=menu.GetComponent<JellyGarden.Scripts.GUI.AnimationManager>();
            var leave=new GameObject("Leave",typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));leave.transform.SetParent(card,false);
            Button(leave.transform,"LEAVE",-118,-88,210,76,28);
            leave.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{if(manager)manager.BackToMap();});
            var stay=new GameObject("Stay",typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));stay.transform.SetParent(card,false);
            Button(stay.transform,"STAY",118,-88,210,76,28);
            stay.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{dim.SetActive(false);RoyalAudioDirector.Get?.Play(RoyalCue.Click);});
            dim.SetActive(false);
        }
        public static void Apply(Transform root,Font display)
        {
            font=display;
            foreach(Transform menu in root)
            {
                var p=menu.Find("Image");if(!p||p.Find("Royal skin"))continue;
                foreach(var text in menu.GetComponentsInChildren<Text>(true))Typography(text,32);
                switch(menu.name)
                {
                    case "MenuPlay":
                        Base(menu,p,820,"LEVEL");Field(p,"Level",0,265,220,80,60);
                        for(int s=1;s<=3;s++)HideVisual(p.Find("Star"+s));
                        Label(p,"Royal goals heading","LEVEL GOALS",0,182,500,45,24);
                        var gp=new GameObject("Royal goal plate",typeof(RectTransform)).transform;gp.SetParent(p,false);Place(gp,0,101,506,120);Surface(gp,false,true);gp.SetSiblingIndex(1);
                        Place(p.Find("Targets"),0,103,460,94);HideVisual(p.Find("TargetDescription1"));HideVisual(p.Find("TargetDescription2"));
                        Label(p,"Royal boosters heading","START WITH A BOOST",0,-10,460,44,25);
                        for(int i=1;i<=3;i++){var b=p.Find("Boost"+i);Place(b,(i-2)*174,-113,128,128);var im=b.GetComponent<Image>();im.sprite=RoyalPieceSkin.Art(new[]{"orb","rocket","tnt"}[i-1]);im.preserveAspect=true;Boost(b);Label(p,"Royal boost name "+i,new[]{"MAGIC ORB","ROCKET","TNT"}[i-1],(i-2)*174,-204,160,36,19);}
                        Button(p.Find("Info"),"?",247,-10,46,46,26);Button(p.Find("Play"),"PLAY",0,-306,382,100,44);HideVisual(p.Find("Offline"));break;
                    case "GemsShop":
                        Base(menu,p,880,"ROYAL GEMS");Label(p,"Royal subtitle","A little extra for your next adventure",0,315,520,45,21);
                        for(int i=1;i<=4;i++){
                            var pack=p.Find("Pack"+i);Place(pack,i%2==1?-142:142,i<=2?135:-140,260,250);Surface(pack,false,true);
                            Art(pack,"Royal gem","gem",0,37,118,105);Field(pack,"Count",0,-24,180,49,38);
                            var buy=pack.Find("Buy");Place(buy,0,-86,224,67);Surface(buy,true);Field(buy,"Price",0,0,196,50,28);
                        }Button(p.Find("Video"),"WATCH & EARN",0,-371,360,64,23);break;
                    case "LiveShop":
                        Base(menu,p,720,"MORE LIVES");foreach(Transform tr in p)if(tr.name=="Label")HideVisual(tr);
                        Art(p,"Royal heart","heart",0,105,220,210);Label(p,"Royal message","Refill your hearts and keep playing",0,-42,505,50,24);
                        Label(p,"Royal next life","",0,-88,505,44,22);
                        var life=p.Find("BuyLife");Place(life,0,-147,380,96);Surface(life,true);HideVisual(life.Find("Text"));Label(life,"Royal refill","REFILL",-75,0,210,64,30);
                        Field(life,"Price",72,0,70,62,35);Art(life,"Royal gem","gem",135,0,55,55);Button(p.Find("Video"),"WATCH FOR A LIFE",0,-265,380,66,24);break;
                    case "MenuPause":case "Settings":
                        bool paused=menu.name=="MenuPause";
                        Base(menu,p,paused?720:600,paused?"TAKE A BREATHER":"SETTINGS");
                        Art(p,"Royal crest","crown",0,paused?181:124,116,98);
                        Button(p.Find("Continue"),"CONTINUE",0,70,420,86,34);
                        float soundY=paused?-46:9;
                        Button(p.Find("Music"),"MUSIC ON",-119,soundY,218,74,23);Button(p.Find("Sound"),"SFX ON",119,soundY,218,74,23);
                        foreach(string n in new[]{"Music","Sound"}){var off=p.Find(n+"/"+n+"Off");if(off)HideVisual(off);}
                        var hint=new GameObject("Hints",typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));hint.transform.SetParent(p,false);
                        Button(hint.transform,"MATCHING HINTS ON",0,paused?-143:-89,456,74,24);
                        hint.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>{RoyalHintSettings.Enabled=!RoyalHintSettings.Enabled;RoyalAudioDirector.Get?.Play(RoyalCue.Click);});
                        Button(p.Find("Map"),"HOME",0,-241,280,68,24);Button(p.Find("Info"),"HELP",0,-189,280,68,24);
                        if(paused)Label(p,"Royal hint description","A gentle clue after a few quiet seconds",0,-303,510,38,19);
                        if(paused)LeaveConfirm(menu,p);
                        break;
                    case "MenuComplete":case "MenuFailed":
                        bool win=menu.name=="MenuComplete";Base(menu,p,870,win?"WELL PLAYED!":"TRY AGAIN");Field(p,"Level",0,288,420,50,30);
                        for(int i=1;i<=3;i++){var star=p.Find("Star"+i);Place(star,(i-2)*130,191+(i==2?12:0),118,118);if(star){star.GetComponent<Image>().sprite=RoyalPieceSkin.Art("star");star.GetComponent<Image>().preserveAspect=true;}}
                        Art(p,"Royal king",win?"king-cheer":"king-think",0,29,165,146);Place(p.Find("Targets"),0,-84,450,75);
                        Field(p,"Score",0,-165,490,65,48);Field(p,"BestScore",0,-224,490,42,23);
                        foreach(string n in new[]{"TargetDescription1","TargetDescription2","TargetCheck1","TargetCheck2","TargetUnCheck1","TargetUnCheck2"})HideVisual(p.Find(n));
                        Button(p.Find(win?"Next":"Again"),win?"CONTINUE":"TRY AGAIN",0,-335,390,96,34);
                        Label(p,"Royal score label","SCORE",0,-137,350,30,20);Field(p,"Score",0,-181,490,60,44);Field(p,"BestScore",0,-230,490,38,21);
                        if(!win){
                            Place(p,0,0,620,740);Place(p.Find("Royal heading"),0,302,480,70);Field(p,"Level",0,233,420,50,30);
                            for(int i=1;i<=3;i++)HideVisual(p.Find("Star"+i));Place(p.Find("Royal king"),0,102,180,164);
                            Place(p.Find("Targets"),0,-30,450,75);Place(p.Find("Royal score label"),0,-105,350,30);Field(p,"Score",0,-148,490,60,44);Field(p,"BestScore",0,-196,490,38,21);Place(p.Find("Again"),0,-279,390,96);
                        }
                        var share=p.Find("Share");if(share)Button(share,"SHARE",0,-264,220,45,18);break;
                    case "BoostShop":
                        Base(menu,p,850,"ROYAL BOOSTERS");Place(p.Find("Boost"),-180,211,138,138);Field(p,"BoostDescription",78,211,300,140,23);
                        for(int i=1;i<=3;i++){var b=p.Find("BuyBoost"+i);Place(b,0,43-(i-1)*128,490,96);Surface(b,true);Field(b,"Count",-130,0,140,65,32);Field(b,"Price",105,0,90,65,32);Art(b,"Royal gem","gem",181,0,56,56);}
                        Field(p,"Gems",30,-334,230,46,28);Art(p,"Royal balance gem","gem",-125,-334,50,50);break;
                    case "BoostInfo":
                        Base(menu,p,810,"POWER UP YOUR PLAY");
                        int ix=0;var descriptions=p.GetComponentsInChildren<Text>(true);
                        foreach(var desc in descriptions)if(desc.name=="BoostDescription"){
                            Place(desc.transform,66,211-ix*192,315,136);Typography(desc,23);desc.text=new[]{"Magic orb\nClears pieces of one colour","Rocket\nClears a full row or column","TNT\nBlasts the surrounding area"}[Mathf.Min(ix,2)];Art(p,"Royal info art "+ix,new[]{"orb","rocket","tnt"}[Mathf.Min(ix,2)],-187,211-ix*192,128,128);ix++;
                        }break;
                    case "Reward":
                        Base(menu,p,670,"A ROYAL REWARD");HideVisual(p.Find("Label"));Art(p,"Royal reward","chest",0,70,270,260);HideVisual(p.Find("icon"));Button(p.Find("Ok"),"COLLECT",0,-220,360,92,34);break;
                    case "Tutorial":
                        Base(menu,p,720,"HOW TO PLAY");Button(p.Find("Next"),"NEXT",174,-266,210,80,28);Button(p.Find("Back"),"BACK",-174,-266,210,80,28);break;
                }
                if(p.Find("Royal skin"))menu.gameObject.AddComponent<RoyalMenuPresenter>();
            }
            var retry=root.Find("PreFailed");if(retry&&!retry.Find("Banner/Royal skin")){
                var banner=retry.Find("Banner");Surface(banner);Place(banner,0,0,640,660);
                Label(banner,"Royal heading","SO CLOSE!",0,238,520,65,42);Label(banner,"Royal retry text","A few more moves could do it",0,170,520,50,23);
                Place(banner.Find("Targets"),0,63,450,90);
                foreach(var group in retry.GetComponentsInChildren<JellyGarden.Scripts.Targets.TargetsGroup>(true)){
                    group.transform.SetParent(banner,false);group.name="Royal live goals";Place(group.transform,0,63,450,90);
                }
                foreach(string n in new[]{"TargetCheck1","TargetCheck2","TargetUnCheck1","TargetUnCheck2"})HideVisual(banner.Find("Targets/"+n));
                Button(retry.Find("Buy"),"",0,-85,420,95);var buy=retry.Find("Buy");HideVisual(buy.Find("Royal caption"));Label(buy,"Royal more","CONTINUE",-65,0,250,65,30);Field(buy,"Price",107,0,66,65,32);buy.Find("Price").GetComponent<Text>().enabled=true;Art(buy,"Royal coin","coin",167,0,52,52);
                Button(retry.Find("GiveUp"),"END LEVEL",0,-199,320,70,26);Button(retry.Find("Video"),"WATCH FOR MOVES",0,-280,400,68,23);retry.gameObject.AddComponent<RoyalMenuPresenter>().ConfigureRetryMotion();
            }
        }
    }
}
