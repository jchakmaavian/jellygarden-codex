# King Leo board assets — inventory and art plan

Date: 2026-09-08  
Status: first direction batch approved and installed in Unity (29 PNG replacements for 11 designs).

Open the [art comparison](../art-review/king-leo-board/index.html) or the [delivery notes](../art-review/king-leo-board/README.md). Approved masters, review exports, prompts, and backups are saved outside `Assets/`. The first batch is installed; the remaining asset families are still planned. See the [installation record](../art-review/king-leo-board/integration-v1/installation.json) and [running Unity capture](../art-review/king-leo-board/integration-v1/unity-installed.png).

## Intended result

A warm, welcoming royal board that belongs beside King Leo's current blue-and-gold HUD: polished storybook objects, honey-gold metal, warm wood, soft cream highlights, and readable colour families. Keep the friendly character of the king. Aim for playful palace treasures and tools rather than heavy military equipment.

This document inventories the current local assets and plans their replacement. The initial planning task produced documentation only. The subsequent art task has now created the first 11 separate candidates and an HTML comparison for the owner to inspect. The owner approved this first batch with “add in game”; those assets are now installed. Later batches follow the same review process.

## Original board inventory

Verified from the local PNGs, `Resources/Prefabs/Item.prefab`, relevant blocker prefabs, and the scripts that select or use them during planning. This section preserves the pre-installation baseline. The installation record above lists the approved replacements now in the game; historical filenames still contain candy terminology.

All paths in this section are relative to `Assets/JellyGarden/`.

### Normal match pieces — six designs

| Current file | Current appearance | Proposed King Leo art direction |
| --- | --- | --- |
| `Textures/Items/item_01.png` | Red closed book, cream page edge | Crimson royal storybook; rounded spine, one broad gold corner detail; red remains dominant |
| `Textures/Items/item_02.png` | Orange/gold round coin | Amber treasury coin with a simple embossed lion mark and a thick, readable rim |
| `Textures/Items/item_03.png` | Bright purple faceted gem | Plum amethyst with fewer, broader facets and a warm reflected highlight |
| `Textures/Items/item_04.png` | Light blue shield | Sapphire enamel shield with a restrained honey-gold rim; preserve a large blue centre |
| `Textures/Items/item_05.png` | Green leaf | Palace garden leaf with a soft sculpted curve and a single clear centre vein |
| `Textures/Items/item_06.png` | Yellow three-point crown | Sunny golden crown, chunky points, warm interior shadow; avoid tiny jewels that disappear on a phone |

Retain these six identities and their current colour order. The task is a cohesive art refresh. The orange coin and yellow crown must remain distinct in hue, silhouette, and brightness.

### Board power-ups — four designs represented by 19 files

| Current files | Current appearance and role | Proposed artwork |
| --- | --- | --- |
| `Textures/Items/item_01_stripes_horiz.png` through `item_06_stripes_horiz.png` | Horizontal wooden rocket with a steel cone; line clear | Royal firework rocket: warm wood, red band, brushed gold fittings, clear horizontal silhouette |
| `Textures/Items/item_01_stripes_vert.png` through `item_06_stripes_vert.png` | Vertical version of the same rocket; line clear | Matching vertical firework, with lighting redrawn consistently rather than simply rotating all highlights |
| `Textures/Items/item_01_extra.png` through `item_06_extra.png` | Black spherical bomb with lit fuse; area-clear `PACKAGE` type | Rounded royal powder bomb, dark plum/bronze body, broad gold band and short amber fuse |
| `Textures/Items/candy10_choco.png` | Glossy multicolour segmented ball; colour-bomb `BOMB` type | Royal prism orb with broad jewel-colour segments, a slim gold seam, and a warm white highlight |

Each six-file rocket/bomb family currently contains byte-identical pictures. There are four unique power-up designs, not nineteen concepts to commission. Keep the existing variant slots in the eventual export mapping. These power-ups already behave independently of a normal piece's colour.

### Falling collectibles — two designs

| Current file | Current appearance | Proposed artwork |
| --- | --- | --- |
| `Textures/Items/ingredient_01.png` | Blue heart locket, gold border and chain | Queen's sapphire locket: simplify the chain and crest so the heart silhouette reads at board size |
| `Textures/Items/ingredient_02.png` | Gold ring with a large red gemstone | Royal ruby signet: broad band, one large ruby, warm gold, distinct from the purple match gem |

Their HUD target pictures should come from the same approved masters. Preserve the recently approved arrangement: icon above count, targets side by side.

### Board ground, borders, and obstacles — eleven texture files

| Current file(s) | Current appearance / use | Proposed artwork |
| --- | --- | --- |
| `Textures/Blocks/square.png`, `square_2.png` | Two translucent dark cell textures | Quiet warm ivory/sand cell surfaces; subtle alternating value, minimal grain |
| `Textures/Blocks/border_01.png`, `border_02.png`, `border_03.png` | Thin yellow straight/corner border pieces | Soft honey-gold edge pieces with consistent thickness and clean joins on irregular boards |
| `Textures/Blocks/block.png` | Pale translucent frost with a purple rim | Warm-lit frosted palace glass; retain transparency so the underlying item remains identifiable |
| `Textures/Blocks/block_02.png` | Purple inset tile used for the double-block appearance | A visibly reinforced glass layer; stronger rim/crack structure that reads as a second layer |
| `Textures/Blocks/wireBlock.png` | Cyan outlined, cracked transparent enclosure | Light brass restraint with open centre; keep the trapped piece visible |
| `Textures/Blocks/solidBlock.png` | Brown wooden square/crate | Honey-oak crate, chunky slats and a restrained royal stamp |
| `Textures/Blocks/undestroyable.png` | Grey masonry square | Warm castle stone with large fitted blocks; visually heavier than breakable wood |
| `Textures/Blocks/thriving_block.png` | Chocolate square marked “Choco” | Tangled palace ivy over a dark soil base; a spreading obstacle, visibly different from the single green leaf piece |

