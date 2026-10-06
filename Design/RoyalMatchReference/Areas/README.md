# Areas: reference match evidence (2026-09-28)

- `REFERENCE.md`: what the real Royal Match Areas do, with the timings measured from the recordings.
- `GAPS.md`: the gap list (reference, ours before, fix, acceptance test, final status), missing art, not observed.
- `RESULTS.md`: the check-suite results, the timing comparison, the real-save comparison and the phone test.
- `tests/`: every suite's own result file and the drivers' logs.
- `compare/`: the reference and ours side by side.
  - `build_ordinary.jpg`, `view_replay.jpg`: sampled at the same times from the tap.
  - `last_task.jpg`: ours re-timed onto the reference sheet's clock.
  - `states_1.jpg` and `states_2.jpg`: fifteen states paired.
- `ours_before/`: our Areas before this work (the committed code, 720x1612, QA profile): stills and a frame sheet of one build.
- `ours_after/`: the same after it.
  - Stills and frame sheets of an ordinary build, the last task, the chest, the banner and View.
  - `suite_*`: the check suite's shots.
  - `guide_*`: the first-visit guide.
  - `other_aspects/`: 16:9 and tablet.
- `device/`: our build on the phone.
- `reference/`: the reference.
  - `recordings/`: normal-speed video of the reference.
    - `phone_01_tasklist_tooltip_closes.mp4`: the list opening, the chest tooltip, and closing by X, Back and a tap outside.
    - `phone_02_areas_tab_scroll_view_replay.mp4`: the Areas tab list and View's replay of the Library. The frames at
      93.4-93.9 s were cut: the tab switch slides past the Leaderboard, which shows other players' names.
    - `emulator_03_first_task_build_castle.mp4` and `emulator_04_task3_build_gazebos.mp4`: ordinary builds. They are excerpts
      of this morning's `../recordings/journey_005` and `journey_012` (ARM translation, slower than a phone).
  - `screens/`: `p*` are phone stills and `e*` emulator stills, named by state. The Leaderboard page is excluded.
  - `frames/`: frame-by-frame sheets, with the time labelled on each frame.
    - `b1*` and `b3*`: the emulator builds.
    - `pb*`, `pstar*` and `ctr_sheet`: the phone's last-task build and its star counter.
    - `open1` and `close1`: the list opening and closing.
    - `unl_sheet0`: Unlock.
    - `view1_sheet0`: the View replay.
    - `na2_sheet0`: New Area into Area 6.

Only the text files here are committed. The images and videos stay on disk, like `../recordings/`: the reference media are
Royal Match's, and the rest are large.

**Lost recordings.** At 14:29 the external D: drive dropped off with a disk error. It held the first raw phone recordings:
- the first list and tooltip;
- the closes;
- the first Areas tab visit;
- the last-task build, chest, card pack, "Area Completed!" and New Area / Unlock (12:48-12:55).

The phone copies had already been deleted after the pull. The build and completion sequence cannot be re-recorded without
another spend. Its stills (`p14`-`p24`) and its frame sheets (`pb*`, `pstar*`, `ctr_sheet`, `unl_sheet0`, `na2_sheet0`) were on
C: and are kept here. The list, closes, tooltip and Areas tab videos were recorded again at 14:48-14:54 with nothing spent.
