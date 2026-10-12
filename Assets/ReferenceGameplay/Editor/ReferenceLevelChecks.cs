#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RoyalThrone.Campaign;
using UnityEditor;
using UnityEngine;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    /// <summary>Checks imported data provenance without entering or modifying a player profile.</summary>
    public static class ReferenceLevelChecks
    {
        [Serializable] sealed class Provenance
        {
            public string runtimeResource, runtimeSha256, decodedSource, decodedSourceSha256;
            public string schemaEvidence, schemaEvidenceSha256;
            public SourceLevel[] levels;
        }
        [Serializable] sealed class SourceLevel
        {
            public int number, width, height, moves, payloadBytes;
            public long sourcePathId, sourceByteOffset;
            public string name, sourceSha256, rawPayload;
            public CampaignGoal[] goals;
        }

        [MenuItem("Tools/Royal Throne/Reference Import/Verify imported levels 01–50")]
        public static void VerifyData()
        {
            const string folder = "Design/ReferenceGameplayImport/Levels";
            var evidence = JsonUtility.FromJson<Provenance>(File.ReadAllText("Assets/ReferenceGameplay/Levels/Provenance.json"));
            Require(evidence != null && evidence.levels.Length == 50, "Source manifest must have 50 levels.");
            Require(evidence.runtimeResource == RoyalReferenceCampaign.ResourcePath, "Campaign must load the imported resource.");
            var imported = Resources.Load<TextAsset>(evidence.runtimeResource);
            Require(imported, "Imported level resource is missing.");
            Require(AssetDatabase.GetAssetPath(imported) == "Assets/ReferenceGameplay/Levels/Resources/ReferenceGameplay/levels01-50.json",
                "A stale or duplicate Resources asset supplied the level data.");
            Require(Hash(imported.bytes) == evidence.runtimeSha256, "Imported runtime data differs from the source-checked projection.");
            Require(Hash(File.ReadAllBytes(evidence.decodedSource)) == evidence.decodedSourceSha256, "Decoded source changed after import.");
            Require(Hash(File.ReadAllBytes(evidence.schemaEvidence)) == evidence.schemaEvidenceSha256, "Schema evidence changed after import.");
            var data = RoyalReferenceCampaign.Levels;
            Require(data.Length == 50 && data.Select(l => l.number).SequenceEqual(Enumerable.Range(1, 50)), "Level numbers must be 01–50 without duplicates.");
            int cells = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var level = data[i];
                var source = evidence.levels[i];
                RoyalReferenceCampaign.Validate(level);
                var payload = File.ReadAllBytes(source.rawPayload);
                Require(payload.Length == source.payloadBytes && Hash(payload) == source.sourceSha256,
                    "Original TextAsset payload changed: " + level.number);
                Require(level.number == source.number && level.name == source.name && level.sourceSha256 == source.sourceSha256 &&
                        level.width == source.width && level.height == source.height && level.moves == source.moves,
                    "Imported identity/layout/moves differ: " + level.number);
                Require(level.goals.Length == source.goals.Length && level.goals.Zip(source.goals, (a,b) =>
                    a.name == b.name && a.count == b.count && a.fromSettings == b.fromSettings).All(equal => equal),
                    "Authored objectives differ: " + level.number);
                cells += level.cells.Length;
            }
            Directory.CreateDirectory(folder);
            var result = "PASS: 50 imported levels, " + cells + " cells; original payload hashes, authored moves/objectives, " +
                         "resource identity and runtime loader verified. No profile/save changes.\n" +
                         "This validates imported data, not exact reference runtime behavior. See RULE-PROVENANCE.md.\n";
            File.WriteAllText(folder + "/unity-data-checks.txt", result);
            Debug.Log("[Reference Gameplay] " + result);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        static string Hash(byte[] value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(value)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
