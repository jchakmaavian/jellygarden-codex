using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.ReferenceGameplay;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalSuperBombFuseChecks
    {
        const string Folder = "Design/QaFeedback/05-SuperBombFuse/after/";
        static GameObject Fuse() => Object.FindObjectsByType<SortingGroup>()
            .Select(g => g.gameObject).FirstOrDefault(g => g.name.StartsWith("TntTntComboFuseParticles"));

        public static IEnumerator Run(LevelManager lm, Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);
            var director = RoyalPowerupDirector.Get;
            float capture = Time.captureDeltaTime;
            try
            {
                foreach (int fps in new[] { 30, 60 })
                {
                    Time.captureDeltaTime = 1f / fps;
                    if (fps == 60) yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                    var a = lm.GetSquare(3,3).item;
                    var b = lm.GetSquare(4,3).item;
                    a.currentType = b.currentType = ItemsTypes.PACKAGE;
                    int plays = director.NativeTntComboPlays;
                    check(RoyalPowerupDirector.TrySwap(a,b), fps + " FPS: two bombs combine through gameplay");
                    int frames = 0, lit = 0;
                    bool attached = true, inFront = true, emitting = true, exploded = false;
                    Vector3 first = Vector3.zero, last = Vector3.zero;
                    float deadline = Time.realtimeSinceStartup + 20;
                    while ((RoyalPowerupDirector.Busy || frames == 0) && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        var fuse = Fuse();
                        var view = Object.FindObjectsByType<ReferenceActivationVisual>().FirstOrDefault(v => v.Active && v.Socket("FitilPartikul").name == "FitilPartikul");
                        if (view && fuse)
                        {
                            var socket = view.Socket("FitilPartikul");
                            if (lit == 0) first = socket.position;
                            last = socket.position; lit++;
                            attached &= Vector3.Distance(fuse.transform.position, socket.position) < .0001f;
                            var body = socket.GetComponentInParent<SortingGroup>();
                            inFront &= body && fuse.GetComponent<SortingGroup>().sortingOrder > body.sortingOrder;
                            emitting &= fuse.GetComponentsInChildren<ParticleSystem>().Any(p => p.isEmitting && p.particleCount > 0);
                            if (lit == 12)
                            {
                                lm.gameStatus = GameState.Pause;
                                yield return null; yield return new WaitForEndOfFrame();
                                Vector3 paused = socket.position;
                                yield return null; yield return new WaitForEndOfFrame();
                                check(Vector3.Distance(paused,socket.position) < .0001f && fuse.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isPlaying), fps + " FPS: fuse and fire pause together");
                                lm.gameStatus = GameState.Playing;
                            }
                        }
                        exploded |= Object.FindObjectsByType<SortingGroup>().Any(g => g.name.StartsWith("TntTntComboExplodeParticles"));
                        if (fps == 60 && frames % 2 == 0)
                        {
                            var image = ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(Folder + frames.ToString("D3") + ".png",image.EncodeToPNG());
                            Object.Destroy(image);
                        }
                        frames++;
                    }
                    check(director.NativeTntComboPlays == plays + 1, fps + " FPS: dedicated super bomb sequence runs once");
                    check(lit > fps / 2 && emitting, fps + " FPS: fire emits throughout the fuse sequence (" + lit + " frames)");
                    check(attached, fps + " FPS: fire remains on the animated tip after every rendered frame");
                    check(inFront, fps + " FPS: fire renders above the bomb body");
                    check(Vector3.Distance(first,last) > .1f, fps + " FPS: burning tip travels with the shortening rope");
                    check(exploded && !RoyalPowerupDirector.Busy, fps + " FPS: fuse completes and super bomb explodes");
                    var tail = Fuse();
                    check(!tail || tail.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isEmitting), fps + " FPS: fuse stops emitting after explosion");
                    director.Cancel();
                    check(!Fuse(), fps + " FPS: cancel clears the pooled fuse");
                }
                // The pool must not retain a previous socket/order on a new lease.
                var socketOwner = new GameObject("QA disposable fuse socket");
                try
                {
                    var handle = ReferenceVfxPool.Play("TntTntComboFuseParticles", Vector3.zero, sustained:true);
                    handle.Follow(socketOwner.transform,37);
                    ReferenceVfxPool.Clear();
                    var reused = ReferenceVfxPool.Play("TntTntComboFuseParticles",Vector3.one,sustained:true);
                    handle.Follow(socketOwner.transform,99);
                    socketOwner.transform.position = Vector3.one * 5;
                    yield return new WaitForEndOfFrame();
                    check(reused.GameObject.transform.position == Vector3.one && reused.GameObject.GetComponent<SortingGroup>().sortingOrder == 35,
                        "Pool reuse clears attachment and restores default order; stale handles cannot modify a new lease");
                    reused.Stop();
                }
                finally { Object.Destroy(socketOwner); }
            }
            finally { Time.captureDeltaTime = capture; director.Cancel(); }
        }
    }
}
