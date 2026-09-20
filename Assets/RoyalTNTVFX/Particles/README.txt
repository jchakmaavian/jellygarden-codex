This effect uses no ParticleSystem components.

Every visible element is a quad on Prefabs/TNTExplosionActor.prefab, animated in
Scripts/TNTExplosionActor.cs through material property blocks on Shaders/TNTGlow.shader:

  ExplosionSocket/WhiteHotCore    compact white-hot ball              (_Mode 5)
  ExplosionSocket/GoldBlastGlow   gold light field                    (_Mode 8)
  ExplosionSocket/RadialRays      broad gold rays                     (_Mode 4)
  ExplosionSocket/Shockwave       broken expanding ring of light      (_Mode 1)
  ExplosionSocket/SmokeGlow       soft residual smoke/light           (_Mode 8)
  Sparkles/Star 0-27              four-point stars thrown out, falling (_Mode 2)
  CellImpacts/Cell impact 0-24    per-cell spark flashes              (_Mode 7)

Because the pieces are a fixed set of renderers, the explosion cannot leak particles: when it
ends every renderer is disabled and the pooled object deactivates.
