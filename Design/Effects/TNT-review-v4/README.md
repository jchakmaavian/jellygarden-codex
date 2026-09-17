# TNT explosion structure update

Reference: Royal-Match-TNT-2.mp4. The reference inspection identified a compact flash opening into broad irregular rays, a thick broken luminous wave, individual sparkling cell impacts, and lingering light over colored falling fragments.

Only two runtime files change: TNT3DAnimator.cs and RoyalTNTGlow.shader. Original TNT artwork, reveal/ignition animation, damage sequence, fragment physics, Rocket, HUD, levels, prefabs and serialized references remain unchanged.

- The white center develops transparent areas while the expanding gold/white wave remains visible.
- Broad rays vary in width, reach and brightness. The outer wave uses soft, irregular textured patches instead of a narrow circular outline.
- Each existing pooled cell-impact quad carries a local flash, small ring and four stars. These overlap debris for 0.55 seconds.
- Existing pooled stars have staggered timing and varied brightness, with warm soft residual light through the debris phase.
- Cleanup ends at 0.86 seconds, after the final star lifetime. No new runtime objects, materials, textures or particle pools are allocated.

The two editor check files only update the capture destination and verify that local impacts, stars and residual light overlap falling fragments. Phase captures and checks.txt document the focused 0.25x/1x replay. This is a visual interpretation using Royal Throne artwork, not a pixel-identical copy of the reference.

verified-source contains the exact changed source and original metadata. incremental.patch is relative to the verified TNT-review-v3 source snapshot. Existing unrelated working changes are not included in the backup.
