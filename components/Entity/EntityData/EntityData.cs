using Godot;
using System;
using System.Collections.Generic;

public partial class EntityData : Node
{
	public string EntityName { get; set; }
	public override void _Ready()
	{
		EventManager.I.Subscribe<Stat>(GameEvent.StatChange, UpdateStats);



	}

	public override void _ExitTree()
	{
		EventManager.I.Unsubscribe<Stat>(GameEvent.StatChange, UpdateStats);


	}

	public EntityData(BaseStats PlayerBaseStats)
	{
		EntityStats.Add(StatType.MaxHealth, new Stat(StatType.MaxHealth, PlayerBaseStats.MaxHealth, PlayerBaseStats.MaxHealth));
		EntityStats.Add(StatType.CurrentHealth, new Stat(StatType.CurrentHealth, PlayerBaseStats.MaxHealth, PlayerBaseStats.MaxHealth));
		EntityStats.Add(StatType.MaxMana, new Stat(StatType.MaxMana, PlayerBaseStats.MaxMana, PlayerBaseStats.MaxMana));
		EntityStats.Add(StatType.CurrentMana, new Stat(StatType.CurrentMana, PlayerBaseStats.MaxMana, PlayerBaseStats.MaxMana));
		EntityStats.Add(StatType.MaxStamina, new Stat(StatType.MaxStamina, PlayerBaseStats.MaxStamina, PlayerBaseStats.MaxStamina));
		EntityStats.Add(StatType.CurrentStamina, new Stat(StatType.CurrentStamina, PlayerBaseStats.MaxStamina, PlayerBaseStats.MaxStamina));
		EntityStats.Add(StatType.AttackPower, new Stat(StatType.AttackPower, PlayerBaseStats.AttackPower, 999));
		EntityStats.Add(StatType.DefencePower, new Stat(StatType.DefencePower, PlayerBaseStats.DefencePower, 999));
		EntityStats.Add(StatType.MagicPower, new Stat(StatType.MagicPower, PlayerBaseStats.MagicPower, 999));
		EntityStats.Add(StatType.MagicDefencePower, new Stat(StatType.MagicDefencePower, PlayerBaseStats.MagicDefencePower, 999));
		EntityStats.Add(StatType.MoveSpeed, new Stat(StatType.MoveSpeed, PlayerBaseStats.MoveSpeed, 99));
		EntityStats.Add(StatType.AttackSpeed, new Stat(StatType.AttackSpeed, PlayerBaseStats.AttackSpeed, 99));
		EntityStats.Add(StatType.MagicSpeed, new Stat(StatType.MagicSpeed, PlayerBaseStats.MagicSpeed, 99));
		EntityStats.Add(StatType.Experience, new Stat(StatType.Experience, PlayerBaseStats.Experience, 999999999));

		Bonus.Add(StatType.MaxHealth, new Stat(StatType.MaxHealth, 0, 999));
		Bonus.Add(StatType.MaxStamina, new Stat(StatType.MaxStamina, 0, 999));
		Bonus.Add(StatType.MaxMana, new Stat(StatType.MaxMana, 0, 999));
		Bonus.Add(StatType.AttackPower, new Stat(StatType.AttackPower, 0, 999));
		Bonus.Add(StatType.DefencePower, new Stat(StatType.DefencePower, 0, 999));
		Bonus.Add(StatType.MagicPower, new Stat(StatType.MagicPower, 0, 999));
		Bonus.Add(StatType.MagicDefencePower, new Stat(StatType.MagicDefencePower, 0, 999));
		Bonus.Add(StatType.MoveSpeed, new Stat(StatType.MoveSpeed, 0, 999));
		Bonus.Add(StatType.AttackSpeed, new Stat(StatType.AttackSpeed, 0, 999));
		Bonus.Add(StatType.MagicSpeed, new Stat(StatType.MagicSpeed, 0, 999));

	}
	public EntityData() { }


	public Dictionary<StatType, Stat> EntityStats = new Dictionary<StatType, Stat>();
	public Dictionary<StatType, Stat> Bonus = new Dictionary<StatType, Stat>();


