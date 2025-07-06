using Godot;

/// Reduces the target’s speed multiplier for a limited time.
public partial class SlowEffect : Node, IStatusEffect
{
    public int Divider = 2;   // 0.6 = 40 % slower
    public float Duration   = 4f;

    private float _elapsed;

    public void Start(Entity target)
    {
        target.Data.EntityStats[StatType.MoveSpeed].changeStatValue(target.Data.EntityStats[StatType.MoveSpeed].Value/ Divider);          // assume you expose this
    }

    public bool Tick(float delta, Entity target)
    {
        _elapsed += delta;
        if (_elapsed >= Duration)
        {
            target.Data.EntityStats[StatType.MoveSpeed].changeStatValue(target.Data.EntityStats[StatType.MoveSpeed].Value * Divider);      // restore speed
            return true;                               // tell Entity to remove me
        }
        return false;
    }
}