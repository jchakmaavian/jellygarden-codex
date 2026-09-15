# Rocket exhaust inspection — 15 September 2026

The supplied crop shows compact gold flame at the nozzle, a brilliant white middle with four-point stars, then soft white light fading into transparency, over approximately five gem pitches.

The live rocket is built once in RoyalPowerupDirector from EffectsRig.prefab into a 32-projectile pool. It uses cached half-sprite views of the original horizontal/vertical rocket textures. There is no separate RocketLineClear prefab. Each head has a core TrailRenderer, broad glow TrailRenderer, custom flame mesh and LineRenderer. Four fixed ParticleSystems supply launch/contact rings, lights, stars and embers. All use the custom unlit RoyalThrone/RocketFire shader. The ribbon uses premultiplied blending; the light layers are additive.

The previous ribbon stays anchored at launch and uses tiled UVs without longitudinal colour or opacity changes. It grows for the entire flight, and old sections receive new spark emissions. Core/glow times are fixed at .32/.70 seconds regardless of measured rocket speed. Those are the causes of excessive length and the uniform orange line.

Movement uses actual grid-axis pitch, .22 seconds anticipation, and constant speed derived from the existing .8–1.2 second board sweep. Each nose crossing runs the existing local impact, .055 second contact delay, then destruction service. Camera-derived endpoints already carry both heads beyond the viewport. Those paths will remain unchanged.

The runtime GameplayVFX container is unmasked, on the Default sorting layer. The overlay gameplay HUD and UI popup sorting remain above it. No prefab, art, mask, canvas, sorting, UI, level, rules, other-booster or break-service changes are needed.

Change plan: bounded five-pitch plume, normalized distance UVs, speed-derived core/glow/particle lifetimes and spatial emission; white smoke plus dedicated exhaust stars in fixed pools. Compare 4.5 / 5 / 5.5 at .5× and 1× through the existing disposable Lab regression.
