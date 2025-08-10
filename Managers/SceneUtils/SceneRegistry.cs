using Godot;
using System.Collections.Generic;

public static class SceneRegistry
{

	// add new entries by giving the desired name and the path from the editor
	public static readonly Dictionary<string, string> ScenePaths = new(){
		{"Player", "res://PackedScenes/Characters/Player/player.tscn" },
		{"MainMenu", "res://components/UI/PackedScenes/MainMenu.tscn" },
		{"TestZone", "res://PackedScenes/Zones/TestZone.tscn" },
		{"Slime", "res://PackedScenes/Characters/Enemy/training_dummy.tscn"},
		{"GameOver" , "res://components/UI/PackedScenes/GameOver.tscn"}
	};

	public static void ValidateAll()
	{
		foreach (var entry in ScenePaths)
		{
			var scene = ResourceLoader.Load<PackedScene>(entry.Value);
			if (scene == null)
				GD.PushError($"SceneRegistry: Could not load scene '{entry.Key}' at path: {entry.Value}");
			else
				GD.Print($"SceneRegistry: Validated '{entry.Key}'");
		}
	}

	// 🧩 Use this to safely load scenes
	public static PackedScene Load(string key)
	{
		if (ScenePaths.TryGetValue(key, out var path))
			return ResourceLoader.Load<PackedScene>(path);

		GD.PushError($"SceneRegistry: Scene '{key}' not found.");
		return null;
	}
}
