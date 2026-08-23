using Godot;

namespace ethra.V1;

[GlobalClass]
public partial class MapLayerState : Resource
{
    [Export] public string StateId { get; set; } = string.Empty;
    [Export] public string[] VisibleLayerIds { get; set; } = [];
    [Export] public string[] HiddenLayerIds { get; set; } = [];
    [Export] public string[] CollisionEnabledLayerIds { get; set; } = [];
    [Export] public string[] CollisionDisabledLayerIds { get; set; } = [];
    [Export] public Godot.Collections.Array<MapLayerFadeOperation> FadeOperations { get; set; } = [];
}
