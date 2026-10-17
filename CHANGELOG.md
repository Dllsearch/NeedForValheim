# Changelog

## Local development - vehicle alignment and garage presentation

- Correct complete split badge words, MW emblems and plates; preserve source
  geometry, alpha and surface orientation. Align U2 bodies and per-axle hubs.
- Preserve MW rotor alpha; adapt empty shared brake textures with original MW
  rotor art and cast-metal calipers; omit unavailable decorative billboards.
- Recover stalled entry paths at a revalidated free door capsule, with bounded
  walking time and door positions derived from each fitted cabin.
- Texture the buildable service bay; switch virtual U2 studio/MW warehouse
  scenery with the frontend; keep the fullscreen backdrop opaque.
- Add MW Race/Drag gauges with original local artwork and live telemetry;
  keep gauges above the actual hint panel and fit long hint labels.
- Add opt-in title-screen GUI/isolated PhysX checks. Screenshots require visual
  review; this does not claim player-controlled driving acceptance.

## Local development � Unified vehicle pipeline

- U2 badge overlays preserve source alpha and correct mirrored surface UVs,
  including rotated atlas islands and replacement body parts. Missing shared
  centre brake-lamp textures retain a red lens material and brake-light role.

- One U2 export command and one U2/MW preview command; removed per-model export/render entry points.
- Shared-pack wheels/spinners, source panel animation formats and pivots, engine
  variants, trunk audio and original exhaust styles now work through the shared U2 exporter.
- Preserved built-in chassis settings and the explicit DDAY visual preset.
- Generic fleet and vehicle-asset checks replace the H2 test bucket.
- Schema-3 exhaust supports one to four mouths; engine idle follows each profile.

## Local development — Built-in SUV renamed to h2, English source language

- The built-in SUV is addressed by its own identity now: car id `h2`, settings
  prefix `h2.`, asset folders `h2/` and `audio/h2/`, prefab `H2_Local_Prototype`
  and styling preset `NFVU2_H2_YELLOW_SPINNER26`. Behaviour keeps the capability
  name: `SuvTuning` (was `HummerTuning`), `IsSuv`, `SuvBands`,
  `VehicleOrbitCamera.Begin(target, isSuv)`. Renamed files:
  `src/HummerTuning.cs` → `src/SuvTuning.cs`, `tools/export_hummer.py` →
  `tools/export_h2.py`, `tools/render_hummer.py` → `tools/render_h2.py`,
  `tools/prepare_hummer_audio.py` → `tools/prepare_engine_audio.py`,
  `tests/hummer/` → `tests/h2/`. The display name "Hummer H2" and the short name
  "H2" are unchanged, and NFSU2 source identifiers (`CARS/HUMMER/`,
  `HUMMER_KIT00_*`, `GIN_Hummer.gin`, `CarTypeMapping/HUMMER`) are literal game
  data and stay as written.
- `tools/install.py` migrates a live install: `[Hummer.*]` sections in
  `local.needforvalheim.prototype.cfg` are rewritten to `[h2.*]` after a
  `config-before-sections.cfg` backup; a section that already exists wins. The
  legacy asset folders are removed only when provably empty, and `allowed_name`
  still accepts the old manifest names so the previous installed-file manifest
  can be backed up and deleted.
- UI source text is English. `src/InterfaceText.cs` is inverted into a
  418-pair English→German locale table with exact-match lookup first and
  word-boundary fragment replacement after it, so German remains a working
  locale; `Tr(text)` selects it from `Interface.Language`, which now defaults to
  `EN` and offers `English`/`German` in the controls menu. 187 literals were
  converted mechanically from the existing table and 58 composed strings by hand.
- 188 tracked files changed. `AGENTS.md` and `CONTRIBUTING.md` state the naming
  rule (identity `h2`, capability `suv`, never the car's name as a generic flag)
  and the English-source convention.
- Documentation translation is deliberately out of this change: the remaining
  German docs and translation constraints were recorded in the local
  `handoffs/TRANSLATION_HANDOFF.md` file.
- Automated: build green; full suite 82 tests with the one pre-existing
  `test_cabin_fit` error (optional third-party interior source) and 1 skip —
  identical before and after the language pass. Installed and SHA-256 verified
  2040 files; the installed plugin folder contains `h2` (2 files) and
  `audio/h2` (9), the installed cfg has 15 `[h2.` sections and no `[Hummer.`
  section. In-game load, garage spawn and the `English`/`German` toggle are not
  verified.

## Local development — Plugin renamed to NeedForValheim

- Rename `Rachel350Z.dll` to `NeedForValheim.dll`, the namespace `Rachel350Z` to
  `NeedForValheim`, the embedded audio resource prefix to `NeedForValheim.Audio.*`,
  the plugin id to `local.needforvalheim.prototype` and the install folder to
  `BepInEx/plugins/NeedForValheim/` — 224 files across `src`, `tests`, `tools`,
  `installer` and the docs.
- `tools/install.py` migrates the rename: saved settings are copied from
  `local.rachel350z.prototype.cfg` to `local.needforvalheim.prototype.cfg`
  (config categories do not contain the plugin id, so values carry over
  unchanged), the old cfg is kept, and the legacy `BepInEx/plugins/Rachel350Z`
  folder is moved into the dated backup and removed — BepInEx loads every dll
  under `plugins`, so leaving it would run the old plugin next to the new one.
- `AGENTS.md` and `CONTRIBUTING.md` state the new id and filename.
- `tests/test_menu_import.py` fixture fixed: `payload_files` reads `cars.json`
  to derive MW asset folders, so the fixture writes a valid catalog instead of
  opaque bytes. That failure predates the rename.
- Automated: 1021 catalog checks; full suite 82 tests with the one pre-existing
  `test_cabin_fit` error (optional third-party interior source) and 1 skip.
  Installed and SHA-256 verified 2040 files. In-game load of the renamed plugin
  is not verified.

## Local development — Underground garage

- Replace the floating settings window with a fullscreen Underground 2 frontend,
  original logo/vehicle badges, cut-corner controls and an independent 3D garage.
  Preview/tune cars before spawning; navigate with keyboard, controller or mouse.
- Add an NFVU2 GARAGE inventory button; move vehicle hints above the bottom strip
  to avoid overlapping the network diagnostics controls.
- Enable NOS kits on the full roster, export their original effect artwork and
  fit exhaust/purge emitters. Return to the car lot after removal, reuse the freed
  parking position and display/log rejected spawn reasons.

- Replace the crowded 30-car toolbar with a paginated car lot and parked fleet;
  add showroom orbit/zoom, saved garage recall and three named builds per model.
- Add real Stage 1–3 engine, turbo, gearbox, tyre, service-brake and NOS upgrades,
  while preserving stock handling, reverse limits and refill/input gates.
- Import up to 203 original body/rim options per car from the local game files;
  add live paint, finishes, rim colour and reversible procedural vinyls.
- Fix schema-3 wheel/material loading, saved rim sizing on newly selected meshes,
  per-model showroom settings and cargo guards when switching saved builds.
- Batch startup configuration binding; stage mesh swaps and release styling data
  with the car. Restore frontend artwork/sounds from the installed language data.
- Keep native zone streaming centred on the seated player while Player is disabled;
  publish the exit position immediately. Use neutral imported materials, discard
  SUV widebody window placeholders and oversized rim atlas backing quads.

## Unreleased — The whole NFSU2 roster through the same importer

- Import all **28 drivable cars** the game ships with usable data (Skyline,
  RX-7, RX-8, Supra, Miata, 240SX, Eclipse, 3000GT, Mustang GT, GTO, A3, Golf,
  Celica, Civic, Corolla, Corsa, Focus, RSX, Lancer Evo 8, Impreza WRX, IS300,
  G35, Tiburon, Sentra, Peugeot 206, Peugeot 106, Escalade, Navigator) with the
  importer unchanged in shape: one `SPECS` entry per car, 67–92 sections, 9–12
  textures and eight original engine loops each. The remaining 18 roster entries
  are traffic vehicles without their own wheel mesh or without wheel locators
  (BUS, FIRETRUCK, SUV, TT), so the importer cannot resolve them.
- Fix the geometry parser for the meshes that broke it: vertex records come in
  two strides, 36 bytes (position, normal, reserved, uv) and **24 bytes with no
  normals**; the index buffer decides which one a mesh uses. Material and
  texture slots use `255` for "unassigned", which normal-less meshes carry.
  Both were hard failures before (`3000GT_KIT00_ENGINE_B` did not parse).
- Fix wheel and brake classification for the whole roster: geometry headers
  truncate mesh names at 24 characters, so `_FRONT_WHEEL` arrives as
  `_FRONT_WHEE` and `_FRONT_BRAKE` as `_FRONT_BRAK`. Families are matched on the
  suffix, which keeps `_BRAKELIGHT` (a tail-lamp panel) in the body. Truncated
  brake discs had been silently dropped from ESCALADE, MUSTANGGT and PEUGOT106.
- Derive the spawn lift from the **chassis floor**, exhaust meshes excluded:
  NFSU2 hangs tailpipes under the floor and on COROLLA and NAVIGATOR they sit
  20 cm below it, which lifted those cars until their tyres floated above the
  ground plane. Tyre clearance across the fleet is now −0.255..−0.020 m
  (median −0.165), previously −0.255..+0.041.
- `tools/mod_files.py` ships all 28 model folders and their `audio/<car>/`
  loops; `tools/build_catalog.py` merges 28 profiles into `cars.json`, which
  stays under the runtime cap of 32 profiles.
- Automated: `--suite carprofile` now runs 215 checks over the shipped
  `cars.json`. Full suite: 62 tests, 1 pre-existing error (`test_cabin_fit`
  needs the optional third-party interior source) and 2 skips. In-game garage
  selection and spawning of the new cars remain to be checked.

## Unreleased — Six more cars through the same importer (no new code per car)

- Import **RX-7, Supra, Miata, 240SX, Eclipse GSX and Mustang GT** with
  `tools/export_car.py` unchanged: each car is one `SPECS` entry (paint tint,
  mass, gear ratios, RPM bands, rim range). Geometry, chassis numbers and engine
  sound all come from the game — 69–92 sections, 9–11 textures and eight
  original engine loops per car.
- Resolve each car's stock engine bank and RPM range from the game's own
  mapping chain (`CarTypeMapping/<CAR>.ini → CarDataMapping → EngineData`,
  community mirror `nlgxzef/NFSU2CarSoundTuner`). Banks are shared between
  cars, so `tools/extract_audio.py --gin <BANK>.gin` extracts by bank filename:
  SUPRA and IS300 both use the 350Z bank, 240SX and COROLLA share one. The
  Hummer check reproduces the shipped H2 profile exactly (engine 38,
  `GIN_Hummer.gin`, 1000–4782 RPM), which is what validates the chain.
- `tools/prepare_hummer_audio.py --engine <N>` records the `EngineData` index a
  bank came from in each loop manifest, so provenance is per car instead of a
  hard-coded Hummer reference.
- `tools/mod_files.py` lists imported cars in one tuple and ships each car's
  model folder plus its own `audio/<car>/` loops.
- Automated: `--suite carprofile` now runs 110 checks over the shipped
  `cars.json` (seven imported cars), validating every model against the runtime
  loader and every chassis against its own mesh bounds. In-game garage
  selection and spawning of the new cars remain to be checked.

## Unreleased — Second car through a generic NFSU2 importer (Skyline)

- Add `tools/export_car.py <CAR>`: a data-driven importer that builds a car
  from its own `CARS/<CAR>/GEOMETRY.BIN` families — body from `<CAR>_BASE` and
  `<CAR>_KIT00_*`, wheel and brakes from the same families (NFSU2 stores them
  per car, not in `CARS/WHEELS`), tyre radius measured on the rubber triangles,
  brake disc scaled to the wheel. Mesh families are matched by name prefix and
  the densest LOD, because geometry headers truncate long mesh names.
- Read the car's four wheel locators from the decompressed `GlobalB.lzc` at
  `NAME\0 + 0x120` with stride `0x30`; that table exists for all 44 cars and
  reproduces the values the H2 importer already used. Track width, front and
  rear axles, wheel radius and box sizes are derived from it plus the body
  bounds, so a new car's chassis numbers come from the game, not from guesses.
- Export as schema 3 (showroom, trunk and exhaust catalogs optional) and wrap
  UVs that tile past the loader's limit (the Skyline front bumper reaches
  u=203) into the unit square, which Unity's repeat addressing renders the same.
