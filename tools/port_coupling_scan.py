#!/usr/bin/env python3
"""List the Royal Throne couplings the level port has to cut (plan Task 1.3).

The port imports Royal Throne's level code (kit core, Juice, the in-level GUI
subset, the V4/V5 boosters, ReferenceGameplay and the QA suites) but not its
home screen, win sequence, coin economy, reference campaign, QA mod menu or the
KingVanguard rig. Every line of imported code that still reaches one of those
is a coupling. This script finds them and gives each one resolution.

Two uses:

  1. Before the import: scan the source branch and print the cut list that
     docs/port/COUPLINGS.md is built from.

         python3 tools/port_coupling_scan.py                   # TSV, origin/royal-throne
         python3 tools/port_coupling_scan.py --format md       # Markdown tables

  2. After Tasks 3.1-3.5: scan the imported files in the working tree. Every
     coupling whose resolution is not "import" must be gone; files marked
     "not imported" must be absent. Exit status 1 lists what is left.

         python3 tools/port_coupling_scan.py --worktree

Read-only: it uses `git ls-tree` / `git cat-file` and never writes the index or
the working tree. Comments are reported separately and never fail the check.

    python3 tools/port_coupling_scan.py --self-test
"""

import argparse
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_REF = "origin/royal-throne"
JG = "Assets/JellyGarden/Scripts/"

# --------------------------------------------------------------------------- the import set
# Directories whose .cs files the port imports (plan §5, Task 3.5, Task 4.0).
IMPORT_DIRS = [
    JG + "Juice/",
    "Assets/Boosters/Propeller/V5/",
    "Assets/Boosters/ColorBall/V4/",
    "Assets/Boosters/ColorBall/V5/",
    "Assets/ReferenceGameplay/",
]
# Single files the port imports or takes from Royal Throne.
IMPORT_FILES = [
    # Kit core taken from Royal Throne (plan §4). MenuManager is main's file plus
    # one member, so it is not scanned here; see COUPLINGS.md "Members main lacks".
    JG + "LevelManager.cs", JG + "Item.cs", JG + "Square.cs", JG + "AI.cs",
    JG + "InputManager.cs", JG + "System/CombineManager.cs",
    JG + "GUI/BoostShop.cs", JG + "GUI/BoostIcon.cs",
    JG + "Targets/TargetIcon.cs", JG + "Targets/TargetLevel.cs", JG + "Targets/TargetsGroup.cs",
    JG + "SoundBase.cs", JG + "MusicBase.cs",           # merged with main's (union)
    # In-level GUI subset (plan §5) plus what its closure needs (COUPLINGS.md §3).
    JG + "GUI/RoyalGameplayPresentation.cs", JG + "GUI/RoyalGameplayHud.cs",
    JG + "GUI/RoyalBoardSurface.cs", JG + "GUI/RoyalArchShape.cs", JG + "GUI/RoyalCardShape.cs",
    JG + "GUI/RoyalPanelGraphic.cs", JG + "GUI/RoyalVisualCatalog.cs",
    JG + "GUI/RoyalTextStyle.cs", JG + "GUI/RoyalPressTracker.cs",
    JG + "RoyalSave.cs", JG + "RoyalQaService.cs",
    # Editor files the import closure adds (IMPORT-MANIFEST.md §4, §7; AUDIT-REVIEW.md).
    JG + "Editor/RoyalGameplayArtImporter.cs", JG + "Editor/RoyalWebPImporter.cs", JG + "Editor/RoyalVfxLab.cs",
    # Play-mode suites (Task 4.0) and the suites RoyalEffectsChecks runs.
] + [JG + "Editor/" + n + ".cs" for n in (
    "RoyalEffectsChecks", "RoyalHintPresentationChecks", "RoyalOverlapChecks",
    "RoyalPropellerFormationChecks", "RoyalSpecialFormationChecks", "RoyalFormationReferenceChecks",
    "RoyalMatchResponseChecks", "RoyalSquarePlusOneChecks", "RoyalBlockedRefillChecks",
    "RoyalSuperBombFuseChecks", "RoyalQaChecks", "RoyalColorBallQaChecks",
    "RoyalInputRegression", "RoyalColorBallChecks", "RoyalTNTChecks", "RoyalRocketChecks",
    "RoyalCellBreakChecks", "RoyalBoardSurfaceChecks", "RoyalToolkitChecks",
    "RoyalBoardPresentationChecks", "RoyalOfflineBuildPolicy")]

