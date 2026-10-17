# Dedicated server development payload

Build the experimental client with
`.venv/Scripts/python.exe tools/build.py --experimental-network` (output:
`artifacts/network-client/`). Normal builds keep multiplayer disabled.

Build the server with `.venv/Scripts/python.exe tools/build_server.py` and verify with
`.venv/Scripts/python.exe tools/test_server.py --skip-build`.
Outputs are `artifacts/NFVU2-server.zip` and its SHA-256 file.

This is a development multiplayer prototype. Matching protocol version 3 clients
are required. An isolated native dedicated-server smoke test loaded the server plugin and all76
profiles, connected to Steam, generated a new disposable world and shut down with
native world files written and exit code0. This does not prove two-client traffic
or seating/physics/inventory behavior; that acceptance has not been completed. The manifest keeps
`networkReady: false`; do not treat compilation or managed tests as a release gate.

The server DLL retains the client plugin ID and filename, with a separate entry
point/source list. Install one variant per instance while stopped. The server
package includes no client models, textures, audio, frontend data or original
game binaries. It retains all 76 chassis/capability/gear profiles, persistent
identity, driver/cargo leases and versioned native routed-RPC handlers. The current
compressed payload is under 64 KiB, verified against a 1 MiB unpacked ceiling.

Motion physics run on the granted driver's client. The server validates sender,
sequence, movement/presentation ranges and ownership, retains vehicle ZDOs and
replicates state for observers. It is not a server wheel/terrain simulation or
anti-cheat implementation. Native inventories temporarily transfer ownership
through Valheim's trusted-client container flow. Cargo close waits for the final
native item-byte hash before reclaiming ownership; native socket/quit ordering
still requires real acceptance.

Implemented paths include spawn, claim/release, motion, setup revisions, distant
owner roster/removal and native cargo access/close. Remote wheels, flight effects
and engine presentation are connected. Remote avatar cabin posing and lightweight server chassis colliders are implemented
but unverified in Unity. Flight/landing blend, charge and ground driving mode now
survive driver handover through protocol v3. Failed entry releases the lease without
replacing the previous suspended flight state. An abandoned car retains its last
replicated pose; automatic server-side powered landing is not implemented.
The owner can use My garage > RECOVER on a vacant vehicle, including a distant
record. The client finds a nearby pad; the server independently checks five ground
samples, loaded zones, slope/liquid/lava and chassis clearance. Recovery retains
identity, native cargo, setup and fuel, and rejects occupied/open-cargo vehicles.
The same server placement checks now gate new vehicle spawning.
Remaining frontend/HUD/vehicle acceptance is still in progress.

Packaging verifies exact file allowlist, archive/file checksums, client/server
catalog parity, zero embedded resources and no audio/image/input/IMGUI/particle
assembly dependencies. The build/package commands never launch or install a server. The separate local
native smoke lab is ignored and uses a watchdog plus an isolated save directory. Preserve
existing worlds/configuration and use an isolated test world for native acceptance.

Native vehicle persistence evidence and two-client steps: [NETWORK_TESTING.md](NETWORK_TESTING.md).

Dedicated placement validation: remote player zones are generated in Ghost mode by native ZoneSystem.Update, so local scene readiness/raycast/overlap checks cannot validate their requested spawn pads. The dedicated build explicitly delegates floor and clearance checks to the requesting client, which uses the same SpawnSupport path as single-player. The server still validates pose, upright orientation, distance from the authenticated character, ownership, occupancy and garage limits. This prototype does not provide authoritative terrain/obstacle anti-cheat. Listen hosts retain local physics validation.
