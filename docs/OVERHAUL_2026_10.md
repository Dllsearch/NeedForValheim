# Vehicle and frontend overhaul - October 2026

This is the full scope of the user-requested goal. Checks below are acceptance
requirements, not claims that an implementation has passed.

## Confirmed by the user

- Obstructed entry recovery teleports to the door successfully.
- Badges and lettering look correct in the models inspected so far.
- MW kit-dependent wheel placement works in the combinations inspected so far.

## Vehicle geometry and recovery

- Fix garage preview ground placement for the fitted 350Z and H2 after migration
  to the shared exporter. Inspect H2 wheel rear surfaces and normals.
- Remove the demonstrated dangling underbody triangles on 3000GT and RX-7 by
  correcting their source assembly/topology; preserve intended parts.
- Match U2 wheel track to each selected bodykit, including Miata and Supra;
  verify stock and widebody transitions and retain manual fitment controls.
- Exit reliably when stationary on a rock, overturned or with the driver door
  obstructed. Search safe nearby ground without placing the player in geometry.
- Add accessible Unspawn for the current world vehicle, including while seated.
  Restore the real player, camera, renderers, colliders and controls before
  destruction; preserve cargo through the existing release behavior.

## Controls and HUD

- Remove all mod function-key bindings, including persisted F-key shortcuts.
  Keep native E interactions, inventory garage access, seated Tab and controller
  navigation. Remove the upper-left function-key/status overlay.
- Replace the large permanent driving-controls panel with accessible help in
  the menu. Gauges must anchor independently of help visibility.
- Correct U2 NOS fill/placement using source artwork; replace the MW straight
  substitute NOS bar with native MW artwork. Apply chosen gauge color and align
  gear/speed/unit labels. Keep an explicitly adapted matching turbo display.

## Complete frontend and garage redesign

- NFS-like navigation and screen composition across the whole menu, not only
  typography: car selection, performance, customization, builds and options.
- Consistent transition from performance to customization; remove duplicate
  left/right Wheels destinations. Put style, color, diameter and fitment in one
  coherent wheel flow; MW diameter is a separate selector from rim design.
- Show door/trunk animation controls only when supported; describe genuine MW
  animation availability accurately. Prevent clipped roster/rim labels.
- Richer U2 workshop and MW main-menu warehouse/showroom using the supplied
  reference images for composition, props, lighting and surface detail.
- Preview continuous neon tubes, exhaust and nitro effects during configuration
  without leaving the garage. Effect preview must not consume gameplay resources.
- Fix the newly reported world-car neon regression too: the MW BMW has only a
  small green pool rather than a continuous underbody glow.

## Upstream, persistence, multiplayer and server delivery

- Review current Backwoodz/NFVU2 changelog and source commits against the fork.
  Adapt relevant streaming/lifecycle/persistence fixes without overwriting local
  features or blindly merging into the dirty worktree.
- Persist vehicles and their relevant configuration/position in the correct
  world, across save/load, join/leave and unloaded areas without duplication.
- Implement authoritative multiplayer spawning, ownership/driver arbitration,
  transform/config synchronization, departure and reconnect recovery. Validate
  with separate clients and a dedicated server, not only managed unit tests.
- Provide a headless server payload without render models, textures or audio;
  retain compact simulation/profile/network data needed for server authority.
  Clients still require their own visual/audio assets.

## Validation and operating constraints

- Preserve existing handling, correct badges, working entry recovery, original
  assets, local configuration, worlds and unrelated installer edits.
- User controls the game. Do not launch/stop/focus/input Valheim without renewed
  authorization. Never overwrite an installed mod while the game is running.
- Targeted managed tests and source renders do not prove Unity acceptance.
- Use the screenshot cases above as explicit regressions; record which actual
  gameplay checks and client/server checks are still unverified.

## Small tasks suitable for a local model

- Audit English/German controls help and obsolete function-key references in a
  named documentation/UI file; preserve locale keys and leave runtime input alone.
- Prepare concise display labels for wheel brands/styles, keeping stable IDs.
- Inventory MW door/trunk animation metadata and report supported cars with source
  paths; do not invent animations or rewrite the importer.
- Draft a regression checklist from this document and screenshots.
- Inventory reusable workshop/warehouse textures and props from already extracted
  local sources, with provenance and dimensions; do not change asset files.

Keep local-model changes isolated to one named file/task for review. Player
restoration, topology, physics and multiplayer authority remain coupled work.

## Implementation evidence - first stage

- Created the full goal and retained every requested scope item above.
- Removed F6/F7/F8 actions and migrated persisted function-key menu shortcuts to
  disabled. Inventory interaction, E, seated Tab and controller input remain.
- Removed permanent upper-left/driving help overlays. Controls now have an
  Options > Controls page; gauges use a fixed independent 24-pixel bottom inset.
- Added Vehicle > UNSPAWN with validated nearby exit placement before cleanup.
  Player/camera restoration runs in finally even if cabin visual cleanup fails.
- Exit search retains the strict driver-door position for entry and adds a
  world-up radial search for exits, including overturned cars. Validations reject
  occupied capsules, characters, moving supports, water, lava and unloaded areas.
- Preview raises the whole fitted model by its actual lowest tyre point, retaining
  wheel/body relationships and lowering compensation.
- MW models lacking a neon mask now get a continuous fallback pool and visible
  tubes. Garage projections use the isolated showroom plane and camera layer.
- Fetched upstream without merging: upstream/master b8094bc; fork HEAD is four
  commits ahead and five behind. 0.30.0 a8fd0fe persists cars via chest metadata;
  0.29.0 includes streaming/inner-surface fixes. Multiplayer remains separate work.
- Build passed; 146 controls, 11 entry timing, 40 safety and 12 mesh checks passed.
  Controls locale fixtures were corrected to the current English-source/German
  translation API, rather than changing runtime localization back to its old API.
- Game was closed before guarded DLL-only installation. See ignored
  artifacts/overhaul-{build,tests,install}.log. No native game input was performed.
- Native acceptance of this first stage is still pending. Existing safety tests
  cover placement/parking and fall grace; they do not execute the new Unity exit
  raycasts, neon rendering or preview floor adjustment. Other goal items above,
  including complete frontend, native NOS, bodykit geometry, persistence and
  multiplayer/headless packaging, remain open.

## Implementation evidence - geometry and customization stage

- The dangling 3000GT/RX-7 surfaces were original factory spoilers missing their
  base attachment transform, not corrupt triangle indices. Shared conversion now
  applies the source spoiler socket without the exhaust-specific axis rotation.
  All generic U2 models were regenerated; no stock driving profiles changed.
- U2 kit metadata now records front/rear lateral arch deltas. Sampling uses the
  original axle and exterior body triangle crossings, including carbon/textured
  widebody shells, excluding glass, badges and inner/bright trim. This is a stated
  geometric fit, not decoded NFS tuning. 108 wide kits were sampled without fit
  errors. Stock radius, height and wheelbase remain unchanged. Miata examples
  need around 11-12 cm extra per side; Supra varies with kit.
- Offline previews now apply the same kit offset/tyre-width metadata as runtime.
  Original factory-spoiler locations and Miata/Supra wide kits were rendered and
  inspected under artifacts/overhaul-geometry. H2's inner wheel geometry appears
  in the source render; the reported native rear-surface issue remains unproven
  fixed until a new game check after the floor-placement correction.
