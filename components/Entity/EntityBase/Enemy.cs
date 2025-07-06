using Godot;
using System.Collections.Generic;


public partial class Enemy : Entity
{
    [Export] public int MaxHp = 999;

    public override void _Ready()
    {
        // Initialise stats
        Data.EntityStats[StatType.MaxHealth].SetCurrent(MaxHp);
        Data.EntityStats[StatType.CurrentHealth].SetCurrent(MaxHp);
    }

    /*  Take damage, flash red, stay alive  */
    public override void TakeDamage(int amount, DamageType type ,Entity? attacker = null)
    {
        base.TakeDamage(amount,type, attacker);        // runs your default HP logic

        /* Visual: flash red */
        Modulate = Colors.Red;
        CreateTween()
            .TweenProperty(this, "modulate", Colors.White, 0.15f);

        /* Auto-heal so it never dies */
        Data.EntityStats[StatType.CurrentHealth].SetCurrent(MaxHp);
    }
}