using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using JellyGarden.Scripts.Story;
using RoyalThrone.ReferenceGameplay.Preview;
public static class RocketFlameGapCheck
{
    public static void Run()
    {
        string output = "outputs/rocket-flame-fix";
        Directory.CreateDirectory(output);
        var host = typeof(StoryOriginalBoostersHost);
        var configure = host.GetMethod("ConfigureRocketFlame", BindingFlags.Static | BindingFlags.NonPublic);
        var emit = host.GetMethod("EmitRocketFlame", BindingFlags.Static | BindingFlags.NonPublic);
        int checks = 0;
        foreach (int fps in new[] { 15, 30, 60 })
        foreach (Vector3 dir in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down })
        foreach (float pitch in new[] { .7f, 1f })
        {
            var go = UnityEngine.Object.Instantiate(ReferenceTnt.VfxPrefab(ReferenceBoosters.RocketTrailVfx));
            go.transform.localScale = Vector3.one * pitch;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg - 90);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var fire = (ParticleSystem[])configure.Invoke(null, new object[] { go });
            if (fire.Length != 2) throw new Exception("Missing native fire layers");
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Play(false);
            Vector3 previous = Vector3.zero;
            emit.Invoke(null, new object[] { fire, previous, previous, pitch });
            for (int frame = 1; frame <= fps / 2; frame++)
            {
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(1f / fps, false, false, false);
                Vector3 nozzle = dir * ReferenceBoosters.RocketCellsPerSecond * frame / fps * pitch;
                go.transform.position = nozzle;
                emit.Invoke(null, new object[] { fire, previous, nozzle, pitch });
                foreach (var ps in fire)
                {
                    var particles = new ParticleSystem.Particle[ps.main.maxParticles];
                    int count = ps.GetParticles(particles);
                    float gap = float.PositiveInfinity;
                    for (int k = 0; k < count; k++) gap = Mathf.Min(gap, Vector3.Distance(particles[k].position, nozzle));
                    if (gap > .001f * pitch) throw new Exception("Detached nozzle flame at " + fps + " FPS: " + gap);
                    checks++;
                }
                previous = nozzle;
                if (fps == 30 && pitch == 1f && frame == 8) Capture(go, dir, nozzle, output);
            }
            ReferenceTnt.StopEmission(go);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                // Simulate advances edit-mode systems even after Stop; turn off automatic
                // emission before this offline lifetime check.
                var emission = ps.emission; emission.enabled = false;
                ps.Simulate(2, false, false, false);
                if (ps.particleCount != 0) throw new Exception("Flame did not retire: " + ps.name);
                checks++;
            }
            UnityEngine.Object.DestroyImmediate(go);
        }
        File.WriteAllText(output + "/result.txt", "PASS " + checks + " native particle checks: all four directions, 15/30/60 FPS, two board scales, nozzle continuity and retirement.\n");
        Debug.Log("ROCKET FLAME GAP CHECK PASSED " + checks);
    }
    static void Capture(GameObject trail, Vector3 dir, Vector3 nozzle, string output)
    {
        string key = dir.x > 0 ? ReferenceBoosters.RocketRightSprite : dir.x < 0 ? ReferenceBoosters.RocketLeftSprite : dir.y > 0 ? ReferenceBoosters.RocketTopSprite : ReferenceBoosters.RocketBottomSprite;
        var head = new GameObject("Rocket head");
        var sr = head.AddComponent<SpriteRenderer>();
        sr.sprite = ReferenceBoosters.Sprite(key); sr.sortingOrder = ReferenceBoosters.FlyerOrder;
        float extent = dir.x != 0 ? sr.bounds.extents.x : sr.bounds.extents.y;
        head.transform.position = nozzle + dir * extent;
        var cameraObject = new GameObject("Capture camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 2.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .12f, .18f);
        camera.transform.position = nozzle - dir * 1.7f + Vector3.back * 10;
        var rt = new RenderTexture(900, 900, 24); camera.targetTexture = rt; camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var image = new Texture2D(900, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0,0,900,900),0,0); image.Apply();
        File.WriteAllBytes(output + "/" + (dir.x > 0 ? "right" : dir.x < 0 ? "left" : dir.y > 0 ? "up" : "down") + ".png", image.EncodeToPNG());
        RenderTexture.active = old; camera.targetTexture = null; rt.Release();
        UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(head);
    }
}
