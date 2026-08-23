# Stable IDs

Stable IDs are authored string keys used to identify persistent game data across saves, loads, scene changes, and future content migrations. They are not display names, Godot node names, resource paths, or temporary debug labels.

Use stable IDs anywhere the value may be saved, restored, compared across sessions, or referenced by another persistent system.

## Format

Use lowercase dot-separated strings:

```text
category.subcategory.specific_name
```

Allowed characters are lowercase letters, digits, dots, underscores, and hyphens. Avoid spaces, slashes, uppercase letters, localized text, and Godot scene paths.

Good examples:

```text
item.consumable.small_potion
quest.main.intro_find_sap_collector
npc.phosphor.sap_collector
scene.phosphor.port
spawn.phosphor.port.dock_boat
area.phosphor.harbor
flag.chest.phosphor.port.boathouse.opened
```

Avoid:

```text
SmallPotion
res://PackedScenes/EthraV1/Core/TestingGround/LabScene.tscn
PhosphorDockBoat
3001
Start Node
```

## Why This Matters

Save files need durable identity. A display name can change, a scene can move, and a Godot node can be renamed. A stable ID should only change when the underlying authored thing is intentionally replaced and the save migration has been planned.

Stable IDs make these systems safer:

- Save/load metadata and restore targets
- GameStateManager flags, counters, strings, explored areas, and NPC friendship
- Quest and objective progress
- Dialog tree and dialog node references
- Pickups, chests, doors, puzzles, cutscenes, and other one-time world objects
- Shops, loot tables, recipes, skills, and status effects

## Category Conventions

Use these prefixes for new persistent data:

| Type | Prefix | Example |
| --- | --- | --- |
| ItemId | `item.` | `item.material.copper_ore` |
| WeaponId | `item.weapon.` | `item.weapon.rusted_sword` |
| ArmorId | `item.armor.` | `item.armor.traveler_hood` |
| ConsumableId | `item.consumable.` | `item.consumable.small_health_potion` |
| QuestId | `quest.` | `quest.main.intro_find_sap_collector` |
| QuestObjectiveId | `objective.` | `objective.collect_copper_ore` |
| NpcId | `npc.` | `npc.phosphor.sap_collector` |
| EnemyId | `enemy.` | `enemy.slime.green` |
| AreaId | `area.` | `area.phosphor.harbor` |
| SceneId | `scene.` | `scene.phosphor.port` |
| SpawnId | `spawn.` | `spawn.phosphor.port.dock_boat` |
| ChestId | `chest.` | `chest.phosphor.port.boathouse` |
| DoorId | `door.` | `door.phosphor.port.boathouse_front` |
| PuzzleId | `puzzle.` | `puzzle.phosphor.port.lighthouse_lens` |
| CutsceneId | `cutscene.` | `cutscene.main.arrival` |
| DialogTreeId | `dialog.` | `dialog.phosphor.sap_collector.intro` |
| DialogNodeId | `node.` | `node.start` |
| ShopId | `shop.` | `shop.phosphor.port.general_store` |
| LootTableId | `loot.` | `loot.enemy.green_slime` |
| RecipeId | `recipe.` | `recipe.potion.small_health` |
| SkillId | `skill.` | `skill.combat.quick_slash` |
| StatusEffectId | `status.` | `status.poison.light` |
| WorldFlagId | `flag.` | `flag.chest.phosphor.port.boathouse.opened` |

`Core/GameState/StableIds.cs` contains shared prefix constants and simple validation helpers for this convention.

## Save And Game State Integration

GameStateManager owns persistent world-state metadata. Any value that may appear in a save slot or world-state snapshot should come from GameStateManager or a GameStateManager-owned snapshot model.

Store these values as stable IDs when authored IDs exist:

- `CurrentSceneId`
- `CurrentSpawnId`
- `CurrentAreaId`
- `CurrentMainStoryQuestId`
- `ExploredAreaIds`
- `NpcFriendshipScores` keys
- `WorldFlags` keys
- `IntegerValues` keys
- `StringValues` keys

