#!/usr/bin/env python3
"""Dry-run the kit-core merge specs (docs/port/merge-specs/*.md) into a scratch folder.

For every kit file the specs cover, this script:
  1. reads the spec's base version with `git show` (origin/royal-throne or origin/main);
  2. applies the spec's edits as exact-text replacements, failing if an anchor does
     not match the expected number of times;
  3. writes the result to OUT_DIR (default: a new temp folder; never Assets/).

Then it checks the output:
  * brace balance against the base file;
  * leftover coupling tokens (campaign, KingVanguard, RoyalWinSequence, RoyalEconomy, ...);
  * C# syntax with the .NET SDK's Roslyn `csc.dll`, when one is installed. Only
    parse errors (CS1xxx) count; unresolved Unity types are expected and ignored.

The output is a *reference* for the Phase 3 merge agent: after merging for real, diff
Assets/JellyGarden/Scripts/<file> against OUT_DIR/<file>. Read-only on the repo (git show only).

Usage:  python3 tools/port_merge_dryrun.py [OUT_DIR]
Exit status 1 on any failed anchor, unbalanced braces, leftover coupling or syntax error.
"""
import glob, os, re, subprocess, sys, pathlib, tempfile
REPO = str(pathlib.Path(__file__).resolve().parent.parent)
if len(sys.argv) > 1 and sys.argv[1] in ("-h", "--help"):
    print(__doc__)
    sys.exit(0)
if len(sys.argv) > 1 and sys.argv[1].startswith("-"):
    sys.exit("unknown option %r; usage: port_merge_dryrun.py [OUT_DIR]" % sys.argv[1])
OUT = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else tempfile.mkdtemp(prefix="port-merge-dryrun-"))
OUT.mkdir(parents=True, exist_ok=True)
if "Assets" in OUT.resolve().parts and str(OUT.resolve()).startswith(REPO):
    sys.exit("refusing to write under the project's Assets/")
SPECS = pathlib.Path(REPO) / "docs/port/merge-specs"
def show(ref, path):
    return subprocess.run(["git", "show", f"{ref}:{path}"], cwd=REPO, check=True, capture_output=True).stdout.decode("utf-8-sig")
def code_blocks(spec, heading_regex):
    """Return fenced csharp blocks under the first heading matching regex (until next heading of same/higher level)."""
    t = (SPECS / spec).read_text()
    m = re.search(heading_regex, t, re.M)
    assert m, (spec, heading_regex)
    rest = t[m.end():]
    nxt = re.search(r"^#{2,3} ", rest, re.M)
    sect = rest[: nxt.start()] if nxt else rest
    return re.findall(r"```csharp\n(.*?)```", sect, re.S)
fails = []
def rep(src, old, new, count=1, label=""):
    n = src.count(old)
    if n != count:
        fails.append(f"{label}: expected {count} match(es), found {n}: {old[:90]!r}")
        return src
    return src.replace(old, new)
def delete_line(src, text, count=1, label=""):
    lines = src.split("\n"); idx = [i for i, l in enumerate(lines) if l.strip() == text]
    if len(idx) != count:
        fails.append(f"{label}: delete_line expected {count}, found {len(idx)}: {text[:90]!r}"); return src
    return "\n".join(l for i, l in enumerate(lines) if i not in idx)
def replace_line(src, text, new, count=1, label=""):
    lines = src.split("\n"); idx = [i for i, l in enumerate(lines) if l.strip() == text]
    if len(idx) != count:
        fails.append(f"{label}: replace_line expected {count}, found {len(idx)}: {text[:90]!r}"); return src
    for i in idx:
        indent = lines[i][: len(lines[i]) - len(lines[i].lstrip())]
        lines[i] = "\n".join((indent + n if n else n) for n in new.split("\n"))
    return "\n".join(lines)
def replace_range(src, first, last, new, label=""):
    """Replace from the line whose strip()==first through the next line whose strip()==last (inclusive)."""
    lines = src.split("\n")
    s = [i for i, l in enumerate(lines) if l.strip() == first]
    if len(s) != 1: fails.append(f"{label}: range start {len(s)}x {first[:80]!r}"); return src
    e = next((j for j in range(s[0], len(lines)) if lines[j].strip() == last), None)
    if e is None: fails.append(f"{label}: range end missing {last[:80]!r}"); return src
    return "\n".join(lines[: s[0]] + new.rstrip("\n").split("\n") + lines[e + 1 :])
