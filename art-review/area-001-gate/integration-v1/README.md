# Area 1 integration

The four approved gate states and three approved animation clips are installed in the game following the user's “ok implement” instruction.

The existing `area_001_state0.jpg` through `area_001_state3.jpg` paths and sprite GUIDs are preserved. Each image was exported from the approved PNG at JPEG quality 97 with no chroma subsampling. The review PNGs remain the original source assets.

The six-second videos are copied unchanged into `Assets/JellyGarden/Resources/Story/RepairAnimations/area_001_task1.mp4` through `area_001_task3.mp4`. They play on the repair overlay in the existing task order: timber, banner, then lanterns. The completed painting is revealed underneath the final video frame and fades back in before the task list returns. These are the approved simplified 3D motion-study clips, not videos of the exact generated paintings.

`StoryRepairVideo` loads clips by the area/task filename convention. Tasks without clips retain the existing repair effects. Preparation and playback have time limits; a decoder failure before the first frame falls back to the ordinary repair effect. Skipping or a failure after playback starts still reveals the purchased state. Video surfaces are released after completion. A second purchase is rejected while a repair is already playing.

Only Area 1 art and clips are installed. The repair-region table was measured again for Area 1: the banner and lanterns now have their own correct effect regions. The timber edit's pixel difference is too broad for a reliable localized rectangle, so its fallback retains the existing room-wide effect. Other areas' region entries are unchanged.

The area screen now recognizes portrait artwork and fills the background behind the task card. This prevents the new portrait gate appearing as a miniature duplicate in the old landscape art band. Its captions stay above the panel and use light lettering. Landscape areas retain their existing framing. Video playback covers the room during the repair so strips of the underlying painting do not show around the clip.

`installation.json` records the installed assets and hashes. `backups/` contains the former gate JPEGs and repair-region table.

Validation results are stored alongside this file. The complete existing screen-test suite also reports three unrelated stale expectations: two navigation signs versus the current four, and old repair/star totals for areas 1–5. The focused gate asset and repair-region checks pass. Those unrelated expectations have been left intact.
