# Treasure Hunt authoring and builds

The accepted screenplay is [The Great Royal Treasure Hunt](../GREAT-ROYAL-TREASURE-HUNT.md). The implementation source is [campaign.json](campaign.json). Author one complete area at a time; the remaining catalogue names and goals come from the screenplay's 150-area table.

## Compile content

Run from the repository root:

```sh
python3 tools/treasure_hunt_build.py
python3 tools/treasure_hunt_build.py --check
python3 -m unittest discover -s tools -p test_treasure_hunt_build.py
```

This writes `Assets/JellyGarden/Resources/Story/TreasureHunt/{campaign,index,arc1..6,level_plan,scenes,cutscenes,themes,repair_regions}.json` and selects it in `Story/active_campaign.json`. Commit the source and generated output together. The compiler validates sequential tasks, funded star budgets, unique board assignments, move floors tied to assigned boards, dialogue, required room states, and art references. `minimumMoves` in each authored area supplies runtime allowances without changing the kit board files. It never rewrites the kit's board or target files.

Area 001 authors four native transitions: `vines`, `door`, `flags`, and `lanterns`. Vine clearing dissolves upward with falling leaves, the wooden door assembles with brass fittings, the royal flags unfurl and settle, and the lanterns light sequentially before the sun glints. Tasks 1–3 return to the task panel without dialogue. Area 1 has exactly four dialogue cards: Leo and Pip at the opening, then Pip and Freya in the final discovery.

For a task without a completion card, author both `speaker` and `onComplete` as empty strings. A spoken task needs both fields filled. The area's final `payoffScene` runs independently of its last task's completion line.

Vine anchors come from `python3 tools/vine_transition_build.py`; rerun this if states 0–1 change. `node tools/gate_transition_build.cjs` bakes the approved door and flag canvas layers into one 1024 × 512 atlas (using a dedicated Chrome debug instance on 9232). Its manifest records source-art hashes. Original room sprites and the atlas stay non-readable. The approved preview videos remain outside Assets. `instantRepair: true` remains available, but no current task uses it. All older installed repair videos were removed; no area currently loads a video clip.

The older `story_sync.py`, `cutscene_build.py`, and `/Users/avian/Desktop/story` workflow belong to the legacy campaign. They do not import this new screenplay. `validate_story.py` still checks the legacy content; use the new compiler's `--check` for Treasure Hunt.

## Run and test

Open the project in Unity 6000.5.10f1 and press Play in `main` or `game`. Treasure Hunt enters the first puzzle automatically on a fresh campaign; returning players resume at their saved gates. A first win introduces Pip, then each star pays for one repair.

Campaign selection is startup-only, so changing it requires a fresh player process or Unity domain reload. `tools/run_player.sh -legacy-story` opens the original campaign in a built player. The new campaign uses `treasure_hunt_save.json`; the original `story_save.json` is preserved. Audio preferences remain shared.

```sh
python3 tools/check_vfx_intact.py
python3 tools/check_launch_args.py
python3 tools/lint_levels.py --levels 1-4
python3 tools/make_typecheck_csproj.py
dotnet build Story.Typecheck.csproj -v q --nologo
```

Unity batch entry points:

- `JellyGarden.Scripts.Story.Editor.TreasureHuntTests.RunHeadless`: saves, reward deduplication, ordering, skip/replay/recovery, ready-area assets, and content locks.
- `JellyGarden.Scripts.Story.Editor.StoryBuild.BuildMac`: macOS development player.

Use a separate checkout/project copy for batch runs while the project's Editor is open. After building, launch exclusively through `tools/run_player.sh`, which uses the shared portrait window settings.

```sh
tools/run_player.sh
# Automated real-player review, using legal swaps and normal UI handlers:
tools/run_player.sh --exec -treasure-review -treasure-review-dir=/absolute/path/to/review
```

The review installs a separate timestamped save directory before story startup and writes PNG screenshots and `runtime-check.json`. It captures task panels, room states, and dialogue at 393 × 852 and 320 × 568, checks native transitions and the silent return to tasks 1–3, and reloads after each repair. To continue a review from an earned checkpoint, pass `-treasure-review-save=/absolute/path/to/treasure_hunt_save.json`; the source file is copied into a new isolated save directory, and the report records `startingTasks`. Use `-treasure-review-task=N` to stop after a focused task and `-treasure-review-opening` to inspect the two-card opening replay before that task. `KINGLEO_APP` can select a build in another location.

## Adding Area 002

Start from [Area 001's brief](areas/area-001.md), then add Area 002 to the authoring JSON with its new task/scene records and artwork. Board 4 now belongs to Area 001 in this campaign: allocate new unique board IDs for Area 002 rather than inheriting the old mapping. Keep first-win reward count equal to task cost. Mark the next area ready only when its required assets and playable handoff pass review.

Later keys are deliberately not awarded in Area 001. The map currently shows six empty spaces; persistent key rewards enter development at Area 025 as scheduled in the plan.