def method_span(lines, sig):
    s = [i for i, l in enumerate(lines) if l.strip() == sig]
    assert len(s) == 1, (sig, len(s))
    depth = 0; started = False
    for j in range(s[0], len(lines)):
        depth += lines[j].count("{") - lines[j].count("}")
        if "{" in lines[j]: started = True
        if started and depth == 0: return s[0], j
def replace_method(src, sig, new, label=""):
    lines = src.split("\n")
    try: a, b = method_span(lines, sig)
    except AssertionError as e: fails.append(f"{label}: {e}"); return src
    return "\n".join(lines[:a] + new.rstrip("\n").split("\n") + lines[b + 1 :])
def write(name, text): (OUT / name).write_text(text)
J = "Assets/JellyGarden/Scripts/"
AV = "JellyGarden.Scripts.Juice.AvatarEvents."
# ---------------- CombineManager
s = show("origin/royal-throne", J + "System/CombineManager.cs")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item))", label="CM")
lines = s.split("\n")
# the 'return false;' right after the deleted if: find IsSquareNotNull's duplicated 'return false;' pair
s = rep(s, "\t\t\tif (square.item == null)\n\t\t\t\treturn false;\n\t\t\t\treturn false;\n", "\t\t\tif (square.item == null)\n\t\t\t\treturn false;\n", label="CM return")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item) || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(nextItem)) return false;", label="CM2")
write("CombineManager.cs", s)
# ---------------- AI
s = show("origin/royal-throne", J + "AI.cs")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)) return;", label="AI125")
s = replace_line(s, "allowShowTip = !JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints;   // a lesson keeps the hint off while it shows", "allowShowTip = true;", label="AI158")
s = rep(s, " || RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)", "", label="AI209")
s = rep(s, " && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(square.item)", "", count=4, label="AI4x")
s = replace_line(s, "gotTip = true; if (JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints) return;   // the lesson's own outline stays", "gotTip = true;", label="AI589")
s = rep(s, "if(!allowShowTip||JellyGarden.Scripts.GUI.TutorialStuff.RoyalPowerTutorial.HoldsHints||BoardSettling(lm))break;", "if(!allowShowTip||BoardSettling(lm))break;", label="AI638")
write("AI.cs", s)
# ---------------- InputManager (take RT as-is)
write("InputManager.cs", show("origin/royal-throne", J + "InputManager.cs"))
# ---------------- Square
s = show("origin/royal-throne", J + "Square.cs")
k1 = code_blocks("Square.md", r"^\*\*Anchor\.\*\* Royal Throne method `Start`")[0]
s = replace_range(s, "// GenItem();", "}", "", label="SQ start-probe")  # placeholder to test range exists
s = show("origin/royal-throne", J + "Square.cs")
lines = s.split("\n"); a = [i for i, l in enumerate(lines) if l.strip() == "// GenItem();"][0]
# RT:64..73 => from '// GenItem();' through the 2nd closing brace after obj.transform.localPosition line
b = [i for i, l in enumerate(lines) if l.strip() == "obj.transform.localPosition = Vector3.zero + Vector3.down * 0.8f;"][0] + 2
s = "\n".join(lines[:a] + k1.rstrip("\n").split("\n") + lines[b + 1 :])
s = rep(s, "            // An earned power-up replaces its matched piece; it is not a refill and\n            // must not consume a queued egg/coin or inherit that item's identity.\n            if (campaignRefill)\n                RoyalThrone.Campaign.RoyalReferenceCampaign.ConfigureSpawn(itemInstance, falling);\n", "", label="SQ GenItem")
s = rep(s, "            if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this.item))\n                return countedSquares;\n", "", label="SQ FMM")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item)) return false;", label="SQ CanGoOut")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.HandleHit(this, rocketClear, campaignWave, campaignMatchedItem)) return;", label="SQ DestroyBlock")
write("Square.cs", s)
# ---------------- MenuManager (main + BlocksBoardInput)
s = show("origin/main", J + "GUI/MenuManager.cs")
blk = code_blocks("MenuManager.md", r"^## 3\. ADD-FROM-ROYAL-THRONE")[0]
s = rep(s, "        [SerializeField] private GameObject[] stars;\n", "        [SerializeField] private GameObject[] stars;\n" + blk, label="MM")
write("MenuManager.cs", s)
# ---------------- SoundBase
s = show("origin/royal-throne", J + "SoundBase.cs")
blk = code_blocks("SoundBase.md", r"^### K1\.")[0]
s = rep(s, "\t\tpublic void SetVolume(float volume) {", blk + "\n\t\tpublic void SetVolume(float volume) {", label="SB")
write("SoundBase.cs", s)
# ---------------- MusicBase
s = show("origin/royal-throne", J + "MusicBase.cs")
s = delete_line(s, "if (Juice.RoyalAudioDirector.Get) { if (_audioSource) _audioSource.Stop(); return; }", label="MB")
write("MusicBase.cs", s)
# ---------------- TargetLevel
s = show("origin/royal-throne", J + "Targets/TargetLevel.cs")
s = rep(s, """            // Migrate runtime copies only; retain authored assets and player progress.
            if (other.type == Target.COLLECT && other.color == 5)
            {
                other.color = Juice.RoyalGameplayArt.BoardColor(other.color);
                other.icon = Juice.RoyalGameplayArt.For(ItemsTypes.NONE, other.color) ?? other.icon;
            }
""", "            other.icon = JellyGarden.Scripts.Story.StorybookBoardArt.Goal(other);\n", label="TL")
write("TargetLevel.cs", s)
# ---------------- TargetIcon
s = show("origin/royal-throne", J + "Targets/TargetIcon.cs")
blk = code_blocks("TargetIcon.md", r"^\*\*Replace RT:45–50 with:\*\*")[0]
s = replace_range(s, "// Cache the data reference before replacing the visual sprite.", "imageRenderer.sprite = RoyalThrone.Presentation.RoyalVisualCatalog.Goal(target);", blk, label="TI")
write("TargetIcon.cs", s)
# ---------------- TargetsGroup
s = show("origin/royal-throne", J + "Targets/TargetsGroup.cs")
tg = re.findall(r"```csharp\n(.*?)```", (SPECS / "TargetsGroup.md").read_text(), re.S)[0]
s = rep(s, tg, "", label="TG")
write("TargetsGroup.cs", s)
# ---------------- BoostShop / BoostIcon
s = show("origin/royal-throne", J + "GUI/BoostShop.cs")
s = delete_line(s, "Juice.RoyalEconomy.Apply(boostProducts);   // pack sizes and prices come from the coin rules", label="BS")
write("BoostShop.cs", s)
write("BoostIcon.cs", show("origin/royal-throne", J + "GUI/BoostIcon.cs"))
# ---------------- InitScript (main + RoyalSave alias; default adds no members, InitScript.md §3)
s = show("origin/main", J + "InitScript.cs")
if os.environ.get("PORT_DRYRUN_INITSCRIPT_SHIMS"):   # the spec's alternative: QA shims
    blk = code_blocks("InitScript.md", r"^\*\*Alternative: add QA shims")[0]
    s = rep(s, "        public static string DateOfExit;\n", "        public static string DateOfExit;\n" + blk, label="IS")
