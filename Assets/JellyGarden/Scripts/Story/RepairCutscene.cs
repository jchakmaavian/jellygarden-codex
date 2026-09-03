// What pressing Do It actually looks like.
//
// The name is now half wrong and kept anyway, because the class still IS a
// cutscene on one of its two paths. What it plays depends on whether there is a
// room already on screen to play it over.
//
// ON A ROOM — every repair bought from the area page or from home. An
// animation, not a cutscene: nothing cuts, nothing is written, the room does
// not move, and it is done in under two seconds.
//
//   0.00  gold sparkles fall over the part of the room this repair changes
//   0.70  they stop
//   0.80  a soft flash, and the room's own view crossfades to the repaired
//         state with its neglect wash lifting under it
//   1.35  the star rises away
//   1.85  over
//
// ON ITS OWN SCREEN — the map, the kit's own scenes, anywhere with no room of
// its own. Still a cutscene, and still four seconds, because there is nothing
// else on screen to carry it and the story's line for that repair has nowhere
// else to be said:
//
//   0.0  the room fills a black screen and the camera pushes in
//   0.3  the task's own words appear: "Repave the cracked stones"
//   0.7  the work happens — dust, sparks or embers chosen by what the task IS
//   2.4  a flash as it lands, and the repaired state wipes across
//   2.9  the story's own line for that repair
//   3.6  the star is spent and it fades back to the area
//
// Nothing here is authored per task. The work type comes from the story's own
// verb (RepairWork), the effect from that (RepairVfx), where in the room it
// lands from a pixel diff of the artwork (StoryRepairRegions), and the lines
// from the story data where it has them.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    public sealed class RepairCutscene : MonoBehaviour
    {
        // Two beat maps, because this plays two different things.
        //
        // ON A ROOM it is an animation and it is short. Royal Match's own repair
        // is over in well under two seconds — the sparkles are already falling
        // as the task list leaves, the room has changed before they land, and
        // the player is back on the button. An area costs eight stars, so the
        // repair is watched eight times; four seconds of it is four seconds the
        // eighth time too.
        //
        // ON ITS OWN SCREEN it is still a cutscene, and keeps the slower map it
        // was written with: there is nothing else on screen to look at, the
        // title and the story's line have to be read, and it is not a beat in a
        // screen the player is already standing on.
        private const float TitleIn = 0.3f;
        private const float WorkStart = 0.7f;
        private const float WorkEnd = 2.4f;
        private const float RevealAt = 2.4f;
        private const float LineAt = 2.9f;
        private const float StarAt = 3.6f;
        private const float EndAt = 4.2f;
        private const float FadeTime = 0.35f;

        private const float RoomWorkStart = 0.0f;
        private const float RoomWorkEnd = 0.70f;
        private const float RoomRevealAt = 0.80f;
        private const float RoomStarAt = 1.35f;
        private const float RoomEndAt = 1.85f;
        private const float RoomFadeTime = 0.12f;

        // The chosen map, so Run does not branch on _overRoom eight times.
        private float _workStart;
        private float _workEnd;
        private float _revealAt;
        private float _starAt;
        private float _endAt;
        private float _fadeTime;

        // Where the stage sits when this plays on its own black screen — the map
        // and the kit's own scenes have no room of theirs to play over.
        private const float StageBottomFrac = 0.28f;
        private const float StageTopFrac = 0.94f;
        private const float StageSidePad = 30f;

        private static RepairCutscene _instance;

        public static bool IsPlaying
        {
            get { return _instance != null && _instance._playing; }
        }

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _stage;
        private Image _before;
        private Image _after;
        private Image _damage;
        private Image _flash;
        private Text _title;
        private Text _line;
        private Image _star;
        private Text _skipHint;
        private Image _backdrop;
        private RepairVfx _vfx;

        private bool _playing;
        private bool _skipped;
        private bool _revealed;

        /// <summary>
        /// Whether this repair is playing on a room the player can already see.
        ///
        /// It is the difference between the two things this class does. Over a
        /// room it is an ANIMATION: no words, no black, over in a moment, and
        /// the room never moves. On its own screen it is still a cutscene,
        /// because there is nothing else on screen to carry it — the map and the
        /// kit's own scenes have no room of theirs to play over.
        /// </summary>
        private bool _overRoom;
        private RectTransform _room;
        private Action _onDone;

        /// <summary>
        /// Plays the cutscene for a repair, then calls back. The star has already
        /// been spent by the time this runs — the cutscene shows what the spend
        /// bought, it does not decide it.
        /// </summary>
        public static void Play(int areaId, StoryTask task, int stateAfter, Action onDone)
        {
            Play(areaId, task, stateAfter, null, onDone);
        }

        /// <summary>
        /// As above, played OVER a room that is already on screen: the stage is
        /// matched to <paramref name="room"/>, the black backdrop comes off and
        /// the reveal is the live room's own, so nothing cuts away from the screen
        /// the player pressed Do It on. A null room falls back to the black screen,
        /// which is what the map and the kit's own scenes get.
        ///
        /// Either way this owns the reveal: the room changes on the beat the
        /// repair lands, not when the star was spent, so the wipe has something to
        /// wipe to.
        /// </summary>
        public static void Play(int areaId, StoryTask task, int stateAfter,
            RectTransform room, Action onDone)
        {
            if (task == null)
            {
                if (onDone != null) onDone();
                return;
            }

            // Outside play mode there is no frame loop, so the coroutine that
            // drives the cutscene would never advance and the callback would
            // never fire — the repair would simply hang. Editor tests exercise
            // the economy and the clue rule; the cutscene is verified by running
            // the built player.
            if (!Application.isPlaying)
            {
                AreaSceneView.NotifyTaskCompleted(areaId, task.Index);
                if (onDone != null) onDone();
                return;
            }

            Ensure();
            _instance.Begin(areaId, task, stateAfter, room, onDone);
        }

        private static void Ensure()
        {
            if (_instance != null) return;

            var go = new GameObject("~StoryRepairCutscene");
            StoryObjects.Persist(go);
            _instance = go.AddComponent<RepairCutscene>();
            _instance.Build();
        }

        #region Build

        private void Build()
        {
            _canvas = StoryUi.CreateOverlayCanvas("RepairCutsceneCanvas", 780);
            _canvas.transform.SetParent(transform, false);

            RectTransform root = StoryUi.Stretch(StoryUi.CreateRect("Root", _canvas.transform));
            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            // Tap anywhere to skip. No failure state, no waiting on the 807th
            // repair. It is also the backdrop: opaque black on its own screen,
            // cleared to nothing when this plays over a room, and blocking taps
            // either way so the screen underneath cannot be pressed mid-repair.
            _backdrop = StoryUi.CreateFullScreenButton("Skip", root, Color.black, Skip)
                .targetGraphic as Image;

            // The room. Two copies stacked: the state before this repair, and the
            // state after it, revealed by growing the top one's fill.
            _stage = StoryUi.CreateRect("Stage", root);
            StageOnItsOwnScreen();

            // Three layers, and the order is the whole trick:
            //
            //   Before  the room as it was
            //   Damage  its neglect, as a wash — lifts while the work happens
            //   After   the repaired room, wiped across on top
            //
            // Putting the wash above the clean layer would dirty the thing the
            // repair just cleaned. Below it, an area with only ONE picture still
            // reads as before-and-after: the same room, grimed and then not.
            _before = StoryUi.CreateImage("Before", _stage, null, Color.white);
            StoryUi.Stretch(_before.rectTransform);
            _before.preserveAspect = true;

            _damage = StoryUi.CreateImage("Damage", _stage, null, new Color(0.05f, 0.07f, 0.05f, 0.55f));
            StoryUi.Stretch(_damage.rectTransform);

            _after = StoryUi.CreateImage("After", _stage, null, Color.white);
            StoryUi.Stretch(_after.rectTransform);
            _after.preserveAspect = true;
            _after.type = Image.Type.Filled;
            _after.fillMethod = Image.FillMethod.Horizontal;
            _after.fillAmount = 0f;

            _vfx = RepairVfx.Attach(_stage);

            _flash = StoryUi.CreateImage("Flash", root, null, new Color(1f, 0.96f, 0.86f, 0f));
            StoryUi.Stretch(_flash.rectTransform);

            _title = StoryUi.CreateText("Title", root, "", 48, Color.white, TextAnchor.UpperCenter);
            StoryUi.TopAnchored(_title.rectTransform, 90f, 130f, 70f);
            _title.fontStyle = FontStyle.Bold;

            _line = StoryUi.CreateText("Line", root, "", 40, new Color(1f, 0.93f, 0.78f),
                TextAnchor.UpperCenter);
            _line.rectTransform.anchorMin = new Vector2(0f, 0.08f);
            _line.rectTransform.anchorMax = new Vector2(1f, 0.26f);
            _line.rectTransform.offsetMin = new Vector2(70f, 0f);
            _line.rectTransform.offsetMax = new Vector2(-70f, 0f);
            _line.fontStyle = FontStyle.Italic;

            _star = StoryUi.CreateImage("Star", root, StoryUi.StarSprite, StoryUi.Gold);
            _star.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _star.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _star.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _star.rectTransform.sizeDelta = new Vector2(90f, 90f);
            _star.gameObject.SetActive(false);

            _skipHint = StoryUi.CreateText("SkipHint", root, "tap to skip", 26,
                new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight);
            StoryUi.BottomAnchored(_skipHint.rectTransform, 24f, 44f, 50f);

            gameObject.SetActive(false);
        }

        #endregion

        #region Playback

        private void Begin(int areaId, StoryTask task, int stateAfter, RectTransform room,
            Action onDone)
        {
            _onDone = onDone;
            _playing = true;
            _skipped = false;
            _revealed = false;
            _room = room;

            RepairWorkType work = RepairWork.Classify(task.Label);

            // The room before and after. Where an area has no per-state art, both
            // are its arc backdrop and the damage wash carries the change on its
            // own — which is why every one of the 150 areas has something to show.
            Sprite before = ResolveState(areaId, stateAfter - 1);
            Sprite after = ResolveState(areaId, stateAfter);

            // Where an area has one restored picture rather than a picture per
            // repair, both resolve to the same image — and a wipe between two
            // identical images shows nothing. The neglect wash is what carries
            // the change there, so it wipes in cleanly over the grimed copy.
            _before.sprite = before;
            _before.color = before != null ? Color.white : new Color(0.18f, 0.20f, 0.18f);
            _after.sprite = after;
            _after.color = after != null ? Color.white : new Color(0.22f, 0.25f, 0.22f);
            _after.fillAmount = 0f;

            // Over a room, the stage carries no picture of its own: the room on
            // screen IS the "before", and the live view's own reveal is the
            // "after". A second copy laid on top of it would only be a chance for
            // the two to disagree by a pixel, and the black behind it is exactly
            // the separate screen this is not supposed to be.
            bool overRoom = _room != null;
            _overRoom = overRoom;

            _workStart = overRoom ? RoomWorkStart : WorkStart;
            _workEnd = overRoom ? RoomWorkEnd : WorkEnd;
            _revealAt = overRoom ? RoomRevealAt : RevealAt;
            _starAt = overRoom ? RoomStarAt : StarAt;
            _endAt = overRoom ? RoomEndAt : EndAt;
            _fadeTime = overRoom ? RoomFadeTime : FadeTime;
            _before.enabled = !overRoom;
            _damage.enabled = !overRoom;
            _after.enabled = !overRoom;
            if (_backdrop != null)
                _backdrop.color = overRoom ? new Color(0f, 0f, 0f, 0f) : Color.black;

            if (overRoom) StageOverRoom(_room);
            else StageOnItsOwnScreen();

            // Royal Match writes nothing over a repair. The reference capture of
            // one mid-play has the room, the sparkles and the buttons, and not a
            // word anywhere — the player watches it, they do not read it. The
            // task's name was already read on the row they pressed.
            _title.text = _overRoom ? string.Empty : task.Label;
            _title.color = new Color(1f, 1f, 1f, 0f);
            _line.text = string.Empty;
            _skipHint.enabled = !_overRoom;
            _star.gameObject.SetActive(false);
            _flash.color = new Color(1f, 0.96f, 0.86f, 0f);

            gameObject.SetActive(true);
            _group.alpha = 0f;
            _group.blocksRaycasts = true;

            StorySfx.Play(RepairWork.SoundKey(work));

            StopAllCoroutines();
            StartCoroutine(Run(areaId, task, work, stateAfter));
        }

        private IEnumerator Run(int areaId, StoryTask task, RepairWorkType work, int stateAfter)
        {
            float started = StoryClock.Now;
            float damageBefore = DamageFor(areaId, stateAfter - 1);
            float damageAfter = DamageFor(areaId, stateAfter);

            bool workStarted = false;
            bool flashed = false;
            bool lineShown = false;
            bool starShown = false;

            while (!_skipped)
            {
                float t = StoryClock.Now - started;

                // Fade the whole thing in, then hold.
                _group.alpha = Mathf.Clamp01(t / _fadeTime);

                // A slow push in on the own-screen path. Small — 4% — because a
                // big zoom on a still image reads as a mistake rather than a
                // camera move.
                //
                // Over a room there is no push at all. The room is the screen the
                // player is standing on, not a shot; moving it is the cut this
                // whole change exists to remove, and at a second and a half the
                // move would read as a jolt rather than a camera.
                if (!_overRoom)
                {
                    float push = 1f + 0.04f * Mathf.Clamp01(t / _endAt);
                    _stage.localScale = new Vector3(push, push, 1f);
                }

                if (!_overRoom && t >= TitleIn)
                {
                    float fade = Mathf.Clamp01((t - TitleIn) / 0.35f);
                    _title.color = new Color(1f, 1f, 1f, fade);
                }

                if (!workStarted && t >= _workStart)
                {
                    workStarted = true;
                    Rect stage = _stage.rect;

                    if (_overRoom)
                    {
                        // Gold sparkles on the thing being repaired. Which part
                        // of the room that is comes off a pixel diff of the
                        // room's own artwork, taken offline; where nobody has
                        // measured it, the region is the whole room and the
                        // sparkles fall over all of it rather than guess.
                        _vfx.PlayRoyal(StoryRepairRegions.For(areaId, stateAfter),
                            stage.width, stage.height);
                    }
                    else
                    {
                        _vfx.Play(work, stage.width, stage.height);
                        _line.text = RepairWork.WorkingLine(work);
                        _line.color = new Color(1f, 0.93f, 0.78f, 0.75f);
                    }
                }

                // The neglect lifts while the work happens.
                if (t >= _workStart)
                {
                    float p = Mathf.Clamp01((t - _workStart) / (_workEnd - _workStart));
                    float damage = Mathf.Lerp(damageBefore, damageAfter, p);
                    _damage.color = new Color(0.05f, 0.07f, 0.05f, damage);
                }

                if (t >= _workEnd && workStarted) _vfx.Stop();

                if (!flashed && t >= _revealAt)
                {
                    flashed = true;
                    StorySfx.PlayRepair(false);

                    // The room changes here, on the beat, whether the change is
                    // this cutscene's own wipe or the live view's reveal under it.
                    Reveal(areaId, task.Index);
                }

                if (flashed)
                {
                    float since = t - _revealAt;

                    // A bloom as it lands. Half as bright and half as long over a
                    // room, because there it is a flash across a screen the
                    // player is standing on rather than a cut in a film: at 0.55
                    // the whole home screen blinks white, which reads as a bug.
                    float peak = _overRoom ? 0.24f : 0.55f;
                    float span = _overRoom ? 0.22f : 0.45f;
                    _flash.color = new Color(1f, 0.96f, 0.86f,
                        Mathf.Clamp01(1f - since / span) * peak);

                    // Then the repaired state wipes across — on the OWN-SCREEN
                    // path only. Over a room the room's own view crossfades the
                    // new layer in under the sparkles, which is what Royal Match
                    // does and what a wipe across a picture the player is
                    // standing in front of would fight.
                    if (!_overRoom)
                        _after.fillAmount =
                            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(since / 0.55f));
                }

                if (!lineShown && !_overRoom && t >= LineAt)
                {
                    lineShown = true;

                    // The story's own words where it wrote them; a plain line for
                    // the work where it did not.
                    _line.text = !string.IsNullOrEmpty(task.OnComplete)
                        ? task.OnComplete
                        : RepairWork.CompletedLine(work);
                    _line.color = new Color(1f, 0.93f, 0.78f, 1f);
                }

                if (!starShown && t >= _starAt)
                {
                    starShown = true;
                    _star.gameObject.SetActive(true);
                }

                if (starShown)
                {
                    // The star rises and fades — the thing the player spent.
                    float since = Mathf.Clamp01((t - _starAt) / (_endAt - _starAt));
                    _star.rectTransform.anchoredPosition =
                        new Vector2(0f, 120f + 160f * since);
                    _star.color = new Color(StoryUi.Gold.r, StoryUi.Gold.g, StoryUi.Gold.b,
                        1f - since);
                    _star.rectTransform.localScale = Vector3.one * (1f + 0.4f * since);
                }

                if (t >= _endAt) break;
                yield return null;
            }

            // A skip taken before the reveal beat still bought the repair, so the
            // room still has to change.
            Reveal(areaId, task.Index);

            yield return Finish();
        }

        /// <summary>
        /// Lets every live view of the room in on the repair, once. The reveal is
        /// the cutscene's to fire rather than the purchase's: the room the player
        /// is watching must not change while the "before" is still on screen.
        /// </summary>
        private void Reveal(int areaId, int taskIndex)
        {
            if (_revealed) return;
            _revealed = true;
            AreaSceneView.NotifyTaskCompleted(areaId, taskIndex);
        }

        private IEnumerator Finish()
        {
            _vfx.Stop();
            if (_skipped) _vfx.Clear();

            float started = StoryClock.Now;
            while (StoryClock.Now - started < _fadeTime)
            {
                _group.alpha = 1f - (StoryClock.Now - started) / _fadeTime;
                yield return null;
            }

            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _playing = false;
            _stage.localScale = Vector3.one;

            // The room is not this object's to leave pushed in: it belongs to the
            // screen underneath, which is about to draw its panel over it again.
            if (_room != null)
            {
                _room.localScale = Vector3.one;
                _room = null;
            }

            gameObject.SetActive(false);

            Action done = _onDone;
            _onDone = null;
            if (done != null) done();
        }

        private void Skip()
        {
            if (!_playing) return;
            _skipped = true;
        }

        #endregion

        #region Staging

        /// <summary>The stage as it sits when this plays on a black screen of its own.</summary>
        private void StageOnItsOwnScreen()
        {
            _stage.anchorMin = new Vector2(0f, StageBottomFrac);
            _stage.anchorMax = new Vector2(1f, StageTopFrac);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.offsetMin = new Vector2(StageSidePad, 0f);
            _stage.offsetMax = new Vector2(-StageSidePad, 0f);
        }

        /// <summary>
        /// Lays the stage exactly over a rect that belongs to another canvas, so
        /// the effects land on the room rather than near it. Measured through
        /// world corners rather than copied anchors: the room's parent is not this
        /// one, and the two canvases scale independently.
        /// </summary>
        private void StageOverRoom(RectTransform room)
        {
            var parent = _stage.parent as RectTransform;
            if (parent == null) return;

            var corners = new Vector3[4];
            room.GetWorldCorners(corners);

            Camera camera = null;
            Canvas canvas = room.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            Vector2 min, max;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                    RectTransformUtility.WorldToScreenPoint(camera, corners[0]), null, out min)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                    RectTransformUtility.WorldToScreenPoint(camera, corners[2]), null, out max))
            {
                StageOnItsOwnScreen();
                return;
            }

            _stage.anchorMin = new Vector2(0.5f, 0.5f);
            _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.pivot = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            _stage.anchoredPosition = (min + max) * 0.5f;
        }

        #endregion

        #region Art

        /// <summary>
        /// The room at a given number of completed repairs, falling back to the
        /// arc backdrop. Never null for an authored area, so the cutscene always
        /// has something to show.
        /// </summary>
        private static Sprite ResolveState(int areaId, int state)
        {
            if (state < 0) state = 0;

            Sprite exact = StoryArt.AreaState(areaId, state);
            if (exact != null) return exact;

            // Walk down to the last state that does exist — an area with three
            // pictures and eight repairs should hold on the third, not go blank.
            for (int s = state - 1; s >= 0; s--)
            {
                Sprite earlier = StoryArt.AreaState(areaId, s);
                if (earlier != null) return earlier;
            }

            // Most areas ship one restored picture instead of a set.
            return StoryArt.AreaRestored(areaId);
        }

        /// <summary>
        /// How ruined the room looks at a given repair count. Full neglect at the
        /// start, none when the area is restored. This is what makes an area with
        /// a single backdrop still visibly improve.
        /// </summary>
        private static float DamageFor(int areaId, int state)
        {
            StoryArea area = StoryDatabase.GetArea(areaId);
            if (area == null || area.Tasks.Count == 0) return 0f;

            float progress = Mathf.Clamp01((float)state / area.Tasks.Count);
            return Mathf.Lerp(0.55f, 0f, progress);
        }

        #endregion
    }
}
