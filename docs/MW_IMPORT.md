# Most Wanted (2005) fleet import

The local garage contains 30 U2 cars plus all 46 Racing registrations in the
installed MW PC CarTypeInfo table. Source ids use `mw-`; U2/MW selection, saved
builds and settings remain separate. Catalog capacity is 128; four cars can park.
Target: MW 2005 PC, not MW 2012 or console data.

## Complete export

Configure `mw_root` in ignored `config.local.json`. Install the normal
`requirements.txt` dependencies (Pillow/numpy) and configure vgmstream as for U2.

```powershell
# Export fleet/parts/vinyls/dependencies, verify, then promote complete results:
python tools/import_mw.py --promote
# Reuse completed car exports from the same generator version:
python tools/import_mw.py --resume --promote
# Audit existing staging without regenerating it:
python tools/import_mw.py --verify-only
python tools/build.py
python tools/test.py --skip-build --suite mw --suite carprofile --suite wheelsize --suite loading --suite menu
python -m unittest discover -s tests -p test_mw_import.py
```

`--source <folder>` overrides the configured installation. Individual inspection:
`export_mw_fleet.py --car A3` writes a partial staging catalog, so run the full
importer before promotion. `export_mw_car.py <car>` retains the historical three-preset diagnostic path; its `--all` now delegates to the complete importer.

Ignored `.local/mw-export/` contains:

- `cars.json`, per-car models/profiles/provenance and `styling/*.json`;
- 46 `menu/vinyl-NFVU2_MW_*.json` catalogs, with 5,306 original entries;
- `decals/`: 428 original RECT/WIDE logos and 793 native projection meshes,
  providing 775 independently editable positions across the 39 supported models;
- shared `audio/mwbank-*` banks: eight acceleration and eight coast loops per
  original stage, original RPM ranges, source hashes and generated WAV hashes;
- `original/`: complete unchanged car/shared packages, VPAK attributes, global
  textures and every linked GIN/ABK. This retains all source LODs, damage states,
  precomposites, decals and native part variants;
- `manifest.json`: source roster, failures and every dependency's SHA-256.

The importer checks archive/source hashes, runs the real C# loader on every car
and accessory catalog, validates wheels at slider limits, original axle dimensions,
all audio stages and every vinyl's coverage. Only generated JSON/WAV is promoted.
Raw BIN/BUN/GIN/ABK and provenance stay out of the mod payload and Git.

## Parts and runtime adaptation

Every car exports all native kits/hoods, the spoiler family matching its original
mount, roof variants when a mount exists, all 164 native wheel models (41 designs
in 17/18/19/20) and four shared brakes: 12,656 selectable part variants in total. Empty categories stay empty. Each rim keeps
its native bead and nominal size. Tyre outside diameter and colliders retain VLT
values. Rotors spin; calipers follow steering. Kit offset/width scales update both
preview and collider track. MW window tint is stored per vehicle and named build.
NOS uses original MW fire/smoke sprites with a 1x1 particle grid.

The MW Decals tab selects artwork independently for each original door, quarter
or window slot, above the four full-body vinyl layers. Projection geometry follows
the active bodykit and selections persist in vehicle settings and named builds.
The seven models without native slots keep that category empty. Original spoiler
supports accompany aftermarket spoilers; all non-driver/non-damage LOD-A car
geometry is covered by the factory, parts and decal exports.

The read-only PC VPAK reader resolves fixups, class layouts, optional fields,
arrays, inheritance and references. Unknown fields keep type/hash/bytes. The
provenance includes complete pvehicle fields and all linked engine/transmission,
chassis, tyre, brake, induction and NOS collections. The Unity controller and its
Stage 1-3 packages remain NFVU2 adaptations; exported tables do not execute MW's
physics, shops, unlock progression or career.

Vertical body alignment uses original arches; non-circular/service arches have
an explicitly recorded crown fit with 45 mm clearance. The cement truck's tandem
axle follows its simulated rear axle; handling keeps four contact points. MW-only spring/damper/wheel-mass scaling follows original vehicle mass so heavy service cars retain suspension reserve; the established U2/H2 controller is unchanged.
MW wheel anchors also account for the collider's rest position along its travel:
`distance * (1 - targetPosition)`. This removes the unintended 220 mm offroad /
132 mm road body lift while retaining full travel, springs, damping and the
separate 0–12 cm lowering adjustment. U2/H2 retain their previous anchor placement.
Unity's sprung-mass preload already supports gravity at rest; subtracting an
additional `mass*g/spring` would introduce a new height error.
SKIN/accessory paint is generated in MW. Camaro/RX-7 lack front-lamp markers and
the garbage truck lacks rear markers: their NFVU2 projectors are recorded body-fit
fallbacks; no missing lamp geometry is fabricated.

