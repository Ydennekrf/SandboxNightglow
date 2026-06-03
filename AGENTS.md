# AGENTS.md

## Purpose

This repository contains a **Godot 4.6** game project using **C#** as the primary scripting language.

Agents working in this repository should prioritize:
- safe, minimal, reviewable changes
- preserving the existing architecture
- maintaining low coupling between systems
- respecting manager-based orchestration and explicit state-driven behavior
- avoiding speculative refactors unless explicitly requested

The project favors **clear structure, practical maintainability, and consistency with the existing codebase** over clever shortcuts.

---

## Tech Stack

- **Engine:** Godot 4.6
- **Language:** C#
- **Project Type:** Top-down 2D RPG
- **Architecture Style:** Manager-based orchestration, event-driven communication, modular FSM/state composition, repository-based lookup patterns

---

## Core Architectural Expectations

Agents should assume the project follows these principles unless the task explicitly says otherwise:

### 1. Preserve the existing architecture
Do not pivot the project into a completely different pattern.

Prefer working **with** the current design:
- manager-driven initialization
- event-based communication where already in use
- modular FSM/state composition
- repository/registry lookup patterns
- pure data where appropriate
- scene instances kept as lightweight as practical

Do **not** introduce a new framework or radically different architecture unless explicitly asked.

### 2. Prefer composition over monolithic logic
When extending gameplay systems:
- prefer small focused classes over giant multi-purpose classes
- prefer adding actions/transitions/components over hardcoding one-off behavior into a single script
- avoid tightly coupling unrelated systems

### 3. Keep scene logic lean
Godot scene scripts should generally coordinate behavior, not become large dumping grounds for business logic.

Prefer:
- lightweight scene/node scripts
- reusable actions/services/managers
- data-driven configuration where already supported

### 4. Keep changes grounded and local
When fixing a bug or adding a feature:
- change the smallest sensible surface area
- avoid unrelated cleanups
- avoid broad renames unless necessary
- preserve public APIs unless the task requires changing them

### 5. Plan Node Default
 - Enter Plan mode for ANY non-trivial task (3+ steps or architectural decisions)
 -if something goes sideways, STOP and re-plan immediately - dont keep pushing
 - Use plan mode for verification steps, not just building
 - Write detailed steps up from to reduce ambiguity

### 6. Subagent Strategy
 - Use subagents liberally to keep main context window clean
 - Offload research, exploration, and parallel analysis to subagents
 - For complex problems, throw more compute at it via subagents
 - One tack per subagent for focused execution

### 7. Self-improvment loop
 - Write rules for yourself that prevent the same mistake
 - Ruthlessly iterate on these lessons until the mistake rate drops
 - Review lessons at start for relevant project

### 8. Verification before done
 - Ask yourself would a staff engineer approve this?
 - Run tests check logs, demonstrate correctness
### 9. Demand Elegance (Balanced)
 - for non trivial challenges: pause and ask "is there a more elegant way?"
 - if a fix feels hacky: "knowing everything i know now, implement the elegant solution"
 - Skip this for simple, chivious fixes - dont over-engineer
 - Challenge your own work before presenting it

### Core Principals
1. **Simplicity first**: make every change as simple as possible. Impact minimal code.
2. **No Laziness**: Find root causes. No Temporary fixes. Senior Developer Standards

---

## Files and Areas Safe to Edit

Agents may usually edit:

- `*.cs`
- `*.tscn`
- `*.tres`
- `*.res`
- `project.godot`
- JSON, YAML, Markdown, and other text config/data files
- documentation files
- editor plugin scripts and supporting text assets

Agents may also inspect:
- folder structure
- scene references
- exported properties
- resource references
- project configuration

---

## Files and Areas to Avoid

Do **not** modify these unless explicitly asked:

- `.godot/`
- `.import/`
- imported asset metadata unless required for a specific fix
- binary assets such as textures, audio, fonts, models, and compiled outputs
- solution/project-generated files if the change is unrelated
- large serialized editor-generated files unrelated to the task

Avoid deleting or regenerating files just to “clean things up.”

---

## Godot-Specific Guidance

