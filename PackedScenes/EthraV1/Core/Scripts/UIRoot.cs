using Godot;
using System;

public partial class UIRoot : CanvasLayer
{
	 	[Export] public NodePath HudPath { get; set; } = "Hud";
		[Export] public NodePath MainMenuPath { get; set; } = "Menus/MainMenu";
		[Export] public NodePath PlayerUIPath { get; set; } = "Menus/PlayerMenu";

		public Control Hud => GetNodeOrNull<Control>(HudPath);
		public Control MainMenu => GetNodeOrNull<Control>(MainMenuPath);
		public Control PlayerMenu => GetNodeOrNull<Control>(PlayerUIPath);

		public void ShowHud(bool show) => SetVisibleSafe(Hud, show);
		public void ShowMainMenu(bool show) => SetVisibleSafe(MainMenu, show);
		public void ShowPlayerMenu(bool show) => SetVisibleSafe(PlayerMenu, show);

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
		}
}