- Import the Skyline: 71 sections, 9 textures, its own wheel and brake meshes,
  plus eight original engine loops from `GIN_Nissan_Skyline.gin` (1524–8293
  RPM). `tools/extract_audio.py --car` and `tools/prepare_hummer_audio.py
  --car/--gin/--bands` generalize the audio chain; defaults keep the H2 run
  identical.
- Ship the catalog: each importer writes `.local/<car>/profile.json`,
  `tools/build_catalog.py` merges them into `cars.json`, and the build places
  that file next to the DLL, where the game reads it. `skyline/` and
  `audio/skyline/` join the payload allowlist.
- Fix the built-in H2 model path: the build ships `hummer/h2.json`, so the
  profile now points at `hummer/h2.json` instead of a root-level `h2.json`.
- Automated: the `carprofile` suite now loads the shipped `cars.json` and, for
  every listed car, requires a model file that the runtime loader accepts, a
  wheel-mesh radius equal to the chassis radius, axles inside the body and
  tyres within the body width (80 checks). `tools/render_imported_car.py`
  renders previews at the profile's wheel positions. In-game garage selection
  and spawning of the Skyline remain to be checked.

## Unreleased — Vehicle catalog (`cars.json`)

- Describe every vehicle as data in `src/CarProfile.cs`: chassis numbers,
  feature switches, acoustic gearbox and asset layout. The 350Z and Hummer H2
  are built-in profiles with their previous numbers, so existing behaviour and
  settings names are unchanged.
- Read `cars.json` from the plugin folder: entries override a built-in car by
  `id` and new ids are appended, up to 32 cars. An invalid, empty or oversized
  file is refused with one log line and the built-in cars are kept.
- Replace the hard-coded 350Z/H2 branching with profile data: drive-mode
  cycling, menu entries, nitro, turret and horde availability, cargo layout,
  rear access distance and rim size ranges now follow the selected car.
- Bind per-vehicle settings once per catalog car, so a new car gets its own
  prefixed sections (`silvia.Driving`) while the 350Z keeps the plain section
  names and the H2 keeps `Hummer.`.
- Automated: 64 new catalog checks (defaults, merge, per-car cargo and mode
  cycling parity with the old car switch, and rejection of invalid rigs, gear
  tables, ids and catalog files) plus the existing controls, storage, handling
  and horde suites. In-game spawning of a second car, garage selection, the
  unenforced `flight`/`neon`/`radio`/`exhaust` flags and the NFSU2 importers for
  other cars remain open; see [CARS_JSON](docs/CARS_JSON.md).

## Unreleased — Vehicle inner surfaces

- Remove black-backed ring decals that made the closed brake caliper bosses
  look like exposed pistons. Replace hub/bolt atlas squares with solid metal
  discs and separate the metal hub finish from red caliper paint.
- Replace the plain stock hood underside with a STYLE07-shaped graphite inner
  skin, reinforcement profiles, four matte pads and fasteners. This inner
  finish is authored for NFVU2; the original carbon exterior and hinge remain.
- Fit the donor dashboard and pillar fronts behind the original windshield
  and seal, including faces crossing the upper edge. Check clearance against
  all three engine variants and inspect closed/open hood previews.
- Preserve the original door edge returns when loading the detailed cabin.
  Split the original rear inner shell from the simplified cockpit so the
  hatch opening stays connected to the body with every audio enclosure.
- Restore the cabin's full A pillars and roof lining instead of cutting them
  off at one metre; fit the lining below the actual exterior roof. Verify
  the fit in external previews; native shading still needs an in-game check.
- Add reverse faces with separate inward normals to the imported body, wheels,
  brakes and cabin meshes, including roof, doors, hatch and glass. Existing
  pivots and wheel sizing apply to both sides. Audio membranes retain their
  original vertex layout.
- Check winding, normals, UV/color preservation and 16/32-bit index limits.
  Visibility, transparent glass and lighting still need an in-game check.

## 0.28.0 — Horde mode, minimap radar and vehicle storage

Public release highlights since 0.22.3 (Windows setup revision 6):

- New 350Z horde mode: five mixed enemy waves, a Moder finale, vehicle health,
  a shimmering hexagonal shield, colored powerups, combos and victory medals.
- Red enemy dots on the minimap, a localized start hint, and smooth transitions
  to stormy night at the start and clear daytime at the end.
- Randomized god pacts with benefits and drawbacks, including freezing ice
  ammunition and chain lightning. Choices open immediately between waves.
- Vehicle storage: 24 / 18 / 12 / 6 slots by 350Z trunk enclosure and 32 for H2.
- Fix radio dropouts and crowded-out turret audio, shield rendering over windows,
  distracting lightning tint flashes and excessive boss burst damage. Remove
  wave-long automatic fire from the finale's pact selection.
- Bound horde spawns and clean up corpses/projectiles without enemy loot buildup.
  The following development entries document the individual changes in detail.

