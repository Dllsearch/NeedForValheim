# Layered vinyls

Open **Customize → Vinyl**. Select one of four layers, then browse the design
catalog by category and page. Later layers cover earlier layers; **Colours /
opacity** controls three independent mask colours and transparency for each layer.
Artist artwork, flags and sponsor logos retain their original palette. Choosing
**None** removes an extra layer; **Factory** on layer 1 restores the installed
original appearance when the other layers are empty. **Clean body** removes the
factory artwork without changing the selected paint or finish.

Each of the 30 U2 models has its own catalog, about 870–890 original variants:
tribals, flames, lightning, tears, stripes, liquid, modern, wild, flags, hood/body
designs, artwork and sponsors. A/B/C entries retain the game's original placement
variants. These are original per-car masks and coverage, not shared decals stretched
over every vehicle. Car UVs are preserved, including bodykit and door boundaries.
Original mask layouts fit their corresponding U2 car; carbon hood textures remain
carbon. The two legacy generated designs are baked into the existing UV atlas from
car-local geometry; side checkers include height instead of collapsing to a line.

All four layers and their colours/opacity are saved per vehicle and included in
named builds. Existing 0–3 design settings and the original paint/rim settings are
retained. Native artwork IDs are stable across catalog regeneration.

MW models use separately exported native catalogs through the same format.
Catalog source must match the preset: `mw` for `NFVU2_MW_*`, `u2` otherwise.
Original U2 masks are not applied to MW cars.

## Local preparation

```powershell
python tools/export_vinyls.py
# Narrow export:
python tools/export_vinyls.py miata imprezawrx
python tools/test_vinyls.py
```

Use the project's Python dependencies (Pillow, NumPy). The batch decoder is built
with the configured .NET SDK and checked against the existing U2 texture decoder.
It supports the original BGRA indexed palettes, converted to RGBA with alpha
preserved, plus Huffman and JDLZ streams. An independent known-colour fixture
checks channel order in both decoders and both Python texture-pack layouts. Catalogs go
to ignored `.local/menu/vinyl-<preset>.json`, with raw-Deflate RGB/coverage data and
64px thumbnails; the existing build/install menu allowlist ships them. No original
BIN files are packaged. Native compositing runs off the Unity thread, and only
the finished selected 512px texture is uploaded to the GPU. Carbon, glass, lights,
chrome, wheels and unrelated textures are excluded from the vinyl paint surfaces.

Validation checks original coverage and thumbnail data for every variant,
independent colour channels, fixed art colours, layer order/opacity, decompression
bounds, UV-preserving procedural projection and the packaged-file allowlist.
Those are not Unity rendering tests; a game check is still needed after installation.