s = rep(s, "        public static string DateOfExit;\n", "        public static string DateOfExit;\n", label="IS anchor")
s = "using PlayerPrefs = RoyalThrone.Testing.RoyalSave;\n" + s
write("InitScript.cs", s)
write("AnimationManager.cs", show("origin/main", J + "GUI/AnimationManager.cs"))
# ---------------- Item
s = show("origin/royal-throne", J + "Item.cs")
s = replace_line(s, "COLOR = RoyalGameplayArt.BoardColor(value);", "COLOR = value;", label="IT86")
s = rep(s, "            if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryColor(this,exceptColor))\n            {StartCoroutine(FallingCor(square,true));return;}\n", "", label="IT194")
s = replace_line(s, "int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, RoyalGameplayArt.BoardColorCount);", "int paletteSize = Mathf.Clamp(LevelManager.Instance.colorLimit, 1, items.Length);", label="IT200")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this)) { ResetDrag(); return; }", label="IT435")
s = delete_line(s, "if (RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(switchItem)) { ResetDrag(); return; }", label="IT470")
s = replace_line(s, "RoyalThrone.Characters.KingVanguardActivityEvents.NotifyPowerupCreated(LevelManager.THIS, this);", AV + "NotifyPowerupCreated(LevelManager.THIS, this);", count=2, label="IT avatar")
s = delete_line(s, "royalCampaignHitWave = RoyalThrone.Campaign.RoyalCampaignBlockers.CurrentWave;", label="IT1301")
s = delete_line(s, "RoyalThrone.Campaign.RoyalCampaignBlockers.OnItemDestroying(this);", label="IT1302")
s = delete_line(s, "// An accepted campaign coin hit already owns its recovered particles and SFX.", label="IT1328")
s = delete_line(s, "// Release the board through the same completion path without a second fruit pop.", label="IT1329")
s = delete_line(s, "bool royalCoin = RoyalThrone.Campaign.RoyalCampaignBlockers.HasCoinPresentation(this);", label="IT1330")
s = replace_line(s, "if (royalSpecial || royalConsumed || royalCoin)", "if (royalSpecial || royalConsumed)", label="IT1331")
s = delete_line(s, "if (royalSpecial) RoyalWinSequence.FinaleBoosterActivated(this);", label="IT1333")
s = replace_line(s, "if (!royalConsumed && !royalCoin) RoyalPowerupDirector.Get.Queue(this);", "if (!royalConsumed) RoyalPowerupDirector.Get.Queue(this);", label="IT1334")
s = replace_line(s, "else if (royalSpecial || royalConsumed || royalCoin) { }", "else if (royalSpecial || royalConsumed) { }", label="IT1340")
s = replace_line(s, "currentType == ItemsTypes.NONE && !RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(this);", "currentType == ItemsTypes.NONE;", label="IT1417")
s = delete_line(s, "// The profile counts the power-ups matches make while the level is played (RoyalProfile).", label="IT1441")
s = delete_line(s, "if (LevelManager.THIS && LevelManager.THIS.gameStatus == GameState.Playing && Juice.RoyalPowerupDirector.IsPower(i.currentType)) RoyalThrone.RoyalProfile.PowerupMade();", label="IT1442")
s = delete_line(s, "[HideInInspector] public bool royalPropellerV3Clear;", label="IT1495")
s = delete_line(s, "[HideInInspector] public bool royalPropellerV4Clear;", label="IT1496")
s = replace_range(s, "if (royalPropellerV4Clear)", "}", "", label="IT V4")
s = replace_range(s, "// V3's precise air hit uses its own tiny chip pool. TNT/Rocket branches above are unchanged.", "}", "", label="IT V3")
write("Item.cs", s)
# ---------------- LevelManager
s = show("origin/royal-throne", J + "LevelManager.cs")
L = "LevelManager.md"
cb = lambda h, i=0: code_blocks(L, h)[i]
# bottom-up-insensitive because we match by text
s = replace_line(s, "colorLimit = Mathf.Clamp(int.Parse(blocksString), 1, Juice.RoyalGameplayArt.BoardColorCount);", "colorLimit = int.Parse(blocksString);", label="LM D3")
s = rep(s, "            if(RoyalThrone.Campaign.RoyalReferenceCampaign.TryLoad(this,currentLevel,out var referenceBoard))\n            {levelSquaresFile=referenceBoard;return;}\n", "", label="LM C20")
s = replace_method(s, "void CheckStars()", cb(r"^### K10\.", 0), label="LM K10")
s = rep(s, "        void UpdateBar()\n        {\n", "        void UpdateBar()\n        {\n" + cb(r"^### K9\."), label="LM K9")
s = rep(s, "        public List<Square> GetBottomRow()\n", cb(r"^### K5\.") + "        public List<Square> GetBottomRow()\n", label="LM K5")
s = replace_line(s, "RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(this, matchedPieces);", AV + "NotifyMatchResolved(this, matchedPieces);", label="LM C19")
s = rep(s, "                foreach (List<Item> desrtoyItems in combinedItems)\n", cb(r"^### K12\."), label="LM K12")
s = delete_line(s, "if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)RoyalThrone.Campaign.RoyalCampaignBlockers.BeginMatchWave();", label="LM C18")
s = rep(s, "                    if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus==GameState.RegenLevel&&\n                        RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item.GetComponent<Item>()))continue;\n", "", label="LM C17")
s = replace_range(s, "if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)", "}", "", label="LM C16")
s = rep(s, "            if(onlyFalling&&RoyalThrone.Campaign.RoyalReferenceCampaign.Active&&gameStatus!=GameState.PreWinAnimations)\n            {yield return new WaitForFixedUpdate();yield break;}\n", "", label="LM C15")
c14 = cb(r"^C14 replacement", 0) if False else re.findall(r"C14 replacement.*?```csharp\n(.*?)```", (SPECS / L).read_text(), re.S)[0]
s = replace_range(s, "if (InputManager.Instance.Gameplay.Cancel.WasPressedThisFrame())", "}", c14, label="LM C14")
# C14 range ends at the first '}' after start which is the WasReleased block's closing brace: verify
s = replace_method(s, "IEnumerator PreWinAnimationsCor()", cb(r"^### K11\."), label="LM K11")
lines = s.split("\n")
a = [i for i, l in enumerate(lines) if l.strip().startswith("/// A win's lasting effects, once per attempt")]
if len(a) == 1:
    a = a[0] - 1
    b = method_span(lines, "void CommitWin()")[1]
    s = "\n".join(lines[:a] + lines[b + 1 :])
