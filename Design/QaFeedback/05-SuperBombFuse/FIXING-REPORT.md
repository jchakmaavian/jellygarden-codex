# Feedback 5 — Super bomb fuse fire

Implemented and verified on 7 October 2026 in Unity 6000.5.10f1.

When two bombs combine, the burning fire and sparks now remain visible on the shrinking rope until the explosion.

## Cause and change

The existing imported `TntTntComboFuseParticles` emitted correctly, but its pool sorting group rendered at order 35, behind the super bomb body at order 36. Actual before captures reproduced the missing fire. The combo now requests order 37 for its fuse effect only; other effects retain the pool default.

The pool handle follows the animated fuse socket in `LateUpdate`, after Animator evaluation. This removes the previous one-frame position lag without inheriting the socket's animated scale. Attachments are cleared on stop, retirement and reuse; stale handles cannot alter a new lease. Imported particle assets, body animation, burn duration and explosion damage logic are unchanged.

## Verification

`RoyalSuperBombFuseChecks` plus the isolated QA driver passed **22 checks, 0 failures**:

- Two bombs combined through the gameplay entry point at 30 and 60 FPS.
- Fire emitted and remained above the body and attached to the moving tip throughout 48 and 96 sampled burn frames respectively.
- Pause/resume held the fuse and particle animation together.
- The super bomb exploded, and fuse emission stopped afterward.
- Cancellation cleared the fuse; pool reuse reset its socket and sorting order. A stale handle could not change a reused effect.
- Normal player balance and lives were restored after the disposable QA session.

Unity Console reported no errors. The editor returned to the normal Map state with no QA runners and no dirty scene. Physical-phone QA and an updated APK remain pending.

## Review evidence

- [Before / after](before-after.png)
- [Burn sequence](burn-sequence.png)
- [Animated Unity capture](super-bomb-fuse.gif) — 60 FPS simulation, every second frame captured; includes pause verification.
- [Check results](checks.txt)

The Excel tracker marks #5 fixed and adds its cause, implementation and verification to the Fixing Report. Totals: 4 fixed, 1 clarified duplicate, 6 open. Feedback 6–11 remains open.
