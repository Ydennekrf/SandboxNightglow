# Item Seed Editor

## Purpose

The Item Seed Editor is a dockable Godot editor plugin for viewing, editing, validating, and saving item seed CSV files under:

```text
res://Core/Inventory/Data/
```

It is editor tooling only. Runtime item loading still goes through `MasterRepository.FillCsvRepo`, `GameManager`, and `InventoryManager`.

## Enable And Open

The plugin lives at:

```text
res://addons/item_seed_editor/plugin.cfg
```

It is enabled in `project.godot`. If the dock is hidden, open `Project > Project Settings > Plugins` and enable `Item Seed Editor`, then look for the `Item Seed Editor` dock on the right side of the editor.

## CSV Files

The dock discovers `.csv` files directly under `res://Core/Inventory/Data/`.

Current files:

- `items_seed.csv`: item definitions keyed by `id`
- `item_effects_seed.csv`: stat effects keyed by `item_id`

`items_seed.csv` currently uses these columns:

```text
id,name,description,rarity,sell_value,category,subtype,max_stack,icon_path,weapon_up_draw_path,weapon_down_draw_path,weapon_up_stow_path,weapon_down_stow_path,combo_profile_path,upgrade_rune_slots,has_elemental_rune_slot,rune_kind,rune_element,magic_shape
```

`item_effects_seed.csv` currently uses these columns:

```text
item_id,effect_type,effect_stat,effect_power,status_id,status_trigger,status_chance,status_duration,status_stacks
```

Headers and column order are preserved. Unknown columns are loaded, displayed, edited as text, and written back.

## List View

The list shows loaded rows with:

- item id (`id` or `item_id`)
- name when a `name` column exists
- type/category from `category`, `type`, `item_type`, or `effect_type`
- source CSV filename

The search box filters by id, name, category/type, or source file.

Rows with unsaved edits are prefixed with `*`.

## Editing Items

The right side of the dock is split into tabs:

- `New Item` for guided item creation
- `Selected Item` for raw CSV editing plus type helpers
- `Validation` for errors and warnings

Select a row to show one editor field per CSV column in `Selected Item`. Labels match the CSV headers.

Common fields get light editor assistance:

- descriptions use a multi-line field
- boolean-style headers such as `has_elemental_rune_slot` use a checkbox
- category, rarity, rune kind, rune element, and magic shape use dropdowns when values are known
- all other fields use text input

Edits update in-memory CSV data immediately. They are not written to disk until saving.

Path fields ending in `_path` include a `Browse` button that opens a Godot resource file picker and writes the selected `res://` path back into the field.

## Add Item

Use the `New Item` tab for normal item creation.

1. Choose an item type: `Weapon`, `Armor`, `Consumable`, `Material`, `Rune`, or `Elemental Rune`.
2. Fill in the common fields: id, name, rarity, sell value, subtype, stack size, icon path, and description.
3. Fill in the type-specific fields that appear for the selected item type.
4. Add optional stat or status effects.
5. Press `Save Item To List`.

The editor adds the item row to `items_seed.csv` in memory and adds any drafted effects to `item_effects_seed.csv` in memory. Use `Save All` to write the CSV files to disk.

The id field can be edited manually, but `Suggest Next Id` picks a numeric id based on the selected type:

- consumables start near `1001`
- weapons start near `2001`
- materials/crafting items start near `3001`
- armor starts near `4001`
- runes start near `5001`
- elemental runes start near `5101`

The toolbar `Add Raw Row` button is still available for low-level CSV editing.

Choose a target CSV from the toolbar dropdown, then press `Add Raw Row`.

The editor creates a new row using that file's existing headers. If the file has numeric ids, the editor suggests the next numeric id in that CSV.

If no numeric id can be inferred, the placeholder id is:

```text
item.new_item
```

The current runtime item table still expects numeric ids for item definitions, so change non-numeric placeholder ids before relying on the row in gameplay.

## Duplicate Item

Select a row and press `Duplicate`.

The editor copies the selected row into the same source CSV and changes its id to the next numeric id when possible. If no numeric id can be inferred, it falls back to a copied placeholder.

## Delete Item

Select a row and press `Delete`.

The editor asks for confirmation and warns that deleting item ids can break recipes, inventories, equipment, saves, and scene references. Deleting a row only marks it for removal in memory; saving writes the removal to disk.

The tool does not auto-delete recipes, effects, or other related references.

## Saving

Use:

- `Save Current File` to save the selected row's CSV, or the toolbar target CSV if no row is selected
- `Save All` to save every dirty CSV
- `Reload` to discard unsaved edits and read from disk

Before saving, the editor shows a summary of new, edited, and deleted rows per CSV file.

Saving preserves:

- headers
- header order
- row values for untouched rows
- unknown/custom columns

CSV values are quoted and escaped when needed.

## Backups

Before overwriting a CSV, the editor creates a backup under:

```text
res://Core/Inventory/Data/backups/
```

Backup names include the original filename and a timestamp, for example:

```text
items_seed.20260616_153000.bak.csv
```

## Validation

Press `Validate` to scan loaded CSV rows.

