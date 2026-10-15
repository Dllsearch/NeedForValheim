# NFVU2 local garage work — 2026-10-14

## Frontend and reported garage regressions

- Golf profile declared only radio/drift, so NOS was disabled by the intended
  capability gate. Importer now declares NOS and includes SFX_FIREB, smoke and
  fitted original-exhaust mouth positions for all imported cars; H2 supports NOS too.
- Failed placement returned only a status hidden by the garage. Placement reuses
  a manually removed car's spot, searches 50 candidates, and reports rejected model,
  reason, fleet count and driving/loading state. Removal returns to the car lot.
- On-foot F-key hints moved under the hotbar; vehicle native hints raised above
  bottom controls and hidden in the fullscreen frontend.
- InventoryGui public API verified in installed assembly: m_inventoryRoot,
  m_weight, Hide(). Native Button opens the garage after hiding inventory.
- Frontend uses original FrontB assets (U2_MENU_LOGO, arrow, mask/backing and vehicle
  badges) plus a separate 1600×900 RenderTexture garage, layer 30, at an isolated
  rendering position. It has no active colliders and does not enter the world fleet.
  Shared car settings are edited under preview context; world/car context restored
  after every GUI/preview step. Preview resources/cancellation released on close.
- Native Menu.Show is gated only while the frontend is open so Escape closes the
  garage rather than also opening the pause screen. Keyboard selection uses the
  same focus/edit state as controller input.
- Full local payload built and targeted suites passed before runtime visual QA.

- Route: existing BepInEx plugin and original-data importers; plugin id and
  Rachel350Z.dll retained. No original binaries/decompiled output committed.
- Original models: schema 3 had been accepted by ModelFile but treated as the
  primitive fallback by spawning/rendering. Runtime rich-model checks now use >=2.
- Garage: 30-car paging, fleet, native-camera showroom, persistent model roster,
  three builds per model, staged original styling libraries and actual upgrade stages.
- User reported white/untextured bodykit regions and missing world chunks while seated.
- Installed assembly inspected read-only through Mono.Cecil. Player.LateUpdate
  calls ZNet.SetReferencePosition(transform.position) for the local owner. ZoneSystem
  uses ZNet.GetReferencePosition for CreateLocalZones and CreateGhostZones every
  0.1 seconds. The mod disables Player while seated, which stops that native update.
  VehicleWorldStreaming publishes the moved driver's position at the end of mod
  LateUpdate, and publishes the native exit position before clearing the driver.
- H2 KITW01..04 contain only two small glass-window meshes, with no body shell.
  Those were invalid options and are excluded; loading a removed selection falls
  back to Factory. Bodykit catalogs must contain a real BODY mesh.
- Original rim material 0x010cb64a includes INTERIOR atlas backing planes with
  corners at ~1.39× tyre radius. Filter those triangles like the existing car/rim
  importers. Unknown missing-texture surfaces receive a dark neutral material;
  style libraries include InGameCommon's shared texture pack.
- Imported vehicle materials use the loaded Standard shader when available;
  Custom/Piece fallback disables inherited style/glow masks. All descriptors apply
  their own colour, texture, metallic and smoothness values.
- Exterior chrome can follow custom body paint; Factory retains original chrome.
  H2's body materials named wheel_chrome are classified by their actual mesh group,
  so wheel colour no longer recolours body trim. Removed placeholder choices reset
  to Factory when a validated library is present; missing libraries preserve choices.
- Targeted checks: handling, nitro, catalogs (every model and styling file), menu,
  optional-asset loading, storage, controls and the new streaming regression suite.
  These are managed/native-API-double checks, not proof of Unity terrain generation.
- Earlier installed build spawned the H2 in the user's world (log and window capture).
  User will close the game personally before checksum-verified final installation.
- Restore snapshots live in ignored artifacts/backups; installer retains two dated
  snapshots. No source changes committed/pushed and no public installer released.

## Manual feedback: frontend and garage loop

- User keeps gameplay control and verifies personally. No automation drives their world.
- Live log showed ArgumentNullException in EnsureGarageStage: Shader.Find("Standard")
  was null. Resolve the loaded material shader, clean partial stage creation, and
  contain preview errors without invoking CleanupAll on the world fleet.
- Native inventory button was anchored above the fullscreen inventoryRoot, placing
  it outside the screen. It now belongs above m_player's inventory panel.
- Seven primary U2 categories, contextual subcategories, cyan/acid selection artwork,
  translucent overlays, illuminated checker-floor showroom and reflection probe.
- Toolbar/grid cells are individual focus targets; arrow/Enter navigation does not
  require entering a separate grid edit mode. Mouse wheel zoom works over the preview.
