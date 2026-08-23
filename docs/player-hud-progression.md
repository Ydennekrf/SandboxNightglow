# Player HUD And Progression

## HUD Display

The player HUD shows:

- health as `HP: current / max`
- mana as `MP: current / max`
- current level as `LV N`
- experience as `XP: current / required`
- an XP progress bar

## Files

- HUD script: `res://Core/UI/Scripts/PlayerHud.cs`
- Main HUD scene wiring: `res://PackedScenes/EthraV1/Core/UI/MasterNode.tscn`
- Combat debug HUD wiring: `res://PackedScenes/EthraV1/Core/Debug/CombatDebugScene.tscn`
- Progression model: `res://Core/Entity/Scripts/PlayerProgression.cs`
- XP curve: `res://Core/Entity/Scripts/ExperienceCurve.cs`

## HUD Data Flow

`UIManager.Initialize()` receives the current `UIRoot`, resolves `UIRoot.PlayerHud`, and binds it to `GameManager.Instance.GameState.GetPlayer()`.

`PlayerHud` only displays state. It subscribes to:

- `Player.HealthChanged`
- `Player.ManaChanged`
- `Player.Progression.ExperienceChanged`
- `Player.Progression.LevelChanged`
- `Player.Progression.LeveledUp`

The HUD does not own health, mana, XP, level, rewards, saves, or combat logic.

## Health And Mana Refresh

Health and mana live on `CombatEntity` through `IStats`. `CurHP` and `CurMana` remain delta-based setters, matching the existing combat/save behavior. The setters now emit display events after values change, and `InitializeStats()` emits initial health/mana events.

HUD code should read `CurHP`, `MaxHP`, `CurMana`, and `MaxMana`. Do not assign absolute health or mana through `CurHP` or `CurMana` unless a delta was intentionally calculated.

## XP Gain

Player XP is owned by `PlayerProgression`, exposed through `Player.Progression`.

Use:

```csharp
player.GainExperience(amount);
```

The method ignores non-positive amounts, adds current and total XP, emits refresh events, and writes debug output like:

```text
[Progression] Gained XP: 25
[Progression] XP: 25 / 50
```

## Level-Up

When current XP reaches the required amount, `PlayerProgression`:

- subtracts the current requirement
- carries overflow XP
- increases level
- grants 1 skill point
- recalculates the next requirement
- emits level and XP events

The HUD refreshes automatically. `PlayerHud` publishes a level-up notification when the notification system is present.

## XP Curve

The curve lives in `ExperienceCurve.GetExperienceToNextLevel(int level)`.

Current formula:

```text
ExperienceToNextLevel = 50 + ((Level - 1) * 25)
```

This keeps level 1 to 2 at 50 XP and isolates balance changes in one place.

## Enemy XP Rewards

`EnemyDefinitionResource` and `TestEnemyNode` expose `ExperienceReward`.

`TestEnemyNode` records the last player source from `CombatFeedbackBus.HitResolved`. When `EnterDeadState()` runs, its `_defeated` guard ensures reward logic runs once, then the player receives XP.

If no player source was recorded, the node falls back to `GameManager.Instance.GameState.GetPlayer()`.

## Save/Load

`PlayerSnapshot` now stores:

- `Level`
- `CurrentExperience`
- `TotalExperience`
- `SkillPoints`

`SaveLoadService.CreatePlayerSnapshot()` captures progression data. `GameManager.ApplyPlayerSnapshot()` restores it through `Player.Progression.RestoreSnapshot()`.

Old saves without progression fields load as level 1 with 0 XP.

## Manual Testing

Build:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

Full game:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-game.ps1
```

Combat debug:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-combat-debug.ps1
```

In `CombatDebugScene`, press:

- `X` to grant debug XP directly
- `H` to damage the nearest test enemy
- `K` to defeat the nearest test enemy

Expected checks:

- HUD appears during gameplay/debug scenes.
- Main menu hides the HUD.
- HP and MP display current/max values.
- XP increases when pressing `X` or defeating a rewarded enemy.
- Level increases when enough XP is earned.
- Overflow XP remains after level-up.
- Save/load restores level and XP.

## Current Limitations

- HUD visuals are functional placeholders, not final art.
- HP/MP/XP bars are not animated.
- Skill points are stored but no skill tree UI exists yet.
- Level-up does not increase stats yet.
- Enemy XP balancing is only a first-pass value.
- Quest, crafting, and other XP reward sources are not implemented yet.
- Defeated enemy persistence is still outside this slice.

## Future Follow-Ups

- final HUD art
- animated HP, MP, and XP bars
- skill point spending
- skill tree UI
- stat growth on level-up
- enemy XP balancing
- quest XP rewards
- crafting XP
- save/load polish and migration tests
