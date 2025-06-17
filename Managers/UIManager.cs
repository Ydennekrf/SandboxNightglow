using Godot;

public partial class UIManager : CanvasLayer
{
    [Export] private PackedScene   _labelScene;
    [Export] private VBoxContainer _stack;

    public override void _Ready()
    {
        EventManager.I.Subscribe<string>(
            GameEvent.ToastMessage,
            ShowToast);
    }

    private void ShowToast(string msg)
    {
        var label = _labelScene.Instantiate<ToastBubble>();
        label.SetText(msg);
        _stack.AddChild(label);
    }
    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<string>(
            GameEvent.ToastMessage,
            ShowToast);
    }
}

