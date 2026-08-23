using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace ethra.V1
{
    [Tool]
    [GlobalClass]
    public partial class AbilityPathAuthorNode : Node2D
    {
        [Export] public string NodeId { get; set; } = string.Empty;
        [Export] public string DisplayName { get; set; } = string.Empty;
        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
        [Export] public Array<string> ConnectedNodeIds { get; set; } = new();
        [Export] public int Cost { get; set; } = 1;
        [Export] public AbilityPathNodeType NodeType { get; set; } = AbilityPathNodeType.StatIncrease;
        [Export] public Array<AbilityEffectResource> Effects { get; set; } = new();

        public AbilityPathNodeDefinition ToDefinition()
        {
            List<string> connected = new();
            foreach (string nodeId in ConnectedNodeIds)
            {
                if (!string.IsNullOrWhiteSpace(nodeId))
                {
                    connected.Add(nodeId);
                }
            }

            List<AbilityEffectDefinition> effects = new();
            foreach (AbilityEffectResource effect in Effects)
            {
                if (effect != null)
                {
                    effects.Add(effect.ToDefinition());
                }
            }

            return new AbilityPathNodeDefinition
            {
                NodeId = NodeId,
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? NodeId : DisplayName,
                Description = Description,
                GraphPosition = Position,
                ConnectedNodeIds = connected,
                Cost = Mathf.Max(0, Cost),
                NodeType = NodeType,
                Effects = effects
            };
        }
    }
}
