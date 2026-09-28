#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Imports only the inspected rfv387 coin clips, particle hierarchies and two new audio cues.</summary>
public static class ReferenceCoinImporter
{
    const string Design = "Design/Coin-Reference/";
    const string ClipFolder = "Assets/ReferenceGameplay/Animation/Clips/";
    const string VfxFolder = "Assets/ReferenceGameplay/VFX/Resources/ReferenceGameplay/VFX/";
    const string AudioFolder = "Assets/ReferenceGameplay/Audio/Resources/ReferenceGameplay/Audio/";
    static readonly string[] ClipNames =
    {
        "CoinFlipAnimation", "CoinFlipNoLoopAnimation", "CoinIdleAnimation", "CoinTapAnimation"
    };
    static readonly HashSet<string> ApprovedSprites = new HashSet<string>(StringComparer.Ordinal)
    {
        "Items-CoinItem-coin", "Items-CoinItem-coin.02", "Items-CoinItem-coin.03",
        "Items-CoinItem-coin.04", "Items-CoinItem-coin.05", "Items-CoinItem-coin.10",
        "Items-CoinItem-coin.11", "Items-CoinItem-coin.10_g", "Items-CoinItem-coin.11_g"
    };
    static readonly string[] AudioPaths =
    {
        AudioFolder + "coin_counter__rfv387_17638.wav",
        AudioFolder + "coin_appear__rfv387_17675.wav"
    };

    [Serializable] public class ClipDocument { public ReferenceGameplayMediaImporter.ClipData[] clips; }
    [Serializable] public class NamedDocument { public NamedClip[] clips; }
    [Serializable] public class NamedClip { public NamedCurve[] curves; }
    [Serializable] public class NamedCurve { public bool isPPtr; public NamedKey[] keys; }
    [Serializable] public class NamedKey { public long objectPathId; public string spriteName; }
    [Serializable] public class SpriteCatalog { public CatalogSprite[] sprites; }
    [Serializable] public class CatalogSprite { public string name, assetPath; }
    [Serializable] public class ClipResult
    {
        public string name, assetPath, firstSprite;
        public long sourcePathId;
        public float duration, frameRate;
        public bool loop, initialSpriteAtZero, flipXUsesConstantTangents;
        public int floatCurves, objectCurves, spriteKeys;
    }
    [Serializable] public class ImportResult
    {
        public bool ok;
        public string source = "royal_thron_rfv387.apk/resources.assets";
        public string error;
        public ClipResult[] clips;
        public string[] particlePrefabs, audioAssets, notes;
    }