else: fails.append("LM C12 start")
s = replace_method(s, "public void CheckWinLose()", cb(r"^### K6\."), label="LM K6")
s = rep(s, cb(r"^### K8\.", 0), cb(r"^### K8\.", 1), label="LM K8")
s = rep(s, "                Vector2 pos = objectTarget.guiObj.transform.position;\n", cb(r"^### K7\.", 0) + "                Vector2 pos = objectTarget.guiObj.transform.position;\n", label="LM K7")
s = rep(s, "            MenuManager.Instance.ShowPrePlay(true);\n            if (limitType == LIMIT.MOVES)", cb(r"^### K4\.").rstrip("\n") + "\n            if (limitType == LIMIT.MOVES)", label="LM K4")
s = rep(s, "            // The profile's first-try wins (RoyalProfile): an attempt at this level begins here, once per attempt.\n            RoyalThrone.RoyalProfile.AttemptStarted(currentLevel, PlayerPrefs.GetInt(string.Format(\"Level.{0:000}.StarsCount\", currentLevel), 0) > 0);\n", "", label="LM C10")
s = rep(s, cb(r"^### K3\.", 0), cb(r"^### K3\.", 1), label="LM K3")
s = delete_line(s, "winCommitted = false; StarsGained = 0; committedCoins = 0; ContinuesBought = 0;", label="LM C9")
s = rep(s, "        void PrepareGame()\n", cb(r"^### K2\.") + "        void PrepareGame()\n", label="LM K2")
s = delete_line(s, "RoyalThrone.Campaign.RoyalReferenceCampaign.Build(this);", label="LM C8")
s = replace_line(s, "enableInApps = RoyalThrone.RoyalOfflineMode.ServicesEnabled;", "gameObject.AddComponent<UnityInAppsIntegration>();", label="LM C7b1")
s = replace_line(s, "if (enableInApps) gameObject.AddComponent<UnityInAppsIntegration>();", "enableInApps = true;//1.6.1", label="LM C7b2")
s = replace_line(s, "FacebookEnable = RoyalThrone.RoyalOfflineMode.ServicesEnabled;", "FacebookEnable = true;//1.6.2", label="LM C7a")
s = replace_line(s, 'InitScript.DateOfExit = DateTime.UtcNow.ToString("o", global::System.Globalization.CultureInfo.InvariantCulture);  //1.4', "InitScript.DateOfExit = DateTime.Now.ToString();  //1.4", label="LM C6")
s = rep(s, "            LoadDataFromLocal(currentLevel);\n\n        }", "            LoadDataFromLocal(currentLevel);\n            // Apply campaign tuning after every load, including PrepareGame's\n            // second load. The shipped board files remain the source layout.\n            Story.StoryCampaign.ApplyLevelTuning(this);\n        }", label="LM K1")
# K13
lines = s.split("\n")
wi = [i for i, l in enumerate(lines) if l.strip() == "else if (value == GameState.Win)"]
if len(wi) == 1:
    depth = 0; started = False
    for j in range(wi[0], len(lines)):
        depth += lines[j].count("{") - lines[j].count("}")
        if "{" in lines[j]: started = True
        if started and depth == 0: break
    s = "\n".join(lines[: wi[0]] + cb(r"^### K13\.", 1).rstrip("\n").split("\n") + lines[j + 1 :])