## Scene files (`.tscn`)
Godot scene files are text-based and may be edited when the requested change is structural and clear.

Good candidates:
- adding/removing a node
- updating node names
- adjusting node paths
- fixing script references
- correcting resource paths
- wiring exported NodePaths or references
- standardizing collision/nav setup

Use extra caution with:
- large scene rewrites
- visual layout changes
- animation timing/polish
- inspector-heavy tuning with unclear intent

When editing scenes:
- preserve stable node names unless renaming is required
- do not reorder unrelated sections unnecessarily
- make diffs as small as possible
- update all code references when node names or paths change

## Resource files (`.tres`, `.res`)
These may be edited for:
- state definitions
- action resources
- transition resources
- gameplay data
- reusable configuration

Do not rewrite resource structures unless necessary for the task.

## Project configuration
Changes to `project.godot` should be:
- intentional
- minimal
- clearly explained in the summary

Do not casually alter input maps, autoloads, display settings, physics layers, or project-wide defaults without a concrete reason.

---

## C# Coding Guidance

## General style
Prefer:
- explicit, readable code
- small focused methods
- descriptive naming
- minimal side effects
- straightforward control flow

Avoid:
- unnecessarily clever abstractions
- hidden global state
- giant utility classes
- broad speculative refactors

## Null safety and runtime stability
This project is sensitive to initialization order and scene wiring.

When editing C#:
- guard against nulls where scene references may not yet exist
- be mindful of `_Ready()`, initialization, autoload timing, and scene load order
- do not assume nodes or managers are initialized unless the code guarantees it
- prefer defensive checks when interacting with scene tree objects

## Godot + C# specifics
When writing or editing Godot C# code:
- use the Godot 4 API style consistently with the surrounding code
- respect node lifecycle methods
- avoid using APIs from older Godot versions
- keep node lookups stable and explicit
- prefer exported references or clearly managed lookups over brittle hardcoded assumptions where possible

---

## Project-Specific Preferences

Agents should assume the following repo preferences:

### 1. Manager-based orchestration is intentional
There may be systems such as:
- GameManager
- SceneManager
- InventoryManager
- UIManager
- DialogManager
- CombatManager
- repository/registry services

Do not remove this style in favor of a different orchestration model unless explicitly instructed.

### 2. FSM/state systems are a core design choice
The project may use:
- base states
- state actions
- transitions
- runtime-added modifiers
- equipment-driven behavior changes

When working in the FSM:
- preserve modularity
- prefer adding targeted actions/transitions over hardcoding state-specific branches everywhere
- do not collapse resource-driven or compositional systems into a single giant state script

### 3. Data and scene separation matters
Pure data and scene behavior should remain reasonably separated.

Prefer:
- data objects for save/state/configuration
- scene scripts for runtime coordination
- managers/services for shared orchestration

### 4. Repo-wide consistency matters more than isolated elegance
A “better” solution that conflicts with the rest of the repo is usually not better.

Match:
- existing naming
- existing folder organization
- existing architectural conventions
- existing dependency direction

---

## When Working on Bugs

When fixing bugs, agents should:

1. Identify the smallest likely cause first.
2. Check for initialization order issues.
3. Check for broken node paths, renamed nodes, or missing scene references.
4. Check for Godot inspector/export mismatches.
5. Check for null references caused by scene setup assumptions.
6. Prefer a precise fix over a broad rewrite.

If multiple causes are plausible, choose the most grounded one and explain the reasoning briefly.

---

## When Working on Refactors

Refactors must be conservative unless the user explicitly requests a larger rewrite.

Allowed refactor patterns:
- extracting a small helper method
- clarifying duplicated logic
- reducing obvious coupling in a local area
- renaming for clarity when all references are updated safely

Avoid:
- sweeping folder reorganizations
- mass renames
- architecture swaps
- replacing stable systems just because another pattern is popular

---

## When Working on Scenes and Code Together

Many tasks in this repo will require coordinated changes between:
- `*.tscn`
- `*.tres`
- `*.cs`

When changing one side, always check the others.

Examples:
- renamed node -> update all relevant C# lookups and exported paths
- changed state resource -> check consuming FSM code
- changed manager/service API -> update scene scripts that call it
- altered spawn/setup behavior -> verify scene structure still matches the code assumptions

