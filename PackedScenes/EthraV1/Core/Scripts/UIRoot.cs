using Godot;

public partial class UIRoot : CanvasLayer
{
	private static readonly bool DebugMenuInput = false;

	[Export] public NodePath HudPath { get; set; } = "Hud";
	[Export] public NodePath PlayerHudPath { get; set; } = "Hud/PlayerHud";
	[Export] public NodePath MainMenuPath { get; set; } = "Menus/MainMenu";
	[Export] public NodePath PlayerMenuPath { get; set; } = "Menus/PlayerMenu";
	[Export] public NodePath CraftingPanelPath { get; set; } = "Menus/CraftingPanel";

	public Control Hud => GetNodeOrNull<Control>(HudPath);
	public ethra.V1.PlayerHud PlayerHud => GetNodeOrFallback<ethra.V1.PlayerHud>(PlayerHudPath, "Hud/PlayerHud");
	public Control MainMenu => GetNodeOrFallback<Control>(MainMenuPath, "Menus/MainMenu");
	public Control PlayerMenu => GetNodeOrFallback<Control>(PlayerMenuPath, "Menus/PlayerMenu");
	public ethra.V1.CraftingPanel CraftingPanel => GetNodeOrFallback<ethra.V1.CraftingPanel>(CraftingPanelPath, "Menus/CraftingPanel");

	public bool IsPlayerMenuVisible => PlayerMenu != null && PlayerMenu.Visible;
	public bool IsCraftingPanelVisible => CraftingPanel != null && CraftingPanel.Visible;
	public bool BlocksGameplayInput => IsPlayerMenuVisible || IsCraftingPanelVisible;

	public void ShowHud(bool show) => SetVisibleSafe(Hud, show);
	public void ShowMainMenu(bool show) => SetVisibleSafe(MainMenu, show);

	public void ShowPlayerMenu(bool show)
	{
		Control playerMenu = PlayerMenu;
		bool wasVisible = playerMenu != null && playerMenu.Visible;

		if (DebugMenuInput)
		{
			GD.Print($"[UIDebug] ShowPlayerMenu({show}). path='{PlayerMenuPath}' resolved={playerMenu?.GetPath().ToString() ?? "null"}");
		}

		SetVisibleSafe(playerMenu, show);
		if (wasVisible != show)
		{
			ethra.V1.AudioManager.PlayUi(show ? "sound.ui.menu_open" : "sound.ui.menu_close");
		}
	}

	public void TogglePlayerMenu()
	{
		bool isVisible = IsPlayerMenuVisible;
		if (DebugMenuInput)
		{
			GD.Print($"[UIDebug] TogglePlayerMenu. currentlyVisible={isVisible}");
		}

		ShowPlayerMenu(!isVisible);
	}

	public void OpenCraftingPanel(string stationType)
	{
		bool wasVisible = IsCraftingPanelVisible;
		ShowPlayerMenu(false);
		CraftingPanel?.Open(stationType);
		if (!wasVisible && IsCraftingPanelVisible)
		{
			ethra.V1.AudioManager.PlayUi("sound.ui.menu_open");
		}
	}

	public void CloseCraftingPanel()
	{
		bool wasVisible = IsCraftingPanelVisible;
		CraftingPanel?.Close();
		if (wasVisible && !IsCraftingPanelVisible)
		{
			ethra.V1.AudioManager.PlayUi("sound.ui.menu_close");
		}
	}

	public bool CloseTopGameplayPanel()
	{
		if (IsCraftingPanelVisible)
		{
			CloseCraftingPanel();
			return true;
		}

		if (IsPlayerMenuVisible)
		{
			ShowPlayerMenu(false);
			return true;
		}

		return false;
	}

	public void ShowOnlyHud()
	{
		ShowHud(true);
		ShowMainMenu(false);
		ShowPlayerMenu(false);
		CloseCraftingPanel();
	}

	public void ShowOnlyMainMenu()
	{
		ShowHud(false);
		ShowMainMenu(true);
		ShowPlayerMenu(false);
		CloseCraftingPanel();
	}

	public void ShowOnlyPlayerMenu()
	{
		ShowHud(false);
		ShowMainMenu(false);
		ShowPlayerMenu(true);
	}

	private static void SetVisibleSafe(CanvasItem node, bool visible)
	{
		if (node == null)
		{
			GD.PushError("UIRoot: attempted to toggle visibility, but node was null (check exported paths).");
			return;
		}

		node.Visible = visible;
		if (DebugMenuInput)
		{
			GD.Print($"[UIDebug] SetVisibleSafe: {node.GetPath()} visible={node.Visible}");
		}
	}

	private T GetNodeOrFallback<T>(NodePath configuredPath, string fallbackPath) where T : Node
	{
		T node = null;
		if (configuredPath != null && !configuredPath.IsEmpty)
		{
			node = GetNodeOrNull<T>(configuredPath);
		}

		return node ?? GetNodeOrNull<T>(fallbackPath);
	}
}
