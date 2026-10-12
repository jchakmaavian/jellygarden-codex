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
        // Counters (coins, lives, stars) in reference pixels (1080 wide, y down from the top of the screen), turned into anchors by
        // Px(). Each is a slim cream pill, 77 px tall with a thin edge, a tan lip and a soft shadow, starting under the icon's
        // centre. The + sits in the pill's round right end, and the value between the icon and the +.
        static readonly Vector4[] IconPx={new Vector4(176.5f,56.5f,282.5f,165.5f),new Vector4(441,64,552,165),new Vector4(721,72,805,151)};
        static readonly Vector4[] ValuePx={new Vector4(288,83,366,146),new Vector4(556,82,646,145),new Vector4(806,82,870,145)};
        // Center the visible digit in the heart; the display font sits below its line-box centre.
        static readonly Vector4 LifeCountPx=new Vector4(461.5f,79.5f,531.5f,140.5f);
        static readonly float[] PillLeft={229,497,763},PillRight={434,714,938};
        const float PillTop=79,PillBottom=156,FaceTop=82,FaceBottom=150,PillEdge=2.5f;
        // Values render at about 32 px, the reference digits' size.
        const int ValueSize=21;
        // The + badge's green disc is 53 px across; in its sprite pair it fills 92% of the width and 85% of the height, centred 45%
        // from the top.
        const float PlusDisc=53,PlusWidth=PlusDisc/.921f,PlusHeight=PlusDisc/.853f,PlusCentre=.453f;
        // Level and area cards: 1014 x 201 px, centred 436 px above the safe bottom; the area card is the wider one.
        // Since 2026-10-05 (the user's reference) the area card is hidden, the star counter opens the task list, and the Level
        // button stands alone in the middle, higher (CentredActions): its bottom edge 12% of the width above the tab bar, as in
        // the reference, so the raised Home castle stays clear below it. The castle scene still lines up with the old row.
        const float ActionsWidth=.9394f,ActionsHeight=.1864f,ActionsCentre=.4040f,LevelRight=.4687f,AreaLeft=.5175f,CentredActions=.4774f;
        // Tab bar: 286 px, the imported strip's gold edge and highlight fill its top 14.3 units (at w 720) and stay visible.
        const float NavHeight=.2652f,GoldTrim=.0199f;
        static readonly float[] TabEdges={0,.17f,.347f,.653f,.81f,1};
        // Icon art bottom and top as fractions of the bar's height, at rest and when the tab is selected. The Home castle rises
        // through the gap between the cards; only its spire reaches above their bottom edge.
        static readonly Vector2[] IconRest={new Vector2(.4295f,.8904f),new Vector2(.4609f,.845f),new Vector2(.4539f,.8659f),new Vector2(.4469f,.8799f),new Vector2(.4539f,.8659f)};
        static readonly Vector2 HomeRaised=new Vector2(.493f,1.2989f);
        static readonly Vector2 LabelBox=new Vector2(.269f,.478f);
        const int LabelSize=16,SelectedLabelSize=24;
        // Selected tab: a rounded, slightly lighter panel with a soft rim. The short gold underline below the label is no longer
        // shown (the user's reference, 2026-10-05).
        const float PanelWidth=.2472f,PanelTop=.9253f,PanelRadius=.037f,RimWidth=.0019f,LineWidth=.1046f,LineHeight=.0093f,LineCentre=.1833f;
        // The card row's top edge sits at this fraction of the background's height, above its bottom: the courtyard in front of the
        // castle stays visible between the cards and the tab bar at every aspect ratio.
        const float CardLine=.246f;
        // The reference sky deepens toward the top, behind the resource bar: a veil over the upper 47% of the screen, under the clouds.
        const float SkyDepth=.47f;
        // The + badges and the selected tab stay still (UI polish, 2026-09-30): no idle swell or breathing on controls; the
        // selected tab reads by its raised art, its panel and rim.
        static Sprite Art(int id)=>Resources.Load<Sprite>("ReferenceGameplay/Home/"+id);
        public static void Attach(RoyalHomeScreen home){var v=home.GetComponent<ReferenceHomeLayout>();if(!v)v=home.gameObject.AddComponent<ReferenceHomeLayout>();v.home=home;v.Build();}
        // 0 in place, 1 off the screen: while an area task builds, the resource bar slides up and the cards and tab bar slide down,
        // as the reference clears the scene for the new object (RoyalAreaView). Applied in Relayout, which places them every frame.
        public float HudAway{get;set;}
        // The win payout's entrance (RoyalWinSequence.HomeRewards): the controls slide in over the area scene, which stays put
        // (HudAway also moves the scene). Below 0 the top bar and the Level row overshoot their place a little.
        public float EntranceAway{get;set;}
        static void Bounds(RectTransform r,float x0,float y0,float x1,float y1){Anchor(r,x0,y0,x1,y1);r.localScale=Vector3.one;}
        // Buttons keep their scale: RoyalButtonMotion owns it for the press feedback.
        static void Anchor(RectTransform r,float x0,float y0,float x1,float y1){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
        static void Bounds(RectTransform r,Vector4 b)=>Bounds(r,b.x,b.y,b.z,b.w);
        // A soft drop shadow gives the resource art the reference's depth against the sky.
        static void Shade(Graphic g){var s=g.gameObject.AddComponent<Shadow>();s.effectColor=new Color(.02f,.1f,.3f,.35f);s.effectDistance=new Vector2(0,-3);}
        static void Shade(Transform parent,string name){foreach(Transform c in parent)if(c.name==name)Shade(c.GetComponent<Graphic>());}
        // Reference pixels -> anchors inside resource control i: x from the bar's left edge, y down from the top of the screen.
        static Vector4 Px(int i,Vector4 p){const float W=1080;float barW=HudWidth*W,barH=HudHeight*W,top=HudCentre*W-barH*.5f,left=(W-barW)*.5f+ResourceEdges[i*2]*barW,width=(ResourceEdges[i*2+1]-ResourceEdges[i*2])*barW;
            return new Vector4((p.x-left)/width,(top+barH-p.w)/barH,(p.z-left)/width,(top+barH-p.y)/barH);}
        static RoyalPanelGraphic PillLayer(Transform parent,int i,string name,Vector4 p,Color top,Color bottom){var g=Surface(name,parent,top,bottom);Bounds(g.rectTransform,Px(i,p));g.cornerRadius=1000;return g;}
        // The counter's cream pill, drawn to the reference's profile rather than the imported panel sprite, whose deep lower bevel
        // made the bar a third taller than the reference.
        static void Pill(Transform parent,int i,float left,float right){
            PillLayer(parent,i,"Counter shadow far",new Vector4(left,PillTop+10,right,PillBottom+10),new Color(0,.06f,.2f,.3f),new Color(0,.06f,.2f,.3f));
            PillLayer(parent,i,"Counter shadow",new Vector4(left,PillTop+5,right,PillBottom+5),new Color(0,.06f,.18f,.55f),new Color(0,.06f,.18f,.55f));
            PillLayer(parent,i,"Counter edge",new Vector4(left,PillTop,right,PillBottom),new Color(.60f,.49f,.37f),new Color(.70f,.53f,.35f));
            PillLayer(parent,i,"Counter face",new Vector4(left+PillEdge,FaceTop,right-PillEdge,FaceBottom),new Color(.985f,.965f,.91f),new Color(.895f,.815f,.66f));
            PillLayer(parent,i,"Counter shine",new Vector4(left+14,FaceTop+2,right-14,FaceTop+20),new Color(1,1,1,.55f),new Color(1,1,1,0));}
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
            // Home's info dialogs speak in the game's display type, like every card (UI polish, 2026-09-30): the subtitle and the
            // detail were the default font. Navy on the cream; best fit still shrinks a long line.
            var display=Resources.Load<Font>("RoyalGameplay/Display");
            if(display){if(home.dialogCopy){home.dialogCopy.font=display;home.dialogCopy.color=new Color(.07f,.2f,.49f);}if(home.dialogDetail){home.dialogDetail.font=display;home.dialogDetail.color=new Color(.14f,.25f,.47f);}}
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
            for(int i=1;i<=3;i++){var b=home.resourceButtons[i].transform;int k=i-1;Pill(b,i,PillLeft[k],PillRight[k]);
                var im=Pic(b,icons[k],Px(i,IconPx[k]));im.preserveAspect=true;Shade(im);TextPlace(values[k],Px(i,ValuePx[k]),ValueSize);
                // Every counter carries the same + badge. It is art inside the counter, so a tap still opens that counter's panel.
                float cx=PillRight[k]-(PillBottom-PillTop)*.5f,cy=(PillTop+PillBottom)*.5f;
                var badge=new GameObject("Plus badge",typeof(RectTransform)).GetComponent<RectTransform>();badge.SetParent(b,false);
                Bounds(badge,Px(i,new Vector4(cx-PlusWidth*.5f,cy-PlusHeight*PlusCentre,cx+PlusWidth*.5f,cy+PlusHeight*(1-PlusCentre))));Pair(badge,16282,0,0,1,1);plus[k]=badge;}
            foreach(var t in home.resourceButtons[1].GetComponentsInChildren<Text>(true))if(t!=home.coinLabel)t.enabled=false;
            TextPlace(home.lifeLabel,Px(2,LifeCountPx),31);
            // Settings: the blue dial inside a gold outer rim, as in the reference.
            var settings=home.resourceButtons[4].transform;var ring=Surface("Settings gold rim",settings,new Color(1,.88f,.4f),new Color(.84f,.5f,.07f));Anchor(ring.rectTransform,.013f,.164f,.987f,.844f);ring.cornerRadius=1000;Shade(ring);
            Pair(settings,17072,.066f,.201f,.934f,.807f,false,true);
            Hide(home.levelButton.transform);Hide(home.areaButton.transform);
            ActionFace(home.levelButton.transform,21955);
            // The display face's capitals sit below the middle of their line box, so a centred box drew "Level 43" low on the
            // button (the user's screenshot, 2026-10-06: 38 px of face above the words, 26 below). The box is raised by 5.5%.
            TextPlace(home.levelLabel,.06f,.155f,.94f,.965f,45);
            ActionFace(home.areaButton.transform,17031);
            var chest=Pic(home.areaButton.transform,16593,.60f,.17f,.92f,.84f);chest.preserveAspect=true;
            Pair(home.areaButton.transform,17034,.10f,.24f,.61f,.47f,true);
            // Existing progress amount controls the imported green track.
            fill=Pic(home.areaFill.parent,16395,0,0,1,1);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
            Bounds((RectTransform)home.areaFill.parent,.10f,.24f,.61f,.47f);home.areaFill.parent.SetAsLastSibling();
            TextPlace(home.areaLabel,.08f,.49f,.65f,.86f,32);home.areaLabel.color=new Color(1,.97f,.83f);
            // A warm drop shadow keeps the title readable on the orange face, like the level title on green.
            if(!home.areaLabel.GetComponent<Shadow>()){var title=home.areaLabel.gameObject.AddComponent<Shadow>();title.effectColor=new Color(.42f,.18f,0,.7f);title.effectDistance=new Vector2(1.5f,-2);}TextPlace(home.areaProgress,0,0,1,1,25);
            // The reference's Home has no area card (the user's decision, 2026-10-05): the star counter opens the task list.
            home.areaButton.gameObject.SetActive(false);
            Hide(home.navigationBackground);Pic(home.navigationBackground,21453,0,0,1,1,true);
            Hide(home.navigation);Pic(home.navigation,21453,0,0,1,1,true).transform.SetAsFirstSibling();
            // The reference bar deepens from royal blue to navy under the imported gold edge.
            navDepth=Surface("Royal navigation depth",home.navigation,new Color(0,.33f,.72f),new Color(0,.21f,.52f));Anchor(navDepth.rectTransform,0,0,1,1);navDepth.transform.SetSiblingIndex(1);
            tabArt=new Image[5];tabRim=new RoyalPanelGraphic[5];tabFace=new RoyalPanelGraphic[5];tabLine=new RoyalPanelGraphic[5];emphasis=new float[5];
            // The first tab is the Areas list (the user's decision of 2026-09-28, as in the reference); the book art names it.
            int[] ids={16927,22238,16525,17106,16492};string[] names={"Areas","Leaderboard","Home","Team","Collection"};
            for(int i=0;i<5;i++){var tab=home.tabButtons[i].transform;
                tabArt[i]=Pic(tab,ids[i],.13f,.20f,.87f,.94f);tabArt[i].preserveAspect=true;home.tabButtons[i].GetComponent<Image>().enabled=true;home.tabButtons[i].GetComponent<Image>().color=Color.clear;
                tabRim[i]=Surface("Selected tab rim",tab,new Color(.36f,.64f,1,.9f),new Color(.14f,.40f,.82f,0));tabRim[i].transform.SetSiblingIndex(0);
                tabFace[i]=Surface("Selected tab panel",tab,new Color(.01f,.35f,.76f),new Color(0,.22f,.52f));tabFace[i].transform.SetSiblingIndex(1);
                tabLine[i]=Surface("Selected tab underline",tab,new Color(1,.87f,.36f),new Color(.93f,.55f,.08f));
                foreach(var g in new[]{tabRim[i],tabFace[i],tabLine[i]}){var r=g.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,0);r.pivot=new Vector2(.5f,0);}
                tabLine[i].gameObject.SetActive(false);
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
        void PlaceBackground(float cardTop,float navTop,float away=0){if(!sky)return;var art=sky.GetComponent<Image>().sprite;if(!art)return;var root=((RectTransform)home.transform).rect;
            float aspect=art.rect.width/art.rect.height,h=Mathf.Max(root.width/aspect,Mathf.Max(root.height,(root.height-cardTop)/(1-CardLine))),bottom=cardTop-CardLine*h;
            // Its lower edge stays hidden behind the tab bar.
            if(bottom>navTop){bottom=navTop;h=Mathf.Max(h,root.height-bottom);}
            // With the HUD and the tab bar put away (a restored area viewed full screen) nothing hides the lower edge: the scene
            // moves down to the screen's bottom edge. It is at least the screen's height, so its top still covers the screen.
            if(away>0){float full=Mathf.Min(bottom,0);bottom=Mathf.Lerp(bottom,full,away);h=Mathf.Max(h,root.height-bottom);}
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
            float away=Mathf.Clamp01(HudAway),slide=Mathf.Clamp(away+EntranceAway,-.3f,1),top=(1-home.safeArea.anchorMax.y)*((RectTransform)home.transform).rect.height;
            home.hud.sizeDelta=new Vector2(w*HudWidth,w*HudHeight);home.hud.anchoredPosition=new Vector2(0,-w*HudCentre+slide*(w*(HudCentre+HudHeight)+top+24));
            for(int i=0;i<5;i++)Anchor((RectTransform)home.resourceButtons[i].transform,ResourceEdges[i*2],0,ResourceEdges[i*2+1],1);
            float navH=w*NavHeight,drop=slide*(w*(CentredActions+ActionsHeight)+inset+24);
            home.actions.sizeDelta=new Vector2(w*ActionsWidth,w*ActionsHeight);home.actions.anchoredPosition=new Vector2(0,w*CentredActions-drop);
            Anchor(home.levelRect,.5f-LevelRight*.5f,0,.5f+LevelRight*.5f,1);Anchor(home.areaRect,AreaLeft,0,1,1);
            float sink=Mathf.Max(0,slide)*(navH*1.35f+inset+24);home.navigation.sizeDelta=new Vector2(w,navH);home.navigation.anchoredPosition=new Vector2(0,navH*.5f-sink);
            // The bar's blue base stays for the win entrance: the area scene ends above it, so it covers what is under the tabs.
            home.navigationBackground.sizeDelta=new Vector2(0,navH+inset);home.navigationBackground.anchoredPosition=new Vector2(home.navigationBackground.anchoredPosition.x,-away*(navH*1.35f+inset+24));
            navDepth.rectTransform.offsetMin=new Vector2(0,-inset);navDepth.rectTransform.offsetMax=new Vector2(0,-w*GoldTrim);
            const float glow=1;
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
            for(int i=0;i<plus.Length;i++)if(plus[i].localScale!=Vector3.one)plus[i].localScale=Vector3.one;
            PlaceBackground(inset+w*(ActionsCentre+ActionsHeight*.5f),inset+navH,away);
            if(fill)fill.fillAmount=home.Progress.areaTotal>0?(float)home.Progress.areaCompleted/home.Progress.areaTotal:0;
            FitDialog();
        }
    }
}