    static string Project => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    [MenuItem("Royal Throne/Reference Gameplay/Import Coin Media Only")]
    public static void Build()
    {
        var result = new ImportResult();
        try
        {
            string json = "{\"clips\":" + File.ReadAllText(Path.Combine(Project, Design + "DecodedCoinAnimations.json")) + "}";
            var input = JsonUtility.FromJson<ClipDocument>(json);
            var named = JsonUtility.FromJson<NamedDocument>(json);
            if (input.clips == null || input.clips.Length != ClipNames.Length ||
                !input.clips.Select(c => c.name).OrderBy(n => n).SequenceEqual(ClipNames.OrderBy(n => n)))
                throw new InvalidOperationException("Expected exactly the four inspected native coin clips.");

            var particles = JsonUtility.FromJson<ReferenceGameplayVfxImporter.Document>(
                File.ReadAllText(Path.Combine(Project, Design + "NativeCoinParticles.json")));
            foreach (var texture in particles.textures)
                if (!File.Exists(Path.Combine(Project, texture.assetPath)))
                    throw new FileNotFoundException("Run Design/Coin-Reference/prepare_coin_import.py first.", texture.assetPath);
            foreach (string path in AudioPaths)
                if (!File.Exists(Path.Combine(Project, path)))
                    throw new FileNotFoundException("Missing exact extracted coin audio.", path);

            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<SpriteCatalog>(
                File.ReadAllText(Path.Combine(Project, "Assets/ReferenceGameplay/Art/NativeSpriteCatalog.json")));
            var byName = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (string name in ApprovedSprites)
            {
                var entries = catalog.sprites.Where(s => s.name == name).ToArray();
                if (entries.Length != 1) throw new InvalidOperationException("Ambiguous approved coin sprite: " + name);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entries[0].assetPath);
                if (!sprite) throw new InvalidOperationException("Missing approved coin sprite: " + entries[0].assetPath);
                byName.Add(name, sprite);
            }

            // APK versions have different path IDs. Match the recovered sprite name
            // to the established club artwork, never to the old catalog's numeric ID.
            var bySourceId = new Dictionary<long, Sprite>();
            foreach (var clip in named.clips)
            foreach (var curve in clip.curves.Where(c => c.isPPtr))
            foreach (var key in curve.keys)
            {
                if (key.objectPathId == 0) continue;
                if (!byName.TryGetValue(key.spriteName ?? "", out var sprite))
                    throw new InvalidOperationException("Unapproved coin sprite binding: " + key.spriteName);
                if (bySourceId.TryGetValue(key.objectPathId, out var existing) && existing != sprite)
                    throw new InvalidOperationException("Conflicting native sprite ID: " + key.objectPathId);
                bySourceId[key.objectPathId] = sprite;
            }
            foreach (var source in input.clips)
            {
                if (source.missingBindings != null && source.missingBindings.Length != 0)
                    throw new InvalidOperationException("Unresolved coin binding in " + source.name);
                var spriteCurve = source.curves.Single(c => c.property == "m_Sprite" && c.path == "");
                if (spriteCurve.keys.Length == 0 || Mathf.Abs(spriteCurve.keys[0].time) > .000001f ||
                    !bySourceId.ContainsKey(spriteCurve.keys[0].objectPathId))
                    throw new InvalidOperationException("Initial streamed sprite sentinel was not decoded: " + source.name);
            }

            foreach (string path in AudioPaths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var audio = AssetImporter.GetAtPath(path) as AudioImporter;
                if (audio == null) throw new InvalidOperationException("Audio importer unavailable: " + path);
                var settings = audio.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                audio.defaultSampleSettings = settings;
                audio.forceToMono = false;
                audio.SaveAndReimport();
            }
            ReferenceGameplayVfxImporter.BuildFile(Design + "NativeCoinParticles.json");

            Directory.CreateDirectory(Path.Combine(Project, ClipFolder));
            AssetDatabase.Refresh();
            var clipResults = new List<ClipResult>();
            foreach (var source in input.clips)
            {
                string path = ClipFolder + source.name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (!clip)
                {
                    clip = new AnimationClip();
                    AssetDatabase.CreateAsset(clip, path);
                }
                else clip.ClearCurves();
                clip.name = source.name;
                clip.frameRate = source.frameRate;
                clip.legacy = false;

                var clipResult = new ClipResult
                {
                    name = source.name, assetPath = path, sourcePathId = source.pathId,
                    duration = source.duration, frameRate = source.frameRate, loop = source.loop,
                    flipXUsesConstantTangents = true
                };
                foreach (var curve in source.curves)
                {
                    Type type = curve.typeID == 4 ? typeof(Transform) : curve.typeID == 212 ? typeof(SpriteRenderer) :
                        curve.typeID == 210 ? typeof(SortingGroup) : curve.typeID == 1 ? typeof(GameObject) : null;
                    if (type == null) throw new InvalidOperationException("Unsupported native coin curve type " + curve.typeID);
                    var binding = new EditorCurveBinding { path = curve.path, type = type, propertyName = curve.property };
                    if (curve.isPPtr)
                    {
                        var keys = curve.keys.Select(k => new ObjectReferenceKeyframe
                        {
                            time = k.time, value = k.objectPathId == 0 ? null : bySourceId[k.objectPathId]
                        }).ToArray();
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                        clipResult.objectCurves++;
                        if (curve.property == "m_Sprite" && curve.path == "")
                        {
                            var imported = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                            clipResult.initialSpriteAtZero = imported.Length > 0 && imported[0].time == 0 && imported[0].value == keys[0].value;
                            clipResult.firstSprite = imported[0].value.name;
                            clipResult.spriteKeys = imported.Length;
                            if (!clipResult.initialSpriteAtZero) throw new InvalidOperationException("Lost initial coin frame: " + source.name);
                        }
                    }
                    else
                    {
                        var anim = new AnimationCurve(curve.keys.Select(k => new Keyframe(k.time, k.value, k.inSlope, k.outSlope)).ToArray());
                        bool stepped = curve.property == "m_FlipX" || curve.property == "m_FlipY" ||
                            curve.property == "m_IsActive" || curve.property == "m_Enabled" || curve.property == "m_SortingOrder";
                        if (stepped)
                            for (int k = 0; k < anim.length; k++)
                            {
                                AnimationUtility.SetKeyLeftTangentMode(anim, k, AnimationUtility.TangentMode.Constant);
                                AnimationUtility.SetKeyRightTangentMode(anim, k, AnimationUtility.TangentMode.Constant);
                            }
                        AnimationUtility.SetEditorCurve(clip, binding, anim);
                        clipResult.floatCurves++;
                        if (curve.property == "m_FlipX")
                        {
                            var imported = AnimationUtility.GetEditorCurve(clip, binding);
                            for (int k = 0; k < imported.length; k++)
                                clipResult.flipXUsesConstantTangents &=
                                    AnimationUtility.GetKeyLeftTangentMode(imported, k) == AnimationUtility.TangentMode.Constant &&
                                    AnimationUtility.GetKeyRightTangentMode(imported, k) == AnimationUtility.TangentMode.Constant;
                            if (!clipResult.flipXUsesConstantTangents) throw new InvalidOperationException("Lost coin flip steps: " + source.name);
                        }
                    }
                }
                var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
                clipSettings.startTime = 0;
                clipSettings.stopTime = source.duration;
                clipSettings.loopTime = source.loop;
                AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
                AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
                EditorUtility.SetDirty(clip);
                clipResults.Add(clipResult);
            }
            AssetDatabase.SaveAssets();
            result.clips = clipResults.ToArray();
            result.particlePrefabs = particles.prefabs.Select(p => VfxFolder + p.name + ".prefab").ToArray();
            result.audioAssets = AudioPaths;
            result.notes = new[]
            {
                "Only the four native coin clips, named coin particle hierarchies and two new audio cues were imported.",
                "Sprite IDs were remapped by source sprite name to the nine established club coin sprites.",
                "Initial sprites come from each clip's own raw streamed sentinel; FlipX tangents remain constant.",
                "Original clip duration, frame rate, loop setting, transform values and slopes are retained.",
                "Original native data, extraction provenance and hashes remain under Design/Coin-Reference.",
                "Native gameplay method bodies and source event callbacks are not part of the imported media."
            };
            result.ok = true;
            Debug.Log("REFERENCE_COIN_IMPORT_COMPLETE: " + result.clips.Length + " native clips; " +
                result.particlePrefabs.Length + " particle prefabs; " + AudioPaths.Length + " exact audio cues.");
        }
        catch (Exception exception)
        {
            result.error = exception.ToString();
            throw;
        }
        finally
        {
            File.WriteAllText(Path.Combine(Project, Design + "import-result.json"), JsonUtility.ToJson(result, true));
        }
    }
}
#endif