The board ground also has a **runtime-generated fill**, selected/tinted by `StoryBoardTheme` and the arc settings in `Resources/Story/themes.json`; it is not a separate authored PNG. The future mockup must include that fill when judging new cells and obstacles. Changing a square PNG alone will not describe the full on-screen ground.

### In-level boosters — five button designs, plus shared UI

| Current file | Current appearance | Behaviour found in handlers | Proposed asset |
| --- | --- | --- | --- |
| `Textures/Boosts/boost_5_mooves.png` | Gold `+5` and double arrow on blue/gold plate | Adds five moves in move-limited play | Warm gold move arrows; number remains clearly readable |
| `Textures/Boosts/boost_5_time.png` | Gold hourglass with `+30` on blue/gold plate | Adds thirty seconds in timed play | Cream-sand royal hourglass with matching warm-gold frame |
| `Textures/Boosts/boost_bomb.png` | Black/gold bomb on blue/gold plate | `BoostType.Bomb` targets an item; completion calls that item's destruction | Royal mallet concept for the targeted action; review naming/picture consistency before integration |
| `Textures/Boosts/boost_hand.png` | Armoured gold/blue glove | `BoostType.Hand` permits a free swap | Friendly royal glove, rounded fingers and warm leather accents |
| `Textures/Boosts/boost_random_color.png` | Golden hammer on blue/gold plate | `BoostType.Random_color` regenerates nearby normal item colours | Jewelled colour wand with a small multicolour flourish; avoid suggesting a single-tile hammer action |

**Mapping issue to resolve visually:** some story comments call Hand a hammer, Bomb a 3×3 clear, and Random_color a shuffle. The handlers inspected describe the actions above. Preserve behaviour in this art pass and label the comparison with the implemented action. A desired gameplay change would be a separate decision. In particular, the board's `PACKAGE` bomb and the targeted `BoostType.Bomb` tool must not accidentally promise the same effect.

Shared files in `Textures/Boosts/`:

- `boost_nomber.png`: green circular quantity badge; plan a quieter cream/gold count badge.
- `boost_plus_button.png`: red/gold add badge; match the approved button family.
- `boost_over_button.png`: grey locked overlay; plan a desaturated bronze lock state.
- `5sec.png`: small `+5` timed pickup overlay; keep distinct from the `+30` time booster.
- `star.png`: white star silhouette used as supporting art; review its tint/context before replacing.
- `lollipop.png` and `animations_lollipop/1.png`–`4.png`: remaining candy-style animation artwork; replace with the approved tool/effect family when its consuming animation is reviewed.

For every booster, review the bare icon and its existing-size button version. Keep active, pressed, selected, unavailable, and quantity states readable. New artwork should be supplied with icon and plate separated for review, plus a composite matching the current asset's dimensions for a later compatible replacement.

### Pre-level booster icons — current mismatch

| Current story UI file | Current picture | Proposed replacement |
| --- | --- | --- |
| `Resources/Story/UI/booster_stripes.png` | Striped candies in a blue burst badge | Approved royal rocket from the board family |
| `Resources/Story/UI/booster_packages.png` | Wrapped “Boom” candies in a blue burst badge | Approved royal powder bomb from the board family |
| `Resources/Story/UI/booster_colorbomb.png` | Multicolour candy cluster in a blue burst badge | Approved prism orb from the board family |
| `Resources/Story/UI/booster_locked.png` | Grey lock plate | Matching bronze/blue locked state |

The pre-level flow uses these four story UI resources. It grants five stripes, five packages, or one colour bomb; review any quantity artwork against those existing amounts.

There are additional kit counterparts in `Textures/menu/play_menu/`, `Textures/menu/booster_shop/`, and `Textures/menu/boosts_items/`. They are included in the file inventory below as compatibility assets. Their presence does not prove that every old menu is visible in the story flow. Plan matching exports only after checking the consumer, so an unused shop plate does not take priority over visible board art.

### Targets, effects, and backdrops

- Target support: `Textures/menu/game_menu/arrow_ingredients.png` and `Textures/map/map_target_01.png`–`map_target_05.png`. Review falling-item exit arrows and generic objective pictures alongside the new collectibles. The HUD also reuses live item/block sprites; do not commission a separate conflicting set of those icons.
- Six destruction frame families in `Textures/effects/explosion_item_01/`–`explosion_item_06/`, four PNGs per family. Keep the six colour assignments, replace candy fragments with broad object-colour flecks and a small warm sparkle. Some filenames within family 01 say “purple” despite its red index; use the actual pixels and animation references when exporting.
- Other effect textures: `stipes_destroy.png`, `Bolt.png`, `Bubble.png`, `snow.png`, `Star_particle.tga`, and `emty_sprite.png`. Review as rocket streak, colour-bomb connection, bubble/splash, debris, sparkle, and blank support respectively; confirm each material/clip consumer before choosing replacement art.
- Animation/effect consumers include `Resources/Prefabs/Effects/`, `Resources/Particles/StripesEffect.prefab`, `Resources/Particles/PopupScores.prefab`, `Resources/Prefabs/5sec.prefab`, and `Resources/Prefabs/arrow_ingredients.prefab`. Their connected materials, controllers, and clips are listed in the dependency appendix.
- Six story backdrops: `Resources/Story/Art/backdrop_castle.jpg`, `backdrop_village.jpg`, `backdrop_mines.jpg`, `backdrop_border.jpg`, `backdrop_crypts.jpg`, and `backdrop_siege.jpg`. Five kit fallbacks: `Textures/backgrounds/background_01.png`–`background_05.png`.
- Backdrops are context for this pass. Retain the current scenes initially; test piece readability on all six arc settings. A warm art direction should still allow cooler environments and shadows.
- Existing source art in `tools/art/royal/`, `tools/art/flat/`, and `tools/art/pieces/` contains previous concepts and masters. Treat these as reference material, not evidence of what is currently rendered. Historical skin scripts can overwrite live assets and are outside the candidate-art workflow.