Vehicle storage details:

- Add native chest inventory access via Vehicle → Open storage while parked,
  outside and near the rear. Each vehicle has an independent inventory.
- 350Z enclosures provide 24 / 18 / 12 / 6 slots; H2 provides 32. Block an
  undersized enclosure for any loaded 350Z; repack retained item instances.
- Save cargo through persistent vanilla chest ZDOs. Loaded cargo follows the
  car; removal/world exit restores a normal chest with its existing contents.
  Zone unloading never creates a replacement inventory. No custom save format.
- Show occupancy/capacity and localized German/English access guidance; opening
  the 350Z storage also opens its hatch. Keep driving/handling unchanged.
- Add automated storage lifecycle/resize checks. Native UI, world save/reload,
  chest placement and controller operation need in-game verification.

## 0.27.2 — Boss damage spikes and horde discovery hint

- Reduce encounter Moder's incoming damage by 45%, cap final hits at 180
  (225 enraged), and limit her multi-hit breath/projectile bursts to one
  accepted vehicle hit per second. Other enemy and collision damage is unchanged.
- Add an outlined gold/white German/English start hint below the minimap in a
  ready, grounded 350Z: “Face the horde / Numpad 9 · Begin the battle”. Position
  follows the native map and biome label; hide during gameplay UI and active runs.
- Verify boss burst protection, post-modifier caps, regular enemy damage and
  localization in automated checks. In-game boss balance and hint layout remain
  manual checks.

## 0.27.1 — Horde feedback and damage tuning

- Remove lightning's blue enemy tint flashes, retaining chain arcs and colored
  tracers. Ice keeps its freeze tint and thaw restoration.
- Exclude Valkyrie's Watch from the final wave's offers; retain three distinct
  choices with at least two changes from the previous wave. Temporary auto
  pickups remain available.
- Increase incoming enemy attack damage in horde mode by 20%. Pact and boss
  enrage multipliers still apply; collision damage is unchanged.

## 0.27.0 — Elemental ammunition and dragon finale

- Open pact selection immediately after a completed wave. Offer three of six
  pacts, ensuring at least two offers change between consecutive waves.
- Add Skadi's Winter: briefly freeze ordinary enemies with white-blue tint and
  icy tracers; reload 20% slower. Bosses are slowed rather than immobilized.
  An immunity window prevents permanent freezing; thaw/cleanup restores visuals
  and movement. Add Storm Chain: visible lightning arcs to up to three other
  nearby horde enemies, with falling damage, obstruction checks and a shared
  cooldown; direct bullet damage is reduced by 20%.
- Add Valkyrie's Watch: automatic turret for the whole wave, in exchange for
  25% more incoming enemy damage. Replace redundant ammo/auto pickups during
  corresponding pacts. All new UI supports German and English.
- Replace the troll finale with native Moder, two drakes and four Fulings.
  Retain native 7,500 base boss HP and attacks; add a half-health enrage and
  faster transitions back to flight. Extend auto targeting to 60 m in horde mode.
- Suppress the encounter dragon's progression flags, cinematic and loot;
  clean up its tracked projectiles and suppress persistent ice obstacles.
  Use a separate best-score entry for the changed rules.
- Automated checks cover offer diversity, immediate transitions, freeze/thaw,
  boss slow, material restoration, chain filtering/range/cover/cooldown, bounded
  arc visuals, expanded auto range and existing combat behavior. Native visuals,
  boss flight/navigation, attack balance and controller input need in-game testing.

## 0.26.0 — Mixed waves and pacts of the gods

- Replace the long forest-only encounter with five mixed waves of 15/20/18/14/13
  enemies: forest creatures, skeletons, Draugr, wolves, Fulings and berserkers,
  ending with a marked troll and boss health bar. Encounter members are allied.
- Choose Odin's Haste (40% shorter reload / 20% faster enemies), Thor's Wrath
  (wave-long explosive ammunition / 35% more enemy HP), Iron Chariot (double
  run-over damage / 25% less turret damage), or no pact before each wave.
  Choices support mouse, number keys and controller; temporary effects reset
  between waves and on exit, defeat, reset and world changes.
- Clear 85% of actual spawns plus every elite to advance; remaining small enemies
  despawn without score. Retain the eight-second break, then open pact selection.
- Waves 2–4 offer a marked elite hunt: 25 seconds for 500 points and 250 HP repair.
  Use a separate personal-best entry for the new wave rules.
- Add German/English selection, pact, objective and boss UI. Preserve existing
  shield, corpse cleanup, no-loot policy, minimap radar and weather transitions.
- Build and targeted automated checks cover scheduling, deadlines, pact expiry,
  reload time, impact damage, bullet/blast damage and prior horde integrations.
  Native AI, terrain navigation, UI input and difficulty still need an in-game test.

## 0.25.5 — Horde minimap radar

- Show living horde enemies as small red dots with a dark outline on the native
  minimap. Positions follow movement, map scrolling and zoom; off-screen dots
  are hidden within the minimap border.
- Reuse at most 100 UI dots. Remove dead/missing enemies from the display and
  dispose the radar on run end, reset or world exit. Respect the hidden HUD.
- Use a local UI overlay without saved/shared pins, icon-filter changes or
  exploration changes. Keep the large map and unrelated creatures untouched.
- Native build and projection/horde/weather checks passed; minimap appearance
  and HUD scaling still need an in-game check.

## 0.25.4 — Horde storm and night transition

- Starting a horde run transitions to midnight and native ThunderStorm weather
  over eight seconds. Victory, defeat, reset and other encounter exits transition
  back to noon and Clear weather, including after removing the car.
- Keep clear daytime for the current world session. Restore previous local
  environment/time overrides when leaving the world or unloading the plugin;
  never advance the saved world clock or carry overrides into another world.
- Weather lifecycle, restart/abort, world-switch and existing horde checks passed;
  sky/rain/lightning presentation still needs an in-game check.

## 0.25.3 — Shield over glass and turret audio priority

- Give the shield its own material, drawn after transparent cabin windows;
  retain world depth testing and leave pickup materials unchanged.
- Prioritize occupied-car turret shots over ordinary crowd sound effects,
  with quieter tails and servos below the shot attacks. Radio retains first
  priority; parked turrets return to ordinary sound priority.
- Verify bounded gunfire playback and priority transitions with adapter checks.
  Window compositing and crowd-combat audibility need an in-game check.

## 0.25.2 — Shield dome and radio during combat

- Project a connected hexagonal grid over a larger shield dome. A visible,
  gently shimmering baseline remains at any positive charge; hits brighten it.
- Rename the pickup to “Explosivmunition” / “Explosive ammunition” and remove
  “test” from horde reset/already-active messages.
- Give occupied-car radio the same audio priority as native Valheim music.
  Bound turret gunfire to four shot sources and two quieter tail sources,
  replacing stacked three-second one-shots that could crowd out the radio.
- Build, bounded-audio, radio, turret, localization and horde checks passed.
  Actual music continuity and dome appearance still require an in-game check.

## 0.25.1 — Horde cleanup and visual polish

- Keep horde entry restricted to a grounded 350Z with turret assets; H2 remains excluded.
- Track horde ragdolls separately: no saved loot, nonpersistent, three-second
  lifetime, maximum twelve and immediate cleanup on run end/reset. Ordinary
  world corpses and loot are unaffected.
- Replace gray horde HUD/pickup/result boxes with larger outlined text,
  slim colored health/shield bars, wave milestones and a combo countdown.
- Replace the shield rings with a subtle animated hexagonal shell that flares
  on shield hits and recharge. Health-only hits do not revive the shield effect.
- Build and adapter/logic checks passed; HUD layout rendered in DE/EN at
  720p/1080p. Native ragdoll cleanup and shield appearance still need an in-game check.

## 0.25.0 — Vehicle survival and powerups

- Make the five-wave horde a 350Z vehicle mode with 1,000 HP, 300 shield and
  off-road driving. Native enemy hits and hard obstacle collisions drain shield
  before HP. Route seated-driver hits to the vehicle while the mode is active.
- Add colored beacon/rune pickups: one 30-round explosive magazine, 15 seconds
  of automatic turret fire, 150 shield or 250 HP. Limit pickups to four; place
  them on clear dry terrain near the route and expire them after 45 active seconds.
- Add a shield visual, health/shield HUD, combo scoring up to x5, victory medals,
  survival/time bonuses and a local best score. Incomplete formations cannot set
  records. Losing ends the run and keeps the vehicle available for another try.
- Scope driving/turret overrides to the encounter, restore normal settings on
  cleanup and suppress other parked turrets during the run. No saved preference
  changes for temporary buffs. Exiting also cancels the encounter.
