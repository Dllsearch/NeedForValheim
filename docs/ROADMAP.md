# Upcoming tasks

## Garage station, world safety and MW frontend (local build)

- View-directed safe spawning; seated hover hints hidden; exit fall history reset
  with narrow own-car collision grace. Parked cars hold their last supported pose
  while terrain/objects are unloaded and resume after stable loaded support.
- Native buildable NFS Garage, 10 Wood; prototype model ownership and new
  performance levels cost 1 Coin each, with free recall and paid-level reuse.
- Actual player avatar and adapted entry/controls across all 76 models; native
  map markers, optional ground dust, grime and wet car paint.
- Separate U2/MW menu designs; 27 U2 + 26 MW songs, station filters and saved TRAX
  selections with random starts and shuffled queues.
- Build/managed checks are separate from player-controlled Unity acceptance.
  Details: [station](GARAGE_STATION.md), [driver](DRIVER.md), [TRAX](TRAX.md).

## Most Wanted 2005 — complete local vehicle lineup

- 46 racing entries from the installed CarTypeInfo table, separate `mw-` IDs
  and U2/MW tabs; 76 vehicles total. Four parked vehicles remain the limit.
- Original bodies, all available kits/hoods, matching spoiler families,
  roof variants, brakes, and 41 rim designs in all four native sizes.
  5,306 MW vinyls use the separate layer system; 428 original logos
  cover 775 individual sticker positions on 39 vehicles.
- VLT axes, tire width and radius per axle, kit track offset, mass,
  transmission/RPM values, and original GIN banks with boost and engine stages.
  Cement mixer tandem axle, transparent windows, and saved tint.
- NFVU2 performance stages, including NOS/intake, remain the driving adaptation;
  original VLT tuning and all source packages are in the ignored archive.
  This does not simulate MW driving physics/career.
- End-to-end importer and validation limits: [MW_IMPORT](MW_IMPORT.md).

## Underground Garage (local development status)

- 30 vehicles with paginated selection, fleet selection, preview camera,
  saved Garage list and three named builds per vehicle model.
- Real NFVU2 performance stages for engine, turbo, transmission, tires, brakes
  and NOS; stage 0 retains the confirmed handling.
- Original parts catalogs for all 30 vehicles: stock/widebody,
  bumpers, side skirts, hoods, spoilers, and branded rims; up to 203 options
  per vehicle. Paint, surfaces, rim color, and custom vinyl patterns.
- Schema 3 vehicles now use their original materials and wheels.
  Trunk/door/window settings are also bound per vehicle;
  storage size changes check instances of the same vehicle model only.
- Controls, customization limits and import: [GARAGE](GARAGE.md).
  Original NFS career, shops, dyno, and complete decal layers remain open work.

## Data-driven vehicle profiles and `cars.json`

- Vehicles are described as data (`src/CarProfile.cs`): suspension values,
  feature flags, acoustic transmission, and asset paths. The 350Z and H2 remain
  built-in profiles with their existing values.
- `cars.json` beside the DLL overrides built-in vehicles by `id` and adds
  new ones (up to 128). An invalid, empty, or oversized file is rejected with a
  log line; built-in vehicles remain usable.
- Driving mode switching, menu entries, nitro, turret/horde availability,
  storage, rear access distance, and rim sizes now follow the profile instead of
  a 350Z/H2 branch. Settings are bound per vehicle in the catalog;
  350Z retains the existing section names, H2 retains `h2.`.
- Automated checks (215 catalog checks in `--suite carprofile`, plus
  controls/storage/handling/horde): defaults, merge, capacities and
  mode switching identical to the old car-switch logic, rejection of invalid
  suspension values, transmission tables, IDs and catalog files. The suite also reads
  the shipped `cars.json` beside the DLL and checks for each
  listed vehicle: model file present, accepted by the runtime loader,
  wheel radius matches suspension, axles within body, tires within
  body width.
- Still to check in-game: Garage selection and spawning a second vehicle,
  custom settings sections, storage access through the new
  rear distance, rim sliders per vehicle.
- The generic importer `tools/export_car.py` is data-driven:
  body from `<CAR>_BASE`/`<CAR>_KIT00_*`, wheel and brakes from the same
  families, four wheel locators from unpacked `GlobalB.lzc`
  (`NAME\0 + 0x120`, step `0x30`) — track width, front and rear axle
  follow from these. It writes Schema 3 and `.local/<car>/profile.json`;
  `tools/build_catalog.py` combines profiles into `cars.json`, and the build
  places it beside the DLL. All **28 drivable vehicles** in the
  roster have been imported (67–92 sections, 9–12 textures, 8 original engine loops each): Skyline,
  RX-7, RX-8, Supra, Miata, 240SX, Eclipse, 3000GT, Mustang GT, GTO, A3, Golf,
  Celica, Civic, Corolla, Corsa, Focus, RSX, Lancer Evo 8, Impreza WRX, IS300,
  G35, Tiburon, Sentra, Peugot 206, Peugot 106, Escalade and Navigator. Only one
  `SPECS` entry is needed per vehicle — hue, mass,
  transmission, RPM bands, and rim range; geometry, suspension, and sound
  come from the game data.
- The game shares engine banks: `extract_audio.py --gin <BANK>.gin`
  extracts by bank name, not vehicle name (SUPRA and IS300 use the 350Z bank,
  240SX and COROLLA share a bank). The vehicle-to-bank mapping
  and RPM limits come from the chain
  `CarTypeMapping/<CAR>.ini → CarDataMapping → EngineData` in the
  community mirror `nlgxzef/NFSU2CarSoundTuner`; the bank number is listed as
  `--engine` in the loop provenance.
- Open work: `flight`, `neon`, `radio`, and `exhaust` are documented but
  not enforced; turbo clips cannot yet be configured per vehicle;
  panel animations, engine bays, trunk audio and exhaust variants now use the shared
  U2 exporter for all 30 cars; styling and decals retain their separate catalogs; the 18 roster entries without their own wheel meshes or
  without locators (traffic vehicles, BUS, FIRETRUCK, SUV, TT) cannot be
  imported this way. Field descriptions and limits:
  [CARS_JSON](CARS_JSON.md).

## Inner surfaces of vehicle panels (local correction)

- User visually confirmed the complete local state, including the brake caliper correction,
  in-game; the following checkpoints document the scope.
- Brake caliper: black ring decals were in front of already closed red
  housing surfaces. Decals removed; hub/screw atlas rectangles replaced with
  metal surfaces. Close views with/without rim and in-game image confirmed.
- Third user feedback: smooth underside of hood and parts in front of windshield.
  Donor dashboard and trim measurably protruded through the original windshield.
  Adjusted behind windshield/seal, including adjacent triangles;
  checked vertices, edge midpoints, and face centers.