## Proposed visual style

**Form:** chunky, softly bevelled storybook objects with a slight top view and consistent scale. A clear silhouette should communicate each item before the small decoration does.

**Lighting:** broad upper-left cream highlight, warm reflected light, short soft lower-right shadow. Avoid pinprick sparkle and large baked glow that muddles adjacent pieces.

**Materials:** honey gold, amber brass, lacquered enamel, warm oak, cream paper, and jewel glass. The normal pieces remain mostly their matching colour; reserve stronger gold treatment for the crown, power-ups, and tools.

| Palette role | Proposed colour | Use |
| --- | --- | --- |
| Cream highlight | `#FFF1CD` | Soft highlights, paper, light UI detail |
| Honey gold | `#E8AE45` | Metal rims and royal accents |
| Amber shadow | `#A85C2A` | Gold/wood depth |
| Royal blue | `#184E96` | Enamel and continuity with the approved HUD |
| Warm ivory | `#E7D3AC` | Candidate board ground |
| Crimson | `#CF4148` | Book family |
| Amber orange | `#ED902D` | Coin family; darker/oranger than crown |
| Amethyst | `#A44BD0` | Gem family |
| Sapphire | `#379AD7` | Shield family |
| Leaf green | `#55AD55` | Leaf family |
| Crown yellow | `#F5C747` | Crown family |

These are starting art swatches for review, not approved game tokens. Evaluate colour and contrast in the actual board composition before locking them.

## Production sequence and approval points

### 1. Small direction batch

Create **11 candidate cutouts**: six normal pieces, horizontal rocket, vertical rocket, area bomb, prism orb, and one royal glove. The glove tests whether the board style also works for tools. Show the existing HUD above a representative board so the owner can judge the complete visual relationship.

Supply one coherent primary style. If needed, show a restrained alternative on only the book and rocket to compare softness and ornament; avoid generating the full collection in competing styles before a direction is chosen.

**Owner review:** approve or request changes to warmth, royal detail, shapes, colour separation, and small-size readability before expanding the set.

### 2. Complete static asset set

After direction approval, create the two collectibles, six obstacle-state textures, two cell textures, three border pieces, remaining tool/bonus icons, and shared button states. Derive pre-level icons and legacy menu composites from the approved board/tool masters. Resolve the mallet/wand/button mappings in the review labels before integration.

**Owner review:** inspect every asset next to its current counterpart and on sample boards. Confirm blockers communicate transparency, restraint, durability, and spreading clearly.

### 3. Effect artwork and animation previews

Create compatible replacement destruction frames, rocket streaks, orb connections, pickup indicators, and tool animation art. Preview a short loop or storyboard showing appearance, activation, and disappearance, including a dense board and a two-power-up combination. Keep current mechanics and animation timing as the reference.

**Owner review:** check that effects feel warm and celebratory while leaving the board readable.

### 4. Approved delivery package

Deliver approved masters, transparent exports, preview sheets, and an old-file → new-file mapping. Keep all candidates in a separate review folder outside `Assets/` until verified. Document rejected versions without replacing approved ones.

Only after the owner accepts the actual artwork should a separate implementation task update Unity assets and validate their in-game use.

## Planned HTML comparison — next phase

Available output: `art-review/king-leo-board/index.html`, with local images so it opens without a server. This is a review artifact, not a game screen. It currently covers the first 11 candidates; expand it as later art batches are approved and created.

1. **Current / proposed board:** two boards with the same layout, piece identities, blockers, and current HUD. Keep scale and background identical so the comparison isolates the artwork.
2. **Asset comparison cards:** existing image, proposed image, plain-language role, current filename, and candidate version. Show actual image assets; proposed slots stay explicitly “not generated” until real art exists.
3. **Size checks:** enlarged detail plus 24, 32, 48, and 64 px previews. Include the small HUD target size. The final acceptance size should come from a real phone screenshot.
4. **Context checks:** warm ivory board, current arc ground, dark background, transparency checkerboard, and grayscale. Compare the blue shield against ice and the green leaf against ivy.
5. **Power-up checks:** horizontal and vertical rockets side by side; area bomb and targeted tool clearly labelled; all pre-level icons shown beside their corresponding board object.
6. **Review checklist:** an asset ID and space for keep/change notes, with a visible candidate version. The owner's explicit review determines approval; simply viewing the page does not approve assets.

## Asset delivery requirements

- Candidate object masters: transparent RGBA PNG, preferably 1024×1024, with a consistent centre, scale, and padding. Export individual assets as well as comparison sheets.
- Base pieces, rockets, bombs, and collectibles currently use 128×128 files; preserve those export dimensions for a later compatible swap. Use the measured appendix for all other dimensions.
- Supply clean alpha with no white/grey matte, no clipped tips/fuses, and no shadow cutoffs. Keep opaque colour areas large enough to survive downscaling.
- Keep editable/source artwork where the creation tool supports it; do not imply layered source files exist for a flattened generation.
- Preserve existing filenames, sprite identities, pivots, import settings, and animation mappings during eventual integration unless a specific migration is separately planned. Item names also participate in target matching.
- Separate tiny text/numbers from the art master where practical. Supply a compatibility composite for existing buttons that currently have numerals baked in; no text-rendering changes are part of this plan.
- Normal pieces need distinct silhouettes in grayscale. Rockets must convey their axis at a glance. A power-up must stand apart from a normal piece without relying on glow alone.
- No new gameplay items are implied by this plan: no new propeller, extra booster ability, or new blocker rules.

