# Buildable garage and prototype prices

Use the native **Hammer → Crafting → NFS Garage**. The station costs **10 Wood**
and creates a service bay with a workbench, roof and marked parking floor.
Use **E** at the bench to open the existing garage, call cars onto its bay and
customize them. The ordinary inventory/F9 garage remains available. Once the
station menu is closed, normal F6 spawns follow the player's view again.

The station uses the native Piece, WearNTear and ZNetView lifecycle: it is saved
with the world, takes normal building wear and can be removed with the hammer.
Cars remain temporary; ownership, tuning rights, named builds and the saved
garage roster can recall them after reloading a world.

The **My garage → Prototype economy** checkbox controls test prices:

- Buy each vehicle model once for **1 Coin**; recalling/removing an owned car is
  free. The starter 350Z and the first existing character's saved cars are retained.
- Each new performance level in each of the six systems costs **1 Coin**. Jumping
  from stock to Stage 3 costs 3 Coins for that system; a complete six-system Stage 3
  package costs 18. Unsupported NOS is excluded.
- Downgrading, resetting to stock and reselecting purchased levels cost nothing.
  Named builds use the same purchased levels and cannot bypass prices.
- Ownership and purchased levels are stored per character in the existing plugin
  configuration. The first existing character retains current tuning.
- Failed/blocked/cancelled loading does not charge. A failed purchase save restores
  ownership/levels and refunds the withdrawn coins. Disabling the prototype prices
  restores the existing sandbox controls.

Map markers follow live cars and are not written into the map save. Use
**Lights → Map / car surface effects** for map pins, ground-sensitive dust, gradual
road grime and rain/water sheen. These effects affect the car's materials only.

Placement, hover, fall-history reset, parked-terrain hold, wallet progression,
map lifecycle and surface rules have targeted managed checks. Native station
construction/persistence, coin inventory interaction, avatar animation and final
graphics still need acceptance in a player-controlled game session.