else: fails.append("LM K13 win")
s = rep(s, "                    CommitWin();\n                    StartCoroutine(PreWinAnimationsCor());", "                    StartCoroutine(PreWinAnimationsCor());", label="LM C4")
s = rep(s, """        /// <summary>Stars this win added to the Area balance: 1 on a level's first win, 0 on a replay (set by CommitWin).</summary>
        public int StarsGained { get; private set; }
        // A win's lasting effects are paid once per attempt (CommitWin), the moment its goals are met.
        bool winCommitted; int committedCoins, committedCoinsBefore;
""", "", label="LM C3")
s = replace_line(s, "RoyalThrone.Characters.KingVanguardActivityEvents.NotifyExtraMovesGranted(this);", AV + "NotifyExtraMovesGranted(this);", label="LM C2")
s = rep(s, "        /// <summary>Continues bought in this attempt: each one costs more (RoyalEconomy.ContinueCost).</summary>\n        [HideInInspector] public int ContinuesBought;\n", "", label="LM C1")
write("LevelManager.cs", s)

BASES = {"CombineManager.cs": ("origin/royal-throne", "System/CombineManager.cs"), "AI.cs": ("origin/royal-throne", "AI.cs"),
         "InputManager.cs": ("origin/royal-throne", "InputManager.cs"), "Square.cs": ("origin/royal-throne", "Square.cs"),
         "MenuManager.cs": ("origin/main", "GUI/MenuManager.cs"), "SoundBase.cs": ("origin/royal-throne", "SoundBase.cs"),
         "MusicBase.cs": ("origin/royal-throne", "MusicBase.cs"), "TargetLevel.cs": ("origin/royal-throne", "Targets/TargetLevel.cs"),
         "TargetIcon.cs": ("origin/royal-throne", "Targets/TargetIcon.cs"), "TargetsGroup.cs": ("origin/royal-throne", "Targets/TargetsGroup.cs"),
         "BoostShop.cs": ("origin/royal-throne", "GUI/BoostShop.cs"), "BoostIcon.cs": ("origin/royal-throne", "GUI/BoostIcon.cs"),
         "InitScript.cs": ("origin/main", "InitScript.cs"), "AnimationManager.cs": ("origin/main", "GUI/AnimationManager.cs"),
         "Item.cs": ("origin/royal-throne", "Item.cs"), "LevelManager.cs": ("origin/royal-throne", "LevelManager.cs")}
