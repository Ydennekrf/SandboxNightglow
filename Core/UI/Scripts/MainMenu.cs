using Godot;
using System;
using System.Threading;
using System.Collections.Generic;
using ethra.V1;

public partial class MainMenu : Control
{
	[Export] private Button NewGameButton;
	[Export] private Button LoadGameButton;
	[Export] private Button QuitButton;
	[Export] private VBoxContainer MenuOptions;
	[Export] private VBoxContainer LoadOptions;
	[Export] private PackedScene LoadOptionButton;
	private LineEdit _newGameNameInput;

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

	public override void _Notification(int what)
	{
		if (what == NotificationVisibilityChanged && Visible)
		{
			ShowMainOptions();
		}
	}

	private void OnNewGame()
	{
		ShowNewGameSetup();
	}

	private void ShowNewGameSetup()
	{
		MenuOptions.Visible = false;
		LoadOptions.Visible = true;
		BuildNewGameSetup();
	}

	private void BuildNewGameSetup()
	{
		ClearLoadOptions();

		Label title = new Label
		{
			Text = "New Game",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		LoadOptions.AddChild(title);

		_newGameNameInput = new LineEdit
		{
			Text = "Player",
			PlaceholderText = "Player name",
			CustomMinimumSize = new Vector2(320, 36)
		};
		LoadOptions.AddChild(_newGameNameInput);

		IReadOnlyList<SaveSlotInfo> slots = ethra.V1.GameManager.Instance?.GetSaveSlotInfos() ?? new List<SaveSlotInfo>();
		for (int i = 0; i < SaveLoadService.MaxSaveSlots; i++)
		{
			SaveSlotInfo slot = i < slots.Count ? slots[i] : SaveSlotInfo.Empty(i + 1);
			Button button = BuildSaveSlotButton(slot, newGameMode: true);

			int selectedSlot = slot.SlotNumber;
			button.Pressed += () => StartNewGameInSlot(selectedSlot);
			LoadOptions.AddChild(button);
		}

		AddBackButton();
	}

	private void StartNewGameInSlot(int slot)
	{
		var gm = ethra.V1.GameManager.Instance;
		if (gm == null)
		{
			GD.PushError("GameManager.Instance is null. Confirm GameManager is an AutoLoad and its _Ready() ran.");
			return;
		}

		string playerName = string.IsNullOrWhiteSpace(_newGameNameInput?.Text) ? "Player" : _newGameNameInput.Text.Trim();
		gm.StartNewGameInSlot(slot, playerName);
	}

	private void OnLoadGame()
	{
		MenuOptions.Visible = false;
		LoadOptions.Visible = true;
		RefreshSaveSlots();

	}

	private void OnQuit()
	{
		GetTree().Quit();
	}
	
	public static List<string> GetAllSaveFiles()
	{
		return new List<string>();
	}

	private void RefreshSaveSlots()
	{
		ClearLoadOptions();

		var gm = ethra.V1.GameManager.Instance;
		IReadOnlyList<SaveSlotInfo> slots = gm?.GetSaveSlotInfos() ?? new List<SaveSlotInfo>();

		for (int i = 0; i < SaveLoadService.MaxSaveSlots; i++)
		{
			SaveSlotInfo slot = i < slots.Count ? slots[i] : SaveSlotInfo.Empty(i + 1);
			Button button = BuildSaveSlotButton(slot, newGameMode: false);

			button.Pressed += () => OnSaveSlotPressed(slot);
			LoadOptions.AddChild(button);
		}

		AddBackButton();
	}

	private static string BuildSlotText(SaveSlotInfo slot)
	{
		if (slot == null || !slot.Occupied)
		{
			return $"Save Slot {slot?.SlotNumber ?? 0}\nEmpty Slot";
		}

		return $"Save Slot {slot.SlotNumber}\n{slot.PlayerName} | {slot.GameTimePlayed}\n{slot.SceneName}\n{slot.CurrentMainQuestName}";
	}

	private Button BuildSaveSlotButton(SaveSlotInfo slot, bool newGameMode)
	{
		Button button = LoadOptionButton?.InstantiateOrNull<Button>() ?? new Button();
		if (button is SaveSlotButton saveSlotButton)
		{
			saveSlotButton.SetSlotInfo(slot, newGameMode);
		}
		else
		{
			button.Text = newGameMode
				? BuildNewGameSlotText(slot)
				: BuildSlotText(slot);
			button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		}

		button.CustomMinimumSize = new Vector2(320, newGameMode ? 72 : 64);
		return button;
	}

	private static string BuildNewGameSlotText(SaveSlotInfo slot)
	{
		if (slot == null || !slot.Occupied)
		{
			return $"Start in Save Slot {slot?.SlotNumber ?? 0}\nEmpty Slot";
		}

		return $"Start in Save Slot {slot.SlotNumber}\nOverwrite: {slot.PlayerName} | {slot.GameTimePlayed}\n{slot.SceneName}";
	}

	private void OnSaveSlotPressed(SaveSlotInfo slot)
	{
		if (slot == null || !slot.Occupied)
		{
			GD.Print($"[SaveLoad] No save found for slot {slot?.SlotNumber ?? 0}.");
			return;
		}

		ethra.V1.GameManager.Instance?.LoadSavedGame(slot.SlotNumber);
	}

	private void ShowMainOptions()
	{
		LoadOptions.Visible = false;
		MenuOptions.Visible = true;
	}

	private void ClearLoadOptions()
	{
		foreach (Node child in LoadOptions.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void AddBackButton()
	{
		Button backButton = new Button
		{
			Text = "Back"
		};
		backButton.Pressed += ShowMainOptions;
		LoadOptions.AddChild(backButton);
	}
}
