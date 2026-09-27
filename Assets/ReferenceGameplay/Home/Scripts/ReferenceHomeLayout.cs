using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay
{
    // Packaged home artwork, adapted to the existing safe-area canvas and live callbacks.
    // The castle/background, logo, dialogs and player resources remain owned by RoyalHomeScreen.
    // Proportions follow the approved Home reference of 2026-09-27, measured at 1080x1920 and kept as fractions of the layout
    // width w, so every portrait size keeps the same composition.
    [DefaultExecutionOrder(10000)]
    public sealed class ReferenceHomeLayout : MonoBehaviour
    {
        RoyalHomeScreen home; Image[] tabArt; Image fill; bool built;
        RectTransform sky; RectTransform[] plus; RoyalPanelGraphic navDepth,skyDepth; RoyalPanelGraphic[] tabRim,tabFace,tabLine; float[] emphasis; bool emphasisSet;
        // Resource bar: 1046 x 162 px at 1080 wide, centred 116 px below the safe top. Edges of the avatar, coins, lives, stars and
        // settings as fractions of the bar's width.
        const float HudWidth=.9685f,HudHeight=.1498f,HudCentre=.1071f;
        static readonly float[] ResourceEdges={0,.1424f,.1520f,.4044f,.4054f,.6721f,.6730f,.8824f,.8910f,.9990f};
        // Counters (coins, lives, stars), as x0,y0,x1,y1 of each counter: the cream panel, the icon over its left end, the value
        // between them and the + badge at the panel's right end. The panel sprite's cream face is its top 10-65%.
        static readonly Vector4 Panel=new Vector4(.13f,.023f,1,.7954f);
        static readonly Vector4[] Icon={new Vector4(.002f,.192f,.4034f,.866f),new Vector4(0,.195f,.398f,.82f),new Vector4(0,.2818f,.3836f,.7701f)};
        static readonly Vector4[] Value={new Vector4(.432f,.3869f,.716f,.6589f),new Vector4(.416f,.3869f,.742f,.6589f),new Vector4(.3836f,.3869f,.6712f,.6589f)};
        static readonly Vector4[] Plus={new Vector4(.7225f,.2683f,.9555f,.6791f),new Vector4(.7426f,.2683f,.9634f,.6791f),new Vector4(.6746f,.2683f,.9556f,.6791f)};
        // Level and area cards: 1014 x 201 px, centred 436 px above the safe bottom; the area card is the wider one.
        const float ActionsWidth=.9394f,ActionsHeight=.1864f,ActionsCentre=.4040f,LevelRight=.4687f,AreaLeft=.5175f;
        // Tab bar: 286 px, the imported strip's gold edge and highlight fill its top 14.3 units (at w 720) and stay visible.
        const float NavHeight=.2652f,GoldTrim=.0199f;
        static readonly float[] TabEdges={0,.17f,.347f,.653f,.81f,1};
        // Icon art bottom and top as fractions of the bar's height, at rest and when the tab is selected. The Home castle rises
        // through the gap between the cards; only its spire reaches above their bottom edge.
        static readonly Vector2[] IconRest={new Vector2(.4295f,.8904f),new Vector2(.4609f,.845f),new Vector2(.4539f,.8659f),new Vector2(.4469f,.8799f),new Vector2(.4539f,.8659f)};
        static readonly Vector2 HomeRaised=new Vector2(.493f,1.2989f);
        static readonly Vector2 LabelBox=new Vector2(.269f,.478f);
        const int LabelSize=16,SelectedLabelSize=24;
        // Selected tab: a rounded, slightly lighter panel with a soft rim, and a short gold underline below the label.
        const float PanelWidth=.2472f,PanelTop=.9253f,PanelRadius=.037f,RimWidth=.0019f,LineWidth=.1046f,LineHeight=.0093f,LineCentre=.1833f;
        // The card row's top edge sits at this fraction of the background's height, above its bottom: the courtyard in front of the
        // castle stays visible between the cards and the tab bar at every aspect ratio.
        const float CardLine=.246f;
        // The reference sky deepens toward the top, behind the resource bar: a veil over the upper 47% of the screen, under the clouds.
        const float SkyDepth=.47f;
        // Soft attention on the + badges: a 0.6 s swell every 7 s, a little apart. The selected tab breathes over 2.8 s.
        const float PulsePeriod=7,PulseLength=.6f,PulseAmount=.07f,PulseStagger=.22f,GlowPeriod=2.8f;
        static Sprite Art(int id)=>Resources.Load<Sprite>("ReferenceGameplay/Home/"+id);
        public static void Attach(RoyalHomeScreen home){var v=home.GetComponent<ReferenceHomeLayout>();if(!v)v=home.gameObject.AddComponent<ReferenceHomeLayout>();v.home=home;v.Build();}
        static void Bounds(RectTransform r,float x0,float y0,float x1,float y1){Anchor(r,x0,y0,x1,y1);r.localScale=Vector3.one;}
        // Buttons keep their scale: RoyalButtonMotion owns it for the press feedback.
        static void Anchor(RectTransform r,float x0,float y0,float x1,float y1){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Bounds(RectTransform r,Vector4 b)=>Bounds(r,b.x,b.y,b.z,b.w);
        // A soft drop shadow gives the resource art the reference's depth against the sky.
        static void Shade(Graphic g){var s=g.gameObject.AddComponent<Shadow>();s.effectColor=new Color(.02f,.1f,.3f,.35f);s.effectDistance=new Vector2(0,-3);}
        static void Shade(Transform parent,string name){foreach(Transform c in parent)if(c.name==name)Shade(c.GetComponent<Graphic>());}
        static Image Pic(Transform parent,int id,float x0,float y0,float x1,float y1,bool sliced=false){var r=new GameObject("Imported "+id,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Bounds(r,x0,y0,x1,y1);var im=r.gameObject.AddComponent<Image>();im.sprite=Art(id);im.raycastTarget=false;im.type=sliced?Image.Type.Sliced:Image.Type.Simple;im.pixelsPerUnitMultiplier=2;return im;}
        static Image Pic(Transform parent,int id,Vector4 b)=>Pic(parent,id,b.x,b.y,b.z,b.w);
        static void Pair(Transform p,int id,float x0,float y0,float x1,float y1,bool slice=false,bool rightHalf=false){float m=(x0+x1)*.5f;var a=Pic(p,id,x0,y0,m,y1,slice);var b=Pic(p,id,m,y0,x1,y1,slice);(rightHalf?a:b).rectTransform.localScale=new Vector3(-1,1,1);}
        static RoyalPanelGraphic Surface(string name,Transform parent,Color top,Color bottom){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.gameObject.layer=5;r.SetParent(parent,false);var g=r.gameObject.AddComponent<RoyalPanelGraphic>();g.raycastTarget=false;g.topColor=top;g.bottomColor=bottom;g.cornerRadius=0;return g;}
        // A hidden button keeps its own graphic as a clear hit area: a disabled target graphic takes no taps, which left
        // the profile and area buttons (panel graphics, not Images) dead to touch.
        static void Hide(Transform t){foreach(var g in t.GetComponentsInChildren<Graphic>(true))if(!(g is Text))g.enabled=false;var hit=t.GetComponent<Image>();if(hit){hit.enabled=true;hit.color=Color.clear;}
            else if(t.GetComponent<Selectable>()){var own=t.GetComponent<Graphic>();if(own&&!(own is Text)){own.enabled=true;own.color=Color.clear;}}}
        static void TextPlace(Text t,float x0,float y0,float x1,float y1,int size){t.enabled=true;Bounds(t.rectTransform,x0,y0,x1,y1);t.transform.SetAsLastSibling();t.fontSize=t.resizeTextMaxSize=size;t.resizeTextMinSize=15;t.resizeTextForBestFit=true;}
        static void TextPlace(Text t,Vector4 b,int size)=>TextPlace(t,b.x,b.y,b.z,b.w,size);
        static void ActionFace(Transform parent,int face){
            // Overlap the inset beneath the trim so packed sprite edges never leave a gap.
            Pair(parent,face,.016f,.045f,.984f,.98f,true);
            Pair(parent,16521,0,0,1,1,true);
        }
        void Build(){if(built||!home)return;built=true;
            ReferenceHomeSky.Attach(home);
            // The background is placed against the card row in LateUpdate instead of centred on the screen.
            sky=home.transform.Find("Castle framing/Royal blue courtyard") as RectTransform;
            var fitter=sky?sky.GetComponent<AspectRatioFitter>():null;if(fitter)fitter.enabled=false;
            if(sky){skyDepth=Surface("Royal sky depth",sky,new Color(0,.30f,.74f,.8f),new Color(0,.38f,.95f,0));skyDepth.transform.SetSiblingIndex(0);}
            if(home.logo)home.logo.gameObject.SetActive(false);
            foreach(var b in home.resourceButtons)Hide(b.transform);
            // Keep the Royal Throne identity inside the profile; use reference resource artwork.
            var portrait=home.resourceButtons[0].transform.Find("King portrait").GetComponent<Image>();portrait.enabled=true;portrait.color=Color.white;
            // The bust is framed inside the aperture; gold trim is drawn over the portrait.
            var portraitBack=Pic(home.resourceButtons[0].transform,16613,.075f,.085f,.925f,.95f);
            portraitBack.name="Portrait blue inset";portraitBack.sprite=null;portraitBack.color=new Color(.065f,.23f,.52f);portraitBack.transform.SetAsFirstSibling();
            portrait.transform.SetAsLastSibling();Bounds(portrait.rectTransform,.13f,.12f,.87f,.91f);portrait.preserveAspect=true;
            Pair(home.resourceButtons[0].transform,16613,0,0,1,1,true);Shade(home.resourceButtons[0].transform,"Imported 16613");
            int[] icons={22081,21591,16674};Text[] values={home.coinLabel,home.lifeStatus,home.starLabel};plus=new RectTransform[3];
            for(int i=1;i<=3;i++){var b=home.resourceButtons[i].transform;Pair(b,21706,Panel.x,Panel.y,Panel.z,Panel.w,true,true);var im=Pic(b,icons[i-1],Icon[i-1]);im.preserveAspect=true;Shade(im);TextPlace(values[i-1],Value[i-1],24);
                // Every counter carries the same + badge. It is art inside the counter, so a tap still opens that counter's panel.
                var badge=new GameObject("Plus badge",typeof(RectTransform)).GetComponent<RectTransform>();badge.SetParent(b,false);Bounds(badge,Plus[i-1]);Pair(badge,16282,0,0,1,1);plus[i-1]=badge;}
            foreach(var t in home.resourceButtons[1].GetComponentsInChildren<Text>(true))if(t!=home.coinLabel)t.enabled=false;
            TextPlace(home.lifeLabel,.08f,.398f,.33f,.778f,31);
            // Settings: the blue dial inside a gold outer rim, as in the reference.
            var settings=home.resourceButtons[4].transform;var ring=Surface("Settings gold rim",settings,new Color(1,.88f,.4f),new Color(.84f,.5f,.07f));Anchor(ring.rectTransform,.013f,.164f,.987f,.844f);ring.cornerRadius=1000;Shade(ring);
            Pair(settings,17072,.066f,.201f,.934f,.807f,false,true);
            Hide(home.levelButton.transform);Hide(home.areaButton.transform);
            ActionFace(home.levelButton.transform,21955);
            TextPlace(home.levelLabel,.06f,.1f,.94f,.91f,45);
            ActionFace(home.areaButton.transform,17031);
            var chest=Pic(home.areaButton.transform,16593,.60f,.17f,.92f,.84f);chest.preserveAspect=true;
            Pair(home.areaButton.transform,17034,.10f,.24f,.61f,.47f,true);
            // Existing progress amount controls the imported green track.
            fill=Pic(home.areaFill.parent,16395,0,0,1,1);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
            Bounds((RectTransform)home.areaFill.parent,.10f,.24f,.61f,.47f);home.areaFill.parent.SetAsLastSibling();
            TextPlace(home.areaLabel,.08f,.49f,.65f,.86f,32);home.areaLabel.color=new Color(1,.97f,.83f);
            // A warm drop shadow keeps the title readable on the orange face, like the level title on green.
            if(!home.areaLabel.GetComponent<Shadow>()){var title=home.areaLabel.gameObject.AddComponent<Shadow>();title.effectColor=new Color(.42f,.18f,0,.7f);title.effectDistance=new Vector2(1.5f,-2);}TextPlace(home.areaProgress,0,0,1,1,25);
            Hide(home.navigationBackground);Pic(home.navigationBackground,21453,0,0,1,1,true);
            Hide(home.navigation);Pic(home.navigation,21453,0,0,1,1,true).transform.SetAsFirstSibling();
            // The reference bar deepens from royal blue to navy under the imported gold edge.
            navDepth=Surface("Royal navigation depth",home.navigation,new Color(0,.33f,.72f),new Color(0,.21f,.52f));Anchor(navDepth.rectTransform,0,0,1,1);navDepth.transform.SetSiblingIndex(1);
            tabArt=new Image[5];tabRim=new RoyalPanelGraphic[5];tabFace=new RoyalPanelGraphic[5];tabLine=new RoyalPanelGraphic[5];emphasis=new float[5];
            int[] ids={16927,22238,16525,17106,16492};string[] names={"Events","Tournaments","Home","Team","Collection"};
            for(int i=0;i<5;i++){var tab=home.tabButtons[i].transform;
                tabArt[i]=Pic(tab,ids[i],.13f,.20f,.87f,.94f);tabArt[i].preserveAspect=true;home.tabButtons[i].GetComponent<Image>().enabled=true;home.tabButtons[i].GetComponent<Image>().color=Color.clear;
                tabRim[i]=Surface("Selected tab rim",tab,new Color(.36f,.64f,1,.9f),new Color(.14f,.40f,.82f,0));tabRim[i].transform.SetSiblingIndex(0);
                tabFace[i]=Surface("Selected tab panel",tab,new Color(.01f,.35f,.76f),new Color(0,.22f,.52f));tabFace[i].transform.SetSiblingIndex(1);
                tabLine[i]=Surface("Selected tab underline",tab,new Color(1,.87f,.36f),new Color(.93f,.55f,.08f));
                foreach(var g in new[]{tabRim[i],tabFace[i],tabLine[i]}){var r=g.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);}
                // The reference names every tab under its icon; the events book and trophy art name the first two.
                var label=tab.GetComponentInChildren<Text>(true);label.text=names[i];label.alignment=TextAnchor.MiddleCenter;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;
                // A plain Shadow, not an Outline (which is a Shadow too).
                bool shaded=false;foreach(var s in label.GetComponents<Shadow>())if(s.GetType()==typeof(Shadow)){shaded=true;s.enabled=true;}
                if(!shaded){var s=label.gameObject.AddComponent<Shadow>();s.effectColor=new Color(.02f,.08f,.25f,.6f);s.effectDistance=new Vector2(0,-1.5f);}}
        }
        // R-22: a tall dialog (Settings, Story) is centred in the safe area. On squat screens (4:3 tablets) it reached under the
        // resource bar and down into the raised tab art, across its bottom button. Where it does not fit, shorten and move it so the
        // button clears the art and the dialog, close control included, stays below the bar. Where it fits (16:9, 20:9) nothing moves.
        static float Edge(RectTransform r,RectTransform space,bool top){var c=new Vector3[4];r.GetWorldCorners(c);float a=space.InverseTransformPoint(c[0]).y,b=space.InverseTransformPoint(c[1]).y;return top?Mathf.Max(a,b):Mathf.Min(a,b);}
        // The layout's height for the open dialog (any height not set here) and the last one set here: the 60% minimum and the
        // upper limit come from the layout's height, so a dialog that cannot fit even at 60% stays there instead of shrinking again.
        float designHeight,fittedHeight=float.NaN;
        void FitDialog(){var d=home.dialogRect;if(!d||!home.dialog||!home.dialog.activeInHierarchy||!home.dialogButton)return;var p=(RectTransform)d.parent;const float gap=8;
            float floor=Edge(tabArt[home.SelectedTab].rectTransform,p,true)+gap,ceiling=Edge(home.hud,p,false)-gap,f=((RectTransform)home.dialogButton.transform).anchorMin.y;
            float h=d.rect.height,bottom=p.rect.center.y+d.anchoredPosition.y-h*d.pivot.y,reach=home.closeButton?Mathf.Max(0,Edge((RectTransform)home.closeButton.transform,p,true)-(bottom+h)):0;
            if(d.sizeDelta.y!=fittedHeight)designHeight=h;
            if(bottom+f*h>=floor-.5f&&bottom+h<=ceiling+.5f)return;
            float fit=Mathf.Max(designHeight*.6f,Mathf.Min(designHeight,(ceiling-reach-floor)/(1-f))),y=floor-f*fit+fit*d.pivot.y-p.rect.center.y;
            if(d.sizeDelta.y==fit&&d.anchoredPosition.y==y)return;
            d.sizeDelta=new Vector2(d.sizeDelta.x,fit);d.anchoredPosition=new Vector2(d.anchoredPosition.x,y);fittedHeight=fit;}
        // Never stretched and always covering the screen; at 16:9 this lifts the castle so the card row meets the courtyard as in
        // the reference. Taller screens enlarge it just enough to keep that line and still cover the top.
        void PlaceBackground(float cardTop,float navTop){if(!sky)return;var art=sky.GetComponent<Image>().sprite;if(!art)return;var root=((RectTransform)home.transform).rect;
            float aspect=art.rect.width/art.rect.height,h=Mathf.Max(root.width/aspect,Mathf.Max(root.height,(root.height-cardTop)/(1-CardLine))),bottom=cardTop-CardLine*h;
            // Its lower edge stays hidden behind the tab bar.
            if(bottom>navTop){bottom=navTop;h=Mathf.Max(h,root.height-bottom);}
            sky.anchorMin=sky.anchorMax=new Vector2(.5f,0);sky.pivot=new Vector2(.5f,0);sky.sizeDelta=new Vector2(h*aspect,h);sky.anchoredPosition=new Vector2(0,bottom);
            if(skyDepth)Anchor(skyDepth.rectTransform,0,(root.height*(1-SkyDepth)-bottom)/h,1,(root.height-bottom)/h);}
        // A panel anchored at the tab's bottom centre, reaching below the screen edge so only its top corners show.
        static void Stand(RoyalPanelGraphic g,float width,float top,float below,float radius){g.cornerRadius=radius;g.rectTransform.sizeDelta=new Vector2(width,top+below);g.rectTransform.anchoredPosition=new Vector2(0,-below);}
        void LateUpdate(){if(!built||!home)return;
            float step=!emphasisSet||!home.MotionEnabled?1:1-Mathf.Exp(-19*Time.unscaledDeltaTime);emphasisSet=true;
            for(int i=0;i<5;i++)emphasis[i]=Mathf.Lerp(emphasis[i],i==home.SelectedTab?1:0,step);
            Relayout();
        }
        // Also run by RoyalHomeScreen.RefreshLayout, whose own placement this replaces: code that reads the layout right after a
        // refresh (the Home checks' overlap and tap tests) then sees what is drawn.
        public void Relayout(){if(!built||!home)return;float w=home.layout.rect.width,inset=home.safeArea.anchorMin.y*((RectTransform)home.transform).rect.height;
            home.hud.sizeDelta=new Vector2(w*HudWidth,w*HudHeight);home.hud.anchoredPosition=new Vector2(0,-w*HudCentre);
            for(int i=0;i<5;i++)Anchor((RectTransform)home.resourceButtons[i].transform,ResourceEdges[i*2],0,ResourceEdges[i*2+1],1);
            home.actions.sizeDelta=new Vector2(w*ActionsWidth,w*ActionsHeight);home.actions.anchoredPosition=new Vector2(0,w*ActionsCentre);
            Anchor(home.levelRect,0,0,LevelRight,1);Anchor(home.areaRect,AreaLeft,0,1,1);
            float navH=w*NavHeight;home.navigation.sizeDelta=new Vector2(w,navH);home.navigation.anchoredPosition=new Vector2(0,navH*.5f);
            home.navigationBackground.sizeDelta=new Vector2(0,navH+inset);
            navDepth.rectTransform.offsetMin=new Vector2(0,-inset);navDepth.rectTransform.offsetMax=new Vector2(0,-w*GoldTrim);
            bool motion=home.MotionEnabled&&home.IsHomeVisible;float time=Time.unscaledTime;
            float glow=motion?.5f+.5f*Mathf.Sin(time*Mathf.PI*2/GlowPeriod):1;
            for(int i=0;i<5;i++){var b=home.tabButtons[i];Anchor((RectTransform)b.transform,TabEdges[i],0,TabEdges[i+1],1);
                float t=emphasis[i];
                Vector2 rest=IconRest[i],raised=i==2?HomeRaised:new Vector2(.47f,.47f+1.3f*(rest.y-rest.x));
                Bounds(tabArt[i].rectTransform,0,Mathf.Lerp(rest.x,raised.x,t),1,Mathf.Lerp(rest.y,raised.y,t));
                float panel=Mathf.Min(w*PanelWidth,(TabEdges[i+1]-TabEdges[i])*w*.96f),radius=w*PanelRadius,rim=w*RimWidth;
                Stand(tabFace[i],panel,navH*PanelTop,inset+radius,radius);Stand(tabRim[i],panel+rim*2,navH*PanelTop+rim,inset+radius,radius+rim);
                tabFace[i].color=new Color(1,1,1,t);tabRim[i].color=new Color(1,1,1,t*Mathf.Lerp(.7f,1,glow));
                var line=tabLine[i].rectTransform;tabLine[i].cornerRadius=w*LineHeight*.5f;line.sizeDelta=new Vector2(w*LineWidth,w*LineHeight);line.anchoredPosition=new Vector2(0,navH*LineCentre-w*LineHeight*.5f);
                float shine=Mathf.Lerp(.88f,1,glow);tabLine[i].color=new Color(shine,shine,shine,t);
                int size=Mathf.RoundToInt(Mathf.Lerp(LabelSize,SelectedLabelSize,t));
                foreach(var label in b.GetComponentsInChildren<Text>(true)){label.enabled=true;label.transform.SetAsLastSibling();Bounds(label.rectTransform,.02f,LabelBox.x,.98f,LabelBox.y);label.fontSize=label.resizeTextMaxSize=size;}}
            for(int i=0;i<plus.Length;i++){float s=1;
                if(motion&&!home.dialog.activeSelf){float p=Mathf.Repeat(time-i*PulseStagger,PulsePeriod);if(p<PulseLength)s=1+PulseAmount*Mathf.Sin(p/PulseLength*Mathf.PI);}
                plus[i].localScale=new Vector3(s,s,1);}
            PlaceBackground(inset+w*(ActionsCentre+ActionsHeight*.5f),inset+navH);
            if(fill)fill.fillAmount=home.Progress.areaTotal>0?(float)home.Progress.areaCompleted/home.Progress.areaTotal:0;
            FitDialog();
        }
    }
}