	private void UpdateStats(Stat statToUpdate)
	{
		if (!EntityStats.ContainsKey(statToUpdate.type)) return;

		EntityStats[statToUpdate.type] = statToUpdate;
	}

	/// True final value = base + bonus
	public int GetValue(StatType t)
	{
		int baseVal = EntityStats.TryGetValue(t, out var sBase) ? sBase.Value : 0;
		int bonusVal = Bonus.TryGetValue(t, out var sBonus) ? sBonus.Value : 0;
		return baseVal + bonusVal;
	}

	/// Adds a permanent or temporary modifier (+/-).  Returns the new bonus total.
	public int AddModifier(StatType t, int delta)
	{
		if (!Bonus.TryGetValue(t, out var statBonus))
		{
			statBonus = new Stat(t, 0, int.MaxValue);
			Bonus[t] = statBonus;
		}

		statBonus.Value += delta;
		// Optional: raise a single StatChange for listeners
		EventManager.I.Publish(GameEvent.StatChange, statBonus);

		return statBonus.Value;
	}

	/// Removes a modifier (pass the same delta you added, but negative)
	public void RemoveModifier(StatType t, int delta)
	{
		AddModifier(t, -delta);
	}

	public void Init(BaseStats stats)
	{
		EntityStats.Add(StatType.MaxHealth, new Stat(StatType.MaxHealth, stats.MaxHealth, stats.MaxHealth));
		EntityStats.Add(StatType.CurrentHealth, new Stat(StatType.CurrentHealth, stats.MaxHealth, stats.MaxHealth));
		EntityStats.Add(StatType.MaxMana, new Stat(StatType.MaxMana, stats.MaxMana, stats.MaxMana));
		EntityStats.Add(StatType.CurrentMana, new Stat(StatType.CurrentMana, stats.MaxMana, stats.MaxMana));
		EntityStats.Add(StatType.MaxStamina, new Stat(StatType.MaxStamina, stats.MaxStamina, stats.MaxStamina));
		EntityStats.Add(StatType.CurrentStamina, new Stat(StatType.CurrentStamina, stats.MaxStamina, stats.MaxStamina));
		EntityStats.Add(StatType.AttackPower, new Stat(StatType.AttackPower, stats.AttackPower, 999));
		EntityStats.Add(StatType.DefencePower, new Stat(StatType.DefencePower, stats.DefencePower, 999));
		EntityStats.Add(StatType.MagicPower, new Stat(StatType.MagicPower, stats.MagicPower, 999));
		EntityStats.Add(StatType.MagicDefencePower, new Stat(StatType.MagicDefencePower, stats.MagicDefencePower, 999));
		EntityStats.Add(StatType.MoveSpeed, new Stat(StatType.MoveSpeed, stats.MoveSpeed, 99));
		EntityStats.Add(StatType.AttackSpeed, new Stat(StatType.AttackSpeed, stats.AttackSpeed, 99));
		EntityStats.Add(StatType.MagicSpeed, new Stat(StatType.MagicSpeed, stats.MagicSpeed, 99));
		EntityStats.Add(StatType.Experience, new Stat(StatType.Experience, stats.Experience, 999999999));

		Bonus.Add(StatType.MaxHealth, new Stat(StatType.MaxHealth, 0, 999));
		Bonus.Add(StatType.MaxStamina, new Stat(StatType.MaxStamina, 0, 999));
		Bonus.Add(StatType.MaxMana, new Stat(StatType.MaxMana, 0, 999));
		Bonus.Add(StatType.AttackPower, new Stat(StatType.AttackPower, 0, 999));
		Bonus.Add(StatType.DefencePower, new Stat(StatType.DefencePower, 0, 999));
		Bonus.Add(StatType.MagicPower, new Stat(StatType.MagicPower, 0, 999));
		Bonus.Add(StatType.MagicDefencePower, new Stat(StatType.MagicDefencePower, 0, 999));
		Bonus.Add(StatType.MoveSpeed, new Stat(StatType.MoveSpeed, 0, 999));
		Bonus.Add(StatType.AttackSpeed, new Stat(StatType.AttackSpeed, 0, 999));
		Bonus.Add(StatType.MagicSpeed, new Stat(StatType.MagicSpeed, 0, 999));
	}



	

	
}
