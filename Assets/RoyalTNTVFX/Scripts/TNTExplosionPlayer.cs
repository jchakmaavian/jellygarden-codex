using System.Collections;
using UnityEngine;

namespace RoyalTNTVFX
{
    /// <summary>
    /// Plays the Royal Throne TNT explosion: the TNT artwork squashes and glows for a short
    /// ignition, then the blast opens and a spark wave rolls outward ring by ring across a
    /// square of cells. Purely visual - it never destroys anything. Subscribe to
    /// <see cref="CellReached"/> to break your own board pieces in sync with the wave.
    ///
    /// Drop the RoyalTNTVFX prefab into a scene and call
    /// <see cref="TNTExplosionVFX.Play(Vector3, int, float, float)"/>.
    /// </summary>
    [AddComponentMenu("Royal TNT VFX/TNT Explosion Player")]
    [DisallowMultipleComponent]
    public sealed class TNTExplosionPlayer : MonoBehaviour
    {
        // Short hold between the blast opening and the first ring, matching the shipped game.
        const float FirstRingDelay = .012f;

        [Header("Explosion")]
        [Tooltip("Pooled explosion actor. Prefabs/TNTExplosionActor.")]
        [SerializeField] TNTExplosionActor actorPrefab;

        [Tooltip("World units per board cell. Play() can override this per call.")]
        [SerializeField, Min(.01f)] float cellSize = 1f;

        [Tooltip("Delay between successive rings of the spark wave, in seconds.")]
        [SerializeField, Range(0f, .2f)] float blastWavePerCell = .025f;

        [Header("TNT artwork")]
        [Tooltip("Show the TNT sprite squashing and warming up during ignition.")]
        [SerializeField] bool showTNTSprite = true;
        [Tooltip("TNT artwork texture. Textures/TNT.")]
        [SerializeField] Texture2D tntTexture;
        [Tooltip("Pixel rect of the visible artwork inside the texture (x, y, width, height).")]
        [SerializeField] Rect tntTrimRect = new Rect(76, 41, 360, 430);
        [Tooltip("Normalised pivot inside the trim rect.")]
        [SerializeField] Vector2 tntPivot = new Vector2(.5f, .5f);
        [Tooltip("Sprite size as a fraction of one cell.")]
        [SerializeField, Range(.2f, 2f)] float tntSpriteCellFraction = .9f;

        [Header("Rendering")]
        [Tooltip("Sorting layer for every TNT renderer. 'Default' always exists.")]
        [SerializeField] string sortingLayer = "Default";
        [Tooltip("Added to the built-in orders: blast 29-33, impacts 34, stars 35, TNT sprite 36.")]
        [SerializeField] int sortingOrderOffset;

        [Header("Pool")]
        [Tooltip("Explosions that can be on screen at once. The shipped game uses 32.")]
        [SerializeField, Range(1, 64)] int poolSize = 32;
        [Tooltip("Register as the player used by the static TNTExplosionVFX API.")]
        [SerializeField] bool registerAsDefault = true;

        /// <summary>Raised when the blast opens: world position and radius in cells.</summary>
        public event System.Action<Vector3, int> Exploded;
        /// <summary>Raised as the spark wave reaches each cell: world position and cell offset
        /// from the centre (x right, y up). Break your own pieces here.</summary>
        public event System.Action<Vector3, Vector2Int> CellReached;
        /// <summary>Raised when an explosion has fully faded and returned to the pool.</summary>
        public event System.Action<Vector3> Completed;

        /// <summary>Freeze every explosion without touching Time.timeScale.</summary>
        public bool Paused { get; set; }

        /// <summary>True while any explosion or TNT sprite is still visible.</summary>
        public bool IsBusy
        {
            get
            {
                if (entries == null) return false;
                foreach (var e in entries)
                    if (e.actor.gameObject.activeSelf || e.sprite.gameObject.activeSelf) return true;
                return false;
            }
        }