- Native Hoverable/Interactable on real car colliders: Use enters, Shift+Use opens
  garage. Suppress same-frame enter/exit; Tab opens the frontend while seated, with
  native InventoryGui.Show gated only for seated/frontend contexts.
- Enforce one instance per model at Spawn preflight, deduplicate recalled rosters.
  Explicit per-row REMOVE for parked cars and saved roster entries. Take/select car
  supports parked occupied vehicles, with velocity/horde checks and cargo preservation.
- Build and controls/menu/loading/catalog/streaming suites pass. Native API inspected:
  InventoryGui.Show(Container,int), m_player RectTransform, Player hover lookup supports
  GetComponentInParent<Hoverable>. These checks do not verify rendered Unity gameplay.

## Miata, inventory routing and parked terrain follow-up

- User confirmed 3D preview and two distinct spawned models, but Miata had no
  lights/NOS flames, drifted to an increasing distance after switching to Supra,
  and could not be removed. Inventory button and seated Tab did not open frontend.
- Inventory/Pause Harmony classes were omitted from InitializeControls registration;
  both are now explicitly patched. Button is a topmost inventoryRoot sibling at
  the item panel's world-space corner, outside grid masks. Click queues a menu
  request after native inventory hide, with click/open breadcrumbs in the log.
- Miata uses chrome reflectors for headlamp surfaces and f7fc7674 rear lenses;
  light classification now handles those alternatives and other missing rear lenses.
- Generic EXHAUST geometry was part-local. Attach at original rear-bumper socket
  matrices with the same exhaust axis conversion used by the established 350Z
  importer. NOS emitters now fit actual rear tips; preserve original purge alpha.
  H2 has no stock exhaust mesh; its flame uses the original rear exhaust socket.
- Separate parking lifecycle protects unoccupied distant/unsupported vehicles:
  retain supported pose, hold kinematic before terrain unload, resume on loaded
  terrain near player. Occupied/flight handling remains dynamic. Unoccupied removal
  ignores target velocity and preserves the active vehicle when deleting another.
- Original RDP_AIRPORT_ROADPATCH_A from STREAML4RR.BUN is tiled on showroom floor;
  original file/pixels stay in ignored assets. Remove checker cubes. Preview mouse
  speed defaults to 1.0 versus prior driving-camera 0.15, with its own UI slider,
  controller speed and inversion settings under Options/Camera.
- Regression checks cover Miata's four lamps/emissive surfaces, rear/above-ground
  exhaust emitters and purge alpha for all shipped cars, floor texture detail, and
  parking transitions during streaming. User retains runtime control/testing.

- Driving camera follow-up: user also found seated mouse orbit too slow. Separate
  Driving camera and Showroom camera option pages expose each mouse speed/inversion
  and controller speed/inversion. Driving default changes 0.15 to 1.0 (6.67x),
  range 0.02–4. One-time migration upgrades only the unchanged old 0.15 value;
  subsequent choices and other existing custom values remain saved.

- Subaru/Evo headlamp follow-up: stock source headers truncate names at 27
  characters, while recovery previously ran only at length 24. Left/right meshes
  collapsed to the same HEADLIGHT_ family and pick() kept just one highest LOD.
  Recover LOD/side suffixes at any header length only when the complete name matches
  the original mesh hash. Re-export Subaru/Evo models and dependent styling targets.
  Regressions verify original left/right hashes and geometry sides at both string
  limits, distinct emitting lamp meshes for both cars, and bilateral headlight
  projection for every shipped model. No synthesized mirror/copy used as a lamp.


## 2026-10-15 — MW import foundation

- Researched NFSTools/NFS-ModTools MW solids and shared TPKs, and nfs-toolbox MW CarTypeInfo. Exact references and limits: docs/MW_IMPORT.md.
- Added PC MW parser with 104-byte material groups, named materials, repeated effect streams, local stream indices and aligned padding. Read BMWM3GTR, BMWM3GTRE46 and 997S source models.
- Added tools/export_mw_car.py, ignored mw_root config, separate mw- profile ids, U2/MW car-lot tabs, capacity 128 and catalog-driven model-only MW packaging. Current imports retain source body/tyres/brakes/lamp atlases; chassis is authored NFVU2, audio is the existing embedded fallback.
- Excluded alpha-only DAMAGE0/DOORLINE/headlamp-glass/TIRE_BACK overlays after actual Blender previews exposed opaque damage masks and square tyre backings. Original MW livery and audio remain follow-up work.
- Initial targeted checks: 542 catalog checks and 18 menu asset checks passed; 5 MW importer regressions passed. Final checks follow any remaining adjustments. These are not Unity runtime tests. No installed mod replacement or game installation writes.
- Final validation: 542 catalog checks, 18 menu checks, 5 MW importer tests, 9 installer transaction tests and one installer payload test passed. BMW Race front/side and Porsche side Blender previews inspected; excluded the alternative universal spoiler base to prevent a duplicate Porsche spoiler. Three shipped MW model files match their local generated files byte-for-byte. Source assets and paths remain ignored. Valheim runtime untested.

