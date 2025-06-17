using Godot;
using System;
using System.Collections.Generic;


[Serializable]
public partial class PlayerSaveData
{

	public Dictionary<string, int> CurrentStats {get; set;} = new();

	public List<ItemStack?> Inventory { get; set; } = new();

	// public Dictionary<ItemType, string> EquippedItems {get; set;} = new();
	 public WorldStateDto WorldState { get; set; }

	public string SceneID { get; set; }

	public string SpawnID {get; set;}

	public int SaveSlot {get; set;}

	public DateTime SavedAt {get; set;} = DateTime.UtcNow;


}
