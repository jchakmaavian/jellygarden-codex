# Color Ball link pass

Reference comparison: the Royal Match links remain connected through contact, have a narrow white centre with gold surround, and briefly illuminate the full target silhouette. The v4 live links looked like plain white wires, and the target outline was too faint.

This pass changes only Color Ball beam/target presentation:

- Separate additive white core and soft gold surround, with a travelling brightness pulse and tiny pooled sparkles along the connection.
- Beam arrives in the existing 0.12 seconds, holds at contact for 0.13 seconds, then fades smoothly over 0.15 seconds. Width remains stable during fade instead of collapsing to a hairline.
- Stronger white/gold contour following the original target sprite, a compact warm flash and a larger four-point contact star. No additional expanding bubble.
- Same fixed 16-beam / 192-light pool; beam parameters initialize in pool setup and reset per launch.

Reveal, Idle, charged disc and finish are unchanged from v4. Target selection, conversion timing, Rocket creation, queue/handoff code, Rocket effects, TNT, UI and levels are unchanged. The effect tail ends before the existing finish completes.

The reference contact sheet and live half-speed/normal-speed clips support visual comparison. `checks.txt` records the final focused Play Mode result, including exact target arrivals, deferred Rocket launch, cleanup/reuse, input restoration and normal-save restoration. Screenshot recording can introduce frame-time spikes; it is not a device performance benchmark.

`ColorBall-Link-Polish.zip` contains the installed sources and metadata on top of v4. `package_review.py` verifies the approved source-frame and protected Rocket/TNT hashes before packaging the live captures.
