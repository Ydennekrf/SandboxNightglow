# Equipment Armor Trinkets

## Overview

Armor and trinket equipment is owned by `InventoryManager`, with static item metadata loaded through `MasterRepository` from `Core/Inventory/Data/items_seed.csv` and `Core/Inventory/Data/item_effects_seed.csv`.

The inventory UI displays equipment and requests equip/unequip actions. It does not apply equipment effects directly.

## Equipment Slots

Current slots:

- `Armor`
- `Trinket1`
- `Trinket2`

The current UI exposes one visual armor slot and two visual trinket slots. Existing broader armor subtype data, such as `Head` or `Chest`, remains valid item metadata, but this first pass maps armor into the single visual `Armor` slot.

## Item Definitions

Armor rows use category `Armor`. Trinket rows use category `Trinket`.

Example debug rows:

- `4301` Debug Leather Vest: category `Armor`, subtype `Armor`
- `4421` Debug Focus Ring: category `Trinket`, subtype `Ring`

The active item database is numeric, so these debug examples use numeric IDs. Future migration can add string stable IDs such as `item.armor.debug_leather_vest`.

## Equipment Effects

Equipment effects use the existing item effect CSV shape:

```csv
item_id,effect_type,effect_stat,effect_power
4301,plus,str,1
4421,plus,magic_range_bonus,1
```

Supported first-pass equipment modifiers:

- core stats through `PlusStat` and `MinusStat`
- `magic_range_bonus`

Unknown effect stat keys are tracked but do not directly change a player stat yet.

## Strength +1

`Debug Leather Vest` has `plus,str,1`. When equipped, `InventoryManager` applies the item effects to the player using a source ID for that slot and item. Strength increases by 1. Unequipping removes the same source and decreases Strength by 1.

## MagicRange +1

`Debug Focus Ring` has `plus,magic_range_bonus,1`. The player exposes `MagicRangeBonus`, and magic shape calculation reads that value through `MagicShapePreview`, `MagicAreaHighlighter`, and `CombatManager` target filtering.

Current behavior:

- projectile and linear previews extend by one cell per bonus
- cone depth extends by one row per bonus
- circle wave radius extends by one cell per bonus

## Duplicate Stacking Prevention

Equipment modifiers are stored on the player by source ID:

```text
equipment:Trinket1:4421
```

Before applying a source, the player removes any prior modifiers from that same source. Unequipping removes the source once. This prevents repeated equip/unequip from permanently stacking stats.

## Inventory UI Flow

The player menu:

- shows inventory items in the backpack grid
- shows weapon, armor, trinket 1, and trinket 2 visual equipment slots
- selects armor/trinkets without immediately toggling them
- enables `Equip` for selected backpack armor/trinkets
- enables `Unequip` for selected equipped armor/trinkets
- shows item details, effects, equipped slot, and current Magic Range Bonus

Equipped armor/trinket copies are subtracted from the backpack display count, so they visually move into equipment slots.

Weapon click-to-equip behavior is preserved.

## Save And Load

`InventorySave` stores:

- base inventory stacks
- equipped armor slots
- equipped trinket slots
- equipped weapon slots and weapon instance slots

On restore, `InventoryManager` clears runtime equipment effects, restores inventory data, restores equipped slots, and reapplies armor/trinket effects once.

## Adding Armor

1. Add an `Armor` row to `items_seed.csv`.
2. Use subtype `Armor` for the current single armor slot.
3. Add optional effect rows to `item_effects_seed.csv`.
4. Build and test equip/unequip from the player menu.

## Adding Trinkets

1. Add a `Trinket` row to `items_seed.csv`.
2. Use a descriptive subtype such as `Ring`, `Charm`, or `Locket`.
3. Add optional effect rows to `item_effects_seed.csv`.
4. Build and test both trinket slots from the player menu.

## Manual Test Checklist

- Run `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`.
- Run `powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1`.
- Open the inventory panel.
- Confirm armor and two trinket slots are visible.
- Select `Debug Leather Vest`, press `Equip`, and confirm Strength increases by 1.
- Select the equipped armor slot, press `Unequip`, and confirm Strength returns to its prior value.
- Select `Debug Focus Ring`, press `Equip`, and confirm Magic Range Bonus is 1.
- Preview a magic shape and confirm its range increases.
- Unequip the trinket and confirm Magic Range Bonus returns to 0.
- Repeatedly equip/unequip and confirm bonuses do not duplicate.
- Confirm weapon equipment still works.
- Save/load with equipment and confirm equipment restores once without duplicate items.

## Current Limitations

- Armor is represented by one visual slot, even though some item subtypes still describe specific body areas.
- Trinket slot selection is automatic: first open slot, then `Trinket1` replacement when both are occupied.
- There is no drag/drop equipment yet.
- Equipment comparison and stat preview before equipping are not implemented.
- Unknown effect stats are tracked but do not yet bind to gameplay systems.
- The item database still uses numeric IDs.

## Future Follow-Ups

- armor-specific slots such as head/chest/legs
- trinket rarity
- equipment comparison UI
- drag/drop equipment
- armor rune slots
- resistance stats
- passive ability equipment effects
- full magic attack integration
- equipment stat preview before equipping
