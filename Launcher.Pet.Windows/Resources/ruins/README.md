# Desktop ruins assets

Original transparent PNG assets generated with imagegen for Sumrak. Existing sprites remain unchanged.

- `environment.png`: 1536 x 1024 module atlas. Source rectangles are declared in `RuinsWindow`; tiles are stone ledge, arch, column, bridge, climbing root, and glowing moss.
- `climbing.png`: 1448 x 1086 atlas, four equal columns and two equal rows. Top: without hat. Bottom: with hat. Columns: grip, alternating climbing poses, pull-up. `PetImages` normalizes the cells into an additional runtime atlas row; geometry lives in `PetSpriteCatalog`.

Actual alpha transparency is present, including the arch opening. Hidden RGB values in transparent pixels are not a backdrop. Keep the PNG alpha when processing these resources.

Desktop scale is 0.5. Scene geometry is independent of raster dimensions. Ruin platforms and roots define collision and navigation; arches, columns, moss and particles are decoration only.
