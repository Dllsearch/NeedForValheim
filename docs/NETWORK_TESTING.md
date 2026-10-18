# Native multiplayer acceptance

## Evidence already obtained

The isolated dedicated-server lab loads BepInEx and the server plugin, connects
with Steam, generates a new world and exits normally. A separate native fixture
created a persistent 350Z cargo anchor through VehiclePersistence.Save, added
three Wood with the real Container/Inventory API, and let Valheim save the world.
After restarting the same test world, the fixture found exactly one vehicle,
verified position (25,26,27), paint snapshot, charge0.42 and the three Wood.
Native ZDO user-ID prefixes can change during world reload; persistent creator,
logical record uniqueness and saved contents were checked instead of assuming
that a live session ID string remains unchanged.

Evidence logs are ignored artifacts/native-vehicle-create.log and
artifacts/native-vehicle-reload.log. The fixture source is
tests/servernative/ServerPersistenceSmoke.cs. It is a test-only BepInEx plugin:
it creates data only in the disposable lab world and requests automatic shutdown.
Never install this fixture on a real server or alongside a gameplay client.

The native fixture also creates a temporary anchor, invokes the production server
hull manager, checks the chassis has a kinematic body and no renderers, verifies
its BoxCollider answers a native physics ray, then deletes the anchor and verifies
hull cleanup. Evidence: artifacts/native-vehicle-hull.log. This does not prove
vehicle-to-player or vehicle-to-vehicle behavior during networked motion.

These checks do not exercise actual client RPCs, player control or inventory UI.

## Switching the installed client for a manual test

Both build commands write a build-variant.json checksum stamp. A compile-only
build cannot pass variant activation. The activation helper compares installed
cars.json/body.json against the selected source before touching the DLL.

```powershell
# Read-only validation; works without installing or starting the game.
.venv/Scripts/python.exe tools/activate_client.py network

# Explicit activation with Valheim closed, backup and SHA-256 verification.
.venv/Scripts/python.exe tools/activate_client.py network --apply

# Return to the normal single-player variant, also while closed.
.venv/Scripts/python.exe tools/activate_client.py singleplayer --apply
```

Only NeedForValheim.dll is replaced. Existing assets, settings, worlds and other
plugins are preserved. The helper restores the previous DLL if copying/verifying
fails and keeps the newest two dated client-variant backups. It never launches,
focuses or stops the game. The experimental variant remains unaccepted until the
actual two-client cases below pass.

## Manual LAN kit (prepared locally)

User confirmed a laptop is available. tools/prepare_lan_test.py prepares a private
manual launcher in .local/server-lab/start_lan_test.cmd, moves the test watchdog
out of plugin discovery and selects a separate nfvu2_lan world in lan-worlds.
It starts no process, changes no firewall rules and activates no gameplay DLL.
Connection password and port are in ignored LAN_CONNECTION.txt beside the launcher.
Stop the manual server with Ctrl+C and wait for native save/shutdown.

artifacts/NFVU2-network-client-patch.zip contains only the protocol3 client DLL,
manifest/checksum requirements and README. It is not a complete client install.
The laptop needs its own Valheim/BepInEx plus the full NeedForValheim asset folder;
then apply the matching network DLL to both clients while closed. Keep original
DLL backups, avoid duplicate plugin copies and do not use the asset-free server
DLL on a gameplay client. No game or BepInEx binaries are in the patch ZIP.

## Current follow-up patch

Refresh only the small client patch, without stopping/touching a live server:

```powershell
.venv/Scripts/python.exe tools/prepare_lan_test.py --package-only
```

For the original neon/purge mounts, include the regenerated U2 data as well:

```powershell
.venv/Scripts/python.exe tools/prepare_lan_test.py --package-only --include-u2-assets
```

This produces artifacts/NFVU2-network-U2-update.zip with the network DLL, all
30 U2 model JSON files, their 30 styling catalogs and cars.json. It omits music,
MW assets, settings and worlds, so it requires an existing full client install.
Before extracting, close Valheim and back up every installed path listed in the
manifest; both clients need the same update. Each archived payload file is
SHA-256 checked before the completed archive replaces any previous update.

The DLL-only command above writes artifacts/NFVU2-network-client-patch.zip and its SHA-256 file after
validating the full network build stamp. It does not stage server files, edit
launchers, touch installed clients, or start/stop anything. Both players need
the matching client update and the server needs its separately built payload;
replace each only after its process has closed, preserving backups.

Prioritize these manual regressions after updating:

- Own parked car: open styling, change multiple values quickly, wait, reopen,
  then reconnect and compare. Report the exact edit-denial text if blocked.
- Change local music/engine volume while driving a borrowed car. Disable
  autoplay, spawn/enter, use manual Play, exit/re-enter; playback should wait.