def balance(text): return text.count("{") - text.count("}"), text.count("(") - text.count(")")
for name, (ref, rel) in BASES.items():
    if balance((OUT / name).read_text()) != balance(show(ref, J + rel)):
        fails.append(f"{name}: brace/paren balance differs from its base {ref}")
BANNED = re.compile(r"RoyalThrone\.(Campaign|Characters|RoyalProfile|RoyalOfflineMode)|RoyalBackButton|RoyalWinSequence|RoyalEconomy|CommitWin|"
                    r"winCommitted|StarsGained|ContinuesBought|BoardColor|RoyalPowerTutorial|RoyalShopPage|Propeller\.V[34]|royalPropellerV[34]Clear|"
                    r"royalCoin|KingVanguardActivityEvents\.|StoryOriginal[A-Za-z]*\.|StoryBoardScale|StoryRoyalVfx|OverlapProbe|RoyalMenuSkin")
for name in BASES:
    for i, line in enumerate((OUT / name).read_text().split("\n"), 1):
        code = line.split("//")[0]
        if BANNED.search(code):
            fails.append(f"{name}:{i}: leftover coupling: {line.strip()[:100]}")
csc = sorted(glob.glob(os.path.expanduser("~/.dotnet/sdk/*/Roslyn/bincore/csc.dll")))
dotnet = os.path.expanduser("~/.dotnet/dotnet")
if csc and os.path.exists(dotnet):
    for name in BASES:
        r = subprocess.run([dotnet, csc[-1], "-nologo", "-t:library", "-out:" + os.devnull, str(OUT / name)], capture_output=True, text=True)
        errs = sorted(set(re.findall(r"error CS1\d{3}[^\n]*", r.stdout + r.stderr)))
        if errs:
            fails.append(f"{name}: C# syntax errors: " + "; ".join(errs[:3]))
    print("syntax check: Roslyn", csc[-1].split("/sdk/")[1].split("/")[0])
else:
    print("syntax check: skipped (no ~/.dotnet SDK)")
print("output:", OUT)
print("FAIL" if fails else "PASS: %d files merged, anchors matched, braces balanced, no leftover couplings, no syntax errors" % len(BASES))
for f in fails: print("  " + f)
sys.exit(1 if fails else 0)
