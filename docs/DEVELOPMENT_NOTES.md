# Detailed gameplay notes

Historical implementation notes retained from the former README.
For player setup use [README](../README.md); for development use
[CONTRIBUTING](../CONTRIBUTING.md). Version-specific statements describe their
original release; current limitations and pending in-game checks are in
[ROADMAP](ROADMAP.md).

## Controls

F6 or the spawn button starts a loading operation and displays the current
stage. File reading, JSON parsing and sound decoding run in the background;
Unity textures, meshes and audio clips are created incrementally. The finished
vehicle then appears at a newly checked
free spot in front of the player. You can
keep moving meanwhile; further spawns and entering a vehicle wait until loading
finishes. F8 or “Cancel” discards the operation without removing an existing car.
Death, teleport and world changes cancel it.

Unchanged model and sound data is cached for further spawns in the same world;
Unity resources remain independent per car. A changed file size or modification
time forces a new read. The cache uses additional memory and is freed when
leaving the world. The build budget is about 3 ms per frame; individual Unity
calls cannot be interrupted and may take longer. The log reports total duration
and the longest main-thread step. Smoothness has not yet been measured in-game.
The suspension tuning remains unchanged.

F9 → Vehicle → **Fly** enables flight mode in the occupied car. All four wheels
fold 90° in about 1.2 seconds, with the visible rim side facing up; the car lifts
gently and hovers. W/S flies forward/backward, A/D rotates the vehicle, Shift
ascends and Space descends. On release, it slows down and holds its position.
The mouse continues to control the camera. Menus and loss of focus block flight
input; stabilization remains active.

Four blue jets use the original NFS nitro flame texture and a locally tuned,
lower-pitched nitro/gas loop. Size and sound respond to thrust. Movement, flight
control and folding animation are NFVU2 adaptations. Hovering consumes no nitro.
W + left mouse button activates flight nitro, adding forward thrust up to about
158 km/h and drawing from a six-second
reservoir. After release, the reserve recharges
while moving, after a two-second delay. The
350Z shares one reservoir shared between ground
and flight; the H2 has nitro only in flight. During an actual flight boost, the
350Z also uses its normal green exhaust flames and nitro sound. Hovering alone
does not activate them. Purge and burnout remain disabled in the air. The H2 also
uses the 350Z-FIREB atlas. Shift ascends at up to 12 m/s and Space descends at up
to 16 m/s; movement slows near the ground. The wheels tilt with forward/backward
and steering input; the flames below them point opposite to the thrust direction.
The speedometer displays digital KM/H/MPH; since 0.22.0, the small left gauge is
again the original turbo/vacuum gauge. The large original NFS scale remains the
tachometer, with an amplified needle.

Since 0.15.2, nitro also reads Valheim's `ZInput.GetMouseButton`; in
flight, W + left Ctrl also works. After closing the menu, release the boost
button briefly before pressing it again. A status line shows activation,
reserve and release lockout; limited diagnostic lines log the input path.
The original tyre texture had already been imported but was heavily darkened.
The 350Z now uses a fresh rubber material with less color multiplication.
The 128×128 rim atlas includes tread and sidewall; these are not HD tyres.

To land, select a ground mode in F9: descend under control, then unfold the
wheels and return to Off-road/Street/Drift. W/S and A/D allow corrections during
the approach; “Fly” cancels landing. F7 and the exit button become available only
after landing. F8 removes the vehicle in flight as well and restores the player.
Flight state belongs to the individual
vehicle and is not saved as a startup mode.

Since 0.13.1, you can drag the F9 menu by its title bar with the left mouse
button held. Its position remains saved after restart; at lower resolutions the
entire menu is moved back onscreen. Closing the menu or losing focus ends the
drag. Categories on the left become available once a vehicle is spawned and in
range; without a car, the garage remains available.

