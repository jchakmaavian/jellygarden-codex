# Color Ball ownership/cancel correction proposal

Not applied to Assets yet. Unity entered Play Mode in another session during preparation.

`fall-color-ownership.patch` contains three focused changes. `Proposed/` holds the complete staged files, and `base-hashes.json` records the exact source hashes used.

- Keep the original selected item identity through beam launch and contact. A replacement item or a target converted by another action is skipped.
- Clear only still-ordinary charged targets. Only a successful conversion owned by this sequence enters its deferred payload handoff; check that item and payload again before activation.
- Keep conversion-contact pose leases on the director so cancellation restores every held scale even if Unity does not dispose nested iterators. Existing beam rendering and contact animation are unchanged.
- Run the existing direct/swap/cancel and payload cases at requested 30 and 60 FPS. Add a competing-converter state-transition injection and cancellation during the real payload contact squeeze. These are focused ownership tests, not a claim of full simultaneous-combo coverage.

Before applying, verify `Design/ReferenceGameplayImport/status.json` reports Edit Mode, no compilation/update, and confirm all three hashes still match. If a hash changed, merge only these hunks into the latest file rather than replacing it with a staged copy.

No Unity compilation, Play Mode validation or gameplay success is claimed for this proposal.
