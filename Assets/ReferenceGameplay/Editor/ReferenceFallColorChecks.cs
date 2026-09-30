#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using RoyalThrone.Boosters.ColorBall.V4;
using UnityEngine;

namespace RoyalThrone.Editor
{
    // Called by the rfv387 harness AFTER it owns an isolated QA save/board.
    public static class ReferenceFallColorChecks
    {
        const string Output = "Design/Gameplay-rfv387/Validation/fall-color.txt";
        static readonly List<string> results = new List<string>();
        static void Check(bool ok, string message)
        {
            results.Add((ok ? "PASS " : "FAIL ") + message);
            if (!ok) throw new InvalidOperationException(message);
        }
        static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        static bool Eligible(Item item) => (bool)Method(typeof(RoyalPowerupDirector), "EligibleColorBallTarget").Invoke(null, new object[] { item });
        static int Majority(IEnumerable<Item> items) => (int)Method(typeof(RoyalPowerupDirector), "MostCommonColorBallColour").Invoke(null, new object[] { items });
        static void Set(Item item, ItemsTypes type, int colour)
        {
            item.StopIdleAnim(); item.currentType = type; item.NextType = ItemsTypes.NONE;
            item.color = type == ItemsTypes.BOMB ? 555 : colour;
            item.GetComponent<RoyalPieceSkin>()?.ApplyNow();
        }
        static IEnumerator Drop(Item item, Vector2 offset, string label)
        {
            var destination = item.square.transform.position + Vector3.back * .2f;
            var restScale = item.transform.localScale;
            item.transform.position = destination + (Vector3)offset;
            item.falling = false; item.StartFalling();
            Check(item.falling, label + ": nonzero drop enters the render-frame fall path");
            float start = Time.realtimeSinceStartup, previous = Vector2.Distance(item.transform.position, destination);
            bool rigid = RoyalPowerupDirector.IsPower(item.currentType);
            int samples = 0; float worstStep = 0;
            while (item.falling)
            {
                yield return null;
                Check(Time.realtimeSinceStartup - start < 4, label + ": drop completes");
                float remaining = Vector2.Distance(item.transform.position, destination);
                // Ignore the tiny rebound once falling ownership is released.
                if (item.falling) Check(remaining <= previous + .0001f, label + ": monotonic approach " + samples);
                worstStep = Mathf.Max(worstStep, previous - remaining); previous = remaining; samples++;
                if (rigid) Check((item.transform.localScale - restScale).sqrMagnitude < .000001f, label + ": rigid scale in flight");
            }
            float end = Time.realtimeSinceStartup + .24f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (rigid) Check((item.transform.localScale - restScale).sqrMagnitude < .000001f, label + ": rigid landing scale");
            }
            Check(Vector3.Distance(item.transform.position, destination) < .0001f, label + ": exact final cell position");
            Check(!item.suppressIdleScale, label + ": landing releases visual ownership");
            results.Add("MEASURE " + label + ": " + samples + " rendered fall samples, maximum step " + worstStep.ToString("F4"));
        }
        static IEnumerator OrdinaryAction(RoyalQaService qa, bool swapped, bool cancel)
        {
            yield return qa.Practice(); yield return new WaitForSeconds(.15f);
            var lm = LevelManager.THIS; var director = RoyalPowerupDirector.Get;
            var ball = lm.GetSquare(3, 3).item; var partner = lm.GetSquare(4, 3).item;
            Set(ball, ItemsTypes.BOMB, 0);
            if (swapped)
            {
                // Make the partner a genuine minority without replacing the board.
                var count = new int[RoyalGameplayArt.BoardColorCount];
                foreach (var i in lm.GetItems()) if (Eligible(i)) count[i.color]++;
                int least = 0; for (int c = 1; c < count.Length; c++) if (count[c] < count[least]) least = c;
                partner.SetColor(least);
            }
            int colour = swapped ? partner.color : Majority(lm.GetItems());
            var targets = lm.GetItems().Where(i => Eligible(i) && i.color == colour).ToArray();
            var origins = targets.Select(i => i.transform.position).ToArray();
            var scales = targets.Select(i => i.transform.localScale).ToArray();
            Check(targets.Length > 2, "Colour action has several real target cells");
            int[] clearedFrames = Enumerable.Repeat(-1, targets.Length).ToArray();
            if (swapped) { Check(RoyalPowerupDirector.TrySwap(ball, partner), "Ordinary swap accepted"); lm.FindMatches(); }
            else Check(RoyalPowerupDirector.Tap(ball), "Direct Color Ball activation accepted");
            float until = Time.realtimeSinceStartup + 35;
            bool chargedSeen = false, cancelled = false;
            while ((RoyalPowerupDirector.Busy || lm.IsResolvingMatches) && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (director.ColorBallState == RoyalPowerupDirector.ColorBallPhase.Converting)
                {
                    Check(director.ColorBallSelectedTargets == targets.Length, swapped ? "Gem swap keeps explicit colour selection" : "Direct activation uses eligible majority");
                    if (director.ColorBallBeamArrivals > 0 && director.ColorBallBeamArrivals < targets.Length)
                    {
                        chargedSeen |= ColorBallChargedTargetV4.Active > 0;
                        Check(targets.All(i => i && !i.destroying), "Hit gems stay in place through subsequent shots");
                        for (int i = 0; i < targets.Length; i++) Check((targets[i].transform.localScale - scales[i]).sqrMagnitude < .000001f, "Ordinary charge uses rigid translation " + i);
                        if (cancel)
                        {
                            director.Cancel(); cancelled = true;
                            Check(ColorBallChargedTargetV4.Active == 0, "Cancel releases every charged target");
                            Check(targets.All(i => i && !i.destroying && i.inputConversionLocks == 0), "Cancel does not issue late hits or retain input locks");
                            for (int i = 0; i < targets.Length; i++) Check(Vector3.Distance(targets[i].transform.position, origins[i]) < .0001f, "Cancel restores target position " + i);
                            yield return null;
                            break;
                        }
                    }
                }
                for (int i = 0; i < targets.Length; i++) if (clearedFrames[i] < 0 && (!targets[i] || targets[i].destroying)) clearedFrames[i] = Time.frameCount;
            }
            Check(Time.realtimeSinceStartup < until, "Colour action returns from gameplay resolver");
            Check(chargedSeen, "Observed charged targets between actual beam contacts");
            if (cancel) Check(cancelled, "Cancellation exercised during live firing");
            else
            {
                Check(director.ColorBallBeamArrivals == targets.Length, "Exactly one beam arrival per selected target");
                Check(director.ColorBallOrdinaryClears == targets.Length, "Exactly one clear per selected target");
                Check(clearedFrames.All(f => f >= 0) && clearedFrames.Max() == clearedFrames.Min(), "All ordinary targets begin clearing in one rendered frame");
                Check(ColorBallChargedTargetV4.Active == 0, "Completed action leaves no target glow/vibration");
                Check(!lm.DragBlocked && !lm.IsResolvingMatches, "Refill/cascades finish and input recovers");
            }
        }
        static void VerifyArrivalLeaseAfterStall()
        {
            ColorBallVfxV4.Prewarm(); ColorBallVfxV4.Clear();
            var lm = LevelManager.THIS;
            var from = lm.GetSquare(3, 3).transform.position;
            var to = lm.GetSquare(4, 4).transform.position;
            var first = ColorBallVfxV4.Launch(from, to, 1);
            Check(first != null, "Arrival fault check acquires one real pooled beam");
            int lease = first.Lease, notices = 0, completedLease = -1;
            first.ArrivalNotice = n => { notices++; completedLease = n; };
            // Model a render update that crosses BOTH arrival and visual retirement.
            // Do not block the Editor, alter gameplay time, or inject fake gameplay hits.
            var flightType = first.GetType();
            float duration = (float)flightType.GetField("duration", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(first);
            flightType.GetField("age", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(first, duration + 1f);
            var host = typeof(ColorBallVfxV4).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var update = typeof(ColorBallVfxV4).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            update.Invoke(host, null);
            Check(notices == 1 && completedLease == lease && !first.Active, "Late render update latches one arrival before retirement");
            var second = ColorBallVfxV4.Launch(from, to, 2);
            Check(ReferenceEquals(first, second) && second.Lease != lease, "The same visual slot is genuinely reused");
            Check(completedLease == lease && notices == 1, "Old arrival remains available after visual lease changes");
            Check(second.ArrivalNotice == null, "Reused beam has no stale target callback");
            second.ArrivalNotice = n => notices++;
            ColorBallVfxV4.Clear(); update.Invoke(host, null);
            Check(notices == 1 && second.ArrivalNotice == null, "Cancellation drops callbacks without a phantom hit");
        }
        static IEnumerator PayloadAction(RoyalQaService qa, ItemsTypes payload)
        {
            yield return qa.Practice(); yield return new WaitForSeconds(.15f);
            var lm = LevelManager.THIS; var director = RoyalPowerupDirector.Get;
            var ball = lm.GetSquare(3, 3).item; var partner = lm.GetSquare(4, 3).item;
            Set(ball, ItemsTypes.BOMB, 0); Set(partner, payload, 0);
            int colour = Majority(lm.GetItems());
            var targets = lm.GetItems().Where(i => Eligible(i) && i.color == colour).ToArray();
            int handoffs = director.ColorBallHandoffs;
            bool sawConversion = false, sawFinish = false, sawTntClip = false;
            Check(RoyalPowerupDirector.TrySwap(ball, partner), "Color Ball + " + payload + " enters real combo path");
            lm.FindMatches();
            float until = Time.realtimeSinceStartup + 45;
            while ((RoyalPowerupDirector.Busy || lm.IsResolvingMatches) && Time.realtimeSinceStartup < until)
            {
                yield return null;
                if (director.ColorBallState == RoyalPowerupDirector.ColorBallPhase.Converting && director.ColorBallConversions > 0)
                {
                    sawConversion = true;
                    Check(targets.All(i => i && !i.destroying), payload + ": converted powerups remain seated until handoff");
                    if (payload == ItemsTypes.PACKAGE)
                        foreach (var item in targets.Where(i => i && i.currentType == payload))
                        {
                            var view = item.GetComponent<RoyalThrone.ReferenceGameplay.ReferencePieceVisual>();
                            if (view) foreach (var animator in view.GetComponentsInChildren<Animator>(true))
                                if (animator.enabled && animator.runtimeAnimatorController && animator.GetCurrentAnimatorStateInfo(0).IsName("TntLightballCreationAnimation")) sawTntClip = true;
                        }
                }
                if (director.ColorBallState == RoyalPowerupDirector.ColorBallPhase.Finish)
                {
                    sawFinish = true;
                    Check(director.ColorBallConversions == targets.Length, payload + ": every selected target converted once");
                    Check(targets.All(i => i && !i.destroying && i.currentType == payload), payload + ": correct payload and no early activation");
                }
            }
            Check(Time.realtimeSinceStartup < until, payload + ": combo and gameplay resolver complete");
            Check(sawConversion && sawFinish, payload + ": observed progressive conversion and finish");
            if (payload == ItemsTypes.PACKAGE) Check(sawTntClip, "TNT conversion uses recovered TntLightballCreationAnimation");
            Check(director.ColorBallHandoffs == handoffs + 1, payload + ": exactly one Color Ball handoff");
            Check(director.ColorBallConversions == targets.Length && director.ColorBallBeamArrivals == targets.Length, payload + ": one arrival/conversion per target");
            Check(!lm.DragBlocked && !lm.IsResolvingMatches && director.PropellerReservedTargets == 0, payload + ": cascades finish, reservations released and input recovers");
            Check(ColorBallChargedTargetV4.Active == 0, payload + ": no ordinary-gem charge presentation leaks into payload combo");
        }
        public static IEnumerator Run()
        {
            results.Clear(); var qa = RoyalQaService.Instance;
            if (!Application.isPlaying || !RoyalSave.Testing || !qa || !qa.Ready)
                throw new InvalidOperationException("The parent harness must own an isolated QA session first.");
            int savedRate = Application.targetFrameRate, savedVsync = QualitySettings.vSyncCount;
            try
            {
                Check(Method(typeof(Item), "FallDistanceAt") != null, "Analytic fall sampler is available");
                var sampler = Method(typeof(Item), "FallDistanceAt");
                float prior = -1;
                for (int i = 0; i <= 60; i++)
                {
                    float t = i / 60f;
                    float at = (float)sampler.Invoke(null, new object[] { t, 12f, 55f, 32f });
                    Check(at >= prior, "Analytic fall distance is monotonic at t=" + t.ToString("F3")); prior = at;
                    if (i % 2 == 0)
                    {
                        float t30 = (i / 2) / 30f;
                        float at30 = (float)sampler.Invoke(null, new object[] { t30, 12f, 55f, 32f });
                        Check(Mathf.Abs(at - at30) < .00001f, "30/60 samples share identical elapsed-time positions");
                    }
                }
                QualitySettings.vSyncCount = 0;
                foreach (int rate in new[] { 30, 60 })
                {
                    Application.targetFrameRate = rate;
                    yield return qa.Practice(); yield return new WaitForSeconds(.2f);
                    var lm = LevelManager.THIS;
                    var item = lm.GetSquare(3, 3).item;
                    Set(item, ItemsTypes.NONE, 2);
                    yield return Drop(item, new Vector2(0, .49f), rate + " cap short gem");
                    Set(item, ItemsTypes.BOMB, 0);
                    yield return Drop(item, Vector2.up * lm.squareHeight * 3, rate + " cap rigid Color Ball");
                    Set(item, ItemsTypes.HELICOPTER, 0);
                    yield return Drop(item, new Vector2(lm.squareWidth, lm.squareHeight * 2), rate + " cap diagonal Propeller");
                }
                yield return qa.Practice(); yield return new WaitForSeconds(.15f);
                var pieces = LevelManager.THIS.GetItems().Where(i => i).ToArray();
                for (int i = 0; i < pieces.Length; i++) Set(pieces[i], ItemsTypes.NONE, (i % 4) + 1);
                // Thirty red Rockets must not outvote eight ordinary pink targets.
                for (int i = 0; i < 30; i++) Set(pieces[i], ItemsTypes.HORIZONTAL_STRIPPED, 0);
                for (int i = 30; i < 46; i++) Set(pieces[i], ItemsTypes.NONE, 4);
                Set(pieces[46], ItemsTypes.INGREDIENT, 0);
                Set(pieces[47], ItemsTypes.BOMB, 0);
                Check(Majority(pieces) == 4, "Direct colour counts exclude retained booster/ingredient colours");
                Check(!Eligible(pieces[0]) && !Eligible(pieces[46]) && !Eligible(pieces[47]), "All booster and ingredient targets excluded");
                var special = pieces[48].gameObject.AddComponent<RoyalThrone.Campaign.RoyalCampaignItem>();
                special.Configure(26);
                Check(!Eligible(pieces[48]), "Falling campaign egg/coin excluded from colour count");
                UnityEngine.Object.Destroy(special);
                VerifyArrivalLeaseAfterStall();
                yield return OrdinaryAction(qa, false, false);
                yield return OrdinaryAction(qa, true, false);
                yield return OrdinaryAction(qa, false, true);
                yield return PayloadAction(qa, ItemsTypes.PACKAGE);
                yield return PayloadAction(qa, ItemsTypes.HELICOPTER);
                results.Add("NOTE Frame caps are requested; capture timestamps from the parent recording establish actual frame pacing.");
                yield return qa.Practice();
                results.Add("PASS focused fall/colour checks complete; parent retains save/session ownership");
            }
            finally
            {
                Application.targetFrameRate = savedRate; QualitySettings.vSyncCount = savedVsync;
                Directory.CreateDirectory(Path.GetDirectoryName(Output)); File.WriteAllLines(Output, results);
            }
        }
    }
}
#endif
