# Vehicle catalog (`cars.json`)

The plugin builds its vehicle list from two built-in profiles (350Z, Hummer H2).
A `cars.json` file next to `NeedForValheim.dll` extends that list: entries with a
known `id` replace the built-in profile, unknown ids are appended. Without the
file, with an empty file, or with a file that fails validation, the plugin logs
`cars.json ignored: <reason>` and keeps the two built-in cars.

`src/CarProfile.cs` holds the model, its validation and the built-ins. It uses
only plain fields and Newtonsoft.Json, so it is testable without Unity
(`python tools/test.py --suite carprofile`).

## File rules

- Top level is a JSON array of car objects.
- Read with `TypeNameHandling.None`, a maximum depth of 32 and a 4 MB size
  limit; text after the array is rejected.
- Maximum 128 cars. An empty catalog, a catalog without the `350z` entry and
  duplicate ids are rejected, because spawning, storage and the garage menu
  assume the stock car exists.
- One invalid car rejects the whole file; there is no partial merge.

## Car object

| Field | Required | Default | Effect |
|---|---|---|---|
| `id` | yes | — | Garage value in `Garage/SpawnModel`, config prefix, F6 spawn. Letters, digits, `-`, `_`, at most 32 characters. |
| `sourceGame` | no | `u2` | Car-lot tab: `u2` or `mw`. Older catalogs default to U2; MW imports use independent `mw-` ids. |
| `displayName` | no | `id` | Loading progress, vehicle name, garage list. |
| `shortName` | no | `id` | Fleet counter line in the garage. |
| `prefabName` | no | `<id>_Local_Prototype` | Unity `GameObject` name of the spawned car. |
| `configPrefix` | no | see [Settings](#settings) | Overrides the automatic section prefix. |
| `summary` | no | `displayName` | Description text in the garage. |
| `rig` | yes | — | Chassis numbers, see below. |
| `sound` | yes | — | Acoustic gearbox, see below. |
| `capabilities` | no | all false | Feature switches, see below. |
| `assets` | no | see below | File layout next to the DLL. |

## `rig`

| Field | Validation | Used by |
|---|---|---|
| `boxCenterX/Y/Z`, `boxSizeX/Y/Z` | sizes > 0; offsets finite and abs ≤ 10 | Chassis `BoxCollider`. |
| `mass` | > 0 | Rigidbody. |
| `trackWidth`, `wheelHeight`, `frontAxleZ`, `rearAxleZ`, `wheelRadius` | width/axles/radius > 0; offsets finite | Wheel collider positions and radius. |
| `spawnLift` | finite, ≥ 0 | Vertical offset when placing the car. |
| `clearanceX/Y/Z` | finite, abs ≤ 10 | Free-space test around the spawn point. |
| `trunkApproachZ` | finite, abs ≤ 10; `≤ 0` becomes 1.3 | Standing point behind the car for storage access. |
| `rimNominal`, `rimMin`, `rimMax` | nominal inside min/max | Rim slider range and wheel scaling. |
| `spinners` | — | Loads `spinner` mesh groups as separate spinner transforms. |

## `sound`

The acoustic gearbox is independent of the physics; it drives engine pitch,
the tacho and gear display.

| Field | Validation |
|---|---|
| `gearRatios` | 1–8 positive values. Forward gears only; reverse is synthesized and not taken from the table. |
| `rpmBands` | 2–16 strictly ascending values; one engine recording pair per band. |
| `minRpm`, `maxRpm` | `minRpm < maxRpm`. |
| `downShiftRpm`, `upShiftRpm` | `downShiftRpm ≥ minRpm`, `upShiftRpm > downShiftRpm`, `upShiftRpm ≤ maxRpm`. |

## `capabilities`

`showroom` decides the cargo layout: a showroom trades cargo rows for audio
(6 columns, 4/3/2/1 rows by enclosure), a car without one keeps the full
8×4 floor. `turret` also gates horde mode, because horde mode starts from the
roof turret.

| Flag | Wired today |
|---|---|
| `showroom`, `turret`, `nitro`, `drift`, `interior`, `suvEngine`, `suvTuning`, `turbo` | Yes: storage layout, turret menu/horde, nitro systems, drive-mode cycling, cabin import, engine sample source, SUV suspension, tacho turbo gauge. |
| `flight`, `neon`, `radio`, `exhaust` | Declared only; flight, neon, radio and exhaust visuals still run for every car. Enabling these flags is roadmap work. |

## `assets`

Paths are relative to the plugin folder.

| Field | Default | Content |
|---|---|---|
| `modelFile` | `body.json` | Required model export; see [Model requirements](#model-requirements). |
| `audioFolders` | `audio/effects`, `audio/turbo` | Folders scanned for `.wav` files. |
| `engineFolder` | unset | When set, engine recordings are read as `<folder>/engine_<band>.wav` for each `rpmBands` value. When unset, the engine pairs embedded in the DLL are reused, and those exist only for the 350Z bands `1400 2100 2800 3500 4300 5100 5900 6500` — a car with other bands needs its own `engineFolder`. |
| `engineStageFolders`, `engineCoast` | unset, false | Original MW stage banks, each with matching `engine_<band>.wav` / `coast_<band>.wav` loops. Include those folders in `audioFolders`. |
| `decalFile`, `decalSlots` | unset | Optional native MW projection model and stable slot keys. Artwork comes from `decals/mw-logos.json`; absent/corrupt optional assets retain the base car. |
| `interiorFile` | `interior/interior.json` when `interior` | Cabin import; optional, a failure only logs `Cabin unavailable`. |
| `turretFile`, `turretFolder` | `turret/turret.json`, `turret` when `turret` | Turret model and its audio folder. |
| `hudFile` | `hud/hud.json` | Original gauge graphics; without them the text dashboard is used. |
| `flameModelFile` | the car's own model | Model that supplies the nitro/flight flame atlas. |

Turbo clips are not profile-driven yet: `audio/turbo/charge.wav`,
`blowoff_low.wav` and `blowoff_high.wav` are shared by every car with
`turbo`. Road, skid and horn samples are embedded in the DLL.

## Settings

Each catalog car gets its own copy of the per-vehicle settings in
`BepInEx/config/local.needforvalheim.prototype.cfg`:

- `350z` keeps the plain sections (`Driving`, `Wheels`, …), so existing
  installs are unchanged.
- The built-in H2 keeps its historic `h2.` prefix (`h2.Driving`).
- A new car `silvia` gets `silvia.Driving`, `silvia.Wheels` and so on.

`configPrefix` overrides that rule; `Garage/SpawnModel` stores the `id`.

## Example

```json
[
  {
    "id": "silvia",
    "displayName": "Silvia S15",
    "shortName": "S15",
    "rig": {
      "boxCenterY": 0.72, "boxSizeX": 1.68, "boxSizeY": 0.78, "boxSizeZ": 3.65,
      "mass": 1250, "trackWidth": 0.82, "wheelHeight": 0.3, "frontAxleZ": 1.25,
      "rearAxleZ": 1.3, "wheelRadius": 0.33, "spawnLift": 0.35,
      "clearanceX": 0.3, "clearanceZ": 0.5, "trunkApproachZ": 1.3,
      "rimNominal": 18, "rimMin": 15, "rimMax": 20
    },
    "sound": {
      "gearRatios": [3.8, 2.2, 1.5, 1.0, 0.74],
      "rpmBands": [1200, 1900, 2600, 3300, 4000, 4700, 5400, 6100],
      "upShiftRpm": 5400, "downShiftRpm": 1800, "minRpm": 900, "maxRpm": 6500
    },
    "capabilities": { "showroom": true, "interior": true, "radio": true, "neon": true, "nitro": true, "drift": true, "flight": true, "exhaust": true, "turbo": true },
    "assets": { "modelFile": "silvia/body.json", "audioFolders": ["audio/effects", "audio/silvia"], "engineFolder": "audio/silvia" }
  }
]
```

## Model requirements

A car file is a NFVU2 export, not a generic mesh: `schemaVersion` 0, 2 or 3,
part groups `body`, `wheel`, `brake`, `spinner`, embedded textures, and the
schema-2/3 showroom catalog (audio enclosure and door/engine variant data).
`tools/export_car.py <id>` produces this format and a profile for all 30 supported
U2 vehicles; `all` exports the roster. Built-in identities and settings prefixes stay stable.

Importing a different NFS Underground 2 car needs that export path for the new
car first. `tools/export_car.py <CAR>` produces it generically — body families,
the car's own wheel and brake meshes, wheel locators and chassis numbers from
the global record — and writes the matching catalog entry as
`.local/<car>/profile.json`; `tools/build_catalog.py` merges those into the
`cars.json` the build ships. The Skyline is imported this way; cars without a
model export still cannot appear, so `cars.json` alone is not enough.
See [ROADMAP](ROADMAP.md) for the current state of that work.


### MW wheel fit metadata

`rig.rearTrackWidth` optionally overrides rear half-track; zero retains the front half-track, preserving existing U2 profiles. Generated visual parts may specify `axle: "front"` or `"rear"` (wheel/brake/spinner only); omitted axle renders on both. `wheelRubber: true` in a ModelMaterial identifies tyre rubber independently of U2 material hashes. Rim sizing requires a sidewall with distinct bead and tread radii.


## MW extensions

`sourceGame` is `u2` by default or `mw`; ids/settings stay independent. Optional
`rig.rearTrackWidth`, `rearWheelRadius` and `rearWheelHeight` use the front value
when zero. `tyreWidth`/`rearTyreWidth` and styling-kit offsets/scales keep the tyre
outer face aligned after selecting a wider body. `extraRearAxleOffset` adds the
MW tandem visual axle while retaining four simulated contacts.

`assets.engineStageFolders` lists the original engine banks by upgrade stage;
`engineCoast=true` loads separate `coast_<rpm>.wav`. `engineFolder` remains the
legacy/stock folder. U2 profiles retain their previous defaults. MW's original
VLT performance collections and unknown fields are exported in private provenance;
the existing NFVU2 upgrade controller is the runtime adaptation.

Packed schema-3 parts may provide `vertexCount`, `indexCount`, `packedGeometry`
in place of arrays (raw Deflate: interleaved 8 little-endian floats, then uint32
indices). Textures may provide `rgbaDeflate`. The loader validates expanded size,
indices and finite coordinates. Native wheel parts carry `rimInches`, `wheelBead`
and `wheelOutside`; style options carry `rimInches` and optional kit track/width
metadata. Libraries allow 512 options, 256 ids per category and 2,048 parts.

SUV-specific presentation follows `capabilities.suvTuning`, rather than the `h2` ID.
Model IDs, asset paths and config prefixes continue to identify individual vehicles.
