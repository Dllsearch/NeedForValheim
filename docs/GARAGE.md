# Underground garage

Open the inventory and click **CARS / GARAGE** above the item panel in a single-player
world. The fullscreen Underground 2 frontend uses the original logo, vehicle badges,
icons and menu sounds. Its independent 3D garage previews cars before spawning;
paint, parts, lights, wheels and door poses update there. The menu brakes the occupied
car and captures driving input. **CAR SELECT → Car lot** has 30 cars, nine per page.
**TAKE CAR** loads a car asynchronously and returns to My garage. A model can be
present only once; **SELECT OWNED CAR** selects its existing instance.
Up to four different cars can remain parked. Use the **REMOVE** button beside each
car to remove it regardless of distance, while parked. Saved entries also have a
remove action before recall. Cargo remains in a chest at the removed car's position.
Look at a car and press the native Use key (**E** by default) to enter; **Shift+E**
opens its garage. While seated, **Tab** opens the frontend and **E** exits (or lands
first). **F9 / L3** remains an additional shortcut. Function keys are not required.
Hold the right mouse button to orbit the showroom camera; the mouse wheel zooms.
Controller right stick also rotates the view. Closing the menu restores the
native player/camera and requires released driving controls before driving again.
**Options → Showroom camera → Showroom mouse speed** adjusts preview rotation independently
of the driving camera; the default is faster. Mouse and controller inversion and
controller preview speed are also configurable there. The floor uses original
NFSU2 pavement artwork instead of untextured checker geometry.
**Options → Driving camera → Driving mouse speed** controls the in-game vehicle
camera separately, with a 0.02–4 range. The default is 1.0; an old unchanged 0.15
default upgrades once, while other saved sensitivities are preserved.
Use **↑↓**, **Tab**, **Enter** and **Escape** for keyboard navigation; controller
navigation remains available. NOS kits work for every catalog vehicle, including
Golf and the SUVs, with original flame/purge artwork and car-specific exhaust points.
After removal the menu stays in My garage, tries the freed parking spot first
and searches a wider area if obstructed. A rejected spawn explains why in the
frontend footer and log. Cargo chests or terrain can still obstruct placement.

The garage saves its model roster. After entering a world, **My garage → Recall
saved garage** respawns that roster at free nearby positions. It never restores
cargo or old world positions. Remove individual cars to update the saved roster;
leaving a world retains it. Cars are local objects, not persistent world entities.
Parked cars hold their last supported pose when their terrain unloads and resume
physics after you return to loaded ground. An unoccupied car can be removed even
if it is moving or has fallen; removing the occupied car still requires parking.

The stage resolves its shader from loaded Valheim materials, with a supported
material fallback. Partial or failed stage creation is cleaned up without removing
world cars. The frontend has seven main categories and contextual subcategories;
stage and part buttons can be selected directly with arrow keys and Enter.
Panels are translucent; the scene is no longer covered by an opaque black fallback.

## Performance

Park and finish an active horde run before changing a build. All upgrade packages
are unlocked: this is a sandbox garage with no invented currency or purchase cost.
Each component has Stock and Stages 1–3:

| Component | Stage 3 effect |
|---|---|
| Engine | ×1.36 wheel torque; +12% forward speed limiter |
| Turbo/intake | ×1.30 wheel torque; +12% forward speed limiter |
| Transmission | ×1.105 wheel torque; +18% forward speed limiter |
| Tyres | ×1.24 longitudinal and lateral grip |
| Brakes | ×1.60 service braking torque |
| Nitrous | 10.5-second tank, ×1.30 ground boost acceleration |

Torque multipliers combine. The forward limiter reaches ×1.42 with the full
package; reverse speed, parking brakes, burnout holding brakes, base suspension
and car mass stay at their established values. Larger NOS tanks take proportionally
longer to refill. Menu/exit refill and changing direction to bypass braking remain
blocked. These are NFVU2 adaptations, not recovered original NFS performance tables.

## Styling

Paint has 14 colours plus the original finish; choose gloss, metallic or matte.
With a custom colour, **Paint chrome trim with the body** also colours exterior
chrome surrounds; disable it to retain chrome. Rim colour only affects wheels,
including on the H2 where the source calls some body materials `wheel_chrome`.
Vinyl can preserve the installed livery, remove it, add twin stripes or checkers.
New patterns use car-local UV projection and configurable colour. Factory restores
the source textures and UVs. Carbon/detail materials outside painted surfaces stay
as authored in the model.

**Body parts** exposes the car's own standard/wide body, front/rear bumpers,
skirts and hoods, plus the original spoiler packs at the car's attachment socket.
Widebodies use integrated bumpers/skirts: select **Standard body** before selecting
separate bumper/skirt parts. **Wheels** offers original BBS, Enkei, OZ, Volk, Work,
Racing Hart, 5Zigen, Momo, Konig and Rota designs, with colour and the existing
size/camber/offset/ride-height controls. Imported wheels are fitted to the current
car's tyre diameter and width; colliders and axle locations remain independent.
SUVs and hatchbacks expose fewer original parts where the source provides fewer.
SUV KITW entries consisting only of window placeholders are excluded. Atlas backing
quads outside wheel tyres are excluded. Vehicle surfaces use a neutral texture/tint
shader rather than inheriting a Valheim building material's style/damage masks.

Meshes are created over several frames and swapped after the selected option is
complete. Choosing another option cancels unfinished work. Parts stay attached to
existing door/hood/trunk hinges where available. Settings are independent per car
model, including the 350Z trunk audio, door mechanism and window tint.

## Builds and local assets

**Builds** stores three named setups for each car model. A setup captures performance,
styling, fitment, lights, radio/exhaust and showroom settings. Save/load explicitly;
ordinary menu changes also persist when closing F9. Restoration to the original
setup retains the three saved builds. Loading a smaller trunk layout is blocked
until its cargo can fit. Duplicate instances of the same model share its settings.

Generate the optional libraries from the user's extracted NFSU2 assets:

```text
python tools/export_customization.py          # all 30 cars
python tools/export_customization.py 350z     # one car
python tools/export_menu.py                   # original frontend icons/sounds
python tools/build.py
python tools/test.py --skip-build --suite handling --suite nitro --suite carprofile
```

The import scripts require Pillow/NumPy as used by the existing asset pipeline.
Libraries stay in ignored `.local/styling`, then ship under `styling/*.json` in the
local payload. Without a library, paint, performance and builds still work; the
parts browser offers Factory. Libraries load on a worker, validate through the
runtime model loader and are released with their car rather than retained in the
world cache. Original game data is for local use and is not committed to Git.
While driving, NFVU2 updates ZNet's native reference position after moving the
seated player; this replaces Player.LateUpdate's disabled zone-streaming update.

This recreates an Underground 2 frontend for Valheim. Original career progression,
shops, dyno graphs, the native NFS frontend runtime and all native NFS vinyl/decal
layers are not reconstructed. Most imported cars currently have no door animations,
engine-bay/trunk equipment or detailed cockpit. Those remain the existing 350Z
features; do not describe all 30 cars as having those assets.
