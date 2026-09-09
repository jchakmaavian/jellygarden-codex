# Areas 2–25: warm royal artwork

The new environments follow the approved Area 1 gate: bright cream sandstone,
honey oak, royal blue and gold, rounded 3D materials, warm sunlight and clear
depth. Every area keeps one camera and one layout throughout its repairs.

Open [the comparison gallery](index.html) to choose an area, compare its old
artwork, and step through the new repair images. **Play repairs** previews the
sequence with gentle crossfades; it stops at the last available stage. The gallery
works directly from disk.
The ready count reports the images currently saved; unfinished stages are
identified in the gallery rather than substituted with another image.

The complete image set contains **24 areas, 158 repairs and 182 state images**.
The task order comes directly from the current `Resources/Story/arc1.json`.

- [Inventory](inventory.json): area names, objectives, task order and original
  asset hashes.
- [Art direction](art-direction.json): each room's layout and the precise change
  required for each repair.
- `v1/area-NNN/state-NN.png`: full-resolution generated stage images.
- `v1/area-NNN/state-NN.json`: exact prompt, reference and generation source for
  that image. All artwork uses the built-in `image_gen` tool.
- `before/`: copies of the earlier area artwork for comparison.

Refresh the static gallery after generating more images:

```sh
python3 art-review/areas-002-025/build-gallery.py
python3 art-review/areas-002-025/validate-assets.py --require-complete
```

The generated files in this review directory are the source artwork. New video
clips are not part of this image set; the game's existing repair effects can
reveal the individual stages.
