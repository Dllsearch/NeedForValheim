# NFVU2 quality pass

This replaces the older execution goal. The October overhaul remains historical
evidence and a regression reference, not the current completion checklist.

## Current implementation evidence — 2026-10-18

- Full scope/evidence audit: [QUALITY_ACCEPTANCE.md](QUALITY_ACCEPTANCE.md) maps
  every requirement to current checks and the remaining native acceptance gate.
  The complete test runner passed: 114 Python tests (three explicit skips), then
  all 47 C# suites. Optional donor-cabin and UnityPy/Rust-import coverage remains absent.
  Build stamps, ZIP payloads, shader bundle and HUD/catalog exports were independently
  verified. Logs/reports: `artifacts/quality-full-validation.log` and
  `artifacts/quality-artifact-audit.json`. No gameplay completion claim follows.
- Remote-motion follow-up: interpolation headroom adapts within 120–350 ms to
  observed packet intervals/jitter. A bounded playback clock stops advancing after
  100 ms extrapolation and resumes gradually; late extrapolated position/rotation
  errors reconcile over 150 ms. Reset distance now accounts for elapsed travel;
  epoch/driver resets still clear history. Deterministic gap/stop/turn replays pass,
  including real quaternion math in the test double, not native rendering.
  Evidence: `artifacts/remote-gap-before.log`, `artifacts/remote-stop-before.log`
  and `artifacts/remote-gap-tests.log`. Real two-client acceptance remains open.
- Parked audio follow-up: changing KeepRunningAfterExit on a vacant, previously
  driven server vehicle now updates the replicated running flag. Unrelated paint
  edits do not restart a stopped car; occupied/never-driven cars are protected.
  Release without a valid final motion sample also enforces the saved off preference.
  Both failures were reproduced before fixing them; live audio acceptance is open.
- Radio observers now expose playback controls for a remote active driver, use
  authoritative parked state, and update subwoofer motion. A newly streamed outside
  radio starts fully spatial; actual entry/exit retains its crossfade. These paths
  need native audibility/distance and two-client checks, not just scalar-state tests.
- Garage selection follow-up: the production frontend frame now restores the
  world vehicle only when it actually borrowed a preview. Previously its finally
  block undid parked-car selection or resurrected a removed selection. The new
  `frontend` suite reproduced the failure before the fix and exercises both
  themes with page/drawing doubles; native GUI acceptance is still pending.
- Car-lot paging selects/previews an entry on the new page, and selecting a model
  aligns its catalog page. Keyboard/controller Up/Down now follow visual grid rows
  instead of advancing sideways through a flat control index. Targeted automated
  coverage does not establish actual controller hardware or image layout quality.
- Cargo anchor follow-up: spawn, accepted motion and recovery now share rear
  anchor positioning and immediately align a loaded native chest. Previously a
  fresh vehicle used its centre until first motion/recovery. A regression failed
  before the fix; stock 350Z and the longest MW chassis now pass serialized rear
  access checks while retaining the original saved car pose. Native scene/UI
  acceptance is pending. Evidence: `artifacts/cargo-anchor-before.log` and
  `artifacts/cargo-anchor-tests.log`.
- Setup conflict/transport retry now keeps the submitted draft against the last
  acknowledged baseline. Successful confirmation clears matching pending changes;
  hard validation failures still discard the refused submission, preserving edits
  made afterwards. This is not yet live client/server acceptance.
- A serialized regression reproduced missing Setup replies for truncated unloaded
  cargo. Resizing now rejects parse/serialization failures explicitly before
  replacing inventory bytes or setup revisions. Interior-only field updates are
  covered without altering cargo bytes. Native inventory/UI acceptance remains open.
- Detailed evidence: `MODLOG.md` and ignored `artifacts/interior-sync-retry-tests.log`.
- Both client variants and the headless server compiled. Setup retry checks: 254;
  subsequent cargo-anchor checks: 190 integration plus 53 storage. Frontend
  checks: 12 context, 152 controls and 24 menu assets. Parked-audio follow-up:
  52 radio, 24 showroom/bass and 230 serialized integration checks. Remote-motion
  follow-up: 20 replay and 91 compiled audio checks. Packages remain
  uninstalled:
  - `artifacts/NFVU2-network-U2-update.zip` (62 payload files), SHA-256
    `9b2b2a990ea1515fa09b93c6eccd9bbaeec0e6e9f680c982b7f2fc8f66eca245`.
  - `artifacts/NFVU2-server.zip` (three payload files), SHA-256
    `d24694d6ee2d108af6f1a6ada839bb9382ff459b8ea4aafbfe9b3cc049939156`.
  Archived payload hashes were verified against their manifests/source output.

