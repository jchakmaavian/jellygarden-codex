# Unity validation — 13 September 2026, polish v2

**110 gameplay checks passed** in Unity 6000.5.10f1 Play Mode after the final runtime sources compiled. **18 dialog checks passed** on the final menu layouts. Generated details are in `Unity-review/checks.txt` and `Unity-review-v2/dialog-checks.txt`.

## Gameplay coverage

Verified resource imports; immediate royal art before a piece's first rendered frame; five obstacle prefab skins with preserved sprite names and footprints; wire trapping and layered block damage; target binding to live data; three/four/five-line formations; isolated 2×2 helicopter creation; T-shaped TNT creation; four single power-ups; all ten unordered power-up combinations; TNT chaining into a rocket; pause holding an impact; invalid swaps spending no move; one move per power-up tap; gravity/refill/cascades finishing; square ownership; audio and projectile caps; preservation of coins/lives; and no runtime errors during the suite.

The regression temporarily substitutes a controlled 8×8 board and restores the original board and economy afterwards. It is not a complete blocker, ingredient, timed-level, difficulty or reward-progression audit. Formation checks inspect the detector and resulting piece; combination checks call the shared impact code; tap/refill checks run the live resolution coroutine.

The selected Level 10 also loaded with royal crystal tiles, live goals and 48 moves through the editor's entry without life cost. The real Pause button opened the new dialog, and Continue restored the same board with the same goals and 48 moves. A final pointer-drag move was blocked by automatic approval review because it could spend a move or change progress; it was not executed. User approval for that optional manual check is pending.

## Interface coverage

The review opens the real level-entry, gem, refill, booster, booster-help, settings, tutorial, win, fail and extra-moves dialogs. It checks heading/close clearance, live-goal overlap, visible control bounds and tutorial formation/result separation. All five tutorial pages were captured. The final extra-moves screen has a shorter entrance, correctly placed live goals and the configured extra-move or extra-time quantity.

No purchase callbacks or result-dismissal callbacks were invoked. Win/fail captures are layout previews with the current score, not evidence of completed levels or awarded rewards. The review preserved gems and lives. Screens were visually inspected, including the final retry, refill, settings and TNT tutorial layouts.

## Capture and audio

`Unity-review-v2/royal-throne-unity-effects.mp4` contains approximately 22.01 seconds of Unity's Game view and AudioListener output: 222 distinct captured frames, packaged in a 30 fps container. Fast effects are smoother in live Play Mode. The board is deliberately reseeded between cases; abrupt resets and the oversized test goal are instrumentation. The one-million-point test goal exceeds the normal HUD width at its opening frame. An earlier recording with an editor stall at its beginning was replaced by this final capture.

The captured stereo mix measured **−6.83 dBFS peak**, **−28.96 dBFS RMS**, and **zero full-scale samples**. This verifies captured sample levels, not subjective sound quality or true-peak compliance. The designer should review sound balance and fatigue during normal play.

The original background music is unchanged byte-for-byte, including its importer and playback settings. SHA-256: `389427fe96731cc782aecd21ad27883f3bd4d2227ff0b20aeba710cc2a61aced`.

## Performance, assets and boundaries

Capture stops before the repeated-effect stress measurement. Editor-update p95 was **6.6841 ms** in the final run. This is not a GPU measurement, long-session benchmark or mobile performance claim. Runtime voice, projectile and particle caps were checked. No runtime errors were found in the final clean-session log segment.

All 50 resource files have `.meta` files. Combined source size is **3,320,155 bytes**, including **27 WebP images totaling 497,754 bytes** (about 486 KiB). See `asset-manifest-v2.json`. These are source-file sizes, not installed player size. Review media stays outside Assets. Legacy source art remains for compatibility; this pass is not a complete purge of every inherited file.

The locked logo hash remains `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`. The boss's original project and mobile export settings were not changed. `git diff --check` passed.

Remaining review: optional approved manual pointer move; subjective motion and sound assessment with the designer; comprehensive level/rule/progression coverage; device profiling when export is requested; comparative playtesting before any claim of Royal Match parity or superiority.