- Hood: custom contoured graphite inner skin, reinforcement profiles, four matte
  inserts and fasteners. Adapted to STYLE07 instead of flat stock underside;
  exterior carbon, vents and hood hinge retained. No claim of an
  authentic Nissan/NFS internal structure. Check clearance with all three
  engine visuals and external views; confirm material appearance in-game.
- Second user feedback: backs alone do not close open door edges,
  trunk rim, and cut-off A-pillars. Interior loader hid
  entire original door panels and BASE inner shell; interior import
  removed all triangles above 1 m.
- Original door edges remain visible. Rear inner shell from BASE exported separately,
  visible in all four builds; old cockpit remains hidden.
  Full A-pillars and headliner imported, headliner adapted to NFS roof underside.
  Blender preview checks open doors, trunk edge, and roof from both
  sides; native lighting and camera proximity still need in-game confirmation.
- User images show through-view across roof, open doors/trunk, and
  inner wheel surfaces. Imported surfaces receive back faces with their own inward-
  facing normals; windows retain their tint control.
- Back faces stay in the same mesh and follow door/trunk hinges as well as
  rim size and wheel movement. Audio membranes retain their vertex assignments.
- Automated checks: triangle winding, normals, UVs/colors, unchanged source data,
  and large index buffers. In-game check roof from below, doors/trunk open
  and closed, inner wheels, window tint, and lighting; H2 too.
- Back faces supplement existing surfaces; other missing modeled parts
  must be added separately if needed.

## Release 0.28.0 / Setup r6

- Public feature state since 0.22.3: horde mode with minimap and pacts,
  audio/sign/boss fixes, and vehicle storage. Setup includes all music.
- User feedback confirms the core horde-mode gameplay. The latest
  boss damage cap and storage persistence/controls remain targeted
  playtests; automated adapter checks do not replace them.
- Setup code unchanged; all automated checks passed,
  90 payload files, current DLL, 27 radio tracks and embedded payload
  verified with SHA-256. A real wizard/DPI/game launch test
  on clean and existing installations remains to be checked separately.

## Vehicle storage in 0.28.0

- 350Z Stock / Build 1 / 2 / 3: 24 / 18 / 12 / 6 slots; H2 fixed at 32.
  Local native prefabs checked: wood chest 5×2, reinforced chest 6×4,
  black metal chest 8×4. Capacity follows enclosure, not subwoofer brand.
- Exit while stationary, go to the rear, F9 → Vehicle → open Storage.
  Normal Valheim inventory; each vehicle instance has its own contents.
- Downsizing checks all 350Zs and locks if stacks exceed capacity/inventory is open.
  Reordering retains item instances and metadata. External configuration changes
  must also not reduce capacity below occupied slots.
- Native persistent chest saves in the world state. On removal/world change,
  filled storage remains as a normal chest; new vehicle starts empty.
  In flight, the recovery location is on the ground below. Unloaded
  chests stay in the ZDO; no duplicate inventory copy or replacement on unload.
- Automated: sizes, limits, lossless repacking, access lock,
  position updates, release, empty chest, and unload/reload checked.
- Playtest: DE/EN and controller, drag/drop/split/take all, full chest during
  build change/Stock System, multiple cars, unloaded zones, F8, world change
  and save/reload. Check returned chest on slopes/buildings/water and during flight.

## Boss damage spikes and entry prompt in 0.27.2

- Moder only: damage reduced by 45%, capped at 180 damage per accepted
  hit (225 in rage phase) after all multipliers. At most one hit
  per second from her breath/projectile volleys. Other enemies remain as confirmed.
- In the ready 350Z below the minimap: “Face the Horde” and
  “Numpad 9 · Start battle”, including English translation. Follows map and
  biome text; hidden during mode/menus/map and when exiting.
- Automated: volleys, caps, normal enemies, hit intervals and translation
  checked. Test boss balance and prompt placement in-game at different HUD scales.

## Horde polish in 0.27.1

- Removed blue color pulses on lightning hits; lightning arcs and ice tint remain.
- Valkyrie pact removed from offers in wave 5. Three distinct offers
  and at least two new ones compared with the previous wave remain.
- Enemy attack damage in horde mode +20% for the next test;
  pact/rage-phase bonuses stack multiplicatively. Collision damage unchanged.
- Playtest: assess the new survival balance and lightning visuals.

## Elemental ammunition and dragon finale in 0.27.0

- No wait between wave end and pact selection. Three of six offers,
  at least two of which differ from the previous wave.
- Ice freezes normal horde enemies for 1.2 s (then 2.4 s immunity), tints them
  white-blue, and slows bosses by 35%. Reload time increased by 20%.
- Chain lightning jumps up to three times, up to 8 m per jump, with decreasing damage
  and 0.4 s between triggers; cover and hostile/tame enemies are considered.
  Direct bullet hits deal 20% less damage in return.
- Valkyrie pact: automatic firing for the wave, but +25% enemy damage.
- Finale: Moder + 2 Drakes + 4 Fulings. Native 7,500 base HP, flight/ground phase changes,
  rage phase below half HP. Horde automation range extended to 60 m. Local
  Dragon/Hatchling prefabs and native boss/flight fields checked.
- Does not unlock normal Moder progression or boss loot;
  projectiles are limited/cleaned up, persistent ice obstacles suppressed.
- Automated state and adapter tests do not replace a playtest:
  check frost visuals/animations, lightning visibility, flight/landing on normal
  terrain, rage phase, projectile cleanup, pact selection and balance.

## Mixed waves and god pacts in 0.26.0

- New waves: 15 / 20 / 18 / 14 / 13 enemies, 80 planned spawns total.
  Forest enemies, skeletons, Draugr, wolves, Fulings and Berserkers; finishing with
  a marked Troll as forest tyrant with health bar. No new boss assets.
- Before each wave choose Odin / Thor / Iron Chariot or no pact.
  Benefit and drawback last exactly one wave. No permanent upgrades.
- Completion at rounded-up 85% of actual kills and all defeated
  elite enemies; remaining small enemies are removed without points.
- Optional 25-second hunt contract in waves 2–4: +500 points / +250 HP.
  New high score for the changed rules; old high score is retained.
- Automated: spawn limit, pact selection, wave quotas, required enemies,
  bonus time, buff expiration, reload, running over, and explosion damage checked.
- Still needs in-game testing: mixed AI on normal terrain, native resistances,
  shaman healing, Berserker balance, DE/EN pact selection with mouse/controller,
  boss marker and HP, and Odin/Thor after unloading/reloading a zone.
  Only then adjust values and add more pacts/enemy types.

## Enemy radar in 0.25.5

- Living horde enemies shown as red 9-pixel dots with dark outline on the small
  minimap. Local UI overlay, no saved/shared pins, no changes
  to map filters or exploration. Large map remains unchanged.
