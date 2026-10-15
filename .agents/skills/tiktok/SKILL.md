---
name: tiktok
description: Edit NFVU2 gameplay recordings with FFmpeg into vertical clips using the approved zoom, blurred background and color correction. Use for /tiktok, $tiktok or additional NFVU2 gameplay clips for TikTok.
---

# NFVU2 TikTok clips

Create local video exports in the style approved by the user. All project paths
are relative to the repository root. Do not build a custom editing app.

## Request and local files

- Use the source file and start/end times from the request; duration = end minus
  start. If the video is unclear, ask for the recording and prepare a contact
  sheet in the meantime. Timestamped frames can help choose scenes that have not
  yet been selected. The command alone does not authorize publication.
- Read `.local/tiktok/NOTES.md`, the relevant `.local/tiktok/settings*.json` and
  existing local helpers, if present. Absolute source/export paths belong there
  only. Do not replace missing originals with an old export.
- Previous source files are stored locally in the Videos/Desktop folder. Finished
  MP4 files belong in its `NFVU2-Exports` subfolder, outside the Git repository.
  Use the existing `export_directory` or find the current user's Videos folder.
  Create frames, logs, tools and configuration under `.local/tiktok/`; do not put
  personal absolute paths in tracked files.
- Leave originals unchanged. Do not overwrite or delete existing exports. The
  user deleted earlier Driving test variants; do not restore them.

## Approved standard

Use recordings with **3440 × 1440, 60 FPS, HEVC Main 10, HDR PQ (smpte2084), BT.2020 and full range**. Check every new source with
FFprobe or `ffmpeg -hide_banner -i`; SDR does not need HDR tone mapping.
Other resolutions need adjusted geometry; do not reuse these values blindly.

| Component | Approved setting |
| --- | --- |
| Output | 1080 × 1920, SAR 1:1, H.264, yuv420p, SDR BT.709 limited range |
| Frame rate | Preserve the source frame rate; so far 60 FPS, no slow motion |
| Sharp source crop | 1720 × 1280, x=860, y=160 |
| Foreground | 1080 × 804, x=0, y=558; no stretching |
| Background | Same clip, synchronized and scaled/cropped to 9:16 |
| Background effect | First 270 × 480, Gaussian Blur Sigma 6, saturation 0.65, brightness −0.04; then 1080 × 1920 |
| Gameplay color correction | Contrast 1.14; saturation 1.18; brightness +0.016; gamma 1.10 |
| Title | English scene-specific text, white, black 4 px outline, Arial Bold 54 px; x=(w-text_w)/2, y=464 |
| Video-Encoding | libx264, preset fast, CRF 20, fps_mode passthrough, movflags +faststart |
| Audio | Preserve recorded game audio including radio; AAC stereo 48 kHz, target bitrate 192 kbit/s; no volume change |

Apply color correction only to the sharp gameplay; keep the background soft and
subdued. Do not crop the subject: check the car, including flight jets, open
doors/trunk and any dancing character. The crop is a starting point; adjust it
for other camera positions. A fixed crop has been sufficient so far. Some HUD/menu
fragments at the edges are acceptable; do not crop the car or character to remove
them.

HDR tone mapping before color correction (for the HDR source described above):

```text
zscale=rangein=full:transfer=linear:npl=100,format=gbrpf32le,zscale=primaries=bt709,tonemap=tonemap=mobius:desat=0,zscale=transfer=bt709:matrix=bt709:range=limited,format=yuv420p
```

The existing renderer scales to 2580 × 1080 (75%) before tone mapping. At this intermediate resolution, crop 1290 × 960 at x=645, y=120. Then scale to 1080 ×
804 and apply `eq=contrast=1.14:saturation=1.18:brightness=0.016:gamma=1.10`.
Split foreground/background from the same video branch, composite them, then add
the title.

## Export and verification

The existing FFmpeg is currently at
`.local/tools/audio-encoder/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe`.
If it is missing, first look for an installed FFmpeg; keep required tools locally
under `.local/`. If helpers are missing, recreate this workflow directly with
FFmpeg; do not invent old user-specific paths.

- First extract representative frames, including the start/end and camera pans.
  Check framing and brightness. For a new style, render a short sample first;
  for further clips in the approved style, render the requested segment directly.
- Create a separate ignored settings file for each recording. The existing
  script enables the approved color correction **only with `--color-correction`**:

```powershell
python .local/tiktok/render_preview.py --settings settings-flying.json --start 39 --duration 32 --color-correction --output Flying-39-71-zoom-color.mp4
```

- Filenames should identify the subject, source time range and variant. If a name
  collides, choose a new revision. Use suitable titles; do not present game
  features as your own implementation. Do not export extra comparison videos for
  every new clip.
- Check the entire finished file for decode errors, resolution, frame rate,
  color encoding, audio track and duration. AAC packet boundaries may extend the
  MP4 duration by a few milliseconds. Visually check a layout frame and samples
  throughout the clip. A technical audio-track check is not a listening test.
- Update `.local/tiktok/NOTES.md` with the segment, settings, filename and check
  results. Provide a clickable file path and the exact filename.

## Previous clips

| Source | Selected time range | Title | Export |
| --- | --- | --- | --- |
| Driving.mp4 | 00:24–01:12 | Driving a 350z in Valheim | Driving-24-72-zoom-color.mp4 |
| Driving and turret.mp4 | 00:18–00:56 | 350z with a turret in Valheim | Driving-turret-18-56-zoom-color.mp4 |
| Subwoofer and dance.mp4 | 00:07–00:45 | Dancing with a 350z in Valheim | Dancing-07-45-zoom-color.mp4 |
| Flying and turret.mp4 | 00:39–01:11 | Flying a 350z in Valheim | Flying-39-71-zoom-color.mp4 |

This table records the previous state; it is not a request to render the clips again.
