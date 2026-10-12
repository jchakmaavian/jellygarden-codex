# Approved gold club coin

The supplied three-view gold club coin is the approved design. Sixteen existing coin textures were replaced in place: nine CoinItem pose/variant frames, the imported homepage coin, and six existing home/campaign/reward coin textures. See `installation.json` for exact paths, original hashes, installed hashes, metadata hashes and the timestamped checkpoint.

The built-in image generation tool produced `Source/Generated-views.png` using `Source/Approved-reference.png`. Prompt: preserve the exact approved all-gold club emblem, broad bevel and smooth gold material; render the same rigid coin at front, 10°, 29°, 47°, 64°, 82° and edge views, with consistent lighting, on a transparent sprite sheet. The generated preview matte was removed during packaging. `Frames/` contains the transparent source poses; `Prepared/` retains the fitted production textures.

Only texture pixels are installed. Existing canvas dimensions, filenames, GUIDs, pivots, sprite rectangles, pixels-per-unit and importer settings are retained. No production scripts, animation clips, VFX, materials, audio, prefabs or scene files are edited. The warm/motion variants retain their existing filenames and canvas placement. Existing users of these assets automatically see the new art.

The current imported board CoinItem collects through existing gameplay and sound. Its source code explicitly states that coin collection particles and target-flight timing were not recovered. This change does not add that missing effect or introduce a new coin animation. The replacement pose frames and reward textures are available to existing/future consumers through the original references.

`prepare.cjs` packages the generated poses into the original canvas footprints. `install.cjs` checkpoints then replaces only the manifest-listed texture files. Do not rerun installation merely to import in Unity; Unity refresh is sufficient.

The temporary editor checker is retained here as a text source, not installed as production code. Its results and real-game screenshots are stored beside this document.
