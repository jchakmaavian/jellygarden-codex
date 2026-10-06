// unity command --project-path <project> eval_file --file Design/Cards/LevelCompleted-v5/verify_reference_experience.cs
// Actual Win/Continue/Home flow in an OWNED disposable QA profile. Normal saves are snapshotted and never edited.
// The final capture uses forced QA goal completion, then real PreWinAnimations/finale/Win/Continue/Home code paths.
#pragma warning disable 0162
if (!UnityEngine.Application.isPlaying) throw new global::System.InvalidOperationException("Enter Play Mode on Home first.");
if (RoyalThrone.Testing.RoyalSave.Testing) throw new global::System.InvalidOperationException("An existing QA session is active; refusing to adopt it.");
var qa = RoyalThrone.Testing.RoyalQaService.Instance;
var lm = JellyGarden.Scripts.LevelManager.THIS;
var menus = JellyGarden.Scripts.GUI.MenuManager.Instance;
var qaHome = UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
if (!qa || !qa.Ready || qa.Busy || !lm || !menus || !qaHome || !qaHome.starLabel || !qaHome.coinLabel || lm.gameStatus != JellyGarden.Scripts.GameState.Map)
    throw new global::System.InvalidOperationException("Return to an idle Home before running the owned flow check.");
var originalHomeNumbers = qaHome.levelNumbers;
var originalHomeNumberValues = originalHomeNumbers == null ? null : (int[])originalHomeNumbers.Clone();
void RestoreHomeCatalog() { if (qaHome) qaHome.levelNumbers = originalHomeNumbers; }
bool HomeCatalogRestored()
{
    if (!qaHome || !global::System.Object.ReferenceEquals(qaHome.levelNumbers, originalHomeNumbers)) return false;
    if (originalHomeNumberValues == null) return qaHome.levelNumbers == null;
    if (qaHome.levelNumbers.Length != originalHomeNumberValues.Length) return false;
    for (int i = 0; i < originalHomeNumberValues.Length; i++) if (qaHome.levelNumbers[i] != originalHomeNumberValues[i]) return false;
    return true;
}
var dir = global::System.IO.Path.GetFullPath("Design/Cards/LevelCompleted-v5/Verification");
global::System.IO.Directory.CreateDirectory(dir);
var log = new global::System.Text.StringBuilder();
var timeline = new global::System.Text.StringBuilder("case\tstage\telapsed_s\tstate\tstar_flights\tcoin_flights\tvisible_stars\tvisible_coins\tshown_coins\tpending\tsaved_coins\tstar_hud\tcoin_hud\tnext_level\n");
var errors = new global::System.Collections.Generic.List<string>();
int passes = 0, fails = 0;
bool ownsQa = false, cleaned = false, listening = false, captureRunning = false, captureStop = false;
UnityEngine.Coroutine captureRoutine = null;
float oldCaptureDelta = UnityEngine.Time.captureDeltaTime;
bool oldBackground = UnityEngine.Application.runInBackground;
int oldTargetRate = UnityEngine.Application.targetFrameRate;
string F(float value) => value.ToString("F4", global::System.Globalization.CultureInfo.InvariantCulture);
void Note(string value) { log.AppendLine(value); global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "reference-experience-checks.txt"), log.ToString()); }
void Check(bool ok, string value) { if (ok) passes++; else fails++; Note((ok ? "PASS " : "FAIL ") + value); }
string Snapshot(bool routed)
{
    var result = new global::System.Text.StringBuilder();
    string[] ints = { "OpenLevel", "OpenLevelTest", "Lifes", "Gems", "Colorful_bomb", "Packages", "Stripes", "Cannon", "Hammer", "Hand", "Arrow", "Music", "Sound", "RoyalThrone.HomeMotion", "GetGems", "GetLifes", "GetGoOn", "RoyalThrone.Area.Current", "RoyalThrone.Area.Done", "RoyalThrone.Area.Spent", "RoyalThrone.Area.BoughtStars", "RoyalThrone.Area.Entered", "RoyalThrone.Area.LayoutVersion", "RoyalThrone.Area.MigrationCredit", "RoyalThrone.Area.Guided", "RoyalThrone.Profile.FirstTryWins", "RoyalThrone.Profile.PowerupsMade" };
    foreach (string key in ints) result.Append(key).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(key, -99999) : UnityEngine.PlayerPrefs.GetInt(key, -99999)).Append(';');
    for (int level = 1; level <= 50; level++)
        foreach (string key in new[] { "Level." + level.ToString("000") + ".StarsCount", "Score" + level, "RoyalThrone.Profile.Tried." + level.ToString("000") })
            result.Append(key).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetInt(key, -99999) : UnityEngine.PlayerPrefs.GetInt(key, -99999)).Append(';');
    foreach (string key in new[] { "DateOfExit", "RoyalThrone.NextLifeUtc", "RoyalThrone.Profile.Name", "RoyalThrone.Profile.Avatar", "RoyalThrone.Profile.Since" })
        result.Append(key).Append('=').Append(routed ? RoyalThrone.Testing.RoyalSave.GetString(key, "<missing>") : UnityEngine.PlayerPrefs.GetString(key, "<missing>")).Append(';');
    result.Append("RestLifeTimer=").Append(F(routed ? RoyalThrone.Testing.RoyalSave.GetFloat("RestLifeTimer", -99999) : UnityEngine.PlayerPrefs.GetFloat("RestLifeTimer", -99999)));
    return result.ToString();
}
string Economy(int level)
{
    var result = new global::System.Text.StringBuilder("runtime=" + JellyGarden.Scripts.InitScript.Gems + ";lives=" + JellyGarden.Scripts.InitScript.lifes);
    foreach (string key in new[] { "Gems", "Lifes", "Level." + level.ToString("000") + ".StarsCount", "Score" + level, "RoyalThrone.Area.Current", "RoyalThrone.Area.Done", "RoyalThrone.Area.Spent", "RoyalThrone.Area.BoughtStars", "RoyalThrone.Area.Entered", "RoyalThrone.Area.LayoutVersion", "RoyalThrone.Area.MigrationCredit", "RoyalThrone.Area.Guided", "RoyalThrone.Profile.FirstTryWins" })
        result.Append(';').Append(key).Append('=').Append(RoyalThrone.Testing.RoyalSave.GetInt(key, -99999));
    return result.ToString();
}
int HudValue(UnityEngine.UI.Text label)
{
    return label && int.TryParse(label.text, global::System.Globalization.NumberStyles.Integer | global::System.Globalization.NumberStyles.AllowThousands,
        global::System.Globalization.CultureInfo.CurrentCulture, out int value) ? value : -99999;
}
void SeedProgress(int level, bool alreadyWon, string label)
{
    if (!ownsQa || !RoyalThrone.Testing.RoyalSave.Testing || !RoyalThrone.Campaign.RoyalReferenceCampaign.Enabled)
        throw new global::System.InvalidOperationException("Progress fixture may only write the owned reference QA profile.");
    var authored = RoyalThrone.Campaign.RoyalReferenceCampaign.Levels;
    var numbers = new int[authored.Length];
    int expectedEarned = 0;
    for (int i = 0; i < authored.Length; i++)
    {
        int number = authored[i].number; numbers[i] = number;
        int won = number < level || (number == level && alreadyWon) ? 1 : 0;
        RoyalThrone.Testing.RoyalSave.SetInt("Level." + number.ToString("000") + ".StarsCount", won);
        expectedEarned += won;
    }
    global::System.Array.Sort(numbers); qaHome.levelNumbers = numbers;
    // Explicitly seed every area key: otherwise RoyalSave can read reused QA values or fall back to normal keys.
    // The synthetic profile has paid all catalog tasks; QA-only bought stars reconcile that cost with four left.
    int restored = RoyalThrone.Presentation.RoyalAreas.All.Length + 1, spent = 0;
    foreach (var area in RoyalThrone.Presentation.RoyalAreas.All) spent += area.TotalCost;
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.CurrentKey, restored);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.DoneKey, 0);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.EnteredKey, restored);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.LayoutVersionKey, 1);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.MigrationCreditKey, 0);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreaView.GuidedKey, 1);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.SpentKey, spent);
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.BoughtKey, 0);
    qaHome.RefreshProgress();
    int earned = qaHome.Progress.stars;
    if (earned != expectedEarned || global::System.Array.IndexOf(numbers, level + 1) < 0 || spent + 4 < earned)
        throw new global::System.InvalidOperationException("Authored Home progress does not match the deterministic QA fixture.");
    int bought = spent + 4 - earned;
    RoyalThrone.Testing.RoyalSave.SetInt(RoyalThrone.Presentation.RoyalAreas.BoughtKey, bought);
    RoyalThrone.Testing.RoyalSave.SetInt("OpenLevel", level);
    RoyalThrone.Testing.RoyalSave.SetInt("OpenLevelTest", 0);
    RoyalThrone.Testing.RoyalSave.Save(); qaHome.RefreshProgress();
    Check(qaHome.Progress.available == 4 && HudValue(qaHome.starLabel) == 4 && qaHome.Progress.nextLevel == level + (alreadyWon ? 1 : 0),
        label + " deterministic authored progress starts with four displayed spendable stars and the correct next level");
    Check(RoyalThrone.Presentation.RoyalAreas.AllRestored && !RoyalThrone.Presentation.RoyalAreas.AwaitingEntry && !RoyalThrone.Presentation.RoyalAreaView.NeedsGuide,
        label + " QA area fixture is fully restored with no task or tutorial due");
    Note("FIXTURE " + label + " authored-levels=" + numbers.Length + " earned=" + earned + " bought=" + bought + " spent=" + spent + " available=4; synthetic owned QA setup only, no shop or player award.");
}
string realBefore = Snapshot(false), runtimeBefore = JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel;
UnityEngine.Application.LogCallback onLog = (message, stack, type) => { if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception || type == UnityEngine.LogType.Assert) errors.Add(type + ": " + message); };
global::System.Collections.IEnumerator Wait(float seconds) { float end = UnityEngine.Time.unscaledTime + seconds; while (UnityEngine.Time.unscaledTime < end) yield return null; }
UnityEngine.Rect ScreenRect(UnityEngine.Transform target)
{
    var rect = target as UnityEngine.RectTransform; if (!rect) return new UnityEngine.Rect();
    var corners = new UnityEngine.Vector3[4]; rect.GetWorldCorners(corners);
    var canvas = rect.GetComponentInParent<UnityEngine.Canvas>(); canvas = canvas ? canvas.rootCanvas : null;
    UnityEngine.Camera camera = canvas && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera ? canvas.worldCamera : UnityEngine.Camera.main) : null;
    var lo = new UnityEngine.Vector2(float.PositiveInfinity, float.PositiveInfinity); var hi = -lo;
    foreach (var corner in corners) { var point = UnityEngine.RectTransformUtility.WorldToScreenPoint(camera, corner); lo = UnityEngine.Vector2.Min(lo, point); hi = UnityEngine.Vector2.Max(hi, point); }
    return UnityEngine.Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
}
float VisibleAlpha(UnityEngine.UI.Graphic image)
{
    if (!image || !image.enabled || !image.gameObject.activeInHierarchy) return 0;
    float alpha = image.color.a;
    for (var at = image.transform; at; at = at.parent) foreach (var group in at.GetComponents<UnityEngine.CanvasGroup>()) alpha *= group.alpha;
    return alpha;
}
int VisibleFlights(bool coins, out int escaped)
{
    int count = 0; escaped = 0;
    foreach (var image in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>())
    {
        if (image.name != "Flying reward" || VisibleAlpha(image) <= .01f || !image.sprite) continue;
        bool isCoin = image.sprite.name.StartsWith("Items-CoinItem-coin", global::System.StringComparison.Ordinal);
        if (isCoin != coins) continue; count++;
        var bounds = ScreenRect(image.transform);
        if (bounds.xMin < -1 || bounds.yMin < -1 || bounds.xMax > UnityEngine.Screen.width + 1 || bounds.yMax > UnityEngine.Screen.height + 1) escaped++;
    }
    return count;
}
void Sample(string label, string stage, float elapsed)
{
    int ignored; int visibleStars = VisibleFlights(false, out ignored), visibleCoins = VisibleFlights(true, out ignored);
    timeline.Append(label).Append('\t').Append(stage).Append('\t').Append(F(elapsed)).Append('\t').Append(lm.gameStatus).Append('\t')
        .Append(JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardStarsInFlight).Append('\t').Append(JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardCoinsInFlight).Append('\t')
        .Append(visibleStars).Append('\t').Append(visibleCoins).Append('\t').Append(JellyGarden.Scripts.Juice.RoyalWinSequence.HomeCoinsShown).Append('\t')
        .Append(JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardsPending).Append('\t').Append(RoyalThrone.Testing.RoyalSave.GetInt("Gems")).Append('\t')
        .Append(HudValue(qaHome.starLabel)).Append('\t').Append(HudValue(qaHome.coinLabel)).Append('\t').Append(qaHome.Progress.nextLevel).Append('\n');
}
var flightFlags = global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic;
var flightListField = typeof(JellyGarden.Scripts.Juice.RoyalWinSequence).GetField("flights", flightFlags);
var flightType = typeof(JellyGarden.Scripts.Juice.RoyalWinSequence).GetNestedType("Flight", global::System.Reflection.BindingFlags.NonPublic);
var flightTurningField = flightType?.GetField("turning", flightFlags);
var flightLandedField = flightType?.GetField("landed", flightFlags);
if (flightListField == null || flightTurningField == null || flightLandedField == null)
    throw new global::System.InvalidOperationException("Reward flight inspection fields changed; update this read-only QA observer before running.");
