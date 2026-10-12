# Level music (Royal Throne)

## Status (8 October 2026)

- **Rejected so far:** the first ten tracks ("boring"), The King's Quest, and Crown of Fire. On 8 October the user asked for "march or parade music with melody".
- **The Iron Crown:** an orchestral 3/4 piece. It waits on recorded samples (VSCO 2 CE, CC0), whose download needs the user's permission. Only a synthesised sketch exists.
- **On trial: The Royal Parade March.** Every level plays it (`Music/level-royal-march.ogg`). It's a classic concert march in Sousa's form at 120 BPM, 76 bars (152 s):
  - **Fanfare intro (B♭):** the band in unison, with a roll.
  - **First strain:** trumpets and bells in dotted rhythms over tuba oom-pah, horn afterbeats, bass drum and cymbals.
  - **Second strain:** trombones and horns, through G minor and D7; the trumpets punch the afterbeats.
  - **Trio (E♭):** a gentle clarinet hymn with horns in thirds, including a borrowed A♭ minor chord.
  - **Breakstrain (C minor):** low brass calls, the trumpets answer, and the drums drive every beat.
  - **Grandioso:** the trio tune in full band, trombones an octave down, a piccolo obbligato, and cymbals on every beat. It then loops back to the intro.
  - **Loudness per section:** intro −14.9 dB, strains about −15.6, trio −18.4, breakstrain and grandioso −13.8.
  - All melodies are original.
- Home keeps `castle-theme`. The rejected tracks are kept in git history.

## What makes the castle theme work

From an analysis of `castle-theme.mp3`:

- **Style:** orchestral fantasy adventure in G minor, 90 BPM.
- **Groove:** a low hit on every beat; mid-range strums and strings on every 8th, accented off the beat; tambourine 8ths. The groove never stops, and the loudness is flat across the whole piece.
- **Harmony:** Gm, Cm, D, B♭, F and E♭, with the raised leading tone (F♯) giving a regal, heroic minor colour.
- **Melody:** busy 8th-note lines with leaps and scale runs.
- **Sound:** warm and dark. The spectral centroid is about 1.1 kHz and almost nothing sits above 5 kHz. The stereo image is narrow.

The rejected ten were mostly synth-pop with bright mixes, sparse intros and drum-free breaks. They also had an untuned kick drum pitched near F♯, which clashed in several keys.

## The King's Quest

The melody and arrangement are original, written in the castle theme's style:

- **Key and tempo:** D minor (harmonic), 96 BPM. That's a little faster than Home for gameplay energy.
- **Rhythm section:** frame drum and kick tuned to the key, on every beat. Upright bass, and a strummed lute and string chug on the 8ths. Tambourine 8ths, plus a field snare in the full sections.
- **Melody:** a regal flute hook with dotted-rhythm pickups and a leading-tone turn, then a heroic horn melody (Gm–C–F–B♭–Gm–A–Dm–A).
- **Colour:** harp runs answering each phrase, a string section, and timpani into each section.
- **Structure:** a 40-bar seamless loop, 100 s long: intro 2 bars; A 8 (flute); A′ 8 (flute in thirds, pizzicato doubling, snare); B 8 (horns, flute descant); bridge 4 (half-time drums, harp arpeggios, timpani roll); A″ 8 (full, with horn chords); turnaround 2.
- **Matched to the castle theme:**

| Measure | Castle theme | King's Quest |
|---|---|---|
| Key clarity | 0.82 for G minor | 0.92 for D minor |
| Energy in bands below 120 Hz / 120–300 Hz / 300–800 Hz / 800 Hz–2 kHz / 2–5 kHz | 28 / 21 / 34 / 15 / 1.4 % | 35 / 16 / 36 / 11 / 2 % |
| Loudness | −22.8 LUFS | −22.5 LUFS |

## Source

- Everything is synthesised from scratch by `musicgen.cs`, a .NET 10 file-based app with no samples or third-party audio. `KingsQuest()` holds this score. The rejected ten remain in `Tracks.All()`.
- To regenerate: `dotnet run musicgen.cs -- render kings`.
- Then encode to −22.5 LUFS Ogg Vorbis: measure with ffmpeg `ebur128`, then `ffmpeg -i x.wav -af volume=<gain>dB -c:a libvorbis -q:a 6 x.ogg`.
- The renderer wraps reverb and note tails past the loop end onto the start. Its filters and compressor also run over the loop twice, so the loop point is seamless.

## Not yet verified by ear

These tracks were composed and checked by measurement only; no one has listened to them during development. The user's listening verdict decides whether this direction continues.