# Files inside the import set that the port leaves behind, and why.
NOT_IMPORTED = {
    JG + "Juice/RoyalWinSequence.cs": "D4: main keeps its win flow; only ReferenceWinFinish is taken",
    JG + "Juice/RoyalEconomy.cs": "D5: no coin economy",
    JG + "Juice/RoyalReviewAudioCapture.cs": "editor-only review capture; the import closure cuts it (IMPORT-MANIFEST.md §5, §7), so RoyalEffectsChecks drops its two uses",
    JG + "Editor/RoyalOfflineBuildPolicy.cs": "fails every build while main defines UNITY_ADS/UNITY_INAPPS; nothing imported uses it",
    "Assets/ReferenceGameplay/Home/Scripts/ReferenceHomeLayout.cs": "Royal Throne home screen",
    "Assets/ReferenceGameplay/Home/Scripts/ReferenceHomeSky.cs": "Royal Throne home screen",
    "Assets/ReferenceGameplay/Editor/ReferenceCoinChecks.cs": "D4/D5/D8: win coins, home reward, campaign coin blocker",
    "Assets/ReferenceGameplay/Editor/ReferenceHomeFinishChecks.cs": "drives the finish from RoyalHomeScreen",
    "Assets/ReferenceGameplay/Editor/ReferenceHomeSkyChecks.cs": "Royal Throne home screen",
    "Assets/ReferenceGameplay/Editor/ReferenceLevelChecks.cs": "D8: reference levels 01-50",
    "Assets/ReferenceGameplay/Editor/ReferenceGameplayChecks.cs": "Design/ inbox hub for importers, home, campaign and captures; PortChecks (Task 4.0) replaces it",
    "Assets/ReferenceGameplay/Editor/ReferenceArcherImporter.cs": "one-shot importer reading Design/ sources",
    "Assets/ReferenceGameplay/Editor/ReferenceCannonImporter.cs": "one-shot importer reading Design/ sources",
    "Assets/ReferenceGameplay/Editor/ReferenceCoinImporter.cs": "one-shot importer reading Design/ sources (coin, D5/D8)",
    "Assets/ReferenceGameplay/Editor/ReferenceDualColorBallImporter.cs": "one-shot importer reading Design/ sources",
    "Assets/ReferenceGameplay/Editor/ReferenceDualComboImporter.cs": "one-shot importer reading Design/ sources",
    "Assets/ReferenceGameplay/Editor/ReferenceHammerImporter.cs": "one-shot importer reading Design/ sources",
    "Assets/ReferenceGameplay/Animation/Editor/ReferenceGameplayMediaImporter.cs": "outside the plan's import set; reads Design/",
    "Assets/ReferenceGameplay/Art/Editor/ReferenceBlockerImporter.cs": "outside the plan's import set; D8 blockers",
    "Assets/ReferenceGameplay/VFX/Editor/ReferenceGameplayVfxImporter.cs": "outside the plan's import set; reads Design/",
    "Assets/ReferenceGameplay/VFX/Editor/ReferenceRfv387FuseImporter.cs": "outside the plan's import set; reads Design/",
    "Assets/ReferenceGameplay/WinVFX/Editor/ReferenceWinVfxImporter.cs": "outside the plan's import set; win VFX (D4)",
}

