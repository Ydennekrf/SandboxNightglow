using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
	public partial class UIManager : IResolveable, IUserInterface
	{
		private const bool DebugMenuInput = true;
		private int _resolveOrder = 100;
		private List<IUIRefresh> _uiNodeList;
		private bool _inventoryToggleHeld;
		private ulong _lastInventoryToggleFrame = ulong.MaxValue;
		public int ResolveOrder => _resolveOrder;
		public bool HasRoot => _root != null;

		public List<IUIRefresh> UINodeList { get => _uiNodeList; set => _uiNodeList = value; }

		private UIRoot _root;

		public void Initialize(UIRoot root)
		{
			_root = root;
			_uiNodeList = new List<IUIRefresh>();
			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] UIManager initialized. root={_root?.Name} playerMenu={_root?.PlayerMenu?.GetPath()} mainMenu={_root?.MainMenu?.GetPath()}");
			}
		}

		public void ShowOnlyHud() => _root?.ShowOnlyHud();
		public void ShowOnlyMainMenu() => _root?.ShowOnlyMainMenu();
		public void ShowPlayerMenu(bool show) => _root?.ShowPlayerMenu(show);
        public void TogglePlayerMenu() => _root?.TogglePlayerMenu();

        public void Register(IUIRefresh ui)
		{
			if (!_uiNodeList.Contains(ui))
				_uiNodeList.Add(ui);
		}

		//=========IUserInterface==========//

		public void RefreshUI()
		{

			if (_uiNodeList == null || _uiNodeList.Count == 0)
				return;

			foreach (IUIRefresh control in _uiNodeList)
			{
				if (control.needsRefresh)
				{
					control.Refresh();
				}
			}
		}

		//=========IResolvable=========//

		public void Resolve()
		{
            HandleMenuInput();
            RefreshUI();
		}

		public void Resolve(object obj)
		{
			
		}
        private void HandleMenuInput()
        {
            if (_root == null)
            {
				if (DebugMenuInput && Input.IsKeyPressed(Key.Tab))
				{
					GD.Print("[UIDebug] Tab pressed, but UIManager root is null.");
				}
                return;
            }

			bool inventoryTogglePressed = Input.IsActionPressed("Inventory_Toggle");
			if (!inventoryTogglePressed)
			{
				_inventoryToggleHeld = false;
				return;
			}

			if (_inventoryToggleHeld)
            {
				if (DebugMenuInput && Input.IsKeyPressed(Key.Tab))
				{
					GD.Print("[UIDebug] Inventory_Toggle is held; waiting for release before toggling again.");
				}
                return;
            }

			_inventoryToggleHeld = true;
			ulong currentFrame = Engine.GetProcessFrames();
			if (_lastInventoryToggleFrame == currentFrame)
			{
				if (DebugMenuInput)
				{
					GD.Print("[UIDebug] Inventory_Toggle ignored because this frame already toggled the player menu.");
				}
				return;
			}

			_lastInventoryToggleFrame = currentFrame;

			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] Inventory_Toggle detected. mainMenuVisible={_root.MainMenu?.Visible.ToString() ?? "null"} playerMenuVisible={_root.PlayerMenu?.Visible.ToString() ?? "null"}");
			}

            if (_root.MainMenu != null && _root.MainMenu.Visible)
            {
				if (DebugMenuInput)
				{
					GD.Print("[UIDebug] Inventory_Toggle ignored because MainMenu is visible.");
				}
                return;
            }

            _root.TogglePlayerMenu();
			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] After toggle call. playerMenuVisible={_root.PlayerMenu?.Visible.ToString() ?? "null"}");
			}
        }
    }
}