New in 0.13.0: F9 has a side category bar with original NFSU2 icons, selected
German menu labels and original frontend sounds. The layout, actions and mapping
of the three sounds are NFVU2 adaptations; no complete NFS menu flow is emulated.
Up/down arrow keys switch categories. `Menu.SoundVolume` controls navigation
volume (0 = silent). Import: `python tools/export_menu.py`; without media, the
menu remains usable.

F9 → Wheels adds **lowering from 0 to 12 cm** in 0.5 cm steps, saved separately
for the 350Z/H2. It raises the suspension attachment points relative to the car:
the body sits lower and ground clearance decreases. Suspension travel, springs,
dampers and existing drive modes keep their tuning. Reset also restores stock
ride height. On exit, the actual front steering angle is retained; on re-entry,
steering smoothly returns to the input value.

F9 → Turret → **Guard automatically after exiting** is enabled by default once
the turret itself is enabled and the car has been driven. With the laser switch
active, the turret visibly scans a forward 180° sector (±90° relative to the
vehicle's front) and detects hostile creatures up to 30 m away. It aims the
barrel, fires when the line of fire is clear and reloads automatically. Players,
tamed animals, the car body and cover block the line of fire; players, tamed and
peaceful creatures are not selected as targets. The automation is independent of
the running engine/radio and also works on other parked 350Z cars in the garage.
Menus, loss of focus and the local player's death pause it. On entering the car,
the mouse takes control; a held fire button must be released before the first
manual shot. Ammunition and laser settings also apply to automation. Scan motion,
30 m range and target/fire logic are NFVU2 adaptations. The scan sector is based
on the [official Rust Wiki](https://wiki.facepunch.com/rust/Item/autoturret).

| Taste | Funktion |
|---|---|
| F6 | Spawn the selected vehicle in the garage as an additional instance |
| F7 | Enter/exit |
| W / S | Accelerate / brake and reverse |
| Hold W + S | Burnout while stopped; brake first while moving |
| A / D | Steer |
| Space | Brake; rear handbrake in Drift mode |
| Mouse / wheel | Look around / camera distance |
| Middle mouse button | Center camera |
| F9 | Vehicle menu, including drive mode and display/camera |
| Hold left mouse button | Nitro while driving forward with throttle; purge while stopped |
| Hold H | Horn |
| F8 | Remove current or next vehicle within 5 m |


F9 opens the menu at the upper left when
in or near the car. It brakes the car and
releases the mouse; the radio and engine remain audible. Chat, inventory, the
game menu and loss of focus still pause audio. The driving lights turn on when
entering and off when exiting. Neon is initially off; its color and pulse mode
are saved. F9 → Display selects from eleven original speedometer designs, color,
size (65–140 percent), KM/H or MPH, and visibility. The lower-right speedometer
shows actual driving speed, RPM/gear of the acoustic transmission and the actual
mod nitro reserve, including consumption/recharge.
Original graphics are generated
locally in `hud/hud.json`; layout, needle
calibration and NOS fill are adapted for
Valheim. A text display appears if the graphics are missing. F9 → Display also
controls the dynamic chase camera and its follow behavior. It follows turns with
a delay and partially tracks the direction of travel. Mouse look remains free
briefly; after about 2.2 seconds without mouse movement, the camera catches up
while driving. The selected view stays in place when stopped. The middle mouse
button centers it; disabling the feature restores the previous camera behavior.

F9 → Vehicle offers Off-road, Street and Drift. Drift is an additional mod tune
with rear-wheel drive, less rear lateral grip, more steering angle, faster
countersteering and 20 percent more torque than Street. Space applies only the
rear handbrake and interrupts drive; S first brakes while moving forward as
before and engages reverse only when stopped. Smoke and tyre audio respond to
actual slip/ground contact. Menus, loss of focus and exiting still brake all four
wheels. Off-road and Street retain their existing tuning; switching restores it.
The camera and drift feel need in-game tuning on Valheim surfaces.

Under “Lights”, choose from 14 presets in the original NFSU2 language files:
Stock, Xenon 4000K, Red, Blue, Green, Purple, Yellow, Extra Bright White, Xenon
8000K and extra-bright variants of the five colors. The intensity slider ranges
from 0 to 300 percent and affects the front headlight beams and emission.
Settings are saved; “Reset headlights” restores Stock/100 percent. RGB values
and brightness ratios are approximations for Valheim. Rear, brake, reverse and
plate lights remain independent. The white reverse lights in the bumper respond
to selecting reverse: S first brakes while driving forward, and reverse engages
only near a stop. Orange rear strips are visible but do not yet have turn signal
control. Since 0.5.0, the reverse light is substantially dimmed (light intensity
0.45, range 4 m, emission 0.7). When accelerating forward, 20 percent more torque
is available up to about 11 km/h; the boost fades out by about 22 km/h. The first
audible gear is about nine percent shorter.
W+S starts a burnout near a stop: the front brakes hold while the rear wheels
spin. Smoke and original NFS tyre squeal respond to spinning tyres in ground
contact. Releasing the keys, pressing Space, opening a menu, losing focus or
exiting ends the burnout and restores normal grip. Nitro is disabled during it.
The speed-dependent reverse transmission whine is synthesized; no NFS reverse
gear sample has been identified. F9 → Doors opens the left/right doors, hood and
trunk individually while stopped. They close automatically when driving off. The
door mechanism can switch among Normal, Suicide and Scissor; the change takes
effect after closing. Stock/Style 1/Style 2 engine appearance affects only the
model. F9 → Trunk offers the stock trunk or three enclosures with 2/4/6 slots.
Each slot can use a cover, one of four subwoofer designs, one of four amplifiers
and one of four LCDs. Setup 2 with four subwoofers is the default. Parts and
mounting points come from local NFS files; the matching panel size was selected
from the geometry. LCDs use static original textures, not video playback. Trunk
neon has its own switch, seven colors and pulse mode. The light strips, lighting
effect and pulse frequency are mod adjustments. If the radio remains on, music
outside becomes louder and less muffled as a door or trunk opens further. The
trunk has the strongest effect; closing it makes the music quieter and more
muffled again. The sound source follows the open side or rear, or the midpoint
when multiple openings are open. The hood
does not affect the music. Radio volume,
pause and the “Keep playing after exit” switch still apply.
The optional interior import in 0.14.0 adds the cockpit, seats, pedals, gear
shift and door panels from the locally supplied BeamNG mod. The steering wheel
follows steering input. Without the import,
the simplified NFS base model remains.
F9 → Wheels lets you set front/rear camber (−15 to +10 degrees), front/rear
wheel offset (−10 to +20 cm per wheel) and shared rim size (16 to 22 inches,
based on the nominal NFS 20-inch model). Negative camber tilts the top inward;
positive offset moves wheels outward. The tyre sidewall adjusts to the rim, while
the outer tyre diameter remains 70 cm. These settings are visual; wheelbase and
wheel collision remain unchanged by these visual controls. The additional
lowering control moves the suspension attachment points. Brakes follow camber
and offset but retain their size. Changes appear immediately, are saved and can
be reset together.
Nitro works while driving forward with throttle and ground contact; while
stopped, purge jets appear at the bumper and
hood. A full nitro reserve lasts six seconds
and recharges over ten seconds while driving, after a two-second delay from
release. F9 → Nitro toggles the feature and optional lift-off flames. The flames
are initially off; a matching bang sample has not yet been identified. Original
fire/smoke textures and nitro bank recordings are imported; boost, recharge,
particle movement and purge jet positions are mod adjustments. The two nitro-bank
loops are provisionally mapped to boost/purge. Whether and at which tuning stage
NFS automatically triggers throttle-change flames remains unresolved. The plate
uses the fixed original “UNDRGND 2” texture on its own small plate surface. Free
NFS plate text has not been established. Neon pulse, light intensities, plate
position and turbo triggering are NFVU2 adaptations; a complete recreation of
Rachel's original setup remains unresolved.