## 2026-10-15 — MW wheel fit correction

- Corrected BMW axle positions by sampling source BODY/BODY_TOP triangle contours and fitting the wheel arches: Street approximately +1.617/-1.119 m; Race +1.612/-1.160 m. Porsche +1.177/-1.165 m. Hub height now comes from those contours, with body lift chosen for tyre contact at y=0; no visual offset above the physical wheel centre. Per-axle track widths follow arch width and the original tyre mesh. This is geometric inference, not decoded MW VLT/collision data.
- Added wheelRubber material metadata and sidewall-annulus selection. The previous MW models could pass ModelFile validation yet fail WheelSizeVisuals construction because it only recognised the U2 rubber hash; Porsche also lists tread before sidewall. Actual sizing tests now construct and exercise the production class for all 33 cars, including slider endpoints and exact resets.
- Added axle-specific visual parts and rearTrackWidth fallback. Source FRONT_BRAKE and REAR_BRAKE are now separate in spawn, garage and Blender; old U2 profiles/parts retain shared axle behaviour. These three MW sources have only FRONT_TIRE meshes, so no invented rear wheel model.
- 245 actual sizing/axle checks, 542 catalog checks and 6 MW importer tests passed. BMW Race and Porsche side previews inspected after correction: tyres centred in arches, above-ground body clearance, distinct front/rear calipers. Build produced locally; no installed mod overwrite or Valheim gameplay validation.

## Layered original U2 vinyls
- Coordinated file ownership/build window with MW agent after user authorization. Vinyl implementation limited to VehicleStyling/Workshop/new Vinyl files; MW source models and wheel fixes retained. Added requested Roof category and read-only active-part accessors for separate MW helper.
- 30 per-preset catalogs, 26,211 original UV/coverage variants; four independently colored/opacity-controlled layers, artist palettes, 64px browser thumbnails, stable IDs and old build defaults. Original mesh UVs preserved; legacy procedural designs bake from source geometry.
- Private assets stay in ignored menu catalogs, packaged by existing allowlist; no source BINs included. Selected texture compositing off-thread; thumbnails bounded to 32.
- 52,467 vinyl/coverage tests, original decoder parity/payload tests, separate full source compile, and joint wheelsize/catalog/menu/loading/MW tests passed. Miata front/side/back Blender previews inspected, with two layers crossing panels and hood. User retains game control; Unity GUI/material runtime not verified in this turn.


## 2026-10-15 — full installed MW fleet

- 46 Racing registrations exported; 76 cars in the combined U2/MW build.
- 12,656 original part/size variants, 5,306 MW vinyls, all VLT engine-stage
  acceleration/coast GIN mappings and 371 complete source dependencies archived.
- Original per-axle dimensions, native rim-size metadata, kit track/tyre changes,
  caliper/rotor separation, tandem visuals, transparent glass/tint and MW NOS art.
- Complete source pvehicle and performance collections retained in private
  provenance; NFVU2 driving remains the adaptation. MW-only suspension loading
  now follows original mass; established U2/H2 handling remains separate.
- Managed full-fleet loader: 300,645 checks; catalog/wheel/menu/loading/model/mesh/
  audio/controller checks passed. 136 controller checks after MW mass adaptation.
- Source archive hashes and all 1,968 installed files checked by SHA-256.
  Blender reviewed 46 factory and 46 modified configurations. Unity acceptance
  is running separately; first attempt stopped before 3D because Steam was absent.
- Indexed BGRA fix coordinated with the U2 vinyl task; both source pipelines and
  U2/MW catalogs regenerated. ROADMAP encoding failure recovered from exact task
  history, preserving the pre-existing U2/profile sections and historical tail.

## 2026-10-15 — MW completion and loaded suspension height

- Coordinated suspension ownership with the U2 task. MW alone now aligns collider
  anchors to the imported resting hubs: +220 mm offroad / +132 mm road/drift,
  plus independent user lowering. Full travel, springs and damping remain intact;
  unchanged settings do not wake parked bodies. U2/H2 anchors retain prior behavior.
