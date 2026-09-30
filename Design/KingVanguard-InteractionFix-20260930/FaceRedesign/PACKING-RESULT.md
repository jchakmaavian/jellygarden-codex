# Teeth and shadow packing result

Review outputs, not written into `Assets` by the packer:

- [King_Vanguard.png](Packed/King_Vanguard.png) — SHA-256 `7694e6d1316da214004d19c6944f27f93143160c2df0b7296c6a7aa272004753`
- [AreaCompleted_Vanguard.png](Packed/AreaCompleted_Vanguard.png) — SHA-256 `abfaac51558cce0fd972b4fc20dc344e872a433e4fcfaa7494b77c70b73152c1`
- [Original / before / packed component preview](Review/teeth-shadow-packed-comparison.png)
- [Pack manifest](teeth-shadow-pack-manifest.json)
- [Input painted workboard](TeethShadow-painted-workboard.png)

The two tooth regions use only generated RGB, including nearest generated edge color where required, with exact original source alpha. The five shadow regions use constant RGB sampled as the per-channel median of the darker half of valid generated brown core pixels. Green-contaminated candidates are excluded. No original shadow or facial RGB is sampled as a replacement color. Original technical shadow alpha, UV placement and edge softness are retained exactly; runtime attachment opacity is applied separately.

The generated RGB values are moustache `(117,57,34)`, right brow `(136,66,45)`, left brow `(136,65,45)`, beard `(121,53,33)` and crown underside `(128,64,40)`. The same colors are used in the corresponding portrait and Area regions.

Verification passed for both output PNGs:

- Zero changed pixels outside the seven original selected alpha islands.
- Zero selected-region alpha differences from the original APK source.
- Zero changed mouth-surround pixels.
- Zero PNG save/read RGBA byte differences.
- Zero changes to opaque pixels of other facial components.
- Current runtime input atlases still matched their snapshots at the end of packing.

The portrait atlas contains one previously documented shared antialias tail texel at `(415,1853)`, original alpha `2`, belonging to both the selected right-brow shadow island and the neighbouring right-brow UV island. Its new shadow RGB is recorded in the report; it is within the selected source island. There are no nonselected changes at alpha 4 or higher. Area has no such shared change.

The complete input atlas snapshots and hashes are under [Before/Atlases](../Before/Atlases). [Pack-TeethShadows.ps1](../Tools/Pack-TeethShadows.ps1) snapshots and guards those inputs, while [Pack-TeethShadows.cs](../Tools/Pack-TeethShadows.cs) performs the selective packing and verification. Re-running after the installed atlas changes intentionally fails the baseline guard until a new, explicitly reviewed baseline is selected. No binary skeleton, FFD timeline, mouth paint or other runtime file was edited by this tool.
