using Godot;
using System;

public partial class UIRoot : CanvasLayer
{
	 	[Export] public NodePath HudPath { get; set; } = "Hud";
		[Export] public NodePath MainMenuPath { get; set; } = "Menus/MainMenu";
		[Export] public NodePath PlayerMenuPath { get; set; } = "Hud/PlayerUi";

		public Control Hud => GetNodeOrNull<Control>(HudPath);
		public Control MainMenu => GetNodeOrNull<Control>(MainMenuPath);
		public Control PlayerMenu => GetNodeOrNull<Control>(PlayerMenuPath);

		public void ShowHud(bool show) => SetVisibleSafe(Hud, show);
		public void ShowMainMenu(bool show) => SetVisibleSafe(MainMenu, show);

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

		public void ShowPlayerMenu(bool show) => SetVisibleSafe(PlayerMenu, show);

		public void TogglePlayerMenu()
		{
			Control menu = PlayerMenu;
			if (menu == null)
			{
				GD.PushError("UIRoot: PlayerMenu node not found (check PlayerMenuPath).");
				return;
			}

			menu.Visible = !menu.Visible;
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event.IsActionPressed("Inventory_Toggle"))
			{
				TogglePlayerMenu();
				GetViewport().SetInputAsHandled();
			}
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