---

## Validation Expectations

After making changes, agents should validate as much as possible.

Preferred validation:
1. compile/build C# if available
2. run project-specific tests if present
3. inspect for broken references
4. grep for stale names/paths after renames
5. summarize any validation that could not be performed

If full validation is not possible, say so clearly.

---

## Change Summary Expectations

After edits, provide a concise summary including:
- what changed
- why it changed
- which files were touched
- any assumptions made
- any follow-up manual checks recommended in the Godot editor

For scene/resource edits, include a brief note on:
- nodes added/removed/renamed
- paths or references updated
- anything the user should verify visually in the editor

---

## What Not to Do

Do not:
- invent requirements not stated by the user
- rewrite working systems unnecessarily
- introduce unrelated formatting churn
- touch broad areas of the repo without reason
- silently change architecture direction
- assume visual scene/layout changes are correct without verification
- overwrite user-authored design choices just because another approach exists

---

## Good Task Examples

Good tasks for agents in this repo:
- fix broken node path references after a scene rename
- add a new FSM action class for a specific behavior
- wire a new child node into an enemy scene and connect it in C#
- standardize exported references across related scripts
- fix null reference issues caused by initialization order
- update save/load wiring for a new data field
- patch resource paths after folder moves
- add small editor tooling for existing workflows

Less suitable unless explicitly requested:
- redesigning combat architecture
- replacing managers with a different pattern
- rebuilding UI layout by guesswork
- reauthoring animation content
- changing project-wide scene organization

---

## Instructions for Agents

When in doubt:
- ask what the smallest safe change is
- preserve the current architecture
- keep edits minimal
- make diffs easy to review
- explain assumptions
- leave visual polish for editor verification

If a task involves Godot scenes, you are allowed to help with scene/resource files **as text**, not only C# code.
However, stay conservative and make structural edits only when the intent is clear.

---

## Local Godot/C# Debugging Setup

This repository includes helper scripts under `tools/`.

Use these scripts instead of inventing new commands:

- `./tools/godot-doctor.ps1`
  - Checks local Godot, .NET, solution, project, and configured scene paths.

- `./tools/build.ps1`
  - Builds the C# solution at `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox\ParksSandbox.sln`.

- `./tools/run-game.ps1`
  - Launches the full Godot project from `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox`.

- `./tools/run-test-scene.ps1`
  - Launches a specific Godot scene.
  - Default scene: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`.

- `./tools/run-combat-debug.ps1`
  - Launches the combat debug scene.

- `./tools/run-movement-debug.ps1`
  - Launches the movement debug scene.

### Debugging rules

- Always run `./tools/build.ps1` before attempting runtime debugging.
- If the build fails, fix compile errors before runtime issues.
- Prefer small debug scenes over the full game when testing focused systems.
- If an error appears unrelated to the current task, report it and stop.
- Do not make broad architecture changes just to make a local test pass.
- Do not add new packages.
- Temporary debug logs are allowed while investigating, but remove noisy logs before finalizing unless they are behind a clear debug flag.
- Do not claim a feature works unless it was built or run successfully.
- Include the exact command run and relevant error output in the final summary.

## Local Project Paths

- Godot executable: `E:\GameProject\Godot\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe`
- Project root: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox`
- Solution file: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox\ParksSandbox.sln`
- C# project file: `E:\GameProject\Godot-Projects\ParksSandbox\parkssandbox\ParksSandbox.csproj`
- Main startup scene: `res://PackedScenes/EthraV1/Core/UI/MasterNode.tscn`
- Main player scene: `res://PackedScenes/EthraV1/Core/Entities/Selene.tscn`

## Codex Environment Expectations

Codex may:

- create helper scripts
- create debug scenes when requested
- inspect scenes, resources, and project configuration
- add temporary logs during debugging

Codex should ask first before modifying existing production scenes unless the user clearly requests the scene change.

Codex must not:

- introduce GDScript
- add packages without approval
- rewrite architecture without approval
- treat pre-existing warnings as failures unless directly related to the task
