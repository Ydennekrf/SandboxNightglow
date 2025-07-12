using Godot;
using System.Collections.Generic;


public partial class Enemy : Entity
{

    [Export] public BaseStats baseStats;
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
        // base.TakeDamage(amount,type, attacker);        // runs your default HP logic

        // /* Visual: flash red */
        

        // /* Auto-heal so it never dies */
        // Data.EntityStats[StatType.CurrentHealth].SetCurrent(MaxHp);
    }
}