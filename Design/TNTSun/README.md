# TNT sun emblem

Replaced the TNT lettering with the Royal Throne sun from the supplied image. The shipped emblem uses the supplied artwork directly, cut through a native Unity mesh and rendered to a transparent sprite. Image-generator cutout attempts were rejected because they contained a painted checkerboard; neither generated output is used.

Updated three texture assets in place, keeping their GUIDs:
- Items-SpecialItems-TntItem-Animation-tnt_yazi__22066.png: animated sun badge.
- Items-SpecialItems-TntItem-Combo-kemer__21603.png: combo band using the existing blank gold band and sun.
- Items-SpecialItems-TntItem-tnt__16602.png: static TNT baked from the existing rig with its new badge, used during activation and in menus.

RoyalPieceSkin routes the TNT menu/shop/tutorial icon lookup to the same updated sprite. Original art remains in the timestamped checkpoint. Historical inactive prototypes are retained; active TNT board, combo and icon paths are updated.

Animation clips/controllers, prefab transforms, VFX, sound, gameplay logic and timing were not edited. Hash verification is in animation-vfx-preserved.json.

Focused Unity Play Mode checks passed: shared board/menu art, native animated badge, activation, pooled reuse and normal save restoration. Reveal/idle/activation/explosion screenshots were captured. Zero tracked errors. Static compilation passed with existing warnings. No full-game QA or APK build.

The TNT body retains its existing native artwork/resolution so the assembled animation and static activation appearance match. Only its front insignia changes.
