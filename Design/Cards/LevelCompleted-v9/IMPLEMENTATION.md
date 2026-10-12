# Approved title artwork animation

The completion popup now displays the generated, user-approved Well Done! image instead of the previous font approximation. Cream faces, navy outlines, gold bevels and brown extrusion come directly from the bitmap. This is the generated recreation, not a claim of pixel identity with the original reference attachment.

The runtime PNG is byte-identical to well-done-reference-recreation.png (SHA256 6DAA3BE5FBD6F85558BEFC0DD6391E5FA29C07213AB0562F67FE055D0DFBD4BB). Unity imports its original 2170 x 725 dimensions as uncompressed RGBA32 with alpha, no mipmaps and no resizing.

RoyalWinTitleArt animates a continuous 160 x 8 mesh to keep the connected gold silhouettes intact. The title fades in over 32 ms, then a left-to-right wave grows each letter region, dips slightly and settles. The wave begins at 0.18 s, uses 0.04 s logical letter spacing and a 0.40 s envelope. It settles completely at 0.94 s with the original rectangular UV mapping and white tint. It is a continuous art wave, not the reference video's progressive individual-letter reveal. Motion off uses only the existing card fade. The disabled text remains a fallback if the resource is missing.

Validation: Unity 6000.5.10f1 compiled without errors. verify_title_art.cs completed 99 checks with zero failures at 1080 x 1920, 720 x 1280 and 1536 x 2048. Actual renderer meshes were inspected for ordered growth and undershoot, exact settled geometry/UVs, opaque crown clearance, screen bounds, reduced motion and hierarchy reuse. Continue and close routed once without awards. Normal save keys, runtime currency/lives/level and Home catalog were restored. No Unity runtime errors were recorded. Editor was left stopped in clean main.unity; the temporary editor reservation was removed.

Royal-Throne-v9-Unity-Card.mp4 is the actual Unity preview. The comparison video and six-stage sheet use the supplied reference clip for motion context and preserve image proportions. Capture contains 64 rendered frames, resampled to 30 fps; it is silent review media, not a device frame-rate benchmark. No Android build or physical-device validation was performed.

Runtime scope: RoyalCards, RoyalWinCard, new RoyalWinTitleArt and the new texture/meta. Reward logic, saves and shared button styling were not changed.