        /// <summary>How many pooled explosion actors are currently active.</summary>
        public int ActiveExplosions
        {
            get
            {
                int n = 0;
                if (entries != null) foreach (var e in entries) if (e.actor.gameObject.activeSelf) n++;
                return n;
            }
        }

        /// <summary>Total renderers still visible across the whole pool. Zero when fully cleaned up.</summary>
        public int VisibleRenderers
        {
            get
            {
                int n = 0;
                if (entries == null) return 0;
                foreach (var e in entries)
                {
                    if (e.sprite.gameObject.activeSelf && e.sprite.enabled) n++;
                    if (!e.actor.gameObject.activeSelf) continue;
                    foreach (var r in e.actor.blast) if (r.enabled) n++;
                    foreach (var r in e.actor.stars) if (r.enabled) n++;
                    foreach (var r in e.actor.impacts) if (r.enabled) n++;
                }
                return n;
            }
        }

        /// <summary>Times a pooled actor has been reused rather than used for the first time.</summary>
        public int Reuses { get; private set; }

        public float CellSize => cellSize;

        sealed class Entry
        {
            public TNTExplosionActor actor;
            public SpriteRenderer sprite;
            public Vector3 origin;
            public int uses;
        }

        Entry[] entries;
        Transform poolRoot;
        Sprite tntSprite;

        void Awake()
        {
            Build();
            if (registerAsDefault) TNTExplosionVFX.Register(this);
        }

        void OnDestroy()
        {
            TNTExplosionVFX.Unregister(this);
            if (tntSprite) Destroy(tntSprite);
        }

        void OnDisable() => Clear();

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Explode at <paramref name="position"/>, sweeping a square of cells out to
        /// <paramref name="radius"/> (2 = 5x5, 4 = 9x9). <paramref name="cellSize"/> is world units
        /// per cell; pass 0 to use the Inspector value. <paramref name="intensity"/> scales the blast;
        /// pass 0 for the game's rule (1.45 when radius is above 2, otherwise 1).
        /// </summary>
        public void Play(Vector3 position, int radius, float cellSize = 0f, float intensity = 0f)
        {
            if (!isActiveAndEnabled) return;
            float cell = Mathf.Max(.01f, cellSize > 0f ? cellSize : this.cellSize);
            radius = Mathf.Max(0, radius);
            float strength = intensity > 0f ? intensity : (radius > 2 ? 1.45f : 1f);
            var entry = Take(position, cell);
            if (entry == null) return;
            StartCoroutine(Sequence(entry, position, radius, cell, strength));
        }

        /// <summary>Stop every explosion immediately and return everything to the pool.</summary>
        public void Clear()
        {
            StopAllCoroutines();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e.actor) e.actor.Cleanup();
                if (e.sprite) e.sprite.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ construction

        void Build()
        {
            poolRoot = new GameObject("TNT explosion pool").transform;
            poolRoot.SetParent(transform, false);

            if (tntTexture)
            {
                var rect = tntTrimRect.width > 0 && tntTrimRect.height > 0
                    ? tntTrimRect
                    : new Rect(0, 0, tntTexture.width, tntTexture.height);
                tntSprite = Sprite.Create(tntTexture, rect, tntPivot, 100f, 0, SpriteMeshType.FullRect);
                tntSprite.name = "TNT";
            }

            if (!actorPrefab)
            {
                Debug.LogError("[RoyalTNTVFX] Actor Prefab is not assigned on " + name + ".", this);
                entries = new Entry[0];
                return;
            }

            entries = new Entry[Mathf.Max(1, poolSize)];
            for (int i = 0; i < entries.Length; i++)
            {
                var actor = Instantiate(actorPrefab, poolRoot);
                actor.name = "TNT explosion " + i;
                actor.Owner = this;
                ApplySorting(actor.blast);
                ApplySorting(actor.stars);
                ApplySorting(actor.impacts);
                actor.gameObject.SetActive(false);

                var spriteObject = new GameObject("TNT sprite " + i);
                spriteObject.transform.SetParent(poolRoot, false);
                var sr = spriteObject.AddComponent<SpriteRenderer>();
                sr.sprite = tntSprite;
                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = 36 + sortingOrderOffset;
                spriteObject.SetActive(false);

                entries[i] = new Entry { actor = actor, sprite = sr };
            }
        }

