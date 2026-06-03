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

## Planned Debug Scenes

These helper scripts assume these debug scene paths. They do not need to exist immediately.

- Combat debug scene: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`
- Movement debug scene: `res://PackedScenes/EthraV1/Core/Debug/MovementDebugScene.tscn`

## Important Code Areas

- GameManager: `res://Core/Managers/GameManager.cs`
- FSM script: `res://Core/Entity/Scripts/StateMachine/StateMachine.cs`
- FSM folder: `res://Core/Entity/Scripts/StateMachine/`
- Combat folder: `res://Core/Combat/`
- Inventory folder: `res://Core/Inventory/`
- Dialog folder: `res://Core/Dialog/`
- Entity folder: `res://Core/Entity/`
- Primary node script folder: `res://Core/Nodes/`

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