- Clear each spawned enemy's drop list as well as disabling direct drops, so
  native ragdolls cannot generate loot. Mark spawned enemies as test spawns.
- New logic and native callback adapter checks; in-game balance, collision
  behavior, HUD and beacon rendering still require playtesting.

## 0.24.0 — The forest strikes back

- Replace the 30-Greyling test with five waves: 60 Greylings; 90 Greylings;
  50 Greydwarfs/8 brutes/2 shamans; 40 Greydwarfs/10 brutes/1 troll;
  30 Greydwarfs/3 trolls. All enemies remain at no-star level.
- Eight-second breaks after clearing each wave, a wave/enemy/countdown HUD,
  five DE/EN attack announcements and a final victory message.
- Groups approach from the front and diagonally, following the driver's current
  position. Trolls spawn farther away with clearance based on their collider.
- Cap living test enemies at 100; bound placement work per frame and per slot.
  Abort completely blocked waves; keep reset, pause and world-change cleanup.
- Build and automated logic/adapter checks do not establish Unity gameplay or
  performance. The initial 30-Greyling test ran well on the user's PC.

## 0.23.0 — Local horde recording test

- Numpad 9 spawns up to 30 no-star Greylings in a forward arc, 30–45 m away,
  on clear, dry terrain. Works on foot and while driving; uses native hunting AI.
- Announce the attack in the selected DE/EN interface language. Spawn gradually,
  cap placement attempts and allow only one living test horde at a time.
- Numpad 0 removes this test horde and cancels pending spawns. Death, teleport
  and world changes clear it too. Test enemies are not saved and drop no loot.
- Menu, chat, pause and focus loss block the hotkeys and pause pending spawns.
  Vehicle handling, collision damage and turret damage are unchanged.
- Local build; Unity AI, placement, combat and PC performance await playtesting.

## 0.22.3 — Vehicle crosshair fix

- Hide Valheim's crosshair while seated in either vehicle, including when no
  turret is equipped. Restore it on exit; keep the turret's own aim marker.
- Windows setup revision 5 packages the crosshair fix; vehicle handling is unchanged.
- Add project skills for local installer builds and GitHub release publication.

## 0.22.2 — Rust menu badge fix

- Keep the Rust logo on the Turret menu entry when the menu is moved or scaled.
  Preserve the tilted sticker style without shifting it onto the Display entry.
- Add three animated in-game previews to the README: front, rear and flight mode.
- Windows setup revision 4 packages the updated mod; vehicle handling is unchanged.

## 0.22.1 — First public installer release

Windows setup revision 4 includes the complete radio playlist in Ogg Vorbis
format. The installer is approximately 148 MB.

### Windows setup and documentation

- Setup revision 4 explains that BepInEx loads mods, is required by NFVU2,
  is installed automatically when needed and should be kept for other mods.
- Retired the old prototype tag `v0.1.5`; reserve tags for actual releases.

- Setup revision 3 supports a smaller radio payload: prepare verified Ogg
  Vorbis copies at a 192 kbit/s target while retaining local WAV masters.
  The build preserves all tracks, titles and order, validates source/output
  checksums and rejects stale compressed copies. The encoder stays local.
- Installer regression checks cover WAV-to-Ogg updates, removal of obsolete
  owned WAV files and preservation of personal music files.

- Added an offline Windows setup EXE with game selection, installation,
  updates and removal; no Python or developer tools needed by players.
- Bundled BepInExPack Valheim for fresh installs; existing loaders are preserved.
- Added SHA-256 verification, recovery after
  interrupted operations and retention of two timestamped backups.
- Setup revision 2 adds the supplied logo to the wizard and README, renames
  the notices button to LICENSE and adds Close/Escape/Enter to its dialog.
  Radio music is always included and installed; removed the radio checkbox.
- Uninstall preserves settings and other mods. The loader can also be removed
  when this wizard installed it and no other plugins or patchers need it.
- Replaced the README with a short English player guide. Moved development
  requirements to CONTRIBUTING and kept detailed import/gameplay notes in docs.
- Package filenames now use the mod version instead of a fixed old version.
- No changes to vehicle handling, game controls or the plugin identity.

### Controller rumble, door warnings and nitro visuals

- Clear native camera shake while the vehicle/menu camera owns the view, so
  a shake cannot keep controller vibration running without its normal decay.
  Restore native rumble behavior on exit.
- Show blocked driver-door messages in the center of the screen, like spawn messages.
- Strengthen the nitro edge blur, set its depth test explicitly and smoothly
  widen the camera field of view during boost. Restore the original view on
  release, menus, pause, exit and effect removal.
- Avoid accessing an absent vehicle rigidbody during exit/world teardown.
- Document vehicle removal with a controller in the README.
- Automated checks cover logic/adapters; vibration and rendered appearance
  still need an in-game controller check.

Earlier entries below retain their original German wording.

## 0.22.0 — Turbo load, original boost gauge and small trees

- Small trees/bushes are hit at 0.25 m/s instead of 2.5 m/s; native
  contacts can
  still hit the plant through physics after braking. Directed
  throttle on actual
  contact frees a vehicle that is already stuck. Enemies keep their previous
  damage threshold; walls and large trees remain protected.
- Turbo and blow-off share a common load state. Event volume is
  captured before
  boost pressure decays; fixed low/high levels are removed. Import preserves
  relative sample levels with shared gain instead of RMS normalization.
- Analog throttle release over several frames also triggers blow-off; small
  trigger fluctuations do not. Menus, pause, exit and flight reset the turbo
  without triggering an artificial discharge.
- Original turbo gauges for all eleven NFS styles replace the additional
  speed display. Digital speed remains; H2 without a turbo hides the small
  instrument. Older HUD files remain readable.
- Vacuum and 0.42 s of decaying flutter augment needle movement. The
  boost/gain
  curve is NFVU2's own tuning, not a complete reconstruction of NFS trigger
  logic. See REFERENCES for evidence and scope.
- Offline audio comparison: partial and full load differ by about 8.3 dB;
  the turbo stays silent without engine load, with no clipped samples.
  Unity physics, actual mix volume and the in-game HUD still need checking.
- Build, 54 Python tests, 87 audio checks, 24 HUD/speed-display checks,
  59 collision checks and 31 loading checks passed. These are not
  Unity runtime tests.

## 0.21.0 — Menu navigation, drivetrain, turret and exhaust

- Categories and content have separate controller focus; the stick/D-pad
  navigate both areas. LB/RB jump between them; sliders and selection fields
  have an edit mode operated with A/B.
- Garage/settings remain visible when no car is available. Added the
  “by BCKWDZ”
  author line and an optional original Rust logo on the turret entry.
- Reduced normal ammunition from 35 to 14 piercing damage; explosive
  ammunition
  retains native wood/stone damage for large trees and mineable rocks.
- Increased engine torque and targeted launch assist forward/reverse;
  suspension
  and chassis geometry remain separate from audio and exhaust changes.
- Added an exhaust menu for sound variants of existing original samples and
  nitro flame color. Sound tuning and vehicle physics are NFVU2 rules.
- Ten original 350Z exhaust tips, STYLE01–10, from local NFS files; flame
  positions follow the actual openings. STYLE08 remains the default, and older
  model imports keep their existing tailpipes.
- Build, 53 Python tests and all 20 C# test suites passed. Blender checks of
  STYLE01/STYLE08 show correctly mounted single/dual pipes without overlap.
  Plugin ID and Rachel350Z.dll remain; controller, handling, sound
  and particle
  effects still need Unity gameplay tests.

## 0.20.5 — Vehicle prompts with Valheim controller glyphs

- Replaced native combat/build/fishing prompts in the vehicle with a
  two-column
  panel at the bottom right, using Valheim's font and existing
  Xbox/PS5 sprites.
- Added bindings for road, flight and the open mod menu; LB/L1
  descends in flight,
  and the turret is shown as automatic. H2 nitro is flight-only.
- Handles device changes, forced controller type, DE/EN and prompt size.
  Speedometer sits above the prompts; the previous in-vehicle text
  bar is removed.
- Exiting restores native prompts; inventory/pause retain theirs.
  Hiding the HUD
  also hides vehicle prompts.
- Glyph names checked in installed game assets; mapping and existing
  logic tests
  passed. In-game layout/controller check remains.

## 0.20.4 — Camera while the mod menu is open

- The right stick rotates the camera while the menu is open. With a
  mouse, hold
  and drag the right button; the wheel zooms while in the vehicle.
- On foot, native camera positioning with obstacle checks remains.
  Menu clicks,
  song-list scrolling and dragging do not rotate the camera.
- Driving, nitro and weapon actions remain blocked. Pause/focus loss
  ends camera
  control. Usage instructions appear in the menu (DE/EN).