The value of a flag, counter, or string can be primitive state. The key should still be stable. For example, an opened chest should use a flag key such as:

```text
flag.chest.phosphor.port.boathouse.opened
```

## Quest Integration

Quest IDs should use `quest.main.*`, `quest.side.*`, or another `quest.*` subcategory. Objective IDs should be stable within their quest and should not depend on objective display text.

Example:

```text
quest.main.intro_find_sap_collector
objective.speak_to_sap_collector
```

Current debug quest data uses transitional IDs such as `quest_test_collect_item`, and objective IDs such as `collect_copper_ore`. Do not mass rename those while active saves or references may exist. Replace them only through a planned content migration.

## Dialog Integration

Dialog tree IDs should use `dialog.*`. Dialog node IDs should be stable inside a tree, even when the line text changes.

Example:

```text
dialog.phosphor.sap_collector.intro
node.start
node.ask_about_sap
```

Existing dialog data contains debug and legacy IDs such as `interaction_debug_merchant`, `new_dialog_test`, and `Selene_Phosphor`. Treat these as current data, not as examples for new production content.

## World Object Integration

Any object that can change persistent state should have an authored stable ID:

- Pickups and checks
- Chests
- Doors
- Puzzles
- Cutscene triggers
- One-time interactables
- Spawn points

Use GameStateManager flags or counters to record one-time state. For example, a pickup can set:

```text
flag.pickup.phosphor.port.small_health_potion_01.collected
```

On load, the pickup scene should ask GameStateManager for that flag before spawning or enabling interaction.

## Inventory Integration

Core inventory data currently uses numeric IDs in `Core/Inventory/Data/items_seed.csv`. Some older item resources and scenes use PascalCase or underscore IDs such as `Rusty_Short_Sword`.

Do not mass rename these IDs during unrelated work. For future content, prefer stable string item IDs and add any numeric-to-stable mapping through an explicit migration plan.

## Scene And Spawn Integration

Some scene selection currently uses scene file names or registry keys such as `LabScene`, `MainMenu`, and `TestZone`. Some spawn names use exported scene values such as `PhosphorDockBoat`.

For new save-facing scene data, prefer authored IDs such as:

```text
scene.phosphor.port
spawn.phosphor.port.dock_boat
```

Until a scene display-name system exists, `CurrentSceneDisplayName` may use the scene key, path, or name. The display name should not become the persistent identity.

## Adding New IDs

When adding a new persistent thing:

1. Pick the category prefix.
2. Include enough context to avoid collisions.
3. Keep the final segment human-readable.
4. Add the ID to the authored data/resource, not just to code.
5. Use the same ID in GameStateManager state, save data, and cross-system references.

Prefer:

```text
chest.phosphor.port.boathouse
```

Over:

```text
chest.01
```

## Renaming IDs

Renaming a stable ID is a save migration. Do not rename an ID just for style.

If an ID must change:

1. Add a migration map from old ID to new ID.
2. Update save loading to rewrite the old key.
3. Update authored data and references.
4. Test loading a save that contains the old ID.
5. Remove the migration only when old saves are no longer supported.

## Current Repository Notes

Inspection found mixed existing patterns:

- Numeric inventory IDs in `Core/Inventory/Data/items_seed.csv`
- Snake_case debug quest and objective IDs in `Core/Quest/Data`
- Snake_case and PascalCase dialog tree IDs in `Core/Dialog/Data`
- File-name-derived scene keys in scene managers and registries
- PascalCase/underscore legacy item IDs in older item resources and zones
- String-keyed GameStateManager data for flags, counters, strings, explored areas, and friendship

These should be treated as existing data. This document defines the forward-looking convention; it does not require immediate mass migration.

## Future Tooling

Good future validation checks:

- Scan authored data for invalid stable ID characters.
- Warn when a save-facing ID uses a scene path, display name, or Godot node path.
- Detect duplicate stable IDs across content.
- Validate category-specific prefixes.
- Generate a report of legacy IDs that need migration.
- Add editor tooling for world objects that require persistent IDs.
