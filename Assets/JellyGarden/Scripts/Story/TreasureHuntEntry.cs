// The treasure hunt opens on the splash, and waits there for Play.
//
// This class used to load `game` one frame after `main` came up, on the
// reasoning that the hunt starts with the first puzzle and a returning player
// reaches their saved gates through the shell anyway. What that cost was the
// title screen: StorySplash dresses the kit's menu into King Leo's — the
// castle, the wordmark, the painted Play button, the loading screen behind it —
// and every one of those was built, logged as dressed, and discarded before it
// could draw a single frame. The log said `splash dressed: splash_logo,
// splash_background, play button, loading screen` immediately followed by the
// game scene's own lines, which is the whole story in two entries.
//
// Nothing replaces it, because nothing has to: the kit's Play button is already
// wired in main.unity to GUIEvents.Play, which plays the click, switches on the
// Loading child and calls SceneManager.LoadScene("game"). StorySplash redresses
// that button in place rather than rebuilding it, precisely so that wiring
// survives. Deleting the auto-load hands the scene back to the button that was
// always there.
//
// If the hunt ever does want to skip the menu again, the honest version waits
// on the splash for a beat first and is a coroutine here; a one-frame skip is
// indistinguishable from the scene never having been shown.

namespace JellyGarden.Scripts.Story
{
}
