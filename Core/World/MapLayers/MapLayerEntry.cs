using Godot;

namespace ethra.V1;

[GlobalClass]
public partial class MapLayerEntry : Resource
{
    [Export] public string LayerId { get; set; } = string.Empty;
    [Export] public NodePath LayerPath { get; set; }
    [Export] public bool IsVisualLayer { get; set; } = true;
    [Export] public bool IsCollisionLayer { get; set; }
    [Export] public bool DefaultVisible { get; set; } = true;
    [Export] public bool DefaultCollisionEnabled { get; set; } = true;
}