- Build and logic checks do not replace an in-game camera/cursor test.

## 0.20.3 — Vehicle spawn as an in-game message

- After loading and placement succeed, “Spawned: Nissan 350Z” or
  “Spawned: Hummer
  H2” appears in the native yellow center display (DE/EN).
- Cancelled or failed loads show no success message.

## 0.20.2 — Drive mode as an in-game message

- Successful drive-mode changes by keyboard, controller and menu show a native
  yellow center message: “Drive mode: 'Off-road/Road/Drift/Fly'”.
- DE/EN follows the language setting. Unchanged selections and rejected flight
  starts show no change message; landing displays the selected ground mode.
  No extra entry is added to the message log.

## 0.20.1 — Lock the hotbar while controlling a vehicle

- Native hotbar navigation and item use are blocked while driving, flying or
  with the mod menu open; they are available on foot when the menu is closed.
- Inventory, durability and stack-size displays continue to update.
- Input-filter regression tests; in-game verification remains.

## 0.20.0 — Controller, direct actions and language

- Analog Xbox/PS5 control, L3 menu and B/Circle to enter/exit. Xbox: X nitro,
  A handbrake, LB turret; PS5 Square, Cross, L1. In flight: RB/R1 ascend,
  LB/L1 descend. The flight turret uses the existing target detection
  automatically.
- Select drive modes directly with 1–4 or D-pad up/down; H2 skips Drift.
  F7/B/Circle lands before attempting to exit in flight. Change songs with
  arrow keys/D-pad left/right; show the title after playback actually starts.
- Controller menu focus for buttons, toggles, sliders, selection fields and the
  song list; shoulder buttons switch categories, B/Circle goes back; mouse control retained.
- DE/EN in global settings, device prompts with a larger adjustable font;
  controller type can be automatic or manually set to Xbox/PlayStation.
- Separate input policy, release required after interruptions, targeted blocking
  of native player controls. Existing handling tuning remains.
- Automated logic tests; controller, UI and Unity gameplay tests remain.

## 0.19.0 — Headlight show after exiting

- F9 → Lights: “Show-off when exiting”, off by default and saved separately for
  the 350Z and H2. After exiting, flash left twice, right twice, then pause;
  2.4-s cycle with four 160-ms flashes. Left/right are from the driver's view.
- Light beams and emissive lamp surfaces follow the same phase. Sides are
  determined from vehicle coordinates rather than mirrored NFS part names.
- Entering the vehicle ends the show and enables normal headlights. Disabling it
  restores normal lighting; re-enabling starts on the left. Pause/focus loss
  freezes the phase. Each vehicle has an independent state.
- Color and intensity also apply to the show; engine/radio persistence is
  independent. Brake/reverse/taillights are unchanged. Reset turns show-off off.
- Automated sequence checks; verify visible beams, emission and menu in-game.

## 0.18.4 — Metallic paint

- Paint surfaces are explicitly marked during import. 350Z paint/vinyl and the
  yellow H2 body receive a fresh material with Metallic 0.72 and Smoothness 0.88;
  use the loaded standard shader with a verified native fallback. No inherited
  building normal/specular masks. Color and vinyl are retained. The carbon hood,
  windows, interior and rims keep their own settings.
- Import mapping checked automatically; verify gloss and reflections in-game
  in sunlight, shade and at night.

## 0.17.0 — Nitro tunnel vision and taillight trails

- Six radially offset image layers blur the screen edges during an actual nitro
  boost. A soft protected area keeps the vehicle projection and image center
  sharp; HUD/overlays are drawn afterward. GPU copy at half resolution, no CPU
  readback. The filter is active only during boost/fade.
- Two short red light bands follow the taillight positions. Soft cross-section,
  tapered end and about three meters maximum length. Depth test remains enabled.
  Active only with taillights on and an actual boost, including in flight.
- F9 → Nitro: tunnel vision and taillight trails can be disabled separately;
  shared strength 0–100%, default 70%. Settings are shared by both vehicles.
  Menus, pause, focus loss, exit, world/camera changes clean up the effects.
- Custom UVs, weighting, mask and trail timing; NFS shaders are not reused
  unchanged. The additionally reviewed yugecin source provides graphics toggles
  and parameter names, not a complete nitro/trail implementation.
- No change to vehicle controls, nitro thrust, tank or exhaust effects.
  Automated checks do not replace Unity rendering/GPU measurements.

## 0.16.1 — Exhaust nitro also works in flight

- The shared effects lock previously included all of flight mode, suppressing
  the existing 350Z exhaust flames and nitro sounds. Both now follow active
  nitro on the ground and in flight. They remain off without an actual boost;
  the four hover jets are independent.
- Tire smoke, purge and overrun pops remain blocked in flight. Menus, focus loss,
  exit and an incomplete entry still block exhaust effects. No change to thrust,
  tank or suspension.
- 41 nitro and 37 flight checks passed; particles and sound still need a Unity
  gameplay test.
- Investigated NFSU2 screen filters and light trails: separate graphics options,
  embedded D3D9 filters and a local flare texture were confirmed. Exact nitro
  coupling, filter weights and trail history remain unverified. Sources and
  limitations are in REFERENCES; no new screen filter has been added yet.

## 0.16.0 — Prepare vehicles in the background and spawn them incrementally

- File reading, JSON parsing, texture uploads, mesh construction and audio
  creation previously ran together in the spawn call. The optional interior adds
  about 24 MB of JSON. Model data and PCM sounds are now prepared on a worker;
  the JSON reader reads from the stream instead of making another full string copy.
- Loading display shows the current section and an animated bar while reading.
  Body, wheels, interior, turret, speedometer and engine clips are built in small
  steps with about a 3 ms budget per update. Individual Unity calls can exceed
  this budget; there is no hard frame-rate guarantee.
- Unchanged managed model/sound data is reused within the same world. Mutable
  Unity materials or meshes are not shared. File size/modification time
  invalidates the cache; changing worlds releases it.
- The car remains inactive and outside the vehicle list until completion. The
  spawn location is checked again before release. F8/cancel, death, teleport,
  world changes and load errors dispose partial resources. Existing vehicles
  remain independent; concurrent spawns and entry during loading are blocked.
  Missing optional modules keep their previous fallback.
- Automated checks cover background loading of real assets, cache/invalidation,
  cancellation, errors and resource finalization. In the .NET test, the first
  load took about 1.4 seconds versus about 4 ms from cache; these are not Unity
  frame times. The log now includes total duration and the longest main-thread step.
- Plugin ID, DLL name and VehicleHandling are retained. In-game test remains.

## 0.15.2 — Nitro input, flame origin and tire material

- Corrected flame origin from 48 to 26 cm from the wheel center; non-stretched
  particles are retained.
- User still reports missing flight nitro. Installed 0.15.1 and active nitro
  configuration were confirmed, but the existing log has no input data. In
  addition to Unity Legacy Mouse, the code now reads Valheim's existing
  `ZInput.GetMouseButton`. W + left Ctrl is an alternative in flight, with
  visible boost/tank/release status and limited diagnostics. This does not claim
  that the specific runtime failure has been reproduced.
- The original 128×128 Racing Hart atlas contains the tire tread and sidewall;
  texture and UV mapping were already present. The 350Z rubber color reduced
  their visibility to 16%. It is now 80%, non-metallic and rough, using a fresh
  material instead of inherited prop settings. No new tire assets.
- Tests cover native input when the legacy mouse value fails, keyboard
  alternative, release lockout, flight thrust and existing ground controls;
  tires were rendered. Actual input and effect position need an in-game test.

## 0.15.1 — Flight speed, thrust direction, air nitro and speedometer needle

- User confirms flight mode. Ascend speed is now up to 12 m/s, manual descent up
  to 16 m/s, and automatic landing up to 10 m/s with braking before touchdown.
  The previous two-meter target limit reduced vertical speed.
- The visible side of the rims now faces up when folding. Wheels, brakes and
  spinner tilt with thrust. Flames point backward in forward flight, forward in
  reverse and toward the opposite side while steering; the tilt eases back on release.
- Moved jets below the wheels; regular rather than stretched particles prevent
  flame sprites from extending backward past the outlet. Emission starts near
  the end of folding so flames do not pass through the body.
- W + left mouse button activates nitro in flight: higher forward speed and
  thrust, with tank use and recharge. The 350Z shares its ground/flight tank;
  H2 has separate flight nitro. No extra ground nitro activation during jumps.
- More visible RPM needle above other HUD layers and an additional analog speed
  display with matching KM/H/MPH scale, alongside the NFS instrument.
- Build and targeted flight, nitro, HUD, handling, H2 and audio checks; HUD
  preview rendered. New driving and effects still need Unity verification.

