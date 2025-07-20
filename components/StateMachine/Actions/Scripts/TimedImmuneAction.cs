using Godot;

public partial class TimedImmuneAction : Node, IStateAction, IStateTransition
{
    [Export] public NodePath TargetPath { get; set; }
    [Export] public float Duration = 0.25f;
    private float _timer;

    public void Enter(Entity o, BaseState state)
    {
        _timer = Duration;
        o.SetImmune(true);
    }

    public void Execute(float dt, Entity o, BaseState state)
    {
        _timer -= dt;
        if (_timer <= 0f)
            o.SetImmune(false);
    }

    public void Exit(Entity o) => o.SetImmune(false);
    
     public BaseState Target => GetNode<BaseState>(TargetPath);

    public bool ShouldTransition(Entity owner)
    {
        if (_timer <= 0f)
        {
            return true;
        }

        return false;
    }
}