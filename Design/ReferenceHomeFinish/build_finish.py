from pathlib import Path
import shutil
root=Path.cwd();backup=Path('Design/ReferenceHomeFinish/checkpoint.txt').read_text().strip()
p=Path('Assets/ReferenceGameplay/VFX/Editor/ReferenceGameplayVfxImporter.cs');q=Path(backup)/p;q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
s=p.read_text(encoding='utf8');s=s.replace('public static void Build()\n    {','public static void Build() => BuildFile("Design/ReferenceGameplayImport/Media/NativeParticles.json");\n    public static void BuildFile(string sourceFile)\n    {',1).replace('Path.Combine(project,"Design/ReferenceGameplayImport/Media/NativeParticles.json")','Path.Combine(project,sourceFile)',1);s=s.replace('"Design/ReferenceGameplayImport/Media/VfxIncomplete.txt"','sourceFile+".limitations.txt"');p.write_bytes(s.encode())
p=Path('Assets/ReferenceGameplay/Home/Scripts/ReferenceWinFinish.cs')
p.write_text('''using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.ReferenceGameplay
{
    // Reconstructed orchestration from packaged RemainingMovesItemReplacer/Exploder.
    // Native particle modules are imported; board damage and all booster sequences stay existing.
    public sealed class ReferenceWinFinish : MonoBehaviour
    {
        public int Sent {get;private set;} public int Converted {get;private set;} public int Fired {get;private set;}
        public bool Running {get;private set;} int travelling; LevelManager lm; Text skip; readonly List<ReferenceVfxPool.Handle> leases=new List<ReferenceVfxPool.Handle>();
        public static bool Power(Item i)=>i&&!i.destroying&&(i.currentType==ItemsTypes.HORIZONTAL_STRIPPED||i.currentType==ItemsTypes.VERTICAL_STRIPPED||i.currentType==ItemsTypes.PACKAGE||i.currentType==ItemsTypes.BOMB||i.currentType==ItemsTypes.HELICOPTER);
        List<Item> Regular()=>lm.GetItems().Where(i=>i&&!i.destroying&&!i.falling&&i.currentType==ItemsTypes.NONE&&i.NextType==ItemsTypes.NONE&&i.square&&i.square.CanGoOut()&&!RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(i)).ToList();
        bool Active=>lm&&lm.gameStatus==GameState.PreWinAnimations;
        public static IEnumerator Play(LevelManager owner){var view=owner.GetComponent<ReferenceWinFinish>();if(!view)view=owner.gameObject.AddComponent<ReferenceWinFinish>();yield return view.Run(owner);}
        IEnumerator Delay(float normal,float fast){float t=0;while(Active&&t<(lm.SkipWinCelebration?fast:normal)){t+=Time.deltaTime;yield return null;}}
        IEnumerator Settle(){lm.FindMatches();do{yield return null;}while(Active&&(lm.IsResolvingMatches||RoyalPowerupDirector.Busy||lm.GetItems().Where(i=>i).Any(i=>i.destroying||i.falling)));}
        IEnumerator Run(LevelManager owner){lm=owner;Sent=Converted=Fired=travelling=0;Running=true;
            try{
                BuildLabel();skip.gameObject.SetActive(true);yield return Delay(.35f,.03f);
                int left=lm.limitType==LIMIT.MOVES?Mathf.Max(0,lm.Limit):8;
                while(Active){
                    var candidates=Regular();int count=Mathf.Min(left,candidates.Count);
                    for(int n=0;n<count&&Active;n++){
                        int pick=Random.Range(0,candidates.Count);var item=candidates[pick];candidates.RemoveAt(pick);
                        left--;Sent++;if(lm.limitType==LIMIT.MOVES)lm.Limit=Mathf.Max(0,lm.Limit-1);
                        travelling++;StartCoroutine(Send(item));yield return Delay(.12f,.015f);
                    }
                    while(Active&&travelling>0)yield return null;
                    yield return Delay(.22f,.02f);
                    var powers=lm.GetItems().Where(Power).ToList();
                    if(powers.Count==0){
                        // No fabricated target: a fully blocked board cannot receive further conversions.
                        // Preserve its remaining counter rather than pretend unavailable placements happened.
                        break;
                    }
                    foreach(var item in powers){if(!Active)yield break;if(!Power(item))continue;item.DestroyItem();Fired++;yield return Delay(.18f,.025f);}
                    yield return Settle();
                    if(left==0&&!lm.GetItems().Any(Power))break;
                }
                yield return Delay(.35f,.08f);
            }finally{Running=false;if(skip)skip.gameObject.SetActive(false);foreach(var h in leases)h?.Stop();leases.Clear();}
        }
        IEnumerator Send(Item item){ReferenceVfxPool.Handle flight=null;
            try{
                if(!item)yield break;Vector3 end=item.transform.position,start=end+Vector3.up*3;
                var label=GameObject.Find("Moves remaining");var camera=Camera.main;
                if(label&&camera){var canvas=label.GetComponentInParent<Canvas>();Vector2 screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,label.transform.position);start=camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,camera.WorldToScreenPoint(end).z));}
                flight=ReferenceVfxPool.Play("RemainingMovesParticles",start,sustained:true);if(flight!=null)leases.Add(flight);
                float progress=0;while(Active&&item&&progress<1){progress+=Time.deltaTime/(lm.SkipWinCelebration?.10f:.34f);float u=Mathf.Clamp01(progress);Vector3 p=Vector3.Lerp(start,end,u)+Vector3.right*Mathf.Sin(u*Mathf.PI)*.5f;if(flight!=null&&flight.Valid)flight.GameObject.transform.position=p;yield return null;}
                if(Active&&item&&!item.destroying&&item.currentType==ItemsTypes.NONE){ReferenceVfxPool.Play("RemainingMovesHitParticles",end);item.NextType=Random.value<.5f?ItemsTypes.HORIZONTAL_STRIPPED:ItemsTypes.VERTICAL_STRIPPED;item.ChangeType();Converted++;}
            }finally{flight?.Stop();travelling=Mathf.Max(0,travelling-1);}
        }
        void BuildLabel(){if(skip)return;var r=new GameObject("Finishing stage overlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));r.transform.SetParent(transform,false);var c=r.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=23;var scale=r.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(720,1280);
            var rt=new GameObject("Tap to skip",typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(r.transform,false);rt.anchorMin=new Vector2(.25f,.12f);rt.anchorMax=new Vector2(.75f,.16f);rt.offsetMin=rt.offsetMax=Vector2.zero;skip=rt.gameObject.AddComponent<Text>();skip.font=Resources.Load<Font>("RoyalGameplay/Display");skip.text="Tap to Skip";skip.fontSize=28;skip.alignment=TextAnchor.MiddleCenter;skip.color=new Color(1,.97f,.8f);skip.raycastTarget=false;var shadow=rt.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.10f,.22f);shadow.effectDistance=new Vector2(1,-2);}
        void Update(){if(Running&&!Active){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}}
        void OnDisable(){StopAllCoroutines();travelling=0;Running=false;foreach(var h in leases)h?.Stop();leases.Clear();if(skip)skip.gameObject.SetActive(false);}
    }
}
''',encoding='utf8')
p=Path('Assets/JellyGarden/Scripts/LevelManager.cs');s=p.read_bytes().decode('utf8');start=s.index('            List<Item> items = GetRandomItems(Mathf.Clamp(limitType');end=s.index('            MenuManager.Instance.ShowCompleteLabel(false);',start);s=s[:start]+'''            yield return RoyalThrone.ReferenceGameplay.ReferenceWinFinish.Play(this);
            if (gameStatus != GameState.PreWinAnimations) { preWinAnimationRunning = false; yield break; }

'''+s[end:];p.write_bytes(s.encode('utf8'))
p=Path('Assets/ReferenceGameplay/Editor/ReferenceGameplayChecks.cs');s=p.read_text(encoding='utf8');s=s.replace('                    case "tool-checks":','                    case "import-finish":\n                        ReferenceGameplayVfxImporter.BuildFile("Design/ReferenceGameplayImport/Media/NativeFinishingParticles.json");\n                        break;\n                    case "home-finish-checks":\n#if !ROYAL_DISABLE_QA\n                        RoyalThrone.Editor.RoyalRegressionRunner.Run(ReferenceHomeFinishChecks.Run());\n#endif\n                        break;\n                    case "tool-checks":');p.write_bytes(s.encode())