## 0.15.0 — Flight mode with folding wheels

- F9 → Vehicle → Fly for 350Z/H2: animated 90° wheel folding, gentle takeoff and
  position-holding hover. W/S forward/back, A/D turn, Shift ascend, Space descend.
  Maximum forward speed is about 65 km/h.
- Four blue wheel jets from the existing NFS-FIREB atlas; a locally sourced,
  lower-pitched nitro loop with thrust modulation and purge sound as an optional
  fallback. No new original assets; custom flight control and animation.
- A separate flight controller handles forces and temporarily disables the
  WheelCollider. The proven VehicleHandling controller is unchanged; gravity,
  damping and wheel colliders are restored on landing/teardown.
- Selecting a ground mode starts a controlled landing followed by wheel
  unfolding. F7 and menu exit wait for it; menu exit then resumes the regular
  exit sequence. F8/death/world change retain a teardown path.
- Nitro/burnout and tire sounds are blocked in flight. Menus/focus loss neutralize
  input; hover stabilization remains active. Each vehicle has its own flight
  state; new vehicles start on the ground.
- New state/controller checks use controlled physics responses. Actual flight
  behavior, collisions, particles and audio require Unity testing.

Build, full test suite (49 Python tests and 546 C# checks including 32 flight
checks) and wheel geometry preview verified. No Unity runtime tests.

## 0.14.3 — Polished chrome/gold rims

The rim lip was only 28% metallic and the star 22%. An extra orange-gold color
was multiplied with the already-gold original texture. Both surfaces now use
fully metallic materials with high smoothness; the star gets only a slight,
cooler correction to its original color.

Rims use a fresh standard material when its shader is available, otherwise a
fresh instance of the existing vehicle shader. No inherited prop textures or
emission; reflections and highlights are enabled. Wheel renderers use Valheim's
existing Reflection Probes. The shader choice is logged. Geometry, tires and
vehicle physics are unchanged. Build and 142 model, interior and H2/garage checks
passed; rim detail was also rendered in Blender. The exact appearance under
Valheim lighting needs an in-game test.

## 0.14.2 — Red tint in window tinting

The previous material selection could copy `lavalanternglass_mat` or its broken
variant. Both contain a red emission color and the active shader keyword
`_EMISSION` in the local game assets. Tinting changed only the base color and
left that emission intact.

The window material now uses a fresh material with the loaded shader reference;
foreign textures, colors and keywords are no longer copied. Emission is also
explicitly disabled. Tint colors are neutral gray. The selected shader reference
is logged for diagnostics. Build and interior checks passed; in-game appearance
still needs checking.

## 0.14.1 — Driver pose, open window and window tint

- User confirms the 0.14.0 interior and entry/exit; the head went through the
  roof, hands were too far forward and steering animation was reversed.
- Lowered and moved the seat back slightly; added a limit based on the head bone
  with clearance for the head/hair under the roof.
- Moved the right wrist back on the wheel and corrected its rotation and hand
  orientation. The left arm rests on the open window; it moves inward when the
  door opens or the driver exits.
- F9 → Doors: saved window tint, 0–100%. The physically left side window is
  hidden while seated and shown again after exiting.
- The left door is manually operable again after entry: the menu button unlocks
  it and the seat animation no longer overrides its state. Door movement is
  available in the menu once seated; entry/exit keep their lockout.
- Build and targeted driver, door and handling checks passed. New pose, window
  effect and equipment need another in-game test.

## 0.14.0 — Local interior and visible driver

- Optional local BeamNG 350Z interior import: cockpit, two seats, pedals, gear
  shifter and trim attached to the NFS doors. Separate steering-wheel rotation;
  transparent windows use an available game shader.
- F7 while stationary: walk to driver's door, open it, transition to Valheim's
  sitting animation, place hands on wheel and feet on pedals, close door and
  enable driving. Reverse on exit; a clear floor area and room for the player
  are required.
- Input/nitro/turret are blocked during the transition. Menu/focus loss pauses
  the sequence; removal, errors and world changes restore the player.
- Native sitting animation plus custom transitions/limb alignment; no BeamNG
  character animation reused. Visual seat scale is 85%. H2 and existing handling
  tuning are unchanged.
- Import, model data and state sequences checked automatically; external model
  preview reviewed. Unity gameplay test, including clothing intersections,
  remains open.
- Original author of the provided Modland archive is unknown. Assets remain
  local in ignored folders; no permission to redistribute is claimed.

## 0.13.1 — Movable vehicle menu

Move the F9 menu by dragging its title bar with the left mouse button. Position
is saved and clamped to the visible screen when the resolution changes.
Closing/focus loss ends dragging. The close button and content sliders remain
independently usable. Existing vehicle binding for categories remains: without
a vehicle, only the garage is active. Build and menu tests checked; verify actual
dragging in Unity in-game.

## 0.13.0 — Lowering, NFS menu and parking automation

- F9 → Wheels: added lowering slider, 0–12 cm in 0.5-cm steps, saved separately
  for H2/350Z; physically offsets suspension points. Reset restores stock ride
  height; spring/damper and drive-mode tuning are retained.
- Front wheels retain their actual steering angle when exiting. Re-entry resumes
  smoothed steering; parking brake stays active.
- New F9 layout with category bar, original NFSU2 icons, selected original
  German titles, keyboard navigation and three frontend sounds. Local import
  with `tools/export_menu.py`, optional media folder `menu/`. Layout/actions and
  sound roles are adapted; this is not an NFS menu emulation.
- An enabled 350Z turret automatically guards after exit: visible laser search,
  forward 180° sector, 30 m range, hostile creatures, barrel tracking, controlled
  line of fire and automatic reload. Toggle under Turret; independent of engine/
  radio persistence. Laser color, intensity and ammunition carry over. Players
  and tamed animals are not targets.
- Menus, focus loss and player death block automation; after manual control,
  held RMB remains blocked until released. Each turret has its own search,
  ammunition and movement. H2 turret is still not configured.
- Checked with build, import/payload tests, handling and turret state tests, and
  controlled line-of-sight API responses. No Unity gameplay test.

## 0.12.0 — Hummer H2, spinner rims and garage

Original H2 from the game's own NFSU2 files: stock body in yellow,
26-inch SPINNER_STYLE01 with separate original inserts, 34 cm wide tires,
and suspension, brakes and launch force tuned for the heavier SUV.
The inserts spin up with a delay and coast down after stopping. Rim size is
22–28 inches; tire outer diameter is retained. Original wheel positions from
the HUMMER global data, with NFVU2 vehicle physics. Taillights use the matching
stock texture with separate red/white areas; the two atlas regions for the rear
HUMMER lettering are mirrored together.

Dedicated stock sound from GIN_Hummer.gin: eight RPM ranges, 1000–4782 RPM and
four audible forward gears. The original stock data uses no second Ginsu
throttle-release channel; the mod plays the same Hummer sound more quietly on
throttle release. No renamed or pitch-shifted 350Z recordings.

F9 → Garage is available even without a car. Model selection and spawn button;
F6 spawns the saved selection. Up to four independent temporary cars, searching
for free adjacent spaces and never replacing the previous car. On foot, F7/F9
select the nearest car; F8 removes only that car. While driving, selection stays
with the driver's car. Spawning is available only after exiting. Model/load
errors during spawning preserve existing cars.

Each car has separate physics, meshes, materials, audio, radio, nitro and turret
state; camera/player control and music ducking remain centralized. Lights, radio,
wheels and drive mode have separate H2 configuration entries; existing 350Z
entries remain valid. Cars of the same model share settings. Camera/speedometer
remain shared. The H2 camera is higher and farther back.

Initially, H2 has no engine/trunk customization, door animations, nitro or
turret. Radio, lights, neon, burnout and collision systems are reused. Vehicles
are not saved; this remains local single-player. Automated checks and Blender
previews do not replace a Unity gameplay test.

## 0.11.0 — Brighter adjustable laser and moving subwoofers

Wider laser with a bright core and larger impact point. Under F9 → Turret →
Laser, set 25–400% intensity (default 200), choose from seven color presets or
use RGB sliders. Color and intensity are saved and apply to both beam and point;
obstacles still stop the laser.

The radio analyzes the bass content of the samples actually playing without
changing them. Separate channel analysis avoids cancellation in out-of-phase
stereo. The bass envelope drives visible cone movement up to 14 mm in both
directions. The enclosure and outer rim remain fixed. Each speaker position has
its own animated mesh copies; amplifiers and LCDs do not move. Also applies to
the radio after exiting. Pause, radio off, zero volume and missing audio data
let movement decay; changing tracks resets the displacement. This is bass
visualization, not a sample-accurate speaker simulation.

