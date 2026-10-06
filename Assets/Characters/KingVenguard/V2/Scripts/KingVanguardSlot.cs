using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters
{
    /// <summary>Mounts only the new APK-derived portrait or celebration prefab in an existing UI slot.</summary>
    [DisallowMultipleComponent]
    public sealed class KingVanguardSlot : MonoBehaviour
    {
        public enum Mode { Idle, HappyOnShow, SadOnShow, ExcitedOnShow }
        public const string PortraitResource = "KingVanguard/KingVanguard";
        public const string CelebrationResource = "KingVanguard/KingVanguardCelebration";
        [SerializeField] Graphic previousGraphic;
        [SerializeField] GameObject holder;
        [SerializeField] KingVanguardCharacter character;
        [SerializeField] Mode mode;
        bool shown = true, restoreGraphic;
        public KingVanguardCharacter Character => character;
        public bool Mounted => character;

        public static KingVanguardSlot Mount(Graphic slot, KingVanguardCharacter.Framing framing, Mode mode = Mode.Idle,
            RectTransform area = null, bool clipToArea = true)
        {
            if (!slot) return null;
            var mount = slot.GetComponent<KingVanguardSlot>();
            if (mount && mount.character)
            {
                mount.mode = mode;
                mount.character.Frame((RectTransform)mount.holder.transform, framing);
                return mount;
            }
            string resource = framing == KingVanguardCharacter.Framing.Celebration ? CelebrationResource : PortraitResource;
            var prefab = Resources.Load<GameObject>(resource);
            // An old character is never exposed as a fallback if the new import is missing.
            slot.enabled = false;
            if (!prefab) { Debug.LogError("King Vanguard prefab is missing: Resources/" + resource); return null; }
            if (!mount) mount = slot.gameObject.AddComponent<KingVanguardSlot>();
            mount.previousGraphic = slot; mount.mode = mode;
            var go = new GameObject("King Vanguard", typeof(RectTransform)); go.layer = slot.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(area ? area : slot.rectTransform, false); rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (clipToArea) go.AddComponent<RectMask2D>();
            var instance = Instantiate(prefab, rt, false); instance.name = "KingVanguard";
            mount.holder = go; mount.character = instance.GetComponent<KingVanguardCharacter>();
            mount.character.Frame(rt, framing); mount.ApplyShown(); mount.Restart();
            return mount;
        }

        void Restart()
        {
            if (!character || !shown) return;
            character.ResetToIdle();
            if (mode == Mode.HappyOnShow) character.PlayHappy();
            else if (mode == Mode.SadOnShow) character.PlaySad();
            else if (mode == Mode.ExcitedOnShow) character.PlayExcited();
        }

        public void SetShown(bool value, bool showOtherGraphic = false)
        {
            if (shown == value && restoreGraphic == showOtherGraphic) return;
            restoreGraphic = showOtherGraphic;
            shown = value; ApplyShown();
            if (shown) Restart(); else if (character) character.ResetToIdle();
        }
        void ApplyShown()
        {
            if (holder) holder.SetActive(shown && isActiveAndEnabled);
            if (previousGraphic) previousGraphic.enabled = !shown && restoreGraphic;
        }
        public bool PlayHappy() { return shown && character && character.PlayHappy(); }
        public void SetWorried(bool value) { if (shown && character) character.SetWorried(value); }
        void OnEnable() { ApplyShown(); Restart(); }
        void OnDisable() { if (holder) holder.SetActive(false); if (character) character.ResetToIdle(); }
        void OnDestroy() { if (holder) Destroy(holder); }
    }
}
