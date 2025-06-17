using Godot;

[GlobalClass]
public partial class StatDelta : Resource
{
    [Export] public StatType Type;
    [Export] public int      Delta;
}