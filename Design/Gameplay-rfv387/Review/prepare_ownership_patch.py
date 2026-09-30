from pathlib import Path
import difflib, hashlib, json

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
files = {}
def read(path):
    raw = (root/path).read_bytes()
    files[path] = {'sha256':hashlib.sha256(raw).hexdigest(), 'before':raw.decode('utf-8-sig').replace('\r\n','\n')}
    return files[path]['before']
def replace(text, before, after):
    assert text.count(before)==1, (text.count(before), before[:120])
    return text.replace(before, after)
def stage(path, text):
    files[path]['after'] = text
    dest=out/'Proposed'/path
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(text, encoding='utf-8', newline='\n')

path='Assets/Boosters/ColorBall/V4/Scripts/ColorBallV4Sequence.cs'
s=read(path)
s=replace(s,'public bool hit,flashed,arrivalNotified;', 'public bool hit,flashed,arrivalNotified,scaleHeld;')
mark='        IEnumerator AnimateColorBallV4Targets'
helpers='''        // Explicit ownership survives Unity cancelling nested enumerators without disposal.
        readonly HashSet<V4Target> colorBallContactPoses = new HashSet<V4Target>();
        void BeginColorBallContactPose(V4Target target)
        {
            if (target.scaleHeld || !target.item) return;
            target.rest = target.item.transform.localScale;
            foreach (var owner in colorBallContactPoses)
                if (owner.item == target.item) { target.rest = owner.rest; break; }
            target.scaleHeld = true; colorBallContactPoses.Add(target);
        }
        void EndColorBallContactPose(V4Target target)
        {
            if (!target.scaleHeld) return;
            target.scaleHeld = false; colorBallContactPoses.Remove(target);
            if (target.item && !target.item.destroying) target.item.transform.localScale = target.rest;
        }
        void ReleaseColorBallContactPoses()
        {
            foreach (var owner in colorBallContactPoses)
            {
                owner.scaleHeld = false;
                if (owner.item && !owner.item.destroying) owner.item.transform.localScale = owner.rest;
            }
            colorBallContactPoses.Clear();
        }
'''
s=replace(s,mark,helpers+mark)
s=replace(s,'var deferred=new List<Square>();','var deferred=new List<V4Target>();')
s=replace(s,'            var charged=new List<V4Target>(targets.Count);var collected=new HashSet<Item>();', '''            var charged=new List<V4Target>(targets.Count);var collected=new HashSet<Item>();
            var selectedItems=new Dictionary<Square,Item>();
            foreach(var square in targets)if(square)selectedItems[square]=square.item;''')
s=replace(s,'if(!sq||!sq.item||sq.item.destroying){next++;prepared=false;continue;}', 'if(!sq||!selectedItems.TryGetValue(sq,out var selected)||!selected||sq.item!=selected||selected.destroying||selected.currentType!=ItemsTypes.NONE){next++;prepared=false;continue;}')
s=replace(s,'if(!f.square||!f.item||f.item.destroying||f.square.item!=f.item){if(f.item)f.item.transform.localScale=f.rest;', 'if(!f.square||!f.item||f.item.destroying||f.square.item!=f.item||f.item.currentType!=ItemsTypes.NONE){EndColorBallContactPose(f);')
s=replace(s,'                            if(!convert&&f.item.currentType==ItemsTypes.NONE){f.charge=', '                            if(convert)BeginColorBallContactPose(f);\n                            if(!convert&&f.item.currentType==ItemsTypes.NONE){f.charge=')
s=replace(s,'                                f.item.transform.localScale=f.rest;','                                EndColorBallContactPose(f);')
s=replace(s,'                                    ColorBallConversions++;ColorBallVfxV4.Converted(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetConverted,f.square.transform.position);}', '''                                    if(f.item.currentType==payload){deferred.Add(f);ColorBallConversions++;ColorBallVfxV4.Converted(f.square.transform.position);ball.Send(ColorBallEvent.ColorBallTargetConverted,f.square.transform.position);}}''')
s=replace(s,'if(!convert&&f.item.currentType==ItemsTypes.NONE)charged.Add(f);else deferred.Add(f.square);','if(!convert&&f.item.currentType==ItemsTypes.NONE)charged.Add(f);')
s=replace(s,'if(f.square&&f.item&&!f.item.destroying&&f.square.item==f.item&&collected.Add(f.item))','if(f.square&&f.item&&!f.item.destroying&&f.item.currentType==ItemsTypes.NONE&&f.square.item==f.item&&collected.Add(f.item))')
s=replace(s,'                foreach(var sq in deferred)Hit(sq); // Unchanged Rocket queue/launch/destruction owner.', '''                // Only this sequence's successful conversions belong to its handoff.
                foreach(var f in deferred)if(f.square&&f.item&&!f.item.destroying&&f.square.item==f.item&&f.item.currentType==payload&&collected.Add(f.item))Hit(f.square);''')
s=replace(s,'                foreach(var f in flights)if(f.item&&!f.item.destroying)f.item.transform.localScale=f.rest;', '                foreach(var f in flights)EndColorBallContactPose(f);')
stage(path,s)

path='Assets/JellyGarden/Scripts/Juice/RoyalPowerupDirector.cs'
s=read(path)
s=replace(s,'        void ReleaseConversionInput()\n        {','        void ReleaseConversionInput()\n        {\n            ReleaseColorBallContactPoses();')
stage(path,s)