- Stable root navigation removes duplicated left-side wheel/door/trunk entries.
  Customize > Wheels contains Design / Diameter / Colour / Fitment. MW's four
  native diameter variants share one design item, retaining the selected design
  when diameter changes. Labels no longer repeat the diameter in the design list.
- Showcase groups lights, neon, nitro, exhaust and available door/trunk animations.
  Unsupported MW door/trunk animations are not offered there. Vehicle groups
  Drive / Display / Turret (where supported), keeping existing functions reachable.
- Category titles reflect the actual destination; entering Customize from another
  category starts at Paint. Roster entries use normal-size choice text rather
  than the oversized sidebar style; content labels may wrap.
- Showcase Nitro / Exhaust has an explicit Off / Flames / Purge / Backfire preview.
  The isolated preview owns its particles, uses unscaled time and allocates no
  boost/fuel/handling authority. Exiting those pages stops preview emission.
- Build, 17 pipeline regressions, 721 wheel/axle checks, 24 menu-asset checks and
  146 controls checks passed. No new native gameplay or menu-layout acceptance.
- Guarded full install completed: 2041 files SHA-256 verified; backup
  artifacts/backups/20261016-222450-629845. Installer log:
  artifacts/overhaul-stage2-install.log. All commands from this stage are terminal.
- Still open: native NOS and gauge styling, richer showroom composition, upstream
  persistence adaptation, multiplayer/headless delivery and real client/server
  acceptance. H2/350Z floor, recovery, neon and new menus still need native checks.
- 2026-10-17 neon follow-up: replaced scene-dependent particle-shader discovery
  with explicit supported unlit/transparent shader selection. Arbitrary particle
  materials may require depth/custom streams that the terrain mesh does not supply.
  Preview projection has a 2.5 cm surface bias; source mask and fallback stay intact.
  Added shader/mask and first ground-triangle diagnostics to the runtime log.
  Build and diff whitespace checks passed. DLL-only guarded installation succeeded,
  SHA-256 e92d57533a98ca83a174c74f18e674982b9a3e44b39193b8cbede794882ba996;
  backup artifacts/visual-code-backups/20261017-020129-331797.
  This is a candidate correction; the screenshot alone does not prove the root
  cause and native world/garage rendering remains to be checked by the player.

### Native MW NOS and gauge readouts - 2026-10-17

- Inspected the local original HUD_SingleRace FNG with NFSTools/FEngLib in
  ignored .local tooling. NOS MultiImage 931705 references METER_BACKING2
  (HUDTEXRACE.BIN), with HEATMETERMASKTOP as a secondary shader mask.
  Source size 152x150 versus 140x140 tach; quaternion and negative width
  project to a 140-degree screen rotation. Backing object 930097 uses the same
  art at alpha 60/255. This replaces the invented straight NOS bar.
- Exporter retains the native meter pixels at 256px. Runtime precomputes 49
  charge masks, preserving empty/full endpoints. Live clipping and user-selected
  tint are NFVU2 adaptations; the original game's widget scripts are not ported.
  Older HUD catalogs lacking MW NOS remain loadable.
- MW faces/ticks now take the configured accent. Gear/speed move into the
  source panels, use upright bold text and choose contrasting ink. Adapted
  pressure gauge is below the NOS arc with its own pointer and PSI value.
- Inspected all 11 MW variants in artifacts/hud-mw-designs.png and idle render.
  Three source-pixel tests and 29 managed camera/HUD checks passed; build passed
  with the existing CS1701 warning. These are not Unity rendering acceptance.
- Guarded two-file install (DLL and hud/hud.json) succeeded with SHA verification;
  backup artifacts/visual-code-backups/20261017-020702-179854. DLL SHA-256:
  13a9a92fcb2d0b1fae2186351efb88728b3257160b601ba91567e29b3d6a108a.
- U2 NOS layout still needs source-widget placement investigation. Native N20 icon
  and native numeric-font reproduction are not yet included. Persistence/network,
  headless delivery, richer garage and player-controlled runtime checks remain open.

### World lifecycle prerequisite - 2026-10-17

- Reviewed upstream VehiclePersistence / VehicleWorldPersistence / VehicleStorage
  at b8094bc. Its durable anchor is the existing vanilla cargo chest ZDO, including
  empty cars. Restore waits for native area load and tracks distant records for
  the garage cap. Its model discriminator only represents 350Z/H2, so it cannot
  be copied unchanged into the 76-profile fork.
- Adapted its unloaded-cargo and world-identity corrections: cargo ZDO position
  now follows a still-active car even while its native chest is unloaded; only
  the ZDO owner writes position. Captured scene/ZDOMan identity prevents old
  vehicle Tick/Release from touching a newly loaded world's objects.
- Added regressions for unloaded sector movement, nonowner writes and world
  replacement. 34 storage checks passed with API doubles; production build passed.
  This does not yet implement durable vehicle restoration or multiplayer.
- Next integration boundaries: generic versioned profile-id metadata (not a SUV
  flag); saved pose and build snapshot on the SAME cargo record; eager anchor
  creation at successful spawn commit; distinction between teardown and explicit
  removal; restore through existing staged SpawnAssets path without purchase or
  free-position relocation; reserve records during staged loading to prevent
  duplicate reconstruction. Validate saved profiles/build bounds before mutation.
  Preserve native inventory on failure and account for distant anchors in limits.

### Generic world vehicle persistence - 2026-10-17

- Added VehiclePersistence.cs and VehicleWorldPersistence.cs adapting upstream's
  one-vanilla-ZDO identity to all catalog profile IDs. New cars eagerly create a
  cargo anchor even when empty. Versioned metadata stores source profile, exact
  world pose, driven state, trunk layout and bounded serialized build settings.
- Existing cargo ZDO is reused on restore. Restoration waits for native area
  loading and uses the existing staged asset builder, preserving saved location
  instead of searching for a new spawn point or charging another purchase.
  The restored rigidbody begins parked until terrain readiness permits physics.
- World save and shutdown capture current snapshots. Teardown keeps the anchor;
  explicit removal clears vehicle metadata and retains nonempty cargo in the
  same native chest. A canceled/failed restoration retains its anchor and defers
  retries for that world session; unknown profiles/invalid poses are logged.
- Added source-compatible cargo visibility component so hiding during native
  Container.Awake and later attachment does not overwrite the original visible
  states. Marked cargo is protected from native wear/removal handlers.
- Distant records count against the four-car cap and prevent duplicate profile
  spawns; the garage shows a distant-record hint and legacy recall excludes
  profiles already saved in this world. No multiplayer authority is enabled yet.
- Passed 43 storage/persistence checks, 35 staged-loading checks and 146 controls
  checks. Updated the stale loading fixture from 11 to 22 actual HUD variants.
  Build and diff whitespace checks passed. API doubles do not prove a real world
  save/rejoin, Unity chunk reload, native Harmony timing or multiplayer behavior.
- Guarded DLL install succeeded; SHA-256
  29f84c5b72f32d3c8e55c95c217b3d83b26e2e46a8e1fcdb940cc8733a764e86;
  backup artifacts/visual-code-backups/20261017-021650-029434. Runtime acceptance
  remains under player control: spawn with/without cargo, save/rejoin, return to a
  distant car, then explicitly remove and verify cargo contents and player state.
