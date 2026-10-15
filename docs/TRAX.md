# Frontend designs and EA TRAX

Choose **U2 / MW** in the garage header, or **Options → Menu design** with the
keyboard/controller. The saved design choice is independent of the selected car.
U2 artwork remains installed. MW uses its own original grunge, graffiti, icons,
car logos and frontend sounds. Titles and navigation labels are rasterized from
the original `FONT_MW_TITLE` atlas and FNTF glyph metrics. Garage actions and
layout are NFVU2 adaptations; this does not execute MW's frontend packages.
The three sound roles are adapted from local FE_COMMON samples, not a recovered
event-to-sample table. Missing optional MW media leaves text controls usable.

Open **Radio** and select **Underground 2**, **Most Wanted**, or **Mixed**.
The U2 and MW source toggles apply globally. **EA TRAX / Enable tracks** lets you
exclude individual tracks, including tracks from an inactive source. All choices
are saved under `EA TRAX` in the existing plugin configuration and shared across
parked cars. The car's radio ON/volume and existing cabin/exterior acoustics remain
per vehicle. Turning every eligible track off gives silence; it does not silently
switch to another station. Missing libraries retain their saved exclusions.

Playback starts randomly among eligible tracks, avoiding the last played track
when there is another choice. A shuffled bag plays each eligible track once before
refilling, without an immediate repeat at the cycle boundary. Previous follows
actual listening history; Next returns through that history before continuing the
bag. A failed stream is skipped until manually retried; a completely failed or
disabled station cannot spin in a retry loop. Late async load results cannot
replace a newer selection. Exclusions use source-prefixed filename stems, so WAV
to Ogg conversion and identical U2/MW filenames do not change their identity.

## Local extraction

```text
python tools/export_mw_frontend.py
python tools/extract_mw_music.py --mpf-tool <local MPFmaster.exe> --ffmpeg <local ffmpeg.exe>
python tools/test_trax.py
python -m unittest discover -s tests -p test_mw_frontend_music.py
```

Use the existing Pillow/NumPy dependencies, the configured vgmstream decoder,
local FFmpeg with libvorbis, and [MPFmaster 1.8](https://github.com/xan1242/MPFmaster/releases/tag/1.8).
`mw_root` in ignored `config.local.json` selects the source installation; optional
`mw_mpf_tool` selects MPFmaster, or pass the command-line path. No music is
downloaded and neither game installation is modified by the extractors.

MW's 26 artist/title/event records are read from the installed attributes vault.
Each event resolves to a single licensed-song chain in Section 5 of Pathfinder
5.1. The exporter preserves the ordered segments, including intentional repeated
samples; it rejects ambiguous branches or cycles instead of guessing from physical
stream order. Short audio fragments and interactive pursuit score are not exposed
as standalone songs. Whole stereo songs are joined at their original sample rate,
encoded to 192 kbps Vorbis, then fully decoded to verify channels/rate/frame count.
Native edits/censorship and original audio levels are preserved.

WAV segments/masters and source provenance remain ignored under
`.local/music-mw-source`. Only the 26 Ogg songs and `tracks.json` ship in
`radio-mw/`. U2's `radio`/`radio-ogg` pipeline is unchanged. MW artwork, bitmap
labels and the three sounds ship under the existing `menu/` allowlist, with hashes
and resource keys in `mw-frontend-provenance.json`. Original source archives,
fonts and MPFmaster are not included in the mod.

Targeted validation covers shuffle cycles/history, source and track persistence,
all-off and corrupt-source behavior, stale async identities, original event-chain
membership, exact segment boundaries, all exported Ogg hashes/frame counts,
bitmap alpha coverage, menu deserialization and PCM upload input. The isolated
compile writes `artifacts/trax-checks/`, never the install payload. These checks
do not establish Unity GUI rendering or audible in-game acceptance. No game
launch is needed for these preparation checks.
