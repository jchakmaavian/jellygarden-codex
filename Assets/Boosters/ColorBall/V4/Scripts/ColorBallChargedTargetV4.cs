using System.Collections.Generic;
using JellyGarden.Scripts;
using UnityEngine;

namespace RoyalThrone.Boosters.ColorBall.V4
{
    // Reconstructed target hold using the existing outline material. The source
    // metadata exposes ShakeItem/CreateItemBg and AllRaysComplete, not editable code.
    // No targets, hits or conversion callbacks originate in this presentation helper.
    [DefaultExecutionOrder(1250)]
    public sealed class ColorBallChargedTargetV4 : MonoBehaviour
    {
        static readonly HashSet<ColorBallChargedTargetV4> active = new HashSet<ColorBallChargedTargetV4>();
        static Material outlineMaterial;
        Item item; SpriteRenderer outline; Vector3 rest, applied; float age, phase; bool held;
        public int Lease { get; private set; }
        public static int Active => active.Count;
        public static ColorBallChargedTargetV4 Hold(Item owner)
        {
            if (!owner || owner.destroying || owner.currentType != ItemsTypes.NONE) return null;
            var pose = owner.GetComponent<ColorBallChargedTargetV4>();
            if (!pose) pose = owner.gameObject.AddComponent<ColorBallChargedTargetV4>();
            pose.Release(); pose.item = owner; pose.rest = pose.applied = owner.transform.position;
            pose.age = 0; pose.phase = owner.square ? (owner.square.col * .43f + owner.square.row * .71f) : 0;
            pose.held = true; pose.Lease++; pose.enabled = true; active.Add(pose);
            if (!pose.outline && owner.sprRenderer)
            {
                var shader = Resources.Load<Shader>("RoyalColorBall/ColorBallOutline");
                if (shader)
                {
                    if (!outlineMaterial) outlineMaterial = new Material(shader) { name = "Color Ball held target contour" };
                    pose.outline = new GameObject("Charged target contour").AddComponent<SpriteRenderer>();
                    pose.outline.transform.SetParent(owner.sprRenderer.transform, false);
                    pose.outline.sharedMaterial = outlineMaterial;
                    pose.outline.maskInteraction = SpriteMaskInteraction.None;
                }
            }
            if (pose.outline)
            {
                pose.outline.sprite = owner.sprRenderer.sprite;
                pose.outline.sortingLayerID = owner.sprRenderer.sortingLayerID;
                pose.outline.sortingOrder = owner.sprRenderer.sortingOrder + 1;
                pose.outline.gameObject.layer = owner.sprRenderer.gameObject.layer;
                pose.outline.enabled = true;
            }
            return pose;
        }
        void LateUpdate()
        {
            if (!held) return;
            if (!item || item.destroying || item.currentType != ItemsTypes.NONE || item.falling || item.inputSwapLocked ||
                !item.square || item.square.item != item) { Release(); return; }
            if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Pause) return;
            age += Time.deltaTime;
            float cell = ColorBallVisualV4.CellSize;
            float strength = Mathf.SmoothStep(0, 1, age / .06f) * cell * .012f;
            applied = rest + new Vector3(Mathf.Sin(age * 39f + phase), Mathf.Sin(age * 31f + phase) * .48f, 0) * strength;
            item.transform.position = applied;
            if (outline) outline.color = new Color(1, .92f, .60f, .52f + .06f * Mathf.Sin(age * 17f + phase));
        }
        public void Release(int expectedLease) { if (expectedLease == Lease) Release(); }
        void Release()
        {
            if (held && item && !item.destroying && !item.falling && !item.inputSwapLocked &&
                (item.transform.position - applied).sqrMagnitude < .000001f) item.transform.position = rest;
            held = false; active.Remove(this); if (outline) outline.enabled = false;
        }
        public static void ClearAll()
        {
            foreach (var pose in new List<ColorBallChargedTargetV4>(active)) if (pose) pose.Release();
            active.Clear();
        }
        void OnDisable() { Release(); }
        void OnDestroy() { active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { active.Clear(); outlineMaterial = null; }
    }
}
