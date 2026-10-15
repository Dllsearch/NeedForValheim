# NFVU2 development notes

- For `/installer`, `$installer`, or a request to build a local installer,
  read and follow `.agents/skills/installer/SKILL.md`.
- For `/release`, `$release`, or an explicit GitHub publication request,
  read and follow `.agents/skills/release/SKILL.md`.
- For `/tiktok`, `$tiktok`, or a request to cut NFVU2 gameplay into vertical
  social clips, read and follow `.agents/skills/tiktok/SKILL.md`.
- This repository is the source of truth for further NFVU2 development.
- Read README.md and docs/ROADMAP.md for current scope and known limitations.
- If present locally, see `handoffs/AGENT_HANDOFF.md` for U2 importer format
  findings and low-resource workflow. The `handoffs/` directory is local and
  ignored by Git.
- Keep original game files, extracted assets, downloaded tools and generated
  binaries in ignored `.local/` or `artifacts/`. Never force-add those files.
- Preserve the existing plugin ID and NeedForValheim.dll name unless a deliberate
  migration is requested; the user already has the mod installed.
- Write source text, comments and documentation in English. UI strings are English
  source text; `src/InterfaceText.cs` is the English-to-German locale table and
  `Interface.Language` defaults to `EN`, with English and German language labels.
  Name the built-in SUV by its identity (`h2` id, `h2.` config prefix, `h2/` and
  `audio/h2/`), name behaviour after the
  capability (`SuvTuning`, `IsSuv`), and never use the car's name as a generic flag.
  NFSU2 identifiers such as `CARS/HUMMER/` and `GIN_Hummer.gin` are literal game data.
- VehicleHandling.cs from version 0.1.4 is confirmed working in-game. Keep audio
  and visual changes separate from handling changes when possible.
- Build with `python tools/build.py`; run relevant checks with
  `python tools/test.py --skip-build`. These are not Unity runtime tests.
- Game installation paths belong in ignored config.local.json. Do not commit
  user-specific absolute paths, logs, credentials or personal identifiers.
- Do not overwrite the installed mod while Valheim is running. Keep a backup
  before replacing an installed version and verify the copied file's checksum.
- Keep only the two newest timestamped installation backups per backup folder.
  Remove older backups only after a successful, checksum-verified installation
  or activation/deactivation. Apply this to local extension installers as well.
  Preserve separately named backups (for example Flatgrass), source assets and
  private plugin sources. Never follow junctions/symlinks during backup cleanup.
- Be precise about provenance: the current model is a stock 350Z; Rachel's
  complete tuning/paint setup and a dual-game bridge remain future work.
