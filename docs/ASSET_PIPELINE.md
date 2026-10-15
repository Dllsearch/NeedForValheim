# Local asset pipeline

Detailed import notes retained in German from the former README.
Start with [CONTRIBUTING](../CONTRIBUTING.md) for prerequisites and build steps.
All source assets, tools, exports and binary packages stay ignored locally.

## Generate local data

Run all the following commands from the project directory:

```powershell
python -m pip install -r requirements.txt
python tools/extract_assets.py
python tools/export_car.py 350z
python tools/export_car.py h2
python tools/export_car.py SKYLINE
python tools/build_catalog.py
python tools/prepare_engine_audio.py
python tools/extract_audio.py --car Skyline
python tools/prepare_engine_audio.py --car skyline --gin GIN_Nissan_Skyline.gin \
    --bands 1500,2500,3500,4400,5400,6400,7300,8300
python tools/export_hud.py
python tools/export_menu.py
python tools/import_rust_logo.py --download
python tools/extract_audio.py
python tools/decode_audio.py
python tools/prepare_audio.py
python tools/extract_audio.py --turbo-only
python tools/decode_audio.py --turbo-only
python tools/prepare_audio.py --turbo-only
python tools/prepare_effects_audio.py
python tools/extract_music.py
```

First extract the official vgmstream decoder locally and enter its path in
`config.local.json`. The importer reads only the required data from the CD images;
it does not run an NFS installer.

The radio loads local stereo WAV/OGG files from the `radio/` subfolder beside
the DLL. The import creates `.local/radio/tracks.json` with 27 verified EA Trax
songs and their actual artist/title names. MPF and audio checksums validate the
mapping in `tools/music_titles.json`; other container streams are not offered as
songs. Existing imports can be updated with
`python tools/extract_music.py --refresh-titles`. Then,
`python tools/prepare_radio.py --ffmpeg path/to/ffmpeg.exe` creates smaller Ogg
Vorbis copies under `.local/radio-ogg/` (target bitrate 192 kbit/s). WAV masters
remain unchanged; songs are fully decoded again and checked for stereo, sample
rate and duration. After checksum verification, the build automatically uses the
compressed playlist. Repeat Ogg preparation after reimporting or changing titles.
Turbo samples are stored separately in `.local/audio-turbo/`, Nitro/Purge sounds
under `.local/audio-effects/`; the build, package and installer include these
media folders. The 19 short engine/driving sounds remain embedded in the DLL.
The H2 import uses the same local CD images and existing geometry/texture reader.
It creates `.local/h2/h2.json` and eight original engine loops under
`.local/audio-h2/`. The build and installer include them as `h2/` and
`audio/h2/` beside the DLL, whose name remains unchanged.
`tools/render_vehicle.py -- h2` creates a local preview with Blender.

### Shared vehicle pipeline

`tools/export_car.py <CAR>` reads a vehicle completely from its own
`CARS/<CAR>/GEOMETRY.BIN` families: body from `<CAR>_BASE` and
`<CAR>_KIT00_*`, native wheels/brakes or configured shared packs, including separate spinner meshes, rim diameter from tyre triangles,
brake scaling at the wheel. The four wheel locators are in the unpacked
`GlobalB.lzc` at `NAME\0 + 0x120` with a stride of `0x30`; they provide
track width and front/rear axle positions. The export writes schema 3
(`.local/<car>/<car>.json`), including original panel animations, engine
variants, four trunk layouts, shared audio components and exhaust styles where mounted, and also `.local/<car>/profile.json` with
suspension values from locators and bounding dimensions.

`tools/build_catalog.py` combines all `profile.json` files into
`.local/cars/cars.json`; the build places the file beside the DLL, where the game
reads it.
`tools/prepare_engine_audio.py --car <car> --gin <GIN> --engine <N> --bands …`
creates original engine loops in the same format. `tools/extract_audio.py`
needs the **bank name** (`--gin GIN_Mazda_RX7.gin`), not the vehicle name:
NFSU2 shares engine banks; for example, SUPRA and IS300 use the
350Z bank. `--engine` is the `EngineData` number from which the bank comes and
is recorded in the provenance. `tools/render_vehicle.py -- <car>` renders the
preview using the exact wheel positions from the profile.

All **28 drivable vehicles** in the roster have been imported (67–92 sections,
9–12 textures, 8 engine loops each): Skyline, RX-7, RX-8, Supra, Miata, 240SX,
Eclipse, 3000GT, Mustang GT, GTO, A3, Golf, Celica, Civic, Corolla, Corsa,
Focus, RSX, Lancer Evo 8, Impreza WRX, IS300, G35, Tiburon, Sentra, Peugot 206,
Peugot 106, Escalade and Navigator. Vehicle data is stored in a
`SPECS` entry per car; the importer itself is shared by all vehicles.
Not exported: decals, engine bays, trunk audio and body kit variants.

Two format details were needed for completeness:
Vertex records come in two strides — 36 bytes (position, normal, reserved, UV)
and 24 bytes without a normal; the index buffer determines which applies since
it fits only one of the two sizes. Mesh names in geometry headers are truncated
at 24 characters, so `_FRONT_WHEEL` appears as `_FRONT_WHEE` and `_FRONT_BRAKE`
as `_FRONT_BRAK`; families are therefore identified by suffix rather than
substring — `_BRAKELIGHT` is a rear light strip and belongs to the body. Material
and texture slots use `255` for “unused”, which meshes without normals lack.

