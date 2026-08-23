# Crafting System

## What It Does

The crafting system lets a player interact with a crafting bench, inspect recipes, compare required materials against the player inventory, and craft items when enough materials are available.

Crafting rules are reusable and live outside debug scenes. The flow is:

```text
CraftingPanel -> CraftingManager -> InventoryManager + MasterRepository
```

## Player Flow

1. Walk near a crafting bench.
2. The prompt shows `Press E to Craft`.
3. Press the `Interact` action.
4. The crafting panel opens.
5. Select a recipe to view the result item, result quantity, description, and required materials.
6. Available recipes are marked `[Can Craft]` and tinted green.
7. Unavailable recipes are marked `[Missing Materials]` and tinted grey.
8. Press `Craft` on an available recipe to remove materials, add the result item, refresh the UI, and show a notification.

## Recipe Data Location

Recipe resources live in:

```text
res://Core/Crafting/Data/
```

`GameManager.CraftingRecipeDataFolderPath` defaults to that folder, and `CraftingManager` loads `.tres` and `.res` recipes from it.

## Recipe Resource Model

Recipes use `res://Core/Crafting/Scripts/CraftingRecipe.cs`.

Fields:

- `RecipeId`
- `DisplayName`
- `Category`
- `ResultItemId`
- `ResultQuantity`
- `RequiredMaterials`
- `RequiredStationType`
- `IsUnlocked`
- `SortOrder`
- `Description`

Ingredients use `res://Core/Crafting/Scripts/CraftingIngredient.cs`.

Fields:

- `ItemId`
- `Quantity`

## Creating A New Recipe

Create a new `.tres` resource using `CraftingRecipe`.

Use a stable-style recipe ID:

```text
recipe.category.item_name
```

Examples:

```text
recipe.consumable.small_health_potion
recipe.weapon.iron_longsword
recipe.armor.leather_cap
```

Set `RequiredStationType` to `Workbench` for the current crafting bench.

## Result Items

`ResultItemId` must be an existing item ID from:

```text
res://Core/Inventory/Data/items_seed.csv
```

Current example recipes use:

- `1001` Small Health Potion
- `2002` Iron Longsword
- `2004` Ember Scepter
- `2005` Stonebreaker Maul
- `2006` Gale Knife
- `4001` Leather Cap

## Required Materials

Each required material is a `CraftingIngredient` entry with an existing `ItemId` and a positive `Quantity`.

Current example recipes use:

- `3001` Copper Ore
- `3106` Bitterthorn
- `3108` Silverbloom
- `3121` Greenwood
- `3122` Ironbark
- `3123` Ashwood
- `3124` Moonpine
- `3125` Copper Vein Ore
- `3126` Iron Ore
- `3127` Silver Ore
- `3128` Starsteel Ore
- `3129` Emberstone Ore

The authored debug combo weapon recipes are:

- `recipe.weapon.ember_scepter`: 3 Copper Ore -> Ember Scepter
- `recipe.weapon.stonebreaker_maul`: 4 Copper Ore -> Stonebreaker Maul
- `recipe.weapon.gale_knife`: 2 Copper Ore -> Gale Knife

The expanded weapon combo recipes are:

