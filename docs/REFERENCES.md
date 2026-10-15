# Technical references

## NFSU2 nitro image filter and light trails, investigation 2026-10-09

- [yugecin: NFSU2-Grafikoptionen](https://yugecin.github.io/nfsu2-re/docs.html)
  maps Motion Blur, Light Trails and Light Glow to separate switches
  `g_MotionBlurEnable`, `g_LightStreaksEnable`, `g_LightGlowEnable` zu.
  The same names were found in the local `SPEED2.EXE` at file
  offsets `0x3a37e4`, `0x3a37cc`, `0x3a37b8`. This confirms separate options,
  but not the exact algorithm or exclusive activation by nitro.
- Local EXE: SHA-256
  `2cbd698fee8338a13d1891bc17d27e6641b4b9dd40580de818594b9140222899`.
  The following were read from its PE resources for local research only:
  `IDI_FILTER_FX` (14376 bytes), `IDI_OVERBRIGHT_FX` (5692 bytes) and
  `IDI_SCREENFILTER_FX` (16116 bytes). Embedded names indicate
  four filter textures with weights, an overbright filter and a separate
  color/image filter. This confirms D3D9 filter components, not a
  complete analysis of draw calls or the nitro filter core.
- Continued investigation: 20 embedded vertex/pixel shaders from these three
  resources were analyzed with the local Microsoft D3DX9 disassembler, without
  running the game EXE. `IDI_FILTER_FX` contains four variants with
  1, 2, 3 or 4 samples each: a component-wise weighted sum
  `output = sum(texture_i(uv_i) * weight_i)`. The related vertex shaders
  pass through position and four UV sets unchanged. Radial displacement,
  vanishing point, weight values and any previous frames are not defined there;
  bound textures, vertex data and draw calls from the running game would need
  to be examined. Four inputs do not mean four previous frames and do not
  prove that nitro uses this filter.
- `IDI_OVERBRIGHT_FX` reads a texture, subtracts an adjustable offset,
  amplifies and clamps RGB, weights it by brightness and squares
  the result. This shader isolates/amplifies bright areas; it does not itself
  calculate spatial blur. The examined `SCREENFILTER` variants
  contain color/gain/ramp-map processing. The name “Screenfilter” alone
  therefore does not establish a nitro tunnel-vision effect.
- Static code analysis limit: the local EXE has an additional
  entry point at VA `0x93309e`; its `.text` section has near-maximum
  byte entropy (about 7.999 bits/byte) and yields no plausible normal
  x86 code. This suggests a packed/protected code image. Direct
  callers of the readable effect names cannot be reliably reconstructed from it.
  No specific protection method has been identified.
  The next reliable evidence would be a readable runtime image and
  a graphics capture of the same driving scenario with/without nitro and with
  Motion Blur, Light Trails and Light Glow toggled individually. In particular,
  inspect texture history, UVs, filter weights, blend states and trail geometry.
- [NFSU2_CustomShaders: eEffect.h, Revision 1e850a4](https://github.com/ArchieGoldmill/NFSU2_CustomShaders/blob/1e850a41661181cef4e65effb6ba75ac8bc7251f/eEffect.h)
  documents related effect types and shader parameters such as
  `FILTERWEIGHTS`, `SCREENOFFSET`, `MOTIONALPHA`. These parameter names also appear
  also appear in the local EXE; `MOTIONALPHA` was not found as a string in any
  PE resource. This does not rule out another use.
  Third-party mod shaders are not evidence
  of the unchanged original nitro implementation.
- Local `GLOBAL/GlobalB.lzc`, decompressed: original texture
  `SFX_FLARE_STREAK_A`, 128 × 128, successfully extracted with the existing decoder
  extracted and visually checked. It shows a small bright flare on
  a dark background. Its specific mapping to the taillight trail and its
  lifetime/blend mode have not been established. Original data
  and extracted effects remain under `.local/effects-research/`.
- Implication for a possible NFVU2 recreation: a separate radial image filter
  (blurring toward/from the vanishing point), short-lived light trails at the
  taillights and a soft glow. Exact NFS sample positions, weights,
  movement/nitro coupling and temporal history remain open research questions.
  Implemented in 0.17.0 as a custom recreation: six radially offset
  image layers with a soft vehicle/center mask, plus short red 3D light bands.
  UV offset, weighting, envelope, trail texture and timing are NFVU2 values.
- The complete [yugecin repository, revision 192090c] was also(https://github.com/yugecin/nfsu2-re/tree/192090c564159b6a25e8dda06a8701477f7b5f1b)
  checked: `docs/docs.txt` lists the three switches; `docs/dumps/hashes.txt` lists
  the filter parameter names. No complete nitro filter/light-trail logic was
  found. No unverified original NFS values are derived from it.
- Unity [OnRenderImage](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html)
  and [GL.LoadOrtho](https://docs.unity3d.com/ScriptReference/GL.LoadOrtho.html):
  camera image filters in the Built-in Renderer. `UI/Default` is confirmed in the locally installed
  Valheim `unity_builtin_extra`. NFVU2 uses its own
  material instance and UV geometry; it does not distribute game shaders. Switching SRPs
  disables the screen filter; world light trails remain independent.
  The local D3D11 variant was also disassembled: `_MainTex_ST` transform
  in the vertex shader, alpha premultiplication in the pixel shader, Blend One/InvSrcAlpha.
  Therefore, radial displacement does not require a mesh change for each image layer.

## Additional sources

- Local `GLOBAL/HUD_CustomTextures_00.bin` through `_10.bin`: eleven original
  TACH_FILL_CUSTOM panels, 3RDPERSON_7500LINES_CUSTOM scales and
  3RDPERSON_RPMDIAL_CUSTOM needles. Padding is cropped for the new layout.
  `GLOBAL/InGameRace.bun`: TACH_NOS_ALPHA and TACH_NOS_BACKING; the alpha variant
  avoids the black background of the separate TACH_NOS color texture.
  Imported with the existing TexturePack decoder; live values, color selection,
  positioning and fill masks are custom NFVU2 adjustments.
- Local `CARS/350Z/GEOMETRY.BIN`: KIT00–03_TRUNK_AUDIO, KIT00/STYLE01/STYLE02
  ENGINE and ENGINE_UNLIT, plus door panels and hood undersides.
  `CARS/AUDIO/GEOMETRY.BIN` and `CARS/TEXTURES.BIN`: original components and
  AUDIO_SKIN; placement via MARKER_AUDIO_COMP_0..5. Part dimensions chosen to fit.
- Local `CARS/350Z/PARTS_ANIMATIONS.bin`: ELF32 animation channels and 21
  pivot matrices. Eight tracks imported; source pose and lateral translation
  are preserved, while playback time is normalized for the mod.
- [PartAnimatorulator](https://github.com/TVasya/PartAnimatorulator): Referenz
  for pivot order, quaternion/translation data and ELF animation blocks.
  Independent read-only import in `tools/showroom_assets.py`; the tool and
  original data are not distributed.
- Locally installed `assembly_valheim.dll` (Valheim 1.0.17): signatures/IL for
  `Character.Damage`, `Destructible.Damage`, `HitData.SetAttacker` and
  `BaseAI.IsEnemy` were checked for native vehicle hits; no game library is bundled.
- [Jotunn prefab list](https://valheim-modding.github.io/Jotunn/data/prefabs/prefab-list.html):
  `Beech_small1/2`, `FirTree_small/_dead` and `Bush01`, `Bush01_heath`,
  `Bush01_deepnorth`, `Bush02_en` have `Destructible`; large trees and
  planted saplings are explicitly excluded from vehicle hits.
- Local original files `LANGUAGES/English.bin`,
  `LANGUAGES/German.bin` and `LANGUAGES/Labels.bin`: contiguous list
  of the 14 headlight presets, from STOCK/SERIE through the BRIGHT_YELLOW preset.
  The English Xenon string begins at 0xABD7; the labels block contains
  STOCK, XENON, RED/BLUE/GREEN/PURPLE/YELLOW and their corresponding BRIGHT names.
  Only names are reused; RGB values and brightness factors are approximations.
- [OpenUG2-Dateiformate](https://github.com/whoismept/OpenUG2/blob/main/docs/FORMATS.md): background for NFSU2 model extraction.
- [OpenUG2-Parser](https://github.com/whoismept/OpenUG2/blob/main/src/nfsu2.h):
  Reference for UG2 material ranges, attachment sockets and compressed textures.
- [Nikki-PresetRide](https://github.com/SpeedReflect/Nikki/blob/master/Nikki/Support.Underground2/Class/PresetRide.cs):
  Field order of the `DDAY_PLAYER_CAR` preset (0x338 bytes).
- [EA-Huffman-Format](https://problemkaputt.de/psx-spx.htm#cdromfilecompressioneamethodshuffman):
  Specification for local vinyl decoding.
- [vgmstream r2117](https://github.com/vgmstream/vgmstream/releases/tag/r2117): unmodified external audio decoder.
- [vgmstream GIN parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/gin.c): GIN header and audio codec.
- [NFSU2CarSoundTuner](https://github.com/nlgxzef/NFSU2CarSoundTuner): reconstructed engine/turbo tables; not evidence of a complete trigger state machine.
- [Unlimiter CarEffect](https://github.com/nlgxzef/NFSU2Unlimiter/blob/master/CarEffect.h): separate nitro, exhaust blow-off and NOS blow-off effect types.
- [350Z sweetener table](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/SweetnerDataSet/27.ini): sputter/shift volumes and bank; exact throttle-change/upgrade conditions remain open.
- [vgmstream ABK parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c): original bank substreams and loop metadata.
- [XtraTrax importer](https://github.com/RadeonChan/XtraTrax/blob/main/patcher.py): SDAT.VIV/BIG4 and MPF/MUS music containers.
- [vgmstream-MPF/MUS-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_map_mpf_mus.c): Substream-Decodierung.
- [NFSU2TraxRenamer](https://github.com/nlgxzef/NFSU2TraxRenamer): original 27 EA Trax titles; exact source revisions and file checksums are in `tools/music_titles.json`.
- [Nissan 350Z Werkstatthandbuch 2004, LT-109](https://manuals.plus/m/9e82ecaf8253036632c55385423e37081480f44596e1ec36936b69de0ec59c91.pdf): reverse and turn lights in the rear bumper, brake/taillights above; original document hosted on a mirror server.
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/): mod loader.
- [Unity WheelCollider](https://docs.unity3d.com/6000.0/Documentation/Manual/WheelColliderTutorial.html): vehicle physics.
- [Unity AudioSource](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource.html): spatial audio playback.
- [Unity Texture2D.SetPixelData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.SetPixelData.html)
  and [Apply](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.Apply.html):
  upload an individual texture level and generate a mipmap chain.

External libraries and tools retain their respective licenses. They are not
provided here as source code or binaries. Original game content is prepared
from the user's local files.

Rust import in 0.9.0: [UnityPy 1.25.4](https://github.com/K0lb3/UnityPy) to
for reading locally installed Unity objects, mesh channels, textures and audio clips.
Source prefabs: `assets/prefabs/npc/autoturret/autoturret_deployed.prefab` and
`assets/prefabs/weapons/ak47u/ak47u.worldmodel.prefab`. The import includes
LOD0 and main textures, no Rust assemblies. WAVs are downmixed to mono for spatial
playback. The specific asset list and adjustments are also recorded
in the ignored `.local/turret/provenance.json`.

0.10.0 adds `assets/prefabs/weapon mods/lasersight/lasersight.entity.prefab`
and the `explosive-ammo-close` clip. Mount position and beam rendering are
custom adjustments. Valheim's terrain API (`TerrainComp.DoOperation`,
`Heightmap.GetAndCreateTerrainCompiler`) was inspected in the locally installed
assembly was inspected with ILSpyCmd 8.2. The mod uses its save, grass and
mesh recalculation; decompiled game sources remain exclusively in `.local/`.

Subwoofer in 0.11.0: [Unity OnAudioFilterRead](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html)
for analyzing interleaved samples on the audio thread. The callback
does not change samples or call Unity APIs; the bass envelope and
membrane deformation are custom NFVU2 implementations.
# Rust logo in 0.21.0

- [Offizieller Facepunch-Rust-Pressebereich](https://facepunch.com/games/rust)
  and the [logo archive] linked there(https://facepunch.com/s/rust-logos.zip).
  `tools/import_rust_logo.py` imports `Rust_Branding-1080.png`, removes
  only the transparent outer margin and scales it down proportionally.
  Original colors and the complete word/device mark are retained; the
  angled placement in the menu is NFVU2 design. © Facepunch Studios Limited.
  Download, pixels and checksums remain in ignored local files.

## Exhaust and sound in 0.21.0

- Lokales `CARS/EXHAUST/GEOMETRY.BIN`: `STYLE01_LEVEL1_A` bis
  `STYLE10_LEVEL1_A`. Tailpipe openings are identified from connected original
  dark outlet surfaces; installation uses the existing sockets.
- [NFSU2 Car Sound Tuner, explanation of original data](https://github.com/nlgxzef/NFSU2CarSoundTuner#explanation):
  NFSU2 has sound mappings for Stock/Street/Pro/Extreme performance stages.
  This does not establish freely selectable exhaust sound presets in the original menu.
  NFVU2 continues to use its previously imported recordings; “Deep” and
  “Sport” change pitch and load/coast mix as custom tuning options.

## Turbo and boost gauge in 0.22.0

- [350Z-Zuordnung](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarTypeMapping/350Z.ini)
  and [CarDataMapping 02](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarDataMapping/02.ini):
  Stock/Street/Pro/Extreme use TurboDataSet 6/6/7/8.
- [TurboDataSet 08](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/TurboDataSet/08.ini):
  `TURBO_MED_2_MB.abk`, `TurboChargeVol=8000`, `LowBoostBovVol=24500`,
  `HighBoostBovVol=16500`, `ChargeTime=0.5`. These are reconstructed original
  data tables from a community tool, not official engine documentation.
  The tables distinguish low/high boost blow-off; the exact original
  trigger, pressure and volume curve could not be established from them.
- [vgmstream ABK-Parser](https://github.com/vgmstream/vgmstream/blob/master/src/meta/ea_schl_abk.c)
  and local decoding: five substreams, of which 1 and 5 loop. The applied
  role mapping 1=spooling, 2=Low, 3=High remains an NFVU2 assumption.
  The shared import factor 0.8 preserves relative sample levels; the previous
  independent RMS normalization of each clip is no longer used. Dividing the above
  values by 32768 is our Unity gain conversion, not a verified mixer unit.
- Local HUD textures `3RDPERSON_TURBOFILL_CUSTOM_XX` and related
  `TURBOLINES`: original graphics for all eleven designs. The scale shows
  -30/0/+30; the unit is unverified and is not labeled as PSI.
  Load model, pressure mapping, vacuum, needle flutter and dynamic gains
  are custom NFVU2 tuning. Sound and gauge use the same state;
  vehicle physics remains independent of it.

# Menu and parking automation in 0.13.0

- Local NFSU2 files: `FRONTEND/FrontB.lzc`,
  `LANGUAGES/Labels.bin`, `LANGUAGES/German.bin` and
  `SOUND/GLOBAL/FE_COMMON_MB.abk`. Export records SHA-256, texture keys,
  language keys and audio substreams in `.local/menu/provenance.json`.
- Icons/titles are original data; layout, menu actions and sound roles are
  custom adjustments. The native sound roles have not yet been established.
- [Facepunch: Automated Turret](https://wiki.facepunch.com/rust/Item/autoturret):
  180° frontal target search. The mod uses ±90° relative to the vehicle front;
  scan motion, 30 m range, Valheim enemy filtering and firing logic are custom
  rules, not a complete recreation of Rust behavior.

# Hummer H2 in 0.12.0

- Local NFSU2 CDs: `CARS/HUMMER/GEOMETRY.BIN`, `TEXTURES.BIN`,
  `CARS/WHEELS/GEOMETRY_SPINNER.BIN`, shared tyre/brake textures.
  `SPINNER_STYLE01_26_34_A` and separate `SPINNER_STYLE01_26_34_SPIN_A`.
- HUMMER wheel locators in `GLOBAL/GlobalB.lzc`, at 0x120 + n*0x30
  relative to the model name. Import checks wheelbase/track width before use.
- [CarTypeMapping/HUMMER](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarTypeMapping/HUMMER.ini),
  [CarDataMapping/14](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/CarDataMapping/14.ini),
  [EngineData/38](https://github.com/nlgxzef/NFSU2CarSoundTuner/blob/master/CarSoundData/EngineData/38.ini):
  Stock H2 uses `GIN_Hummer.gin`, `UseDualGinsu=0`, 1000–4782 RPM.
  Local GIN grain table: recording range about 1266.9–4782.7 RPM.
  Below the recording limit, its grains are resampled to the target RPM.
- Yellow paint, spinner persistence, tyre width, audible shift timing,
  driving physics and light projection are NFVU2 adjustments. No complete
  recreation of the original NFS suspension or audio system is claimed.