Validation reports errors and warnings in the dock. Duplicate or missing ids are errors. Warnings do not block saving.

Validation results are shown in a selectable table. Selecting a result jumps the main editor selection to the affected row.

Checks include:

- missing `id` or `item_id`
- duplicate ids across loaded CSV data
- non-numeric ids with invalid stable-id characters
- missing name/display name where a name column exists
- missing category on item rows
- invalid numeric values
- missing or invalid resource paths in known path columns
- missing icon paths
- weapon rows missing weapon draw/stow/combo paths
- armor rows missing subtype/equipment slot
- consumable rows missing subtype/effect family
- crafting/material rows missing subtype/material family
- rune rows missing `rune_kind`
- elemental rune rows with missing or unsupported element/shape

Supported elemental rune elements:

- `Electricity`
- `Ice`
- `Fire`
- `Acid`
- `Darkness`

Supported elemental rune shapes:

- `ProjectileBolt`
- `Linear`
- `Cone`
- `CircleWaveAwayFromPlayer`

## Helper Panels

The helper panel does not replace the raw column editor. It summarizes important fields for the selected row.

Weapon helper fields:

- `icon_path`
- `weapon_up_draw_path`
- `weapon_down_draw_path`
- `weapon_up_stow_path`
- `weapon_down_stow_path`
- `combo_profile_path`
- `upgrade_rune_slots`
- `has_elemental_rune_slot`

Armor helper fields:

- `icon_path`
- `subtype`

Consumable helper fields:

- `icon_path`
- `subtype`

Material/crafting helper fields:

- `icon_path`
- `subtype`
- `rarity`
- `max_stack`

Rune helper fields:

- `icon_path`
- `rune_kind`
- `rune_element`
- `magic_shape`

Elemental rune rows also show a simple color preview.

## Item Effects Panel

When an item definition row from `items_seed.csv` is selected, the helper area also shows matching rows from `item_effects_seed.csv`.

The effects panel supports:

- add effect row for the selected item
- duplicate an existing effect row
- mark an effect row for deletion
- read a plain-language summary of each effect
- edit `effect_type` with a dropdown
- edit `effect_stat` with a dropdown
- edit `effect_power` with a numeric field
- edit status id, trigger, chance, duration, and stacks for `status` rows

This uses the runtime-supported effect schema:

```text
item_id,effect_type,effect_stat,effect_power,status_id,status_trigger,status_chance,status_duration,status_stacks
```

Supported effect types in the editor:

- `plus`
- `minus`
- `status`

Supported stat dropdown values in the editor:

- `maxhp`
- `maxmana`
- `str`
- `dex`
- `int`
- `spi`
- `vit`
- `luk`
- `knockback`

Supported status IDs:

- `status.knockback`
- `status.stun`
- `status.armor_break`
- `status.cold`
- `status.burn`
- `status.poison`
- `status.mana_burn`
- `status.silence`
- `status.blind`
- `status.thorns`

Supported status triggers:

- `on_hit`: weapon and elemental rune status effect is added to the attack payload and applies to hit targets.
- `on_equip`: weapon or armor status effect applies to the owner when equipped and is removed on unequip.
- `on_use`: consumable status effect applies to the owner when used.

`status_chance` is a decimal from `0` to `1`. It is respected for owner-triggered statuses and for whether an on-hit status is added to an attack payload.

`status_duration` overrides catalog duration for `on_equip` and `on_use`. On-hit payloads currently use the attack payload/catalog duration path.

`status_stacks` controls owner-triggered stacks. On-hit payload status application currently applies one stack through the existing combat payload path.

Temporary runtime validation is enabled by `DebugValidateItemStatusEffectsOnStartup` in `GameManager`. It logs loaded item status effect counts after item CSV loading. Remove that flag/call after manual testing.

## Resource Paths

Path fields are edited as text and include a `Browse` button. Validation checks whether known resource paths exist. Icon paths get a small texture preview when Godot can load the texture.

## MasterRepository Relationship

The editor does not create a runtime item registry and does not bypass `MasterRepository`.

Runtime item loading remains:

1. `GameManager` calls `MasterRepository.FillCsvRepo` for `items_seed.csv`.
2. `GameManager` calls `MasterRepository.FillCsvRepo` for `item_effects_seed.csv`.
3. `InventoryManager` reads item definitions from `MasterRepository`.

There is no direct editor-time repository reload button yet. After saving CSV changes, restart or reload the project/game when runtime item data needs to be refreshed.

## Known Limitations

- no undo/redo integration
- no schema editor
- no bulk edit
- no recipe cross-reference validation yet
- no weapon animation or paperdoll preview
- no rune effect preview
- no direct `MasterRepository` reload
- placeholder ids are string-based, while current runtime item ids are numeric
- on-hit status effects use the existing attack payload status path, so per-status duration/stacks are not individually represented yet

## Future Follow-Up Tasks

- resource picker polish
- CSV schema editor
- bulk edit
- import/export templates
- recipe cross-reference validation
- weapon animation preview
- paperdoll preview
- rune effect preview
- direct MasterRepository reload
- undo/redo support