## 1. Reliable multiplayer customization and cargo

- Resolve U2 interior detailing rollback on the server: trunk layout, audio
  equipment, engine appearance, tint and hinge settings. Distinguish persisted
  settings from transient panel-opening state. Test edits, acknowledgement,
  reopening the menu, streaming and reconnecting.
- Validate the prepared unloaded-cargo resize and observer-configuration fixes
  in the actual client/server path. Do not equate a UI reorganization or animation
  capability guard with a synchronization fix.
- Make cargo access understandable and protect contents during capacity changes,
  handover, disconnect, recovery and vehicle removal.
- Preserve user-confirmed paint, rims, bodykit, performance and finish persistence.

## 2. A cohesive garage interface

- Refine the entire garage workflow: selection and ownership, performance,
  exterior styling, wheels, detailing, effects, saved builds, radio and options.
- Evaluate the new shared layout as an implementation candidate, not an accepted
  design. Make hierarchy, spacing, selected state, labels and action placement
  coherent; retain a useful car viewport and unobstructed inspection camera.
- Resolve the crowded tachometer picker with readable U2/MW choices and accurate
  previews. Keep this separate from the U2 interior synchronization defect.
- Support mouse, keyboard and controller with consistent focus/back behavior and
  readable layouts across window sizes/aspect ratios. Prevent clipping, overlap,
  duplicate destinations and camera input capture over controls.
- Make local versus server-owned preferences, pending saves, rejection messages
  and unavailable capabilities clear. Maintain distinct U2/MW presentation.

## 3. Vehicle, HUD and sound fidelity

- Validate pending geometry fixes: H2 inner tyre sidewalls and preview grounding;
  3000GT/RX-7 underbody assembly; U2 widebody track, especially Miata and Supra.
- Validate the MW black-cavity material fix on Cobalt SS plus GTO and Mustang GT,
  including different kits. Preserve exterior paint, vinyl, chrome, glass and
  carbon. Investigate any remaining material mismatch from authored roles.
- Fit neon tubes/glow consistently in world, garage, flight and remote views;
  preserve exhaust/nitro previews without spending gameplay resources.
- Finish common U2 NOS alignment using the shared circle, with 07/08/09/11 as
  visual references. Retain accepted MW fill behavior and polish residual text,
  colour and alignment defects.
- Validate radio distance falloff after server exit, borrowed-car volume/autoplay,
  and remote continuous engine pitch. Preserve the accepted paint palette and
  Pearl/Chameleon behavior while refining materials and lighting.

## 4. World lifecycle and multiplayer quality

- Validate quiet distance streaming without duplicate vehicles or spawn notices;
  improve remote body/wheel motion and RPM smoothing under real packet timing.
- Verify world save/reload, joining, reconnecting, driver handover and cargo leases
  with two clients when available. A solo server test cannot close co-op cases.
- Verify safe exit on rocks/overturned cars and seated Unspawn restoration of the
  real player/camera/control. Validate offline-owner obstruction removal while
  protecting active owners, occupied vehicles and retained cargo.
- Preserve the asset-free headless server and matching client/server protocol.

## Completion and operating boundaries

- Separate implemented, automated-check-passed and gameplay-accepted states.
  Reuse earlier acceptance for unchanged behavior; do not reopen confirmed work
  without regression evidence. Keep unresolved cases explicit.
- Investigate shared causes across representative cars rather than patching one
  photographed example. Preserve existing handling, assets, saves and settings.
- Run targeted checks, prepare checksum-verified client/server artifacts, and
  maintain English README/status documentation. No publication is implied.
- The user controls Valheim and manual testing. Do not operate game/server
  processes or replace running installations. Prepare useful independent work
  without repeatedly requesting unavailable manual checks.
- Close the goal only after important remaining defects and gameplay acceptance
  gates are resolved; builds and API doubles alone do not establish completion.
