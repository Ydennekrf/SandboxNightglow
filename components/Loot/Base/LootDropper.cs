using Godot;
using Godot.Collections;

namespace ethra.V1
{
    [GlobalClass]
    public partial class LootDropper : Node2D
    {
        [Export] public Array<LootTable> LootTables { get; set; } = new();
        [Export] public PackedScene PickupScene { get; set; }

        private readonly RandomNumberGenerator _random = new();
        private bool _hasDropped;

        public override void _Ready()
        {
            _random.Randomize();
        }

        public void DropLoot()
        {
            if (_hasDropped)
            {
                return;
            }

            _hasDropped = true;

            foreach (LootTable table in LootTables)
            {
                if (table?.Entries == null)
                {
                    continue;
                }

                foreach (LootItem entry in table.Entries)
                {
                    if (entry == null || !MeetsQuestRequirement(entry))
                    {
                        continue;
                    }

                    float chance = Mathf.Clamp(entry.DropChancePercent, 0f, 100f);
                    if (chance < 100f && _random.RandfRange(0f, 100f) >= chance)
                    {
                        continue;
                    }

                    SpawnPickup(entry);
                }
            }
        }

        private static bool MeetsQuestRequirement(LootItem entry)
        {
            if (string.IsNullOrWhiteSpace(entry.RequiredQuestId))
            {
                return true;
            }

            return GameManager.Instance?.Quest?.IsQuestActive(entry.RequiredQuestId) == true;
        }

        private void SpawnPickup(LootItem entry)
        {
            GameManager gameManager = GameManager.Instance;
            InventoryItem item = gameManager?.DB?.GetItemFromRepo(entry.ItemId);

            if (item == null || PickupScene == null)
            {
                GD.PushWarning($"LootDropper could not spawn item {entry.ItemId}.");
                return;
            }

            ItemPickup pickup = PickupScene.Instantiate<ItemPickup>();
            pickup.ItemId = entry.ItemId;
            pickup.Quantity = Mathf.Max(1, entry.Quantity);
            pickup.PickupTexture = item.Icon;

            Node spawnParent = ResolveSpawnParent();
            if (spawnParent == null)
            {
                pickup.QueueFree();
                return;
            }

            spawnParent.AddChild(pickup);
            pickup.GlobalPosition = GlobalPosition;
        }

        private Node ResolveSpawnParent()
        {
            Node current = GetParent();
            while (current != null)
            {
                if (current is WorldSceneRoot world)
                {
                    return world.Interactables;
                }

                current = current.GetParent();
            }

            return GetParent()?.GetParent() ?? GetTree()?.CurrentScene;
        }
    }
}
