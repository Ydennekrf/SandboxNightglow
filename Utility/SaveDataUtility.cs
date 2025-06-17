using System;
using Godot;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class SaveData
{

	private static readonly string path = ProjectSettings.GlobalizePath("user://saveData");
	private static readonly int MaxSlots = 4;
	private static readonly string FilePattern = "Save_{0}.json";

	private static readonly JsonSerializerOptions JsonOpts = new() {
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter()}
	};




	public static bool SaveGame(PlayerSaveData data){
		if(!IsValidSlot(data.SaveSlot)) return false;

		EnsureDir();
		string path = SlotPath(data.SaveSlot);
  
		string json = JsonSerializer.Serialize(data, JsonOpts);
		GD.Print(json);
		return SaveTextToFile(path, json);

	}

	public static bool SaveTextToFile(string path, string data){

		try{
			File.WriteAllText(path, data);
			return true;
		}
		catch(System.Exception e){
			GD.Print(e);
			return false;
		}
	}

	public static PlayerSaveData Load(int slot){
		if (!IsValidSlot(slot)) return null;

		string loadPath = SlotPath(slot);
		if (!File.Exists(loadPath))
			return null;

		try
		{
			string json = File.ReadAllText(loadPath);
			return JsonSerializer.Deserialize<PlayerSaveData>(json, JsonOpts);
		}
		catch (Exception e)
		{
			GD.PushError($"SaveData.Load({slot}) failed: {e}");
			return null;
		}

	}

// Helper functions

	private static string SlotPath(int slot) =>
		Path.Combine(path, string.Format(FilePattern, slot));

	private static bool IsValidSlot(int s)
	{
		if (s < 1 || s > MaxSlots)
		{
			GD.PushError($"SaveData: slot {s} out of range 1‑{MaxSlots}");
			return false;
		}
		return true;
	}

	    public static int GetNextAvailableSlot()
    {
        DirAccess.MakeDirRecursiveAbsolute(path);           // ensure dir exists
        using var dir = DirAccess.Open(path);

        int max = 0;
        foreach (var file in dir.GetFiles())
        {
            // slot_5.json -> 5
            if (file.StartsWith("Save_") && int.TryParse(file[5..^5], out var n))
                max = Math.Max(max, n);
        }
        return max + 1;                                         // next free number
    }

	private static void EnsureDir()
	{
		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);
	}
}
