# Unity validation — 13 September 2026

**85 checks passed** in Unity 6000.5.10f1 Play Mode. The generated details are in `Unity-review/checks.txt`.

Verified: resource imports; three/four/five-line formations; an isolated 2×2 formation and its surviving helicopter piece; T-shaped TNT creation; four single power-ups; all ten unordered power-up combinations; TNT chaining into a rocket; pause holding an impact; invalid swaps returning without spending a move; one move per power-up tap; gravity/refill/cascades finishing; square ownership; audio and projectile caps; preservation of coins/lives; no runtime errors during the suite.

The test uses a temporary 8×8 board and restores the player's original level afterwards. It is not a difficulty or complete blocker-rule audit. Formation tests inspect both the detector and the resulting piece; combination tests call the shared impact code, and tap/refill checks run the live resolution coroutine.

The recording is 21.92 seconds of Unity's Game view and AudioListener output. It captures about ten distinct frames/sec, packaged in a 30 fps video container. Fast effects look smoother in live Play Mode. The board is deliberately reseeded between cases; the abrupt resets and large test goal are review instrumentation. Audio capture measured -7.8 dBFS peak and -28.4 dBFS mean, with no clipped samples indicated by the peak measurement.

The capture is stopped before the stress measurement. Editor-update p95 in the short repeated-effect check was 5.94 ms. This is not a GPU measurement, a representative long-session frame-time benchmark, or a mobile performance claim.

All 37 new resource files have `.meta` files. Their combined source size is 3,033,901 bytes; the per-file manifest records dimensions, hashes and sizes. This is not installed player size. Review media lives outside Assets and does not ship in the game.

The locked logo hash remains `7397cba6758c49fc1cc5bf13f64919edc3a33bb97d0515c460623575b298ea22`. No original-project files or mobile export settings were changed.

After the successful Play Mode run, a missing label on the newly skinned Play button was corrected. The final runtime sources were compiled separately using this Unity editor's compiler, defines and references, with zero errors, without interrupting the user's ongoing Play session. Reload Play Mode to load that final presentation adjustment. It does not change the tested board rules or VFX paths.

Remaining review: subjective sound balance and motion feel together with the designer; comprehensive blocker/ingredient/timed-level behavior; mobile device memory/GPU/frame times when exports are requested; comparative playtesting before making any claim of superiority over Royal Match.
