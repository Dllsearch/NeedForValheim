# NFVU2 development journal

The current user goal, regression cases, acceptance gates and implementation
evidence are tracked in [the October overhaul](docs/OVERHAUL_2026_10.md).

Work uses the existing BepInEx/Harmony plugin and generic U2/MW importers.
Game assets and generated QA evidence remain ignored. The user retains game
controls; a build/test result must not be described as native gameplay acceptance.

## 2026-10-18 — full automated validation and scope audit

The first complete run exposed a stale eleven-only HUD assertion and an import
from the removed export_mw_car tool. HUD coverage now retains all original U2
checks and additionally validates the optional eleven native MW designs. The MW
folder test now targets Source.meshes, which rejects nonlocal folder components.
Temporary fixture paths and child Python UTF-8 are explicitly scoped by the runner.
The optional donor cabin is absent locally: independent state/original-body checks
still run, while donor-specific tests are explicit skips rather than false passes.

The corrected complete run passed 114 Python tests (two donor-cabin case skips and
one optional UnityPy/Rust-import class skip), followed by all 47 C# suites. The
native installer engine passed 46 checks on disposable
fake game directories; its junction fixture required running outside the sandbox.
There was no actual installation, game launch or server control. Full log:
`artifacts/quality-full-validation.log`.

Build stamps, complete archive file lists and payload hashes, the shader bundle
and current HUD/catalog source assets were independently verified in
`artifacts/quality-artifact-audit.json`. Existing client/server package hashes
remain current; no binary rebuild was needed for exporter/test-only changes.
QUALITY_ACCEPTANCE.md maps the complete goal to evidence and unresolved native
gates. Automated success does not close visual, audio, control or co-op acceptance.

## 2026-10-18 — remote packet-gap recovery

The fixed 120 ms buffer extrapolated at most 100 ms, but its hidden render time
continued advancing while the visible pose was held. A later packet could then
jump to that advanced time. A 10 m/s deterministic replay reproduced a 0.8 m
single-frame jump at 50 FPS. Adaptive headroom (120–350 ms) now follows observed
arrival spacing/jitter; the bounded playback clock freezes with exhausted
extrapolation and adjusts speed within 0.85–1.15 while rebuilding the buffer.

A delayed braking sample additionally reproduced a 0.879 m correction jump.
Late samples now reconcile the displayed extrapolation error in both position
and orientation over 150 ms. Stops converge to the exact authoritative pose;
epoch/driver resets clear delay/correction history immediately. The distance
reset threshold includes plausible elapsed travel instead of treating every
12 m packet gap as a teleport.

The position, wheel and continuous-effect sample selection shares the playback
clock. This still uses packet arrival time, not a synchronized sender timestamp.
Higher buffering trades added display latency for continuity; native two-client
acceptance is required. The expanded replay uses actual System.Numerics quaternion
operations rather than the previous no-op Slerp double. Gap, stop and sparse-update
peaks are approximately 0.2064, 0.2 and 0.2 m per 20 ms render frame in these fixtures.
The synthetic replays do not prove live Unity interpolation or audible RPM behavior.

Both client variants compiled. Twenty remote-motion replay checks and 91 compiled
audio checks passed (111 total), including transmitted RPM convergence and existing
render-rate-independent pitch smoothing. Logs use the `artifacts/remote-gap-` prefix.
The client update was repackaged with verified payload hashes; server protocol and
payload are unchanged. No game/server process or installation was touched.

## 2026-10-18 — parked audio state and streamed radio mix

Changing KeepRunningAfterExit saved the setting but left the observer's replicated
running flag unchanged. The server now updates it when that setting changes on a
vacant car. It does not start never-driven vehicles, stop occupied ones, or restart
a stopped car for an unrelated paint edit. A second regression reproduced engine
state staying on after Release without a final motion sample despite the saved off
preference. That fallback now respects the preference without starting a stopped
engine; an accepted final sample retains its explicit state.

Observer UI reads authoritative parked state, and radio controls remain available
when another player is driving. Observers also update existing subwoofer visuals
after radio analysis. Previously only the local simulation branch did so.

New outside radios started their spatial blend at zero, leaving a short global-2D
mix window if a track loaded quickly. RadioCabinBlend initializes to the first
listening context, then crossfades genuine entry/exit. Existing volume, filtering,
playlist, autoplay and distance-rolloff settings are retained.

