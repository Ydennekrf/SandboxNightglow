using Godot;
using System;

public partial class UIRoot : CanvasLayer
{
	 	[Export] public NodePath HudPath { get; set; } = "Hud";
		[Export] public NodePath MainMenuPath { get; set; } = "Menus/MainMenu";

		public Control Hud => GetNodeOrNull<Control>(HudPath);
		public Control MainMenu => GetNodeOrNull<Control>(MainMenuPath);

		public void ShowHud(bool show) => SetVisibleSafe(Hud, show);
		public void ShowMainMenu(bool show) => SetVisibleSafe(MainMenu, show);

		public void ShowOnlyHud()
		{
			ShowHud(true);
			ShowMainMenu(false);
		}

		public void ShowOnlyMainMenu()
		{
			ShowHud(false);
			ShowMainMenu(true);
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
