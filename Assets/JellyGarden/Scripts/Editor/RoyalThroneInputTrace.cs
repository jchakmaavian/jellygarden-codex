using System;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Opt-in, Editor-only diagnostics. Does not inject input or change game state.</summary>
[InitializeOnLoad]
public static class RoyalThroneInputTrace
{
    private const string MenuPath = "Tools/Royal Throne/Playtest/Toggle input trace _F8";
    private static StreamWriter writer;
    private static Vector2 previousPosition;
    private static bool previousPressed;
    private static double lastWrite;
    private static int samples;

    static RoyalThroneInputTrace()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.quitting += Stop;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Stop();
        };
    }

    [MenuItem(MenuPath)]
    public static void Toggle()
    {
        if (writer != null)
        {
            Stop();
            Debug.Log("Royal Throne input trace stopped. See Temp/RoyalThroneInputTrace.jsonl.");
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("Enter Play Mode, then press F8 in the Game view to record input.");
            return;
        }

        string folder = Path.Combine(Application.dataPath, "../Temp");
        Directory.CreateDirectory(folder);
        writer = new StreamWriter(Path.Combine(folder, "RoyalThroneInputTrace.jsonl"), false);
        samples = 0;
        lastWrite = double.NegativeInfinity;
        previousPressed = false;
        InputSystem.onAfterUpdate += Capture;
        Debug.Log("Royal Throne input trace started. F8 stops; leaving Play Mode also stops. Maximum 2000 samples.");
    }

    private static void Stop()
    {
        InputSystem.onAfterUpdate -= Capture;
        writer?.Dispose();
        writer = null;
    }

    private static void Capture()
    {
        // Editor input updates can use a different coordinate space from the Game view.
        if (writer == null || !EditorApplication.isPlaying ||
            InputState.currentUpdateType == InputUpdateType.Editor) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 position = mouse.position.ReadValue();
        bool pressed = mouse.leftButton.isPressed;
        double now = EditorApplication.timeSinceStartup;
        if (!mouse.leftButton.wasPressedThisFrame && !mouse.leftButton.wasReleasedThisFrame &&
            pressed == previousPressed && (!pressed || position == previousPosition) &&
            now - lastWrite < 1) return;

        InputManager input = UnityEngine.Object.FindAnyObjectByType<InputManager>();
        LevelManager level = LevelManager.THIS;
        Camera camera = Camera.main;
        Vector2 routedPosition = input != null ? input.TouchPosition : position;
        Vector3 worldPosition = camera != null ? camera.ScreenToWorldPoint(routedPosition) : Vector3.zero;
        Collider2D hit = camera != null ? Physics2D.OverlapPoint(worldPosition) : null;
        Item item = hit != null ? hit.GetComponent<Item>() : null;
        var sample = new Sample
        {
            utc = DateTime.UtcNow.ToString("O"),
            frame = Time.frameCount,
            timeScale = Time.timeScale,
            focusedWindow = EditorWindow.focusedWindow != null ? EditorWindow.focusedWindow.GetType().Name : "none",
            width = Screen.width,
            height = Screen.height,
            mouse = position,
            routed = routedPosition,
            pressed = pressed,
            pressEdge = mouse.leftButton.wasPressedThisFrame,
            releaseEdge = mouse.leftButton.wasReleasedThisFrame,
            inputPresent = input != null,
            routedPressed = input != null && input.WasTouchPressed,
            routedReleased = input != null && input.WasTouchReleased,
            routedHeld = input != null && input.IsTouching,
            actionEnabled = input != null && input.Gameplay != null && input.Gameplay.PrimaryTouch.enabled,
            level = level != null ? level.currentLevel : 0,
            state = level != null ? level.gameStatus.ToString() : "no level",
            dragBlocked = level != null && level.DragBlocked,
            moves = level != null ? level.Limit : -1,
            score = LevelManager.Score,
            hit = hit != null ? hit.name : "none",
            hits = camera != null ? Physics2D.OverlapPointAll(worldPosition).Select(c => c.name).ToArray() : Array.Empty<string>(),
            itemColor = item != null ? item.color : -1,
            itemFalling = item != null && item.falling
        };
        writer.WriteLine(JsonUtility.ToJson(sample));
        writer.Flush();
        previousPosition = position;
        previousPressed = pressed;
        lastWrite = now;
        if (++samples >= 2000) Stop();
    }

    [Serializable]
    private class Sample
    {
        public string utc;
        public int frame;
        public float timeScale;
        public string focusedWindow;
        public int width, height;
        public Vector2 mouse, routed;
        public bool pressed, pressEdge, releaseEdge, inputPresent;
        public bool routedPressed, routedReleased, routedHeld, actionEnabled;
        public int level;
        public string state;
        public bool dragBlocked;
        public int moves, score;
        public string hit;
        public string[] hits;
        public int itemColor;
        public bool itemFalling;
    }
}
