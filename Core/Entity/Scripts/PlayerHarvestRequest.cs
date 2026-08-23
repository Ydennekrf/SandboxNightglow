using System;
using Godot;

namespace ethra.V1
{
    public enum HarvestToolType
    {
        None,
        Pickaxe,
        Axe
    }

    public sealed class PlayerHarvestRequest
    {
        public PlayerHarvestRequest(Node2D target, int itemId, int quantity, Action<PlayerHarvestRequest> completed)
        {
            Target = target;
            ItemId = itemId;
            Quantity = quantity;
            Completed = completed;
        }

        public Node2D Target { get; }
        public int ItemId { get; }
        public int Quantity { get; }
        public Action<PlayerHarvestRequest> Completed { get; }
        public string AnimationKey { get; init; } = "Harvest";
        public float DurationSeconds { get; init; } = 0.35f;
        public HarvestToolType ToolType { get; init; } = HarvestToolType.None;
    }
}
