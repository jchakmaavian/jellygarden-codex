# Level music (Royal Throne)

Requested by the user on 7 October 2026: ten in-game level tracks. The brief was "enjoyable, not only melodies but also beat music that feels excitement and refreshment". The castle theme was liked and stays the Home music.

## In the game

- The files are `Assets/JellyGarden/Resources/RoyalGameplay/Music/level-NN-*.ogg`.
- `RoyalAudioDirector` plays track `(level - 1) mod 10` while a level is open; level 11 reuses track 1. Home (`GameState.Map`) plays `castle-theme`.
- Switching between Home and a level crossfades. Each track loads in the background before it fades in and is unloaded after it fades out.
- Level tracks are seamless loops and repeat natively. The existing win behaviour applies to them: the music fades out for the celebration and returns softly, muffled, on the win card.
- All tracks are mastered to −22.5 LUFS integrated, level with the castle theme (−22.8 LUFS), with true peaks around −9 dBFS.

## The tracks

| # | Title | Style | Key, tempo | Lead voices |
|---|---|---|---|---|
| 1 | Crown Jewels | Bright pop-house, sidechain pump | D major, 122 | Saw pluck, square lead, marimba |
| 2 | Garden Gallop | Folk-pop stomp, strummed guitar | G major, 128 | Whistle, pizzicato, glockenspiel |
| 3 | Palace Funk | Swung funk, ghost-note snare | E minor, 108 | Saw lead, brass section, e-piano |
| 4 | Knight's Dash | Driving electro-orchestral | A minor, 134 | Saw lead, brass, string ostinato |
| 5 | Sugar Tower | Bouncy chiptune pop | F major, 118 | Square chip lead, glockenspiel |
| 6 | Moonlit Ballroom | Laid-back chillhop (the calm one) | B♭ major, 88 | Vibraphone, flute, celesta |
| 7 | Market Day | Ska bounce with organ skank | C major, 140 | Accordion, brass, glockenspiel |
| 8 | Sky Kites | Breezy tropical house | E major, 112 | Pan flute, marimba, kalimba |
| 9 | Dragon Drums | Epic taiko and synths | D minor, 126 | Saw lead, brass, harp |
| 10 | Royal Parade | Marching-band pop, drumline | E♭ major, 120 | Trumpets, piccolo, glockenspiel |

Each track is a 44-bar loop of 75–120 s with this structure:

- intro (4 bars), where the beat joins halfway;
- hook A (8 bars);
- contrasting B (8 bars);
- A with harmony (8 bars);
- a kick-free break that builds back (4 bars);
- B with doubling (8 bars);
- outro (4 bars), which leads back into the intro.

## Source

- The tracks are original and synthesised from scratch by `musicgen.cs`, a .NET 10 file-based app with no samples or third-party audio. It contains the synthesis engine and every score: chord progressions, drum, bass and arpeggio patterns, and melodies written as scale degrees.
- To regenerate: `dotnet run musicgen.cs -- render`.
- Then encode each WAV to −22.5 LUFS Ogg Vorbis: measure with ffmpeg `ebur128`, then `ffmpeg -i x.wav -af volume=<gain>dB -c:a libvorbis -q:a 6 x.ogg`.
- The renderer wraps reverb and note tails past the loop end onto the start. Its high-pass filter and compressor also run over the loop twice, so the loop point is seamless. A seam check confirmed that each jump at the loop point is smaller than a normal sample-to-sample step.

## Verified

Checked on 7 October 2026 in Play mode (QA practice session):

- Home played `castle-theme`.
- Level 1 played Crown Jewels, looping.
- With the level number set to 6 and then 17, the music switched to Moonlit Ballroom and then Market Day.
- Returning Home restored the castle theme.
- The console showed no errors.

The tracks were not auditioned by ear during development. They need a listening pass, ideally on phones.