The spawn hub is calculated from the **bottom of the body**, excluding exhaust
meshes: NFSU2 mounts tailpipes underneath, and on COROLLA and NAVIGATOR they hang
20 cm below the bottom, which would have lifted the whole car above its tyres.

### Optional 350Z interior and driver

Extract the locally supplied ZIP “Nissan 350Z FIX UKBEAMNG” to
`.local/interior-source/beamng-350z/`; it must contain
`vehicles/Nis350Z/Nis350Z.dae`. Only models, textures and material data are
processed; no BeamNG scripts are run.

```powershell
python tools/export_interior.py
python tools/build.py
python tools/test.py --skip-build
```

The import creates the optional mod payload `interior/interior.json`:
62 submeshes, about 41,000 triangles and 15 embedded textures. Two missing source
textures receive simple dark replacement materials. The roof and trunk remain
from the NFS model; overlapping interior surfaces are replaced.
`tools/render_interior.py` creates local review renders with Blender.

F7 while stopped: the character walks to the left driver's door, it opens, the
character visibly gets in and the door closes. Throttle, nitro and turret are
enabled only afterward. F7 to exit requires the car to be stopped and clear space
beside the driver's door. F8 removes the car and immediately restores the player.
Menus/focus loss pause the transition. The H2 is unchanged.

Since 0.14.1, the driver sits lower; the measured head position limits the
height beneath the roof. The right hand steers, while the left arm rests by the
open driver's window. The left arm retracts when the door opens. The visible
steering wheel rotation has been corrected; the right hand follows the same
rotation. F9 → Doors provides saved window tint from 0% (clear) to 100% (dark)
for closed 350Z windows. While stopped, doors can also be operated from the seat,
after entering/exiting has completed. Opening the menu while seated does not
pause door movement; the doors close when driving off.

0.14.2 removes inherited red self-illumination from a possible lava-lantern
glass template. The window material uses only the loaded shader and neutral gray
values without self-illumination.

The visible driver uses Valheim's existing chair-sitting animation, a custom
movement sequence and procedural limb alignment. The character visual is scaled
to 85% while seated; the player, inventory and equipment remain the same. The
animation is not an entry animation copied from BeamNG. The pose, clothing,
window shader, door styles and collisions need in-game checks.

Source: [Modland — Nissan 350Z FIX
UKBEAMNG](https://www.modland.net/beamng.drive-mods/cars/nissan-350z-fix-ukbeamng.html)
UKBEAMNG](https://www.modland.net/beamng.drive-mods/cars/nissan-350z-fix-ukbeamng.html)
UKBEAMNG](https://www.modland.net/beamng.drive-mods/cars/nissan-350z-fix-ukbeamng.html).
The original author is not identified on the download page; permission to
redistribute the assets has not been established. The archive, extracted files
and generated interior remain ignored local files.

### Optional Rust import

In the ignored `config.local.json`, set the `rust_dir` key to your Rust
installation. The importer reads its bundles unchanged and places only the
selected models, textures and eight sounds in `.local/turret/`:

```powershell
python -m pip install --target .local/tools/unitypy UnityPy==1.25.4 numpy
python tools/export_rust_turret.py
python tools/build.py
python tools/test.py --skip-build
python tools/install.py
```

Rust is required for import, not while driving. Without imported assets, the
turret menu item remains disabled with an explanation. The reader supports the
locally verified UnityFS bundles with uncompressed payloads; other bundle
variants are rejected. Original files, tools, exports and local packages remain
ignored and do not belong in the Git repository.

`tools/prepare_engine_audio.py` prepares GIN engine loops for any vehicle.

### Export, preview and validate all vehicles

```powershell
python tools/export_car.py all
python tools/build_catalog.py
python tools/build.py
python tools/test.py --skip-build --suite fleet --suite vehicle_assets
blender --background --python tools/render_vehicle.py -- h2 --open 1 --view rear
blender --background --python tools/render_vehicle.py -- 350z --door-style SCISSOR --open 1
```

Use `--output <root>` for isolated export fixtures and
`tools/test.py --vehicle-assets-root <root> --suite vehicle_assets --skip-build`
to load them with the actual C# model loader. Rendering accepts `--root`,
`--layout`, `--engine`, `--exhaust`, `--view wheel` and `--paint <composite.png>`.
It uses embedded textures, supports compressed MW models and extra axles, and
poses panels around their own source pivots. Audio equipment stays on the trunk floor.
The DDAY 350Z assembly remains an explicit visual preset (`vehicle_preset.py`);
its fitted parts, artwork and hood repair are preserved, not copied onto other cars.
Shared wheel/brake/exhaust conversion is in `vehicle_geometry.py`.
Showroom lighting for schema-3 vehicles is fitted to their own trunk bounds.
These checks and Blender previews do not execute Unity physics or audio playback.

When exporting into `.local`, existing styling catalogs are refreshed together
with their base model so mesh replacement names and wheel fit cannot drift.
Shared rim conversion preserves original authored wheel textures; visual size
comes from export provenance, including adapted tyres and shared wheel packs.
Use `--audio 1,2,5,9,3,4` to preview individual trunk components and
`--save-blend` to retain an editable scene with embedded images.