# --------------------------------------------------------------------------- the rules
# (key, regex, default resolution, allowed_after_port)
# allowed_after_port: the line may stay once the port is done (an "import"
# resolution); everything else must be gone from the working tree.
RULES = [
    ("requirecopy", r"\bRequireCopy\b|\bRoyalHomeScreenBuilder\b",
     "remove (Task 3.5 Step 4: the folder-name guard)", False),
    ("home", r"\bRoyalHomeScreen\b|\bReferenceHome(Layout|Sky)\b",
     "remove (Royal Throne home screen, out of scope)", False),
    ("title", r"\bRoyalTitleScreen\b|\bQueueStartupWork\b",
     "remove (startup loading screen, out of scope)", False),
    ("win", r"\bRoyalWinSequence\b|\bCommitWin\b|\bRoyalWinCard\b|\bwinCommitted\b|\bcommittedCoins(Before)?\b|\bCommittedCoins\b|\bStarsGained\b",
     "remove (D4)", False),
    ("economy", r"\bRoyalEconomy\b|\bRoyalShopPage\b|\bContinuesBought\b",
     "remove (D5)", False),
    ("campaign", r"\bRoyalReferenceCampaign\w*|\bRoyalCampaign(Blockers|Item|Piece|Checks|BlockerChecks|ArtImporter)\b|\bIsSpecialItem\b|\bCampaignGoal\b|ROYAL_REFERENCE_CAMPAIGN|\broyalCampaignHitWave\b|\bCampaignArt\b|\bCampaignResource(Root|Path)\b|\bCampaignKeys\b",
     "remove (D8)", False),
    ("menuskin", r"\bRoyalMenuSkin\b",
     "replace-with-main-equivalent: RoyalGameplayHud.Place (copy of RoyalMenuSkin.Place, 5 lines)", False),
    ("menupresenter", r"\bRoyalMenuPresenter\b",
     "remove (main's StoryPrePlay owns the pre-level card)", False),
    ("back", r"\bRoyalBackButton\b",
     "replace-with-main-equivalent: main LevelManager Cancel handling (ShowMenuPause / Application.Quit)", False),
    ("tutorial", r"\bRoyalPowerTutorial\b|\bHoldsHints\b",
     "remove (Royal Throne power tutorial not ported)", False),
    ("king", r"\bKingVanguard\w*",
     "replace-with-main-equivalent: StoryAvatar (D2)", False),
    ("kingfield", r"\bkingVanguard\b|\bkingFeedback\b|\bKingToolUsed\b|\bKingToolStruck\b",
     "replace-with-main-equivalent: StoryAvatar (D2: presentation-side KingVanguard driver)", False),
    ("toolevents", r"Notify\(Tool(Used|Struck)",
     "retarget:AvatarEvents.ToolUsed / ToolStruck (raise next to this line)", True),
    ("toolstock", r"RoyalSave\.GetInt\(Types\[",
     "replace-with-main-equivalent: StoryBoosters.Owned(Types[index]) (D5)", False),
    ("toolshop", r"\bOpenShop\(",
     "guard-off (D5: only when StoryFreeGame.ShopsAllowed, which is false)", True),
    ("qaui", r"\bRoyalModMenu\b|\bRoyalQaMenu\b|\bRoyalHandoffCapture\b|\bRoyalReviewAudioCapture\b",
     "remove (QA mod-menu UI / capture tools, out of scope)", False),
    ("offline", r"\bRoyalOfflineMode\b|\bRoyalOfflineBuildPolicy\b",
     "remove", False),
    ("qadefine", r"\bROYAL_DISABLE_QA\b",
     "import (keep the guard; ROYAL_DISABLE_QA is defined on neither branch)", True),
    ("colorremap", r"\bBoardColor\s*\(",
     "remove (D3: no colour-5->1 remap)", False),
    ("colorcount", r"\bBoardColorCount\b",
     "import (D3: BoardColorCount becomes 6, the palette maximum)", True),
    ("oldboosters", r"\bBoosters\.(Propeller\.V[234]|ColorBall\.V[23])\b|\bPropeller(Visual|Vfx|Seat|Gameplay|Audio|Motion)V[234]\b|\bColorBall(Visual|Vfx|Seat|Audio)V[23]\b|\broyalPropellerV[34]Clear\b",
     "remove (Boosters V2/V3/V4 propeller and V2/V3 colour ball are not imported)", False),
    ("profile", r"\bRoyalProfile\w*",
     "remove (profile page, out of scope)", False),
    ("meta", r"\bRoyal(Area\w*|Cards|CardKit|LessonDemo|LottiePlayer|SettingsPage|LeaderboardPage|LevelCard\w*|Notice|RewardCard|FeaturePage|Store)\b",
     "remove (meta screen, out of scope)", False),
    ("music", r"\bLevelMusic\b|RoyalGameplay/Music/|\bmusic\[0\]\.Play\(\)|\bDesiredMusic\(\)|\bMusicTarget\(|\bStartFade\(|\bmusicFilter\b|var musicHost|music=new AudioSource|_audioSource\.Stop\(\); return; \}",
     "guard-off (music is out of scope; main's MusicBase keeps playing)", False),
    ("font", r"RoyalGameplay/Display",
     "import (Display.ttf is imported and byte-identical to main's Story/Fonts/story_display.ttf, PROVENANCE.md §4; pointing the load at StoryUi.DisplayFont is optional)", True),
    ("initscript", r"\bInitScript\.(NextLifeKey|Instance\.(LifeRefillDeadline|RestoreLifeScheduleForTesting|SetRefillSeconds|TryOpenLevel))\b",
     "remove (royal-throne-only InitScript member)", False),
    ("blocksinput", r"\bBlocksBoardInput\b",
     "import (add MenuManager.BlocksBoardInput to main's MenuManager)", True),
    ("guiclosure", r"\bRoyalTextStyle\b|\bRoyalPressTracker\b",
     "import (add the file to the GUI subset)", True),
    ("suites", r"\bRoyal(InputRegression|ColorBallChecks|TNTChecks|RocketChecks|CellBreakChecks|BoardSurfaceChecks|ToolkitChecks|BoardPresentationChecks)\b",
     "import (add the suite to Task 4.0)", True),
    ("importers", r"\bReference(Archer|Cannon|Coin|DualColorBall|DualCombo|Hammer)Importer\b|\bReferenceGameplay(Media|Vfx)Importer\b|\bReferenceBlockerImporter\b|\bReferenceRfv387FuseImporter\b|\bReferenceWinVfxImporter\b",
     "remove (asset importer not imported)", False),
    ("hub", r"\bReference(Coin|HomeFinish|HomeSky|Level)Checks\b|\bReferenceGameplayChecks\b",
     "remove (suite not imported)", False),
    ("design", r"\"(\.\./)?Design/|\"\.\./Design/|\"Design\b",
     "replace-with-main-equivalent: Logs/port-checks/<suite>/ (Task 4.0 Step 1)", False),
]