- Positions in LateUpdate from living local characters, or from their existing ZDO
  for unloaded instances. Hide dead/missing enemies;
  at most 100 reused UI dots. Death, reset, win/loss, and
  world change dispose of overlay, sprite and texture. Respect HUD hiding.
- Projection matches the locally checked minimap implementation:
  world X/Z, pixel/texture size, and current UV view. Edge margin prevents
  dots from overflowing. 14 checks for directions, movement/zoom, different
  map sizes, edges, and invalid values; existing horde/weather tests passed.
- In-game check display at 720p/1080p, 90 enemies, minimap zoom, opening/closing map,
  enemy deaths and wave changes. Native UI masking/readability
  is not covered by automated projection tests.

## Night and thunderstorms in 0.25.4

- Start fades to midnight and native ThunderStorm weather over eight seconds.
  Time changes with smooth acceleration; sky/fog/rain/storm use EnvMan’s
  existing weather transition. No change to ZNet world time.
- Win, loss, Numpad 0, exit, and vehicle removal transition to noon
  and Clear. Transition continues after the vehicle is released. Clear
  daytime remains for this world session; leaving world/multiplayer/unloading plugin
  restores previous local time/weather overrides and transition duration.
- 17 adapter checks: start, evening/midnight/sunrise/noon, hold,
  early abort, repeated cleanup, quick restart, invalid delta,
  previous overrides, new world, missing weather presets, and Dispose. Other
  horde/corpse checks passed. In-game check native weather effects and
  turret audibility during storms; automated test does not render.

## Sign through windows and turret priority in 0.25.3

- User images show missing honeycomb through side/rear windows. Code finding:
  glass and shield shared render queue 3000. Shield gets its own material
  with queue 3100, no ZWrite, and depth test retained. Pickup glow
  unchanged. Check around the car in-game with clear/dark windows;
  walls and body must still occlude shield parts behind them.
- User reports no longer hearing turret during many simultaneous enemy attacks.
  Shots previously used default priority 128. In occupied vehicle now 16,
  actions 32, tail 80, servo 160; radio remains 0. After exiting,
  normal priority for parked turrets. Automation and manual firing
  use the same rule; no global SFX volume changes.
- Audio adapter additionally checks priority changes without playback restart,
  alongside existing sustained-fire/source-limit checks. Actual displacement or
  acoustic masking by enemy sounds still needs in-game testing.

## Shield dome and combat audio in 0.25.2

- User confirms improved shield visuals. Now a continuous hexagon
  grid on a larger hemisphere instead of individual honeycomb panels. Visible
  base brightness at any positive charge, slow shimmer, and
  brighter hit/recharge pulse. Check at 1/150/300 shield, day/night and all
  camera angles; turret and hood should remain clearly visible.
- Pickup is named Explosive ammunition / Explosive ammunition; horde reset and
  already-active message omit “Test”. Translation checks passed.
- User reports radio dropouts while firing. Code finding: 3 s tail per
  shot as unlimited one-shots, radio priority 128 instead of native music value 0.
  Suspected cause: voice stealing due to too many audio voices; not proven at runtime.
  Radio priority now 0 in occupied vehicle, back to 128 outside. Shots
  limited to four sources, tails to two at 40% volume. Existing
  0.34-s shot attacks remain fully audible at 450 shots/min.
- Adapter test with 1,000 shots checks source limit, complete transients,
  volume change and fade-out. Radio/turret/horde/language checks passed.
  Check radio/turret acoustically in-game while stationary/driving, manual/automatic sustained fire, explosive ammunition,
  pause, and vehicle switching. No global mixer
  or audio device settings changed.

## Horde mode polish in 0.25.1

- User confirms the core gameplay. Start still only in landed
  350Z with turret; check H2 and starting on foot in-game.
- Track own ragdolls separately through native setup callback: no
  saved loot, not persistent, remove after three active seconds,
  maximum twelve during mass kills. Abort/win/loss clean them up immediately.
  Tests check loaded/unloaded corpses, limit, reset, delayed callback
  and world change; normal world corpses remain untouched. Reapply loot lock and
  marker when reloading an own enemy.
- Larger outlined HUD font without gray default boxes, narrow HP/shield
  bars, five wave markers, combo timeline, colored bonuses and floating
  pickup text. Layout rendered from actual C# drawing calls in DE/EN at
  720p/1080p, with fallback font and no game scene; still check native font,
  readability in sun/night and overlap with game HUD.
- Three shield rings replaced with a transparent honeycomb shell in one mesh,
  faint moving shimmer and short pulse on shield hit/recharge.
  No additional light sources or colliders. Off when shield is empty;
  pure HP hits do not create a new shield flash. Material, silhouette
  and view from driver seat need in-game testing.

## Vehicle horde mode in 0.25.0

- User confirms the complete five-wave sequence. Expanded into arcade mode:
  start in landed 350Z with loaded turret, 1,000 HP/300 shield, off-road only.
  Native enemy weapons hit IDestructible on the car; driver hits are
  redirected by the mod. Shield absorbs first. A weapon’s extra tree/mining damage
  is not counted as additional car damage. Confirm enemy hits in-game
  with Greydwarf, stone throw, Brute and Troll; pure poison AOEs,
  which natively do not hit objects, also do not hit the body.
- Hard side/front obstacle impacts from 8 m/s cost HP/shield; max
  200 per impact, short lockout against repeated contacts. Enemies, small
  vegetation, normal ground contact and merely scraping along are excluded.
- Orange: 30 explosive shots with fresh magazine; purple: 15 s auto turret;
  blue: +150 shield; green: +250 HP. Four local runes with light beam and label,
  expire after 45 active seconds or at large distance, reposition on
  clear dry ground. Test driving through at high speed; full
  HP/shield must not consume the corresponding pickup.
- HUD: HP/shield bars, wave/enemies remaining, points/combo, ammo and car timer;
  shield rings on car with hit flash. Check 720p/1080p, day/night and DE/EN in-game.
  Menu/pause/focus loss pause timer and own damage.
- Five quick kills raise multiplier up to x5; reset after five seconds
  without a kill. Win with time/survival bonus, gold/silver/bronze,
  result screen and local BestScore. Gold: at least 750 HP and at most
  300 seconds; silver: at least 400 HP, otherwise bronze. Skipped or
  unloaded/despawned enemies prevent record scoring; no points for
  removal/abort. Account for final death before completion scoring.
- Loss ends the run and leaves the car for a new attempt.
  Numpad 0, exit, death/teleport, vehicle removal and world change
  clear enemies, pickups, damage adapters and temporary bonuses. Driving mode and
  turret settings are not overwritten. Other parked turrets
  stay inactive during the run. Check recovery in-game.
- Existing working test world is the first arena candidate. No new
  seed validated or created; a large flat, dry area is essential.
  Light points are checked dynamically. Existing explosive craters remain active.