- Source car-solid local translations now feed body vertices and chassis bounds.
  This restores Cayman's displaced rear-left glass and corrects its 9.69 m wide,
  below-ground collider to 1.86 m width with 0.36 m bottom clearance. Generator v4.
- All 793 native decal projection meshes expose 775 original slots on 39 cars;
  428 original logos/numbers load independently of body vinyls. Bodykit alignment,
  saved builds, preview, resource disposal and optional-asset fallback included.
  Four original spoiler supports are enabled with aftermarket spoilers only.
- Full source/loader verification: 302,669 MW checks, eight Python importer tests,
  283 controller checks; catalog/wheel/loading/menu and 11 installer tests passed.
- Installed 2,008 files with SHA-256 verification and retained two timestamped
  backups. DLL SHA-256: c39b5403e924f22ee890d52ba6b86db206f056a009f8f507fa1bf61762e63a0c.
- Real Unity 6000.0.75f1 audit passed all 46 cars, all 12,656 accessory choices,
  all 428 uploaded decal textures and all 775 slots. Independent PhysX scenes
  passed 552 road/offroad/drift/lowering cases: maximum hub error 3.226 mm,
  body error 1.849 mm. Archived BMW's prior measured lift was 218.665 mm.
- Audit starts at menu and never opens a player/world. The user entered their
  world during this run; targeted final capture confirmed active play, so PID5428
  was left running. No input, player/world automation or process termination.
  Static PhysX and asset checks are distinct from manual driving acceptance.

## 2026-10-15 — world safety, service station, driver and MW TRAX

- Coordinated MW frontend/music with the existing U2 task; safety, driver and
  map/surface helpers were implemented as separate scoped agent tasks. No game
  launch or automated player input in this work.
- Spawn uses the saved world-look yaw while the garage camera orbits, limits search
  to a forward cone, verifies all wheel supports and loaded zones, and reports
  blocked placement. Station requests capture their bay for the pending spawn;
  closing the station restores normal view-directed F6 placement.
- Exit resets native fall history, with 0.8 s/local landing grace and temporary
  ignored collision pairs only for the just-exited car. No global damage filter.
  Seated hover entry/garage hints are suppressed. Parked cars hold unloaded terrain
  immediately and wait for all supports/object readiness to stabilize before thaw.
- Hammer/Crafting/NFS Garage: 10 Wood native persistent service station. Model
  purchase costs 1 Coin once; new performance levels 1 Coin each. Character-specific
  rights, free recall/downgrades/paid reuse, legacy garage migration and named-build
  accounting. Failed save rolls rights back and refunds payment; failed/cancelled
  loading never charges. Native prefab/ghost/hash/save paths checked against IL.
- Visible live avatar across all 76 models with adapted entry/exit, steering,
  pedals, shift and handbrake. Native car map pins and bounded terrain dust/grime/
  wet paint are optional; world shaders and glass/decal textures remain separate.
- 27 U2 + 26 complete MW songs; original MW VLT/MPF chains, separate radio stations,
  saved source/track selection, random starts and no-repeat shuffled bags/history.
  U2 frontend retained; optional MW frontend has 70 source graphics and 3 FE sounds.
- Targeted checks passed: safety40, streaming14, progression17 including save-fault
  refunds, surface1253, map lifecycle16, cabin319 across76 models; controls145,
  handling283, storage30, loading35, radio43, menu24. Python MW frontend/music4,
  installer11, compression5 and wizard4 (including46 native installer checks).
  Fixture catalogs corrected to valid [] for current dynamic payload validation.
- Build/package: 2040 files, DLL SHA-256 3234e373bd73e6bd8f91bf943d9ae3919b1b48cc0eb4db2f7067946dbc97203f.
  Install is tracked separately below. Native construction/coin interaction,
  in-world animation/graphics/audio remain player-controlled runtime acceptance.
- Installation completed while Valheim was closed: all 2,040 files SHA-256 verified; backup `artifacts/backups/20261015-164908-840337`. The installer retained the newest two timestamped backups. Valheim was not launched.

## 2026-10-15 — map mouse capture (local build only)

- User is actively playing on a server: no install, process/input actions or game
  launch. Installed DLL checksum and mtime were checked read-only and unchanged.
- Root cause: UIVisible omitted the large map, so the vehicle orbit camera kept
  locking/hiding the cursor and sampling clicks. Native Minimap.IsOpen IL confirms
  large-root visibility plus opening/closing grace frames. It now blocks vehicle
  input through the existing UI path, releasing the orbit cursor and release gate.
- Built locally; controls/driving targeted checks passed. Map clicking inside
  the Unity game remains unverified. This fix has NOT been installed.

