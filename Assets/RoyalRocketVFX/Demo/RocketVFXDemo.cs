using UnityEngine;

namespace RoyalRocketVFX.Demo
{
    /// <summary>
    /// Minimal test harness for the rocket effect. Deliberately uses IMGUI (OnGUI) and a
    /// built-in line material so the demo pulls in no Canvas, no EventSystem, no UI package and
    /// no TextMeshPro - it drops into any project and any render pipeline.
    ///
    /// Controls: on-screen buttons, or arrow keys to fire and 1 / 2 to set the time scale.
    /// </summary>
    [AddComponentMenu("Royal Rocket VFX/Rocket VFX Demo")]
    public sealed class RocketVFXDemo : MonoBehaviour
    {
        [Tooltip("Player to drive. Empty uses whichever player registered itself.")]
        [SerializeField] RocketVFXPlayer player;

        [Header("Demo board")]
        [Tooltip("World units per cell, passed straight to Play().")]
        [SerializeField] float cellSize = 1f;
        [Tooltip("How many cells each rocket sweeps before it continues off screen.")]
        [SerializeField] float distanceInCells = 8f;
        [Tooltip("Draw a faint reference grid so plume length can be counted in cells.")]
        [SerializeField] bool showGrid = true;
        [SerializeField] int gridHalfExtent = 9;

        Material lineMaterial;
        string status = "Ready";

        RocketVFXPlayer Target => player ? player : RocketVFX.Player;

        void OnDestroy()
        {
            if (lineMaterial) Destroy(lineMaterial);
            Time.timeScale = 1f;
        }

        // Keys are read from the IMGUI event queue rather than UnityEngine.Input, so the demo
        // works whether the importing project uses the legacy input manager or the Input System
        // package. Touching UnityEngine.Input here would throw on Input-System-only projects.
        void HandleKey(Event e)
        {
            if (e == null || e.type != EventType.KeyDown) return;
            switch (e.keyCode)
            {
                case KeyCode.UpArrow: Fire(RocketDirection.Up); e.Use(); break;
                case KeyCode.DownArrow: Fire(RocketDirection.Down); e.Use(); break;
                case KeyCode.LeftArrow: Fire(RocketDirection.Left); e.Use(); break;
                case KeyCode.RightArrow: Fire(RocketDirection.Right); e.Use(); break;
                case KeyCode.Alpha1: Time.timeScale = 1f; e.Use(); break;
                case KeyCode.Alpha2: Time.timeScale = .5f; e.Use(); break;
            }
        }

        void Fire(RocketDirection direction)
        {
            var target = Target;
            if (!target)
            {
                status = "No RocketVFXPlayer found - add a prefab to the scene.";
                Debug.LogWarning("[RoyalRocketVFX] " + status);
                return;
            }
            target.Play(transform.position, direction, distanceInCells, cellSize);
            status = direction + " at " + Time.timeScale.ToString("0.0") + "x";
        }

        void FirePair(RocketAxis axis)
        {
            var target = Target;
            if (!target) { status = "No RocketVFXPlayer found."; return; }
            target.PlayPair(transform.position, axis, distanceInCells, cellSize);
            status = axis + " pair at " + Time.timeScale.ToString("0.0") + "x";
        }

        void OnGUI()
        {
            HandleKey(Event.current);
            const int w = 190, h = 30, pad = 8;
            var box = new Rect(pad, pad, w + pad * 2, h * 8 + pad * 6);
            GUI.Box(box, "Royal Rocket VFX");

            int y = pad + 26;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "Up  (↑)")) Fire(RocketDirection.Up);
            y += h + 4;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "Down  (↓)")) Fire(RocketDirection.Down);
            y += h + 4;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "Left  (←)")) Fire(RocketDirection.Left);
            y += h + 4;
            if (GUI.Button(new Rect(pad * 2, y, w, h), "Right  (→)")) Fire(RocketDirection.Right);
            y += h + 10;

            if (GUI.Button(new Rect(pad * 2, y, w / 2 - 2, h), "H pair")) FirePair(RocketAxis.Horizontal);
            if (GUI.Button(new Rect(pad * 2 + w / 2 + 2, y, w / 2 - 2, h), "V pair")) FirePair(RocketAxis.Vertical);
            y += h + 10;

            bool half = Mathf.Approximately(Time.timeScale, .5f);
            if (GUI.Button(new Rect(pad * 2, y, w / 2 - 2, h), half ? "0.5x ✔" : "0.5x")) Time.timeScale = .5f;
            if (GUI.Button(new Rect(pad * 2 + w / 2 + 2, y, w / 2 - 2, h), half ? "1.0x" : "1.0x ✔")) Time.timeScale = 1f;
            y += h + 6;

            var target = Target;
            GUI.Label(new Rect(pad * 2, y, w, h),
                target ? status + "   |   particles " + target.ParticleCount : status);
        }

        // Faint cell grid, drawn with Unity's built-in colored shader so the demo ships no assets.
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
            Vector3 o = transform.position;
            float span = gridHalfExtent * cellSize;
            for (int i = -gridHalfExtent; i <= gridHalfExtent; i++)
            {
                float d = i * cellSize;
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