- New logic/adapter tests check HP/shield, bonuses, points, native hit forwarding,
  crash exceptions and cleanup after mode end. Unity hits, visible runes,
  balance and actual frame times still need in-game confirmation.

## Forest waves in 0.24.0

- User confirms: 30 Greydwarves run smoothly. Now test five waves:
  60 Greydwarves; 90 Greydwarves; 50 Greydwarf Brutes/8 Brutes/2 Shamans;
  40 Greydwarf Brutes/10 Brutes/1 Troll; 30 Greydwarf Brutes/3 Trolls.
- Numpad 9 starts; Numpad 0 removes all test enemies and pending
  sequence. Eight-second pause after the last creature is defeated; after wave 5
  completion message. DE/EN announcements and wave/enemies remaining/pause display.
  Remaining count includes pending spawns; respect HUD hiding.
- Groups spawn ahead and diagonally to the side, 30–45 m away; Trolls 45–60 m
  ahead with placement check based on prefab capsule. Groups reorient
  while moving. Check native AI, paths, healing, attacks and placement needs
  of all new enemies, plus 720p/1080p readability, in-game.
- One run at a time, max 100 live test enemies, at most one
  spawn or three placement checks per frame. No space: skip individual slots;
  abort if the entire wave fails to spawn. Log missing prefabs.
- Menu/chat/inventory, focus loss and pause hold the sequence including countdown.
  Check abort during spawn/combat/pause, death, teleport, world change, win and restart.
  No loot or saved enemies; remove dead enemies before next wave.
- Logic tests check exact enemy counts, order, last survivor,
  timing, cap, placement failures, abort and restart. DE/EN checked;
  actual frame times and Troll finale impact need in-game testing.

## Test horde for recordings in 0.23.0

- Numpad 9: up to 30 unstarred Greydwarves in an arc 30–45 m in front of
  player or vehicle. Numpad 0 removes only this test horde.
  Turn Num Lock on. No loot; enemies are not saved.
- DE: “Der Wald erwacht – eine Horde greift an!”; EN: “The forest awakens –
  a horde attacks!” Language follows F9 → Settings → Language.
- Single-player on open meadow: test on foot, in 350Z/H2 and with turret;
  observe approach, running over, firing and frame times. User now confirms
  adequate performance for 30 Greydwarves; expansion follows in 0.24.0.
- Repeat/hold keys, reset active wave and restart after defeating. Check
  menu/chat/inventory, pause, Alt-Tab, death, teleport and world change;
  no delayed spawn. Ghost mode prevents native targeting.
- Water, slopes, forest, rocks and buildings: skip occupied spots; report smaller/no
  horde if space is insufficient. Start point remains fixed during the wave;
  new enemies stay at least 18 m from player. Spawns still pending end
  when more than 65 m from the start.
- Automated tests check limits, timing, abort and translation;
  native AI, terrain/collisions and actual system load remain playtests.

## Vehicle crosshair in 0.22.3

- Native Valheim crosshair is hidden while seated in 350Z and H2, regardless
  of turret. The custom turret reticle remains visible.
- In-game check without turret, with turret on/off, in flight and after
  exit/vehicle removal; normal crosshair must return on foot.

## Rust menu logo in 0.22.2

- Rust logo rotation now happens before menu scaling and positioning;
  the sticker therefore remains on the turret entry.
- User confirmed the fix in-game after installing 0.22.2.
- In-game check at default and moved menu positions, high and low
  resolution, and DE/EN: logo inside turret row, display without logo.
- Three animated in-game previews show front, rear and flight mode in the README.

## Smaller radio installer (setup revision 3)

- 27 songs as Ogg Vorbis at target bitrate 192 kbit/s; local WAV masters retained.
- Local setup comparison: 746.68 MB (r2) to 148.30 MB (r3), about 80% smaller.
- Full decode-back, stereo/sample rate/duration and checksums are
  checked automatically. Check song changes, track order, pause/resume, and
  compare sound with WAV in-game; especially cymbals, vocals and bass.
- WAV-to-OGG update removes WAV files previously managed by installer;
  personal files are retained. Saves and radio settings
  are unaffected by package change.

## Controller and nitro in 0.22.1

- User confirmed installation and game launch with the previous wizard.
- Start flight mode with controller, land, exit and remove vehicle:
  no persistent vibration. Also check menu, world change and re-entry;
  normal Valheim vibration must still work after exiting.
- Block driver door before entry/exit: central game message in DE/EN.
- Test nitro on ground/in flight: visible edge filter and smooth field-of-view change,
  vehicle remains sharp. Release, pause, menu, Alt-Tab and exit restore
  field of view. Effect off/intensity zero causes no image change.
- Automated logic/adapter tests do not replace this Unity/controller check.

## Windows installer and public release

- New EXE wizard: install, update, uninstall, and optionally
  remove self-installed BepInEx. Since setup revision 2, music is always installed;
  logo in wizard and README, LICENSE with Close/Escape.
- Automated tests use temporary game directories. Manually check the real wizard
  on clean and already-modded Valheim installations:
  file selection, permissions, scaling, game launch and vehicle spawn.
- Package contents also installed in an isolated directory,
  updated and uninstalled with BepInEx. Setup revision 4 layout
  was also checked by WinForms rendering; further DPI checks remain open.
- Windows EXE and SHA-256 are produced locally in `artifacts/installer/`.
  First public release: `v0.22.1`, setup revision 4. The build itself
  does not publish anything; upload EXE and checksum as release assets.
- Two clearly labeled Blender previews of the 350Z are in the README.
  Add/replace with real in-game screenshots once suitable captures
  are available; a short gameplay clip can later be embedded as a GIF.
- Developer guide: [CONTRIBUTING](../CONTRIBUTING.md),
  version history: [CHANGELOG](../CHANGELOG.md).

## Turbo and small trees in 0.22.0

- At about 1–3 km/h, drive through small beeches/firs/bushes forward/reverse;
  after stopping at trunk, keep accelerating. Side contact, rolling without
  throttle, brake/handbrake, parking, menu and flight must not create artificial hits.
  Enemies take no collision damage below 9 km/h; walls provide cover.
- 350Z: listen at partial/full throttle, high RPM without load, abrupt and gradual
  throttle release, shifts and throttle reapplication. Blow-off responds
  to prior boost and must not trigger without load.
- All eleven dials: −30/0/+30, brief vacuum flutter, digital
  speed in KM/H/MPH, 720p/1080p; check H2 has no turbo gauge.
- Under load, check menu/pause/Alt-Tab, exit, reverse and flight: no old
  or artificially triggered blow-offs. Menu sound choice remains audible.
- Original NFS trigger/pressure curve and semantics of five turbo substreams
  remain unresolved. 1/2/3 mapping and pressure/gain/flutter are approximations;
  compare later with actual NFS runtime behavior using audio/telemetry data.