- Still open: older upstream v1 anchor migration, distant record management,
  multiplayer ownership/replication, server payload, U2 NOS source placement,
  richer showroom and remaining native geometry/neon/UI checks.

### Distant vehicle management and upstream v1 compatibility - 2026-10-17

- My garage lists unloaded/deferred saved vehicles with profile name, distance,
  status and explicit REMOVE. Unknown profile IDs remain visible/removable.
  Loaded plus distant entries share the four-car capacity display. Legacy recall
  is shown only when there are no current world records, preventing overlapping
  rows and duplicate recall of already-persistent cars.
- Distant removal clears only vehicle discriminators when inventory is unloaded;
  its native cargo record is retained regardless of unknown item count. Loaded
  records load their existing inventory before deciding whether to destroy an
  empty chest or restore a nonempty vanilla chest. Busy storage refuses removal
  before metadata mutation. Singleplayer world identity/ownership guards remain.
- Read upstream v1 integer discriminators and original pose/layout/driven fields.
  350Z/H2 map to actual catalog IDs. Their missing build snapshot uses existing
  local per-profile configuration. First successful generic snapshot publishes
  v2 then clears v1; explicit removal clears both, preventing resurrection.
- Corrected garage label character encoding and the capacity label that still
  counted only loaded fleet entries. Source reads/writes for these edits are UTF-8.
- 52 storage/record checks and 146 controls checks passed; build and whitespace
  checks passed. Native UI/world lifecycle acceptance remains outstanding.
- Guarded DLL installation succeeded with SHA-256
  592767b89ca810e5fa3e68384af30e0356120163d041f2e0c502d4e4a333fa77;
  backup artifacts/visual-code-backups/20261017-022000-606812.
- Multiplayer synchronization and the minimal server payload remain unimplemented;
  the multiplayer guard is deliberately still active until authority/replication
  are implemented and tested.

### Asset-free server build - 2026-10-17

- Added separate server/ServerPlugin.cs entry point and tools/build_server.py.
  Shared catalog, storage-capacity and persistent-record code compile into the
  server DLL, excluding the entire client frontend/renderer/audio implementation
  and its embedded WAV resources. Existing plugin ID/DLL filename are preserved;
  client and server variants must not coexist in one installation.
- Development server component protects saved cargo identities from native wear
  and suppresses native chest renderers/colliders. Network driving/replication is
  explicitly not enabled yet; readiness is false in its manifest and README.
- Generated artifacts/NFVU2-server.zip: 26,749 bytes, 76 profiles. SHA-256:
  ed718d0e0bba625182dee19169462e6d900cc8ef747b819afc8b2a3fe4a653de.
  Exact ZIP allowlist: server DLL, cars.json, README and checksum manifest.
- tools/test_server.py passed archive/file checksum checks, strict asset-free file
  list, exact client/server catalog parity, unpacked size budget and real assembly
  inspection: zero embedded resources and no audio/image/input/IMGUI/particle
  module dependencies. Build uses only core/physics/native API and loader refs.
- No server was installed or launched. Native headless startup and multiplayer
  authority, state replication, join/leave recovery and two-client acceptance
  remain outstanding. See docs/SERVER.md for the current delivery boundary.

### Server ownership and state protocol - 2026-10-17

- Inspected actual installed ZRoutedRpc/ZNet/ZNetPeer/ZPackage/ZDO signatures with
  read-only assembly reflection; reference output artifacts/network-api.txt.
- Added shared bounded/versioned VehicleNetworkProtocol and connection-scoped
  VehicleNetworkLeases. One connection holds at most one driver lease; a second
  driver cannot steal it. Only the holder can publish, with monotonic sequence,
  finite pose/velocity, bounded displacement, update rate and four-second timeout.
  Disconnect releases authority and server restart clears stale driver/velocity.
- Added actual native routed-RPC server handlers for hello, spawn, claim, state,
  release and remove. Server derives player identity/character position from the
  connected peer. Spawn validates catalog/setup, proximity and per-player four-car
  / duplicate-profile limits. Server owns the persistent ZDO; driver simulations
  publish motion snapshots, not direct ownership changes to the cargo record.
- State updates include pose/velocity/steering/wheel phase/boost. Cargo anchor
  follows the car with a grounded trunk recovery location where terrain is loaded.
  Remove requires the persistent creator and a vacant seat/closed inventory.
  Cargo records are retained. Unowned imported world records are adopted by their
  first nearby driver only when that player's garage has room. Legacy metadata
  upgrade preserves pose/layout without constructing a client model/Transform.
- Server-only cargo visibility now restores native components when vehicle
  metadata is removed. Maintenance prunes disconnected rate buckets and forgotten
  records; it runs at 10 Hz rather than repeatedly scanning all records per frame.
- Passed 22 authority/lease checks and 53 storage/legacy checks. Client and server
  compilation passed; server packaging/resource/dependency/parity checks passed.
  Latest server ZIP is 33,188 bytes, SHA-256
  a34804a5dc5bf93764fbb72c4f3657cc0f4f85c5ed6b34b52f48b6db3ad3ac68.
- This is the server half only. The client adapter, remote visual interpolation,
  native avatar/seat integration, setup changes, cargo interaction and two-client
  transport/gameplay tests remain outstanding. Client multiplayer guards remain
  active and package networkReady stays false. No server was launched/installed;
  the latest client compile was not installed.

### Client transport and host routing - 2026-10-17

- Added VehicleNetworkClient with verified server UID/version, correlated request
  IDs, five-second deadlines, bounded pending requests and packet sizes, and a
  periodic server heartbeat. Wrong-sender/version, duplicate, malformed, trailing
  or late replies cannot grant authority. Timeout/world switch cancels callbacks;
  loss of heartbeat revokes protocol readiness. Client updates now run this
  connection service, but readiness does not bypass the existing multiplayer gate.
- Split request/reply RPC names so a host can receive server requests and client
  replies on the same Valheim router. Moved server handler into shared source
  VehicleNetworkServer; dedicated and hosted servers use the same handler.
  Host identity comes from the actual local Player/character, not a fabricated
  remote peer. Broker lifetime survives world changes without duplicate route
  registration; internal world-scoped records/leases are reset separately.
- Passed 19 client transport checks plus 22 lease/authority checks using API
  doubles. Client/server builds and server package checks passed. No native peers
  were started, connected or driven. Latest server ZIP: 33,425 bytes; SHA-256
  cbb1868a292bfbc4b7d1273b8f29fc2f58a72321cf64c61c0dd1afe14235bfaa.
- These changes are built but not installed. Next: map native network records
  into staged client vehicle loading without touching server cargo ownership;
  render observers without local wheel physics; gate native player entry on the
  server claim, publish/release driver state and restore player on lease loss.
  Style updates/cargo/economy and native multi-client tests also remain open.

### Network observer loading and independent settings - 2026-10-17

- Added per-instance NetworkVehicleSettings backed by unsaved in-memory BepInEx
  entries. Canonical profile definition names and acceptable ranges remain shared
  with saved build snapshots; two players using the same model no longer share
  paint, parts, rims, decals or driving configuration. Missing network values use
  factory defaults rather than the local user's current customization.
- Network records use the normal staged SpawnAssets/BuildVehicle path with their
  own read-only ZDO reference. This path never constructs/claims local cargo,
  charges purchases, publishes world snapshots or changes the saved roster.
  Cancellation disposes only local resources and preserves the server anchor.
