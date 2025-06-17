using Godot;

public partial class ToastBubble : Panel
{
    [Export] private AnimationPlayer _anim;
    [Export] private Label           _label;

    public void SetText(string txt)
    {
        _label.Text = txt;
        _anim.Play("appear");
    }

    public override void _Ready()
    {
        _anim.AnimationFinished += name => QueueFree();
        _anim.Play("fade");                 // same fade animation as before
    }
}