using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoyalThrone.Editor
{
    /// <summary>Checks rendered hints on the disposable fixture, without invoking the mutating match solver.</summary>
    public static class RoyalHintPresentationChecks
    {
        const string Folder="Logs/port-checks/QaFeedback/04-SingleMoveHint/rotation/raw/";
        sealed class Example
        {
            public string Name;public Vector2Int Source;public Vector2Int[] Cells;public int Edges;
            public Example(string name,Vector2Int source,int edges,params Vector2Int[] cells){Name=name;Source=source;Cells=cells;Edges=edges;}
        }
        static Vector2Int P(int x,int y)=>new Vector2Int(x,y);
        static readonly Example[] examples={
            new Example("hint-3",P(3,4),8,P(1,3),P(2,3),P(3,3)),
            new Example("hint-4",P(3,4),10,P(1,3),P(2,3),P(3,3),P(4,3)),
            new Example("hint-5",P(3,4),12,P(1,3),P(2,3),P(3,3),P(4,3),P(5,3)),
            new Example("hint-L",P(3,4),12,P(1,3),P(2,3),P(3,3),P(3,1),P(3,2)),
            new Example("hint-T",P(3,2),12,P(2,3),P(3,3),P(4,3),P(3,4),P(3,5)),
            new Example("hint-square",P(2,3),8,P(3,3),P(4,3),P(3,4),P(4,4))
        };
        static Item At(LevelManager lm,Vector2Int p)=>lm.GetSquare(p.x,p.y).item;
        static RoyalHintPlan Setup(LevelManager lm,Example example,int colour=0)
        {
            // A prior gameplay resolver schedules idle discovery on LevelManager.
            // Retire that fixture's pending scan before assigning this exact shape.
            lm.StopAllCoroutines();
            AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
            foreach(var sq in lm.squaresArray){var item=sq.item;item.SetColor((colour+1+(sq.col+sq.row*2)%4)%5);item.currentType=item.NextType=ItemsTypes.NONE;item.falling=item.destroying=false;}
            foreach(var p in example.Cells)At(lm,p).SetColor(colour);
            At(lm,example.Source).SetColor(colour);At(lm,P(3,3)).SetColor((colour+4)%5);
            foreach(var sq in lm.squaresArray)sq.item.GetComponent<RoyalPieceSkin>().ApplyNow();
            return RoyalHintSolver.EvaluateSwap(lm,At(lm,example.Source),At(lm,P(3,3)));
        }
        static HashSet<Item> Affected(RoyalHintPlan plan)
        {var items=new HashSet<Item>(plan.PreviewCells.Select(s=>s.item));items.Add(plan.First);items.Add(plan.Second);items.Remove(null);return items;}
        static RoyalHintPlan DualResult(LevelManager lm,bool horizontal=false,int secondLength=3)
        {
            lm.StopAllCoroutines();AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
            foreach(var sq in lm.squaresArray){sq.item.SetColor(2+(sq.col+sq.row*2)%3);sq.item.currentType=sq.item.NextType=ItemsTypes.NONE;sq.item.falling=sq.item.destroying=false;}
            Item Piece(int x,int y)=>At(lm,horizontal?P(y,x):P(x,y));
            Piece(3,3).SetColor(0);Piece(3,4).SetColor(1);
            Piece(1,3).SetColor(1);Piece(2,3).SetColor(1);
            for(int i=1;i<secondLength;i++)Piece(3+i,4).SetColor(0);
            foreach(var sq in lm.squaresArray)sq.item.GetComponent<RoyalPieceSkin>().ApplyNow();
            return RoyalHintSolver.EvaluateSwap(lm,Piece(3,3),Piece(3,4));
        }
        static IEnumerator SingleMoveChecks(LevelManager lm,RoyalHintPresenter presenter,Action<bool,string> check)
        {
            foreach(var test in new[]{(horizontal:false,length:3),(horizontal:true,length:3),(horizontal:false,length:4),(horizontal:true,length:4),(horizontal:false,length:5)}){
                var plan=DualResult(lm,test.horizontal,test.length);string label="Dual result "+(test.horizontal?"horizontal":"vertical")+" 3+"+test.length;
                check(plan!=null&&plan.MatchCells.Count==3+test.length,label+" retains both genuine match results");
                check(MatchesProductionResult(lm,plan),label+" complete result still matches production resolution");
                var expected=new HashSet<Square>();
                for(int i=0;i<(test.length==3?3:test.length);i++){
                    int x=test.length==3?1+i:3+i,y=test.length==3?3:4;
                    expected.Add(lm.GetSquare(test.horizontal?y:x,test.horizontal?x:y));
                }
                check(expected.SetEquals(plan.PreviewCells),label+" previews only the strongest connected match, with a stable tie choice");
                var reversed=RoyalHintSolver.EvaluateSwap(lm,plan.Second,plan.First);
                check(expected.SetEquals(reversed.PreviewCells),label+" reversing swap arguments cannot change the marked group");
                var rest=lm.squaresArray.ToDictionary(s=>s.item,s=>s.item.sprRenderer.transform.localScale);
                check(presenter.Show(plan),label+" starts its single-group hint");
                yield return new WaitForSecondsRealtime(.95f);yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                check(presenter.VisibleSwapCount==2&&presenter.ContourCount==1&&presenter.OutlineCellCount==expected.Count,label+" shows one outline and one swap pair");
                var affected=Affected(plan);
                check(lm.squaresArray.Where(s=>!affected.Contains(s.item)).All(s=>s.item.sprRenderer.transform.localScale==rest[s.item]&&
                    !s.item.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name.StartsWith("Hint match gem glow")&&r.enabled)),label+" secondary result and alternative moves stay unmarked and unpulsed");
                check(affected.All(i=>i.sprRenderer.transform.localScale.x/rest[i].x>1.05f&&i.sprRenderer.transform.localScale.x/rest[i].x<=1.0601f),label+" retains the approved six-percent pulse for the marked group and swap pair");
                if(!test.horizontal&&test.length==3)yield return Capture("single-group-after");
                presenter.Hide();
            }
            var candidate=DualResult(lm);var ai=AI.THIS;var plans=new List<RoyalHintPlan>();
            // A separate legal swap, away from the two-result exchange, verifies
            // selection when the board really offers more than one player move.
            At(lm,P(1,0)).SetColor(0);At(lm,P(2,0)).SetColor(0);At(lm,P(3,0)).SetColor(1);At(lm,P(3,1)).SetColor(0);
            foreach(var sq in lm.squaresArray)sq.item.GetComponent<RoyalPieceSkin>().ApplyNow();
            foreach(var sq in lm.squaresArray)foreach(var d in new[]{Vector2Int.right,Vector2Int.up}){
                var next=lm.GetSquare(sq.col+d.x,sq.row+d.y);if(!next)continue;
                var option=RoyalHintSolver.EvaluateSwap(lm,sq.item,next.item);if(option!=null)plans.Add(option);
            }
            check(plans.Count>1,"Regression board contains multiple available legal swaps");
            string Key(RoyalHintPlan p)=>p.First.square.col+","+p.First.square.row+":"+p.Second.square.col+","+p.Second.square.row;
            var choices=RoyalHintSolver.FindAll(lm);
            check(choices.Count==plans.Count&&choices.Select(Key).Distinct().Count()==choices.Count,"Rotation includes every legal exchange exactly once");
            var rotationRest=lm.squaresArray.ToDictionary(s=>s.item,s=>s.item.sprRenderer.transform.localScale);
            for(int i=0;i<3;i++)ai.StartCoroutine(ai.CheckPossibleCombines());
            float deadline=Time.unscaledTime+8;while(!presenter.IsVisible&&Time.unscaledTime<deadline)yield return null;
            var chosen=presenter.CurrentPlan;
            check(chosen!=null&&chosen.IsCurrent(lm)&&presenter.VisibleSwapCount==2&&presenter.ContourCount==1,"Concurrent idle finders display exactly one legal swap and one matching group");
            yield return new WaitForSecondsRealtime(1.7f);
            check(presenter.CurrentPlan==chosen,"Each suggestion stays readable instead of changing midway through its interaction");
            yield return Capture("rotation-1");
            var visited=new HashSet<string>{Key(chosen)};var previous=chosen;bool single=true,restored=true,wrapped=false;
            deadline=Time.unscaledTime+AI.HintSuggestionSeconds*choices.Count+2;
            while(Time.unscaledTime<deadline&&!wrapped){
                yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());var current=presenter.CurrentPlan;
                single &= current!=null&&current.IsCurrent(lm)&&presenter.ContourCount==1&&presenter.VisibleSwapCount==2;
                if(current==null)break;
                if(current!=previous){
                    var affected=Affected(current);
                    restored &= lm.squaresArray.Where(s=>!affected.Contains(s.item)).All(s=>s.item.sprRenderer.transform.localScale==rotationRest[s.item]&&
                        !s.item.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name.StartsWith("Hint match gem glow")&&r.enabled));
                    wrapped=Key(current)==Key(chosen);visited.Add(Key(current));previous=current;
                    if(visited.Count==2&&!wrapped)yield return Capture("rotation-2");
                }
            }
            check(visited.Count==choices.Count&&wrapped,"Idle hints visit other legal combinations and wrap only after every option has been shown");
            check(single,"Rotation keeps exactly one current legal swap and one outline visible throughout");
            check(restored,"Switching suggestions restores the previous group's size and removes its glow");
            ai.CancelVisibleHint();check(!presenter.IsVisible,"Cancellation clears an actively rotating hint immediately");
            yield return new WaitForSecondsRealtime(.2f);
            check(!presenter.IsVisible,"Rotation cannot resurrect a cancelled suggestion before the fresh idle delay");
            ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;
            candidate=DualResult(lm);presenter.Show(candidate);
            var host=new GameObject("QA secondary hint presenter");var other=host.AddComponent<RoyalHintPresenter>();
            try{
                check(other.Show(candidate)&&other.IsVisible&&!presenter.IsVisible,"A second presenter retires the previous hint before displaying another");
                check(presenter.Show(candidate)&&presenter.IsVisible&&!other.IsVisible,"Hint ownership can return without leaving a second marking behind");
            }finally{other.Hide();UnityEngine.Object.DestroyImmediate(host);presenter.Hide();}
        }
        static bool NoArtOverlap(LevelManager lm,RoyalHintPlan plan)
        {
            foreach(var item in Affected(plan))foreach(var sq in lm.squaresArray){
                if(!sq||!sq.item||sq.item==item)continue;
                var a=item.sprRenderer.bounds;var b=sq.item.sprRenderer.bounds;
                if(Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x)>.0001f&&
                    Mathf.Min(a.max.y,b.max.y)-Mathf.Max(a.min.y,b.min.y)>.0001f)return false;
            }
            return true;
        }
        static bool ExactEdges(RoyalHintContour.Shape shape,HashSet<Vector2Int> cells)
        {
            string Edge(Vector2Int a,Vector2Int b)=>a.x+","+a.y+":"+b.x+","+b.y;
            var expected=new HashSet<string>();
            foreach(var p in cells){
                if(!cells.Contains(p+Vector2Int.down))expected.Add(Edge(p,p+Vector2Int.right));
                if(!cells.Contains(p+Vector2Int.right))expected.Add(Edge(p+Vector2Int.right,p+Vector2Int.one));
                if(!cells.Contains(p+Vector2Int.up))expected.Add(Edge(p+Vector2Int.one,p+Vector2Int.up));
                if(!cells.Contains(p+Vector2Int.left))expected.Add(Edge(p+Vector2Int.up,p));
            }
            var actual=new HashSet<string>();
            foreach(var loop in shape.Loops)for(int i=0;i<loop.Count;i++){
                if(!actual.Add(Edge(loop[i],loop[(i+1)%loop.Count])))return false;
            }
            return expected.SetEquals(actual)&&actual.Count==shape.BoundaryEdges;
        }
        static bool MatchesProductionResult(LevelManager lm,RoyalHintPlan plan)
        {
            // Test-only oracle on the disposable board. Restore every side effect
            // before returning; runtime hint evaluation never calls this solver.
            var first=plan.First;var second=plan.Second;var a=first.square;var b=second.square;
            var items=lm.squaresArray.Where(s=>s&&s.item).Select(s=>s.item).ToArray();
            var next=items.Select(i=>i.NextType).ToArray();var random=UnityEngine.Random.state;
            try{
                a.item=second;b.item=first;first.square=b;second.square=a;
                var actual=new JellyGarden.Scripts.System.CombineManager().GetCombine().SelectMany(g=>g).Select(i=>i.square);
                return new HashSet<Square>(actual).SetEquals(plan.MatchCells);
            }finally{
                a.item=first;b.item=second;first.square=a;second.square=b;
                for(int i=0;i<items.Length;i++)items[i].NextType=next[i];UnityEngine.Random.state=random;
            }
        }
        static void State(LevelManager lm,GameState value)
        {
            // Only fixture backing state, so pause/resume assertions do not open menus or start live level coroutines.
            typeof(LevelManager).GetField("GameStatus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(lm,value);
        }
        static IEnumerator Capture(string name)
        {
            if(UnityEngine.Application.isBatchMode)yield break;yield return new WaitForEndOfFrame();Directory.CreateDirectory(Folder);
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.Destroy(texture);}
        }
        public static IEnumerator Run(LevelManager lm,Action<bool,string> check)
        {
            if(!lm.GameField.name.StartsWith("Royal workshop test board"))throw new InvalidOperationException("Hint checks require the disposable workshop board.");
            var ai=AI.THIS;ai.StopAllCoroutines();ai.CancelVisibleHint();
            var presenter=ai.GetComponent<RoyalHintPresenter>()??ai.gameObject.AddComponent<RoyalHintPresenter>();
            var originalItems=lm.squaresArray.Select(s=>s.item).ToArray();var originalColours=originalItems.Select(i=>i.color).ToArray();
            var originalTypes=originalItems.Select(i=>i.currentType).ToArray();var originalNext=originalItems.Select(i=>i.NextType).ToArray();
            int moves=lm.Limit,score=LevelManager.Score,move=lm.moveID,gems=InitScript.Gems,lives=InitScript.lifes;
            var random=UnityEngine.Random.state;var status=lm.gameStatus;bool blocked=lm.DragBlocked;var savedField=lm.GameField;
            bool hadHints=PlayerPrefs.HasKey(RoyalHintSettings.PreferenceKey),hadMotion=PlayerPrefs.HasKey("RoyalThrone.HomeMotion");
            int hints=PlayerPrefs.GetInt(RoyalHintSettings.PreferenceKey,1),motion=PlayerPrefs.GetInt("RoyalThrone.HomeMotion",1);
            var previousMouse=Mouse.current;var input=InputManager.Instance;var devices=input.inputActionAsset.devices;
            var mouse=InputSystem.AddDevice<Mouse>("Royal hint QA pointer");
            Action<InputAction.CallbackContext> observe=null;
            try
            {
                RoyalHintSettings.Enabled=true;PlayerPrefs.SetInt("RoyalThrone.HomeMotion",1);State(lm,GameState.Playing);lm.DragBlocked=false;
                input.inputActionAsset.devices=new[]{mouse};yield return null;
                yield return SingleMoveChecks(lm,presenter,check);
                foreach(var example in examples){
                    var plan=Setup(lm,example,Array.IndexOf(examples,example)%5);var affected=Affected(plan);
                    check(plan!=null&&new HashSet<Vector2Int>(plan.MatchCells.Select(s=>P(s.col,s.row))).SetEquals(example.Cells),example.Name+" previews every resulting matched cell and excludes unrelated neighbours");
                    check(plan.First==At(lm,example.Source)&&plan.Second==At(lm,P(3,3))&&!plan.IsTap,example.Name+" identifies both actual exchange partners");
                    check(MatchesProductionResult(lm,plan),example.Name+" full preview exactly matches the existing production match solver's consumed cells");
                    var grid=new HashSet<Vector2Int>(example.Cells.Select(p=>P(p.x,-p.y)));var shape=RoyalHintContour.Build(grid);
                    check(shape.Loops.Count==1&&shape.BoundaryEdges==example.Edges&&ExactEdges(shape,grid),example.Name+" has one closed full-shape boundary, with no internal or phantom edges");
                    var roots=originalItems.Select(i=>i.transform.position).ToArray();var sprites=originalItems.Select(i=>i.sprRenderer.transform.localPosition).ToArray();
                    var scales=originalItems.Select(i=>i.sprRenderer.transform.localScale).ToArray();
                    var colliders=originalItems.Select(i=>i.GetComponent<BoxCollider2D>().bounds).ToArray();
                    var next=originalItems.Select(i=>i.NextType).ToArray();var solverRandom=UnityEngine.Random.state;
                    RoyalHintSolver.Find(lm);
                    check(originalItems.Select((item,index)=>item.NextType==next[index]&&item.square.item==item).All(v=>v)&&UnityEngine.Random.state.Equals(solverRandom),example.Name+" preview leaves ownership, power creation and RNG untouched");
                    check(presenter.Show(plan),example.Name+" starts its native rendered hint");
                    yield return new WaitForSecondsRealtime(.18f);yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                    check(originalItems.Select((item,index)=>item.sprRenderer.transform.localPosition==sprites[index]&&Vector3.Distance(item.sprRenderer.transform.localScale,scales[index])<.001f).All(v=>v),example.Name+" waits gently before moving or growing either gem");
                    // Sample the actual rendered approach instead of assuming a
                    // fixed wait lands on the peak during first-use shader warmup.
                    float peakDeadline=Time.unscaledTime+2.5f;
                    while(Time.unscaledTime<peakDeadline&&plan.First.sprRenderer.transform.localScale.x/scales[Array.IndexOf(originalItems,plan.First)].x<1.057f)
                        yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                    var direction=(plan.Second.transform.position-plan.First.transform.position).normalized;
                    var a=Array.IndexOf(originalItems,plan.First);var b=Array.IndexOf(originalItems,plan.Second);
                    Vector3 Delta(int index)=>originalItems[index].sprRenderer.transform.parent.TransformVector(originalItems[index].sprRenderer.transform.localPosition-sprites[index]);
                    check(presenter.CurrentPlan==plan&&Vector3.Dot(Delta(a),direction)>lm.squareWidth*.005f&&Vector3.Dot(Delta(b),direction)<-lm.squareWidth*.005f,
                        example.Name+" visibly moves the two rendered gems toward each other after Animator and skin updates (visible="+presenter.IsVisible+", first="+Vector3.Dot(Delta(a),direction)+", second="+Vector3.Dot(Delta(b),direction)+")");
                    check(originalItems.Select((item,index)=>index==a||index==b||Vector3.Distance(item.sprRenderer.transform.localPosition,sprites[index])<.0001f).All(v=>v),example.Name+" leaves all other gem positions steady");
                    check(originalItems.Select((item,index)=>Vector3.Distance(item.transform.position,roots[index])<.0001f&&item.GetComponent<BoxCollider2D>().bounds==colliders[index]&&item.square.item==item).All(v=>v),example.Name+" keeps roots, hit areas and square ownership unchanged during motion");
                    check(affected.All(item=>{float ratio=item.sprRenderer.transform.localScale.x/scales[Array.IndexOf(originalItems,item)].x;return ratio>1.05f&&ratio<=1.0601f;}),
                        example.Name+" all matching-area gems and both swap partners gently grow by no more than 0.06x");
                    check(affected.All(item=>item.sprRenderer.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name.StartsWith("Hint match gem glow")&&r.enabled&&r.color.a>.45f&&r.sprite==item.sprRenderer.sprite)),
                        example.Name+" every matching-area gem and both swap partners brighten together");
                    check(originalItems.Where(item=>!affected.Contains(item)).All(item=>item.sprRenderer.transform.localScale==scales[Array.IndexOf(originalItems,item)]&&
                        !item.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name.StartsWith("Hint match gem glow")&&r.enabled)),example.Name+" unrelated gems neither grow nor glow");
                    check(NoArtOverlap(lm,plan),example.Name+" enlarged gem artwork stays separated from every neighbouring gem");
                    var line=presenter.ContourRenderer(0);var points=new Vector3[line.positionCount];line.GetPositions(points);
                    var bounds=new Bounds(plan.MatchCells[0].transform.position,Vector3.zero);foreach(var sq in plan.MatchCells)bounds.Encapsulate(sq.transform.position);
                    check(line.enabled&&line.loop&&line.positionCount>=24&&presenter.OutlineCellCount==example.Cells.Length&&presenter.BoundaryEdgeCount==example.Edges,
                        example.Name+" renders a closed rounded outline for all "+example.Cells.Length+" future match cells");
                    check(points.Min(p=>p.x)<bounds.min.x-lm.squareWidth*.4f&&points.Max(p=>p.x)>bounds.max.x+lm.squareWidth*.4f&&
                        points.Min(p=>p.y)<bounds.min.y-lm.squareHeight*.4f&&points.Max(p=>p.y)>bounds.max.y+lm.squareHeight*.4f,
                        example.Name+" outline covers the full cell extents");
                    yield return Capture(example.Name+"-flow-a");
                    var glint=presenter.GetComponentsInChildren<LineRenderer>().First(r=>r.name.Contains("travelling")&&r.enabled);var before=glint.GetPosition(0);
                    yield return new WaitForSecondsRealtime(.36f);yield return Capture(example.Name+"-flow-b");
                    check(Vector3.Distance(before,glint.GetPosition(0))>lm.squareWidth*.12f,example.Name+" light visibly travels along the contour between captured frames");
                    presenter.Hide();
                    check(!presenter.IsVisible&&originalItems.Select((item,index)=>Vector3.Distance(item.sprRenderer.transform.localPosition,sprites[index])<.0001f).All(v=>v),example.Name+" cancellation restores both sprite positions immediately");
                    check(originalItems.Select((item,index)=>Vector3.Distance(item.sprRenderer.transform.localScale,scales[index])<.001f).All(v=>v)&&
                        presenter.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Hint match gem glow")).All(r=>!r.enabled),example.Name+" cancellation restores size and removes gem brightness immediately");
                }

                // Observe repeated live frames, including the return/rest phase and
                // the second approach. Sampling the rendered result catches Animator conflicts.
                var loopPlan=Setup(lm,examples[0]);
                var loopItems=lm.squaresArray.Select(s=>s.item).ToArray();
                var loopRoots=loopItems.Select(i=>i.transform.position).ToArray();
                var loopBounds=loopItems.Select(i=>i.GetComponent<BoxCollider2D>().bounds).ToArray();
                var loopRest=loopItems.Select(i=>i.sprRenderer.transform.localPosition).ToArray();
                var loopScale=loopPlan.First.sprRenderer.transform.localScale.x;
                presenter.Show(loopPlan);float loopStart=Time.unscaledTime,nextCapture=0;
                int frame=0;bool stationary=true,separated=true,firstPeak=false,secondPeak=false,returned=false;
                var captureTimes=new List<string>();
                while(Time.unscaledTime-loopStart<3.65f){
                    yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());float time=Time.unscaledTime-loopStart;
                    stationary &= loopItems.Select((item,index)=>item.transform.position==loopRoots[index]&&item.GetComponent<BoxCollider2D>().bounds==loopBounds[index]&&item.square.item==item&&
                        (item==loopPlan.First||item==loopPlan.Second||item.sprRenderer.transform.localPosition==loopRest[index])).All(v=>v);
                    float scale=loopPlan.First.sprRenderer.transform.localScale.x/loopScale;
                    separated &= NoArtOverlap(lm,loopPlan);
                    if(time<1.3f&&scale>1.057f)firstPeak=true;
                    if(time>2.2f&&scale>1.057f)secondPeak=true;
                    if(time>1.6f&&time<1.95f&&Mathf.Abs(scale-1)<.001f)returned=true;
                    if(time>=nextCapture&&!UnityEngine.Application.isBatchMode){
                        Directory.CreateDirectory(Folder);var tex=ScreenCapture.CaptureScreenshotAsTexture();
                        try{File.WriteAllBytes(Folder+"loop-"+frame.ToString("D3")+".png",tex.EncodeToPNG());}finally{UnityEngine.Object.Destroy(tex);}
                        captureTimes.Add(time.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));frame++;nextCapture=time+1f/15;
                    }
                }
                Directory.CreateDirectory(Folder);File.WriteAllLines(Folder+"loop-times.txt",captureTimes);
                check(firstPeak&&secondPeak&&returned,"Interaction repeats two full approaches with a smooth return to normal size between them");
                check(stationary,"Every sampled loop frame preserves all cell owners, root positions, hit areas and other gem positions");
                check(separated,"No hinted gem overlaps any neighbour during either complete rendered cycle");
                check(loopItems.Sum(i=>i.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name.StartsWith("Hint match gem glow")))==Affected(loopPlan).Count,"Repeated animation reuses one glow per matching-area gem and swap partner");
                presenter.Hide();

                var mixed=new Example("line-square-overlap",P(4,3),8,P(1,3),P(2,3),P(3,3));Setup(lm,mixed);
                At(lm,P(2,2)).SetColor(0);At(lm,P(3,2)).SetColor(0);
                var mixedPlan=RoyalHintSolver.EvaluateSwap(lm,At(lm,mixed.Source),At(lm,P(3,3)));
                check(mixedPlan!=null&&new HashSet<Vector2Int>(mixedPlan.MatchCells.Select(s=>P(s.col,s.row))).SetEquals(mixed.Cells),
                    "A line overlapping a 2x2 previews only the consumed line, matching the existing helicopter precedence rule");
                check(MatchesProductionResult(lm,mixedPlan),"Overlapping line/square preview matches the production solver exactly");
                foreach(var coords in new[]{new[]{P(0,0),P(1,1)},new[]{P(0,0),P(1,0),P(2,0),P(0,1),P(2,1),P(0,2),P(1,2),P(2,2)}}){
                    var cells=new HashSet<Vector2Int>(coords);var shape=RoyalHintContour.Build(cells);
                    check(shape.Loops.Count==2&&ExactEdges(shape,cells),"Contour builder retains separate diagonal outlines and a hollow union's inner boundary");
                }
                var candidate=Setup(lm,examples[3]);check(presenter.Show(candidate),"Input cancellation fixture starts a visible hint");
                bool cancelledInsideEvent=false;
                observe=_=>cancelledInsideEvent=!presenter.IsVisible;
                input.Gameplay.PrimaryTouch.performed+=observe;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100),buttons=1});yield return null;
                check(cancelledInsideEvent&&!presenter.IsVisible,"Pointer press removes the full hint during the input callback, before another rendered frame");
                check(!presenter.Show(candidate),"A held pointer suppresses a direct hint reveal");
                ai.StartCoroutine(ai.CheckPossibleCombines());yield return new WaitForSecondsRealtime(AI.HintIdleSeconds+1.2f);
                check(input.IsTouching&&!presenter.IsVisible&&ai.VisibleHintCount==0,"Automatic idle hints stay hidden while the pointer remains held past the idle threshold");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100),buttons=0});yield return null;
                ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;

                // End-to-end path: only start the real idle finder. Do not call
                // presenter.Show; observe the plan and child poses it produces.
                Setup(lm,examples[3]);
                var idleRest=originalItems.ToDictionary(i=>i,i=>i.sprRenderer.transform.localPosition);
                var idleRoots=originalItems.ToDictionary(i=>i,i=>i.transform.position);
                float idleStarted=Time.realtimeSinceStartup,idleDeadline=idleStarted+8;
                ai.StartCoroutine(ai.CheckPossibleCombines());
                while(ai.VisibleHintCount==0&&Time.realtimeSinceStartup<idleDeadline)yield return null;
                var idlePlan=presenter.CurrentPlan;
                check(ai.gotTip&&ai.VisibleHintCount==2&&idlePlan!=null&&idlePlan.Second&&!idlePlan.IsTap&&
                    Time.realtimeSinceStartup-idleStarted>=AI.HintIdleSeconds-.1f,
                    "Natural idle finder reveals a real two-gem swap only after the quiet-time delay");
                check(idlePlan.IsCurrent(lm)&&MatchesProductionResult(lm,idlePlan),
                    "Natural idle finder selects the complete match actually consumed by production gameplay");
                yield return new WaitForSecondsRealtime(RoyalHintPresenter.PairStartDelay+.34f);yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());
                var idleDirection=(idlePlan.Second.transform.position-idlePlan.First.transform.position).normalized;
                Vector3 IdleDelta(Item item)=>item.sprRenderer.transform.parent.TransformVector(item.sprRenderer.transform.localPosition-idleRest[item]);
                check(presenter.CurrentPlan==idlePlan&&Vector3.Dot(IdleDelta(idlePlan.First),idleDirection)>lm.squareWidth*.005f&&
                    Vector3.Dot(IdleDelta(idlePlan.Second),idleDirection)<-lm.squareWidth*.005f,
                    "Natural idle hint visibly nudges both rendered swap partners after Animator and LateUpdate");
                check(originalItems.All(i=>Vector3.Distance(i.transform.position,idleRoots[i])<.0001f&&i.square.item==i)&&
                    originalItems.Where(i=>i!=idlePlan.First&&i!=idlePlan.Second).All(i=>Vector3.Distance(i.sprRenderer.transform.localPosition,idleRest[i])<.0001f),
                    "Natural idle hint moves only the two child sprites while every board root and other gem stays steady");
                check(presenter.OutlineCellCount==idlePlan.PreviewCells.Count&&presenter.ContourCount==1&&presenter.ContourRenderer(0).enabled&&presenter.ContourRenderer(0).loop,
                    "Natural idle hint renders a closed outline for one predicted matching group");
                yield return Capture("hint-natural-idle-moving");
                cancelledInsideEvent=false;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100),buttons=1});yield return null;
                check(cancelledInsideEvent&&ai.VisibleHintCount==0&&!presenter.IsVisible&&
                    originalItems.All(i=>Vector3.Distance(i.sprRenderer.transform.localPosition,idleRest[i])<.0001f),
                    "Real pointer input immediately cancels the natural idle hint and restores both rendered gem positions");
                yield return Capture("hint-natural-idle-input-cancelled");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(-100,-100),buttons=0});yield return null;
                ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;

                candidate=Setup(lm,examples[3]);presenter.Show(candidate);RoyalHintSettings.Enabled=false;
                check(!presenter.IsVisible&&presenter.ContourCount==0,"Hints OFF removes flowing contours synchronously");RoyalHintSettings.Enabled=true;
                presenter.Show(candidate);At(lm,P(1,3)).SetColor(2);yield return null;
                check(!presenter.IsVisible,"A changed matched gem invalidates the hint without waiting for the idle loop");
                candidate=Setup(lm,examples[3]);presenter.Show(candidate);var changedSquare=lm.GetSquare(3,3);var previousType=changedSquare.type;
                changedSquare.type=SquareTypes.WIREBLOCK;yield return null;
                check(!presenter.IsVisible,"A newly trapped swap partner invalidates the hint immediately");changedSquare.type=previousType;
                candidate=Setup(lm,examples[3]);presenter.Show(candidate);State(lm,GameState.Pause);yield return null;
                check(!presenter.IsVisible,"Pause clears native hint poses and contours");State(lm,GameState.Playing);
                candidate=Setup(lm,examples[3]);presenter.Show(candidate);var originalField=lm.GameField;lm.GameField=null;yield return null;
                check(!presenter.IsVisible,"Changing game fields clears the hint");lm.GameField=originalField;
                candidate=Setup(lm,examples[3]);PlayerPrefs.SetInt("RoyalThrone.HomeMotion",0);
                var firstRest=candidate.First.sprRenderer.transform.localPosition;var firstScale=candidate.First.sprRenderer.transform.localScale;
                presenter.Show(candidate);yield return new WaitForSecondsRealtime(1f);
                check(presenter.IsVisible&&candidate.First.sprRenderer.transform.localPosition==firstRest&&candidate.First.sprRenderer.transform.localScale==firstScale&&
                    presenter.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Hint match gem glow")).All(r=>!r.enabled)&&
                    presenter.GetComponentsInChildren<LineRenderer>().Where(r=>r.name.Contains("travelling")).All(r=>!r.enabled),
                    "Reduced motion retains a readable full outline with no gem nudging or moving glint");presenter.Hide();
                check(lm.Limit==moves&&LevelManager.Score==score&&lm.moveID==move&&InitScript.Gems==gems&&InitScript.lifes==lives,"Rendered hint checks consume no moves, score, coins or lives");
                PlayerPrefs.SetInt("RoyalThrone.HomeMotion",1);candidate=Setup(lm,examples[3]);presenter.Show(candidate);
                UnityEngine.Object.DestroyImmediate(At(lm,P(1,3)).gameObject);yield return null;
                check(!presenter.IsVisible,"Destroying a different matched gem clears the complete hint even before its stale cell reference is replaced");
            }
            finally
            {
                if(observe!=null)input.Gameplay.PrimaryTouch.performed-=observe;
                ai.StopAllCoroutines();ai.CancelVisibleHint();ai.allowShowTip=false;presenter.Hide();
                input.inputActionAsset.devices=devices;if(mouse.added)InputSystem.RemoveDevice(mouse);if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();
                for(int i=0;i<originalItems.Length;i++)if(originalItems[i]){originalItems[i].SetColor(originalColours[i]);originalItems[i].currentType=originalTypes[i];originalItems[i].NextType=originalNext[i];}
                RoyalHintSettings.Enabled=hints!=0;if(!hadHints)PlayerPrefs.DeleteKey(RoyalHintSettings.PreferenceKey);else PlayerPrefs.SetInt(RoyalHintSettings.PreferenceKey,hints);
                if(!hadMotion)PlayerPrefs.DeleteKey("RoyalThrone.HomeMotion");else PlayerPrefs.SetInt("RoyalThrone.HomeMotion",motion);PlayerPrefs.Save();
                lm.GameField=savedField;State(lm,status);lm.DragBlocked=blocked;UnityEngine.Random.state=random;
            }
        }
    }
}
