# UI Feedback Systems

## Overview

The project now has three reusable HUD feedback systems:

- Interaction Prompt UI: shows the active interactable prompt, such as `Press E to Talk`.
- Floating Text: spawns short text near a world target or world position, such as damage numbers or pickup messages.
- Notifications/Toasts: shows short HUD messages for game/system events, such as quest, save, loot, and error messages.

These systems are intentionally small and event-driven so dialog, combat, loot, quests, save/load, crafting, skill trees, and scene transitions can request UI feedback without each system creating one-off labels.

## Files

- Prompt metadata: `components/Interact/Interfaces/IInteractionPromptSource.cs`
- Interactable prompt defaults: `components/Interact/Interfaces/IInteractable.cs`
- Events/payloads:
  - `GameEvents/InteractionPromptChanged.cs`
  - `GameEvents/FloatingTextRequest.cs`
  - `GameEvents/NotificationRequest.cs`
- HUD scripts:
  - `Core/UI/Scripts/InteractionPromptView.cs`
  - `Core/UI/Scripts/FloatingTextManager.cs`
  - `Core/UI/Scripts/FloatingTextLabel.cs`
  - `Core/UI/Scripts/NotificationManager.cs`
- HUD scene wiring:
  - `PackedScenes/EthraV1/Core/UI/MasterNode.tscn`
  - `PackedScenes/EthraV1/Core/Debug/InteractionDebugScene.tscn`
  - `PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`

## Interaction Prompt UI

Interactables expose prompt metadata through `IInteractionPromptSource`:

```csharp
[Export] public string InteractionVerb { get; set; } = "Open";
[Export] public string InteractionPromptText { get; set; } = "Press E to Open";
[Export] public int InteractionPriority { get; set; } = 0;
public bool CanInteract => !_opened;
```

`IInteractable` extends this prompt source interface, so the existing `InteractComponent` can choose one active prompt from nearby candidates without hardcoding text in the player controller.

The generic `InteractComponent` chooses the best target by:

1. Ignoring candidates where `CanInteract` is false.
2. Preferring the highest `InteractionPriority`.
3. Falling back to nearest distance when priorities match.

When the selected target changes, it publishes:

```csharp
GameEvent.InteractionPromptChanged
InteractionPromptChanged(promptText, sourceNode)
```

Ethra debug/world interactables that currently do their own range detection also publish the same event directly. This keeps the change local and avoids rewriting interaction architecture.

Prefer publishing through `GameManager.Instance` so debug scenes and normal gameplay scenes use the same autoload event bus. Legacy callers may still fall back to `EventManager.I` where that older manager stack exists.

## Floating Text

Use `GameEvent.FloatingTextRequested` with a `FloatingTextRequest`.

Examples:

```csharp
GameManager.Instance?.Publish(
	GameEvent.FloatingTextRequested,
	FloatingTextRequest.AtTarget("12", targetNode, FloatingTextType.Damage));

GameManager.Instance?.Publish(
	GameEvent.FloatingTextRequested,
	new FloatingTextRequest
	{
		Text = "Found: Test Loot",
		WorldTarget = playerNode,
		Type = FloatingTextType.Pickup,
		DurationSeconds = 1.1f
	});
```

`FloatingTextManager` resolves a world target through `GetGlobalTransformWithCanvas()` so text follows the active camera/canvas transform. It supports default colors for `Damage`, `Healing`, `Pickup`, `Debug`, and `Generic`.

Damage numbers should use floating text through `HurtBox.ShowDamagePopup`, which now emits a reusable floating text request while preserving the existing `DamagePopupShown` signal.

Pickup messages should publish a pickup floating text request above the player when possible, or use a world position fallback if no player node is available.

## Notifications

Use `GameEvent.NotificationRequested` with `NotificationRequest`.

Examples:

```csharp
GameManager.Instance?.Publish(
	GameEvent.NotificationRequested,
	new NotificationRequest("Game saved.", NotificationType.Save));

GameManager.Instance?.Publish(
	GameEvent.NotificationRequested,
	new NotificationRequest("Not enough gold.", NotificationType.Error));
```

`NotificationManager` also listens to the older `GameEvent.ToastMessage` string event and displays it as an info notification, so existing toast-style callers continue to work.

Supported categories:

- `Info`
- `Quest`
- `Save`
- `Error`
- `Loot`

Messages stack up to the configured visible limit and auto-fade.

## Debug Scenes

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-interaction-debug.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-combat-debug.ps1
```

`InteractionDebugScene` demonstrates:

- NPC prompt: `Press E to Talk`
- Chest prompt: `Press E to Open`
- Button prompt: `Press E to Activate`
- Chest pickup floating text above the player
- Chest loot notification
- Button activation notification and floating text

Debug scenes should include HUD feedback views when the test needs visible UI feedback, but gameplay systems should publish events rather than creating one-off labels in the scene root.

`CombatDebugScene` demonstrates:

- Damage numbers via `HurtBox.ShowDamagePopup`
- A notification when the debug shortcut defeats an enemy

## Manual Test Checklist

Interaction prompt:

- Start `InteractionDebugScene`.
- Walk near the NPC and confirm the prompt appears.
- Walk away and confirm the prompt hides.
- Walk near the chest and confirm the prompt text changes.
- Walk near the debug button and confirm the activation prompt appears.
- Stand near multiple interactables if possible and confirm one selected prompt appears.

Floating text:

- Open the chest and confirm pickup text appears near/above the player and disappears.
- Hit or debug-damage an enemy in `CombatDebugScene`.
- Confirm damage text appears near the target and disappears.

Notifications:

- Open the debug chest and confirm the loot notification appears and auto-hides.
- Activate the debug button and confirm its notification appears.
- Defeat an enemy with the combat debug shortcut and confirm the defeat notification appears.

## Current Limitations

- Prompt glyphs are hardcoded as text (`Press E`) for now.
- Visual styling is functional only; no final art pass has been done.
- The prompt system supports priority and nearest selection, but existing Ethra debug/world interactables that self-detect player range publish directly rather than sharing one detector.
- Notifications stack with simple auto-fade behavior; there is no message history UI.
- Floating text uses simple rise/fade behavior and default type colors.

## Future Follow-up Tasks

- Polish visuals.
- Add icons.
- Add controller glyphs.
- Add localization-ready prompt strings.
- Refine notification queue styling.
- Add accessibility options.
- Tune UI animation timing.
- Add audio cues.
- Add color/category theming.
