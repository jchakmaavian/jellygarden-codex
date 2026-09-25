using JellyGarden.Scripts;
using RoyalThrone.Presentation;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay
{
    // Packaged home artwork, adapted to the existing safe-area canvas and live callbacks.
    // The castle/background, logo, dialogs and player resources remain owned by RoyalHomeScreen.
    [DefaultExecutionOrder(10000)]
    public sealed class ReferenceHomeLayout : MonoBehaviour
    {
        RoyalHomeScreen home; Image[] tabArt; Image fill; bool built;
        static Sprite Art(int id)=>Resources.Load<Sprite>("ReferenceGameplay/Home/"+id);
        public static void Attach(RoyalHomeScreen home){var v=home.GetComponent<ReferenceHomeLayout>();if(!v)v=home.gameObject.AddComponent<ReferenceHomeLayout>();v.home=home;v.Build();}
        static void Bounds(RectTransform r,float x0,float y0,float x1,float y1){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
        static Image Pic(Transform parent,int id,float x0,float y0,float x1,float y1,bool sliced=false){var r=new GameObject("Imported "+id,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);Bounds(r,x0,y0,x1,y1);var im=r.gameObject.AddComponent<Image>();im.sprite=Art(id);im.raycastTarget=false;im.type=sliced?Image.Type.Sliced:Image.Type.Simple;im.pixelsPerUnitMultiplier=2;return im;}
        static void Pair(Transform p,int id,float x0,float y0,float x1,float y1,bool slice=false,bool rightHalf=false){float m=(x0+x1)*.5f;var a=Pic(p,id,x0,y0,m,y1,slice);var b=Pic(p,id,m,y0,x1,y1,slice);(rightHalf?a:b).rectTransform.localScale=new Vector3(-1,1,1);}
        // A hidden button keeps its own graphic as a clear hit area: a disabled target graphic takes no taps, which left
        // the profile and area buttons (panel graphics, not Images) dead to touch.
        static void Hide(Transform t){foreach(var g in t.GetComponentsInChildren<Graphic>(true))if(!(g is Text))g.enabled=false;var hit=t.GetComponent<Image>();if(hit){hit.enabled=true;hit.color=Color.clear;}
            else if(t.GetComponent<Selectable>()){var own=t.GetComponent<Graphic>();if(own&&!(own is Text)){own.enabled=true;own.color=Color.clear;}}}
        static void TextPlace(Text t,float x0,float y0,float x1,float y1,int size){t.enabled=true;Bounds(t.rectTransform,x0,y0,x1,y1);t.transform.SetAsLastSibling();t.fontSize=t.resizeTextMaxSize=size;t.resizeTextMinSize=15;t.resizeTextForBestFit=true;}
        static void ActionFace(Transform parent,int face){
            // Overlap the inset beneath the trim so packed sprite edges never leave a gap.
            Pair(parent,face,.016f,.045f,.984f,.98f,true);
            Pair(parent,16521,0,0,1,1,true);
        }
        void Build(){if(built||!home)return;built=true;
            ReferenceHomeSky.Attach(home);
            if(home.logo)home.logo.gameObject.SetActive(false);
            foreach(var b in home.resourceButtons)Hide(b.transform);
            // Keep the Royal Throne identity inside the profile; use reference resource artwork.
            var portrait=home.resourceButtons[0].transform.Find("King portrait").GetComponent<Image>();portrait.enabled=true;portrait.color=Color.white;
            // The bust is framed inside the aperture; gold trim is drawn over the portrait.
            var portraitBack=Pic(home.resourceButtons[0].transform,16613,.075f,.085f,.925f,.95f);
            portraitBack.name="Portrait blue inset";portraitBack.sprite=null;portraitBack.color=new Color(.065f,.23f,.52f);portraitBack.transform.SetAsFirstSibling();
            portrait.transform.SetAsLastSibling();Bounds(portrait.rectTransform,.13f,.12f,.87f,.91f);portrait.preserveAspect=true;
            Pair(home.resourceButtons[0].transform,16613,0,0,1,1,true);
            int[] icons={22081,21591,16674};Text[] values={home.coinLabel,home.lifeStatus,home.starLabel};
            for(int i=1;i<=3;i++){var b=home.resourceButtons[i].transform;Pair(b,21706,.13f,.2f,1,.83f,true,true);var im=Pic(b,icons[i-1],-.07f,.04f,.43f,.97f);im.preserveAspect=true;TextPlace(values[i-1],.38f,.23f,.97f,.79f,30);}
            foreach(var t in home.resourceButtons[1].GetComponentsInChildren<Text>(true))if(t!=home.coinLabel)t.enabled=false;
            Pair(home.resourceButtons[1].transform,16282,.21f,-.03f,.44f,.34f,false);
            TextPlace(home.lifeLabel,-.02f,.17f,.38f,.81f,30);
            Pair(home.resourceButtons[4].transform,17072,0,.10f,1,.90f,false,true);
            Hide(home.levelButton.transform);Hide(home.areaButton.transform);
            ActionFace(home.levelButton.transform,21955);
            TextPlace(home.levelLabel,.06f,.1f,.94f,.91f,45);
            ActionFace(home.areaButton.transform,17031);
            var chest=Pic(home.areaButton.transform,16593,.60f,.17f,.92f,.84f);chest.preserveAspect=true;
            Pair(home.areaButton.transform,17034,.10f,.24f,.61f,.47f,true);
            // Existing progress amount controls the imported green track.
            fill=Pic(home.areaFill.parent,16395,0,0,1,1);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
            Bounds((RectTransform)home.areaFill.parent,.10f,.24f,.61f,.47f);home.areaFill.parent.SetAsLastSibling();
            TextPlace(home.areaLabel,.08f,.49f,.65f,.86f,32);home.areaLabel.color=new Color(1,.97f,.83f);TextPlace(home.areaProgress,0,0,1,1,25);
            Hide(home.navigationBackground);Pic(home.navigationBackground,21453,0,0,1,1,true);
            Hide(home.navigation);Pic(home.navigation,21453,0,0,1,1,true).transform.SetAsFirstSibling();
            tabArt=new Image[5];int[] ids={16927,22238,16525,17106,16492};
            for(int i=0;i<5;i++){tabArt[i]=Pic(home.tabButtons[i].transform,ids[i],.13f,.20f,.87f,.94f);tabArt[i].preserveAspect=true;home.tabButtons[i].GetComponent<Image>().enabled=true;home.tabButtons[i].GetComponent<Image>().color=Color.clear;}
        }
        // R-22: a tall dialog (Settings, Story) is centred in the safe area. On squat screens (4:3 tablets) it reached under the
        // resource bar and down into the raised tab art, across its bottom button. Where it does not fit, shorten and move it so the
        // button clears the art and the dialog, close control included, stays below the bar. Where it fits (16:9, 20:9) nothing moves.
        static float Edge(RectTransform r,RectTransform space,bool top){var c=new Vector3[4];r.GetWorldCorners(c);float a=space.InverseTransformPoint(c[0]).y,b=space.InverseTransformPoint(c[1]).y;return top?Mathf.Max(a,b):Mathf.Min(a,b);}
        void FitDialog(){var d=home.dialogRect;if(!d||!home.dialog||!home.dialog.activeInHierarchy||!home.dialogButton)return;var p=(RectTransform)d.parent;const float gap=8;
            float floor=Edge(tabArt[home.SelectedTab].rectTransform,p,true)+gap,ceiling=Edge(home.hud,p,false)-gap,f=((RectTransform)home.dialogButton.transform).anchorMin.y;
            float h=d.rect.height,bottom=p.rect.center.y+d.anchoredPosition.y-h*d.pivot.y,reach=home.closeButton?Mathf.Max(0,Edge((RectTransform)home.closeButton.transform,p,true)-(bottom+h)):0;
            if(bottom+f*h>=floor-.5f&&bottom+h<=ceiling+.5f)return;
            float fit=Mathf.Max(h*.6f,Mathf.Min(h,(ceiling-reach-floor)/(1-f)));
            d.sizeDelta=new Vector2(d.sizeDelta.x,fit);d.anchoredPosition=new Vector2(d.anchoredPosition.x,floor-f*fit+fit*d.pivot.y-p.rect.center.y);}
        void LateUpdate(){if(!built||!home)return;float w=home.layout.rect.width;
            home.hud.sizeDelta=new Vector2(w*.95f,w*.16f);home.hud.anchoredPosition=new Vector2(0,-w*.115f);
            float[] edges={0,.15f,.435f,.70f,.865f,1};for(int i=0;i<5;i++)Bounds((RectTransform)home.resourceButtons[i].transform,edges[i],0,edges[i+1]-.01f,1);
            home.actions.sizeDelta=new Vector2(w*.92f,w*.195f);home.actions.anchoredPosition=new Vector2(0,w*.34f);
            Bounds(home.levelRect,0,0,.475f,1);Bounds(home.areaRect,.525f,0,1,1);
            home.navigation.sizeDelta=new Vector2(w,w*.19f);home.navigation.anchoredPosition=new Vector2(0,w*.095f);
            home.navigationBackground.sizeDelta=new Vector2(0,w*.19f);
            float[] nav={0,1/6f,2/6f,4/6f,5/6f,1};
            for(int i=0;i<5;i++){var b=home.tabButtons[i];Bounds((RectTransform)b.transform,nav[i],0,nav[i+1],1);bool selected=i==home.SelectedTab;Bounds(tabArt[i].rectTransform,i==2?.20f:.10f,selected?.34f:.18f,i==2?.80f:.90f,selected?1.40f:.91f);foreach(var t in b.GetComponentsInChildren<Text>(true)){t.enabled=true;t.transform.SetAsLastSibling();}}
            if(fill)fill.fillAmount=home.Progress.areaTotal>0?(float)home.Progress.areaCompleted/home.Progress.areaTotal:0;
            FitDialog();
        }
    }
}
