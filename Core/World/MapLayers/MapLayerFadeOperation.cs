using Godot;

namespace ethra.V1;

[GlobalClass]
public partial class MapLayerFadeOperation : Resource
{
    [Export] public string LayerId { get; set; } = string.Empty;
    [Export(PropertyHint.Range, "0,1,0.01")] public float TargetAlpha { get; set; } = 1f;
    [Export(PropertyHint.Range, "0,5,0.05")] public float DurationSeconds { get; set; } = 0.2f;
    [Export] public bool HideWhenTransparent { get; set; } = true;
}
