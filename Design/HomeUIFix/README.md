# Homepage buttons, king frame and logo

Fixed overlapping/gapped button layers by extending the green/gold faces beneath their gold trims and drawing the trims last. Framed the existing animated king inside an inset blue aperture, with the gold frame above his portrait. Hid only the homepage logo; its asset and startup appearance are unchanged.

Runtime change: Assets/ReferenceGameplay/Home/Scripts/ReferenceHomeLayout.cs only. No character artwork, animation, background, cloud motion, game rules or button callbacks changed.

Validation: static compile passed. Visually reviewed the live 1080x1920 homepage screenshot: clean button borders, contained king portrait, no homepage logo. Reused the focused homepage Play Mode checks: three clouds, movement, reduced motion, hidden-home pause, Settings and normal-save restoration all passed. Zero tracked errors. No full-game QA.
