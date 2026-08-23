# Item Seed Expansion

## Summary

This pass expands inventory and crafting seed data with additional weapons, armor, trinkets, consumables, crafting materials, quest items, and crafting bench recipes. It preserves the current numeric item ID CSV schema and adds stable-style recipe IDs through `CraftingRecipe` resources.

## Files Changed

- `Core/Inventory/Data/items_seed.csv`
- `Core/Inventory/Data/item_effects_seed.csv`
- `Core/Crafting/Data/*.tres`
- `tools/validate-item-seed-data.ps1`
- `docs/item-seed-expansion.md`

## Weapons Added

Four tiers were added for each family: Sword, Mace, Axe, and Twin Blade. The new numeric ID ranges are:

- `2101-2104`: swords
- `2111-2114`: maces
- `2121-2124`: axes
- `2131-2134`: twin blades

Weapon rows use existing weapon sprite paths, existing combo profile resources, `upgrade_rune_slots`, and `has_elemental_rune_slot`. Base damage is not a CSV field, so this seed pass represents weapon tier strength with supported `plus str` item effects and existing combo profile behavior. Tier 3 and Tier 4 weapons also include supported on-hit status hooks where appropriate.

Follow-up combo authoring added one item-specific combo profile for each new weapon under `Core/Combat/Data/Combos`. Tier 1 and Tier 2 profiles establish family-specific melee patterns, Tier 3 profiles add stronger/status-capable finishers, and Tier 4 profiles make the final phase an elemental magic payload while keeping a melee animation name.

## Armor Added

Three tiers were added for each requested damage type: Physical, Electricity, Ice, Fire, Acid, and Darkness. The new armor ID range is `4201-4218`. Armor rows use category `Armor` and subtype strings that encode element and tier, such as `Armor:Fire:Tier2`.

## Trinkets Added

Ten trinkets were added in the `4401-4410` range. Each trinket has one supported stat effect and one future-integration modifier effect. Runtime trinket equipment now uses `TrinketItem`, `Trinket1`, and `Trinket2`; see `docs/equipment-armor-trinkets.md`.

## Consumables Added

Consumables were added in the `1101-1131` range: four health potion tiers, four mana potion tiers, seven stat draughts, and one experience tonic. Stat draughts use supported `plus` effects for current stats where available. Health restore, mana restore, Defense, and XP grant are represented as future-integration effect stat labels because the current consumable effect system does not implement those behaviors.

## Materials Added

Thirty crafting materials were added in the `3101-3130` range: 20 herbs, 4 woods, and 6 ores. These use category `Crafting`, existing stack behavior, and material subtypes `Herb`, `Wood`, and `Ore`.

## Quest Items Added

Ten future gather quest items were added in the `6101-6110` range with category `Quest` and subtype `Gather`. They have no recipes and no gameplay effects.

## Recipe Categories Added

Recipes were added for every new weapon, armor, trinket, and consumable. They use `RequiredStationType = "Workbench"`, stable-style `RecipeId` values, newly seeded materials as ingredients, and scaled quantities by tier.

## Stable ID Naming Patterns

Current runtime item IDs remain numeric. New item ranges were chosen to avoid existing IDs and keep categories grouped. Recipe IDs follow `recipe.weapon.*`, `recipe.armor.*`, `recipe.trinket.*`, and `recipe.consumable.*`.

## Schema Limitations Found

- `items_seed.csv` does not have a string stable `ItemId` column.
- `items_seed.csv` does not have base damage, tier, armor defense, armor resistance, trinket slot, consumable heal amount, mana restore amount, or XP grant columns.
- `WeaponItem` currently treats all weapons as `MainHand`; the authored CSV subtype still records family but is not exposed by `WeaponItem` at runtime.
- Armor is equippable, but there is no typed resistance system.
- Some trinket effect stat keys are still future-facing and are tracked without direct gameplay bindings.

## Future Integration Effect Labels

These effect stat labels are intentionally seeded for future systems and currently load without gameplay behavior: `heal_hp`, `restore_mana`, `grant_xp`, `defense`, `resist_physical`, `resist_electricity`, `resist_ice`, `resist_fire`, `resist_acid`, `resist_darkness`, `knockback_bonus`, `magic_range_bonus`, `dodge_distance_bonus`, `healing_received_bonus`, `mana_regen_bonus`, `physical_damage_reduction`, `item_find_bonus`, `mana_cost_reduction`, `combo_finisher_damage`, and `experience_gain_bonus`.

## Manual Test Checklist

- Build with `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`.
- Validate seed data with `powershell -ExecutionPolicy Bypass -File .\tools\validate-item-seed-data.ps1`.
- Launch the game with `powershell -ExecutionPolicy Bypass -File .\tools\run-game.ps1`.
- Launch crafting debug with `powershell -ExecutionPolicy Bypass -File .\tools\run-crafting-debug.ps1`.
- Launch interaction debug with `powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1`.
- Confirm `MasterRepository` loads the expanded item CSV without duplicate ID errors.
- Confirm `InventoryManager` can add at least one new weapon, armor, trinket, and consumable through a debug inventory path.
- Confirm `CraftingManager` loads the expanded recipe set without skipped recipes.
- Confirm the crafting panel can show at least one new recipe at a workbench.
- Confirm no CSV parsing errors occur.

## Future Follow-Up Tasks

- loot table integration
- chest drops
- enemy drops
- shop inventories
- harvestable material distribution
- balance pass
- recipe unlocks
- item icon assignment
- equipment stat/effect integration
- trinket equipment slot integration
- armor resistance integration
- consumable healing, mana, and XP effect integration
- quest item usage in gather quests
