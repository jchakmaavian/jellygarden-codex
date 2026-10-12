# Profile redesign (2026-09-30)

The Home portrait now opens a full Profile page and an Edit Profile card, laid out like the reference's profile. It keeps
Royal Throne's own page and card style: the Settings page's header and panels, the ribbon cards' Edit card, the Leaderboard
page's tabs, and the card kit's gold, blue and cream.

## The user's decisions

- **Scope:** the Profile page plus Edit Profile, with Avatar, Frame, Name and Badge tabs. Everything is saved on this
  device only.
- **Stats:** real ones only.
  - From the save: levels completed, areas completed, total score and best score.
  - First-try wins and power-ups made are counted from this update on.
  - Nothing for teams or collections until those exist.
- **Starting name:** "Player" and four digits, as in the reference. King Vanguard stays the game's king.
- **Avatars:** a new set is to be generated with Magnific, and shown to the user before it is added. Magnific had 11
  credits and an avatar costs 60, so the set is waiting on credits. Until then the list holds King Vanguard (his approved
  "excited" pose), the corgi and the steward, cropped to their heads.

## What the player sees

**Profile page** (while Home's "profile" panel is open, over the navigation bar, as the Settings page):
- The header: "Profile" and the red close.
- The player's card:
  - the framed avatar with a green edit badge;
  - the name in its style;
  - "No Team", since teams are not open;
  - the month play began, as a calendar and MM/YYYY;
  - the current level on a gold-rimmed pennant hanging from the card's top edge.
- "General Stats": six counts on the cream panel, each with the game's own art.
  - Levels Completed (the castle), First-Try Wins (the trophy) and Power-ups Made (the board's Rocket).
  - Areas Completed (the Areas book), Total Score (the star) and Best Score (the board's crown).
- Edit Profile. Tapping the avatar or its badge opens it too.

**Edit Profile card:**
- "Edit Profile" on the ribbon, with the red close.
- The preview: the avatar in its frame, the name field and the orange edit button that opens the keyboard.
- The tabs:
  - **Avatar:** every avatar, the chosen one ringed in green with a tick.
  - **Frame:** Gold, Sapphire, Emerald, Amethyst, Ruby.
  - **Name:** four name styles (Royal Gold, Ivory, Sapphire, Rose), shown on blue plates.
  - **Badge:** "Badges come with Collections, which are not open yet."
- Save.
  - A choice shows in the preview at once. Save keeps it, and stays off until something changes and the name has 3 to
    14 letters or numbers.
  - The close, or Back, leaves the profile as it was. Back closes the card first, then the page.
- Home's portrait shows the chosen avatar. King Vanguard's live portrait stays while he is the choice.

## Data (`RoyalProfile`, through RoyalSave, so a QA session works on its own copy)

- **The player's choices:** `RoyalThrone.Profile.Name`, `.Avatar`, `.Frame` and `.NameStyle`. `.Since` holds the month
  the profile was first seen; the save kept no install date before.
- **First-try wins** (`.FirstTryWins`, with `.Tried.NNN` per level):
  - `LevelManager.PrepareGame` marks an attempt, once per attempt. A Continue is the same attempt.
  - `CommitWin` counts it when the level had never been won or tried before.
- **Power-ups made** (`.PowerupsMade`): counted in `Item`'s DestroyCor, where a match's host piece becomes its
  power-up, while the level is played.
  - Pre-level boosts, QA and Mod Menu spawns, Color Ball conversions and the win finale don't count.
- **Areas completed:** the areas before the current one, plus the current one once all its tasks are done.
- **Scores:** each level's best score (`Score<n>`), summed, and the best of them.
- **Caveat:** a level tried before this update but never won has no mark, so its first win counts as first-try.

## Files

- New:
  - `Assets/JellyGarden/Scripts/RoyalProfile.cs`;
  - `GUI/RoyalProfileArt.cs`: avatars, frames, name styles, the framed tile, the pennant, drawn icons, tabs;
  - `GUI/RoyalProfilePage.cs`;
  - `GUI/RoyalProfileEditor.cs`.
- Changed: one line each in `LevelManager.cs` (the attempt, and the win) and `Item.cs` (a power-up made).
- **Adding avatars needs no code:** put square WebP portraits in `Resources/RoyalProfile/Avatars`. They are listed after
  the built-in three, by file name.

## Verified (editor, QA profile)

- **The profile driver** (`checks/profile-driver.txt`): 24 of 24 pass, with no runtime errors.
  - The page opens from the portrait: a new "Player NNNN", 09/2026, No Team, the pennant at the next level.
  - All six stats match the save.
  - It fits at 1080x1920, 1440x2960, 720x1280 and 1536x2048.
  - Edit Profile: the three avatars (the King chosen), the five frames, the four name styles, and the Badge message.
  - Save turns on only for a change and a valid name. A two-letter name turns it off.
  - Back closes only the card and keeps nothing.
  - Save keeps avatar, frame, style and name, and the page and Home's portrait show them.
  - After the QA session, Home's portrait is the real profile's King again.
  - **First-try wins,** played through the game's own paths: a new level won at once counts +1; its replay counts +0; a
    level lost, then won, counts +0.
  - **Power-ups made:** a match of four counts +1, and the page shows the new counts.
- **The Home suite:** 179 of 179 pass, with the Settings page switched off. That is needed for a check that predates this
  work; see the UI polish README.
- **Real save:** no real profile keys were written. The game values are unchanged (level, coins, boosters, sound).
  - The only differences are the game's own life regeneration and a one-time Areas layout marker. The marker is written
    by the Areas code of another session.
- **Evidence:** `screens/`: the page, the Edit card's tabs, the saved state with Home's portrait, and the screen sizes.

## Not done

- The generated avatar set. It waits for Magnific credits (about 480 for 8), and will be shown to the user before it is
  added.
- Not tested on a phone. The keyboard on the name field in particular needs a device.
- Not committed.
