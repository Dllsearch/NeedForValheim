---
name: installer
description: Build a current local NFVU2 Windows installer with a verified mod and SHA-256. Use for /installer, $installer or a request to create a current local installer.
---

# NFVU2 Installer

Build a current Windows installer from the current project state, including
uncommitted changes. All paths and commands below are relative to the repository
root.

## Scope and preparation

- Read `AGENTS.md`, `README.md`, `docs/ROADMAP.md` and the build instructions in
  `CONTRIBUTING.md`. Check `git status --short` and the relevant diff.
- `/installer` requests a local build. Commit, tag, push, GitHub release and
  installation in the game require a separate corresponding request.
- Use existing local assets and `config.local.json`. If prerequisites are
  missing, identify the specific blocker; do not present an old installer as a
  current release or delete existing installers.
- Keep the plugin ID and `NeedForValheim.dll`. Do not change vehicle behavior
  solely for the release build.

## Version and documentation

- Use the current mod version by default. Verify that `src/NFVU2.csproj` matches
  the `BepInPlugin` attribute in `src/Plugin.cs`. If a new mod version is
  explicitly requested, update both.
- Set `SETUP_REVISION` in `tools/build_installer.py` so the selected mod version
  gets a new filename. If an installer with that revision already exists, use
  the next free revision above all existing revisions for that version. Do not
  overwrite an existing installer.
- Document included changes and the local setup revision in `CHANGELOG.md`; note
  pending in-game tests in `docs/ROADMAP.md`. Change public download links only
  after a release has actually been published.

## Build and verify

Run from the repository root:

```powershell
python tools/build.py
python tools/test.py --skip-build
python tools/build_installer.py
```

- Continue only if each step succeeds. The test run also includes installer
  tests. Checks that already passed on the unchanged current state need not be
  repeated; after changes, verify the affected parts.
- The installer must contain the freshly built DLL and the complete radio music
  collection. Use the existing payload builder with its allowlist and the
  specified BepInEx package; do not package private plugins or configurations.
- Check the generated EXE against its `.sha256` file. Verify every entry in
  `payload.json` against the bytes in `artifacts/installer/payload.zip`, and
  verify the included `NeedForValheim.dll` against `artifacts/mod/NeedForValheim.dll`.
- Also check that the EXE's embedded assembly resource `NFVU2.Payload` has the
  same SHA-256 as `payload.zip`. On Windows, use
  `[Reflection.Assembly]::LoadFile()` and `GetManifestResourceStream()`; do not
  launch the wizard or an installation for this check.
- Run `git diff --check`. Generated files remain ignored.

## Clean up old installers and provide results

- After a successful build and complete verification, keep only the newly
  generated installer and its `.sha256`/`.rsp` files. Remove older files
  directly in `artifacts/installer` whose names exactly match the pattern
  `NFVU2-Setup-<version>-r<revision>.(exe|sha256|rsp)` entspricht.
- Before deletion, verify the absolute target paths and skip links/junctions.
  Do not delete other files or directories. Keep `payload.zip`, source assets and
  separately named files. Installation backups remain subject to the retention
  rule in `AGENTS.md`.
- In the final response, provide clickable paths to the new EXE and checksum,
  the version/revision and the verification results. Clearly distinguish
  automated checks from pending Unity in-game tests.
