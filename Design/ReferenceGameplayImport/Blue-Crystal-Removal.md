# Blue crystal removed from the board

The legacy sixth board piece shown by the user no longer spawns. Normal generation uses at most five colours. Old piece ID 5 and old collection-goal ID 5 map to the blue shield (ID 1), including the goal icon, in runtime copies only. Booster/ingredient sentinel IDs are unchanged. The developer gem selector now exposes 0–4.

Original art files and serialized level assets are retained for recovery; normal player saves and the other gems/diamonds were not changed.

Validation: runtime and Editor compilation passed. Isolated Play Mode checked 64 refill attempts under a legacy six-colour configuration, old-piece rendering, old-goal copy/credit, sentinel preservation and a real Match-3/refill. All passed; zero new runtime errors and normal player profile restored. See Validation/Board-Palette.txt.

Checkpoint: Design/Backup/Remove-BlueCrystal-20260924-141947.