## Review checklist

- [ ] Looks at home beside King Leo and the approved blue/gold HUD.
- [ ] Warm, friendly materials and consistent lighting across all families.
- [ ] All six normal pieces identifiable at phone size, especially coin versus crown.
- [ ] Target thumbnails match the board objects and fit above their counts.
- [ ] Rocket directions, bomb, orb, glove, and targeted/recolour tools are unambiguous.
- [ ] Board objects match pre-level and in-level booster pictures.
- [ ] Transparent blockers reveal their contents; obstacle states remain distinguishable.
- [ ] Irregular board edges join cleanly; cells stay visually quieter than pieces.
- [ ] Destruction/activation artwork matches each object and colour family.
- [ ] Owner has reviewed and accepted the candidate artwork before integration.

## Evidence and inventory scope

This is a filesystem and source inspection, not a playthrough of every level or menu. Direct item references were resolved from `Item.prefab`; the six-file power-up duplicate groups were verified by file hashes. The current appearance of core pieces, blockers, booster buttons, and story pre-level icons was inspected from the images.

Behaviour references: [Item.cs](../Assets/JellyGarden/Scripts/Item.cs), [LevelManager.cs](../Assets/JellyGarden/Scripts/LevelManager.cs), [BoostAnimation.cs](../Assets/JellyGarden/Scripts/GUI/BoostAnimation.cs), [StoryBoosters.cs](../Assets/JellyGarden/Scripts/Story/StoryBoosters.cs), [StoryBoardTheme.cs](../Assets/JellyGarden/Scripts/Story/StoryBoardTheme.cs), and [themes.json](../Assets/JellyGarden/Resources/Story/themes.json).

The appendix records board-related source images, compatibility menu assets, arc backdrops, and image dependencies reached through board/effect prefabs. General story portraits, repair scenes, navigation UI, audio, and the already-approved HUD artwork are outside the replacement inventory.

## Appendix A — measured current image files

Paths below are relative to `Assets/JellyGarden/`. Each filename links to the existing asset. Dimensions are the source image dimensions, not its displayed size. Inclusion in the menu/effect groups records an existing compatibility/dependency asset, not a claim that every image is visible in the story flow.

| Image group | Files |
| --- | ---: |
| Match pieces, power-ups, and collectibles | 27 |
| Board cells, borders, and blockers | 11 |
| In-level booster buttons, indicators, and animation frames | 15 |
| Story pre-level booster icons | 4 |
| Kit pre-level booster counterparts | 6 |
| Kit booster shop art | 11 |
| Kit booster effect icons | 3 |
| Objective support and auxiliary booster badge | 7 |
| Board effects and destruction frames | 30 |
| Story arc backdrops and kit fallback backgrounds | 11 |
| Additional images referenced by board/effect dependencies | 5 |
| **Total inventoried image files** | **130** |

### Match pieces, power-ups, and collectibles

| Current asset | Pixels |
| --- | --- |
| [Textures/Items/candy10_choco.png](<../Assets/JellyGarden/Textures/Items/candy10_choco.png>) | 128 × 128 |
| [Textures/Items/ingredient_01.png](<../Assets/JellyGarden/Textures/Items/ingredient_01.png>) | 128 × 128 |
| [Textures/Items/ingredient_02.png](<../Assets/JellyGarden/Textures/Items/ingredient_02.png>) | 128 × 128 |
| [Textures/Items/item_01.png](<../Assets/JellyGarden/Textures/Items/item_01.png>) | 128 × 128 |
| [Textures/Items/item_01_extra.png](<../Assets/JellyGarden/Textures/Items/item_01_extra.png>) | 128 × 128 |
| [Textures/Items/item_01_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_01_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_01_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_01_stripes_vert.png>) | 128 × 128 |
| [Textures/Items/item_02.png](<../Assets/JellyGarden/Textures/Items/item_02.png>) | 128 × 128 |
| [Textures/Items/item_02_extra.png](<../Assets/JellyGarden/Textures/Items/item_02_extra.png>) | 128 × 128 |
| [Textures/Items/item_02_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_02_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_02_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_02_stripes_vert.png>) | 128 × 128 |
| [Textures/Items/item_03.png](<../Assets/JellyGarden/Textures/Items/item_03.png>) | 128 × 128 |
| [Textures/Items/item_03_extra.png](<../Assets/JellyGarden/Textures/Items/item_03_extra.png>) | 128 × 128 |
| [Textures/Items/item_03_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_03_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_03_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_03_stripes_vert.png>) | 128 × 128 |
| [Textures/Items/item_04.png](<../Assets/JellyGarden/Textures/Items/item_04.png>) | 128 × 128 |
| [Textures/Items/item_04_extra.png](<../Assets/JellyGarden/Textures/Items/item_04_extra.png>) | 128 × 128 |
| [Textures/Items/item_04_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_04_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_04_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_04_stripes_vert.png>) | 128 × 128 |
| [Textures/Items/item_05.png](<../Assets/JellyGarden/Textures/Items/item_05.png>) | 128 × 128 |
| [Textures/Items/item_05_extra.png](<../Assets/JellyGarden/Textures/Items/item_05_extra.png>) | 128 × 128 |
| [Textures/Items/item_05_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_05_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_05_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_05_stripes_vert.png>) | 128 × 128 |
| [Textures/Items/item_06.png](<../Assets/JellyGarden/Textures/Items/item_06.png>) | 128 × 128 |
| [Textures/Items/item_06_extra.png](<../Assets/JellyGarden/Textures/Items/item_06_extra.png>) | 128 × 128 |
| [Textures/Items/item_06_stripes_horiz.png](<../Assets/JellyGarden/Textures/Items/item_06_stripes_horiz.png>) | 128 × 128 |
| [Textures/Items/item_06_stripes_vert.png](<../Assets/JellyGarden/Textures/Items/item_06_stripes_vert.png>) | 128 × 128 |

