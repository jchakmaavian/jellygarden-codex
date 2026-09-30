# Leaderboard page

Built 2026-09-29 from the user's reference (`reference.webp`) in the level-end cards' style (the So Close card: royal blue,
polished gold, warm cream). `reference-before-after.webp` shows the reference, the old Tournaments panel and the new page.

## Where it lives

- `Assets/JellyGarden/Scripts/GUI/RoyalLeaderboardPage.cs` builds the page at runtime, from `RoyalCardKit` shapes. It
  attaches itself to `RoyalHomeScreen`, the same way `RoyalSettingsPage` does. No scene or prefab was edited.
- It shows while Home's `"leagues"` panel is open. The second navigation tab opens that panel; Back, Home and the other
  tabs close it. Its canvas sorts at 15: over Home (10), under the navigation bar (20) and the popups (30). The bar stays
  live, with the Leaderboard tab selected.
- `ReferenceHomeLayout` names that tab "Leaderboard" (it was "Tournaments").
- The one raster: `Assets/JellyGarden/Resources/RoyalLeaderboard/king-venguard-excited.webp`. It is King Venguard's approved
  "excited" pose (`Design/Characters/KingVenguard/V1/Design/cutouts/pose_3_excited.png`), unchanged, cropped to its content
  and resized.

## Layout

The page uses an 800-unit column in the space between the safe area's top and the navigation bar. It scales to the
smaller of the screen's width / 800 and that height / 1210. A phone therefore gets the full width, and a tablet keeps the
phone's column, centred (about 66% of the width on 1536 x 2048 and 2048 x 2732).

From the column's top, in units:

| Row | Top | Height | Notes |
|---|---|---|---|
| Header | 0 | 180 | Blue band, 6-unit gold line and bronze edge, soft shadow; "Leaderboard" at 72 in the Display ink (the So Close title's). |
| Tabs | 206 | 104 | Recessed blue bar with three equal tabs, each 236 x 88. The chosen tab is bright blue in a gold rim; the others are darker blue with a quieter label (36). |
| Friend tabs | 332 | 84 | Friends only. Two equal tabs, each 358 x 70. The chosen one is royal purple in a lilac rim (labels 32). |
| Card | 444 (336 on Players and Teams) | fills, 560 to 980 | The cream panel with a thin gold border, radius 40. The King stands on a soft floor shadow over faint gold rays. A separator, then the message (40, navy). |
| Connect | card + 30 | 116 | Full-width blue pill with a gold rim (the level-end button), caption 46, gloss sweep. It is 34 units above the bar. |

The King holds up a phone drawn in UI shapes: a gold rim, a navy bezel, and a blue screen showing the game's crown and
signal bars. The phone's edges cover his fingertips, and it is tilted 4 degrees to follow his hands.

## Behaviour

- **Tabs:** Friends, Players and Teams switch the tab. Friends List and Add Friends switch the view under Friends.
  Opening the page starts on Friends / Friends List.
- **No data yet:** the game is offline (`RoyalOfflineMode.ServicesEnabled` is false), so every tab shows the connect card.
  Only the message's second line changes: "to save your progress!", "to find your friends!", "to see the top players!" or
  "to see the top teams!".
- **Connect:** it raises `RoyalLeaderboardPage.ConnectRequested`, the one attachment point for a social service. With
  nothing attached, it calls `FacebookManager.CallFBLogin` only in a build that defines `FACEBOOK` and enables services.
  Otherwise its caption reads "Coming soon" for 1.6 s.
- **No Facebook logo:** the button has none, because Facebook is not integrated (no SDK, no `FACEBOOK` define).
- **Entrance and press:** the page fades in over 0.2 s as the Settings page does. `RoyalCardMotion` gives the press
  feedback and the gloss.

## The King's texture

| | |
|---|---|
| Source | `pose_3_excited.png`, 589 x 1000 RGBA. Its content is 581 x 988. |
| Asset | WebP, lossy quality 92, alpha exact (method 6), 494 x 840, 94,758 bytes. PSNR 36.2 dB against the resized source, over opaque pixels. |
| Importer | `RoyalWebPImporter`: pixelsPerUnit 160, bleedTransparentEdges on, astcOnMobile on. |
| Imported | 494 x 840. ASTC 6x6 on Android and iOS (BC3 on desktop). No mipmaps, bilinear, clamp. |
| Memory | 185,920 bytes of texture data on a phone (83 x 140 blocks). The editor's profiler reports 372,768, because it counts the editor's own copy. |
| Largest display | 482 x 819 px at 1170 x 2532, so the texture is about 1:1 at its largest. |

## Checks

`checks.txt` is the Play Mode run in the QA profile. It passed with no failures and no console errors, and the real save
was unchanged.

- **Sizes:** 720 x 1612, 1080 x 1920, 1080 x 2400, 1170 x 2532, 1440 x 2560, 1536 x 2048 and 2048 x 2732, captured in
  `sizes.webp`.
- **At every size:**
  - the rows are in order;
  - Connect is outside and below the card (30 units), and 34 units above the bar;
  - the tabs are equal;
  - every text fits its owner and is at least 24 px tall;
  - the King keeps his art's aspect inside the card;
  - a tablet's card stays under 80% of the width, centred;
  - the bar's buttons are inside the safe area;
  - tab 1 reads "Leaderboard" and is selected.
- **States:** tabs, views, the "Coming soon" Connect, and each way out (Home tab, Team tab, Back). See
  `states-1080x1920.webp`.

The driver is `lb_test.cs` in session 29b64c8a's scratchpad. Sizes come from a temporary "LB temp" Game view preset; it
is removed afterwards and the list compared with the one before the run.

## Open

- **Facebook wording:** the message says "Connect with Facebook" as the spec asked, but no Facebook service exists.
  Changing it, or wiring a real service to `ConnectRequested`, is the user's call.
- **Illustration:** the phone is drawn, not painted into the King's hands. A painted "King with a phone" needs a Magnific
  generation, about 60 credits, which is most of what was left on 2026-09-29.
- **No computer:** the reference's computer and desk were left out. A flat drawing beside the 3D-rendered King would clash.
- **Not yet tested:** a notched or punch-hole safe area, and a physical phone.
