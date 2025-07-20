using Godot;

[GlobalClass]
public partial class WaitDurationAction : Node, IStateAction
{
    [Export] public float Duration = 0.25f;  

    private float _timer;

    public void Enter(Entity owner, BaseState _) => _timer = Duration;

    public void Execute(float delta, Entity owner, BaseState _)
    {
        _timer -= delta;
        if (_timer < 0f) _timer = 0f;       
    }

    public void Exit(Entity owner) { }


}
