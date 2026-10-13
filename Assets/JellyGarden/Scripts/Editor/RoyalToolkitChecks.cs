using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;

namespace RoyalThrone.Editor
{
    public static class RoyalToolkitChecks
    {
        // Call on the suite's already-painted 3/4/5 fixture in place of directly
        // destroying its detected group. This exercises the actual resolver hook.
        public static IEnumerator ResolveProductionMatch(LevelManager level, Action<bool, string> check, int matchSize, string capturePrefix = null)
        {
            var service = RoyalToolkitImpact.Existing;
            int before = service ? service.PlayedIce : 0;
            bool originalLock = level.DragBlocked;
            var resolving = typeof(LevelManager).GetField("royalResolving", BindingFlags.Instance | BindingFlags.NonPublic);
            // A prior GetCombine assertion can assign a provisional special. Let
            // the production pass make that assignment exactly once for this match.
            foreach (var item in level.GetItems()) if (item) item.NextType = ItemsTypes.NONE;
            try
            {
                service?.Clear(); level.FindMatches();
                float until = Time.realtimeSinceStartup + 15;
                float peakPieceFraction=0; int visibleDestroyFrames=0;
                float started=Time.time;int captureFrame=0;var captureTimes=new[]{.04f,.1f,.2f,.4f,.6f};
                do
                {
                    // Inspect the rendered pose after Animator and RoyalPieceSkin
                    // LateUpdate, including the real stagger before fracture.
                    yield return new WaitForEndOfFrame();
                    if(capturePrefix!=null && captureFrame<captureTimes.Length && Time.time-started>=captureTimes[captureFrame])
                    {
                        var texture=ScreenCapture.CaptureScreenshotAsTexture();
                        System.IO.File.WriteAllBytes(capturePrefix+"-"+captureFrame+".png",texture.EncodeToPNG());
                        UnityEngine.Object.Destroy(texture);captureFrame++;
                    }
                    float cellX=Mathf.Abs(level.squareWidth*level.GameField.lossyScale.x);
                    float cellY=Mathf.Abs(level.squareHeight*level.GameField.lossyScale.y);
                    if(service)foreach(var ghost in service.GetComponentsInChildren<SpriteRenderer>())
                        if(ghost.enabled){visibleDestroyFrames++;var bounds=ghost.bounds.size;peakPieceFraction=Mathf.Max(peakPieceFraction,bounds.x/cellX,bounds.y/cellY);}
                    foreach(var item in level.GameField.GetComponentsInChildren<Item>())
                        if(item.destroying&&item.currentType==ItemsTypes.NONE&&item.sprRenderer&&item.sprRenderer.enabled)
                        {
                            Vector3 bounds=item.sprRenderer.bounds.size;
                            peakPieceFraction=Mathf.Max(peakPieceFraction,bounds.x/cellX,bounds.y/cellY);
                            visibleDestroyFrames++;
                        }
                }
                while ((bool)resolving.GetValue(level) && Time.realtimeSinceStartup < until);
                check(visibleDestroyFrames>0,matchSize+"-match observes the actual visible anticipation and stagger frames");
                check(peakPieceFraction<=RoyalPieceSkin.MaxDestroyCellFill+.01f,
                    matchSize+"-match whole-piece animation remains below1.12cell before fracture (peak "+peakPieceFraction.ToString("F3")+")");
                check(!(bool)resolving.GetValue(level), matchSize + "-match production resolver finishes without a VFX wait");
                // With the imported match explosion present, MatchVFXService deliberately skips the Ice centre accent.
                check(RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Has("MatchItemExplodeParticles") || service && service.PlayedIce > before, matchSize + "-match emits restored Ice accent through production CombineManager and FallingDown");
            }
            finally { level.DragBlocked = originalLock; }
        }
        public static IEnumerator Run(LevelManager level, Action<bool, string> check)
        {
            var library = Resources.Load<RoyalToolkitLibrary>("RoyalGameplay/Toolkit/Library");
            check(library && library.ice && library.fire, "Toolkit derived Ice and Fire prefabs load in Resources");
            foreach (var prefab in new[] { library.ice, library.fire })
            {
                check(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, prefab.name + " has no demo self-destroy or camera scripts");
                check(prefab.GetComponentsInChildren<Light>(true).Length == 0, prefab.name + " does not add realtime point lighting");
                foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    check(!ps.main.loop && !ps.main.playOnAwake && !ps.main.useUnscaledTime && ps.main.stopAction == ParticleSystemStopAction.None,
                        prefab.name + "/" + ps.name + " is finite, reusable and respects game speed");
                    check(ps.main.maxParticles <= 16, prefab.name + "/" + ps.name + " has an explicit particle cap");
                }
                foreach (var material in prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m))
                    check(!material.IsKeywordEnabled("SOFTPART_ON") && !material.IsKeywordEnabled("DEPTHGLOW_ON") && material.shader && material.shader.isSupported,
                        material.name + " renders without orthographic camera depth fades");
            }
            var bursts = Resources.Load<RoyalBurstLibrary>("RoyalGameplay/Toolkit/BurstPieces");
            check(bursts && bursts.bursts.Length == 5, "Five supplied colored burst atlases are sliced into reusable pieces");
            foreach (var burst in bursts.bursts)
                check(burst.pieces.Length >= 4 && burst.pieces.All(p => p.sprite && p.sprite.rect.width < p.sprite.texture.width * .46f),
                    burst.color + " breakup uses separate painted chip sprites rather than a whole burst card");