### Board cells, borders, and blockers

| Current asset | Pixels |
| --- | --- |
| [Textures/Blocks/block.png](<../Assets/JellyGarden/Textures/Blocks/block.png>) | 82 × 82 |
| [Textures/Blocks/block_02.png](<../Assets/JellyGarden/Textures/Blocks/block_02.png>) | 82 × 82 |
| [Textures/Blocks/border_01.png](<../Assets/JellyGarden/Textures/Blocks/border_01.png>) | 3 × 83 |
| [Textures/Blocks/border_02.png](<../Assets/JellyGarden/Textures/Blocks/border_02.png>) | 82 × 82 |
| [Textures/Blocks/border_03.png](<../Assets/JellyGarden/Textures/Blocks/border_03.png>) | 85 × 85 |
| [Textures/Blocks/solidBlock.png](<../Assets/JellyGarden/Textures/Blocks/solidBlock.png>) | 82 × 82 |
| [Textures/Blocks/square.png](<../Assets/JellyGarden/Textures/Blocks/square.png>) | 82 × 82 |
| [Textures/Blocks/square_2.png](<../Assets/JellyGarden/Textures/Blocks/square_2.png>) | 82 × 82 |
| [Textures/Blocks/thriving_block.png](<../Assets/JellyGarden/Textures/Blocks/thriving_block.png>) | 82 × 82 |
| [Textures/Blocks/undestroyable.png](<../Assets/JellyGarden/Textures/Blocks/undestroyable.png>) | 82 × 82 |
| [Textures/Blocks/wireBlock.png](<../Assets/JellyGarden/Textures/Blocks/wireBlock.png>) | 82 × 82 |

### In-level booster buttons, indicators, and animation frames

| Current asset | Pixels |
| --- | --- |
| [Textures/Boosts/5sec.png](<../Assets/JellyGarden/Textures/Boosts/5sec.png>) | 50 × 50 |
| [Textures/Boosts/animations_lollipop/1.png](<../Assets/JellyGarden/Textures/Boosts/animations_lollipop/1.png>) | 150 × 150 |
| [Textures/Boosts/animations_lollipop/2.png](<../Assets/JellyGarden/Textures/Boosts/animations_lollipop/2.png>) | 150 × 150 |
| [Textures/Boosts/animations_lollipop/3.png](<../Assets/JellyGarden/Textures/Boosts/animations_lollipop/3.png>) | 150 × 150 |
| [Textures/Boosts/animations_lollipop/4.png](<../Assets/JellyGarden/Textures/Boosts/animations_lollipop/4.png>) | 150 × 150 |
| [Textures/Boosts/boost_5_mooves.png](<../Assets/JellyGarden/Textures/Boosts/boost_5_mooves.png>) | 125 × 124 |
| [Textures/Boosts/boost_5_time.png](<../Assets/JellyGarden/Textures/Boosts/boost_5_time.png>) | 125 × 124 |
| [Textures/Boosts/boost_bomb.png](<../Assets/JellyGarden/Textures/Boosts/boost_bomb.png>) | 125 × 124 |
| [Textures/Boosts/boost_hand.png](<../Assets/JellyGarden/Textures/Boosts/boost_hand.png>) | 125 × 124 |
| [Textures/Boosts/boost_nomber.png](<../Assets/JellyGarden/Textures/Boosts/boost_nomber.png>) | 59 × 62 |
| [Textures/Boosts/boost_over_button.png](<../Assets/JellyGarden/Textures/Boosts/boost_over_button.png>) | 125 × 124 |
| [Textures/Boosts/boost_plus_button.png](<../Assets/JellyGarden/Textures/Boosts/boost_plus_button.png>) | 59 × 62 |
| [Textures/Boosts/boost_random_color.png](<../Assets/JellyGarden/Textures/Boosts/boost_random_color.png>) | 125 × 124 |
| [Textures/Boosts/lollipop.png](<../Assets/JellyGarden/Textures/Boosts/lollipop.png>) | 273 × 408 |
| [Textures/Boosts/star.png](<../Assets/JellyGarden/Textures/Boosts/star.png>) | 142 × 135 |

### Story pre-level booster icons

| Current asset | Pixels |
| --- | --- |
| [Resources/Story/UI/booster_colorbomb.png](<../Assets/JellyGarden/Resources/Story/UI/booster_colorbomb.png>) | 223 × 230 |
| [Resources/Story/UI/booster_locked.png](<../Assets/JellyGarden/Resources/Story/UI/booster_locked.png>) | 175 × 174 |
| [Resources/Story/UI/booster_packages.png](<../Assets/JellyGarden/Resources/Story/UI/booster_packages.png>) | 223 × 230 |
| [Resources/Story/UI/booster_stripes.png](<../Assets/JellyGarden/Resources/Story/UI/booster_stripes.png>) | 223 × 230 |

### Kit pre-level booster counterparts

| Current asset | Pixels |
| --- | --- |
| [Textures/menu/play_menu/boost_info_button.png](<../Assets/JellyGarden/Textures/menu/play_menu/boost_info_button.png>) | 51 × 51 |
| [Textures/menu/play_menu/booster_plus_button.png](<../Assets/JellyGarden/Textures/menu/play_menu/booster_plus_button.png>) | 91 × 96 |
| [Textures/menu/play_menu/play_booster_01.png](<../Assets/JellyGarden/Textures/menu/play_menu/play_booster_01.png>) | 175 × 174 |
| [Textures/menu/play_menu/play_booster_02.png](<../Assets/JellyGarden/Textures/menu/play_menu/play_booster_02.png>) | 175 × 174 |
| [Textures/menu/play_menu/play_booster_03.png](<../Assets/JellyGarden/Textures/menu/play_menu/play_booster_03.png>) | 175 × 174 |
| [Textures/menu/play_menu/play_booster_locked.png](<../Assets/JellyGarden/Textures/menu/play_menu/play_booster_locked.png>) | 175 × 174 |

