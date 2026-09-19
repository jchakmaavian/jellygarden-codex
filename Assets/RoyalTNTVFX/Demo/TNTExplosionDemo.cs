using UnityEngine;

namespace RoyalTNTVFX.Demo
{
    /// <summary>
    /// Minimal test harness for the TNT explosion. Uses IMGUI and a built-in line shader, so it
    /// needs no Canvas, EventSystem, UI package or TextMeshPro, and reads keys from the IMGUI
    /// event queue so it works with the legacy input manager and the Input System alike.
    ///
    /// Keys: Space = TNT (radius 2), C = TNT+TNT (radius 4), 1 = 1.0x, 2 = 0.5x.
    /// </summary>
    [AddComponentMenu("Royal TNT VFX/TNT Explosion Demo")]
    public sealed class TNTExplosionDemo : MonoBehaviour
    {
        [Tooltip("Player to drive. Empty uses whichever player registered itself.")]
        [SerializeField] TNTExplosionPlayer player;

        [Header("Demo board")]
        [SerializeField] float cellSize = 1f;
        [Tooltip("Draw a faint cell grid so the blast area can be read in cells.")]
        [SerializeField] bool showGrid = true;
        [SerializeField] int gridHalfExtent = 6;

        Material lineMaterial;
        string status = "Ready";

        TNTExplosionPlayer Target => player ? player : TNTExplosionVFX.Player;

        void OnDestroy()
        {
            if (lineMaterial) Destroy(lineMaterial);
            Time.timeScale = 1f;
        }

        void Fire(int radius, string label)
        {
            var target = Target;
            if (!target)
            {
                status = "No TNTExplosionPlayer found - add the prefab to the scene.";
                Debug.LogWarning("[RoyalTNTVFX] " + status);
                return;
            }
            target.Play(transform.position, radius, cellSize);
            status = label + " at " + Time.timeScale.ToString("0.0") + "x";
        }

        void HandleKey(Event e)
        {
            if (e == null || e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.Space: Fire(2, "TNT r2"); e.Use(); break;
                case KeyCode.C: Fire(4, "TNT+TNT r4"); e.Use(); break;
                case KeyCode.Alpha1: Time.timeScale = 1f; e.Use(); break;
                case KeyCode.Alpha2: Time.timeScale = .5f; e.Use(); break;
            }
        }

        void OnGUI()
        {
            HandleKey(Event.current);
            const int w = 200, h = 30, pad = 8;
            GUI.Box(new Rect(pad, pad, w + pad * 2, h * 6 + pad * 5), "Royal TNT VFX");

            int y = pad + 26;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "TNT  radius 2  (Space)")) Fire(2, "TNT r2");
            y += h + 4;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "TNT + TNT  radius 4  (C)")) Fire(4, "TNT+TNT r4");
            y += h + 4;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "Small  radius 1")) Fire(1, "TNT r1");
            y += h + 10;

            bool half = Mathf.Approximately(Time.timeScale, .5f);
            if (GUI.Button(new Rect(pad * 2, y, w / 2 - 2, h), half ? "0.5x ✔" : "0.5x")) Time.timeScale = .5f;
            if (GUI.Button(new Rect(pad * 2 + w / 2 + 2, y, w / 2 - 2, h), half ? "1.0x" : "1.0x ✔")) Time.timeScale = 1f;
            y += h + 6;

            var target = Target;
            GUI.Label(new Rect(pad * 2, y, w, h),
                target ? status + "   |   visible " + target.VisibleRenderers : status);
        }

        void OnRenderObject()
        {
            if (!showGrid) return;
            if (!lineMaterial)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (!shader) return;
                lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                lineMaterial.SetInt("_ZWrite", 0);
            }
            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(new Color(1f, 1f, 1f, .13f));
            // Lines sit on cell borders so the blast centre lands in the middle of a cell.
            Vector3 o = transform.position;
            float half = cellSize * .5f, span = gridHalfExtent * cellSize + half;
            for (int i = -gridHalfExtent; i <= gridHalfExtent + 1; i++)
            {
                float d = i * cellSize - half;
                GL.Vertex(new Vector3(o.x - span, o.y + d, o.z + 1f));
                GL.Vertex(new Vector3(o.x + span, o.y + d, o.z + 1f));
                GL.Vertex(new Vector3(o.x + d, o.y - span, o.z + 1f));
                GL.Vertex(new Vector3(o.x + d, o.y + span, o.z + 1f));
            }
            GL.End();
            GL.PopMatrix();
        }
    }
}
