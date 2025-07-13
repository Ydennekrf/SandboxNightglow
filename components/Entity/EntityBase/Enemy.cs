using Godot;
using System.Collections.Generic;


public partial class Enemy : Entity
{

    [Export] public BaseStats baseStats;
    [Export] public NodePath statsComponent;

    public override void _Ready()
    {
        Data = GetNode<EntityData>(statsComponent);
        Data.Init(baseStats);
    }

    /*  Take damage, flash red, stay alive  */
    public override void TakeDamage(int amount, DamageType type, Entity? attacker = null)
    {
        base.TakeDamage(amount, type, attacker);
        // base.TakeDamage(amount,type, attacker);        // runs your default HP logic

        // /* Visual: flash red */


        // /* Auto-heal so it never dies */
        // Data.EntityStats[StatType.CurrentHealth].SetCurrent(MaxHp);
    }

    public override void Die()
    {
        base.Die();
        // delete the enemy scene, drop loot, give the killer expereince and any money.
        // enemy expereince is based on its entity Data Experience Value
        this.QueueFree();
    }
}