### Kit booster shop art

| Current asset | Pixels |
| --- | --- |
| [Textures/menu/booster_shop/booster_30_times_icon.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_30_times_icon.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_5_moves_icon.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_5_moves_icon.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_5_pakages.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_5_pakages.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_5_stripes_jelly.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_5_stripes_jelly.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_bomb.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_bomb.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_colorful_bomb.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_colorful_bomb.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_hand.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_hand.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_info_banner.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_info_banner.png>) | 653 × 781 |
| [Textures/menu/booster_shop/booster_randome_color.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_randome_color.png>) | 223 × 230 |
| [Textures/menu/booster_shop/booster_shop_banner.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_shop_banner.png>) | 653 × 882 |
| [Textures/menu/booster_shop/booster_shop_button.png](<../Assets/JellyGarden/Textures/menu/booster_shop/booster_shop_button.png>) | 289 × 114 |

### Kit booster effect icons

| Current asset | Pixels |
| --- | --- |
| [Textures/menu/boosts_items/bomb_01.png](<../Assets/JellyGarden/Textures/menu/boosts_items/bomb_01.png>) | 137 × 148 |
| [Textures/menu/boosts_items/bomb_02.png](<../Assets/JellyGarden/Textures/menu/boosts_items/bomb_02.png>) | 137 × 148 |
| [Textures/menu/boosts_items/random_color_item.png](<../Assets/JellyGarden/Textures/menu/boosts_items/random_color_item.png>) | 128 × 163 |

### Objective support and auxiliary booster badge

| Current asset | Pixels |
| --- | --- |
| [Textures/map/map_target_01.png](<../Assets/JellyGarden/Textures/map/map_target_01.png>) | 60 × 25 |
| [Textures/map/map_target_02.png](<../Assets/JellyGarden/Textures/map/map_target_02.png>) | 34 × 31 |
| [Textures/map/map_target_03.png](<../Assets/JellyGarden/Textures/map/map_target_03.png>) | 32 × 24 |
| [Textures/map/map_target_04.png](<../Assets/JellyGarden/Textures/map/map_target_04.png>) | 44 × 25 |
| [Textures/map/map_target_05.png](<../Assets/JellyGarden/Textures/map/map_target_05.png>) | 43 × 30 |
| [Textures/menu/game_menu/arrow_ingredients.png](<../Assets/JellyGarden/Textures/menu/game_menu/arrow_ingredients.png>) | 142 × 149 |
| [Textures/menu/map_menu/boost_plus_button.png](<../Assets/JellyGarden/Textures/menu/map_menu/boost_plus_button.png>) | 91 × 96 |

### Board effects and destruction frames

| Current asset | Pixels |
| --- | --- |
| [Textures/effects/Bolt.png](<../Assets/JellyGarden/Textures/effects/Bolt.png>) | 512 × 128 |
| [Textures/effects/Bubble.png](<../Assets/JellyGarden/Textures/effects/Bubble.png>) | 256 × 256 |
| [Textures/effects/Star_particle.tga](<../Assets/JellyGarden/Textures/effects/Star_particle.tga>) | 128 × 128 |
| [Textures/effects/emty_sprite.png](<../Assets/JellyGarden/Textures/effects/emty_sprite.png>) | 150 × 144 |
| [Textures/effects/explosion_item_01/explosion-of-purple-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_01/explosion-of-purple-candy-1.png>) | 163 × 163 |
| [Textures/effects/explosion_item_01/explosion-of-purple-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_01/explosion-of-purple-candy-2.png>) | 163 × 163 |
| [Textures/effects/explosion_item_01/explosion-of-purple-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_01/explosion-of-purple-candy-3.png>) | 163 × 163 |
| [Textures/effects/explosion_item_01/explosion-of-red-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_01/explosion-of-red-candy-4.png>) | 163 × 163 |
| [Textures/effects/explosion_item_02/explosion-of-orange-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_02/explosion-of-orange-candy-1.png>) | 163 × 163 |
| [Textures/effects/explosion_item_02/explosion-of-orange-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_02/explosion-of-orange-candy-2.png>) | 163 × 163 |
| [Textures/effects/explosion_item_02/explosion-of-orange-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_02/explosion-of-orange-candy-3.png>) | 163 × 163 |
| [Textures/effects/explosion_item_02/explosion-of-orange-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_02/explosion-of-orange-candy-4.png>) | 163 × 163 |
| [Textures/effects/explosion_item_03/explosion-of-purple-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_03/explosion-of-purple-candy-1.png>) | 163 × 163 |
| [Textures/effects/explosion_item_03/explosion-of-purple-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_03/explosion-of-purple-candy-2.png>) | 163 × 163 |
| [Textures/effects/explosion_item_03/explosion-of-purple-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_03/explosion-of-purple-candy-3.png>) | 163 × 163 |
| [Textures/effects/explosion_item_03/explosion-of-purple-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_03/explosion-of-purple-candy-4.png>) | 163 × 163 |
| [Textures/effects/explosion_item_04/explosion-of-blue-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_04/explosion-of-blue-candy-1.png>) | 163 × 163 |
| [Textures/effects/explosion_item_04/explosion-of-blue-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_04/explosion-of-blue-candy-2.png>) | 163 × 163 |
| [Textures/effects/explosion_item_04/explosion-of-blue-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_04/explosion-of-blue-candy-3.png>) | 163 × 163 |
| [Textures/effects/explosion_item_04/explosion-of-blue-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_04/explosion-of-blue-candy-4.png>) | 163 × 163 |
| [Textures/effects/explosion_item_05/explosion-of-green-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_05/explosion-of-green-candy-1.png>) | 163 × 162 |
| [Textures/effects/explosion_item_05/explosion-of-green-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_05/explosion-of-green-candy-2.png>) | 163 × 162 |
| [Textures/effects/explosion_item_05/explosion-of-green-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_05/explosion-of-green-candy-3.png>) | 163 × 162 |
| [Textures/effects/explosion_item_05/explosion-of-green-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_05/explosion-of-green-candy-4.png>) | 163 × 162 |
| [Textures/effects/explosion_item_06/explosion-of-yellow-candy-1.png](<../Assets/JellyGarden/Textures/effects/explosion_item_06/explosion-of-yellow-candy-1.png>) | 163 × 163 |
| [Textures/effects/explosion_item_06/explosion-of-yellow-candy-2.png](<../Assets/JellyGarden/Textures/effects/explosion_item_06/explosion-of-yellow-candy-2.png>) | 163 × 163 |
| [Textures/effects/explosion_item_06/explosion-of-yellow-candy-3.png](<../Assets/JellyGarden/Textures/effects/explosion_item_06/explosion-of-yellow-candy-3.png>) | 163 × 163 |
| [Textures/effects/explosion_item_06/explosion-of-yellow-candy-4.png](<../Assets/JellyGarden/Textures/effects/explosion_item_06/explosion-of-yellow-candy-4.png>) | 163 × 163 |
| [Textures/effects/snow.png](<../Assets/JellyGarden/Textures/effects/snow.png>) | 50 × 50 |
| [Textures/effects/stipes_destroy.png](<../Assets/JellyGarden/Textures/effects/stipes_destroy.png>) | 296 × 76 |