- Observer discovery considers native loaded zones and both legacy/current
  records, loads the nearest eight inside 160 m and releases farther/unloaded/
  forgotten instances. The regular four-car ownership policy remains separate
  from the bounded eight-instance rendering capacity. Unloading does not remove
  world identity/cargo. Unsupported/failed models are deferred for that world.
- Observers have kinematic bodies and disabled WheelColliders. Server poses are
  interpolated with capped 100 ms extrapolation; wheel steering/phase, source
  model styling, lights, neon and boost effects use replicated state. Remote
  wheel vertical travel, engine telemetry/audio, driver avatar attachment and
  door/flight animations are not yet fully replicated.
- Client multiplayer entry/menu guards remain active. Protocol-ready sessions
  may load observer visuals, but no driver can bypass the server lease through
  the old local Enter path. The next step is explicit claim/entry, publishing,
  release/lease-loss recovery, followed by styling/cargo/economy integration.
- Build and whitespace checks passed. 17 fleet, 19 client transport, 22 authority,
  11 settings isolation and 35 staged loading checks passed. Settings tests use
  the real installed BepInEx configuration assembly and prove no config files are
  written. No Unity scene, real network peer or observer render was executed.
- Latest client build is not installed; the installed gameplay build still
  predates network changes. Native multiplayer acceptance remains outstanding.

### Client driver lifecycle and network spawning - 2026-10-17

- Added request/claim/entry lifecycle. Existing Enter cannot start network physics
  until the server grants a driver lease. Callback revalidates world, local player,
  distance, vehicle membership, terrain loading and other active driver state.
  Failed/canceled entry releases the lease. Only the granted driver enables its
  WheelColliders and normal proven handling; observers remain kinematic.
- Active driver publishes sequence, world pose, velocity, steering, wheel phase
  and boost at 20 Hz. Loss of protocol readiness, vehicle deletion or revoked
  server driver restores the actual character through the existing guarded Exit
  cleanup, then freezes the local car into observer state. Exit sends final state
  and release; release is not delayed by the server's command-rate limiter.
- Added server-backed spawn and removal requests, pending-request UI and bounded
  waiting/cancellation behavior. Network spawn never creates a local cargo chest
  or charges a local-only purchase. Safe seated Unspawn uses the same exit search
  before requesting removal; server creator/occupied-seat checks remain decisive.
- Compatible protocol readiness now allows the development network client path.
  Singleplayer-only world-save, cargo, horde and turret routines retain explicit
  network guards; they cannot mutate server-owned ZDOs or spawn local test waves.
  Network sessions currently use sandbox purchases, explicitly labeled in the UI;
  singleplayer coin-economy settings are retained. Network performance/main workshop editing and cargo are
  gated pending synchronization. Showcase controls still need their network
  integration, so this build is not ready for installation/acceptance as a full
  multiplayer UI. Native cargo visibility restores after metadata loss.
- Selection/spawn/removal distinguish this player's cars from another player's
  instances of the same model. Nearby owned count no longer includes observers
  belonging to other players; server retains authoritative distant ownership
  limits. A complete server roster for distant management is still outstanding.
- Client and server builds, packaging checks and 319 focused checks passed:
  22 leases, 19 transport, 11 real-BepInEx settings, 53 storage, 17 fleet,
  146 controls, 11 cabin entry and 40 safety. None execute real network peers or
  native seating/physics. Latest server ZIP: 33,423 bytes, SHA-256
  21fbce74456a50697eaf62f84226bd92f7a24e0b6764a740d85c16c45154677d.
- No installation or game/server launch in this stage. Latest client and server
  remain development builds with networkReady=false for release purposes.
  Next: end-to-end protocol harness/native session, server roster, full styling/
  cargo sync, remote avatar/audio/flight/wheel travel, and disconnect/rejoin tests.

### Serialized protocol integration and distant network roster - 2026-10-17

- Added tests/networkintegration: real VehicleNetworkClient and
  VehicleNetworkServer handlers exchange serialized packages through a mocked
  native routed transport with separate endpoint contexts. This covers actual
  request/reply parsing and server world mutations, not just the lease helper.
  64 checks cover spawn/claim/state/release/remove, foreign driver/deletion denial,
  duplicate/reordered/teleport state, four-car limits, independent same-model
  ownership, disconnect/timeout and persistent player identity after peer change.
  This is still API doubles: no sockets, Unity scene, disk world save or avatar.
- Added Roster request/reply containing bounded own profile/id/position/driver
  summaries. Server filters by authenticated persistent player identity and uses
  a separate query throttle. Client validates count, identities, finite positions,
  framing, server sender and correlation before replacing its cache. Periodic
  refresh and successful spawn/claim/removal keep it current across zone unloads.
- My garage count/list now uses the authoritative own roster, including distant
  models; other players' observer instances do not occupy its rows. Distant
  removal uses the saved ID directly, retaining cargo without loading its model.
  Taking an already-saved distant model directs to the roster instead of spawning
  a duplicate. Empty network ownership has an explicit empty-garage message.
- Successful removal immediately removes the roster item and remembers a bounded
  recent-ID set. Observer discovery ignores those IDs, so lagging native ZDO
  metadata cannot immediately reconstruct a deleted car after the RPC reply.
- Inspected actual installed get_IsSinglePlayer IL: it is m_isServer &&
  !m_openServer, independent of peer count. Therefore losing the last peer does
  not switch an open host into the singleplayer path. Evidence:
  artifacts/network-mode-il.txt. No speculative mode-condition rewrite needed.
- Client/server compilation, packaging checks and focused network/settings/
  storage/fleet tests passed. Server ZIP: 33,688 bytes; SHA-256
  789702ab3156cc36996f6f9c0fd5d0aceca3a10311cbd0a7f44e66c79672129c.
  Changes are not installed; networkReady remains false. Native transport,
  seating/physics, styling/cargo synchronization and remote presentation still
  require implementation/acceptance before multiplayer is complete.

### Revisioned network setup and live garage drafts - 2026-10-17

- Added Setup command and committed setup revision. The server checks persistent
  creator, proximity, parked velocity, driver lease and expected revision before
  accepting a bounded snapshot. Stale or foreign writes cannot overwrite it.
- Setup changes load the existing native cargo inventory before any capacity
  reduction. Busy/loading or overfull cargo rejects the change before snapshot
  mutation; legal changes repack existing item grid positions, resize the native
  container and publish layout/setup/revision together. Spawn and updates store
  an explicit canonical trunk-layout key, preventing a later paint edit from
  silently selecting a different capacity through client defaults.
- Added per-instance draft tracking. UI edits publish after a short debounce;
  runtime visual fallbacks and server refreshes do not become unsolicited writes.
  Read-only/in-flight edits are reverted immediately. Settings retain authored
  server values when the local asset catalog needs a display fallback.
- Client waits for acknowledgment, handles expected revision and rolls back
  rejected drafts. Newer ZDO setup revisions refresh each observer's settings.
  A lost acknowledgment can be recognized from the committed matching snapshot.
  Setup commands are paced across vehicles to avoid self-triggered rate rejection.
