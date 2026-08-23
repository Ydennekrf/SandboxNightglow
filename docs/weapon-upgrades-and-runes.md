# Weapon Upgrades And Runes

## Overview

Weapon socketing is owned by reusable inventory and manager code, not by debug scenes or UI-only scripts. The current flow is:

```text
CraftingPanel -> WeaponUpgradeManager -> InventoryManager + MasterRepository
```

`WeaponUpgradeManager` validates socketing rules, consumes rune items, mutates a weapon instance, and exposes summaries for UI. `InventoryManager` owns the persistent instance state.

## Unique Weapon Instances

Socketed weapons are per-item instances. Two copies of the same weapon item ID can now have different socket state.

Stackable items still use item counts. Weapon items create `WeaponInstanceState` records when added to inventory. Saves persist those records through `InventorySave.WeaponInstances`, including:

- `InstanceId`
- `ItemId`
- socketed upgrade rune item IDs
- socketed elemental rune item ID

Old saves that only contain weapon item counts are backfilled into empty weapon instances during restore.

## Item Data

Rune and socket metadata lives in:

```text
res://Core/Inventory/Data/items_seed.csv
res://Core/Inventory/Data/item_effects_seed.csv
```

The live inventory data currently uses numeric IDs. New first-pass IDs follow the existing numeric ranges:

- `2003` Debug Socket Sword
- `5001` Minor Strength Rune
- `5002` Minor Knockback Rune
- `5003` Minor Vitality Rune
- `5101` Fire Bolt Rune
- `5102` Fire Cone Rune
- `5103` Fire Line Rune
- `5104` Fire Circle Wave Rune
- `5111` Ice Bolt Rune
- `5121` Electricity Line Rune
- `5131` Acid Cone Rune
- `5141` Darkness Circle Wave Rune

The string stable-ID convention in `docs/stable-ids.md` should be used when the item database is migrated away from numeric IDs.

## Weapons

Weapons define socket capacity with optional CSV columns:

- `upgrade_rune_slots`
- `has_elemental_rune_slot`

Existing weapons default to zero upgrade slots and no elemental slot, so old weapons remain compatible and show safe empty-state text in the upgrade UI.

Weapons also define their attack combo through `combo_profile_path`. The path points to a `WeaponComboResource`; if it is empty or invalid, player attacks fall back to `res://Core/Combat/Data/Combos/basic_attack_combo.tres`. `Debug Socket Sword` (`2003`) currently uses `res://Core/Combat/Data/Combos/debug_sword_combo.tres` to demonstrate a three-hit chain. See `docs/combat-combos.md`.

## Runes

Rune rows use category `Rune` and these optional CSV columns:

- `rune_kind`: `Upgrade` or `Elemental`
- `rune_element`: `Electricity`, `Ice`, `Fire`, `Acid`, `Darkness`
- `magic_shape`: `ProjectileBolt`, `Linear`, `Cone`, `CircleWaveAwayFromPlayer`

Upgrade rune stat effects are attached through `item_effects_seed.csv`, using the same effect loader as other items.

## Socketing Rules

Socketing currently:

- consumes one rune item from inventory
- prevents upgrade runes from entering elemental slots
- prevents elemental runes from entering upgrade slots
- prevents socketing into occupied slots
- does not yet support unsocketing or replacement

The derived modifier summary is display-only for now. Upgrade rune effects are stored as item effects and can be applied to combat/stat systems in a later pass. On-hit weapon and rune status effects are folded into cloned attack payloads during combo step resolution, so combo attacks and future rune modifiers share the same status pipeline.

## Crafting Table UI

`CraftingPanel` adds a runtime `Weapon Upgrade` tab beside the existing crafting view. It lists weapon instances, shows selected weapon slots, lists available runes, sockets selected runes, and exposes a `Preview Magic Shape` button for elemental runes.

## Magic Shape Preview

`MagicShapePreview` calculates affected grid cells. `MagicAreaHighlighter` renders temporary colored squares for:

- `ProjectileBolt`
- `Linear`
- `Cone`
- `CircleWaveAwayFromPlayer`

Element colors are mapped in `MagicShapePreview.GetElementColor`.

## InteractionDebugScene Test

`InteractionDebugScene` now includes the reusable crafting bench and crafting panel. Its root script seeds debug-only materials and runes:

- Copper Ore for crafting `Debug Socket Sword`
- one upgrade rune set
- one elemental rune for each required element

Manual flow:

1. Run `powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1`.
2. Interact with the crafting bench.
3. Craft `Debug Socket Sword`.
4. Open `Weapon Upgrade`.
5. Select the crafted weapon instance.
6. Socket an upgrade rune.
7. Socket an elemental rune.
8. Press `Preview Magic Shape`.

## Current Limitations

- Rune IDs are numeric because the active CSV item database is numeric.
- Upgrade rune effects are summarized but not yet folded into final combat damage.
- Elemental previews do not deal magic damage.
- Unsocketing and replacing runes are not implemented.
- Armor rune slots are not implemented.

## Future Work

- unsocketing and replacement
- weapon upgrade costs
- magic damage application
- real projectile/cone/area attacks
- elemental status effects
- armor rune slots
- rune rarity and crafting recipes
- visual effects and animations
- migration from numeric item IDs to stable string item IDs

Armor and trinket equip effects now use source-tracked player modifiers, including `magic_range_bonus`; see `docs/equipment-armor-trinkets.md` for the current equipment effect path.
