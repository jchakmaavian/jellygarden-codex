The six particle systems are created at runtime by RocketVFXPlayer.Build(), not stored as
assets.

  Contact rings          ring flash at each cell the nozzle reaches
  Launch and contact light   warm flash at launch and at each contact
  Impact stars           white/gold burst stars at launch, contact and exit
  Gold embers            yellow embers that carry gold past the first cell
  Exhaust star sparkles  the four-point stars along the plume
  White smoke tail       soft wisps that dissolve the final cells of the plume

They are built in code on purpose: emission is spatial (one sample every 0.075 cells of travel)
and every lifetime is derived from the live rocket speed, so the plume keeps its cell length at
any speed and any frame rate. A serialized ParticleSystem asset cannot express that, and editing
one by hand would silently break the length guarantee.

Tune them through the Inspector on the prefab (Star Density, Smoke Amount, Flame Intensity,
Exhaust Length In Cells) or through a Profiles/ settings asset.