- Isolated garage preview shares the selected network instance's settings;
  same-model different instances now invalidate the preview selection correctly.
  Main performance/styling editing is enabled for a nearby parked owned car.
  Legacy snapshots use saved trunk-layout metadata rather than the default layout.
- Passed 79 serialized handler integration checks, 15 real-BepInEx settings
  checks, 53 storage, 19 client transport, 22 authority and 146 controls checks.
  Both builds and asset-free server package verification passed. No native scene,
  sockets or rendered menu/gameplay acceptance was executed.
- Server ZIP: 34,783 bytes; SHA-256
  bd6836573148841488de12b59b06a95317e94223236e97191d5b1e130b77f6ed.
  Builds are not installed and package networkReady remains false. Native cargo
  opening/editing is still gated: it needs the installed game's inventory/open/
  ownership lifecycle adapted coherently to server authority. Remote avatar,
  engine/audio, flight and suspension replication and real client/server tests
  remain open, along with the outstanding nonnetwork overhaul items.

### Native cargo ownership protocol - 2026-10-17

- Inspected installed Container RPC_RequestOpen/RPC_OpenResponse, SetInUse,
  IsInUse, Save, Load, OnContainerChanged and UpdateRows IL. Native open force-
  sends the existing ZDO, transfers ownership to the requesting peer and opens
  InventoryGui. Inventory changes save only on the owner. IsInUse is a local
  field, so checking it only on the dedicated server cannot protect remote edits.
  Evidence: artifacts/cargo-api-il.txt and artifacts/cargo-more-il.txt.
- Added separate cargo access/keep/close commands and connection-scoped leases.
  The server validates persistent creator, proximity, stopped vehicle and vacant
  driver/cargo state. Cargo access blocks driver claims, setup changes and removal;
  the accessing peer cannot simultaneously acquire a driver's lease elsewhere.
- Native container-open RPC is gated on that explicit grant and authenticated
  sender/player identity for dedicated and hosted servers. An owning client
  cannot grant a second peer native access. This preserves the native inventory
  path while preventing it from bypassing vehicle locks.
- While cargo access is valid, server maintenance leaves native client ownership
  intact. Close carries a SHA-256 of the final native item bytes; ownership is
  reclaimed and acknowledged only when the SAME existing ZDO's item field matches.
  No custom item reserialization, copying, stack splitting or replacement chest.
  Disconnect/expiry retains the last replicated native inventory and recovers
  server ownership. Transient cargo peer metadata resets on world/server reload.
- Passed 97 serialized handler integration checks, 19 client transport, 22 leases
  and 53 storage checks. Client/server builds and server package checks passed.
  Tests simulate native inventory-byte arrival/ownership; no real InventoryGui,
  socket ordering, drag/drop, forced send or disconnect/save acceptance executed.
- Server ZIP: 36,323 bytes; SHA-256
  89edd1d8f6dd5042e01e61723d5c53f97946ca8a7fe16b745ebe0c9c0af5af03.
  Builds are not installed. Client network storage button remains gated until its
  native Interact/OpenResponse preparation, UI tracking, heartbeat, final native
  Save/ForceSendZDO and close-ack retry path are wired. Vanilla inventory ownership
  remains a trusted-client mechanism, not an anti-cheat inventory implementation.

### Client native inventory bridge and confirmed close - 2026-10-17

- Added VehicleNetworkCargo: the owner requests cargo access, then uses native
  Container.Interact / RPC_OpenResponse / InventoryGui for actual item editing.
  It prepares the existing native container at the saved trunk position, loads
  its inventory and applies UpdateRows so authored item rows remain visible.
  Cargo title and trunk preview state are local and restored afterward.
- Native open responses are accepted only for the active granted request; late
  responses cannot reopen a closing/abandoned window. Access is renewed while
  native inventory is open or its final snapshot is being confirmed.
- Closing saves the existing native inventory while still owner, force-sends its
  ZDO to the server and submits the final item hash. Retries retain native access
  until acknowledgment or explicit expiry. A server commit marker also confirms
  completion if the routed acknowledgment is lost. No item payload is copied into
  a replacement chest or reimplemented transfer protocol.
- Unused access can cancel with an empty close only while the server still owns
  the record; this handles native ward/access denial without waiting forever on
  a stale client inventory hash. Disconnect/world change clears local GUI state
  without calling a newly loaded world's inventory UI or serializing as nonowner.
  Shutdown requests final native save/send without blocking the game on a wait.
- Fixed a related seated Unspawn bug: urgent driver release/cargo keep/close no
  longer stamp the general command limiter. A following removal can now succeed
  immediately instead of being rejected by the release's own timestamp.
- Passed 101 serialized integration checks, 19 transport, 22 authority, 53 storage,
  15 settings and 146 controls checks. Client/server compilation, package checks
  and whitespace checks passed. Server ZIP: 36,448 bytes; SHA-256
  e4d536f05b3def3b40c5346367ce4ba017e2ef2bb5f9eb76279388ad90c8897a.
- No installation or native UI/server launch. Native drag/drop, ward denial,
  ForceSendZDO/socket ordering, close/reopen, disconnect and quitting with a held
  item still need real client/server acceptance; managed checks do not prove them.
  Remote avatar/audio/flight/suspension, U2 NOS layout, richer garage and remaining
  vehicle/neon/runtime acceptance requirements remain open. networkReady=false.

### Remote wheel/flight/engine telemetry and fuel handoff - 2026-10-17

- Bumped motion protocol to version 2; mismatched development clients fail the
  handshake rather than parsing different state layouts. Shared telemetry carries
  all four raw local wheel poses/RPM, engine RPM/load/gear/shift, contact/slip,
  throttle/brake/horn/burnout/purge/backfire, flight fold/vector/thrust and charge.
  Server validates the complete payload before consuming sequence or writing ZDOs.
- Observers use transmitted wheel suspension/rotation before the existing camber,
  mirrored-side and flight-fold transforms. Spinners receive remote wheel RPM.
  Native observer wheel physics remain disabled. No guessed fixed hub height or
  kinematic-body velocity assignment is needed for remote sound.
- Added engine-model Observe path and optional audio snapshot input. It uses the
  same original staged power/coast bands, turbo/road/reverse/horn mixing and source
  lifetime management, while taking actual remote RPM/load/gear/contact values.
  Observer flight thrusters, flight audio, purge and edge-triggered backfire are
  connected. Engine stage selection follows the per-instance saved setup.
- Driver release can carry a final full state before freeing its lease; temporary
  horn/boost/purge/backfire states are cleared. Disconnect also stops running
  engine/effect metadata. Parked idling still honors the final running state.
- Driver claim restores the saved charge into the shared ground/flight NitroState;
  the server rejects abrupt refill jumps. Local world snapshots also retain
  charge, with a full-tank fallback for old records. This avoids refilling fuel
  simply by handing the seat to another client or reloading a saved car.
- Passed 105 serialized integration checks, 19 transport, 22 authority, 64 nitro,
  89 audio, 12 geometry and 35 loading checks. Both builds and package validation
  passed. These do not prove remote wheel visuals, actual audible playback,
  native physics or a connected Unity session.
- Server ZIP: 38,205 bytes; SHA-256
  d79a97bafe87f583a73d8e6962f44e42771adbfeab136450f8b8b91da82abdf7.
  Updated server README/startup messages to describe the implemented prototype
  accurately. No client/server installation or launch. networkReady remains false.