### Story arc backdrops and kit fallback backgrounds

| Current asset | Pixels |
| --- | --- |
| [Resources/Story/Art/backdrop_border.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_border.jpg>) | 1152 × 2048 |
| [Resources/Story/Art/backdrop_castle.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_castle.jpg>) | 1152 × 2048 |
| [Resources/Story/Art/backdrop_crypts.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_crypts.jpg>) | 1152 × 2048 |
| [Resources/Story/Art/backdrop_mines.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_mines.jpg>) | 1152 × 2048 |
| [Resources/Story/Art/backdrop_siege.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_siege.jpg>) | 1152 × 2048 |
| [Resources/Story/Art/backdrop_village.jpg](<../Assets/JellyGarden/Resources/Story/Art/backdrop_village.jpg>) | 1152 × 2048 |
| [Textures/backgrounds/background_01.png](<../Assets/JellyGarden/Textures/backgrounds/background_01.png>) | 800 × 1280 |
| [Textures/backgrounds/background_02.png](<../Assets/JellyGarden/Textures/backgrounds/background_02.png>) | 800 × 1280 |
| [Textures/backgrounds/background_03.png](<../Assets/JellyGarden/Textures/backgrounds/background_03.png>) | 800 × 1280 |
| [Textures/backgrounds/background_04.png](<../Assets/JellyGarden/Textures/backgrounds/background_04.png>) | 800 × 1280 |
| [Textures/backgrounds/background_05.png](<../Assets/JellyGarden/Textures/backgrounds/background_05.png>) | 800 × 1280 |

### Additional images referenced by board/effect dependencies

| Current asset | Pixels |
| --- | --- |
| [Fonts/Textures/Foam.png](<../Assets/JellyGarden/Fonts/Textures/Foam.png>) | 64 × 64 |
| [Fonts/Textures/Ripple.png](<../Assets/JellyGarden/Fonts/Textures/Ripple.png>) | 128 × 128 |
| [Fonts/Textures/Splash_4Frames.png](<../Assets/JellyGarden/Fonts/Textures/Splash_4Frames.png>) | 512 × 128 |
| [Textures/menu/failed_menu/v_completed.png](<../Assets/JellyGarden/Textures/menu/failed_menu/v_completed.png>) | 72 × 73 |
| [Textures/menu/failed_menu/x_failed.png](<../Assets/JellyGarden/Textures/menu/failed_menu/x_failed.png>) | 72 × 73 |

## Appendix B — board/effect dependency files

These are consumers to check when exporting approved artwork. They are not additional illustration commissions. Listed by following references from the item/cell/blocker, target-block, timed pickup, exit-arrow, particle, and effect prefabs; this does not replace later runtime validation.

### Prefabs