- `recipe.weapon.ashen_shortblade`: 1 Greenwood, 2 Copper Vein Ore -> Ashen Shortblade
- `recipe.weapon.dawnsteel_saber`: 1 Ironbark, 3 Iron Ore, 1 Bitterthorn -> Dawnsteel Saber
- `recipe.weapon.oathcarver`: 2 Ashwood, 3 Silver Ore, 1 Silverbloom -> Oathcarver
- `recipe.weapon.starfall_edge`: 2 Moonpine, 3 Starsteel Ore, 1 Emberstone Ore -> Starfall Edge
- `recipe.weapon.stonebud_cudgel`: 1 Greenwood, 2 Copper Vein Ore -> Stonebud Cudgel
- `recipe.weapon.ironbell_mace`: 1 Ironbark, 3 Iron Ore, 1 Bitterthorn -> Ironbell Mace
- `recipe.weapon.saintbreaker`: 2 Ashwood, 3 Silver Ore, 1 Silverbloom -> Saintbreaker
- `recipe.weapon.dawnforge_maul`: 2 Moonpine, 3 Starsteel Ore, 1 Emberstone Ore -> Dawnforge Maul
- `recipe.weapon.greenbark_hatchet`: 1 Greenwood, 2 Copper Vein Ore -> Greenbark Hatchet
- `recipe.weapon.embercleave_axe`: 1 Ironbark, 3 Iron Ore, 1 Bitterthorn -> Embercleave Axe
- `recipe.weapon.moonfang_cleaver`: 2 Ashwood, 3 Silver Ore, 1 Silverbloom -> Moonfang Cleaver
- `recipe.weapon.stormroot_reaver`: 2 Moonpine, 3 Starsteel Ore, 1 Emberstone Ore -> Stormroot Reaver
- `recipe.weapon.twin_reed_blades`: 1 Greenwood, 2 Copper Vein Ore -> Twin Reed Blades
- `recipe.weapon.silverwind_pair`: 1 Ironbark, 3 Iron Ore, 1 Bitterthorn -> Silverwind Pair
- `recipe.weapon.duskpetal_fangs`: 2 Ashwood, 3 Silver Ore, 1 Silverbloom -> Duskpetal Fangs
- `recipe.weapon.eclipse_twinblades`: 2 Moonpine, 3 Starsteel Ore, 1 Emberstone Ore -> Eclipse Twinblades

## Item IDs And MasterRepository

The current inventory data uses numeric item IDs. `GameManager.GetAllItems()` loads `items_seed.csv` into `MasterRepository`, and crafting uses that repository for item validation, display names, and icons.

The crafting UI does not parse CSV files.

## CraftingManager And InventoryManager

`CraftingManager` is initialized by `GameManager` after `InventoryManager` and `MasterRepository`.

It uses `InventoryManager` to:

- read item counts
- check required quantities
- remove required materials
- add crafted results
- respect stack limits

It uses `MasterRepository` to:

- validate recipe item IDs
- resolve item display names
- resolve item icons

## Crafting Bench Configuration

Reusable bench scene:

```text
res://PackedScenes/EthraV1/Core/Interactables/CraftingBench.tscn
```

Script:

```text
res://Core/Nodes/Interactable/CraftingBenchInteractable.cs
```

Important exports:

- `StationType`
- `InteractionPromptText`
- `InteractionPriority`

The bench only detects interaction and asks `UIManager` to open crafting for its station type.

## Crafting UI

Reusable panel scene:

```text
res://PackedScenes/EthraV1/Core/UI/CraftingPanel.tscn
```

Script:

```text
res://Core/UI/Scripts/CraftingPanel.cs
```

The panel asks `CraftingManager` for recipes at the active station. It marks rows available or unavailable based on `CraftingManager.CanCraft(...)`, shows owned and required material counts, and calls `CraftingManager.Craft(...)` when the craft button is pressed.

## Running The Debug Scene

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-crafting-debug.ps1
```

Debug scene:

```text
res://PackedScenes/EthraV1/Core/Debug/CraftingDebugScene.tscn
```

The debug root script only spawns the player, initializes scene UI, seeds Copper Ore, and prints:

```text
[CraftingDebug] Crafting debug scene ready.
```

## Manual Test Checklist

- Build with `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`.
- Launch the crafting debug scene.
- Walk to the crafting bench.
- Confirm the prompt says `Press E to Craft`.
- Interact with the bench.
- Confirm the crafting panel opens.
- Select a recipe.
- Confirm result item, quantity, description, and required materials appear.
- Confirm owned and required material counts are shown.
- Confirm craftable recipes are green and marked `[Can Craft]`.
- Confirm unavailable recipes are grey and marked `[Missing Materials]`.
- Craft Small Health Potion.
- Confirm Copper Ore decreases.
- Confirm Small Health Potion increases.
- Confirm the UI refreshes.
- Confirm a notification appears.
- Close the crafting panel.

## Current Limitations

- Recipe unlocks default to `IsUnlocked = true`.
- Recipe data uses numeric item IDs because current inventory CSV data uses numeric IDs.
- The first-pass UI is functional and not final art.
- Crafting is instant.
- Batch crafting is not implemented.
- Controller-specific navigation is not implemented.

## Future Follow-Up Tasks

- recipe unlocks through GameStateManager
- crafting skill tree integration
- crafting stations by type
- batch crafting
- crafting time/progress bar
- recipe discovery
- crafting quality
- sound/animation polish
- save/load recipe unlocks
- controller support
