using System;
using UnityEngine;

namespace RoyalThrone.Characters.Activities
{
    /// <summary>Retains native SpriteRenderer animation bindings while rendering the recovered sprite mesh.
    /// Source SpriteRenderer is a property target only; its material never draws a second character.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class NativeKingDrillPart : MonoBehaviour
    {
        [SerializeField] SpriteRenderer source;
        [SerializeField] Renderer output;
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] SkinnedMeshRenderer skin;
        [SerializeField] Sprite[] sprites = Array.Empty<Sprite>();
        [SerializeField] Mesh[] meshes = Array.Empty<Mesh>();
        [SerializeField] bool sceneOverlay;
        [SerializeField] bool sceneOverlayVisible;
        Sprite lastSprite;
        MaterialPropertyBlock properties;
        bool initialized;

        public SpriteRenderer Source => source;
        public Renderer Output => output;
        public bool IsSceneOverlay => sceneOverlay;

        public void Configure(SpriteRenderer spriteRenderer, Renderer meshRenderer, Sprite[] spriteAssets, Mesh[] meshAssets, bool isSceneOverlay = false)
        {
            source = spriteRenderer; output = meshRenderer;
            meshFilter = output.GetComponent<MeshFilter>(); skin = output as SkinnedMeshRenderer;
            sprites = spriteAssets; meshes = meshAssets; initialized = false;
            sceneOverlay = isSceneOverlay; sceneOverlayVisible = false;
            RefreshNow();
        }

        /// <summary>The native drill-room wall shadow is preserved, but hidden in the standalone character presentation.</summary>
        public void SetSceneOverlayVisible(bool value) { sceneOverlayVisible = value; RefreshNow(); }

        void OnEnable() { initialized = false; RefreshNow(); }
        void LateUpdate() { RefreshNow(); }
        void OnDisable() { if (output) output.enabled = false; }

        public void RefreshNow()
        {
            if (!source || !output) return;
            source.forceRenderingOff = true;
            if (!initialized || lastSprite != source.sprite)
            {
                initialized = true; lastSprite = source.sprite;
                Mesh mesh = null;
                if (lastSprite)
                    for (int i = 0; i < sprites.Length && i < meshes.Length; i++)
                        if (sprites[i] == lastSprite) { mesh = meshes[i]; break; }
                if (skin) skin.sharedMesh = mesh;
                else if (meshFilter) meshFilter.sharedMesh = mesh;
            }
            bool hasMesh = skin ? skin.sharedMesh != null : meshFilter && meshFilter.sharedMesh;
            output.enabled = source.enabled && source.sprite && hasMesh && (!sceneOverlay || sceneOverlayVisible);
            output.sortingLayerID = source.sortingLayerID;
            output.sortingOrder = source.sortingOrder;
            if (properties == null) properties = new MaterialPropertyBlock();
            properties.Clear();
            properties.SetColor("_Color", source.color);
            properties.SetColor("_RendererColor", Color.white);
            properties.SetVector("_Flip", new Vector4(source.flipX ? -1f : 1f, source.flipY ? -1f : 1f, 1, 1));
            output.SetPropertyBlock(properties);
        }
    }
}
