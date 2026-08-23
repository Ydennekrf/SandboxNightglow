# Progression And Abilities

## Overview

The progression slice supports player XP, level-ups, ability points, enemy XP rewards, quest XP rewards, and a small ability path foundation. The first UI is intentionally list-based inside the existing player menu; it is not a visual sphere grid.

## Experience

Player XP is owned by `PlayerProgression` on the runtime `Player` model.

The XP curve is isolated in `ExperienceCurve`:

```text
ExperienceToNextLevel = 50 + ((Level - 1) * 25)
```

XP carries over after level-up, and a large XP award can grant multiple levels in one call.

## Ability Points

Ability points are also owned by `PlayerProgression`. `SkillPoints` remains as a compatibility alias for existing save fields, but new code should use `AbilityPoints`.

The level-up reward formula is isolated in `ExperienceCurve`:

```text
AbilityPointsGainedOnLevelUp = 1 + (NewLevel / 5)
```

This uses integer division, so levels 2-4 grant 1 point, levels 5-9 grant 2 points, and levels 10-14 grant 3 points.

## Enemy XP

Debug enemies expose `ExperienceReward`. Enemy definitions also carry the reward, and runtime `Enemy` models now have level/reward progression fields.

`TestEnemyNode.EnterDeadState()` awards XP once because `_defeated` guards repeated defeat processing. The current debug flow logs:

```text
[Progression] Enemy defeated: enemy.debug.test_enemy
[Progression] Awarded XP: 25
[Progression] XP: current / required
```

## Quest XP

Quest definitions support `experienceReward`. When `QuestManager` completes a quest, it awards that XP to the current player and publishes a notification.

The two debug quest JSON files currently grant 50 XP each.

## Ability Path Data

Ability paths are JSON definitions loaded by `MasterRepository` from:

```text
res://Core/Progression/Data
```

The default player path is:

```text
abilitypath.player.debug
```

Nodes connect through stable `connectedNodeIds`. The player starts with only the starting node unlocked. A node can be unlocked only when it is adjacent to an already unlocked node and the player has enough ability points.

## Node Types

Supported node types:

- `Start`
- `StatIncrease`
- `ActiveAbility`
- `PassiveAbility`

The first example path includes +5 Max Health, Dodge active unlock, +3 Max Mana, third-hit combo knockback passive unlock, and +1 Strength.

## Ability Effects

Effects are reusable data objects and are not UI-only logic. Current effect types:

- `StatModifier`
- `UnlockActiveAbility`
- `UnlockPassiveAbility`

Stat modifiers currently apply directly to the player stats. Armor and trinket equipment use a small source-tracked modifier layer on the player so equipment bonuses can be removed cleanly without duplicating on repeated equip/load; see `docs/equipment-armor-trinkets.md`. Active and passive effects store stable IDs on `AbilityPathState`:

```text
ability.player.dodge
passive.combo.third_hit_knockback
```

Weapons, armor, consumables, and buffs can reuse the same `AbilityEffectDefinition` shape later.

Combat status effects now have a reusable runtime foundation in `CombatManager` and stable definitions under `Core/Combat/Scripts/StatusEffectDefinition.cs`. Future active/passive ability unlocks should add stable `status.*` IDs to weapon attack packets or call `CombatManager.ApplyStatus()` rather than duplicating status behavior in progression UI code. See `docs/status-effects.md`.

## Save And Load

`PlayerSnapshot` stores:

- level
- current XP
- total XP
- ability points through the existing `SkillPoints` field
- ability path id
- current node id
- unlocked node ids
- unlocked active ability ids
- unlocked passive ability ids

Stats are already saved in the player stat snapshot. On load, the ability path restore rebuilds unlocked IDs and UI state without reapplying stat node effects, avoiding double stat gains after loading.

## Adding A Node

1. Edit or add a JSON file under `Core/Progression/Data`.
2. Add a unique lowercase stable `nodeId`.
3. Add reciprocal `connectedNodeIds` for neighboring nodes.
4. Set `cost`, `nodeType`, and `effects`.
5. Build and open the Level Up menu to verify the node appears when connected.

## Adding Active Or Passive Unlocks

Use effect types:

```json
{ "effectType": "UnlockActiveAbility", "abilityId": "ability.player.dodge" }
{ "effectType": "UnlockPassiveAbility", "passiveId": "passive.combo.third_hit_knockback" }
```

Combat and movement systems should check the player's unlocked ID collections before enabling full behavior.

## Testing

Build:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

Manual menu check:

- start or load a game
- gain enough XP to level
- open the player menu
- press `Level Up`
- select an available connected node
- unlock it
- confirm ability points decrease and the node's effect is reflected in player state

## Current Limitations

- The Level Up screen is a first-pass list UI, not a visual grid.
- Stat effects apply directly to player stats; a fuller stat modifier layer is still future work.
- Dodge is stored as an unlocked active ability ID, but full dodge behavior is not implemented here.
- Third-hit knockback is integrated as `passive.combo.third_hit_knockback`; `CombatManager` applies `status.knockback` when a player hit resolves on combo phase 3.
- Status effect runtime exists for combat, but ability path nodes do not yet author status applications directly.
- Quest runtime state still follows the existing save/load limitation.

## Future Follow-Ups

- visual sphere-grid style UI
- full stat modifier system
- dodge active ability implementation
- combo passive integration
- weapon/armor ability effects
- quest reward balancing
- enemy level scaling
- skill tree respec
- controller support
- save/load migration
