# Quality acceptance audit

Scope: every requirement in [QUALITY_GOAL.md](QUALITY_GOAL.md). This is an
evidence map, not a release-readiness claim. Source/fixture checks cannot establish
native rendering, audible playback, physical controller behavior or real co-op.
The full validation log is ignored `artifacts/quality-full-validation.log`.

## Current automated audit result

The complete `tools/test.py --skip-build` run passed on the current compiled
client. Python discovery ran 114 tests with three explicit skips: two donor-cabin
geometry cases and the optional UnityPy/Rust-import class. All 47 registered
C# suites then completed. The optional donor-cabin asset checks are not claimed:
only their independent cabin state/geometry checks ran where the donor is absent.
The junction fixture required permission outside the Windows sandbox; its fake
installation directories remained under `artifacts/test-temp`. No real game/server
installation or process was changed.

Independent artifact verification passed for both build-variant DLL stamps,
client/server ZIP file sets and payload hashes, the car shader bundle, and the
current HUD/catalog exports. Report: ignored `artifacts/quality-artifact-audit.json`.
Exporter/test changes in this audit do not alter those already-built payloads.

## Runtime readiness snapshot — 2026-10-18

A read-only comparison found that the installed client matches neither current
client build, and the isolated server lab DLL differs from the prepared server.
Their existing logs therefore cannot establish acceptance of the current fixes.
The comparison is recorded in ignored `artifacts/quality-runtime-readiness.json`.
No installation or game/server process was changed. Further acceptance needs
native evidence from the prepared builds under the user's control; repeating
automated tests or interpreting older logs cannot supply it.

## Multiplayer customization and cargo

| Requirement | Current evidence | Still required |
| --- | --- | --- |
| U2 layout, equipment, engine, hinges and tint persistence | Per-instance bindings, acknowledgement/rebase handling, observer application; `networksettings` and serialized `networkintegration` cases | Native UI edits, acknowledgement, reopening, streaming and reconnect on current builds |
| Loaded/unloaded capacity changes | Native v108 decode/repack path; malformed/unknown/overfull fixtures retain original bytes and revision | Real native items, loaded/unloaded chest, rejected reduction and world reload |
| Understandable cargo access | Rear anchors agree on spawn/motion/recovery; access guidance in menu; fresh 350Z and long MW fixture | Actual rear interaction and native inventory GUI |
| Cargo across handover/disconnect/removal | Lease, close-hash, retained-record and ownership fixture checks; `storage` checks | Two-client transfers, disconnect while open/closing, no lost/duplicated stacks |
| Confirmed exterior/performance tuning | User previously accepted paint, rims, bodykits and performance; current server fixtures retain exact fields | Regression check after later UI/context changes, without treating prior acceptance as invalid |

## Garage interface

| Requirement | Current evidence | Still required |
| --- | --- | --- |
| Whole workflow: catalog, ownership, performance, visuals, detailing, effects, builds, radio, options | Shared frontend, per-page implementations and native art catalogs; frame-context regression reproduced and fixed | Walk the entire workflow in both themes; judge clarity, hierarchy and NFS presentation |
| Readable composition and useful car viewport | Separate viewport/settings region, temporary scenery occlusion, wider catalog/part entries, current-model page alignment | Native clipping/overlap review at different resolutions/aspect ratios and camera angles |
| Tachometer picker | Separate game families, illustrated cards and appearance page; importer validates 11 U2 plus optional 11 MW designs | Native card readability, selection, color and scale controls |
| Mouse/keyboard/controller navigation | Context/focus math fixtures, spatial row/column navigation, bounded preview mouse region | Native focus, back behavior, controller hardware and cursor capture |
| Local versus server settings | Listening preferences remain local; ownership checks and pending/confirmed/rejected setup states | Borrowed-car controls and understandable live rejection/retry messages |
| Capabilities and labels | Animation/geometry guards, per-source controls, English source/German table | Confirm actual panel capabilities per vehicle; inspect long labels and native-art alignment |

## Geometry, materials, HUD and audio

