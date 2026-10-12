# Merge spec: `Assets/JellyGarden/Scripts/MusicBase.cs`

Plan: §4 "`SoundBase.cs`, `MusicBase.cs`" (union), Task 3.4 Step 3. Plan §2 puts "music and ambience" out of scope.

## 1. Base

`origin/royal-throne` (54 lines). `origin/main` is byte-identical to the kit ancestor `a82c4df` for this file, so the "union" is Royal Throne's file. The one change below keeps King Leo's music.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/MusicBase.cs > Assets/JellyGarden/Scripts/MusicBase.cs
```

## 2. KEEP-FROM-MAIN

Nothing to re-apply: main has no changes here. Main's *behaviour* (its own music plays) is preserved by the cut in §3.

## 3. CUT-FROM-ROYAL-THRONE

| RT line | Method | Royal Throne text (exact) | Action |
|---|---|---|---|
| 32 | `PlayMusic` | `if (Juice.RoyalAudioDirector.Get) { if (_audioSource) _audioSource.Stop(); return; }` | **Delete the line (default).** |

**Why.** Main's `Story/StoryMusic.cs` writes the King Leo themes into `MusicBase.music` (track 0 map, track 1 level). With the Royal layer on, this line silences King Leo's music (`MusicBase.PlayMusic(0/1)` from `LevelManager.gameStatus`) and leaves the music to `RoyalAudioDirector`. That director plays Royal Throne's `castle-theme` and `RoyalGameplay/Music/level-royal-march` (`RoyalAudioDirector.cs:18,47,134`). Music is out of scope (plan §2) and `Resources/RoyalGameplay/Music/**` is not imported (Task 3.6), so the gate must go.

**Keep from Royal Throne:** the `Awake` rewrite (RT:23–29):

```csharp
        void Awake() {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            _audioSource = GetComponent<AudioSource>();
        }
```

It fixes the kit's duplicate-singleton handling, which ran `DontDestroyOnLoad` on a camera child and only then destroyed the duplicate, and it removes the `Camera.main` dependency.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None. Main has no hooks here.

## 6. Risks / judgement calls

- **A second music track (blocker for `COUPLINGS.md`, not for this file).** `RoyalAudioDirector.Build()` always starts its own music source: `music[0].Play()` with clip `RoyalGameplay/Audio/castle-theme` (RoyalAudioDirector.cs:44–47). It then crossfades to level music in `Update`. Once `EffectsProfile.asset` is on (Task 4.1), King Leo's `MusicBase` music and Royal Throne's director music would play together, unless the director's music is cut or kept at zero. Options:
  - (a) cut the music sources from `RoyalAudioDirector.Build/Update`;
  - (b) set the profile's `musicVolume` to 0.

  Record the choice in `COUPLINGS.md`. UNVERIFIED: whether `RoyalGameplay/Audio/castle-theme` is imported. `RoyalGameplay/**` is imported except `Music/**` and `Win/**`, so `Audio/castle-theme` probably is.
- **If the owner wants Royal Throne's music after all,** keep RT:32 unchanged. Plan §2 says no.
- **`MusicBase` is no longer parented to the camera.** Main code that finds it under `Camera.main` would break. A search of `origin/main` found no such lookup; scripts use `MusicBase.Instance`. UNVERIFIED for prefab or scene wiring.

```anchors
path: Assets/JellyGarden/Scripts/MusicBase.cs
rt: if (Juice.RoyalAudioDirector.Get) { if (_audioSource) _audioSource.Stop(); return; }
rt: public void PlayMusic(int index, bool loop = true) {
rt: if (Instance && Instance != this) { Destroy(gameObject); return; }
rt: transform.SetParent(null);
main: transform.parent = Camera.main.transform;
absent-main: RoyalAudioDirector
```
