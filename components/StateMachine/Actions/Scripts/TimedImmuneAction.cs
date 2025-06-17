using Godot;

public partial class TimedImmuneAction : Node, IStateAction
{
    [Export] public float Duration = 0.25f;
    private float _timer;

    public void Enter(Entity o, BaseState state)
    {
        _timer = Duration;
        o.SetImmune(true);         // your custom helper
    }

    public void Execute( float dt , Entity o, BaseState state)
    {
        _timer -= dt;
        if (_timer <= 0f)
            o.SetImmune(false);
    }

    public void Exit(Entity o) => o.SetImmune(false);
}