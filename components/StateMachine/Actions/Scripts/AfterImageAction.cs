using Godot;
public partial class AfterImageAction : Node, IStateAction
{
    [Export] public float SpawnInterval = 0.05f;
    [Export] public float Lifetime = 0.3f;

    private float _timer;

    public void Enter(Entity o, BaseState state) { _timer = 0f; }
    public void Exit(Entity o) { }

    public void Execute(float dt, Entity o, BaseState state)
    {
        _timer -= dt;
        if (_timer > 0) return;

        _timer = SpawnInterval;
        o.SpawnAfterImage(Lifetime);
    }
}