This source registers six service vehicles as Racing. Its archived BMWM3 model
lacks a pvehicle graph, so the explicitly marked BMWM3GTR attribute donor is not
claimed as original BMWM3 handling/audio. Shared BRAKES texture packages are empty
in the source installation. Matching shared U2 GlobalB brake atlases are used
as explicit donors when available locally: disc UVs match, including STYLE03,
and the common caliper atlas retains its alpha hardware and lettering. Provenance
records every donor. Without these optional donor atlases, the importer uses a
stock MW rotor texture and solid cast-metal hardware; unavailable decorative
billboards are omitted. Original car brakes retain their own textures. Unresolved required
texture references fail export.

Badge correction reconstructs continuous position/UV topology when source solids
duplicate triangle vertices. Adjacent atlas chunks and spaced single letters
retain word order, including slightly overlapping source seams. This changes UVs
only; the original chassis handedness, normals and triangle winding are retained.

MW vinyls retain original UVs, tint channels, coverage and fixed-colour art;
precomposites and standalone artwork are included. Indexed BGRA palettes are
converted to RGBA. U2 and MW catalogs remain isolated.
Carbon vinyl uses the imported carbon part's metallic/smoothness settings rather
than inheriting body-paint reflections. Original weave textures and UVs remain.

`tools/export_hud.py` also imports all eleven native MW tachometers from
`GLOBAL/HUDS_Custom_00..10.bin`, including authored numbers and needle pivots.
`HUDTEXRACE` meter arcs are unrelated artwork and are not used as tachometers.
Live readouts, boost and NOS placement remain NFVU2 adaptations. The eleven U2
designs retain their original artwork, with their needles aligned to the source
rotation pivot. `tools/render_hud.py` previews both sets without launching a game.

## Formats and evidence

MW solids use 104-byte material groups, effect-dependent 36/44/60-byte streams,
stream-local indices, DWORD padding and null-terminated names after a 160-byte
header. Original marker matrices attach shared parts. Textures use the version-2
TPK decoder. Bounded raw Deflate geometry/texture payloads are expanded and
validated by ModelFile before Unity allocation.

Primary references checked against actual local files:

- [NFS-ModTools MW solid reader](https://github.com/NFSTools/NFS-ModTools/blob/master/Common/Geometry/MostWantedSolidReader.cs): groups, effects and names.
- [Shared solid reader](https://github.com/NFSTools/NFS-ModTools/blob/master/Common/Geometry/SolidReader.cs): vertex stream/index interpretation.
- [Version-2 TPK reader](https://github.com/NFSTools/NFS-ModTools/blob/master/Common/Textures/Version2Tpk.cs): shared U2/MW texture format.
- [nfs-toolbox MW CarTypeInfo reader](https://github.com/krokettenkoal/nfs-toolbox/blob/master/NfsCore/Support.MostWanted/Class/CarTypeInfo/Functional/Disassemble.cs): the U2 `+0x120` locator rule cannot be reused.
- [NFS-CarToolkit author notes](https://nfs-tools.blogspot.com/2020/06/nfs-cartoolkit-v30-released.html): an existing manual-export alternative; the new importer does not require it.

- [VaultLib legacy reader](https://github.com/NFSTools/VaultLib): VPAK/AttribSys layouts.
- [NFSMWUnlimiter renderer](https://github.com/nlgxzef/NFSMWUnlimiter/blob/master/CarRenderInfo.h): VLT tyres, kit offsets, extra rear axle.
- [NFS-ModTools DDS header](https://github.com/NFSTools/NFS-ModTools/blob/master/Common/Textures/Data/DDSHeader.cs): indexed palette channel order.
- [Unity wheel setup](https://docs.unity3d.com/Manual/WheelColliderTutorial.html) and
  [Unity VehicleTools suspension](https://github.com/Unity-Technologies/VehicleTools/blob/master/Assets/Scripts/EasySuspension.cs): anchor/rest position and sprung-mass preload.

Blender QA uses `tools/render_mw_fleet.py -- all stock` / `-- all styled` through
Blender's `--background --python ... --` invocation. Renders use generated per-axle
and kit data, and remain separate from Unity evidence.

Developer-only `NFVU2_MW_AUDIT_REPORT=<report.json>` at launch checks every MW
material/mesh, active engine-stage playback, NOS construction, styling option and
GPU snapshots in a distant temporary scene, plus all native artwork and decal
slots. It also simulates real WheelColliders in an isolated local PhysX scene,
checking loaded hub/body height over road/offroad/drift and 0/12 cm lowering,
without opening a player/world.
Normal launches do not run this audit. Real menu and driving acceptance remain
separate from data/build/isolated-scene checks.
