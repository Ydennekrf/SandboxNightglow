using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    [Tool]
    [GlobalClass]
    public partial class AbilityPathAuthorGraph : Node2D
    {
        [Export] public string PathId { get; set; } = "abilitypath.player.debug";
        [Export] public string StartingNodeId { get; set; } = "abilitypath.player.start";

        public AbilityPathDefinition BuildDefinition()
        {
            AbilityPathDefinition definition = new()
            {
                PathId = PathId,
                StartingNodeId = StartingNodeId
            };

            foreach (AbilityPathAuthorNode node in GetAuthorNodes())
            {
                definition.Nodes.Add(node.ToDefinition());
            }

            return definition;
        }

        public Dictionary<string, Vector2> GetNodePositions()
        {
            Dictionary<string, Vector2> positions = new(System.StringComparer.OrdinalIgnoreCase);
            foreach (AbilityPathAuthorNode node in GetAuthorNodes())
            {
                if (!string.IsNullOrWhiteSpace(node.NodeId))
                {
                    positions[node.NodeId] = node.Position;
                }
            }

            return positions;
        }

        private IEnumerable<AbilityPathAuthorNode> GetAuthorNodes()
        {
            foreach (Node child in GetChildren())
            {
                if (child is AbilityPathAuthorNode node)
                {
                    yield return node;
                }
            }
        }
    }
}