## 0.10.0 — Free aim view, Rust laser and explosive ammunition

In turret mode, the camera pivot is higher and rises further when aiming steeply
downward. The reticle is at 62% of screen height above the bottom edge; the
barrel aims at the world point behind it. Orange indicates lag, tilt limit or a
blocking vehicle body. Normal driving camera is unchanged when the turret is
off. Ammo type and count appear at bottom left.

Original Rust LaserSight entity model, both material areas and emitter point
imported locally and mounted under the AK. Red beam and impact point follow the
barrel and stop at obstacles; can be disabled in the menu. F9 → Turret:
Normal/Explosive. Explosive adds small area damage against enemies to the direct
hit, with distance falloff and cover checks, plus a pooled particle/light flash
and Rust's `explosive-ammo-close` sound.

Each direct ground hit with explosive ammunition lowers a circular area of 1.4 m
radius by up to 18 cm at the center, removes grass and saves through Valheim's
terrain compiler. Repeated hits dig further up to the normal digging limit.
Hits on rocks, buildings or creatures do not dig underneath them. Craters persist
in the world after the temporary car is removed. Models/samples are original;
laser beam, particles, effects and craters are NFVU2. Additional tests cover
terrain hits, tile boundaries, ownership and repetition.

## 0.9.1 — Aim marker and roof mounting corrected

The gun follows the camera view direction from the barrel. With the orbit camera,
the old screen center aimed at the car. A single high-contrast reticle now shows
the projected actual impact point; lag and tilt limits remain visible. Ammo is
at bottom left. Valheim's own crosshair and bow reticle are hidden while
controlling the turret and restored on exit, disable or removal. Moved the
turret 20 cm back and 7.5 cm down so the supports sit on the curved roof.
Geometry preview updated.

## 0.9.0 — Rust AutoTurret with AK on the roof

New F9 “Turret” tab, off by default, with a saved toggle and separate volume.
Local import of original AutoTurret/AK world models, five diffuse textures and
seven Rust sounds. Original rotation/tilt pivots, AK upright with barrel facing
forward; scaled to the roof surface. Removed the cover plate for the empty weapon mount.

Mouse look controls rotation and tilt with limited lag. The aim marker shows the
actual impact point. Hold right mouse button for automatic fire: 30 rounds,
2.5-second reload, unlimited reserve ammunition. Raycast up to 150 m, with 35
piercing damage before Valheim resistances against hostile creatures. Rocks,
buildings and the vehicle body block shots. No firing at players, tamed animals
or structures. Shot variants, outdoor tail, servo loop, activation and reload
sounds play spatially.

Custom recoil animation, muzzle flash, light flash and short tracer. No Rust
scripts or character animations reused. Firing only from the driver's seat;
after focus loss, menu or exit, release is required. Missing/invalid optional
assets disable only the turret. Handling, nitro and existing controls remain.
Build and automated checks plus Blender geometry preview; Unity gameplay test
still open.

## 0.8.0 — NFS speedometers, dynamic camera and drift setup

Eleven original designs from HUD_CustomTextures_00 through 10, with 7500-RPM
scale and needles, plus NOS graphics from InGameRace.bun. Local import preserves
alpha and removes unused texture margins. Display at bottom right: speed,
audible RPM/gear, nitro tank and boost color. Menu → Display: design, size,
color, KM/H/MPH and visibility; simple text fallback without assets. Layout,
needle sweep and NOS fill animation are Valheim adaptations.

Camera follows a smoothed world angle instead of rigidly following vehicle
rotation. Limited influence from movement direction and a look-ahead target for
corners/drifts; reverse does not turn the camera around. Mouse look pauses
automatic following for 2.2 seconds; no recentering while stationary. Can be
disabled and its strength adjusted in the display menu. Obstacle checks remain.

Optional drift mode in addition to off-road/road: rear-wheel drive, less rear
lateral grip, wider steering angle, faster countersteering, 20% more torque than
road mode and less traction control during slip. Space is the rear-wheel
handbrake; menu/exit/focus loss brakes all wheels. Original tire sound and smoke
on slip; W+S burnout remains available. Drift parameters are separate; switching
modes restores the previous base values. Checks and external HUD preview do not
replace a Unity driving/camera test.

## 0.7.2 — Exterior music with doors and hatch open

Actual animation states control volume, low-pass filter and position of the
spatial radio source. The existing damping remains when closed (30%, 1400 Hz);
an open door lets more music out, and a fully open hatch reaches 85% and 12 kHz.
These are mod tuning values. The source moves to the open side of the car or to
the trunk; multiple openings produce a weighted position with a capped overall
level. Existing stream continues without restarting or duplicating tracks.
Interior playback, user volume, pause, focus/menu locks and persistence toggle
are retained. Hood is excluded. Automated mix checks do not replace a listening
test in Valheim.

## 0.7.1 — Missing upper taillight inserts

Import had brought in only the lower orange strips from the shared STYLE04 lamp
material. The six upper triangles on each side were missing, allowing trunk neon
to show through the taillights. Both upper inserts are now imported with their
original geometry and texture. Separately modeled clear outer covers remain
open. Neon position, lighting controls and handling are unchanged. Regression
checks verify all triangles are retained without duplicates. Blender preview
checked; confirm appearance in Valheim.

## 0.7.0 — Trunk builds, door animations and engine bay

F9 → Build: stock plus three original 350Z enclosures with 2/4/6 positions.
Each position has a cover, four subwoofer designs, four amplifiers and four
LCDs. Original AUDIO geometry, AUDIO_SKIN texture and MARKER_AUDIO_COMP
matrices; component size chosen to fit the geometry. Default: KIT02 with four
SPEAKER1 subwoofers. This is a mod preset, not a verified part of Rachel's
preset. Separate trunk neon with seven colors, on/off, static or pulsing display
and reset. Light strips and pulse pattern are custom mod effects; LCDs remain static.

F9 → Doors: left/right doors, hood and hatch animate open while stationary.
Normal/suicide/scissor doors use original quaternions and hinge points, plus the
original lateral translation for scissor doors. Playback is normalized to one
second; door mechanics switch only after closing. Mirrors, door trim, hood
undersides and spoilers follow their groups. Moving off/entering closes parts
automatically. No new door colliders; drivetrain and WheelCollider are unchanged.

Choose from three original engine models with UNLIT inner surfaces under the
hood. The base model has only simple interior geometry; no detailed cockpit was
found. Windows remain opaque. New metadata is optional for older models;
imported audio meshes are reused when parts change. Build, 322 automated checks
and Blender visual checks; verify Unity appearance/controls in-game.

## 0.6.0 — Run over targets and keep the vehicle running after exit

At collision speed of 2.5 m/s or higher, vehicle hits cause native Valheim
damage and knockback to hostile living creatures. Players and tamed animals are
excluded. Small birch/pine trees and four bush variants use native chop damage
with normal destruction effects and drops. Large trees, rocks, structures and
planted saplings are excluded. Brief vehicle-volume preview plus collision
contacts; each target has 0.8 seconds of hit protection. Damage and knockback
are capped; high health/armor still helps. Handling and drivetrain are unchanged.
Build now also references the locally installed assembly_valheim.dll without
including it.

F9 → Vehicle: “Keep running after exit” is on by default and saved. After at
least one entry, the engine idles, and enabled neon and radio remain active.
The toggle also works while away from the car; radio pause and individual
neon/radio switches remain effective. Outside, the radio is spatial, attenuated
to 30% and low-pass filtered at 1.4 kHz; it fades out by 28 meters. Valheim
music is suppressed only while driving. Exterior engine is capped at 55% and
30-meter range. Focus/UI pause and removal still clean up all sources.

Build and 297 automated checks, including 41 new hit adapter tests, passed.
Unity physics, in-game damage handling and audio still need in-game verification.

## 0.5.0 — Stronger launch, reverse sound and burnout

Forward launch torque increased by 20% up to 3 m/s, then linearly reduced to the
previous level by 6 m/s. First audible gear ratio rises from 3.79 to 4.15; other
gears and top speeds are retained. Reverse transmission whine is a new synthetic
loop with speed-dependent pitch/volume. No clearly attributable original reverse
sample was found in the file inventory and event search. Reverse light: intensity
1.6 → 0.45, range 6 → 4 m, emission 2 → 0.7.

W+S starts a burnout below 0.65 m/s when both rear wheels and at least one front
wheel are on the ground. Front axle brakes; rear axle receives torque and
temporarily reduced longitudinal grip. Ends above 2 m/s, on loss of contact,
release, Space, menu or exit. Normal friction values are restored; nitro is
blocked during W+S. Engine holds first gear under high load during burnout.
Original SKID_PAV sound and SFX_SMOKE_B_CS smoke atlas respond to actual rear
wheel spin. Smoke position also follows visual wheel offset. Particles are
limited and cleared when blocked/removed. Effects, audio and controls remain separate.