- `tools/render_audio_preview.py` creates an engine preview from compiled sound model
  and `350Z-Turbovergleich.wav`: 0–5 s partial throttle, 5–10 s full throttle,
  10–15 s high RPM without load. These are offline renders, not game recordings.

## Menu, drivetrain and exhaust in 0.21.0

- Controller: reach all categories with stick/D-pad, right/A into
  contents, left at row start back; LB sidebar, RB contents. Edit sliders and
  selections with A, A/B ends editing. Check laser submenu,
  song list, disabled controls, mouse switching and reopening.
- Without a car or outside 5 m, only Garage/Settings; spawning, removal and
  approaching update visible categories. At 720p/1080p check exhaust entry,
  Rust logo, “by BCKWDZ”, focus frame and DE/EN without overlap.
- Compare 350Z and H2 launches forward/reverse on same slope;
  rolling against selected direction above 1.25 m/s must first
  brake; below that, launch boost also works with partial controller throttle.
  Check Off-road/Street/Drift, partial controller throttle, burnout and braking.
- Normal ammo: about three hits for unarmored 40-HP enemy; stars,
  resistances and larger enemies remain relevant. Test explosive ammunition against
  large trees, felled trunks and several rock sections; check native
  destruction/loot, cover, players, tame animals and structures.
- Exhaust: switch sounds with engine running/stopped, volume zero,
  saved settings per model, multiple vehicles; check nitro color on
  ground/in flight, purge, thrust flames, lights and switching cars.
- Cycle through all ten 350Z tips: exactly one variant visible, two or
  four nitro flames at pipe openings, no old flames after switch;
  STYLE08 matches previous model. H2 exhaust tips remain fixed to model.
- Automated tests check logic and adapters using test doubles. Handling,
  Unity collisions, sound and actual menu layout remain playtests.

## Controller and language in 0.20.0

- 0.20.5: check prompts at bottom right on Xbox/PS5, mouse switching, DE/EN, 720p/1080p,
  HUD/prompt scaling: icons, no clipped lines, speedometer above,
  switching between ground/flight/menu and restoration after exiting,
  inventory, pause, world change and hiding HUD.
- 0.20.4: test camera with menu open on foot, in vehicle and in flight:
  right stick, RMB drag, no movement when clicking menu/slider/title bar;
  cursor after release, close, pause and Alt-Tab; also check obstacles.
- 0.20.1: check hotbar in vehicle and mod menu using D-pad, number keys
  and click: no selection/use, display stays current; after
  exiting with menu closed, normal controls return.
- Check Xbox and PS5 over USB/Bluetooth: analog triggers, stick dead zone,
  camera sensitivity, Y inversion, mouse switching, hotplug and Steam Input.
- On foot, L3 without simultaneous native action; B/Circle near car only enters.
  While driving, X/Square nitro, A/Cross handbrake, LB/L1 manual turret.
- Test all menu categories, disabled entries, sliders, color fields, laser submenu,
  long song list with auto-scroll and moving menu window by mouse.
  A/Cross confirms, B/Circle goes back; menu must not trigger driving action.
- Held pedals/nitro during menu, pause/focus loss and controller switch:
  release first. Do not pass through to native movement/attack while driving.
- 1–4 or D-pad up/down: ground/flight, H2 without drift; F7/B/Circle
  in flight lands before exiting. Blocked door shows an error.
- Turret automatic in flight, even with parked automation off; manual
  firing only on ground. Check visibility, range and cover remain intact.
- DE/EN updates immediately in menu/HUD/messages; values and config IDs unchanged.
  Check forced controller display, larger prompts at low resolutions, and
  song title display on automatic track changes too.

## Headlight show in 0.19.0

- F9 → Lights on 350Z/H2: enable show-off, exit, two physical flashes
  left/right from driver view plus pause; beam and lamp surfaces synchronized.
- Check color/intensity, zero intensity, reset, enabling beside a car already driven,
  entering during flash/pause, two cars with independent settings.
- Check without engine keep-running, manual light switch, brake/reverse lights,
  focus loss/pause and cleanup. New menu item at low resolution.

## Metallic paint in 0.18.4

- Spawn 350Z and H2 fresh: compare metallic reflections in sun/shade/night,
  vinyl colors, carbon hood and plastic. Check paint shader in log.

## Nitro image effects in 0.17.0

- 350Z on ground/in flight and H2 in flight: W + LMB/Ctrl, release and empty
  tank. Purge/hover without boost should have no filter/trails. Taillights off/on.
- F9 → Nitro: toggle each switch separately, intensity 0/70/100%. Menu, pause, chat,
  inventory, Alt-Tab, exit, death, world change, vehicle switch: no leftovers.
- Check DX11/Vulkan, HDR, resolution changes, camera orbit/zoom and obstacles:
  no upside-down/dark image, vehicle/HUD readable, no taillight trails
  through buildings. Visually tune trail length/position on both cars.
- Measure GPU frame times at 1080p/1440p/4K. Six image positions plus half resolution
  for GPU sampling texture; no CPU image readback. With intensity 0/effects hidden,
  camera callback is disabled. No Unity runtime test performed.

## Flight exhaust and NFS image effects in 0.16.1

- 350Z in flight: W + LMB/Ctrl also activates the four existing exhaust flames
  and normal nitro sound alongside wheel nozzles. Empty tank,
  release, menu, focus loss and landing correctly end boost effects.
- Hovering without boost has no exhaust nitro; no purge/tire smoke
  or thrust pop in flight. Recompare ground nitro and purge.
- NFSU2 Motion Blur, Light Trails and Light Glow are separately identified
  (REFERENCES). Radial blur around the vanishing point plus briefly fading
  light trails is a plausible NFVU2 recreation; not yet a verified
  reconstruction of the NFS shader or its nitro control.
- Original filter shaders have since been disassembled: weighted blend
  of 1–4 texture samples, separate brightness and color processing.
  Nitro mapping and taillight trails remain unresolved. Due to packed/protected
  EXE code, next use runtime/graphics data from the original as evidence:
  compare textures, UVs, weights, blend mode and trail geometry.
- For a later recreation: keep vehicle/HUD readable, make intensity adjustable
  and disableable, only activate on actual boost, fade smoothly. Measure graphics
  cost; no CPU image readback per frame. Compare with original NFS.

## Background spawning in 0.16.0

- Spawn 350Z with interior and H2 first and multiple times: loading indicator
  animates, world/player keep moving, complete car appears.
  Compare “Vehicle data ready” and “Vehicle spawn completed” in log;
  measure in-game frame times, especially texture uploads/shaders on first visibility.
- F6 repeatedly, F8 and Cancel at different loading stages; then
  spawn again. No ghost colliders, partial cars, sounds or mesh remnants.