- Remaining network work includes remote driver avatar presentation, dedicated
  collision/world interaction coverage and native two-client lifecycle acceptance.
  Flight abandonment/recovery and remote driving-mode continuity also need native
  inspection. U2 NOS, richer frontend scenery and outstanding geometry/neon/HUD
  player checks remain part of the unchanged full goal.


### Neon correction follow-up (2026-10-17)
- Player screenshot confirms the previous neon candidate was insufficient; native
  neon acceptance remains open. Four spot lights alone are not the intended result.
- Shared world/garage projection now uses a continuous underbody mask for every
  model and prefers the already-loaded Standard shader with explicit transparent
  blending and tinted emission. Tubes use a white emission map; terrain projection
  retains its mask. This removes the untinted Unlit/Transparent candidate and
  dependence on per-car masks for continuous illumination.
- Client build passed (artifacts/neon-continuous-build.log). Not installed: the
  current build also contains unfinished multiplayer work. No game was launched
  or controlled. A clean gameplay-only install and player visual check remain.
- Multiplayer is paused until outstanding gameplay/visual bugs are closed.


### Gameplay-only follow-up installed (2026-10-17)
- Default client builds disable prototype network startup and observers. Explicit
  tools/build.py --experimental-network enables it for future development only.
  Single-player persistence remains enabled; no multiplayer readiness claim.
- Native carbon parts retain their original tint and material finish independently
  of body paint/finish. Carbon vinyl still uses its own original artwork, so exact
  texture color equality is not assumed.
- Added U2 workbenches/drawers, tyre stacks and racks; MW packing crates, cylinders
  and tyre stacks. All props reuse scene materials and have no world colliders.
- Built and passed controls, vehicle_safety, cabin_entry, storage, loading,
  lighting, menu, wheelsize and mesh suites plus all three source HUD asset tests.
  Offline U2 NOS render retains authored arc and pivot; no speculative layout
  adjustment. Player-reported U2 NOS alignment remains unresolved.
- Guarded installation completed with Valheim closed, backup and matching SHA-256:
  8bcb9f06d28959df84d7dfc549321eccc4864115ff072152870184b00692cf13.
  Logs: artifacts/gameplay-followup-{build,tests,hud,wheels,install}.log.
  Backup: artifacts/visual-code-backups/20261017-073631-561799.
- No Unity input, launch or runtime acceptance performed. Next player checks:
  continuous world AND preview neon; safe exit/Unspawn while overturned; fitted
  H2/350Z floor, H2 inner wheels, 3000GT/RX7 underbody and Miata/Supra wide kits;
  U2 NOS at partial/full charge and all MW HUD color/text. Full NFS-like frontend
  fidelity and final lighting/scenery acceptance remain open.


### U2 NOS alignment correction (2026-10-17)
- Player clarified that the NOS ends intersected the tach while the middle had a
  gap. Source contour measurements found the previous pivot (100,122) wrong:
  native ring centre is approximately (92.5,110.5), outer radius about82px.
- Centre NOS on tach (140,170), increase source scale from1.8 to2.1 so its
  roughly68px inner radius clears the140px tach. Measured inner-edge radii at
  -170,-140,-110,-80 degrees are140.7 to143.3 screen pixels after correction.
- Fill clipping uses corrected centre and150-degree sweep; lower-left tail no
  longer wraps to full progress via atan2. Updated offline renderer identically.
- Build,29 camera/HUD checks,3 original HUD asset tests and diff check passed.
  All11 U2 style previews inspected offline; native acceptance remains pending.
- Installed with game closed, backup and verified SHA-256:
  915f00be2c9baca2bf1e83412613cca3ae7050b734c48b736c44c892e6de79ed.
  Logs artifacts/nos-alignment-*.log. No game launch or input.


### NOS inner-edge correction supersedes previous alignment
- User correctly identified reversed mismatch in previous preview. Previous
  uniform-scale alignment was insufficient and should not be considered accepted.
- Fitted inner opening instead of outer silhouette: source centre94.374,113.776,
  horizontal/vertical radii70.638,71.836. Independent scales2.053/2.018 align to
  tach centre140,170 with target inner radius145.
- Sampled131 arc directions (-175 through-45 degrees): projected inner edge
  radius143.87..147.07 versus tach140, giving3.87..7.07px clearance. Enlarged
  preview inspected: artifacts/nos-inner-edge-preview.png. Native check pending.
- Build passed. Guarded installation succeeded with backup and verified SHA256
  1cd15f11d51db2293797e64be2196e4a4cc5e7abf0c673c1bd4f39ddff00f590.


### Shared U2 reference and MW depletion direction
- User requested ONE shared U2 NOS layout using08 as graphical reference.
  Removed interrupted per-design metadata/export helper; no per-style offsets.
  Shared editable constants are HudMath.U2NosX/Y/Width/Height in src/HudFile.cs.
  Renderer uses them in VehicleDashboard.Draw; offline equivalent render_hud.py.
- Shared rectangle(-53.33,-58.47,520.45,248.70) calibrated against08. Comparison
  artifacts/nos-shared-reference08.png includes07,08,09,11 at full charge.
- User explicitly prefers MW depletion TOP TO BOTTOM. Reversed MW source progress;
  preview artifacts/mw-nos-top-down.png verifies100/75/50/25 percent retains the
  lower arc as charge decreases. This is requested behavior, not a proven claim
  about the original game's animated depletion.
- Build and29 camera/HUD checks passed; diff check passed. Installed with game
  closed, backup and SHA2568cb5fd4b1e3611b1735ce8feb8d374a592719cf5b74d0574d2b7b37854acdba0.
  Native acceptance pending. Logs artifacts/nos-shared-{build,tests,install}.log.


### Multiplayer resumed by user (2026-10-17)
- User explicitly reprioritized multiplayer over remaining single-player visuals.
  Goal API reports usageLimited; no tool can resume this status. Manual UI budget
  restoration remains required for automatic goal continuation. Work continued
  within the directly requested turn; no goal completion claim.
- Protocol v3 adds powered-landing state and ground driving mode to telemetry.
  Driver claim snaps to the authoritative persisted pose, restores charge/mode,
  enters through normal player lifecycle, then resumes flight/fold at the current
  height without a takeoff jump. Reclaim resets stale local flight controllers.
- Runtime mode updates stay separate from workshop edit permissions and saved
  creator builds. Remote engine audio follows transmitted terrain mode.
- Failed entry releases authority without overwriting suspended flight telemetry;
  callback failures restore/release control rather than leaving physics active.
- Experimental client output is now artifacts/network-client/, selected by
  tools/build.py --experimental-network. Normal artifacts/mod output rebuilt with
  networking disabled. Neither build installed in this stage.
- Client and dedicated builds passed.41 flight,19 transport,22 authority,
  17 settings,108 serialized integration and146 controls checks passed. Package
  checks confirmed asset-free server dependencies and profile parity. These do
  not validate real sockets, Unity seating/physics, native inventory or saves.
- Server ZIP39922bytes SHA256
  b6eb8fe50536631c242b0f9be35240cd71a645dc219dc3921df4c32522ed975b.
  Logs artifacts/network-resume-*.log. No game/server launched or controlled.
- Next: real isolated server startup and two-client driver/cargo/reconnect tests,
  remote avatar/collider inspection, safe recovery of abandoned airborne cars.
  Cars currently retain their last replicated pose after driver loss; there is
  no server-powered autonomous landing. Full overhaul acceptance remains open.