256 automated checks passed; actual WheelCollider physics, smoke rendering and
audio need an in-game test.

## 0.4.1 — Top-left menu and adjustable headlights

Menu sits 16 pixels from the upper-left corner and scales in small windows to
leave room below for driving prompts. F9 → Lights offers 14 color presets, an
intensity slider from 0 to 300% and reset to stock/100%. Selection and intensity
are saved. Headlight beams and emissive front lamps share color and intensity;
taillights, brake lights, reverse lights and license plate light retain their
previous behavior.

Preset names come from local original files LANGUAGES/English.bin, German.bin
and Labels.bin. RGB values and the 1.45 factor for extra-bright presets are
NFVU2 adaptations for Unity. Build and 232 existing automated checks passed.
Menu appearance and actual lighting still need in-game verification.

## 0.4.0 — Camber, wheel offset and rim size in the menu

F9 → Wheels provides separate camber (−15 to +10 degrees) and lateral offset
(−10 to +20 cm per wheel) for the front/rear axles. A shared slider changes
nominal rim size from 16 to 22 inches relative to the NFS 20-inch source model.
Tire sidewall is adjusted while retaining the 70-cm outer diameter. Changes
apply immediately and are saved in the configuration; reset restores all five
default values.

Camber is applied before wheel rotation so tilted wheels do not wobble while
rolling. Brakes follow camber/offset; calipers remain independent of wheel
rotation. Rim changes use the unchanged source vertices instead of cumulative
scaling and adjust normals. Offset is mirrored per side. This is visual only:
wheelbase, contact geometry and VehicleHandling are unchanged. In-game test of
controls, extremes and lighting remains open. 232 automated checks passed;
external 16/22-inch previews checked.

## 0.3.1 — Rear lettering aligned correctly

The rear 350Z lettering UV mapping is corrected horizontally so it reads from
behind. Only its two triangles are affected; the shared original atlas and other
emblems are retained. Close-up before/after export checked; in-game test remains open.

## 0.3.0 — Nitro, purge and two-tone rims

Hold left mouse button: forward driving with throttle and ground contact gives
extra thrust and green flames from the four measured tailpipes; while stationary,
bright purge jets appear at the bumper and hood. Uses original SFX_FIREB/
SFX_SMOKE_B_CS atlases and two loops from Nitrous_00_MB.abk. Loop-role mapping,
purge nozzle positions and Unity particle animation are documented approximations.
Six-second tank, two-second recharge delay, ten seconds of charge while driving.
Extra thrust is separate from the confirmed VehicleHandling. Menus, focus loss
and exit stop effects; menu clicks do not trigger boost.

F9 has nitro settings and optional flames, off by default, when releasing the
throttle after high load. NFS has sputter/blow-off effects; exact tuning
conditions and the correct pop substream remain unresolved. Therefore, no
unverified backfire sound is presented or installed as original.

The original rim is split into chrome lip and gold star without losing geometry.
Adjusted material values prevent the previously almost-black metal surface.
The texture parser no longer skips actual leading 0x11 pixel bytes. External
rim preview and 222 automated checks passed; Unity driving, particle and audio
tests remain open.

## 0.2.1 — Taillights, wider tires, sport brakes and track titles

User confirms basic operation of 0.2.0 in Valheim. Missing orange strips under
the STYLE04 taillights and rear bumper inserts are imported again. Inner bumper
surfaces glow white in reverse and illuminate the ground behind. S while moving
forward remains brake for now; exiting ends reverse lights. The bumper surface
division is a documented approximation. Orange strips are passive; turn signals
are not implemented yet.

Tire width increased from about 19.5 to 28 cm; 70-cm diameter, outer position
and handling retained. Original STYLE03 sport brakes with red calipers and
38-cm discs. Rotor surface was approximated as round because the original alpha
mask was square; its original atlas is retained. Discs rotate with the wheel;
calipers follow steering only.

Radio shows 27 actual artist/track names from the EA Trax table. MPF events,
original EXE strings and WAV checksums support the mapping. 16 other container
streams remain local but no longer appear as radio tracks. Build/package/installer
include only the active playlist. Changes were rendered externally and tested
with 176 automated checks. User then also confirmed operation in Valheim.

## 0.2.0 — Vehicle menu, radio and lighting

F9 opens a scalable vehicle menu for drive mode, radio, lights and neon.
Input/camera lock and audio pause are separate; radio remains audible while
using the menu. Doors/trunk are explicitly disabled preparation for now. Local
stereo music loads on demand, with track selection, next/previous, pause and
volume. Original music comes from SDAT.VIV; the 43 imported music streams use
neutral track numbers until their mapping is confirmed.

Headlights, taillights and license plate light turn on upon entry and off on exit.
Brake lights also respond to braking. Original LICENSE_PLATE texture uses its
own rectangular geometry; placement is approximate. Original 350Z_NEON mask
with ground-aligned projection, color selection and custom pulse animation.
Shader appearance and light levels still need checking in Valheim.

Audible gear shifts happen earlier off-road; shift interruptions are 140/120 ms.
Wheel slip raises pitch without advancing the selected gear. Original TURBO_MED_2
recordings add boost and blow-off; mapping and triggering are documented
approximations. VehicleHandling remains separate. Installer/package support
external media, file backups, SHA-256 and rollback on copy errors. User later
confirmed basic operation in Valheim; follow-up fixes are described in 0.2.1.

## 0.1.9 — Texture upload during spawn

The 0.1.8 gameplay test failed with `LoadRawTextureData: not enough data provided`.
Textures have a mipmap chain, but the model file contains only level 0.
`SetPixelData<byte>(pixels, 0)` now loads that level specifically; `Apply(true, true)`
then generates the smaller levels. User subsequently confirmed the appearance in Valheim.

## 0.1.8 — Corrected actual tire diameter

Import previously used the diagonal of a square rim backface as the radius.
This made the visible tires in 0.1.7 only about 52 cm across, despite a stated
72 cm. Now only the original tire surface is measured: actual 70 cm diameter at
front and rear, about 35% larger than before. Overhanging backface triangles are
excluded. Two regression tests check the source radius and exported tire. Preview
also includes side and low perspective views for comparison with the reference
image. Unity gameplay test remains open.

## 0.1.7 — Wheel proportions and placement

Wheel scale increased (diameter figure later corrected in 0.1.8), wheels moved
farther out and centered longitudinally in the widebody arches. Wheel and brake
visuals follow the suspension together; their offsets do not rotate with the
wheels. Body visual sits 4 cm higher to make room for the larger wheels.
Colliders, drivetrain and suspension parameters remain as before. External
preview shows an approximate compressed position instead of collider suspension
points. Clearance during steering and compression still needs checking in Valheim.

## 0.1.6 — Rachel's original parts and vinyl

The original `DDAY_PLAYER_CAR` preset from `GlobalB.lzc` determines widebody 02,
carbon hood 07, spoiler 02, lights, Racing Hart wheels and exhaust tips. Original
UVs, normals, material regions, textures and `350Z_ART_F_6` vinyl, including its
mask palette, are prepared locally. Wheels follow the existing WheelColliders;
calipers do not rotate with them. Vehicle physics and sound are unchanged. Model
and textures remain ignored local data. Build, 83 mod checks and three asset
checks passed; external preview rendered. Unity gameplay test remains open.
Paint/rim tint are approximations; window transparency, lamp covers and license
plate remain open.

## 0.1.5 — Vehicle sound

Original NFSU2 recordings for acceleration and throttle release, eight RPM
ranges each, custom RPM/shift logic, rolling and skidding sounds, and a
synthetic horn. Sounds are spatially attached to the vehicle. User confirms
basic operation; audio refinement remains open.

## 0.1.4 — Launch behavior restored

Engine power is independent of wheel contact reports, physics activates on input
and brakes release fully when accelerating. User confirmed: the car drives again
and off-road mode works very well.

## 0.1.3 — Camera and off-road mode

Mouse camera with zoom and obstacle distance; switchable off-road/road mode.
Camera and mode switching confirmed; launch issue later fixed in 0.1.4.

## 0.1.2 — In-game appearance

Use existing Valheim material with a loaded shader. User first confirmed spawning,
driving and entering/exiting.

## 0.1.1 — Load the model

Use Newtonsoft.Json for model data instead of the unsuitable Unity JSON loader.

## 0.1.0 — First prototype

Extracted stock body from NFSU2: 16 parts, 3.867 vertices, 3.449 triangles.

Git history begins with the project import based on 0.1.5. Earlier versions are
documented here; previous local backups are not Git tags.
