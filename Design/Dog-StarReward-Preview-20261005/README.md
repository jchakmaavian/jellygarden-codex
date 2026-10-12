# Dog star-reward animation preview

Date: 2026-10-05

## Scope

Preview-first replacement concept for the King on the level-complete star reward popup. This directory is design evidence only. No Unity runtime, scene, prefab, save, economy, or reward code is changed.

The character direction is based on the existing Royal Throne dog at `Assets/JellyGarden/Resources/RoyalCampaignArt/dog.png`: tan-and-white corgi, white blaze, warm brown eyes, blue-and-gold bandana and gold medallion. That file was inspected locally. It was not uploaded to the generation provider; this is a text-guided concept, not an identity-locked animation of that exact sprite.

## Playback

Open `preview.html` or play `dog-star-reward-preview.mp4`. The blue background is baked into the render; this is not an alpha video or a rigged production asset. Replay restarts the performance; it is not claimed to be a seamless loop.

## Generation provenance

- Provider: ElevenLabs Creative Studio
- Model: `bytedance-seedance-v2.5`
- Requested output: one 8-second, 1:1, 1080p video, audio disabled
- Flow: https://elevenlabs.io/app/flows/rjEC1D6TBxRv1DcPtRH8
- Node: `YtnnlT5ZQ8AHILUlf3ir`
- Session: `c0PJCMJAd4YPZpJrU1Ib`
- Generation: `gJVFl8MOsAXR8QioU8ql`
- Exact creative request: `prompt.txt`
- Earlier Magnific attempt `ksRbjNB16B` failed with "Tool usage limit reached" and produced no delivered clip. It was not restarted.

## Review results

- Master received: 1440 x 1440 HEVC Main 10, 24 fps, 193 frames, 8.04 seconds, no audio stream.
- Playback copy: 1080 x 1080 H.264, 24 fps, 193 frames, silent; fast-start MP4 for wider player support.
- Full playback-copy decode completed without FFmpeg errors.
- Eight evenly sampled frames were visually inspected. The puppy retains four legs, both front paws hold a single five-point star, and head/body volume remains consistent in those samples. Eyes close for the cuddle and reopen before the forward offer. The star lights the fur and bandana.
- Compared with the source dog, the generated medallion sits on the side of the bandana and the facial proportions differ. This remains a new interpretation, not an exact asset match.
- Opening and closing poses differ. No seamless-loop claim. No Unity or device test was run because no runtime change was made.
- Review stills: `review-01.png` through `review-08.png`.

## Future integration boundary

`RoyalMenuSkin.Apply` mounts the King for `MenuComplete` and `MenuFailed`. The requested dog replacement applies to the completed-level star reward presentation; keep the failure presentation separate. `RoyalWinSequence.Reward` presents the reward; LevelManager commits the award separately. Integration should retain the existing award and Continue behavior.

Production preparation still requires the desired visual direction to be settled, background/compositing treatment, clip playback/hold behavior, popup layout, and Unity runtime verification. This preview is not evidence of game integration.
