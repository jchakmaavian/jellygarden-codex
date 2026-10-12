# Merge spec: `Assets/JellyGarden/Scripts/SoundBase.cs`

Plan: §4 "`SoundBase.cs`, `MusicBase.cs`" (union of both sides), Task 3.4 Step 3.

## 1. Base

`origin/royal-throne` (94 lines). Then add main's one overload. This gives the same result as `git merge-file` with base `a82c4df`, ours `origin/main` and theirs `origin/royal-throne`: the two sides touch different regions, so there is no conflict.

```
git show origin/royal-throne:Assets/JellyGarden/Scripts/SoundBase.cs > Assets/JellyGarden/Scripts/SoundBase.cs
```

## 2. KEEP-FROM-MAIN

### K1. `PlaySound(AudioClip, float)` overload

Main's block (`origin/main` SoundBase.cs:73–78):

```csharp
		// STORY: a quieter one-shot (the Royal Throne landing and match sounds have
		// their own gains); the player's volume still applies on top.
		public void PlaySound(AudioClip clip, float volumeScale) {
			if (_audioSource && clip)
				_audioSource.PlayOneShot(clip, volumeScale);
		}
```

**Anchor in Royal Throne:** after the static helper `static bool Contains(AudioClip[] clips, AudioClip clip) => clips != null && global::System.Array.IndexOf(clips,clip) >= 0;` (RT:83) and the blank line after it. Insert the block before `public void SetVolume(float volume) {` (RT:85), copied verbatim.

## 3. CUT-FROM-ROYAL-THRONE

None. Royal Throne's additions are:
- the singleton `Awake` rewrite (`SetParent(null)` instead of parenting to the camera);
- the `RoyalAudioDirector` routing in `PlaySound(AudioClip)` (RT:62–81);
- `Contains`.

All reference `Juice.RoyalAudioDirector` and `Juice.RoyalCue`, which are imported with `Juice/**`. With the switch off, `RoyalAudioDirector.Get` returns null (`RoyalAudioDirector.cs:27`: `if (!Application.isPlaying || !RoyalEffectsProfile.Get) return null;`), so the kit path runs.

## 4. D3 colour changes

None.

## 5. Dropped main hooks

None.

## 6. Risks / judgement calls

- **Overload resolution.** With both `PlaySound(AudioClip)` and `PlaySound(AudioClip, float)`, one-argument calls still bind to Royal Throne's routed method. On `origin/main`, the two-argument overload is called only from retired files: `StoryOriginalBoosters.cs:216,238`, `StoryOriginalColorBall.cs:182`, `StoryOriginalMotion.cs:293` and `StoryOriginalTnt.cs:366`, all deleted in Task 3.7. After the port it has no callers. It is kept only because the plan asks for the union.
- **The two-argument overload bypasses `RoyalAudioDirector`.** A future caller would play through the kit `AudioSource` even when the Royal layer is on. Acceptable while it has no callers.
- **`Awake` no longer parents to `Camera.main`.** Main never changed `Awake`, so this is a pure Royal Throne improvement. It also removes the kit's `Camera.main` null risk at scene start.
- **Music is out of scope (plan §2), but `RoyalAudioDirector.Build()` starts its own music loop** (`music[0].Play()` on clip `RoyalGameplay/Audio/castle-theme`). That is a Juice-side coupling for `COUPLINGS.md`, not part of this file. See MusicBase.md §6.

```anchors
path: Assets/JellyGarden/Scripts/SoundBase.cs
rt: static bool Contains(AudioClip[] clips, AudioClip clip) => clips != null && global::System.Array.IndexOf(clips,clip) >= 0;
rt: public void SetVolume(float volume) {
rt: var royal = Juice.RoyalAudioDirector.Get;
absent-rt: float volumeScale
main: public void PlaySound(AudioClip clip, float volumeScale) {
main: _audioSource.PlayOneShot(clip, volumeScale);
```
