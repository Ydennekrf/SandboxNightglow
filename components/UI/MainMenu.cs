using Godot;
using System;
using System.Threading;
using System.Collections.Generic;

public partial class MainMenu : Control
{
	[Export] private Button NewGameButton;
	[Export] private Button LoadGameButton;
	[Export] private Button QuitButton;
	[Export] private VBoxContainer MenuOptions;
	[Export] private VBoxContainer LoadOptions;
	[Export] private PackedScene LoadOptionButton;

	public override void _Ready()
	{
		NewGameButton.Pressed += OnNewGame;
		// this well eventually need to direct to a new screen that will show all the available slots.
		// these slots will have an int val attached and then this is how we will access the proper file name.
		LoadGameButton.Pressed += OnLoadGame;
		QuitButton.Pressed += OnQuit;
		LoadOptions.Visible = false;
		MenuOptions.Visible = true;
	}

	private void OnNewGame()
	{

		GameManager.Instance.StartNewGame();
	}

	private void OnLoadGame()
	{
		//this should end up being placed within a manager class i would imagine.
		LoadOptions.Visible = true;
		MenuOptions.Visible = false;

		string saveDir = ProjectSettings.GlobalizePath("user://saveData");

		// 2. Build buttons
		foreach (var file in GetAllSaveFiles())          // file = "Save_1.json"
		{
			string fullPath = System.IO.Path.Combine(saveDir, file);          // <- NEW
			string json     = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read)
										.GetAsText();                      // <- NEW

			var slot = LoadOptionButton.Instantiate<SaveSlotButton>();
			LoadOptions.AddChild(slot);
			slot.SetSlotInfo(json);       // pass JSON string, not the file name
			
		}
		// in practice pass the slot number through the event and then pass that to the load function

	}

	private void OnQuit()
	{
		GetTree().Quit();
	}
	
	public static List<string> GetAllSaveFiles()
{
    List<string> saveFiles = new();

    string absPath = ProjectSettings.GlobalizePath("user://saveData");

    if (!DirAccess.DirExistsAbsolute(absPath))
    {
        GD.Print("Save directory does not exist.");
        return saveFiles;
    }

    using var dir = DirAccess.Open(absPath);
    if (dir == null)
    {
        GD.PushError("Failed to open save directory.");
        return saveFiles;
    }

    foreach (string file in dir.GetFiles())
    {
        if (file.EndsWith(".json") && file.StartsWith("Save_"))
            saveFiles.Add(file);
    }

    return saveFiles;
}
}
