# NFVU2 — Need for Valheim: Underground 2

![NFVU2 logo](docs/images/nfvu2-logo.png)

Drive, tune, customize and fly NFS vehicles through Valheim. This development
fork expands the original 350Z mod into a 76-vehicle U2/MW garage, with original
vehicle assets, themed menus, radio, persistent world vehicles and experimental
multiplayer.

Based on [Backwoodz/NFVU2](https://github.com/Backwoodz/NFVU2). The original
project and its published releases are separate from this fork's current source.

*Is it random? — Yes. Is it useful? — Not really. Did I make it anyway? — Kinda. Codex did all of it.* © @Backwoodz

## Current status

The features below describe this working source tree, not the upstream installer.
Single-player and experimental multiplayer use separate client builds. The
multiplayer prototype is undergoing real two-player testing and has known
usability and synchronization problems; it is not a finished multiplayer release.

Vehicles now have persistent world records, including their saved build, pose
and native cargo inventory. They are no longer intentionally temporary objects
that disappear permanently when a world closes. Models load and unload with
nearby world areas; unloading a visual is separate from deleting its saved car.

## Vehicles and garage

- 30 Underground 2 vehicles and 46 Racing entries from Most Wanted (2005), with
  separate U2/MW selection and menu themes.
- Per-model settings, a four-car ownership limit, saved garage rosters and three
  named build slots per vehicle model.
- Stage 0 preserves baseline handling; Stage 1–3 upgrades cover engine, turbo,
  transmission, tyres, brakes and NOS. Driving remains an NFVU2 adaptation,
  rather than a recreation of either NFS physics engine or career.
- Original vehicle parts where available: bodykits, bumpers, skirts, hoods,
  spoilers, roof scoops, brakes, rims, vinyls and decals. Wheel design, diameter,
  colour and fitment share one customization flow; MW rim diameter is separate
  from rim design.
- Body paint uses 14 colour families with seven shades each (98 colour/shade
  choices plus Original), preserving existing saved base-colour IDs. These are
  NFVU2 shade adaptations, not an extracted original NFS paint table. Builds with
  the optional verified car-shader bundle also offer Pearl and Chameleon with
  a separate shift colour/shade. These view-dependent Unity adaptations have
  passed editor rendering checks; vehicle appearance remains under testing.
- Paint, carbon finishes, window tint, neon, exhaust and nitro effects, plus
  capability-dependent door/trunk controls and garage effect previews. U2 neon
  uses authored bumper/skirt/bodykit endpoint markers from updated exports;
  older data and unsupported models retain a geometry-fitted fallback. U2 NOS
  purge nozzles also use original BASE marker positions and orientations.
- Buildable **Hammer → Crafting → NFS Garage** service bay. Current prototype
  prices are 10 Wood for the station and 1 Coin for a car or a new upgrade level.
- Recovery/removal actions for parked world vehicles, with player restoration
  before removing a vehicle while seated.

The Most Wanted importer reads your own PC installation, including native parts,
vinyl/decal data, wheel variants, chassis metadata and acceleration/coast engine
banks. The archived BMW M3 has an explicitly recorded GTR data donor. Original
Underground 2 and Most Wanted game files are not included in this repository.

## Driving, presentation and audio

- Ground driving modes, handbrake, nitro and flight, with the actual player
  avatar seated in the car. Turret and horde features depend on vehicle support.
- Native cargo storage, vehicle map markers, controller support and English /
  German interface options.
- Original U2/MW gauge artwork, selectable HUD styles and independently placed
  gauges. Driving help is available in the menu instead of a permanent large
  overlay. Function keys are no longer default mod shortcuts.
- Separate U2/MW radio stations, EA TRAX track exclusions, shuffled playback,
  vehicle engine audio, and original menu artwork/sounds.
- Optional road dust, grime and wet paint. Garage/showroom scenes have distinct
  U2/MW presentation, with preview rotation and zoom.

## Install and open the garage

Windows 10/11, 64-bit, with Valheim and BepInEx. Close the relevant game or server
before replacing its plugin. Keep a backup and use only one NFVU2 DLL variant
per instance; the plugin filename remains **NeedForValheim.dll**.

There is no current fork installer linked here. The
[upstream releases](https://github.com/Backwoodz/NFVU2/releases) install the
original project's published feature set, not this development tree.
For a source installation, follow [Contributing](CONTRIBUTING.md) to configure
your local game paths, extract/export assets and build the complete client.
Compiling a DLL alone does not supply the models, textures or music it needs.

In the current development client:

1. Enter a world and open **Inventory → CARS / GARAGE**. A built garage station
   also provides access to the garage workflow.
2. Select a vehicle and use the available garage action to spawn or recall it.
3. Use **E** at a car to enter, **Shift+E** to customize, and **Tab** while seated
   to open the mod menu. Controller menu access uses **L3**; **B / Circle** exits.
4. Use **My garage** to manage parked cars. **RECOVER**, where available,
   relocates a vacant owned car to a nearby clear pad while retaining its cargo
   and build. It is distinct from removing the vehicle.

Keyboard driving defaults: **W/S** throttle/brake/reverse, **A/D** steering,
**Space** handbrake, **LMB / Ctrl** nitro, **H** horn, **←/→** radio tracks and
**1–4** driving modes. See the menu's **Options → Controls** for contextual
controls, including flight and controller input. No F-key shortcut is required.

See the [garage guide](docs/GARAGE.md), [garage station](docs/GARAGE_STATION.md),
[driver guide](docs/DRIVER.md), [TRAX guide](docs/TRAX.md) and
[MW importer](docs/MW_IMPORT.md) for subsystem details. Some older guide text may
still describe earlier UI layouts; the current source and this status take precedence.

## Experimental multiplayer and dedicated server

Multiplayer requires the experimental client on every player and a compatible
NFVU2 server plugin. The normal client build keeps multiplayer disabled.
Matching protocol version **3** is currently required. A waiting-for-server
message indicates that the compatible connection has not been established;
building a garage does not resolve a missing or incompatible server plugin.

The driver runs vehicle physics. The server manages persistent records,
driver/cargo leases, ownership, setup revisions and replicated motion. Other
clients reconstruct nearby vehicle visuals and effects. This is not an
authoritative server-side wheel simulation or a complete anti-cheat system.
Dedicated servers delegate terrain/clearance checks to the spawning client;
server-side pose, proximity and ownership checks remain in place.

The separate headless package contains the plugin and vehicle metadata, with
**no client models, textures, music or frontend assets**. Players still need the
full client asset installation. Never install the headless DLL on a gameplay client.
The small client patch ZIP is likewise an update, not a complete installation.

Native server startup, world save/reload with vehicle metadata and cargo, and
basic invisible chassis creation/cleanup have been checked. Automated serialized
client/server tests also exist. These checks do not establish smooth real-world
multiplayer, correct inventory GUI behavior or complete gameplay acceptance.

Build/install boundaries and manual test steps are in
[Dedicated server](docs/SERVER.md) and [Network testing](docs/NETWORK_TESTING.md).

## Known issues and work still in progress

Recent player testing has reported the following unresolved problems:

- Main server tuning (paint, wheels, bodykits and performance), Pearl/Chameleon
  and local music volume passed the player's latest check. U2 interior details
  still reverted in that build. The current source reapplies observer showroom
  settings, resizes unloaded server cargo, and retains drafts for revision-conflict
  or transport retries. Invalid serialized cargo gets an explicit rejection with
  its original bytes retained. Freshly spawned cargo now uses the same rear anchor
  as driven/recovered cargo. Gameplay acceptance of these follow-ups is open.
- Remote cars visibly unload/reload when leaving and re-entering view, with
  inappropriate spawn notifications. Motion and engine pitch changes can jerk;
  the latest observer buffer adapts to packet spacing and reconciles late
  extrapolation errors. Gap/stop/turn replays pass, but real two-client streaming,
  smoothness and engine-pitch acceptance remain open.
- The current source allows nearby removal of a vacant, stationary car whose
  owner is offline, preserving its native cargo chest. The server rejects active
  owners, drivers, open cargo and uncertain joining identities. This policy still
  needs real reconnect and obstruction testing.
- Neon mounting, H2 inner tyre sidewalls and U2 NOS alignment still need visual
  acceptance. Pending fixes include completed H2 sidewall geometry and a uniform
  NOS circle fit. The MW arch investigation found BODY_PLAINNOTHING incorrectly
  registered for paint/vinyl on Cobalt SS, GTO and Mustang GT; old exported models
  are corrected on loading. This has passed fleet data checks, not gameplay.
- Radio / EA TRAX now includes **Autoplay on spawn / entry**. Disabled autoplay
  starts the session paused; manual Play remains available. Local radio and
  engine-volume preferences are separated from network vehicle ownership in
  the current source build. Borrowed-car checks remain pending. A newer observer
  fix restores spatial radio mixing after exit; actual distance falloff needs
  another gameplay check. New observer radios start spatial immediately. Parked
  engine/radio switching now updates the server running flag, including release
  without a final motion sample; unrelated styling changes do not restart it.
- Styling uses wider body-part choices and separate body-detail pages for
  panels, hinges, engine and glass. Trunk audio has layout, equipment and lighting
  pages. The whole garage now uses top-level categories above a dedicated car
  preview and a consistent right settings panel. Gauge selection uses illustrated
  cards and a separate appearance page. The preview hides scenery between the
  camera and car during inspection. These layout and camera changes still need
  in-game visual acceptance; they do not establish server interior synchronization.
  A subsequent frame-context fix preserves parked-car selection/removal; catalog
  paging keeps the preview and purchase target visible, and directional menu
  navigation follows grid rows and columns. Native workflow acceptance is open.

Additional vehicle, HUD, garage and network acceptance remains open. Imported
parts and animations vary by vehicle; not every source car provides every menu
capability. Native NFS shaders are not currently reproduced as a complete Unity
vehicle shader system.

The detailed overhaul scope and validation history are tracked in
[Overhaul status](docs/OVERHAUL_2026_10.md). Player reports take precedence over
an earlier build or test passing; managed tests do not execute Unity gameplay.
The current requirement-by-requirement evidence and open native acceptance gates
are listed in [Quality acceptance](docs/QUALITY_ACCEPTANCE.md). A full automated
run has passed; three Python checks and donor-cabin asset checks remain explicitly
unavailable where their optional input/tool is absent.

## Development

Use an isolated Python environment and keep paths in ignored `config.local.json`:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt

# After configuring paths and preparing the required local assets:
.\.venv\Scripts\python.exe tools/build.py
.\.venv\Scripts\python.exe tools/test.py --skip-build

# Separate experimental network client and asset-free server outputs:
.\.venv\Scripts\python.exe tools/build.py --experimental-network
.\.venv\Scripts\python.exe tools/build_server.py --game-dir "<dedicated-server-folder>"
.\.venv\Scripts\python.exe tools/test_server.py --skip-build
```

Outputs are `artifacts/mod/`, `artifacts/network-client/` and
`artifacts/NFVU2-server.zip`. Building does not install or launch the game.
`tools/install.py` handles the normal client installation;
`tools/activate_client.py` switches a prepared client DLL variant with backups
and checksum verification. Follow the linked development/network instructions
before using either. Python environment activation is optional when invoking
its interpreter directly.

Keep original game files, extracted assets, downloaded tools and generated
binaries in ignored `.local/` or `artifacts/`. Generalizing the tools does not
remove real vehicle identities: `350z` and `h2` remain catalog IDs, while shared
export/render behavior uses generic vehicle tooling.

## Media and credits

![350Z in Valheim](docs/images/350z-ingame-driver.png)

![350Z with neon lighting](docs/images/350z-ingame-preview.gif)

![350Z hovering with thruster effects](docs/images/350z-ingame-flight-preview.gif)

These are existing 350Z examples, not a visual acceptance record for every car
or the current multiplayer build. Blender model previews and in-game lighting
can differ.

Original mod: Backwoodz. This fork builds on that work with AI-assisted
implementation. NFS and Valheim names and assets belong to their respective
owners. Experimental fan project; no official affiliation.

[Changelog](CHANGELOG.md) · [Contributing](CONTRIBUTING.md) · [Roadmap](docs/ROADMAP.md)
