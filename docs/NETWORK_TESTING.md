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