## Shared vehicle naming — 2026-10-16

- Renamed runtime-owned object/material/audio prefixes to `Vehicle_`, including
  matching panel, tint, surface and import-audit lookups; source mesh names remain literal.
- Renamed `z_*` config backing fields to `baseConfig_*`; persisted config keys stay intact.
- Shared logs and UI use vehicle/SUV wording; original model profiles and resource IDs stay intact.
- SUV recognition follows `capabilities.suvTuning` instead of the built-in H2 ID.
- General GIN converter is `tools/prepare_engine_audio.py`; the unnecessary
  model-named compatibility wrapper was removed and documentation references updated.
- Source-only refactor using the existing BepInEx route; no installation or gameplay tuning.
- Validation: full asset build passed; carprofile (1023), handling (283), model (93),
  showroom, audio, radio, nitro, menu and H2 suites passed. The generic converter
  CLI was checked; conversion itself was not rerun.
- Cabin state checks ran, but its asset checks are blocked by the pre-existing
  missing `.local/interior/interior.json` (also absent from the built package).
  No Unity runtime test or installed-mod replacement was performed.

## Unified vehicle asset pipeline � 2026-10-16

- Shared `export_car.py` handles all 30 U2 identities, shared-pack wheels/spinners,
  source panel tracks, engine variants, trunk audio and mounted exhaust styles.
- Removed the H2 export/render and duplicate stock/showroom render entry points.
  Shared geometry lives in `vehicle_geometry.py`; the verified DDAY visual assembly
  is an explicit preset in `vehicle_preset.py`, reached through the shared exporter.
- `render_vehicle.py` uses embedded textures and per-profile geometry for U2/MW,
  including packed MW meshes, extra axles, panel poses and showroom selections.
- `tests/fleet` tests shared fleet/spinner state; `tests/vehicle_assets` checks every
  generated profile/model with the runtime loader. No H2 test bucket remains.
- Source animation decoding supports kind-3 channels and relative closed poses.
  Audio geometry remains in the source mounting frame and fixed to the trunk floor.
- Schema-3 exhaust accepts one to four mouths; acoustic idle uses profile minRpm.
  Schema-2 rules and built-in chassis/handling values stay unchanged.
- Generated outputs were backed up under `artifacts/unified-pipeline-before`,
  exported in isolation, then copied to `.local`; the catalog contains 76 cars.
- Blender renders inspected: open H2, scissor-door 350Z, open Golf and MW cement
  mixer with tandem axle. No Valheim installation was modified or Unity test run.
- Refreshed all 30 U2 styling catalogs against stable mesh section names.
  The customization exporter now filters U2 profiles, deduplicates built-ins,
  preserves authored rim textures and reads fitted wheel dimensions from provenance.
  Repeated canonical exports refresh existing styling catalogs automatically.
- Final verification: 6873 generic asset/audio checks across the 76-car catalog,
  plus catalog (1023), fleet/spinner (16), audio (87), showroom (24), nitro (61),
  storage (30), handling (283), menu (24), and 22 targeted Python checks passed.
- Original H2 body, wheel, spinner and brake triangle positions/UVs are all
  retained in the shared output; the DDAY preset retains its original mesh/material data.
  Canonical `export_car.py h2` was exercised, including automatic styling refresh.
- Final DLL built successfully; remaining acceptance is player-controlled Unity
  rendering, interaction and audio. No installed mod was replaced.

## U2 badge alpha and orientation correction � 2026-10-16

- User's Supra screenshot exposed opaque badge-atlas backgrounds and mirrored lettering.
- Kept original BADGING RGBA; alpha-overlay material 0x02a05578 uses the existing
  runtime blend path. Opaque atlas users retain their material behaviour.
- Shared UV correction works per connected badge island in its surface frame,
  including lettering stored rotated in an atlas. Source vertices, winding and
  outward normals are retained. Split-row H2 and fitted 350Z corrections are preserved.
- Base models and customization libraries use the same correction; the shared
  renderer now supports explicit camera/target positions for close inspection.
- Supra's missing virtual BRAKELIGHT_CENTRE uses a red lens fallback and its
  trunk section gets the brake-light role rather than a grey unlit rectangle.
- Close rear Blender preview reads Supra/TOYOTA correctly with clear badge borders.
  Unity runtime acceptance remains separate; no live installation was overwritten.
- Validation after regeneration: build, catalog/model checks across all 76 entries,
  engine-loop asset checks, lighting checks and 24 targeted Python checks passed.
  Updated close preview is `artifacts/supra-badge-close/supra-rear.png`.