| Requirement | Current evidence | Still required |
| --- | --- | --- |
| H2 tyre interiors and 350Z/H2 preview ground fit | Sidewall exporter completion, fitted hub/body placement, loader/mesh checks | Inspect inner tyre faces and installed rim/kit variants in garage/world |
| 3000GT/RX-7 underbody assembly | Exporter part-axis correction and source/geometry checks | Inspect reported underbody pieces in the actual scene |
| Miata/Supra and other U2 widebody track | Source socket/track adaptation and wheel fit checks | Stock/kit transitions, clearance and manual offsets on representative cars |
| MW cavity material cause across cars/kits | Authored PLAINNOTHING role corrected in exporter and old-descriptor loader; Cobalt SS/GTO/Mustang cases | Native kit/color/vinyl inspection; preserve exterior paint, chrome, glass and carbon |
| Paint palette, Pearl/Chameleon and lighting | Shade IDs and persistence checks; verified shader bundle/editor renders; user accepted new finishes | Current-build regression and remaining lighting/material fidelity review |
| Neon mounts and glow | Authored endpoint markers plus fallback geometry; mesh and source marker checks | World/garage/flight/remote parity, continuous tubes/glow and correct mounting height |
| Exhaust/nitro preview | Preview controls and effect settings present; original anchors where available | Visible effects during configuration without consuming gameplay nitro |
| U2 NOS circle | Source-art fit and common uniform transform; 07/08/09/11 offline references; HUD math checks | Native IMGUI overlay at different HUD scales; no per-design radius hacks |
| MW NOS and text | Native meter art and fill mapping; user accepted behavior | Residual pixel alignment, labels and chosen gauge color |
| Radio, volume and autoplay | State/acoustic checks, observer ticking and initial spatial mix, server parked-running fixtures | Actual distance falloff after exit, no streamed global burst, borrowed-car playback/volume/autoplay |
| Remote engine pitch and subwoofers | Shared playback timing, compiled audio smoothing and bass state checks; observer bass update | Native audible transitions and visible network subwoofer motion |

## World lifecycle and safety

| Requirement | Current evidence | Still required |
| --- | --- | --- |
| Quiet distance streaming and uniqueness | Hysteresis/record lifecycle logic, identity and streaming fixtures | Two-client zone boundaries, no duplicate model/cargo or respawn notices |
| Remote body/wheels/RPM continuity | Adaptive buffered clock, bounded extrapolation and correction; gap/stop/turn/quaternion replays | Real timing, latency tradeoff, wheel presentation and audible RPM during a race |
| World save/reload and reconnect | Persistent native cargo anchor and metadata; storage/network fixtures | Current-build world reload and two-client join/reconnect with retained items/build |
| Driver handover and leases | Authenticated claim/state/release checks and epoch/driver reset paths | Real avatar, physics, charge, flight/landing and camera handover |
| Rock/overturned exit and seated Unspawn | Exit search and restoration code; safety/cabin-entry fixtures | Native collision, grounded placement and restored player/camera/control |
| Offline-owner obstruction removal | Online/unknown identity, occupied vehicle and cargo lease protection; retained native chest fixture | Disconnect/reconnect race and actual obstructed passage removal |
| Headless compatibility | Asset-free server payload, shared protocol 3 and serialized command checks | Matching current native client/server interoperability |

## Delivery and completion gates

- Current artifacts and SHA-256 values are recorded in QUALITY_GOAL.md. Verify
  ZIP payload manifests and build-variant DLL hashes; preserve existing installs.
- English README/status documentation must match current behavior and retain
  the distinction between implementation, fixture evidence and player acceptance.
- The optional detailed donor cabin is absent locally. Core cabin state and
  original-body geometry checks still run; donor-specific checks are explicit
  skips. No coverage of absent donor geometry is claimed.
- Installer tests use disposable fake directories. Their success does not mean
  the user's running installation was changed or the current game accepted it.
- No row requiring native gameplay is closed by a passing full automated run.
  Do not publish or mark the goal complete until the remaining scope is proved.
