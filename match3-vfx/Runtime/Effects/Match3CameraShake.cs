using UnityEngine;

namespace RoyalThrone.Match3Vfx
{
    /// <summary>
    /// Decaying positional shake. The offset is removed before it is re-applied every frame, so
    /// camera scripts that move the camera keep working, and nothing accumulates.
    /// </summary>
    internal sealed class Match3CameraShake
    {
        readonly Match3VfxRig rig;
        Camera camera;
        Vector3 offset;
        float energy;

        public float Energy => energy;

        public Match3CameraShake(Match3VfxRig rig) { this.rig = rig; }

        public void Add(float amount)
        {
            energy = Mathf.Min(1.5f, Mathf.Max(energy, amount));
        }

        public void LateTick(float dt)
        {
            RemoveOffset();
            energy = Mathf.MoveTowards(energy, 0f, dt * 9f);
            var host = rig.Host;
            float strength = rig.Settings.impactShake;
            if (energy <= 0f || strength <= 0f || rig.Paused || host.ReducedMotion) return;
            camera = host.ShakeCamera;
            if (!camera) return;
            offset = new Vector3(Mathf.Sin(Time.time * 83f), Mathf.Cos(Time.time * 97f), 0f) * strength * rig.Cell * energy;
            camera.transform.position += offset;
        }

        void RemoveOffset()
        {
            if (camera && offset != Vector3.zero) camera.transform.position -= offset;
            offset = Vector3.zero;
        }

        public void Reset()
        {
            RemoveOffset();
            energy = 0f;
        }
    }
}
