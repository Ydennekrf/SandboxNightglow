# Project Map

## Project Identity

- Engine: Godot 4.6 stable, .NET/Mono build
- Language: C#
- Project type: Top-down 2D RPG
- Operating system for local development: Windows 11
- Preferred local scripts: PowerShell

## Local Paths

- Godot executable: `E:\GameProject\Godot\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe`
- Project root: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox`
- Solution file: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox\ParksSandbox.sln`
- C# project file: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox\ParksSandbox.csproj`

## Main Scenes

- Main startup scene: `res://PackedScenes/EthraV1/Core/UI/MasterNode.tscn`
- Main player scene: `res://PackedScenes/EthraV1/Core/Entities/Selene.tscn`
- Packed scene root: `res://PackedScenes/EthraV1/Core/`
- GameManager autoload scene: `res://PackedScenes/EthraV1/Core/UI/game_manager.tscn`

## Debug Scenes

These helper scripts assume these debug scene paths.

- Combat debug scene: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`
- Movement debug scene: `res://PackedScenes/EthraV1/Core/Debug/MovementDebugScene.tscn`
- Interaction debug scene: `res://PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn`
- Crafting debug scene: `res://PackedScenes/EthraV1/Core/Debug/CraftingDebugScene.tscn`
- Map-layer debug scene: `res://PackedScenes/EthraV1/Core/Debug/MapLayerDebugScene.tscn`

Debug scenes should use the project autoload `GameManager` and should not instance their own `GameManager` nodes. See `docs/debug-scene-architecture.md`.

## Important Code Areas

- GameManager: `res://Core/Managers/GameManager.cs`
- Debug scene architecture: `res://docs/debug-scene-architecture.md`
- FSM script: `res://Core/Entity/Scripts/StateMachine/StateMachine.cs`
- FSM folder: `res://Core/Entity/Scripts/StateMachine/`
- Combat folder: `res://Core/Combat/`
- Inventory folder: `res://Core/Inventory/`
- Dialog folder: `res://Core/Dialog/`
- Entity folder: `res://Core/Entity/`
- Primary node script folder: `res://Core/Nodes/`
- Crafting folder: `res://Core/Crafting/`
- Progression/ability path folder: `res://Core/Progression/`
- Armor/trinket equipment notes: `res://docs/equipment-armor-trinkets.md`
- Quest folder: `res://Core/Quest/`
- World helpers folder: `res://Core/World/`
- Event payload folder: `res://GameEvents/`
- Readability pass notes: `res://docs/code-readability-pass.md`

## Local Debugging Flow

1. Run `powershell -ExecutionPolicy Bypass -File ./tools/godot-doctor.ps1` if setup is uncertain.
2. Run `powershell -ExecutionPolicy Bypass -File ./tools/build.ps1`.
3. Inspect relevant files.
4. Make the smallest task-related change.
5. Run `powershell -ExecutionPolicy Bypass -File ./tools/build.ps1` again.
6. Run a debug scene only when the relevant scene exists.
7. Stop and summarize exact commands, files changed, and remaining manual checks.

## Known Build Baseline

A PowerShell `dotnet build` from the project root has succeeded against the solution, but currently reports warnings. Existing warnings should not be treated as task failures unless they are directly related to the requested change.
