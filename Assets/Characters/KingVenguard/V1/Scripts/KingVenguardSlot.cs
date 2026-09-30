using UnityEngine;
using UnityEngine.UI;

namespace RoyalThrone.Characters
{
    /// <summary>
    /// Puts King Venguard into an existing king image slot without changing the slot's placement or anchors. The slot's
    /// previous sprite stays assigned (recoverable) and is only hidden while Venguard is shown; if the prefab cannot be
    /// loaded the previous sprite simply stays visible and one error is logged. Mounting is idempotent per slot, so a
    /// re-applied skin or a re-opened screen never creates a second king.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KingVenguardSlot : MonoBehaviour
    {
        public enum Mode { Idle, HappyOnShow }
        public const string PrefabResource = "KingVenguard/KingVenguard";

        static GameObject prefab;
        static bool prefabMissing;

        [SerializeField] Graphic previousKing;
        [SerializeField] GameObject holder;
        [SerializeField] KingVenguardCharacter character;
        [SerializeField] Mode mode;
        bool shown = true;

        public KingVenguardCharacter Character => character;
        public bool Mounted => character;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { prefab = null; prefabMissing = false; }

        /// <param name="slot">The image that showed the previous king.</param>
        /// <param name="area">Where to frame Venguard; null frames him in the slot itself.</param>
        /// <param name="clipToArea">Adds a RectMask2D so the bust is cropped to the area (not needed when the caller masks it).</param>
        public static KingVenguardSlot Mount(Graphic slot, KingVenguardCharacter.Framing framing, Mode mode = Mode.Idle,
                                             RectTransform area = null, bool clipToArea = true)
        {
            if (!slot) return null;
            var existing = slot.GetComponent<KingVenguardSlot>();
            if (existing && existing.character)
            {
                existing.mode = mode;
                existing.character.Frame((RectTransform)existing.holder.transform, framing);
                return existing;
            }
            if (!prefab && !prefabMissing)
            {
                prefab = Resources.Load<GameObject>(PrefabResource);
                if (!prefab) { prefabMissing = true; Debug.LogError("King Venguard prefab not found at Resources/" + PrefabResource + "; the previous king sprite stays visible."); }
            }
            if (!prefab) return null;

            var mount = existing ? existing : slot.gameObject.AddComponent<KingVenguardSlot>();
            mount.previousKing = slot; mount.mode = mode;
            var parent = area ? area : slot.rectTransform;
            var go = new GameObject("King Venguard", typeof(RectTransform));
            go.layer = slot.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false); rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(.5f, .5f);
            if (clipToArea) go.AddComponent<RectMask2D>();
            var instance = Instantiate(prefab, rt, false);
            instance.name = "KingVenguard";
            mount.holder = go;
            mount.character = instance.GetComponent<KingVenguardCharacter>();
            mount.character.Frame(rt, framing);
            mount.ApplyShown();
            return mount;
        }

        /// <summary>For shared slots (the home dialog icon): show Venguard, or give the slot back to its other sprites.</summary>
        public void SetShown(bool value)
        {
            if (shown == value) return;
            shown = value; ApplyShown();
            if (shown && mode == Mode.HappyOnShow && character) character.PlayHappy();
        }

        void ApplyShown()
        {
            if (holder) holder.SetActive(shown);
            if (previousKing) previousKing.enabled = !shown || !character;
        }

        public bool PlayHappy() { return shown && character && character.PlayHappy(); }

        void OnEnable()
        {
            if (holder && holder.transform.parent != transform) holder.SetActive(shown);   // a holder framed elsewhere follows this slot
            if (mode == Mode.HappyOnShow && shown && character) character.PlayHappy();
        }

        void OnDisable()
        {
            if (holder && holder.transform.parent != transform) holder.SetActive(false);
            if (character) character.ResetToIdle();   // a card closed mid-reaction reopens on idle, not on the old reaction
        }
    }
}
