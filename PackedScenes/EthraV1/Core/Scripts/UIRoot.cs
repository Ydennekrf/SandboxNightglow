using Godot;
using System;

public partial class UIRoot : CanvasLayer
{
		private const bool DebugMenuInput = true;
	 	[Export] public NodePath HudPath { get; set; } = "Hud";
		[Export] public NodePath MainMenuPath { get; set; } = "Menus/MainMenu";
		[Export] public NodePath PlayerMenuPath { get; set; } = "Hud/PlayerMenu";

		public Control Hud => GetNodeOrNull<Control>(HudPath);
		public Control MainMenu => GetNodeOrNull<Control>(MainMenuPath);
		public Control PlayerMenu => GetNodeOrNull<Control>(PlayerMenuPath);

		public void ShowHud(bool show) => SetVisibleSafe(Hud, show);
		public void ShowMainMenu(bool show) => SetVisibleSafe(MainMenu, show);
		public void ShowPlayerMenu(bool show)
		{
			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] ShowPlayerMenu({show}). path='{PlayerMenuPath}' resolved={PlayerMenu?.GetPath().ToString() ?? "null"}");
			}

			SetVisibleSafe(PlayerMenu, show);
		}

		public void TogglePlayerMenu()
		{
			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] TogglePlayerMenu. currentlyVisible={IsPlayerMenuVisible}");
			}

			ShowPlayerMenu(!IsPlayerMenuVisible);
		}
		public bool IsPlayerMenuVisible => PlayerMenu != null && PlayerMenu.Visible;

    public void ShowOnlyHud()
		{
			ShowHud(true);
			ShowMainMenu(false);
			ShowPlayerMenu(false);
		}

		public void ShowOnlyMainMenu()
		{
			ShowHud(false);
			ShowMainMenu(true);
			ShowPlayerMenu(false);
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
}
