# Code Readability Pass

## Purpose

This pass adds intent-focused XML summaries and boundary comments to high-value runtime systems without changing gameplay behavior. The goal is to make manager ownership, authored data, runtime state, event payloads, and debug harness responsibilities easier to understand from the code itself.

## Systems Documented

- Core manager orchestration: `GameManager`, `MasterRepository`, `SceneManager`, `UIManager`
- Gameplay domain managers: `CombatManager`, `InventoryManager`, `CraftingManager`, `WeaponUpgradeManager`, `DialogManager`, `QuestManager`, `GameStateManager`, `EntityManager`
- Static/runtime data models: crafting recipes and ingredients, status effect definitions, weapon instance state, ability path definitions/state, quest definitions, world state DTOs
- Event payloads: dialog, floating text, notifications, interaction prompts, and world-state events
- Debug harness roots: combat, interaction, crafting, and map-layer debug scene roots
- Editor-facing interactables: harvestable item configuration

## Major Classes That Received Summaries

- `Core/Managers/GameManager.cs`
- `Core/Managers/MasterRepository.cs`
- `Core/Managers/SceneManager.cs`
- `Core/Managers/UIManager.cs`
- `Core/Managers/CombatManager.cs`
- `Core/Managers/InventoryManager.cs`
- `Core/Managers/CraftingManager.cs`
- `Core/Managers/WeaponUpgradeManager.cs`
- `Core/Managers/DialogManager.cs`
- `Core/Managers/GameStateManager.cs`
- `Core/Managers/EntityManager.cs`
- `Core/Quest/Scripts/QuestManager.cs`

## Exported And Editor-Facing Fields Clarified

- `GameManager` repository paths, player ability path ID, scene folder, item CSVs, crafting recipe folder, and movement speed export.
- `CraftingRecipe` and `CraftingIngredient` exported resource fields.
- `HarvestableInteractable` stable ID, item grant, harvest duration, animation/tool fields, prompt fields, and visual paths.
- Debug roots now state that they are harnesses and not owners of reusable gameplay logic.

## Confusing Areas Found

- `GameManager` still acts as both autoload orchestrator and compatibility facade for several interfaces. This is documented, but broad reshaping was intentionally avoided.
- `MasterRepository` mixes several loader concerns in one file. It is understandable after summaries, but a future split into loader helpers may make sense.
- `GameStateManager` keeps legacy NPC friendship shapes synchronized with newer dictionaries. The compatibility reason is now documented.
- Debug roots contain useful setup logic and some duplicated test scaffolding. This pass labels them as debug-only rather than moving logic.

## Stale Comments Removed Or Replaced

- Replaced older comments in `GameManager`, `MasterRepository`, `SceneManager`, `InventoryManager`, `GameStateManager`, and `EntityManager` with XML summaries that explain ownership/lifecycle.
- Removed a stale inventory note in `InventoryManager.UseItem`.
- Reworded scene-transition comments to clarify that world content is replaced while UI remains intact.

## Files Intentionally Not Touched

- Imported assets, `.import`, `.godot`, binary assets, generated files, and large scene/resource rewrites.
- Legacy top-level `Managers/` and older `components/` runtime paths, except event/data files that were safe to document.
- Large FSM action/transition files beyond the systems already documented. They deserve a focused pass because state behavior is gameplay-sensitive.
- Dialog graph editor tool internals. It is editor tooling with a large surface area and should be documented separately.

## Risky Cleanup Candidates Left For Later

- Decide whether `GameManager` should keep forwarding all legacy interfaces or whether callers can move to dedicated managers directly.
- Split `MasterRepository` CSV/JSON/resource loading into small loader classes once repository contracts stabilize.
- Audit unused private fields reported by the compiler, such as `_playerRef` in `SceneManager` and `_npcData` in `GameStateManager`, before removal.
- Continue retiring legacy `Managers/` and older `components/` code only after active scenes no longer depend on them.
- Consolidate repeated debug-scene player spawning/stat setup into a shared debug helper if more debug scenes are added.

## Recommended Future Documentation Pass

1. Add focused summaries to FSM actions/transitions after confirming state-machine behavior is stable.
2. Document UI panel ownership and event consumption in `Core/UI/Scripts`.
3. Add authoring notes for combat payloads, combo resources, and magic shape previews.
4. Expand `docs/project-map.md` when legacy manager/components paths are formally retired.
