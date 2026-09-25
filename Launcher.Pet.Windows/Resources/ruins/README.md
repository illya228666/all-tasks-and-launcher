# Desktop ruins assets

Original transparent PNG assets generated with imagegen for Sumrak. Existing sprites remain unchanged.

- `environment.png`: 1536 x 1024 module atlas. Source rectangles are declared in `RuinRenderer`; tiles are stone ledge, arch, column, bridge, climbing root, and glowing moss.
- `islands.png`: six floating island variants used by the smaller desktop platforms.
- `wallpaper.png`: atmospheric background shown temporarily on every monitor while the ruins are active.
- `climbing.png`: 1448 x 1086 atlas, four equal columns and two equal rows. Top: without hat. Bottom: with hat. Columns: grip, alternating climbing poses, pull-up. `PetImages` normalizes the cells into an additional runtime atlas row; geometry lives in `PetSpriteCatalog`.

Actual alpha transparency is present, including the arch opening. Hidden RGB values in transparent pixels are not a backdrop. Keep the PNG alpha when processing these resources.

Desktop scale is 0.5. Scene geometry is independent of raster dimensions. Platforms, bridges and roots define collision and navigation; arches, moss and particles are decoration only.
