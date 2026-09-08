# Castle Charm: name, story, and cinematics proposal

Creative proposal, 8 September 2026. The user requested a better game name, a castle/king/queen story, a game trailer, and animated storytelling inside the game. Those are requested directions. The specific title, cast, plot, durations, and visual choices below are proposed creative decisions; they are not implemented assets or user-approved final designs.

## Recommended working name

**Castle Charm**

Suggested store title: **Castle Charm: Match 3**.

Tagline: **Every match brings magic home.**

Castle gives a clear setting; Charm connects the characters' appeal and the castle's magic. The two familiar words support a warm royal adventure, while Match 3 explains the gameplay in the store title. This is a design judgment, not a measured player preference.

Alternatives for comparison:

| Name | Creative character | Tradeoff |
| --- | --- | --- |
| Castle Charm | Familiar wording, royal setting, and magical personality; the current recommendation | Castle is crowded; Charm can evoke jewelry |
| Castlewish | A distinctive invented compound closely linked to the Wishlight story | A new word to learn; spoken spacing may vary |
| Crown Charm | Compact royal and magical associations | Less explicit castle setting; possible accessory-brand association |

The initial recommendation was Castlewish. The user's request for internet, App Store, Google Play, and player-journey research led to the revised Castle Charm recommendation on 8 September 2026. See [NAMING-RESEARCH.md](NAMING-RESEARCH.md) for the evidence, limited exact-name search results, tradeoffs, and validation plan. No exact Castle Charm title surfaced in the sampled store searches; worldwide availability remains unconfirmed. No domains or accounts were registered. The Unity folder, GitHub repository, application identifier, and save identity have not been renamed.

## Story proposal

During the royal Wishlight Festival, the castle's ancient wishing lantern bursts into hundreds of wandering sparks. Rooms begin acting out forgotten wishes: a ballroom floats into the clouds, the kitchen grows a jungle, and the royal library starts rewriting its own stories.

King Rowan and Queen Elara invite the player to help recover the Wishlights and put their enchanted home back together. Each restored area reveals a funny character moment and a clue to the wish that started everything.

The tone is hopeful adventure with affectionate comedy. The king and queen both make decisions, solve problems, and sometimes need help. Their relationship carries the story across chapters.

## Proposed cast

| Character | Personality and role | Animation opportunity |
| --- | --- | --- |
| King Rowan | Warm, brave, hands-on; enthusiastically begins repairs before fully understanding the magic | Confident preparation, inventive physical problem-solving, and expressive surprise |
| Queen Elara | Curious, witty, and resourceful; studies the castle's living maps and hidden history | Observant reactions, decisive gestures, and magical map reveals |
| Pip | A tiny living lantern whose light changes with its emotions | A simple, recognizable silhouette; floating movement, squash/stretch, and light-based VFX |

Initial visual direction: appealing stylized 3D characters, readable silhouettes, warm stone, rich teal/plum fabrics, gold accents, and a distinctive trail of glowing Wishlights. Character sheets will establish proportions, outfits, expressions, and colors before animation production.

## Connect the story to play

Proposed loop: play a match-3 level, earn Wishlights, restore a castle area, watch a short character scene, and discover the next goal.

Wishlights are a proposed progression concept, not a change already made to the game's economy. Keep story progression at chapter or area milestones so that frequent scenes do not interrupt repeated puzzle play. Short character reactions can provide continuity between larger scenes.

## Trailer concept: 30-45 seconds

First create a 30-second storyboard and timed animatic. Extend only where the pacing earns it.

| Time | Shot | Purpose |
| --- | --- | --- |
| 0-4 s | The Wishlight Festival: the king and queen reveal the glowing lantern together | Establish the royal cast and our visual signature |
| 4-8 s | A wish goes wrong; the ballroom lifts, banners unravel, and Pip tumbles out of the light | Create a clear, funny problem |
| 8-11 s | Elara catches a floating map; Rowan holds the door open; both turn toward the player | Invite the player into the partnership |
| 11-22 s | Show actual puzzle play: a readable swap, a match, a booster, and a satisfying combination | Demonstrate the game we are building |
| 22-27 s | Recovered Wishlights restore the courtyard fountain; the royal pair celebrate with Pip | Show why the gameplay matters |
| 27-30 s | Castle Charm title reveal and tagline | Make the name memorable |

The trailer will use the established characters, castle, puzzle pieces, animation style, and VFX. Gameplay shots must represent implemented gameplay. It is currently a script concept; no final video has been produced.

## Story animation inside the game

- Opening: a proposed 15-25 second version of the lantern incident, leading directly to the first puzzle.
- Area introductions and completions: proposed 5-12 second scenes showing the problem or the result of the player's progress.
- Character reactions: reusable 1-3 second celebrations, encouragement, discoveries, and comic surprises.
- Presentation: skippable scenes, captions where dialogue is used, a replay option, and transitions that return reliably to gameplay.

Use a shared character and environment asset set for the trailer and the story scenes. At the prototype stage, use a timed animatic to test clarity and pacing. Once designs are established, choose rendered video for fixed cinematic shots and Unity animation for reactions that must respond to the current game state. These are production options, not a claim that the current project already contains the required rigs or cinematic assets.

## Production order

1. Review the name and story hook together.
2. Create the king, queen, Pip, and castle visual sheets; establish consistent designs.
3. Draw the opening storyboard and build a timed animatic with temporary sound.
4. Produce one finished short shot to prove the character animation and lighting style.
5. Integrate the first story scene with the playable first level.
6. Capture verified gameplay and assemble the trailer; finish sound, captions, and title cards.

The current naming/story pass takes priority before broad visual changes. The previously planned first playable-level milestone remains the gameplay foundation.

## Reference

[Dream Games' official Royal Match page](https://www.dreamgames.com/games/royal-match) describes how its royal characters, castle areas, puzzle pieces, and boosters share a consistent world. That cohesion is a useful quality reference. Castle Charm's proposed king-and-queen partnership, living lantern, and wish-driven castle chapters provide our own story structure.
