using Godot;

public partial class SceneFlowDebugLogger : Node
{
    [Export] public string ReadyMessage { get; set; } = string.Empty;

    public override void _Ready()
    {
        if (!string.IsNullOrWhiteSpace(ReadyMessage))
        {
            GD.Print(ReadyMessage);
        }
    }
}