- With existing cars, F8 during loading may only
  cancel loading. Their radio, turret and physics must continue normally.
- Move during loading, open F9, Alt-Tab, death, teleport and leave world.
  No delayed spawn in next world; do not enter partial resources.
- Check interior, moving doors, window tint, steering wheel, HUD, engine/nitro/turret
  audio, lighting and H2 spinners after activation. Four-car limit.
- Automated loading/cache tests do not replace Unity runtime checks. Some
  indivisible Unity calls and GC can still cause brief spikes.

## Nitro and tires in 0.15.2

- In flight, close menu, release keys, press W + LMB or W + left Ctrl:
  check “NITRO AKTIV”, decreasing tank and speed clearly above 65 km/h.
  If still ineffective, inspect `Flight nitro:` in log: native/legacy
  mouse values, Ctrl, release, phase, throttle, tank and speed. Without this
  runtime data, the cause of the reported failure is not yet confirmed.
- Flames must not extend beyond the tires or float freely below them.
  Compare neutral, full thrust, tilt and H2.
- Check original tire tread in daylight/night and after rim size changes.

## Flight fixes and HUD in 0.15.1

- Check ascent/descent at high altitude and near ground/ceilings; after fast
  descent, ground braking must engage before touchdown. Test auto-landing separately.
- On all four wheels, the finished rim face points upward; flames only
  below. W/S and A/D, release and nitro: tilt/flame direction follows input.
- 350Z/H2: W + LMB in flight, empty tank, recharge, menu, focus loss,
  flight/ground switch and landing. Hovering alone must not consume nitro.
- HUD: all eleven designs and 65–140% scale, KM/H and MPH, stationary,
  reverse and high flight speed. Both needles clear, numbers unobscured.

## Flight mode in 0.15.0

- 350Z and H2: while seated, F9 → Vehicle → Fly; four wheels fold evenly
  downward with brakes/spinners, four nozzles follow the outer rim side.
  Combine camber, rim size, offset and lowering; no mesh overlap.
- W/S forward/back, A/D turn, both Shift keys up, Spacebar down;
  opposing keys cancel. Releasing brakes and holds position.
- Test ground clearance, slopes, rocks, roofs, ceilings and high-altitude starts.
  Hover control and collisions must be confirmed/tuned in Unity.
- Menu, chat, focus loss, pause: no stale inputs or unintended
  descent due to the old Spacebar brake binding.
- Select ground mode at high altitude: controlled landing, unfold wheels,
  then test acceleration/braking/drift/nitro. Cancel landing by switching to Fly.
- Exit only on ground; test F8, death and world change during flight.
  Multiple cars retain own state; new spawn must not inherit flight state.
- Four flames, sound transitions, volume 0 and missing optional audio files.
  Check particles and sound in-game; automated tests do not replace this.

## Rims in 0.14.3

- Walk around 350Z in daylight: chrome lip should reflect light/dark surroundings,
  center should look subdued gold. Compare night, forest and neon.
- Check log entry `350Z wheel metal shader`; prefer default,
  existing vehicle shader as fallback. No artificial glow in darkness.
- Check multiple vehicles and rim sizes/camber. Blender preview shows
  material parameters but does not prove Unity rendering.

## Window tint in 0.14.2

- 0/50/100% in daylight and night: no red self-illumination, neutral dark
  windows at 100%. Compare rear window with taillights and trunk neon.
- Left open window, exit, other 350Zs and restart: tint remains
  saved and window behavior still works.
- Local lava-lantern materials have confirmed red emission; the mod
  now only reuses their shader reference and creates a neutral material.

## Driver pose and windows in 0.14.1

According to the user, 0.14.0 works in general. Reported issues were a head protruding through the
roof, hands positioned too far forward, and visible steering reversed.

- Check head with both character models, hair/helmets, lowering and on slopes
  under roof; lower seat position must not disappear into seat cushion.
- Steer right/left: wheel and right hand must turn together in expected
  direction. Wrist behind rim, fingers on rim, no overextension.
- Left arm on open window; pull inward on door opening and exit.
  Check all three door mechanisms and quickly opening/closing from F9 menu.
- Tint 0/50/100%, restart and multiple 350Zs: closed windows respond,
  left window stays clear while seated and visible again after exit.
- F8, errors and world change restore window, posture and controls.

## Interior and driver in 0.14.0

- F6/F7: check cockpit, seats, door panels, windows and steering rotation in
  daylight and night. Door panels must move with all three door styles.
- Enter from both sides, front/rear, with blocked path and on slope.
  No walking through walls; driving enabled only after door closes.
- Both character models with/without armor, helmet, weapons and cape: hips in
  seat, head under roof, hands on wheel, feet on pedals. Visual scale 85%.
  Exact hand/foot pose and texture/glass appearance require Unity playtest.
- Exit stationary and while moving, blocked driver door, obstacle
  appearing during exit, repeated F7: no stuck controls.
- F9/pause/Alt-Tab during approach, open door and seat transition; F8, death,
  world change/plugin teardown: renderer, equipment, animator, collider, body
  and camera must be restored.
- Multiple cars, H2 switch, missing/invalid interior payload and
  missing transparent window shader: original mod remains usable.
- Further refine native chair pose and custom procedural transitions;
  cockpit instruments are static, not a gauge simulation.

## Movable menu in 0.13.1

- F9: hold title bar on left and drag; try text and empty header area.
- Check all screen edges, 720p/1080p, smaller windows and resolution changes:
  menu fully reachable, mouse follows without scaling jumps.
- Click category/slider/close: no accidental dragging.
- During drag, check F9, focus loss and releasing outside the window;
  no cursor stuck when reopening. Check position after restart.

## Lowering, menu and parking automation in 0.13.0

- 350Z/H2: test 0/6/12 cm on flat ground, driving mode switch, F6 respawn and
  config restart. Existing camber/offset/rim sliders still
  combine; observe wheel clearance and ground contact at extreme values.
- Exit with partial/full steering in both directions, wait,
  open F9 beside car and re-enter: no snapback on exit,
  smooth continuation of steering on entry; brake/burnout/nitro stop.
- Test new menu at 720p/1080p and smaller windows across all categories:
  icons, original German headings, cursor, scroll lists, close,
  categories via ↑↓ and sound volume 0/50/100 percent.
- Compare three original frontend samples against NFS game; native roles
  are not yet verified. Import indices remain configurable via CLI.
- Drive/activate turret once, exit: smooth laser sweep both
  sides. Check enemies in front/side at 5/29/31 m and behind car.
- Rock/building/own car/player/tame animal in front of barrel: no automatic
  fire through cover. Check target loss, death, taming, multiple enemies and reload.
  A different hostile first hit is fired on only after target change.