### Recovery and native server discovery (in progress)
- Previous goal turn changed authority/runtime state and yielded passing tests;
  classified as progress. User redirected discovery to other disks.
- Found installed dedicated server through Steam library metadata (drive E).
  It has no installed BepInEx; original server files/start scripts/worlds unchanged.
- Added tools/build_server.py --game-dir to compile against real dedicated-server
  assemblies, including valheim_server_Data. Compilation and asset-free package
  validation passed against that installation.
- New RECOVER protocol command and owned roster buttons relocate a vacant car to
  a validated nearby pad, retaining identity/setup/fuel/native item bytes. Server
  checks creator, leases, nearby authenticated character and native placement.
  Same placement checks now gate Spawn.124 serialized integration checks passed.
- Native smoke lab under ignored .local/server-lab uses a separate copied server,
  unpacked cached BepInEx pack, server-only mod and disposable world directory.
  Initial run loaded both plugins (zero failed), NFVU2 loaded76 profiles and native
  world/prefabs initialized. Steam authentication failed in restricted execution;
  PlayerPrefs writes also failed. Early watchdog quit during location generation
  hit native Game.OnApplicationQuit -> ZNet.EnoughDiskSpaceAvailable null-profile
  exception despite process exit0. This is NOT clean/full server acceptance.
- Stronger watchdog waits for native m_locationsGenerated; currently under test.
  Original game/server installations have not been patched or launched directly.


### Native dedicated-server smoke verified after cross-disk discovery
- Steam app896660 found in the E Steam library. Original install/start scripts
  and original worlds unchanged. No game client launched or automated.
- Dedicated build against its own assemblies passed; server package41154bytes,
  SHA256d709f27b6d03fb4bd6cb1dfc37eb9eb7c8fbf4b25074673c0e5f1b2c757cc3c4.
- Isolated copied runtime: .local/server-lab. Loader came from existing cached
  BepInExPack_Valheim-5.4.2351.zip; no downloads. Dedicated DLL/catalog only; no
  client mod assets. Native watchdog waits for m_locationsGenerated and exits.
- Restricted runs had Steam/prefs restrictions and native shutdown exception.
  Approved unsandboxed run resolved these: Steam connected, dedicated=True,
  locations generated, NFVU2 loaded76profiles, chainloader2loaded/0failed,
  no NFVU2 MissingMethod/TypeLoad/Harmony errors, orderly ZNet shutdown, exit0.
  Vanilla headless video/shader warnings remain; no GUI or renderer acceptance.
- New native save format files were written in isolated worlds directory:
  _main.1.db2 (147937bytes), _main.1.fwl2, .chunks/.ok and chunk1464bytes.
  Log artifacts/native-smoke-generated-success.log; restricted evidence retained
  separately. This proves native bootstrap/generation/save/shutdown, not vehicle
  save/rejoin, real client RPCs or two-client driving/cargo/ownership acceptance.
-124 serialized integration checks and package validation remain passing.
  Goal remains active. Next: native two-client exercise and vehicle-specific
  save/reload, collision/avatar/cargo acceptance. Gameplay installation unchanged.


### Native vehicle persistence across actual server restart
- Previous turn classified progress: native bootstrap and world generation/save
  evidence changed authoritative acceptance state. Goal remains active.
- Native fixture source retained at tests/servernative/ServerPersistenceSmoke.cs;
  test-only plugin is confined to the ignored disposable lab, never gameplay.
- Created one real piece_chest ZNetView/ZDO and called production persistence Save
  with350z, pose25/26/27, paint3, charge0.42 and a synthetic creator ID. Added3Wood
  through native Inventory.AddItem and saved via native Container.Save.
- Clean shutdown wrote the real world. Next native startup found exactly one
  matching logical vehicle and verified every snapshot field and native inventory
  item/stack. Native ZDO prefix remapped2556505712:82 ->1:82 as expected for world
  serialization; no assumption that live session prefix stays constant was used.
  Logs artifacts/native-vehicle-{create,reload}.log, both exit0.
- Latest server binary rechecked in the same lab after guarded DLL backup/copy/SHA
  verification. Native reload again passed; log native-vehicle-reload-latest.log.
  Lab backups pruned to newest2 after successful copy/test; real installs untouched.
- Fixed first-driver/parked setup mode continuity: initial saved Road/Drift settings
  seed handover mode, and an explicit parked mode edit updates it.125 serialized
  integration checks passed, plus package validation. Experimental client built.
- Latest server ZIP41365bytes SHA256
  26a467417cddc1dd9341cac9106f4c11c01d370aac08068fe8bc19ac1b501611.
- Added docs/NETWORK_TESTING.md with native evidence boundaries and actual two-client
  acceptance steps. Asked user about second-client availability; do not automate
  gameplay without renewed authorization. Socket/driver/cargo GUI/remote avatar
  acceptance remains open. No client DLL installation or game input in this stage.


### Runtime settings and native hull acceptance
- Previous turn classified progress: actual server restart verified vehicle and
  native cargo persistence. No goal completion/blocking claim.
- NetworkVehicleSettings now preserves runtime controls through server refresh
  and rejection rollback, without leaking them into an edited/saved workshop
  snapshot. Explicit workshop edits still replace that key's runtime override.
  Replace rolls back values, edited keys and runtime baseline together on failure.
-20 settings checks passed against real BepInEx; experimental client build passed.
  Log artifacts/network-runtime-settings-{tests,build}.log.
- Extended test-only native fixture to create a temporary cargo anchor, publish
  production vehicle metadata, run the actual server hull manager and verify
  its non-rendered kinematic chassis through native BoxCollider.Raycast. Deleting
  the anchor removed the proxy on subsequent frames. Markers HULL_QUERY_OK and
  HULL_CLEANUP_OK, plus vehicle reload OK and process exit0, are recorded in
  artifacts/native-vehicle-hull.log. This is real Unity physics query/lifecycle
  evidence, not proof of moving-car/player/peer collision behavior.
- User's second-client availability question remains pending. Gameplay clients
  were not launched or controlled. Full two-client seating, cargo transfers,
  reconnect and observer presentation acceptance still open.


### Driver character identity and teleport cleanup
- Previous turn classified progress: runtime-setting fix and actual native hull
  query/removal checks. Full two-client acceptance remains pending.
- Client death/teleport cleanup now runs before driver telemetry publication;
  native teleport retains its current actor position while restoring controls,
  instead of moving the actor back to a previous vehicle door.
- Server ties each motion sample/final release pose to the authenticated claimed
  character ZDO, not only a peer UID. Maintenance revokes a seat when that peer
  replaces/loses the character, preventing old grants surviving respawn.
-131 serialized integration checks include old-character motion rejection,
  respawn lease release/fresh claim and authorized malformed-packet handling.
  Client/server builds,146 controls,40 safety and package validation passed.
- Server ZIP41508bytes SHA256
  b15f98abd7e9e6fc0c9bafa44d58f549bd3a6518d6e17e43a0dcc0da95640859.
  Logs artifacts/network-character-*.log. No gameplay client install/launch/input.
  This revision was compiled and mock-transport checked; prior native hull/save
  evidence does not prove real-client death/teleport or this exact DLL at runtime.


