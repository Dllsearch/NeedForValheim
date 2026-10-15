---
name: release
description: Publish an NFVU2 GitHub release with a version tag, release notes, verified Windows installer and SHA-256. Use for /release, $release or an explicit publication request.
---

# NFVU2 GitHub Release

Publish the currently requested project state as a GitHub release. Calling
`/release` or `$release` requests preparation, build, commit of related changes,
push, version tag, asset upload and publication. If a draft is explicitly
requested, stop at the draft. A question about releases or creating this skill
is not a publication request. `/installer` builds locally only.

## State and access

- Read `AGENTS.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `docs/ROADMAP.md` and the
  [installer skill](../installer/SKILL.md). Run commands from the repository root.
- Check the remote, branch, working tree, relevant changes and local/remote
  tags/releases. The expected repository is `Backwoodz/NFVU2`; do not use another
  destination without a corresponding request.
- Check GitHub tools and sign-in with write access. Prefer the available GitHub
  CLI (`gh auth status`) or suitable API tools. If `gh` is not on PATH, look for
  existing local installations. Alternatively, use the signed-in browser with
  the Computer Use skill. Do not expose tokens.
- Commit only changes related to the requested release. If unrelated changes
  make the target state unclear, prepare notes and checks independently and
  clarify only that specific point.

## Version, build and commit

- Use the requested version. Otherwise, for a new bug-fix release, choose the
  next available patch version above the current mod version and published
  versions. Do not reuse or move existing release tags. Changes to existing
  releases require an explicit request.
- Update `src/NFVU2.csproj` and the `BepInPlugin` attribute together. Move related
  Unreleased entries into a versioned changelog section.
- Build and verify the installer following the installer skill. Write English
  release notes with specific changes, installation instructions, platform and
  verification results. Claim only in-game tests that were actually performed.
  Save the notes under `artifacts/` and use `--notes-file` with `gh` to preserve
  real line breaks.
- Commit the verified state, including version and setup revision, using
  explicit file paths. Ignored assets/binaries stay local. The tag must point
  exactly to the commit whose build inputs produced the installer. Rebuild and
  reverify after any further build-relevant changes.
- Push the release commit to the intended branch without force-pushing. Create
  an annotated `v<version>` tag on its exact SHA and push that tag. Verify the
  remote tag SHA. Do not publish if there are conflicts or errors.

## GitHub-Release

- First create a draft with the tag, title, notes and verified EXE plus its
  `.sha256` as assets. Do not upload `.rsp` or `payload.zip`. GitHub provides the
  source code archives itself. For example, with the CLI:

```powershell
gh release create <tag> <installer.exe> <installer.sha256> --repo Backwoodz/NFVU2 --verify-tag --draft --title <title> --notes-file <notes-file>
gh release view <tag> --repo Backwoodz/NFVU2 --json url,isDraft,tagName,assets
```

- Replace placeholders with specific verified values. On retries, check whether
  the draft or upload already exists. Do not blindly replace existing assets with
  `--clobber`; releases are immutable.
- Download uploaded assets to a separate ignored verification directory and
  compare their SHA-256 values with the local files. Check the tag, target commit,
  title and notes. If anything fails, leave the draft unpublished and report it.
- When publication was requested, publish the verified draft, for example with
  `gh release edit <tag> --repo Backwoodz/NFVU2 --draft=false`. Then check that
  the release page is reachable and both download assets are present. Never call
  a draft published.
- Then update the README download link and size to match the actually published
  asset; commit and push this documentation change. Do not move the release tag
  to this later documentation commit.

## Abschluss

Provide the direct GitHub release link, version/tag, installer download link and
verification result; identify any pending in-game tests. If access is missing,
complete local preparation and state the sign-in step still required.
A regular push does not run this skill; the project does not currently have an
automated release workflow.

Referenzen:
- [GitHub: Releases verwalten](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [GitHub CLI: release create](https://cli.github.com/manual/gh_release_create)
