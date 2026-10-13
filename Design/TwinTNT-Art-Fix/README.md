# Royal Throne twin TNT artwork fix

The dedicated TNTTNTComboRfv387 view was using imported atlas sprites with the old TNT-lettered band. Its SpriteRenderer bindings now use the existing Royal Throne sun band and matching refined barrel, fuse and shadows from TNTTNTCombo. Prefab GUIDs, transforms, controller, animation clip, fuse socket, particles and gameplay remain unchanged.

ReferenceDualComboImporter now preserves the Royal Throne TNT source sprites when rebuilding the dedicated combo view.

Unity 6000.5.10f1 Play Mode verification: two actual TNT swaps, checking every visible animation frame for the expected artwork, successful completion, original 1.583333-second preparation duration and restored player data. Both runs passed. See checks.txt and combo-0.png / combo-1.png. No Android APK rebuild was requested or performed.