- [Resources/Particles/PopupScores.prefab](<../Assets/JellyGarden/Resources/Particles/PopupScores.prefab>)
- [Resources/Particles/StripesEffect.prefab](<../Assets/JellyGarden/Resources/Particles/StripesEffect.prefab>)
- [Resources/Prefabs/5sec.prefab](<../Assets/JellyGarden/Resources/Prefabs/5sec.prefab>)
- [Resources/Prefabs/Block.prefab](<../Assets/JellyGarden/Resources/Prefabs/Block.prefab>)
- [Resources/Prefabs/Effects/CircleExpl.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/CircleExpl.prefab>)
- [Resources/Prefabs/Effects/Firework.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/Firework.prefab>)
- [Resources/Prefabs/Effects/ItemExpl.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/ItemExpl.prefab>)
- [Resources/Prefabs/Effects/PackageExpl.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/PackageExpl.prefab>)
- [Resources/Prefabs/Effects/Replace.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/Replace.prefab>)
- [Resources/Prefabs/Effects/Snow.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/Snow.prefab>)
- [Resources/Prefabs/Effects/Splash 1.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/Splash 1.prefab>)
- [Resources/Prefabs/Effects/Splash.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/Splash.prefab>)
- [Resources/Prefabs/Effects/bomb.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/bomb.prefab>)
- [Resources/Prefabs/Effects/random_color_item.prefab](<../Assets/JellyGarden/Resources/Prefabs/Effects/random_color_item.prefab>)
- [Resources/Prefabs/Item.prefab](<../Assets/JellyGarden/Resources/Prefabs/Item.prefab>)
- [Resources/Prefabs/SolidBlock.prefab](<../Assets/JellyGarden/Resources/Prefabs/SolidBlock.prefab>)
- [Resources/Prefabs/Square.prefab](<../Assets/JellyGarden/Resources/Prefabs/Square.prefab>)
- [Resources/Prefabs/ThrivingBlock.prefab](<../Assets/JellyGarden/Resources/Prefabs/ThrivingBlock.prefab>)
- [Resources/Prefabs/UndestrBlock.prefab](<../Assets/JellyGarden/Resources/Prefabs/UndestrBlock.prefab>)
- [Resources/Prefabs/WireBlock.prefab](<../Assets/JellyGarden/Resources/Prefabs/WireBlock.prefab>)
- [Resources/Prefabs/arrow_ingredients.prefab](<../Assets/JellyGarden/Resources/Prefabs/arrow_ingredients.prefab>)
- [Resources/Targets/TargetBlocks.prefab](<../Assets/JellyGarden/Resources/Targets/TargetBlocks.prefab>)
### Animation controllers

- [Animation/CircleExpl.controller](<../Assets/JellyGarden/Animation/CircleExpl.controller>)
- [Animation/Item.controller](<../Assets/JellyGarden/Animation/Item.controller>)
- [Animation/ItemDestroyController.controller](<../Assets/JellyGarden/Animation/ItemDestroyController.controller>)
- [Animation/bomb_boost.controller](<../Assets/JellyGarden/Animation/bomb_boost.controller>)
### Animation clips

- [Animation/5secfly.anim](<../Assets/JellyGarden/Animation/5secfly.anim>)
- [Animation/BrickRotate.anim](<../Assets/JellyGarden/Animation/BrickRotate.anim>)
- [Animation/anim_fly_loop.anim](<../Assets/JellyGarden/Animation/anim_fly_loop.anim>)
- [Animation/apearing_thriving.anim](<../Assets/JellyGarden/Animation/apearing_thriving.anim>)
- [Animation/appear.anim](<../Assets/JellyGarden/Animation/appear.anim>)
- [Animation/bomb_boost.anim](<../Assets/JellyGarden/Animation/bomb_boost.anim>)
- [Animation/circle_expl.anim](<../Assets/JellyGarden/Animation/circle_expl.anim>)
- [Animation/destroy.anim](<../Assets/JellyGarden/Animation/destroy.anim>)
- [Animation/destroy_package.anim](<../Assets/JellyGarden/Animation/destroy_package.anim>)
- [Animation/disappear.anim](<../Assets/JellyGarden/Animation/disappear.anim>)
- [Animation/floating_word.anim](<../Assets/JellyGarden/Animation/floating_word.anim>)
- [Animation/flyupText.anim](<../Assets/JellyGarden/Animation/flyupText.anim>)
- [Animation/horr_appearing.anim](<../Assets/JellyGarden/Animation/horr_appearing.anim>)
- [Animation/item_anim1.anim](<../Assets/JellyGarden/Animation/item_anim1.anim>)
- [Animation/item_anim2.anim](<../Assets/JellyGarden/Animation/item_anim2.anim>)
- [Animation/item_anim3.anim](<../Assets/JellyGarden/Animation/item_anim3.anim>)
- [Animation/item_anim4.anim](<../Assets/JellyGarden/Animation/item_anim4.anim>)
- [Animation/item_anim5.anim](<../Assets/JellyGarden/Animation/item_anim5.anim>)
- [Animation/item_anim6.anim](<../Assets/JellyGarden/Animation/item_anim6.anim>)
- [Animation/item_package_anim1.anim](<../Assets/JellyGarden/Animation/item_package_anim1.anim>)
- [Animation/package_idle.anim](<../Assets/JellyGarden/Animation/package_idle.anim>)
- [Animation/popupText.anim](<../Assets/JellyGarden/Animation/popupText.anim>)
- [Animation/random_color_boost.anim](<../Assets/JellyGarden/Animation/random_color_boost.anim>)
- [Animation/stop.anim](<../Assets/JellyGarden/Animation/stop.anim>)
- [Animation/stripped_anim.anim](<../Assets/JellyGarden/Animation/stripped_anim.anim>)
- [Animation/tip1.anim](<../Assets/JellyGarden/Animation/tip1.anim>)
### Materials

- [Fonts/Materials/Water/AddSoft.mat](<../Assets/JellyGarden/Fonts/Materials/Water/AddSoft.mat>)
- [Fonts/Materials/Water/Splash_4frms.mat](<../Assets/JellyGarden/Fonts/Materials/Water/Splash_4frms.mat>)
- [Fonts/Materials/Water/m_AddSoft.mat](<../Assets/JellyGarden/Fonts/Materials/Water/m_AddSoft.mat>)
- [Fonts/Materials/snow.mat](<../Assets/JellyGarden/Fonts/Materials/snow.mat>)
- [Materials/Bubble.mat](<../Assets/JellyGarden/Materials/Bubble.mat>)
- [Materials/ElectricBolt_Add 1.mat](<../Assets/JellyGarden/Materials/ElectricBolt_Add 1.mat>)
- [Materials/package_particle.mat](<../Assets/JellyGarden/Materials/package_particle.mat>)
