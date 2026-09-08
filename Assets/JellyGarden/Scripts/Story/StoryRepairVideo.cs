// Optional authored repair movies. Areas without a clip keep the existing VFX.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace JellyGarden.Scripts.Story
{
    public sealed class StoryRepairVideo : MonoBehaviour
    {
        private VideoPlayer _player;
        private RawImage _image;
        private RenderTexture _target;

        public bool HasFrame { get; private set; }
        public bool Finished { get; private set; }
        public bool Failed { get; private set; }
        public string Failure { get; private set; }
        public double Duration { get; private set; }

        public static string ResourcePath(int areaId, int taskIndex)
        {
            return string.Format("Story/RepairAnimations/area_{0:000}_task{1}", areaId, taskIndex);
        }

        public static VideoClip Load(int areaId, int taskIndex)
        {
            if (areaId <= 0 || taskIndex <= 0) return null;
            return Resources.Load<VideoClip>(ResourcePath(areaId, taskIndex));
        }

        public static StoryRepairVideo Attach(RectTransform parent)
        {
            RectTransform root = StoryUi.CreateRect("RepairVideo", parent);
            StoryUi.Stretch(root);
            var video = root.gameObject.AddComponent<StoryRepairVideo>();
            video._image = root.gameObject.AddComponent<RawImage>();
            video._image.raycastTarget = false;
            video.SetOpacity(0f);
            var fitter = root.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            video._player = root.gameObject.AddComponent<VideoPlayer>();
            video._player.playOnAwake = false;
            video._player.isLooping = false;
            video._player.audioOutputMode = VideoAudioOutputMode.None;
            video._player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            video._player.renderMode = VideoRenderMode.RenderTexture;
            video._player.waitForFirstFrame = true;
            video._player.skipOnDrop = true;
            video._player.sendFrameReadyEvents = true;
            video._player.prepareCompleted += video.OnPrepared;
            video._player.frameReady += video.OnFrame;
            video._player.loopPointReached += video.OnFinished;
            video._player.errorReceived += video.OnError;
            return video;
        }

        public void Prepare(VideoClip clip)
        {
            Release();
            HasFrame = Finished = Failed = false;
            Failure = null;
            Duration = clip != null ? clip.length : 0d;
            if (clip == null || clip.width == 0 || clip.height == 0)
            {
                Fail("The repair clip has no video frames.");
                return;
            }

            try
            {
                GetComponent<AspectRatioFitter>().aspectRatio = (float)clip.width / clip.height;
                _target = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Story repair video",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!_target.Create())
                {
                    Fail("Could not allocate the repair video surface.");
                    return;
                }
                _image.texture = _target;
                _player.targetTexture = _target;
                _player.source = VideoSource.VideoClip;
                _player.clip = clip;
                _player.Prepare();
            }
            catch (global::System.Exception e)
            {
                Fail(e.Message);
            }
        }

        public void SetOpacity(float alpha)
        {
            if (_image != null) _image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        public void SetCovering(bool cover)
        {
            GetComponent<AspectRatioFitter>().aspectMode = cover
                ? AspectRatioFitter.AspectMode.EnvelopeParent
                : AspectRatioFitter.AspectMode.FitInParent;
        }

        public void Fail(string reason)
        {
            Failed = true;
            Failure = reason;
        }

        private void OnPrepared(VideoPlayer player)
        {
            if (!Failed && isActiveAndEnabled) player.Play();
        }

        private void OnFrame(VideoPlayer player, long frame) { HasFrame = true; }
        private void OnFinished(VideoPlayer player) { Finished = true; }
        private void OnError(VideoPlayer player, string message) { Fail(message); }

        public void Release()
        {
            SetOpacity(0f);
            if (_player != null)
            {
                _player.Stop();
                _player.targetTexture = null;
                _player.clip = null;
            }
            if (_image != null) _image.texture = null;
            if (_target != null)
            {
                _target.Release();
                if (Application.isPlaying) Destroy(_target);
                else DestroyImmediate(_target);
                _target = null;
            }
        }

        private void OnDisable() { Release(); }

        private void OnDestroy()
        {
            Release();
            if (_player == null) return;
            _player.prepareCompleted -= OnPrepared;
            _player.frameReady -= OnFrame;
            _player.loopPointReached -= OnFinished;
            _player.errorReceived -= OnError;
        }
    }
}
