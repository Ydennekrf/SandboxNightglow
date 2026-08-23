using Godot;
using Godot.Collections;

namespace ethra.V1
{
    [GlobalClass]
    public partial class LootTable : Resource
    {
        [Export] public Array<LootItem> Entries { get; set; } = new();
    }
}