Serialized regressions cover 350Z and a prefixed MW profile; scalar checks cover
startup, exit/entry and invalid frame times. Evidence uses `artifacts/parked-audio-`
and `artifacts/parked-release-before.log`. Native attenuation, streaming audibility,
borrowed-car UI and subwoofer rendering still require gameplay acceptance.

Final targeted checks passed: 52 radio state, 24 showroom/bass and 230 serialized
integration checks (306 total). Both client variants and the asset-free server
compiled. Packages are checksum-verified; no process or installation was changed.

## 2026-10-18 — selected vehicle lifetime and spatial menu navigation

DrawGarageFrontend restored its captured current vehicle unconditionally. That is
needed only for borrowed styling/detail previews; on ordinary garage pages it
undid parked-car selection and could restore a disposed selection after removal.
Restoration is now conditional on actually borrowing the preview. A new frontend
suite compiles the production frame code with drawing/page doubles. It failed on
the old code, then passed selection, replacement, preview loading and exception
cleanup cases for both themes. It does not execute native GUI or the actual fleet
page actions. Logs: `artifacts/frontend-selection-before.log` and
`artifacts/frontend-selection-tests.log`.

Car-lot paging now selects an entry from the newly visible page, keeping preview,
title and purchase action aligned. Returning to a selected model aligns its page.
Up/Down navigation uses recorded screen rows/columns rather than a flat index,
preserving columns in grids and moving to the category bar from the top edge.
Missing/incomplete rows and independent control regions have targeted checks.

Both client variants compiled. Targeted checks passed: 12 frontend-context,
152 controls and 24 menu assets (188 total). This does not establish native
layout, physical-controller focus or the actual car-lot pagination interaction.
Build and package logs use the `artifacts/frontend-selection-` prefix. The server
payload is unchanged by this client UI fix. Nothing was installed or launched.

## 2026-10-18 — consistent rear cargo anchors

Network spawning created the cargo ZDO at the chassis origin. Motion and recovery
subsequently placed it at the rear access point. A fresh, undriven vehicle could
therefore pass the client's rear-approach UI condition but fail the server's
four-metre check against the centre, especially on a long vehicle. A new 350Z
regression failed before the fix (`artifacts/cargo-anchor-before.log`).

Spawn, motion and recovery now share UpdateCargoPose. It grounds the cargo anchor
where terrain is available and aligns both its ZDO and loaded scene object,
retaining the separate chassis pose used for rendering, physics and recovery.
No permission, reach, capacity or item serialization rules were loosened.
Serialized checks cover fresh 350Z placement, loaded motion/recovery and fresh
rear access on the longest catalogued MW chassis. Native API doubles establish
the command/position behavior, not actual vanilla GUI or ward behavior.

190 serialized integration and 53 storage checks passed. Both client variants
and the dedicated-server payload compiled. Server packaging now writes a unique
temporary ZIP, verifies its complete file list, manifest and payload SHA-256 values,
then replaces the output archive. A verification failure retains the previous ZIP.
No game/server process or installation was changed. Current evidence uses the
`artifacts/cargo-anchor-` prefix.

## 2026-10-18 — setup retry drafts and malformed cargo replies

The client previously rebased every unsuccessful Setup reply against the submitted
snapshot, dropping its edits on a revision conflict or transport timeout. Retryable
failures now use the last acknowledged baseline with a retry delay; validation and
authority denials still drop the refused submission while retaining later edits.
Successful replies reconcile dirty fields and clear the matching retry notice.
Rebasing now recognizes fields already present in the server snapshot, preventing
redundant saves after late replication. The wire format remains protocol 3.

Real BepInEx configuration checks cover stock and prefixed U2 layout, equipment,
engine appearance, hinges and tint across conflict, in-flight edits, confirmation,
reconstruction and capacity rejection. Serialized server-handler checks cover all
six audio slots and other interior fields without modifying cargo bytes.

A new truncated-v108 regression failed before the fix: Receive caught the parse
exception but sent no Setup reply. Resizing now catches decode/serialization errors
before committing bytes and returns an explicit refusal; it also checks requested
capacity before native item loading. The same regression passes afterwards with
inventory bytes and setup revision unchanged. Logs:
`artifacts/interior-truncated-cargo-before.log` and
`artifacts/interior-sync-retry-tests.log`. These tests use native API doubles for
the server; they do not prove live UI, native inventory resize or reconnect behavior.