        void ApplySorting(Renderer[] renderers)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.sortingLayerName = sortingLayer;
                r.sortingOrder += sortingOrderOffset;
            }
        }

        // The pool root may sit under a scaled parent; keep world sizes exactly as authored.
        float ParentScale => poolRoot ? Mathf.Max(.0001f, Mathf.Abs(poolRoot.lossyScale.x)) : 1f;

        Entry Take(Vector3 position, float cell)
        {
            if (entries == null) return null;
            foreach (var e in entries)
            {
                if (e.actor.gameObject.activeSelf || e.sprite.gameObject.activeSelf) continue;
                e.actor.gameObject.SetActive(true);
                e.actor.ResetVisualState();
                e.actor.transform.position = position + Vector3.back * .7f;
                e.actor.transform.localScale = Vector3.one * (cell / ParentScale);
                if (e.uses++ > 0) Reuses++;
                e.origin = position;
                return e;
            }
            return null;
        }

        internal void NotifyActorFinished(TNTExplosionActor actor)
        {
            if (entries == null) return;
            foreach (var e in entries)
                if (e.actor == actor) { Completed?.Invoke(e.origin); return; }
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0;
            while (t < seconds)
            {
                if (!Paused) t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Sequence(Entry entry, Vector3 p, int radius, float cell, float strength)
        {
            var actor = entry.actor;
            actor.Activate();

            // Ignition: the TNT artwork squashes and warms toward yellow while the core glow grows.
            var sr = showTNTSprite && tntSprite ? entry.sprite : null;
            Vector3 scale = Vector3.one;
            if (sr)
            {
                var go = sr.gameObject;
                go.SetActive(true);
                go.transform.position = p + Vector3.back * .6f;
                go.transform.rotation = Quaternion.identity;
                var size = tntSprite.bounds.size;
                go.transform.localScale = Vector3.one * (cell * tntSpriteCellFraction / Mathf.Max(size.x, size.y) / ParentScale);
                sr.color = Color.white;
                sr.enabled = true;
                scale = go.transform.localScale;
            }
            float t = 0;
            while (t < TNTExplosionActor.IgnitionDuration)
            {
                if (!Paused) t += Time.deltaTime;
                float k = Mathf.Clamp01(t / TNTExplosionActor.IgnitionDuration), compress = Mathf.Sin(k * Mathf.PI);
                if (sr)
                {
                    sr.transform.localScale = Vector3.Scale(scale, new Vector3(1 + .10f * compress, 1 - .12f * compress, 1));
                    sr.color = Color.Lerp(Color.white, new Color(1, 1, .55f), k * k * .6f);
                }
                yield return null;
            }
            if (sr) sr.gameObject.SetActive(false);

            actor.Explode(strength);
            Exploded?.Invoke(p, radius);
            yield return Wait(FirstRingDelay);

            // Square rings outward from the centre, top row first, left to right - the same
            // order the game uses, which also decides how impact sparks are recycled.
            for (int distance = 0; distance <= radius; distance++)
            {
                for (int y = -distance; y <= distance; y++)
                    for (int x = -distance; x <= distance; x++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != distance) continue;
                        Vector3 cellPosition = p + new Vector3(x * cell, -y * cell, 0);
                        if (actor.gameObject.activeSelf) actor.CellImpact(cellPosition);
                        CellReached?.Invoke(cellPosition, new Vector2Int(x, -y));
                    }
                yield return Wait(blastWavePerCell);
            }
        }
    }
}
