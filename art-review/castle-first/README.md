# Castle First — implemented home screen

Unity implementation of selected concept **03**, from
[the HTML concepts](../home-screen-variants/index.html).

The full-screen room now carries a compact cream wallet, its area name and
restoration count, a floating repair marker, a green level capsule, and a
rounded four-tab navigation dock. The marker and star gently animate when the
next repair is affordable. Long task names can wrap to two lines.

The repair marker opens the existing area screen without spending stars.
Level progression, replay, settings, repair purchases, and the room's existing
reveal animations keep their original destinations and economy. Ranks and
Cards keep their existing unavailable-feature toast.

The UI uses device safe-area bounds and a flexible 360-point layout. The room
stays full bleed, with a lighter home-only neglect wash.

## Preview captures

These are native Unity player captures, not HTML screenshots. Phone and tablet
sizes were tested in a macOS player. The notched phone safe area is simulated;
this is not a physical iOS device test.

- `home-360x720.png`: proportions of the chosen HTML design.
- `home-393x852.png`: tall phone.
- `home-346x641.png`: compact phone.
- `home-768x1024.png`: tablet.
- `home-safe-area.png`: simulated notch and home indicator.
- `after-first-repair.png`: updated artwork, progress, and next repair.
- `repair-marker.gif`: unscaled marker and star animation.

## Validation

- C# typecheck and Unity macOS build.
- Home, navigation, and repair screen checks through
  `JellyGarden.Scripts.Story.Editor.StoryScreenTests.RunCastleFirstHeadless`.
- Runtime screen bounds and button raycasts at all four sizes; safe-area
  layout, paused animation, settings, level popup, repair sheet, navigation,
  long area/task names, and repaired state.
- Story data, launch policy, and kit VFX integrity checks.

The broader screen suite reports five issues outside this redesign: an avatar
idle-frame expectation, two assertions about area 2's generic repair icon, and
two stale task/star totals for areas 1–5.

## Main code

- `StoryHomeScreen.cs`: layout, live labels, repair marker positioning/animation.
- `StoryNavBar.cs`: existing navigation with the new dock style.
- `StoryCastleUi.cs`: reusable floating control construction.
- `StoryCastleFrame.cs`: safe-area sizing.
- `StoryCastleGradient.cs`: lightweight gradient tint for UI geometry.
- `AreaSceneView.cs`: configurable neglect-wash strength, default unchanged.