void ObserveCoinFlights(global::System.Collections.Generic.HashSet<object> observed)
{
    var sequence = lm.GetComponent<JellyGarden.Scripts.Juice.RoyalWinSequence>(); if (!sequence) return;
    var flights = flightListField.GetValue(sequence) as global::System.Collections.IEnumerable;
    if (flights == null) return;
    // Flight objects are allocated per launch. Only their Images are pooled; retirement preserves landed.
    // Retaining each object proves completion even when a slow frame merges several visible counter updates.
    foreach (object flight in flights) if ((bool)flightTurningField.GetValue(flight)) observed.Add(flight);
}
global::System.Collections.IEnumerator Tap(UnityEngine.Transform target, string label)
{
    if (!target) throw new global::System.Exception("Tap target missing: " + label);
    var button = target.GetComponent<UnityEngine.UI.Button>();
    float until = UnityEngine.Time.unscaledTime + 3;
    while (button && !button.IsInteractable() && UnityEngine.Time.unscaledTime < until) yield return null;
    Check(button && button.IsInteractable(), label + " is interactable at the requested time");
    var events = UnityEngine.EventSystems.EventSystem.current;
    var pointer = new UnityEngine.EventSystems.PointerEventData(events) { position = ScreenRect(target).center, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
    var hits = new global::System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); events.RaycastAll(pointer, hits);
    var hit = hits.Count == 0 ? null : UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
    Check(hit == target.gameObject, label + " owns its actual UI raycast");
    if (hit != target.gameObject) yield break;
    UnityEngine.EventSystems.ExecuteEvents.Execute(hit, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
    yield return Wait(.03f);
    UnityEngine.EventSystems.ExecuteEvents.Execute(hit, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
    UnityEngine.EventSystems.ExecuteEvents.Execute(hit, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
global::System.Collections.IEnumerator Shot(string name)
{
    yield return new UnityEngine.WaitForEndOfFrame(); var texture = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
    try { global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(dir, name + ".png"), UnityEngine.ImageConversion.EncodeToPNG(texture)); }
    finally { UnityEngine.Object.Destroy(texture); }
}
global::System.Collections.IEnumerator CaptureFullFlow()
{
    captureRunning = true; captureStop = false;
    string folderName = "full-flow-" + global::System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
    string folder = global::System.IO.Path.Combine(dir, folderName); global::System.IO.Directory.CreateDirectory(folder);
    var timestamps = new global::System.Collections.Generic.List<float>();
    var frameNotes = new global::System.Text.StringBuilder("frame\telapsed_s\tgame_state\n");
    int width = 720, height = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(720f * UnityEngine.Screen.height / UnityEngine.Screen.width));
    var target = UnityEngine.RenderTexture.GetTemporary(width, height, 0, UnityEngine.RenderTextureFormat.ARGB32);
    var pixels = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
    float start = UnityEngine.Time.realtimeSinceStartup, next = start;
    try
    {
        while (!captureStop && UnityEngine.Time.realtimeSinceStartup - start < 45)
        {
            yield return new UnityEngine.WaitForEndOfFrame(); float now = UnityEngine.Time.realtimeSinceStartup;
            if (now < next) continue;
            var screenshot = UnityEngine.ScreenCapture.CaptureScreenshotAsTexture(); var previous = UnityEngine.RenderTexture.active;
            try
            {
                UnityEngine.Graphics.Blit(screenshot, target); UnityEngine.RenderTexture.active = target;
                pixels.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0); pixels.Apply(false, false);
                global::System.IO.File.WriteAllBytes(global::System.IO.Path.Combine(folder, timestamps.Count.ToString("D5") + ".jpg"), UnityEngine.ImageConversion.EncodeToJPG(pixels, 90));
            }
            finally { UnityEngine.RenderTexture.active = previous; UnityEngine.Object.Destroy(screenshot); }
            frameNotes.Append(timestamps.Count).Append('\t').Append(F(now - start)).Append('\t').Append(lm.gameStatus).Append('\n');
            timestamps.Add(now - start); next = UnityEngine.Time.realtimeSinceStartup + 1f / 30f;
        }
    }
    finally
    {
        UnityEngine.RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels);
        var concat = new global::System.Text.StringBuilder("ffconcat version 1.0\n");
        for (int i = 0; i < timestamps.Count; i++) { concat.Append("file '").Append(folderName).Append('/').Append(i.ToString("D5")).Append(".jpg'\n"); concat.Append("duration ").Append(F(i + 1 < timestamps.Count ? timestamps[i + 1] - timestamps[i] : 1f / 30f)).Append('\n'); }
        if (timestamps.Count > 0) concat.Append("file '").Append(folderName).Append('/').Append((timestamps.Count - 1).ToString("D5")).Append(".jpg'\n");
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "full-flow-capture.ffconcat"), concat.ToString());
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "full-flow-capture-timestamps.tsv"), frameNotes.ToString());
        Note("CAPTURE " + folderName + ": " + timestamps.Count + " actual-time JPEG frames, " + width + "x" + height + ". Silent recording; forced QA goals, real pre-win/finale/card/Continue/Home flow.");
        captureRunning = false;
    }
}
global::System.Collections.IEnumerator Scenario(int level, string control, bool early, bool calm, bool alreadyWon, bool realPreWin)
{
    string label = "L" + level + "-" + control + (early ? "-early" : "") + (calm ? "-motion-off" : "") + (alreadyWon ? "-replay" : "") + (realPreWin ? "-full-prewin-capture" : "");
    Note("CASE " + label);
    var home = qaHome; home.ClosePanel();
    RoyalThrone.Testing.RoyalSave.SetInt("RoyalThrone.HomeMotion", calm ? 0 : 1);
    SeedProgress(level, alreadyWon, label);
    const int starsBefore = 4;
    int expectedStars = starsBefore + (alreadyWon ? 0 : 1);
    qa.Level = level; yield return qa.OpenLevel(); qa.Visible = false; qa.Counters = false;
    Check(lm.currentLevel == level && lm.gameStatus == JellyGarden.Scripts.GameState.Playing, label + " opens an authored later level in the isolated profile");
    yield return qa.Execute("Refill lives");
    Check(JellyGarden.Scripts.InitScript.lifes == JellyGarden.Scripts.InitScript.Instance.CapOfLife && RoyalThrone.Testing.RoyalSave.GetInt("Lifes") == JellyGarden.Scripts.InitScript.Instance.CapOfLife,
        label + " starts at the configured life cap with the QA refill timer cleared");
    JellyGarden.Scripts.InitScript.Instance.SetGems(820);
    int before = JellyGarden.Scripts.InitScript.Gems, savedBefore = RoyalThrone.Testing.RoyalSave.GetInt("Gems");
    lm.Limit = 1;
    if (realPreWin)
    {
        captureRoutine = qa.StartCoroutine(CaptureFullFlow()); yield return Wait(.25f);
        foreach (var goal in lm.targetObject) goal.countCollected = goal.targetCount;
        JellyGarden.Scripts.LevelManager.Score = UnityEngine.Mathf.Max(1, lm.star1); lm.CheckWinLose();
        Check(lm.gameStatus == JellyGarden.Scripts.GameState.PreWinAnimations, label + " forced QA goals enter the real pre-win sequence");
        float timeout = UnityEngine.Time.unscaledTime + 35;
        while (lm.gameStatus != JellyGarden.Scripts.GameState.Win && UnityEngine.Time.unscaledTime < timeout) { Sample(label, "prewin", 35 - (timeout - UnityEngine.Time.unscaledTime)); yield return null; }
        Check(lm.gameStatus == JellyGarden.Scripts.GameState.Win, label + " celebration and remaining-move finale reach Win within 35s");
        Check(JellyGarden.Scripts.Juice.RoyalWinSequence.FinaleBurstCount >= 1 && JellyGarden.Scripts.Juice.RoyalWinSequence.FinalePendingCoinValue == 0, label + " accepted finale booster emits its coins and drains assigned value");
    }
    else yield return qa.Execute("Win instantly");
    int reward = lm.CommittedCoins, expected = before + reward;
    Check(reward == 55 && JellyGarden.Scripts.InitScript.Gems == expected && RoyalThrone.Testing.RoyalSave.GetInt("Gems") == savedBefore + reward, label + " one remaining move commits exactly 55 coins before presentation");
    Check(lm.StarsGained == (alreadyWon ? 0 : 1) && RoyalThrone.Testing.RoyalSave.GetInt("Level." + level.ToString("000") + ".StarsCount") == 1, label + " first-win/replay star award is committed exactly once");
    home.RefreshProgress();
    Check(home.Progress.available == expectedStars && home.Progress.nextLevel == level + 1, label + " saved progress now has the exact star balance and next authored level");
    string awarded = Economy(level);
    lm.gameStatus = JellyGarden.Scripts.GameState.Win;
    Check(Economy(level) == awarded, label + " duplicate Win notification does not credit a second award");
    var menu = menus.GetCanvasGlobal().Find("MenuComplete"); var card = menu ? menu.Find("Image") : null;
    if (!card) throw new global::System.Exception("Completed level card not found");
    var piece = RoyalThrone.Presentation.RoyalWinCard.Of(menu);
    Check(menu.gameObject.activeInHierarchy && JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardsPending, label + " card holds queued Home rewards");
    var oldBadge = card.Find("Royal win reward");
    Check(!oldBadge || !oldBadge.gameObject.activeInHierarchy, label + " popup has no visible coin reward badge");
    Check(piece && piece.RewardCoins == reward, label + " committed amount remains available internally without a popup counter");
    float cardStart = UnityEngine.Time.unscaledTime, hold = early ? .25f : calm ? .65f : 3.0f;
    bool cardCoin = false, cardStar = false, prematureHome = false;
    while (UnityEngine.Time.unscaledTime - cardStart < hold)
    {
        int ignored; cardCoin |= VisibleFlights(true, out ignored) > 0; cardStar |= VisibleFlights(false, out ignored) > 0;
        prematureHome |= lm.gameStatus != JellyGarden.Scripts.GameState.Win;
        Sample(label, "card", UnityEngine.Time.unscaledTime - cardStart); yield return null;
    }
    Check(!cardCoin && !cardStar && !prematureHome, label + " no reward flight leaves the visible popup before Continue");
    Check(Economy(level) == awarded, label + " waiting on the card never credits a second reward");
    if (early) Check(piece && !piece.RewardsReady, label + " requested Continue before the star reward settle completes");
    if (!realPreWin && !early && !calm && !alreadyWon) yield return Shot(label + "-card");
    yield return Tap(card.Find(control), label + " " + control);
    float homeStart = UnityEngine.Time.unscaledTime, untilHome = homeStart + 4;
    while (lm.gameStatus != JellyGarden.Scripts.GameState.Map && UnityEngine.Time.unscaledTime < untilHome) yield return null;
    Check(lm.gameStatus == JellyGarden.Scripts.GameState.Map && !menu.gameObject.activeInHierarchy && !menus.menuPlay.activeInHierarchy, label + " action returns to Home without auto-opening the next level card");
    int previousShown = before, maxCoins = 0, maxStars = 0, escapes = 0;
    float starFirst = -1, coinFirst = -1, starLanded = -1, firstArrival = -1;
    bool overlap = false, starSeen = false, prematurePanel = false, nextPopup = false;
    bool heldStarHud = false, starHudMismatch = false, coinHudMismatch = false;
    var arrivals = new global::System.Collections.Generic.List<int>();
    var starHudValues = new global::System.Collections.Generic.List<int>();
    var observedCoinFlights = new global::System.Collections.Generic.HashSet<object>();
    ObserveCoinFlights(observedCoinFlights);
    float timeoutHome = UnityEngine.Time.unscaledTime + 12;
    while (JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardsPending && UnityEngine.Time.unscaledTime < timeoutHome)
    {
        yield return null;
        // Observe what was actually rendered, after Home.LateUpdate and the temporary counter-hold callback.
        yield return new UnityEngine.WaitForEndOfFrame(); float age = UnityEngine.Time.unscaledTime - homeStart;
        ObserveCoinFlights(observedCoinFlights);
        int escapedCoins, escapedStars; int visibleCoins = VisibleFlights(true, out escapedCoins), visibleStars = VisibleFlights(false, out escapedStars);
        int stars = JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardStarsInFlight, coins = JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardCoinsInFlight;
        escapes += escapedCoins + escapedStars; maxCoins = global::System.Math.Max(maxCoins, visibleCoins); maxStars = global::System.Math.Max(maxStars, visibleStars);
        if (visibleStars > 0 && starFirst < 0) { starFirst = age; starSeen = true; }
        if (visibleCoins > 0 && coinFirst < 0) coinFirst = age;
        if (visibleStars > 0 && visibleCoins > 0) overlap = true;
        if (starSeen && stars == 0 && starLanded < 0) starLanded = age;
        int shown = JellyGarden.Scripts.Juice.RoyalWinSequence.HomeCoinsShown;
        int starHud = HudValue(home.starLabel), coinHud = HudValue(home.coinLabel);
        if (starHudValues.Count == 0 || starHudValues[starHudValues.Count - 1] != starHud) starHudValues.Add(starHud);
        if (!calm && !alreadyWon && stars > 0) { heldStarHud |= starHud == starsBefore; starHudMismatch |= starHud != starsBefore; }
        else starHudMismatch |= starHud != expectedStars;
        coinHudMismatch |= coinHud != shown;
        if (shown != previousShown) { if (firstArrival < 0) firstArrival = age; arrivals.Add(shown - before); previousShown = shown; }
        if (home && home.OpenPanelName == "area" && (stars > 0 || coins > 0)) prematurePanel = true;
        nextPopup |= menus.menuPlay.activeInHierarchy;
        Sample(label, "home", age);
    }
    yield return new UnityEngine.WaitForEndOfFrame();
    int finalStarHud = HudValue(home.starLabel);
    if (starHudValues.Count == 0 || starHudValues[starHudValues.Count - 1] != finalStarHud) starHudValues.Add(finalStarHud);
    ObserveCoinFlights(observedCoinFlights);
    int landedCoinFlights = 0;
    foreach (object flight in observedCoinFlights) if ((bool)flightLandedField.GetValue(flight)) landedCoinFlights++;
    Check(!JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardsPending, label + " Home collection completes within 12s");
    Check(!prematurePanel && !nextPopup, label + " no Area task panel or next-level popup covers active collection");
    Check(escapes == 0, label + " complete star and coin sprite bounds remain on screen throughout Home collection");
    Check(!starHudMismatch && finalStarHud == expectedStars && (calm || alreadyWon || heldStarHud),
        label + (alreadyWon ? " rendered star counter stays at four on replay" : calm ? " Motion off renders five stars directly without a flight" : " rendered star counter holds four until arrival, then shows five"));
    Check(!coinHudMismatch && HudValue(home.coinLabel) == expected, label + " rendered coin label matches each sampled arrival total and ends at875");
    Check(home.Progress.available == expectedStars && home.Progress.nextLevel == level + 1 && home.levelLabel.text == "Level " + (level + 1),
        label + " final Home shows exact spendable stars and Level " + (level + 1));
    if (calm) Check(maxCoins == 0 && maxStars == 0 && observedCoinFlights.Count == 0, label + " Motion off completes the counters without star or coin flights");
    else
    {
        Check(maxCoins > 0, label + " visible coin pile is animated on Home");
        if (!alreadyWon)
        {
            Check(starFirst >= 0 && coinFirst > starFirst && overlap, label + " visible star launches first and overlaps the later coin pile");
            Check(starLanded >= 0 && firstArrival > starLanded, label + " star reaches its counter before the first coin arrival");
        }
        else Check(maxStars == 0, label + " replay does not fly or award a duplicate star");
        bool monotonic = true; int last = 0;
        foreach (int shown in arrivals) { bool allowed = false; for (int n = 1; n <= 10; n++) if (shown == JellyGarden.Scripts.Juice.RoyalWinSequence.CoinAmountAtArrival(reward, n, 10)) allowed = true; if (!allowed || shown <= last) monotonic = false; last = shown; }
        Check(monotonic && last == reward, label + " sampled counter totals are a valid increasing arrival subsequence ending at the exact reward");
        Check(observedCoinFlights.Count == 10 && landedCoinFlights == 10, label + " ten distinct scheduled coin flights reach their landed state (scheduled=" + observedCoinFlights.Count + ", landed=" + landedCoinFlights + ")");
    }
    Check(JellyGarden.Scripts.Juice.RoyalWinSequence.HomeCoinsShown == expected && JellyGarden.Scripts.Juice.RoyalWinSequence.ActiveCoinFlights == 0, label + " display settles at 875 with no leftover coin flight");
    Check(Economy(level) == awarded, label + " Home animation and callbacks do not award coins, stars or lives again");
    if (!realPreWin)
    {
        yield return Wait(.30f);
        Check(!JellyGarden.Scripts.Juice.RoyalWinSequence.HomeRewardsPending && JellyGarden.Scripts.Juice.RoyalWinSequence.ActiveCoinFlights == 0 && !menus.menuPlay.activeInHierarchy && Economy(level) == awarded,
            label + " no delayed duplicate presentation, reward or next-level popup appears");
    }
    Note("TIMING " + label + " star=" + F(starFirst) + " coins=" + F(coinFirst) + " star-arrived=" + F(starLanded) + " first-arrival=" + F(firstArrival) + " observed-counter-updates=" + arrivals.Count);
    Note("OBSERVATIONS " + label + " distinct-coin-flights=" + observedCoinFlights.Count + " landed-coin-flights=" + landedCoinFlights + " sampled-reward-totals=[" + string.Join(",", arrivals) + "] rendered-star-totals=[" + string.Join(",", starHudValues) + "] next-level=" + home.Progress.nextLevel);
    if (realPreWin) { yield return Wait(.65f); captureStop = true; while (captureRunning) yield return null; captureRoutine = null; yield return Shot("full-flow-Home-finished"); }
    if (home) home.ClosePanel();
    Check(Snapshot(false) == realBefore, label + " normal player keys remain untouched");
}
global::System.Collections.IEnumerator Body()
{
    qa.StartSession(); ownsQa = RoyalThrone.Testing.RoyalSave.Testing;
    if (!ownsQa) throw new global::System.Exception("Owned QA save routing did not start");
    qa.Visible = false; qa.Counters = false; RoyalThrone.Campaign.RoyalReferenceCampaign.Select(true);
    UnityEngine.Application.runInBackground = true; UnityEngine.Time.captureDeltaTime = 0; UnityEngine.Application.targetFrameRate = 60;
    yield return Scenario(13, "Next", false, false, false, false);
    yield return Scenario(23, "Royal close", false, false, false, false);
    yield return Scenario(13, "Next", true, false, false, false);
    yield return Scenario(23, "Next", false, true, false, false);
    yield return Scenario(13, "Royal close", false, false, true, false);
    yield return Scenario(13, "Next", false, false, false, true);
}
global::System.Collections.IEnumerator Run()
{
    var stack = new global::System.Collections.Generic.Stack<global::System.Collections.IEnumerator>();
    UnityEngine.Application.logMessageReceived += onLog; listening = true;
    Note("Started " + global::System.DateTime.UtcNow.ToString("o") + "; isolated reference campaign levels13/23. Five actual QA instant wins and one forced-goals real pre-win capture.");
    stack.Push(Body());
    try
    {
        while (stack.Count > 0)
        {
            var step = stack.Peek(); bool moved = false; global::System.Exception error = null;
            try { moved = step.MoveNext(); } catch (global::System.Exception ex) { error = ex; }
            if (error != null) { Check(false, "Harness exception: " + error); break; }
            if (!moved) { stack.Pop(); (step as global::System.IDisposable)?.Dispose(); continue; }
            if (step.Current is global::System.Collections.IEnumerator child && !(step.Current is UnityEngine.CustomYieldInstruction)) stack.Push(child); else yield return step.Current;
        }
        while (stack.Count > 0) (stack.Pop() as global::System.IDisposable)?.Dispose();
        captureStop = true; while (captureRunning) yield return null;
        RestoreHomeCatalog();
        if (ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing) yield return qa.EndSession();
        ownsQa = false; cleaned = true; yield return Wait(.3f);
        if (qaHome && !RoyalThrone.Testing.RoyalSave.Testing) qaHome.RefreshProgress();
        Check(!RoyalThrone.Testing.RoyalSave.Testing, "Owned QA session ended");
        Check(HomeCatalogRestored(), "Original Home authored-level array reference and values restored");
        Check(Snapshot(false) == realBefore, "Every snapshotted normal save key is unchanged after restoration");
        Check(JellyGarden.Scripts.InitScript.Gems + "/" + JellyGarden.Scripts.InitScript.lifes + "/" + lm.currentLevel == runtimeBefore, "Normal runtime coins/lives/selected level restored");
        Check(errors.Count == 0, "No Unity errors or exceptions during full-flow QA (" + errors.Count + ")");
        foreach (string error in errors) Note("UNITY ERROR " + error);
        Note("SUMMARY passes=" + passes + " fails=" + fails); Note("DONE");
    }
    finally
    {
        captureStop = true; if (captureRoutine != null && qa) qa.StopCoroutine(captureRoutine);
        while (stack.Count > 0) { try { (stack.Pop() as global::System.IDisposable)?.Dispose(); } catch { } }
        UnityEngine.Application.runInBackground = oldBackground; UnityEngine.Time.captureDeltaTime = oldCaptureDelta; UnityEngine.Application.targetFrameRate = oldTargetRate;
        if (listening) UnityEngine.Application.logMessageReceived -= onLog;
        global::System.IO.File.WriteAllText(global::System.IO.Path.Combine(dir, "reference-experience-timeline.tsv"), timeline.ToString());
        RestoreHomeCatalog();
        if (!cleaned && ownsQa && qa && RoyalThrone.Testing.RoyalSave.Testing) { Note("INTERRUPTED: scheduling owned QA profile restoration"); qa.StartCoroutine(qa.EndSession()); }
    }
}
qa.StartCoroutine(Run());
return "Reference experience verification started. Poll Design/Cards/LevelCompleted-v5/Verification/reference-experience-checks.txt for DONE.";
