using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class LootItem : Resource
    {
        [Export] public int ItemId { get; set; }

        [Export(PropertyHint.Range, "1,99,1")]
        public int Quantity { get; set; } = 1;

        [Export(PropertyHint.Range, "0,100,0.1,suffix:%")]
        public float DropChancePercent { get; set; } = 100f;

        [Export] public string RequiredQuestId { get; set; } = string.Empty;
    }
}
