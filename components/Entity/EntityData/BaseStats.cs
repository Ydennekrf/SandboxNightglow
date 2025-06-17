using Godot;

[GlobalClass]
public partial class BaseStats : Resource 
{
    [Export] public int MaxHealth;
    [Export] public int MaxMana;
    [Export] public int MaxStamina;
    [Export] public int AttackPower;
    [Export] public int DefencePower;
    [Export] public int MagicPower;
    [Export] public int MagicDefencePower;
    [Export] public int MoveSpeed;
    [Export] public int AttackSpeed;
    [Export] public int MagicSpeed;
    [Export] public int Experience;
}