path='Assets/ReferenceGameplay/Editor/ReferenceFallColorChecks.cs'
s=read(path)
test='''        static IEnumerator CompetingTargetOwnership(RoyalQaService qa)
        {
            yield return qa.Practice(); yield return new WaitForSeconds(.15f);
            var lm = LevelManager.THIS; var director = RoyalPowerupDirector.Get;
            var ball = lm.GetSquare(3, 3).item; Set(ball, ItemsTypes.BOMB, 0);
            int colour = Majority(lm.GetItems());
            var targets = lm.GetItems().Where(i => Eligible(i) && i.color == colour)
                .OrderBy(i => i.square.row).ThenBy(i => i.square.col).ToArray();
            Check(targets.Length > 6, "Competing conversion check has enough sequential targets");
            Check(RoyalPowerupDirector.Tap(ball), "Competing conversion check starts real ordinary activation");
            bool injected = false, verified = false; float until = Time.realtimeSinceStartup + 20;
            while (RoyalPowerupDirector.Busy && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (!injected && director.ColorBallBeamArrivals >= 3 && director.ColorBallState == RoyalPowerupDirector.ColorBallPhase.Converting)
                {
                    // Inject precisely the competing converter's state transition. No
                    // fake hits: the ordinary action still owns its real beam sequence.
                    var first = targets[0]; var last = targets[targets.Length - 1];
                    Check(first.GetComponent<ColorBallChargedTargetV4>() != null && last.GetComponent<ColorBallChargedTargetV4>() == null,
                        "Competing transitions occur after charge and before the final launch");
                    first.NextType = ItemsTypes.HELICOPTER; first.ChangeType();
                    last.NextType = ItemsTypes.PACKAGE; last.ChangeType(); injected = true;
                }
                if (injected && director.ColorBallState == RoyalPowerupDirector.ColorBallPhase.Finish)
                {
                    Check(targets[0] && !targets[0].destroying && targets[0].currentType == ItemsTypes.HELICOPTER,
                        "Ordinary batch cannot fire a competing charged-target conversion");
                    var last = targets[targets.Length - 1];
                    Check(last && !last.destroying && last.currentType == ItemsTypes.PACKAGE,
                        "Late target conversion is not acquired or fired by this sequence");
                    Check(director.ColorBallBeamArrivals == targets.Length - 1 && director.ColorBallOrdinaryClears == targets.Length - 2,
                        "Skipped target produces no beam contact or duplicate ordinary hit");
                    verified = true; break;
                }
            }
            Check(injected && verified, "Competing converter ownership regression reaches its assertion frame");
            director.Cancel();
        }
        static IEnumerator PayloadContactCancel(RoyalQaService qa)
        {
            yield return qa.Practice(); yield return new WaitForSeconds(.15f);
            var lm = LevelManager.THIS; var director = RoyalPowerupDirector.Get;
            var ball = lm.GetSquare(3, 3).item; var partner = lm.GetSquare(4, 3).item;
            Set(ball, ItemsTypes.BOMB, 0); Set(partner, ItemsTypes.PACKAGE, 0);
            int colour = Majority(lm.GetItems());
            var targets = lm.GetItems().Where(i => Eligible(i) && i.color == colour).ToArray();
            var scales = targets.Select(i => i.transform.localScale).ToArray();
            Check(RoyalPowerupDirector.TrySwap(ball, partner), "Payload-cancel check enters real combo path"); lm.FindMatches();
            bool cancelled = false; float until = Time.realtimeSinceStartup + 15;
            while (RoyalPowerupDirector.Busy && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (director.ColorBallState != RoyalPowerupDirector.ColorBallPhase.Converting) continue;
                int squeezed = Array.FindIndex(targets, i => i && i.currentType == ItemsTypes.NONE &&
                    (i.transform.localScale - scales[Array.IndexOf(targets, i)]).sqrMagnitude > .000001f);
                if (squeezed < 0) continue;
                director.Cancel(); cancelled = true;
                for (int i = 0; i < targets.Length; i++)
                    Check(targets[i] && (targets[i].transform.localScale - scales[i]).sqrMagnitude < .000001f && targets[i].inputConversionLocks == 0,
                        "Payload cancellation restores scale and input lease " + i);
                Check(ColorBallVfxV4.ActiveBeams == 0 && ColorBallChargedTargetV4.Active == 0 && ColorBallAudioV4.Active == 0,
                    "Payload cancellation clears beams, target holds and local audio");
                break;
            }
            Check(cancelled, "Cancellation exercised during the actual payload contact squeeze");
        }
'''
s=replace(s,'        public static IEnumerator Run()', test+'        public static IEnumerator Run()')
s=replace(s,'''                yield return OrdinaryAction(qa, false, false);
                yield return OrdinaryAction(qa, true, false);
                yield return OrdinaryAction(qa, false, true);
                yield return PayloadAction(qa, ItemsTypes.PACKAGE);
                yield return PayloadAction(qa, ItemsTypes.HELICOPTER);''','''                foreach (int rate in new[] { 30, 60 })
                {
                    Application.targetFrameRate = rate;
                    results.Add("GAMEPLAY CASES requested cap=" + rate);
                    yield return OrdinaryAction(qa, false, false);
                    yield return OrdinaryAction(qa, true, false);
                    yield return OrdinaryAction(qa, false, true);
                    yield return PayloadAction(qa, ItemsTypes.PACKAGE);
                    yield return PayloadAction(qa, ItemsTypes.HELICOPTER);
                    yield return CompetingTargetOwnership(qa);
                    yield return PayloadContactCancel(qa);
                }''')
stage(path,s)

patch=[]
for path, data in files.items():
    patch.extend(difflib.unified_diff(data['before'].splitlines(True), data['after'].splitlines(True), fromfile='a/'+path, tofile='b/'+path))
(out/'fall-color-ownership.patch').write_text(''.join(patch), encoding='utf-8', newline='\n')
(out/'base-hashes.json').write_text(json.dumps({p:d['sha256'] for p,d in files.items()}, indent=2), encoding='utf-8')
print('\n'.join(files))