- Engine keep-running off, drive another 350Z/H2, multiple parked turrets:
  each has its own search motion, shots, reload and spatial sounds.
- F9, inventory, pause, Alt-Tab, player death, revival and RMB while entering:
  check locks and reacquisition. F8/world change removes everything.
- Check explosive ammo/laser color and intensity in both modes. Search sector
  ±90° is Rust-inspired; scan motion and 30-m range are custom
  mod rules. Automated tests do not replace Unity physics/in-game view.

## Hummer and Garage in 0.12.0

- F9 without car → Garage: spawn 350Z and yellow H2 in turn; both remain
  present, no overlap. Tight spots/slopes must yield another position
  or a clear message. Maximum four cars.
- Walk between the two cars: F7/F9 target the nearest; check driving, exit,
  camera and player controls. F8 removes exactly one, world change removes all.
- H2: acceleration, braking, reverse, off-road, suspension travel and body roll.
  Check dedicated Hummer sound, four gears, radio and shared music ducking.
- Spinners: delayed spin-up, spin-down after stop, reverse,
  camber/wheel offset/22–28-inch slider. Brake calipers remain fixed.
- Check yellow, chrome, rim fit, rear lettering, red tail/brake lights and white
  reverse lights in-game. Preview uses approximate suspension position.
- Save H2/350Z settings separately; with keep-running, both retain
  radio and engine states. Two simultaneously running radios are audible.
- Regression: 350Z build/subwoofer, door animations, drift, nitro, turret,
  laser settings and explosive ammo with H2 beside it.
- H2 engine bay/trunk, doors, nitro and turret are not set up yet.

## Laser and subwoofer in 0.11.0

- Check laser by day/night: default intensity 200%, range 25–400,
  color presets and custom RGB values, save, switch off, wall occlusion.
- Music with bass and trunk open: cone moves, rim/enclosure remain
  fixed; cycle four subwoofer models and all slot sizes/builds.
- Check pause, track change, radio off, volume zero, focus loss, entry/exit
  and keep-running; no continuous motion if music missing/loading.
- Rebuild system several times and respawn vehicle: no unintended mesh changes
  or leftover animated mesh copies.
- Signal tests check bass/treble, stereo and silence. DSP callback, visible
  excursion and shader brightness also require Unity playtest.

## Laser and explosive ammunition in 0.10.0

- Check aiming sight above car while zooming, looking around, steep downward view,
  slope and camera obstacles; orange when barrel is obstructed/still aligning.
- Check laser model, red beam and dot with recoil, plus menu switch.
- Switch Normal/Explosive in menu; preserve reload and mouse-button lockout.
- Test explosions on enemies/cover, volume and many consecutive hits.
- Hit ground directly: small crater each hit, further deepening,
  grass removal, tile edges, collision and persistence after world reload.
- Hits on rocks/walls/enemies must not change ground below.
- Terrain API checked locally; adapter tests do not replace Unity playtest.
- No holes/tunnels under overhanging terrain: Valheim modifies a height field.

## Aim marker and mounting in 0.9.1

- Check crosshair at actual impact point with clear view, obstacles,
  camera rotation, zoom, turns and elevation limits; no fixed extra marker.
- Check supports mounted lower/further back on roof from all sides in-game.
- Check ammo display bottom left with speedometer on/off.

## Roof turret in 0.9.0

- F9 → Turret: enable beside/in car, saved state, removal and
  respawn; check roof position and materials by day/night.
- Rotate camera around and vertically; check elevation limits, follow-up, aim point
  and orange marker when barrel is blocked by own vehicle.
- RMB: single shots, continuous fire, 30 rounds, 2.5 s automatic reload,
  shot/reload/servo sounds and separate volume.
- Enemies behind/in front of rock or building: first solid object only; no
  damage to players, tamed animals or structures. Own car blocks shots.
- Menu, pause, Alt-Tab, exit and re-entry with held mouse button:
  no firing until released. Check simultaneous left-button nitro.
- Automated checks and Blender preview do not replace Unity playtest.
  Models, textures, joint points and samples are original; weapon mounting,
  recoil, muzzle flash, hits and reload are custom adjustments.
  No automatic targeting, no complete Rust animation system.

## Gauges, camera and drift in 0.8.0

- Check all eleven designs, color, sizes, KM/H/MPH and saved selection.
- Compare RPM/gear with sound and NOS level with boost/recharge.
- Check HUD at different resolutions, open menu, exit and F6/F8.
- Dynamic camera: test turns, drift, reverse, mouse look, stationary, zoom,
  middle click, menu/Alt-Tab and obstacles; compare follow slider.
- Drift on a clear flat area: throttle, countersteering, handbrake, S brake,
  smoke with ground contact, burnout cancellation, nitro and exit/re-entry.
- Switch back to Off-road/Street; retain prior handling.
- Grip and camera follow need playtesting; no claim of original NFS physics
  or complete GTA camera recreation.

## Outdoor music in 0.7.2

- With radio playing outside, open left/right door and trunk individually and
  together: smooth increase in volume/clarity, positioned at each opening.
- On closing, sound becomes muffled again, no track jump; hood has no effect.
- Listen in-game with all door mechanisms, re-entry, pause, volume 0, keep-running off,
  focus loss and distance from car.
- With several openings, spatial source is at their weighted midpoint;
  no separate propagation or wall-occlusion simulation.

## Taillight correction in 0.7.1

The upper STYLE04 inserts were omitted from the import and are included again.
Check both sides in-game with trunk open/closed and
neon active: no see-through gaps in the lamps; lower orange
stripes and tail/brake/reverse lights still work correctly.
Original geometry and external preview checked; Unity playtest remains open.

## Trunk build, doors and engine bay in 0.7.0

Implemented: four original trunk enclosures, selection per slot,
stock build with four subwoofers, separate neon with color/pulse mode,
three engine visuals and eight original animation tracks with hinge points.
Normal/Suicide/Scissor doors, hood and trunk can be operated individually.
External previews of all door mechanisms, enclosures 1–3 and engine visuals checked.

- In-game F9 → Build: cycle all slots, subwoofers, amplifiers, LCDs and
  covers; changing enclosure must not duplicate anything.
- Check Stock System, saved configuration and repeated F6/F8.
- Set trunk neon independently from underbody: seven colors,
  static/pulsing, exit/keep-running, trunk open/closed.
- Open/close each door separately, hood and trunk; reverse running animation
  and change door mechanism while doors are open.
- Mirrors/interior panels and spoiler must move with doors; side skirts remain fixed.
- Driving off and entering close everything; no lingering misalignment on slopes.
- Showroom animations have no additional door colliders.
- Interior remains a simple original model; detailed seats/cockpit,
  transparent windows and finer material appearance are later work.
- LCDs have static textures. Neon is a visual pulse, not beat analysis.

## Running over objects and keep-running after exit in 0.6.0