### Manual client variant activation prepared
- Previous turn classified progress: character-identity lease/teleport fixes.
- Added read-only-by-default tools/activate_client.py network|singleplayer.
  --apply requires a stopped Valheim, validates variant/SHA stamp and installed
  cars.json/body.json, backs up the DLL, copies/verifies, rolls back on failure
  and retains newest2 dated backups. It changes no assets/config/world data.
- Both builds now write build-variant.json after full successful asset build.
  Compile-only output has no matching valid stamp and cannot be activated.
- Both variants built. Read-only validation passed against the current installed
  core assets. Client installed DLL was not replaced and game not launched.
- Network DLL SHA2566b4b6797565e4edc35170d41d0c652f8852ddb3529dc01e7f0a094005757824f;
  singleplayer DLL2026ce192d4673962c2ad5aa19c0bb5d2cd3203eb49582fe9ef5e3f1232f6b00.
- Stopped isolated server lab updated to current server payload with verified
  copy/backup and pruning. No native process launched. Fixture remains present;
  move test fixture out of plugins before a manual long-lived server session.
- Manual activation/restore commands documented in docs/NETWORK_TESTING.md.
  Second-client availability remains pending; real two-client acceptance open.


### Laptop LAN test kit prepared
- User clarified a laptop is available; installed game/mod status still pending.
  Previous turn classified progress (safe variant activation tooling/builds).
- Added tools/prepare_lan_test.py: verifies build stamps/server manifest, stages
  server files with backups/SHA and newest2 retention, disables the disposable
  test watchdog outside plugin discovery, writes private manual launcher/password
  details, and packages the small client DLL patch. Starts/installs nothing into
  the gameplay client and changes no firewall rules.
- Manual server uses separate nfvu2_lan world/save directory, port2476/public0;
  native smoke world retained. Original server installation remains unchanged.
- Patch ZIP exact three-file allowlist and enclosed DLL hash verified. ZIP SHA256
  441656affa15ce5ff9874c63c80143c4437d7ed34472fd80163bcc14b91fc67f.
- LAN launcher .local/server-lab/start_lan_test.cmd; private details beside it in
  LAN_CONNECTION.txt. Full client asset folder is still required on laptop.
- CIM-based adapter query was blocked in sandbox; no IP address inferred from it.
  Client/server traffic and all user-controlled two-client acceptance still open.


### Manual acceptance blocker audit
- Previous continuation was progress: LAN kit, matching client patch and manual
  launcher prepared without changing the installed gameplay client.
- Current authoritative check: installed DLL is not the network build; no
  Valheim/valheim_server process in native process snapshot; no lan-test.log.
  No live process exists to wait on. Client patch and prepared launcher exist.
- Same external acceptance condition has remained open across the driver-identity,
  variant-activation and LAN-kit continuations: real two-client play under user's
  control. Laptop availability clarified the setup, but no manual run results yet.
- All pending real-client requirements remain unproven (motion/avatar/authority,
  native cargo UI, reconnect and player restore). Single-player visual acceptance
  including neon/HUD/geometry/menu also remains open. Builds, offline renders,
  native server-only fixture and managed checks cannot substitute for these gates.
- No additional observed code failure currently determines another independent
  implementation change. Safe preparation is complete; speculative changes or
  repeating tests would not resolve this requirement. Existing user restriction
  on launching/controlling gameplay clients still applies.
- Mark goal blocked pending manual LAN test/feedback or renewed authorization
  defining allowed gameplay-client testing. Keep the full objective unchanged;
  this is not completion and does not discard remaining single-player scope.


### Inventory garage inactive on LAN: actual installed variant corrected
- User reported CARS/GARAGE not clickable. Current game log confirmed it joined
  a dedicated server with the older single-player DLL (installed SHA8cb5fd4b...);
  startup explicitly said multiplayer disabled. No native click request logged.
- With both processes closed, rebuilt and activated experimental client through
  guarded activate_client.py. Installed SHA256
  3207f25fe617d2bc90709fa05527cc345b3423e4e60b874c6207f4546161f34e.
  Backup artifacts/client-variant-backups/20261017-093511-794120, checksum verified.
- Inventory button now remains clickable for a local player and gives a visible
  explanation if single-player build or missing compatible server prevents
  opening; no silent multiplayer-disabled button. SpawnBusy still disables it.
- Updated client patch ZIP and its SHA file for laptop. Same protocol3, no server
  changes. Build passed; no game/client launch or input. Actual menu opening after
  reconnect remains for user verification; do not count build as runtime success.


### Real LAN handshake failure: native identity source corrected
- User screenshot showed client waiting for compatible server after activation.
  Client/server logs show both mods loaded and character ZDO delivered, but no
  protocol connection. Native decompilation shows ZNetPeer.m_playerID is only
  populated by a separate RPC_PlayerID, whereas Player.GetPlayerID reads the
  authenticated character ZDO's ZDOVars.s_playerID field.
- Removed the peer-playerID cache requirement from Hello; a ready native peer can
  negotiate protocol before character data arrives. Stateful requests and native
  cargo identity now use the actual character ZDO player ID, as Valheim does.
  No client-supplied creator/player-ID field was added or trusted.
-132 serialized integration checks pass with ALL fake peers having zero cached
  m_playerID and real-style character-ZDO IDs. This covers handshake, spawn,
  ownership and native cargo authorization through the corrected source.
- Server rebuilt against dedicated assemblies and asset-free package checked.
  ZIP SHA25650c862af3229c2903bb5871c91d7bfd5628c9d35ffb3543bce00ff35dbd25874.
- Both processes were stopped. Updated isolated LAN server via guarded preparation
  with backup/SHA verification; no process launch, client DLL/config/world change.
  Client remains protocol3 and does not need replacement for this server fix.
- Real reconnect/garage opening is still pending user check; no acceptance claim.

- CORRECTION to staging claim above: sandbox process enumeration missed external
  client/server processes. Copy failed with Windows1224(mapped DLL) before replacement.
  Elevated read confirmed live clientPID2108/serverPID13136. No process stopped.
  Updated payload is BUILT but not yet installed in running LAN server.
- Added prepare_lan_test.py --stage-only and ignored restart_with_server_fix.cmd.
  User stops server via Ctrl+C, then invokes wrapper outside sandbox; real host
  process guard rejects any still-running server, stages backup/verified payload
  and calls existing manual launcher. No live launcher was edited or executed.
- Regression fixtures now start peers with m_playerID0 before Hello, matching the
  observed native field lifecycle.132 integration checks pass. Protocol remains3;
  current client needs only reconnect after server restart, no DLL replacement.

### Dedicated spawn rejection on valid ground
- Native IL confirms ZoneSystem.Update uses CreateGhostZones around peer reference positions; IsAreaReady requires locally loaded zones/instances. Dedicated collision checks therefore rejected valid client-approved spawn pads.
- Added explicit dedicated compile variant for placement: preserve upright/identity/distance/ownership/limit validation, delegate terrain/clearance to the existing client SpawnSupport checks. Listen-host physics stays unchanged. Same correction applies to recovery.
- Dedicated package rebuilt against installed native server assemblies; archive verification and 132 serialized integration checks passed. No gameplay acceptance claimed. Prepared package only; running server not replaced. Existing restart_with_server_fix.cmd stages the latest verified payload after the user stops the server.