- Race together across zone boundaries and out past 180 m/back. Check silent
  visual streaming, motion/turning, wheels and continuous engine pitch.
  The new arrival-time buffer adapts between 120 and 350 ms and reconciles late
  extrapolation errors over 150 ms. Inspect both sparse updates and a delayed
  stop/turn; a stopped car must settle exactly rather than keep drifting. Test
  high speed across a packet gap and explicit recovery separately: only recovery
  should reset immediately. Replay tests do not replace this live timing check.
- Owner disconnects: nearby other player removes the vacant obstruction; cargo
  remains a native chest. Reconnected owners and occupied/open-cargo cars reject
  foreign removal.
- Inspect body finish, inside wheel arches and neon in garage/ground/flight.

## U2 interior synchronization follow-up (pending gameplay acceptance)

The current builds retain setup drafts through revision conflicts and transport
uncertainty. Hard authority/capacity refusals still reject that submission. The
server explicitly refuses malformed unloaded inventory data without replacing
its bytes. Automated coverage is recorded in `artifacts/interior-sync-retry-tests.log`;
it does not exercise native inventory UI or the Plugin's live update loop.

When manual testing is available:

- On a freshly spawned, undriven car, approach from behind and open cargo. Repeat
  after driving, streaming and recovery. Check a stock 350Z and a long MW vehicle;
  cargo access should use the same rear point throughout, without shifting the car.
- On an owned parked U2 car, change layout, several audio sockets, engine
  appearance, hinge style and tint. Wait for server acknowledgement, reopen the
  menu, then reconnect and compare the persisted values and visible equipment.
- Change more values while a save is pending. Later edits must survive the first
  acknowledgement; retry notices must clear after a successful save.
- Reject a capacity reduction with too much cargo. Contents must remain intact,
  with an explicit refusal rather than an endless save indicator.
- Opening/closing panels is transient presentation, separate from saved hinge
  style and equipment. It is not evidence that the interior snapshot persisted.

The user controls all game/server interaction; these are deferred acceptance
cases, not a request to interrupt rest or operate running installations.

## Garage selection workflow (pending gameplay acceptance)

Also verify parked audio after updating: exit a driven car, switch Keep running
after exit off/on nearby, then change paint while the engine is stopped. Only the
running switch should start/stop parked playback. While another player drives,
local radio Play/Pause and volume should remain usable. Re-enter streamed range
and listen for any global music burst; outside playback should start spatial and
fall off with distance. Observe configured subwoofers during network playback.

When manual testing is available, select a different parked car in My garage,
then open performance/styling and verify the selected model is still the target.
Remove the currently selected car and verify the replacement remains selected;
the removed runtime must not reappear through the UI's temporary preview context.
Browse the car lot across pages, switch U2/MW and return to a selected model.
The preview, highlighted model, title and purchase/recall action should agree.
Check keyboard/controller Up/Down across wheel, gauge and paint grids. These
cases require native interaction; drawing/page doubles only verify frame logic.

## Two-client procedure (pending)

Keep the current single-player installation backed up. Build the experimental
client with tools/build.py --experimental-network; its output is separate at
artifacts/network-client. Build the server with tools/build_server.py --game-dir
<installed-dedicated-server-directory>. Use matching protocol v3 on both clients
and server. Do not copy files while their process is running. The dedicated
payload needs no client models/audio; clients need the normal full asset tree.
Use an isolated world and separate save directory. Remove the native test fixture
before doing manual multiplayer acceptance, because its watchdog exits the server.

1. A spawns a car. B observes one car with the same paint/wheels; both see its
   motion, steering/suspension, lights, horn, engine, nitro and driver avatar.
   Simultaneous entry grants one driver; B cannot delete/recover A's occupied car.
2. Switch Road/Off-road/Drift, exit and let the other player drive. Repeat during
   a safe flight/landing transition. Verify fuel does not refill and the body
   does not drop, teleport or restart takeoff during handover.
3. A opens cargo, transfers distinguishable item stacks and closes it. B cannot
   drive/recover/remove while cargo is open. Reopen after handoff and compare
   item counts. Repeat disconnect while open/closing; inspect for loss/duplication.
4. Die/respawn or trigger a native teleport while seated; verify character control
   returns and the old character cannot keep publishing vehicle motion. Then
   disconnect driver during motion, reconnect with the same character, unload
   and reload the zone, then restart the server. Check one logical car, persistent
   creator rights, configuration and cargo. A new peer UID must not strand a lease.
5. Leave an airborne/overturned car. From My garage use RECOVER as owner. Check
   supported nearby placement, same fuel/build/cargo, no old duplicate, and a
   usable seat. Reject foreign recovery and obstructed/unsupported target ground.
6. Use seated UNSPAWN with a blocked door; verify the actual character and camera
   return, and cargo survives. Close/rejoin both clients and verify no ghost car.

Record the actual client/server builds and screenshots/logs for each case.
A passing native bootstrap or managed test suite cannot substitute for this list.