Final targeted checks passed: 36 settings-isolation, 177 serialized integration,
19 client-transport and 22 authority checks (254 total). Both client variants and
the dedicated-server payload compiled. The server ZIP was independently checked
against its manifest (three payload files); no installed files or processes were
changed. Build logs use the `artifacts/interior-sync-` prefix.

## 2026-10-18 — garage inspection and detail navigation

Follow-up scope correction: the user's interior report concerns server
synchronization. The cramped style picker concerns gauges, while the requested
UX redesign applies to the entire garage. Menu changes and animation guards are
not evidence that the synchronization defect is resolved.

The shared garage shell now has a horizontal category bar, a separate 950x630
car viewport and a uniform right settings panel. The render target and mouse
capture/zoom region use that same viewport. Category navigation follows its
horizontal orientation, and Up from the first content control returns to it.
Gauge selection uses native artwork cards, separate U2/MW families and a distinct
appearance page. Car-lot entries have wider two-column choices. The common footer
shows pending server edits or acknowledged revisions when there is no other
message; local rendering must not be mistaken for server acknowledgement.

Both client variants compiled after the shared-layout change. Menu assets (24),
controls (146), camera/HUD math (31), settings isolation (24) and serialized
client/server (161) checks passed: 386 total. These tests do not execute the new
IMGUI hierarchy, thumbnail rendering or physical controller focus. Evidence:
`artifacts/garage-layout-tests.log`, `artifacts/garage-layout-sp-build.log` and
`artifacts/garage-layout-network-build.log`. The client archive is rebuilt without
installing it; the server binary is unchanged by these UI edits.

Split body details into Panels/Hinges/Engine/Glass and trunk audio into
Layout/Equipment/Lighting. Unsupported opening panels are disabled using both
imported geometry and animation availability; missing animation tracks no longer
throw during playback. Existing hinge changes still close before switching.
Body-part choices use wider two-column pages; finish choices have two rows and
the paint content region accommodates shift colour/shade controls.

The U2 room ceiling and nearby props can intercept the orbit camera at high
pitch/zoom. Scenery renderers are cached before creating the preview car. Five
rays towards its inspection focus temporarily suppress blocking scenery, then
restore it when the camera moves or the frontend closes. This also applies to
the MW set, without changing world collision or vehicle renderers. Native scene
appearance and camera transitions remain acceptance work. No installed client
or server files were touched.

Both client variants compiled. Targeted showroom/bass (24), menu assets (24),
camera/HUD math (31), settings isolation (24) and serialized client/server (161)
checks passed: 264 checks total. These do not execute the changed IMGUI layout,
native scenery renderer visibility or real network/audio playback. Evidence:
`artifacts/garage-ux-tests.log`, `artifacts/garage-ux-sp-build.log` and
`artifacts/garage-ux-network-build.log`.

## 2026-10-18 — MW black cavities incorrectly treated as body paint

Compared owned COBALTSS, GTO and MUSTANGGT GEOMETRY.BIN material tables. Each
KIT01_BODY_A has BODY_PLAINNOTHING with effect 4 and texture key 0x1b049702.
The complete files contain 86, 84 and 81 corresponding sections respectively,
including LOD/damage variants. Reports and source SHA-256 values are in ignored
`artifacts/mw-cavity-comparison.json` and `artifacts/vehicle-source-audit/`.

The importer inferred user paint from the BODY prefix without excluding
PLAINNOTHING. Consequently the styling system registered black cavities as vinyl
surfaces and replaced their black texture with the painted composite. A missing
texture also used the exterior paint fallback. This explains replication across
cars and why adding matte reverse faces or changing BOTTOM did not fix it.

The exporter now excludes this role from paint and gives it a matte black
material. ModelFile applies the same correction to old `mw_*plainnothing*`
descriptors before meshes, styling and wetness are created. Neither geometry nor
handling changes. Existing MW asset installs need no full re-export for this fix.
Three-car targeted cases and 302,809 full-fleet loader checks passed; game visual
acceptance is pending. Universal Modder CLI and reverse-engineering skill were
available. REA was not exposed as a callable tool and plugin discovery found no
matching connector; no REA decompilation is claimed.

Material-name reference: https://nfsmodderscorner.blogspot.com/p/need-for-speed-most-wanted.html
