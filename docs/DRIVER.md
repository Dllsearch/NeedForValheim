# Visible local driver

The car uses the actual local Valheim character, including its current body,
hair and worn equipment. It does not clone a Player, change inventory or transfer
network ownership. Held weapons are hidden while driving and restored on exit.
The character's original scale, animator parameters and renderer settings are
restored on exit or interrupted entry.

Entry walks around the nearest end of the vehicle to the physical left door,
checks the ground and character clearance, turns into the doorway and blends the
native chair pose into the seat. Exit reverses that sequence. Driving remains
blocked until the character has sat down. Menu/focus interruptions pause entry
and exit. Existing animated 350Z doors remain synchronized with the transition;
cars without separate door animation use a timed character transition.

Both hands follow the moving steering wheel while driving. The right hand reaches
the gear lever when the existing engine/audio transmission changes gear, and the
handbrake when that control is held. The corresponding pedals and feet move with
accelerator, braking and shift input. These are procedural visual gestures; they
do not replace the drivetrain or add manual transmission.

The separate 350Z interior retains its measured seat and steering coordinates and
idle arm-on-window pose. Other U2/MW cars retain their imported cockpit/exterior;
small generated steering, pedal and lever meshes provide moving controls. Seat
placement is fitted from the original referenced glazing vertices. This keeps
the seat in the actual front cab on service trucks rather than the centre of the
whole vehicle. Existing U2 glazing is made transparent without altering opaque
body materials; MW retains its separate glass/tint system.

These fitted controls and character poses are NFVU2 adaptations, not original
NFS driver animations or newly imported full interiors. All 76 shipped models
have geometry/clearance/transition/control checks in
`python tools/check_cabin_fit.py`. Those managed checks do not prove the final
avatar pose, equipment clipping or animation timing in the Unity player.
