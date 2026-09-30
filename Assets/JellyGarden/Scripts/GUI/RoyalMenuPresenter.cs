using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using UnityEngine;
using UnityEngine.UI;
using JellyGarden.Scripts.Targets;
using JellyGarden.Scripts.Juice;
using System.Collections.Generic;

namespace RoyalThrone.Presentation
{
    // Target instances are created after the menu enables; keep their live fields intact.
    public sealed class RoyalMenuPresenter : MonoBehaviour
    {
        float next;
        AnimationClip retryClip,prePlayClip;
        public void ConfigurePrePlay()
        {
            var root=transform as RectTransform;var banner=transform.Find("Banner");if(!root||!banner)return;
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            // Keep the castle visible throughout the transition. The inherited scene
            // had no fullscreen background here, leaving a black frame around its banner.
            var castle=RoyalMenuSkin.Art(root,"Royal castle journey","home-castle",0,0,720,1280);
            var cr=castle.rectTransform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=cr.offsetMax=Vector2.zero;castle.preserveAspect=false;
            var fitter=castle.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            if(castle.sprite)fitter.aspectRatio=castle.sprite.rect.width/castle.sprite.rect.height;
            castle.transform.SetAsFirstSibling();
            var veil=new GameObject("Royal journey shade",typeof(RectTransform),typeof(Image));veil.transform.SetParent(root,false);veil.transform.SetSiblingIndex(1);
            var vr=(RectTransform)veil.transform;vr.anchorMin=Vector2.zero;vr.anchorMax=Vector2.one;vr.offsetMin=vr.offsetMax=Vector2.zero;veil.GetComponent<Image>().color=new Color(.015f,.06f,.17f,.5f);veil.GetComponent<Image>().raycastTarget=false;
            foreach(var group in GetComponentsInChildren<TargetsGroup>(true))group.transform.SetParent(banner,false);
            foreach(Transform child in root)if(child!=banner&&child!=castle.transform&&child!=veil.transform)Hide(child);
            foreach(Transform child in banner)if(!child.GetComponent<TargetsGroup>())Hide(child);
            // The goals card, built like every other card (RoyalCardKit), over the castle.
            RoyalCards.Journey(root,banner);
            var animation=GetComponent<Animation>();if(!animation||!animation.clip)return;
            prePlayClip=Instantiate(animation.clip);prePlayClip.name="Royal goals journey";prePlayClip.ClearCurves();
            // This card sits on the board's canvas; the other cards on the popup canvas. Both match the screen width, so a fixed
            // ratio of their widths (720 / 800) shows it at the same size on screen as every other card.
            var board=GetComponentInParent<CanvasScaler>(true);var menus=JellyGarden.Scripts.GUI.MenuManager.Instance;
            var popup=menus?menus.GetCanvasGlobal().GetComponentInParent<CanvasScaler>(true):null;
            float k=board&&board.uiScaleMode==CanvasScaler.ScaleMode.ScaleWithScreenSize&&board.matchWidthOrHeight==0?board.referenceResolution.x/(popup?popup.referenceResolution.x:800):1;
            // UI polish (2026-09-30): the goals were just shown on the Level Start card, so this reminder holds about 1.1 s (it
            // leaves at 1.35 s, 0.35 s sooner), and a tap once it is in skips to its exit. Same events, moved with the exit:
            // the leaving swish, and OnFinished (which starts the level) once the card has faded.
            CalmCard(prePlayClip,transform,"Banner",PrePlayLeave,new[]{(.05f,new[]{"Banner/Royal heading"}),(.10f,new[]{"Banner/Royal goals panel","Banner/Royal live goals"}),(.15f,new[]{"Banner/Royal journey caption"})},0,k);
            prePlayClip.events=new[]{new AnimationEvent{time=.18f,functionName="SwishSound"},new AnimationEvent{time=PrePlayLeave+.10f,functionName="SwishSound"},new AnimationEvent{time=PrePlayLeave+Leave+.08f,functionName="OnFinished"}};
            animation.AddClip(prePlayClip,prePlayClip.name);animation.clip=prePlayClip;
        }
        const float PrePlayLeave=1.35f,SkipAfter=.35f;
        // A tap on the goals reminder, once it is in, jumps to its exit (the events after that point still fire as it plays on).
        void Update()
        {
            if(!prePlayClip||name!="PrePlay")return;
            var animation=GetComponent<Animation>();var state=animation?animation[prePlayClip.name]:null;
            if(state==null||!animation.IsPlaying(prePlayClip.name)||state.time<SkipAfter||state.time>=PrePlayLeave)return;
            if(InputManager.Instance!=null&&InputManager.Instance.WasTouchPressed)state.time=PrePlayLeave;
        }
        static void Hide(Transform tr){var cg=tr.GetComponent<CanvasGroup>();if(!cg)cg=tr.gameObject.AddComponent<CanvasGroup>();cg.alpha=0;cg.blocksRaycasts=false;}
        // The cards' calm motion (RoyalCardMotion's timings) on a legacy clip, for the two cards whose clip carries their timing:
        // the card scales 0.96 -> 1 and fades in over 0.22 s (ease-out cubic, no overshoot); each row fades in and rises 8 units
        // over 0.20 s at its delay; from `leave` all of it fades out over 0.30 s while the card eases to 0.98. With `dim` the
        // owner's backdrop fades in to that opacity with the card and out with it.
        const float CardIn=.22f,RowIn=.20f,Rise=8,Leave=.30f;
        static void CalmCard(AnimationClip clip,Transform owner,string card,float leave,(float delay,string[] paths)[] rows,float dim=0,float size=1)
        {
            float end=leave+Leave;
            if(dim>0&&owner.GetComponent<Image>())
                clip.SetCurve("",typeof(Image),"m_Color.a",new AnimationCurve(new Keyframe(0,0,0,3*dim/CardIn),new Keyframe(CardIn,dim,0,0),new Keyframe(leave,dim,0,0),new Keyframe(end,0,0,0)));
            var scale=new AnimationCurve(new Keyframe(0,.96f*size,0,3*.04f*size/CardIn),new Keyframe(CardIn,size,0,0),new Keyframe(leave,size,0,0),new Keyframe(end,.98f*size,0,0));
            clip.SetCurve(card,typeof(Transform),"localScale.x",scale);clip.SetCurve(card,typeof(Transform),"localScale.y",scale);
            Row(clip,owner,card,0,CardIn,leave,end,0);
            foreach(var row in rows)foreach(var path in row.paths)Row(clip,owner,path,row.delay,RowIn,leave,end,Rise);
        }
        static void Row(AnimationClip clip,Transform owner,string path,float delay,float time,float leave,float end,float rise)
        {
            var t=owner.Find(path) as RectTransform;if(!t)return;
            var group=t.GetComponent<CanvasGroup>();if(group&&group.alpha<=0)return;   // hidden on purpose
            if(!group)t.gameObject.AddComponent<CanvasGroup>();
            var fade=new List<Keyframe>();if(delay>0)fade.Add(new Keyframe(0,0,0,0));
            fade.Add(new Keyframe(delay,0,0,3/time));fade.Add(new Keyframe(delay+time,1,0,0));fade.Add(new Keyframe(leave,1,0,0));fade.Add(new Keyframe(end,0,0,0));
            clip.SetCurve(path,typeof(CanvasGroup),"m_Alpha",new AnimationCurve(fade.ToArray()));
            if(rise<=0)return;
            float y=t.anchoredPosition.y;var up=new List<Keyframe>();if(delay>0)up.Add(new Keyframe(0,y-rise,0,0));
            up.Add(new Keyframe(delay,y-rise,0,3*rise/time));up.Add(new Keyframe(delay+time,y,0,0));
            clip.SetCurve(path,typeof(RectTransform),"m_AnchoredPosition.y",new AnimationCurve(up.ToArray()));
        }
        public void ConfigureRetryMotion()
        {
            var animation=GetComponent<Animation>();if(!animation)return;
            var original=animation.GetClip("bannerFailed");if(!original)return;
            // Keep the wait/continue/finish events (the clip holds at WaitForGiveUp until the player chooses, then continues)
            // with the cards' calm entrance; the card fades out as soon as the choice is made.
            retryClip=Instantiate(original);retryClip.name="bannerFailed";retryClip.ClearCurves();
            float hold=2.05f;foreach(var e in original.events)if(e.functionName=="WaitForGiveUp")hold=e.time;
            CalmCard(retryClip,transform,"Banner",hold+.01f,new[]{(.05f,new[]{"Banner/Royal heading","Banner/Royal retry text"}),(.10f,new[]{"Banner/Royal goals panel","Banner/Royal live goals","Banner/Targets"}),(.15f,new[]{"Buy"}),(.20f,new[]{"GiveUp","Video"})},RoyalCardKit.DimColor.a);
            // The inherited OnFinished (3.03 s) came 0.67 s after this card has faded (hold + 0.31 s), and until it fires the board
            // ignores swipes after CONTINUE and the Level Failed card waits after END LEVEL: it now fires as the fade ends.
            var events=retryClip.events;
            for(int i=0;i<events.Length;i++)if(events[i].functionName=="OnFinished")events[i].time=Mathf.Min(events[i].time,hold+.01f+Leave+.02f);
            retryClip.events=events;
            animation.AddClip(retryClip,"bannerFailed");animation.clip=retryClip;
        }
        void OnDestroy(){if(retryClip)Destroy(retryClip);if(prePlayClip)Destroy(prePlayClip);}
        void OnEnable()
        {
            next=0;
            var level=JellyGarden.Scripts.LevelManager.THIS;
            if(name=="PreFailed"&&level){
                var animation=GetComponent<Animation>();
                if(animation&&animation["bannerFailed"]!=null)animation["bannerFailed"].speed=1;
                var message=transform.Find("Banner/Royal retry text");
                if(message)message.GetComponent<Text>().text=level.limitType==JellyGarden.Scripts.LIMIT.MOVES
                    ?level.ExtraFailedMoves+" extra moves to finish your goals"
                    :level.ExtraFailedSecs+" extra seconds to finish your goals";
                var number=transform.Find("Banner/Royal level number");
                if(number)number.GetComponent<Text>().text=level.currentLevel.ToString();
            }
            // The fail card carries its level on the ribbon ("Level 18").
            if(name=="MenuFailed"&&level){var title=transform.Find("Image/Royal ribbon/Royal heading");if(title)title.GetComponent<Text>().text="Level "+level.currentLevel;}
        }
        void OnDisable(){if(name=="PreFailed")RoyalCards.ReleaseOfferGoals(transform);if(name=="MenuFailed")RoyalCards.ReleaseFailedGoals(transform);}
        readonly List<TargetIcon> offerGoals=new List<TargetIcon>();
        void LateUpdate()
        {
            // How to play builds each lesson page the first time it is shown: style it before that frame is drawn.
            if(name=="Tutorial"){var card=transform.Find("Image");if(card)for(int i=0;i<RoyalLessonDemo.Count;i++)RoyalCards.Lesson(card.Find("Royal lesson "+i));}
            // The offer's goals show what is left, not TargetIcon's X: every frame, before it is drawn.
            if(name=="PreFailed")RoyalCards.OfferGoals(transform,offerGoals);
            if(name=="MenuFailed")RoyalCards.FailedGoals(transform,offerGoals);
            // The Quit Level question keeps a phone's share of a tablet's screen, as the level-end cards do.
            if(name=="MenuPause"){var ask=transform.Find("Image/"+RoyalMenuSkin.LeaveConfirmName);if(ask&&ask.gameObject.activeSelf){float s=RoyalCardKit.TabletScale(ask);if(Mathf.Abs(ask.localScale.x-s)>.0005f)ask.localScale=new Vector3(s,s,1);}}
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;
            if(name=="MenuPause"||name=="Settings"){
                SetCaption("Image/Music/Royal caption","MUSIC "+(PlayerPrefs.GetInt("Music",1)!=0?"ON":"OFF"));
                SetCaption("Image/Sound/Royal caption","SFX "+(PlayerPrefs.GetInt("Sound",1)!=0?"ON":"OFF"));
                SetCaption("Image/Hints/Royal caption","MATCHING HINTS "+(RoyalHintSettings.Enabled?"ON":"OFF"));
            }
            foreach(var group in GetComponentsInChildren<TargetsGroup>(true)){
                foreach(var layout in group.GetComponents<LayoutGroup>())layout.enabled=false;
                var targets=group.GetComponentsInChildren<TargetIcon>();
                if(name=="MenuPlay"){for(int i=0;i<targets.Length;i++)RoyalMenuSkin.LevelCardGoal(targets[i],i,targets.Length);continue;}
                // The other cards show their goals on a cream panel beside the group ("Royal goals panel").
                var panel=group.transform.parent?group.transform.parent.Find("Royal goals panel") as RectTransform:null;
                float width=panel?panel.sizeDelta.x:470,height=panel?panel.sizeDelta.y:124;
                // The level-end cards (the offer and the result cards) share the offer's goals card and its larger columns.
                bool levelEnd=name=="PreFailed"||name=="MenuComplete";
                for(int i=0;i<targets.Length;i++)
                    if(name=="MenuFailed")RoyalCardKit.Goal(targets[i],i,targets.Length,width,height,RoyalCards.FailIcon,RoyalCards.FailValue,RoyalCards.FailPad,true);
                    else if(levelEnd)RoyalCardKit.Goal(targets[i],i,targets.Length,width,height,RoyalCards.OfferIcon,RoyalCards.OfferValue,RoyalCards.OfferPad);
                    else RoyalCardKit.Goal(targets[i],i,targets.Length,width,height,60,24);
                if(panel)RoyalCardKit.ShowSeparators(panel.Find("Separators"),targets.Length,width,0);
            }
            // A tablet shows these cards at a phone's share of the screen height, not stretched to its width.
            if(name=="PreFailed"||name=="MenuFailed"||name=="MenuComplete")RoyalCardKit.FitToScreen(transform as RectTransform);
        }
        void SetCaption(string path,string value){var tr=transform.Find(path);if(tr){var text=tr.GetComponent<Text>();if(text)text.text=value;}}
    }
}
