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

		var gm = ethra.V1.GameManager.Instance;
		if (gm == null)
		{
			GD.PushError("GameManager.Instance is null. Confirm GameManager is an AutoLoad and its _Ready() ran.");
			return;
		}

		gm.StartNewGame();
	}

	private void OnLoadGame()
	{
		

	}

	private void OnQuit()
	{
		GetTree().Quit();
	}
	
	public static List<string> GetAllSaveFiles()
	{
	throw new NotImplementedException();
	}
}
