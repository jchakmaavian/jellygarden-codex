# Royal Throne area chest opening

Integrated into `RoyalAreaView.ChestCeremony`, the area-completion reward flow shown in the supplied screenshot.

The purple and gold opening sheet was generated from the existing native `ReferenceGameplay/Home/16593` chest. These are new rendered poses, not recovered APK animation frames. The closed, quarter-open, partly-open and fully-open poses share one transparent atlas; Unity creates and releases four sprites at runtime. The existing box in the task/progress card remains the native sprite.

Opening timeline uses unscaled time: 0–0.30 s anticipation shake, lid opening 0.30–0.66 s, gold flash/sparkles and confetti at 0.34 s, rewards rise at 0.52–1.02 s, Tap to Continue at 1.40 s. The open lid and soft turning gold rays remain until Continue. Motion off shows the open pose without shake, reward travel or confetti.

Economy remains in the existing `RoyalAreas.CollectChest` call. Early taps are consumed during the opening; Continue flies the rewards to the existing counters and resumes Area Completed and the restored-area showcase. No reward amounts, booster rules, catalogue costs, saves or background music were changed.

`RoyalAreaChestChecks.Run()` verifies the actual shipping flow with target frame rates 15/30/60, Motion on/off and timeScale zero, duplicate collection refusal, repeated taps, Continue and restoration of the real player profile. `RoyalAreaChecks.Run()` verifies the complete area build/collection/unlock/showcase loop. See the check logs for results.

The PNGs and GIF are captured from the live Unity Game view at 720x1280. GIF is a sampled review recording of the opening, with holds added on the closed and open states. Android/device performance is not measured by these Editor checks.
