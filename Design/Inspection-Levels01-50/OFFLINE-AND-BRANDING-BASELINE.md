# Royal Throne — offline and branding baseline

Date: 23 September 2026. Changes began only after the verified backup at `Design/Backup/RoyalThrone-before-Levels01-50-20260923-163705`.

## Identity and ownership

The game remains **Royal Throne**. The home profile identifies **King Vanguard**. No player-visible Royal Match or King Arthur text was found in the active scene/code audit. Internal comparison/implementation names are not player branding. Existing third-party software notices and asset `.meta` identities are retained.

No Royal Throne-owned network endpoint or service account was verified. The current build is therefore offline. The inherited GameSparks/Facebook configuration resource is now an empty JSON object, with its existing `.meta` preserved. Inherited ad IDs, store-product IDs, billing public key, rating/sharing URLs and service flags have been cleared from the game scene and service settings. Sensitive former values are intentionally omitted here; the pre-edit backup is the rollback source.

## Offline behavior

- `RoyalOfflineMode.ServicesEnabled` is false and cannot be enabled by player settings. Startup, purchase, ad, social and backend entry points check it. Rating/privacy callbacks no longer open inherited destinations.
- Android, Standalone, iPhone and Windows Store service defines are cleared. Analytics submission is off; existing Unity service switches remain off.
- Both existing Android builders use `RoyalOfflineBuildPolicy`. The legacy level editor preserves unrelated defines and strips service defines. A pre-build check rejects service/analytics defines if another tool reintroduces them.
- `RoyalOfflineAndroidManifest` puts permission-removal directives in the launcher manifest, the high-priority manifest-merge input. Internet, network-state, Wi-Fi-state and advertising-ID permissions are removed. The build fails if the launcher manifest cannot be found.
- Existing local gameplay, lives, inventory and saved progression continue to use their existing local storage. The purchasing package/version is retained but its inherited integration is disabled.

## Isolated levels test build

Entry point: `RoyalThrone.Editor.RoyalAndroidBuild.BuildLevels01To50Test`.

Output: `Design/Backup/apk/RoyalThrone-Levels01-50-Test.apk`.

Build log: `Design/Backup/apk/RoyalThrone-Levels01-50-Test-build.log`.

The test application uses `com.royalthrone.levelsqa` and the launcher name `Royal Throne Levels 01-50 Test`. Android gives this package separate app data, so installing it does not replace the normal `com.codexsandbox.jellygarden` QA application or its progress. Its builder restores the normal app identifier, product name, orientation, backend, architecture, signing and editor build options in `finally`, including after a build failure. The normal builders retain their existing output paths.

## Music preservation

No music asset, audio importer, playback/looping code, music volume or crossfade setting was changed. The seven SHA-256 values below were captured before cleanup and rechecked afterward. `EffectsProfile.asset` retains music volume `0.32` and crossfade `1.2` seconds.

| File | SHA-256 |
| --- | --- |
| `Assets/JellyGarden/Resources/RoyalGameplay/Audio/castle-theme.mp3` | `389427FE96731CC782AECD21AD27883F3BD4D2227FF0B20AEBA710CC2A61ACED` |
| `Assets/JellyGarden/Resources/RoyalGameplay/Audio/castle-theme.mp3.meta` | `A318DEC50A9A22A71544A190878D2F928A3A3B64A217189C4A9D7BE4E85308AD` |
| `Assets/JellyGarden/Resources/RoyalGameplay/EffectsProfile.asset` | `C37E80CC0905533B1E5F1A9DB12D6CC78536BA87773F683324BDC467942F4409` |
| `Assets/JellyGarden/Scripts/Juice/RoyalAudioDirector.cs` | `3F5E72841D3FD30440748BFEEC7EE4D5477E4E24D1829CFA6E994F65E0228DA1` |
| `Assets/JellyGarden/Scripts/MusicBase.cs` | `089BC084604C308D3F50BC85BDB98A4A8EB95E75C796C700BF362CA317088271` |
| `Assets/JellyGarden/Audio/wav/game_music.wav` | `BF589C7B017D3680EC2416E2C1F91C7EEDD03BA9D27487A169AFA8CC2D91A473` |
| `Assets/JellyGarden/Audio/wav/game_map_music.wav` | `40BFBC107A8F766472CC34D243EAED8834EAE10820377244DC7BE316BD656D7F` |

## Verification at handoff

Passed static C# compilation against the installed Unity 6000.5.10f1 assemblies for both Android builders, the offline build policy, the manifest postprocessor and the existing home authoring dependency. Passed checks for cleared current-scene service values, inert credential JSON, preserved normal Android application ID and all seven unchanged music hashes. Confirmed no inherited service/analytics defines remain in any configured player target.

Unity import, player build, final merged APK permissions, isolated-install behavior and device playtesting are subsequent integration checks. This cleanup did not invoke Unity or build an APK.
