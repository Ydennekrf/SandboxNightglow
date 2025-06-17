using Godot;
using System;

public partial class RegenStatAction : Node, IStateAction
{
    [Export] public StatType StatToRegen = StatType.CurrentStamina;

    /// How much to subtract **per second** (not per tick!)
    [Export] public float RegenRatePerSecond = 10f;
    [Export] public float RegenDelay = 1f;

    /// Clamp at zero?
    [Export] public bool ClampToMax = true;

    private float delayRemaining;
    private float _accum = 0f;

    public void Enter(Entity owner, BaseState state)
    {
        delayRemaining = RegenDelay;
        _accum = 0f;
    }

    public void Execute(float delta, Entity owner, BaseState state)
    {


         if (delayRemaining > 0f)
        {
            delayRemaining -= delta;
            return;
        }
        
        if (owner.Data == null || !owner.Data.EntityStats.TryGetValue(StatToRegen, out Stat stat))
            return;

        _accum += RegenRatePerSecond * delta;
        int intGain = (int)_accum;
        if (intGain == 0) return;

        // Clamp?
        if (ClampToMax &&
            TryGetMaxStat(owner, StatToRegen, out var max) &&
            stat.Value + intGain > max.Value)
        {
            intGain = max.Value - stat.Value;   // only top up to max
        }

        if (intGain != 0)
            stat.changeStatValue(intGain);      // fires StatChange event
    }

    public void Exit(Entity owner)
    {
        _accum = 0f;
    }
    

        private bool TryGetMaxStat(Entity owner, StatType current, out Stat maxStat)
    {
        var maxKey = current switch
        {
            StatType.CurrentHealth  => StatType.MaxHealth,
            StatType.CurrentMana    => StatType.MaxMana,
            StatType.CurrentStamina => StatType.MaxStamina,
            _                       => StatType.None
        };

        if (maxKey != StatType.None &&
            owner.Data.EntityStats.TryGetValue(maxKey, out maxStat))
            return true;

        maxStat = null;
        return false;
    }
}