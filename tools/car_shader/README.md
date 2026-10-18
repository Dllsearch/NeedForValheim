# Experimental vehicle paint shader

Experimental source and build tooling. Client builds embed a verified bundle when
available; runtime loads it lazily for Pearl/Chameleon body paint and otherwise
retains the original shaders. Builds without the bundle hide these finish choices.
It requires the Unity Editor and the built-in render pipeline. Prefer the same
editor version as the installed game's Unity runtime when producing a bundle.
Do not install an editor automatically or replace a running game installation.

The opaque paint shader retains texture/vinyl multiplication, Standard lighting,
reflections, shadow casting and the existing `_Color`, `_Metallic` and
`_Glossiness` properties. `_FinishMode` selects ordinary paint (0), a subtle
view-dependent pearl tint (1), or a stronger two-colour chameleon shift (2).
This is an NFVU2 rendering adaptation, not an original NFS shader port or a
physical thin-film simulation. Glass, carbon, rubber, trim and lights must retain
their existing materials. Do not apply this shader to every vehicle material.

Repeatable PowerShell build, after choosing an installed Unity executable:

```powershell
python tools/build_car_shader.py --unity $unityExe
# Also load the bundle and render ordinary, pearl and chameleon test spheres:
python tools/build_car_shader.py --unity $unityExe --render-preview
```

The wrapper enables required built-in Unity modules, writes logs to
`artifacts/car-shader/build.log`, and records a successful bundle's SHA-256 in
`verified-build.json`. It does not install the bundle in Valheim.

Pending gates: load the generated asset bundle in the real client; confirm shader
support, lighting and shadows; compare front and grazing views with vinyls and
multiple base shades. Runtime finish controls use saved/network Styling/Finish
and Styling/ShiftColor keys. A compiler success alone does not prove game gates.

Local editor validation now passes on Unity 6000.0.75f1 / Direct3D 11: the bundle
loads and pearl/chameleon produce measurable changes against ordinary paint.
The test caught an initially ineffective view-angle calculation and now uses
world-space surface normals. Rendered test spheres do not establish vehicle,
vinyl, Valheim lighting or multiplayer acceptance.

The graphics fixture also renders opaque-artwork and exposed-paint masks with
both custom finishes. Covered artwork has zero changed pixels above the test
tolerance; exposed paint must show a measurable shift. CPU composition checks
cover transparent edges and overlapping layers. Actual vehicle vinyl UVs and
Valheim lighting still require game acceptance.