            float timeScale = Time.timeScale;
            int motion = PlayerPrefs.GetInt("RoyalThrone.HomeMotion", 1); bool hadMotion = PlayerPrefs.HasKey("RoyalThrone.HomeMotion");
            var state = level.gameStatus;
            var backingState = typeof(LevelManager).GetField("GameStatus", BindingFlags.Instance | BindingFlags.NonPublic);
            var random = UnityEngine.Random.state;
            try
            {
                PlayerPrefs.SetInt("RoyalThrone.HomeMotion", 1);
                Time.timeScale = 1;
                var service = RoyalToolkitImpact.Existing;
                check(service && service.Capacity == 6 && service.ParticleCapacity <= 176, "Toolkit prewarms six instances with no more than 176 total particle slots");
                service.Clear();
                int moves = level.Limit, moveId = level.moveID, score = LevelManager.Score;
                var position = level.GameField.position;
                float expectedRandom = UnityEngine.Random.value; UnityEngine.Random.state = random;
                for (int i = 0; i < 20; i++) { RoyalToolkitImpact.TryPlayIce(position, 1); RoyalToolkitImpact.TryPlayFire(position, 1); }
                check(UnityEngine.Random.value == expectedRandom, "Decorative toolkit playback does not consume gameplay random state");
                check(service.ActiveCount == 6 && service.GetComponentsInChildren<ParticleSystem>(true).Length == 48, "Repeated simultaneous impacts stay inside the prewarmed instance pool");
                check(level.Limit == moves && level.moveID == moveId && LevelManager.Score == score && !RoyalPowerupDirector.Busy,
                    "Toolkit tails never change moves, score or power-operation ownership");
                service.Clear();
                Time.timeScale = .5f;
                RoyalToolkitImpact.TryPlayFire(position, 1);
                yield return new WaitForSecondsRealtime(2.1f);
                check(service.ActiveCount == 1 && service.GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.particleCount) > 0,
                    "Fire particles remain visible at 0.5 speed beyond the original one-scaled-second destroy window");
                backingState.SetValue(level, GameState.Pause); yield return null;
                var live = service.GetComponentsInChildren<ParticleSystem>(true).Where(p => p.gameObject.activeInHierarchy).ToArray();
                float[] times = live.Select(p => p.time).ToArray();
                yield return new WaitForSecondsRealtime(.18f);
                check(live.Select((p, i) => Mathf.Abs(p.time - times[i]) < .001f).All(v => v), "Pause freezes every toolkit particle layer and its pool timer");
                backingState.SetValue(level, state);
                yield return new WaitForSecondsRealtime(2.3f);
                check(service.ActiveCount == 0, "Fire completes smoothly and returns to its pool after slow-motion review");
                Time.timeScale = 1;
                RoyalToolkitImpact.TryPlayFire(position, 1); yield return null;
                backingState.SetValue(level, GameState.Map); yield return null;
                check(service.ActiveCount == 0 && service.GetComponentsInChildren<ParticleSystem>(true).Sum(p => p.particleCount) == 0,
                    "Leaving gameplay clears toolkit tails without waiting for gameplay operations");
                RoyalToolkitImpact.TryPlayFire(position, 1); yield return null;
                check(service.ActiveCount == 1, "An intentional toolkit preview is allowed while remaining on Map");
                RoyalPowerupDirector.Get.Cancel();
                check(service.ActiveCount == 0, "Director cancellation also clears independent toolkit instances");
                backingState.SetValue(level, state);
                PlayerPrefs.SetInt("RoyalThrone.HomeMotion", 0);
                check(!RoyalToolkitImpact.TryPlayFire(position, 1) && !RoyalToolkitImpact.TryPlayIce(position, 1),
                    "Reduced motion falls back to the restrained existing native effects");
                check(CandyShatterService.Instance.AuthoredBurstColors == 5, "Physical fragment service prewarms all five supplied burst colors");
                PlayerPrefs.SetInt("RoyalThrone.HomeMotion", 1);
                var shatter = CandyShatterService.Instance;
                var item = level.GetItems().First(i => i && i.sprRenderer && i.currentType == ItemsTypes.NONE);
                int originalColor = item.color; Vector3 sourceScale = item.sprRenderer.transform.localScale;
                float cell = Mathf.Abs(level.squareWidth * level.GameField.lossyScale.x);
                try
                {
                    for (int color = 0; color < 5; color++)
                    {
                        shatter.Clear(); item.color = color;
                        // Board gems now break with the imported match explosion instead of generated chips.
                        if (RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Has("MatchItemExplodeParticles"))
                        {
                            int before = RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Active;
                            shatter.Shatter(item.sprRenderer, 1.4f); item.color = originalColor;
                            check(RoyalThrone.ReferenceGameplay.ReferenceVfxPool.Active > before, "Color " + color + " break plays the imported match explosion");
                            continue;
                        }
                        // Deliberately exaggerate the destruction pose. Chip bounds
                        // must still come from cell pitch on this first visible frame.
                        item.sprRenderer.transform.localScale = sourceScale * 3;
                        shatter.Shatter(item.sprRenderer, 1.4f);
                        item.sprRenderer.transform.localScale = sourceScale; item.color = originalColor;
                        var chips = shatter.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
                        check(chips.Length >= 4 && chips.All(r => r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("burst-")),
                            "Color " + color + " breakup draws the supplied atlas components");
                        check(chips.All(r => Mathf.Max(r.bounds.size.x, r.bounds.size.y) <= cell * .405f),
                            "Color " + color + " chips stay below 0.40 cell despite an enlarged source pose");
                        yield return new WaitForSeconds(.16f);
                        check(chips.Where(r => r.enabled).All(r => Mathf.Max(r.bounds.size.x, r.bounds.size.y) <= cell * .405f),
                            "Color " + color + " chip bounds remain restrained during rotation and gravity");
                    }
                }
                finally { item.color = originalColor; item.sprRenderer.transform.localScale = sourceScale; shatter.Clear(); }
                service.Clear();
                var delayedMethod = typeof(LevelManager).GetMethod("PlayMatchCenterDelayed", BindingFlags.Instance | BindingFlags.NonPublic);
                var delayed = (IEnumerator)delayedMethod.Invoke(level, new object[] { position, Color.white, 1f, .065f });
                check(delayed.MoveNext(), "Production match accent schedules its anticipation beat");
                backingState.SetValue(level, GameState.Map);
                check(!delayed.MoveNext() && service.ActiveCount == 0, "Abandoning a board cancels its delayed Ice accent before it can spawn on Home");
                (delayed as IDisposable)?.Dispose();
            }
            finally
            {
                RoyalToolkitImpact.Existing?.Clear(); backingState.SetValue(level, state); Time.timeScale = timeScale;
                UnityEngine.Random.state = random;
                if (hadMotion) PlayerPrefs.SetInt("RoyalThrone.HomeMotion", motion); else PlayerPrefs.DeleteKey("RoyalThrone.HomeMotion");
            }
        }
    }
}
