# Falling and Color Ball scope

Source inspected: `Design/Backup/apk/royal_thron_rfv387.apk`, exact packaged
`assets/bin/Data/Managed/Metadata/global-metadata.dat`. The companion
`colorball-fall-metadata.json` records recovered field names, literal defaults
and method names. It is not decompiled editable gameplay source.

The source exposes `FallComponent`, `FallSquash.BounceItemAfterFall`, separate
Lightball default/Rocket/TNT/Propeller strategies, `FindMostCommonItem`,
`ShakeItem`, `CreateItemBg`, `SingleRayReached` and `AllRaysComplete`.
Fall constants include InitialSpeed 4, MaxSpeed 20 and acceleration 0.5, but
their runtime units/update scaling are not recovered. Existing game fall
speed/acceleration settings are therefore retained.

## Reconstructed fixes

- Render-frame falling uses elapsed-time analytic capped acceleration. The
  half-world-unit snap threshold and extrapolation past the target are removed.
- Existing normal-gem landing shape is retained. Metal boosters settle by
  small position movement; their root scale is not squashed. New falls safely
  take over from unfinished landing motion.
- Direct Color Ball activation counts living ordinary eligible gems. Boosters
  retaining an old colour, ingredients and special falling coin/egg objects
  cannot skew that count. Normal-gem swaps retain their explicit gem colour.
  The existing colour tie order is retained; the native tie breaker is unknown.
- Ordinary gems receive their existing beam/contact event, then stay charged
  with a small continuous translation and the existing outline shader. After
  all contacts complete, they begin clearing together at the source finish.
  This implements the user's requested staging using recovered callback roles;
  it is **not claimed as a measured exact native disappearance timestamp**.
- Color Ball + Propeller now uses the existing progressive per-target
  conversion path and hands generated Propellers to their normal activation
  system. Metadata supports per-ray replacement, not a carried Color Ball.
  Precise native combo scheduling remains unverified.
- Color Ball + TNT uses the same progressive conversion ownership with the
  recovered `TntLightballCreationAnimation`. The final TNT settles before
  handoff if its clip outlasts the existing source finish. This is connected
  native animation with reconstructed scheduling, not a measured native timing claim.
- Beam arrivals are latched per logical target before visual pool retirement.
  A frame spanning both arrival and tail retirement cannot lose its hit when
  the visual slot is immediately reused. Beam rendering is unchanged.
- Native piece visuals stop returning objects to a newly created pool during
  application/Play Mode teardown.

No approved Color Ball body, core animation, aura or beam asset/shader changed.
The beam pool's callback lifecycle changed; its drawing code did not.
No new imported reference art was required for these logic fixes.

## Reference footage limitation

The supplied J: Color Ball reference folder is unavailable at the bounded
supplied/previously used spellings. Existing `journey_010` through `journey_013`
official same-version recordings were surveyed in 12-second contact sheets:
Levels 4–5 are visible, but no Color Ball board activation was found in the
sampled frames. This is not proof that the recordings contain no such action.
They are official version 38174 recordings, not execution of the merged APK.

`ReferenceFallColorChecks.Run()` is a focused live-game coroutine for the parent
harness's isolated QA session. It covers short/long/diagonal falls, rigid
boosters, eligible-colour targeting, ordinary swaps, charged group clearing,
one hit per target, input recovery and cancellation. Results must be read from
`../Validation/fall-color.txt` after it runs; source compilation alone is not
gameplay validation. Requested frame caps are not proof of achieved frame rate.
