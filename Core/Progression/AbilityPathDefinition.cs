using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    /// <summary>
    /// Authoring type for nodes in a player ability path graph.
    /// </summary>
    public enum AbilityPathNodeType
    {
        Start,
        StatIncrease,
        ActiveAbility,
        PassiveAbility
    }

    /// <summary>
    /// Type of effect applied when an ability path node is unlocked.
    /// </summary>
    public enum AbilityEffectType
    {
        StatModifier,
        UnlockActiveAbility,
        UnlockPassiveAbility
    }

    /// <summary>
    /// Player stat targeted by a stat-modifier ability effect.
    /// </summary>
    public enum AbilityTargetStat
    {
        None,
        MaxHealth,
        MaxMana,
        Strength,
        Dexterity,
        Intelligence,
        Spirit,
        Vitality,
        Luck
    }

    /// <summary>
    /// Static ability path graph loaded from JSON by MasterRepository.
    /// </summary>
    /// <remarks>
    /// PathId, StartingNodeId, and NodeId values are stable authored IDs. Runtime unlock state lives in
    /// AbilityPathState and is serialized through AbilityPathSave.
    /// </remarks>
    public class AbilityPathDefinition
    {
        public string PathId { get; set; } = string.Empty;
        public string StartingNodeId { get; set; } = string.Empty;
        public List<AbilityPathNodeDefinition> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Static node definition in an ability path graph.
    /// </summary>
    public class AbilityPathNodeDefinition
    {
        public string NodeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Vector2 GraphPosition { get; set; } = Vector2.Zero;
        public List<string> ConnectedNodeIds { get; set; } = new();
        public int Cost { get; set; } = 1;
        public AbilityPathNodeType NodeType { get; set; } = AbilityPathNodeType.StatIncrease;
        public List<AbilityEffectDefinition> Effects { get; set; } = new();
    }

    /// <summary>
    /// Static effect applied when an ability path node is unlocked.
    /// </summary>
    public class AbilityEffectDefinition
    {
        public string EffectId { get; set; } = string.Empty;
        public AbilityEffectType EffectType { get; set; }
        public AbilityTargetStat TargetStat { get; set; } = AbilityTargetStat.None;
        public int Amount { get; set; }
        public string AbilityId { get; set; } = string.Empty;
        public string PassiveId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