Implemented: speed-dependent native hits on hostile creatures
and selected small trees/bushes; engine idling, neon and muffled
spatial radio after exit, with shared switch under F9 → Vehicle.

- Hit small beeches/firs and bushes forward/reverse from about 9 km/h:
  native destruction, wood and effects; check dense vegetation in particular.
- Hit enemies at different speeds: damage, knockback,
  death/loot and repeated impact; no multiple hits from multiple colliders.
- Large trees, rocks, buildings, tame animals and peaceful NPCs remain excluded.
- Parking: off after fresh spawn, idling and enabled neon after driving;
  check menu switch outside, saved setting, exit/re-entry.
- Radio must not restart on exit; quieter/more muffled outside, fade with
  distance. Check manual pause, track end/change, volume 0.
- Restore Valheim music outside; focus loss/game menu pause;
  F8 and world change must remove all sources/effects.
- Actual physics contacts, native damage RPCs, sound and menu layout
  require playtest; automated tests use test doubles.

## Launch, reverse and burnout in 0.5.0

Implemented: +20 percent launch torque with smooth taper, shorter first
audible gear, synthesized reverse transmission whine, dimmed reverse lights,
and W+S burnout with original tire sound and smoke texture.

- Compare launch with 0.4.1 on the same route, especially on slopes.
- Check W+S while stationary in both driving modes: front axle holds, rear wheels spin.
- Check release of S/W, Spacebar, F9, Alt-Tab, exit and removal:
  no lingering effects or grip changes.
- Losing ground contact and sliding sideways should cancel burnout.
- Check reverse whine with speed/stationary and volume.
- Check reverse lights at night; continue investigating original sample mapping.
- Check smoke position with wheel offset/camber and shader effect in-game.

## Menu position and headlights in 0.4.1

Menu at top left with 16-pixel margin and scaling for smaller windows.
Headlight color from 14 NFSU2 presets, intensity 0–300 percent, saved
values and reset under F9 → Lights implemented.

- Check menu position at different resolutions and clear view of car.
- Compare front light beams and lamp surfaces at night with all colors.
- Check intensity 0/100/300 percent, exit/entry and saved values.
- Rear/brake/reverse and license plate lights must remain independent.

## Wheel menu in 0.4.0

Five sliders implemented: camber and wheel offset per axle, shared rim size.
Live visual adjustment with saved values and reset;
tire outer diameter and confirmed suspension are retained.

- In-game adjust both axles independently and check after restart.
- At steering angle and rotating wheels, check stable camber without wobble.
- Brake calipers must steer/tilt but must still not rotate with wheel.
- Check extreme values for fender/body penetration; sliders are visual only.

## Rear badge in 0.3.1

Corrected mirrored 350Z badge specifically through its UV island.
External close-up checked; confirm orientation in-game.

## Nitro, purge and rims in 0.3.0

Implemented: hold left mouse button for boost with green exhaust flames,
purge at front/hood while stationary, six-second tank, recharge while driving,
F9 nitro settings and chrome lip with gold-bronze rim center.
Original NFS textures and nitro-bank sounds imported locally.
In-game test for this version is still pending.

- Check boost/purge transition, tank, braking, reverse and jumps in-game.
- Close menu by mouse click, Alt-Tab, enter/exit: no unintended activation.
- Check green flames at all four exhaust tips and purge jets day/night.
- Check chrome/gold appearance under Valheim lighting; external preview is ready.
- Confirm native nitro/purge loop mapping by listening against NFS.
- Identify exact NFS load-change/tuning conditions and sputter substream.
  Optional flame pop is a custom, initially disabled approximation;
  without a verified pop sample it remains visual only.

## Feedback from the 0.2.0 playtest

The user confirms the basic operation of the implemented menu, radio,
lights, neon and sound features. The following follow-up items were reported:

- Add missing lamp surfaces in rear bumper and strips below the
  upper taillights; distinguish functions using original parts.
- Add white reverse lights in the correct locations; they must not turn on
  while braking forward with S.
- Widen the tires that look too narrow; retain diameter and suspension.
- Check larger, sportier brake discs/calipers from available NFS parts.
- Add verified artist and song names to radio instead of numbers.

Implemented in 0.2.1: rear strips and bumper inserts, white reverse lights,
28-cm tires, STYLE03 sport brakes and 27 actual radio tracks. The user also confirms
0.2.1 works in Valheim. Targeted edge-case tests remain listed below
as further quality checks.
The orange strips are passive; turn signal control remains future work.

## Further targeted playtests

- Check F9 menu, cursor/camera return, entry/exit and vehicle removal.
- Radio: rapid previous/next, pause, track selection, focus change, exit/resume.
- Sample-check the 27 mapped radio tracks while listening in-game.
- Check lights and license plate at night, brake light with main light off.
- Check neon on flat ground, slopes and steps, and during jumps; tune shader/color intensity.
- Listen to earlier audible shifts and turbo on the same short route.
- Compare original turbo substream roles, Rachel tuning and NFS trigger behavior.
- Doors/trunk: check new 0.7.0 animations and interior surfaces in-game.

## Sound polish

- Reproduce and isolate the occasional odd transitions in-game.
- Tune RPM, load changes, scraping and shift transitions.
- Check idling, reverse, exit, menu pause and volume.
- Determine Rachel’s actual sound mapping from the NFS configuration.

## Rachel’s car

- User confirmed 0.1.9 appearance in Valheim: Widebody 02, Spoiler 02,
  carbon hood, original rims and dragon vinyl imported from `DDAY_PLAYER_CAR`.
- Compare paint reflections and rim tint with original game.
- Check wheel clearance while steering and compressing suspension in both modes.
- Add window transparency, lamp covers, detailed interior and driver seat;
  check approximate license plate fit in-game.
- Complete all outstanding visual work before calling the full
  Rachel recreation complete.

## Handling

- User confirms off-road mode in 0.1.4 works well.
- Compare future changes on the same route and check individually.
- Continue tuning collisions, center of mass and street mode.

## Possible bridge

- Investigate feasibility of a connection between running NFSU2 and Valheim.
- First verify NFS version and readable vehicle state.
- Test controls, position, time synchronization, and terrain/collision transfer separately.
- A working bridge has not been demonstrated and is not part of 0.1.5.

## Unified U2 import and vehicle previews (local build)

- `export_car.py all` exports all 30 U2 entries, including shared wheels/spinners,
  original panel tracks, engine variants, trunk audio and mounted exhaust variants.
- One `render_vehicle.py` previews all U2/MW profiles, with opening panels,
  embedded textures, axle geometry and configurable showroom selections.
- Fleet/spinner state checks are independent of model fixtures; vehicle asset checks
  use every generated profile and the actual runtime model loader.
- Source build and Blender acceptance are separate from in-game Unity acceptance.
