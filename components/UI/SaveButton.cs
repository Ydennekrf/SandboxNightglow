// using Godot;
// using System;
// using System.Collections.Generic;

// public partial class SaveButton : Button
// {
// 	public override void _Ready()
// 	{
// 		Pressed += OnSave;
// 	}

// 	private void OnSave()
// 	{
// 		var player = PlayerManager.Instance.GetFirstPlayer();
// 		if (player == null) return;

// 		var statsComp = player.GetNodeOrNull<EntityData>("Stats"); // or StatsComponent if renamed
// 		if (statsComp == null) return;



// 		foreach (var kv in statsComp.EntityStats)
// 			saveData.CurrentStats[kv.Key.ToString()] = kv.Value.Value;

// 		SaveData.SaveGame(1, saveData);
// 		GD.Print("Game saved.");
// 	}
// }
