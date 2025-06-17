using Godot;
using System;

public partial class DrainStatAction : Node, IStateAction
{
    [Export] public StatType StatToDrain = StatType.CurrentStamina;

    /// How much to subtract **per second** (not per tick!)
    [Export] public float DrainRatePerSecond = 10f;

    /// Clamp at zero?
    [Export] public bool PreventNegative = true;

    private float _accum = 0f;

    public void Enter(Entity owner, BaseState state)
    {
        _accum = 0f;
    }

    public void Execute(float delta, Entity owner,  BaseState state)
    {
        if (owner.Data == null || !owner.Data.EntityStats.TryGetValue(StatToDrain, out Stat stat))
            return;

        float drain = DrainRatePerSecond * delta;
        _accum += drain;

        // only subtract whole units, keep leftover in accumulator
        int intDrain = (int)_accum;
        if (intDrain > 0)
        {
            _accum -= intDrain;

            stat.changeStatValue(-intDrain);

        }
    }

    public void Exit(Entity owner)
    {
        _accum = 0f;
    }
}