# Line-specific resolutions: (path suffix, substring of the line, resolution).
# The first match wins; they override the rule defaults above.
OVERRIDES = [
    # ---- LevelManager.cs
    ("LevelManager.cs", "NotifyExtraMovesGranted", "retarget:AvatarEvents.ExtraMoves"),
    ("LevelManager.cs", "NotifyMatchResolved", "retarget:AvatarEvents.Matched"),
    ("LevelManager.cs", "NotifyGoalsCompleted", "retarget:AvatarEvents.GoalsCompleted"),
    ("LevelManager.cs", "CommitWin();", "replace-with-main-equivalent: main's gameStatus setter (PreWinAnimations/Win branches, D4)"),
    ("LevelManager.cs", "void CommitWin()", "remove (D4: whole method, lines 813-841)"),
    ("LevelManager.cs", "RoyalWinSequence.Celebrate", "replace-with-main-equivalent: main's PreWinAnimationsCor with ReferenceWinFinish.Play (D4)"),
    ("LevelManager.cs", "RoyalWinSequence.BeginFinale", "replace-with-main-equivalent: main's PreWinAnimationsCor with ReferenceWinFinish.Play (D4)"),
    ("LevelManager.cs", "RoyalWinSequence.EndFinale", "replace-with-main-equivalent: main's PreWinAnimationsCor with ReferenceWinFinish.Play (D4)"),
    ("LevelManager.cs", "RoyalWinSequence.Reward", "replace-with-main-equivalent: main's Win branch (AddLife, StarsCount, Score, OnWin)"),
    ("LevelManager.cs", "RoyalBackButton.Quit", "replace-with-main-equivalent: Application.Quit() (main LevelManager Cancel handling)"),
    ("LevelManager.cs", "TryLoad(", "remove (D8: levels 1-5 must load main's Levels/<n>.txt)"),
    ("LevelManager.cs", "colorLimit = Mathf.Clamp", "replace-with-main-equivalent: colorLimit = int.Parse(blocksString) (D3)"),
    ("LevelManager.cs", "FacebookEnable = RoyalThrone.RoyalOfflineMode", "replace-with-main-equivalent: main's `FacebookEnable = true;` (inside #if FACEBOOK)"),
    ("LevelManager.cs", "enableInApps = RoyalThrone.RoyalOfflineMode", "replace-with-main-equivalent: main's UNITY_INAPPS block (`AddComponent<UnityInAppsIntegration>(); enableInApps = true;`) - compiles on main, which defines UNITY_INAPPS"),
    ("LevelManager.cs", "Reference refill cascades", "remove (D8)"),
    ("LevelManager.cs", "BeginMatchWave", "remove (D8)"),
    ("LevelManager.cs", "RoyalReferenceCampaign.Active)", "remove (D8: the campaign branch of GetRandomItems, lines 1591-1598; keep RT's fix below it)"),
    # ---- Item.cs
    ("Item.cs", "square.DestroyBlock(royalRocketClear, royalCampaignHitWave, campaignAdjacentMatch)", "import (kept: plan §4 keeps Square.DestroyBlock(rocketClear, wave, matched) and merge-specs/INDEX.md §5 keeps the inert campaign arguments; they do nothing once HandleHit is cut)"),
    ("Item.cs", "[HideInInspector] public int royalCampaignHitWave;", "import (kept: inert field, merge-specs/INDEX.md §5; only its RoyalCampaignBlockers assignment is cut)"),
    ("Item.cs", "COLOR = RoyalGameplayArt.BoardColor(value)", "remove (D3: `COLOR = value;`)"),
    ("Item.cs", "int paletteSize = Mathf.Clamp", "replace-with-main-equivalent: paletteSize = LevelManager.Instance.colorLimit (D3, no clamp)"),
    ("Item.cs", "NotifyPowerupCreated", "retarget:AvatarEvents.PowerupCreated"),
    ("Item.cs", "FinaleBoosterActivated", "remove (D4: finale coin bursts)"),
    ("Item.cs", "royalCoin = ", "remove (D8: coin blocker presentation; royalCoin is always false)"),
    ("Item.cs", "RoyalProfile.PowerupMade", "remove (profile page)"),
    # ---- Square.cs
    ("Square.cs", "RoyalCampaignBlockers.HandleHit", "remove (D8; keep DestroyBlock(rocketClear, ...) signature, plan §4 Square)"),
    # ---- AI.cs
    ("AI.cs", "allowShowTip = !", "remove (`allowShowTip = true;`)"),
    # ---- GUI/BoostShop.cs
    ("BoostShop.cs", "ReferenceBoosterBar.IsTool", "guard-off (D5: a tool never opens a purchase panel; return when IsTool)"),
    ("BoostShop.cs", "ReferenceBoosterBar.Instance.OpenShop", "guard-off (D5: a tool never opens a purchase panel; return when IsTool)"),
    ("BoostShop.cs", "RoyalEconomy.Apply", "remove (D5: keep the kit's boostProducts)"),
    # ---- Targets
    ("TargetsGroup.cs", "RoyalReferenceCampaign.Active", "remove (D8: the whole campaign if-block, lines 28-35)"),
    ("TargetLevel.cs", "RoyalGameplayArt.BoardColor(other.color)", "remove (D3: whole colour-5 migration block, lines 54-58)"),
    # ---- GUI/RoyalGameplayPresentation.cs
    ("RoyalGameplayPresentation.cs", "QueueStartupWork(this,steps)", "remove (run the build inline: `while(steps.MoveNext()){}`)"),
    ("RoyalGameplayPresentation.cs", "(RoyalTitleScreen.QueueStartupWork)", "remove (stale comment)"),
    ("RoyalGameplayPresentation.cs", "ExtraMovesGranted+=", "retarget:AvatarEvents.ExtraMoves (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "GoalsCompleted+=", "retarget:AvatarEvents.GoalsCompleted (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "MatchResolved+=", "retarget:AvatarEvents.Matched (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "PowerupCreated+=", "retarget:AvatarEvents.PowerupCreated (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "ExtraMovesGranted-=", "retarget:AvatarEvents.ExtraMoves (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "GoalsCompleted-=", "retarget:AvatarEvents.GoalsCompleted (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "MatchResolved-=", "retarget:AvatarEvents.Matched (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "PowerupCreated-=", "retarget:AvatarEvents.PowerupCreated (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "ReferenceBoosterBar.ToolUsed+=", "retarget:AvatarEvents.ToolUsed (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "ReferenceBoosterBar.ToolStruck+=", "retarget:AvatarEvents.ToolStruck (subscription moves to the avatar driver)"),
    ("RoyalGameplayPresentation.cs", "ReferenceBoosterBar.ToolUsed-=", "retarget:AvatarEvents.ToolUsed (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "ReferenceBoosterBar.ToolStruck-=", "retarget:AvatarEvents.ToolStruck (unsubscribe)"),
    ("RoyalGameplayPresentation.cs", "void KingToolUsed", "retarget:AvatarEvents.ToolUsed"),
    ("RoyalGameplayPresentation.cs", "void KingToolStruck", "retarget:AvatarEvents.ToolStruck"),
    ("RoyalGameplayPresentation.cs", "FromImpactStrength(value)", "retarget:AvatarEvents.Reaction(float)"),
    ("RoyalGameplayPresentation.cs", "Feedback.None)QueueKingFeedback", "retarget:AvatarEvents.Reaction(float)"),
    ("RoyalGameplayPresentation.cs", "SetWorried(lm.limitType", "retarget:AvatarEvents.Worried"),
    ("RoyalGameplayPresentation.cs", "KingVanguardSlot.Mount(", "replace-with-main-equivalent: StoryAvatar.Build(<character>, portraitMask.rectTransform) (Task 4.7 Step 2)"),
    ("RoyalGameplayPresentation.cs", "readonly RoyalThrone.Characters.KingVanguardFeedbackQueue", "replace-with-main-equivalent: Juice/AvatarFeedbackQueue (copy of KingVanguardFeedbackQueue, pure C#; RT's throttle, Task 4.7 Step 3)"),
    ("RoyalGameplayPresentation.cs", "RoyalMenuSkin.Apply", "remove (restyles the kit menus; main restyles them with StorybookKitMenus)"),
    # ---- Juice
    ("RoyalGameplayArt.cs", "public const int BoardColorCount = 5", "replace-with-main-equivalent: 6 (palette maximum; the level's own count is LevelManager.colorLimit, D3)"),
    ("RoyalGameplayArt.cs", "public static int BoardColor(int color)", "remove (D3)"),
    ("RoyalGameplayArt.cs", "color = BoardColor(color);", "remove (D3)"),
    ("RoyalGameplayArt.cs", "var campaign = RoyalPieceSkin.CampaignArt(key)", "guard-off (D8, but see COUPLINGS.md §4: RoyalCampaignArt holds gem art)"),
    ("RoyalPieceSkin.cs", "TryGetComponent(out RoyalThrone.Campaign.RoyalCampaignItem", "remove (D8)"),
    ("RoyalPieceSkin.cs", "CampaignResourceRoot = ", "guard-off (D8, but see COUPLINGS.md §4: RoyalCampaignArt holds gem art)"),
    ("RoyalPieceSkin.cs", "", "guard-off (D8, but see COUPLINGS.md §4: RoyalCampaignArt holds gem art)"),
    ("RoyalPowerupDirector.cs", "PropellerVisualV4.Clear()", "remove (V2/V3/V4 propeller Clear() calls; keep the V5 one)"),
    ("RoyalPowerupDirector.cs", "item.color < RoyalGameplayArt.BoardColorCount", "import (D3: works with BoardColorCount = 6)"),
    ("RoyalPowerupDirector.cs", "new int[RoyalGameplayArt.BoardColorCount]", "import (D3: works with BoardColorCount = 6)"),
    ("RoyalPowerupDirector.cs", "colour>=RoyalGameplayArt.BoardColorCount", "import (D3: works with BoardColorCount = 6)"),
    ("RoyalAudioDirector.cs", "", "guard-off (music is out of scope; main's MusicBase keeps playing)"),
    ("MusicBase.cs", "RoyalAudioDirector.Get", "remove (keep main's PlayMusic; music stays main's)"),
    # ---- GUI/RoyalGameplayHud.cs
    ("RoyalGameplayHud.cs", "RoyalMenuSkin.Place", "replace-with-main-equivalent: RoyalGameplayHud.Place (copy of RoyalMenuSkin.Place, 5 lines)"),
    # ---- RoyalQaService.cs / RoyalSave.cs
    ("RoyalQaService.cs", "#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA", "import (keep the guard)"),
    ("RoyalQaService.cs", "LifeRefillDeadline", "remove (snapshot InitScript.RestLifeTimer only; main has no LifeRefillDeadline)"),
    ("RoyalQaService.cs", "RestoreLifeScheduleForTesting", "replace-with-main-equivalent: InitScript.RestLifeTimer = normalTimer"),
    ("RoyalQaService.cs", "SetRefillSeconds", "replace-with-main-equivalent: InitScript.RestLifeTimer = seconds"),
    ("RoyalQaService.cs", "RoyalHomeScreen", "remove (Refresh() becomes a no-op)"),
    ("RoyalQaService.cs", "Coin reward", "remove (the whole \"Coin reward\" command: RoyalQaMenu.CoinReward, D5)"),
    ("RoyalQaService.cs", "Spawn selected gem", "import (D3: works with BoardColorCount = 6)"),
    ("RoyalQaService.cs", "LM.colorLimit=RoyalGameplayArt.BoardColorCount", "replace-with-main-equivalent: LM.colorLimit=5 (practice fixture; keep 5 so existing suites keep their seeds)"),
    ("RoyalSave.cs", "ROYAL_DISABLE_QA", "import (keep the guard)"),
    # ---- ReferenceBoosterBar.cs
    ("ReferenceBoosterBar.cs", "public static bool Locked=>", "replace-with-main-equivalent: LevelManager.THIS.currentLevel <= 5 && !QaPractice (Task 4.8 Step 3)"),
    ("ReferenceBoosterBar.cs", "Tools unlock at Level", "replace-with-main-equivalent: local const OpeningLevels = 5"),
    ("ReferenceBoosterBar.cs", "RoyalEconomy.ToolPack", "remove (D5)"),
    ("ReferenceBoosterBar.cs", "RoyalShopPage.Open", "remove (D5: Buy() is unreachable once the panel is off)"),
    ("ReferenceBoosterBar.cs", "RoyalShopPage.IsOpen", "replace-with-main-equivalent: `if(Cancel released && !RoyalBackButton...)` -> `if(Cancel released)`; LevelManager skips pause while ReferenceBoosterBar.OwnsInput"),
    ("ReferenceBoosterBar.cs", "if(ShopOpen){CloseShop();RoyalThrone.Presentation.RoyalBackButton.Consume()", "remove (Consume(); keep CloseShop/CancelSelection)"),
    ("ReferenceBoosterBar.cs", "if(Selected>=0&&!Busy){CancelSelection();RoyalThrone.Presentation.RoyalBackButton.Consume()", "remove (Consume(); keep CancelSelection)"),
    ("ReferenceBoosterBar.cs", "if(Stock(index)<=0){OpenShop(type);return;}", "guard-off (D5: open the panel only if StoryFreeGame.ShopsAllowed; otherwise show an empty-stock hint)"),
    ("ReferenceBoosterBar.cs", "Notify(ToolUsed", "retarget:AvatarEvents.ToolUsed (raise next to this line)"),
    ("ReferenceBoosterBar.cs", "Notify(ToolStruck", "retarget:AvatarEvents.ToolStruck (raise next to this line)"),
    ("ReferenceBoosterBar.cs", "RoyalMenuSkin.Place", "replace-with-main-equivalent: RoyalGameplayHud.Place (copy of RoyalMenuSkin.Place, 5 lines)"),
    ("ReferenceBoosterBar.cs", "(UNITY_EDITOR || DEVELOPMENT_BUILD) && !ROYAL_DISABLE_QA", "import (keep the guard)"),
    # ---- ReferenceWinFinish.cs
    # ---- Boosters
    ("PropellerGameplayV5.cs", "using RoyalThrone.Campaign;", "remove (D8; the namespace is not imported)"),
    ("PropellerGameplayV5.cs", "", "remove (D8: whole `if(RoyalReferenceCampaign.Active){...} else` branch, lines 43-51)"),
    # ---- Editor suites
    ("RoyalEffectsChecks.cs", "TryOpenLevel", "replace-with-main-equivalent: StoryFlow.LaunchLevel(level, areaId) - or let PortChecks open the level (Task 4.0)"),
    ("RoyalEffectsChecks.cs", "LevelMusic", "remove (music is out of scope)"),
    ("RoyalEffectsChecks.cs", "RoyalAudioDirector.Get.PeakVoices", "import"),
    ("RoyalQaChecks.cs", "InitScript.NextLifeKey", "remove (drop the key from the snapshot; \"RestLifeTimer\" is already there)"),
    ("RoyalQaChecks.cs", "RoyalReferenceCampaign.Active&&LevelManager.THIS.colorLimit", "replace-with-main-equivalent: Check(LevelManager.THIS.colorLimit==5, ...) (practice fixture, see RoyalQaService.Practice)"),
    ("RoyalQaChecks.cs", "for(int color=0;color<RoyalGameplayArt.BoardColorCount", "import (D3: loops over 6 colours once BoardColorCount = 6)"),
    ("RoyalQaChecks.cs", "RoyalEconomy.WinCoins", "remove (D4/D5: drop the win-coins assertion; keep the win-state check)"),
    ("RoyalEffectsChecks.cs", "RoyalReviewAudioCapture", "remove (Juice/RoyalReviewAudioCapture.cs is not imported: drop the `capture` field, its Begin() call and every later use of `capture`)"),
    ("ReferenceArcherChecks.cs", "RoyalTitleScreen", "remove (title-screen wait)"),
    ("ReferenceHammerChecks.cs", "RoyalTitleScreen", "remove (title-screen wait)"),
    ("ReferenceCannonChecks.cs", "RoyalTitleScreen", "remove (title-screen wait)"),
    ("ReferenceCannonChecks.cs", "Check(!RoyalThrone.Campaign", "remove (D8 assertion)"),
    ("ReferenceFallColorChecks.cs", "new int[RoyalGameplayArt.BoardColorCount]", "import (D3: sized 6)"),
    ("ReferenceFallColorChecks.cs", "RoyalCampaignItem", "remove (D8: the campaign-item fixture case)"),
    ("ReferenceRfv387Checks.cs", "campaign=", "remove (D8: drop `campaign=...` from the diagnostic string)"),
    ("ReferenceRfv387Checks.cs", "ReferenceRfv387FuseImporter", "remove (the \"import-fuse\" command)"),
]

COMMENT_RESOLUTION = "remove (stale comment)"

# --------------------------------------------------------------------------- scanning
COMPILED = [(k, re.compile(rx), res, ok) for k, rx, res, ok in RULES]


def code_part(line):
    """The line without its // comment. Good enough for this code base: none of
    the scanned strings contain //."""
    i = line.find("//")
    return line if i < 0 else line[:i]


def resolve(path, line, key, default):
    if path in NOT_IMPORTED:
        return "remove (file not imported: %s)" % NOT_IMPORTED[path]
    for suffix, needle, res in OVERRIDES:
        if not res:
            continue
        if path.endswith(suffix) and (needle == "" or needle in line):
            # An empty needle is a whole-file default; only apply it to rules
            # that file actually owns (campaign art, music, campaign targeting).
            if needle == "" and key not in ("campaign", "music", "colorcount"):
                continue
            return res
    return default


def scan_text(path, text):
    """Yield (path, lineno, kind, key, resolution, allowed, line)."""
    for n, raw in enumerate(text.splitlines(), 1):
        line = raw.rstrip()
        code = code_part(line)
        hit = None
        for key, rx, res, ok in COMPILED:
            if rx.search(code):
                hit = (key, res, ok, "code")
                break
        if hit is None:
            for key, rx, res, ok in COMPILED:
                if key == "design" or ok:
                    continue  # Design/ in comments names reference material; allowed names are not couplings
                if rx.search(line):
                    hit = (key, COMMENT_RESOLUTION, True, "comment")
                    break
        if hit is None:
            continue
        key, res, ok, kind = hit
        if kind == "code":
            default = res
            res = resolve(path, line, key, res)
            ok = res.startswith(("import", "guard-off", "retarget:AvatarEvents.Tool")) or (ok and res == default)
        if path in NOT_IMPORTED:
            ok = False
        yield (path, n, kind, key, res, ok, line.strip())


def git(*args):
    return subprocess.run(["git", "-C", ROOT] + list(args), check=True,
                          capture_output=True).stdout


def in_scope(path):
    if not path.endswith(".cs"):
        return False
    return path in IMPORT_FILES or any(path.startswith(d) for d in IMPORT_DIRS)


def files_at_ref(ref):
    out = git("ls-tree", "-r", "--name-only", ref, "--", *IMPORT_DIRS, *IMPORT_FILES)
    return sorted(p for p in out.decode().splitlines() if in_scope(p))


def read_at_ref(ref, paths):
    """Read many blobs through one `git cat-file --batch`."""
    req = "".join("%s:%s\n" % (ref, p) for p in paths).encode()
    out = subprocess.run(["git", "-C", ROOT, "cat-file", "--batch"], input=req,
                         check=True, capture_output=True).stdout
    texts, pos = {}, 0
    for p in paths:
        nl = out.index(b"\n", pos)
        header = out[pos:nl].split()
        if len(header) < 3 or header[1] != b"blob":
            pos = nl + 1
            continue
        size = int(header[2])
        texts[p] = out[nl + 1:nl + 1 + size].decode("utf-8", "replace")
        pos = nl + 1 + size + 1
    return texts


def files_in_worktree():
    found = []
    for d in IMPORT_DIRS:
        for base, _, names in os.walk(os.path.join(ROOT, d)):
            for n in names:
                p = os.path.relpath(os.path.join(base, n), ROOT).replace(os.sep, "/")
                if in_scope(p):
                    found.append(p)
    found += [p for p in IMPORT_FILES if os.path.isfile(os.path.join(ROOT, p))]
    return sorted(set(found))


# --------------------------------------------------------------------------- output
def md_escape(s):
    return s.replace("|", "\\|").replace("`", "'")


def emit_md(hits):
    by_file = {}
    for h in hits:
        by_file.setdefault(h[0], []).append(h)
    imported = [f for f in by_file if f not in NOT_IMPORTED]
    dropped = [f for f in by_file if f in NOT_IMPORTED]
    print("### Code hits in imported files\n")
    for f in imported:
        rows = [h for h in by_file[f] if h[2] == "code"]
        if not rows:
            continue
        print("#### `%s`\n" % f)
        print("| Line | Code | Resolution |")
        print("|---:|---|---|")
        for _, n, _, _, res, _, line in rows:
            code = line if len(line) <= 160 else line[:157] + "..."
            print("| %d | `%s` | %s |" % (n, md_escape(code), md_escape(res)))
        print()
    print("### Comment-only hits in imported files\n")
    print("Advisory: these compile either way. Resolution for all: %s.\n" % COMMENT_RESOLUTION)
    print("| File:line | Comment |")
    print("|---|---|")
    for f in imported:
        for _, n, kind, _, _, _, line in by_file[f]:
            if kind == "comment":
                text = line if len(line) <= 140 else line[:137] + "..."
                print("| `%s:%d` | %s |" % (f, n, md_escape(text)))
    print()
    print("### Hits in files the port does not import\n")
    print("Resolution for every line below: remove (the file is not imported).\n")
    for f in dropped:
        rows = by_file[f]
        print("<details><summary><code>%s</code>: %d hits. %s</summary>\n" % (f, len(rows), NOT_IMPORTED[f]))
        print("| Line | Code |")
        print("|---:|---|")
        for _, n, _, _, _, _, line in rows:
            code = line if len(line) <= 160 else line[:157] + "..."
            print("| %d | `%s` |" % (n, md_escape(code)))
        print("\n</details>\n")


def emit_tsv(hits):
    for path, n, kind, key, res, ok, line in hits:
        print("\t".join([path, str(n), kind, key, res, "allowed" if ok else "cut", line]))


# --------------------------------------------------------------------------- self-test
def self_test():
    sample = "\n".join([
        "if(RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item)) return;",
        "// RoyalWinSequence plays here",
        "RoyalThrone.Characters.KingVanguardActivityEvents.NotifyMatchResolved(this, matchedPieces);",
        "#if UNITY_EDITOR && !ROYAL_DISABLE_QA",
        "var x = 1;",
    ])
    hits = list(scan_text(JG + "LevelManager.cs", sample))
    assert [h[1] for h in hits] == [1, 2, 3, 4], hits
    assert hits[0][3] == "campaign" and hits[0][4].startswith("remove") and not hits[0][5], hits[0]
    assert hits[1][2] == "comment" and hits[1][5], hits[1]
    assert hits[2][4] == "retarget:AvatarEvents.Matched", hits[2]
    assert hits[3][5], hits[3]
    dropped = list(scan_text(JG + "Juice/RoyalWinSequence.cs", "RoyalWinSequence.Reward(a,b,c);"))
    assert dropped[0][4].startswith("remove (file not imported") and not dropped[0][5], dropped
    print("self-test: OK")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--ref", default=DEFAULT_REF, help="git ref to scan (default %(default)s)")
    ap.add_argument("--worktree", action="store_true", help="scan the imported files on disk instead of a ref")
    ap.add_argument("--format", choices=("tsv", "md", "summary"), default="tsv")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args()
    if a.self_test:
        self_test()
        return 0
    if a.worktree:
        paths = files_in_worktree()
        texts = {p: open(os.path.join(ROOT, p), encoding="utf-8", errors="replace").read() for p in paths}
    else:
        paths = files_at_ref(a.ref)
        texts = read_at_ref(a.ref, paths)
    hits = [h for p in paths if p in texts for h in scan_text(p, texts[p])]
    if a.format == "md":
        emit_md(hits)
    elif a.format == "tsv":
        emit_tsv(hits)
    code = [h for h in hits if h[2] == "code"]
    cut = [h for h in code if not h[5]]
    summary = "%d files scanned; %d code hits (%d to cut, %d kept as import); %d comment hits" % (
        len(texts), len(code), len(cut), len(code) - len(cut), len(hits) - len(code))
    print(summary, file=sys.stderr if a.format != "summary" else sys.stdout)
    if a.worktree:
        present = [p for p in NOT_IMPORTED if os.path.isfile(os.path.join(ROOT, p))]
        for p in present:
            print("NOT-IMPORTED FILE PRESENT: %s (%s)" % (p, NOT_IMPORTED[p]))
        for h in cut:
            if h[0] not in NOT_IMPORTED:
                print("LEFT: %s:%d  %s  -> %s" % (h[0], h[1], h[6][:120], h[4]))
        return 1 if (present or [h for h in cut if h[0] not in NOT_IMPORTED]) else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
