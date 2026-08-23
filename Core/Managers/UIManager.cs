using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1
{
	/// <summary>
	/// Bridges manager-level UI requests to the active scene's UIRoot.
	/// </summary>
	/// <remarks>
	/// UIManager should coordinate HUD/menu visibility and refreshable UI nodes. Specific panel behavior
	/// belongs in the panel scripts under Core/UI/Scripts.
	/// </remarks>
	public partial class UIManager : IResolveable, IUserInterface
	{
		private const bool DebugMenuInput = true;
		private int _resolveOrder = 100;
		private List<IUIRefresh> _uiNodeList;
		private bool _inventoryToggleHeld;
		private ulong _lastInventoryToggleFrame = ulong.MaxValue;
		public int ResolveOrder => _resolveOrder;
		/// <summary>
		/// True when a UIRoot has been bound for the current scene.
		/// </summary>
		public bool HasRoot => _root != null;
		/// <summary>
		/// True when the player menu is visible and gameplay input should usually pause.
		/// </summary>
		public bool IsPlayerMenuVisible => _root?.IsPlayerMenuVisible == true;
		/// <summary>
		/// True when any active UI panel should block world interaction input.
		/// </summary>
		public bool BlocksGameplayInput => _root?.BlocksGameplayInput == true;

		public List<IUIRefresh> UINodeList { get => _uiNodeList; set => _uiNodeList = value; }

		private UIRoot _root;

		/// <summary>
		/// Binds this manager to the current scene's UIRoot and refreshes player HUD binding.
		/// </summary>
		public void Initialize(UIRoot root)
		{
			_root = root;
			_uiNodeList = new List<IUIRefresh>();
			BindPlayerHud(GameManager.Instance?.GameState?.GetPlayer());
			if (DebugMenuInput)
			{
				GD.Print($"[UIDebug] UIManager initialized. root={_root?.Name} playerMenu={_root?.PlayerMenu?.GetPath()} mainMenu={_root?.MainMenu?.GetPath()}");
			}
		}

		/// <summary>
		/// Shows gameplay HUD only and binds it to the current player when available.
		/// </summary>
		public void ShowOnlyHud()
		{
			BindPlayerHud(GameManager.Instance?.GameState?.GetPlayer());
			_root?.ShowOnlyHud();
		}
		public void ShowOnlyMainMenu() => _root?.ShowOnlyMainMenu();
		public void BindPlayerHud(Player player) => _root?.PlayerHud?.BindPlayer(player);
		public void ShowPlayerMenu(bool show) => _root?.ShowPlayerMenu(show);
        public void TogglePlayerMenu() => _root?.TogglePlayerMenu();
		public void OpenCraftingPanel(string stationType) => _root?.OpenCraftingPanel(stationType);
		public void CloseCraftingPanel() => _root?.CloseCraftingPanel();
		public bool CloseTopGameplayPanel() => _root?.CloseTopGameplayPanel() == true;

		/// <summary>
		/// Registers a UI control that participates in manager-driven refresh ticks.
		/// </summary>
        public void Register(IUIRefresh ui)
		{
			if (!_uiNodeList.Contains(ui))
				_uiNodeList.Add(ui);
		}

		/// <summary>
		/// Refreshes registered UI nodes that mark themselves dirty.
		/// </summary>
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

		/// <summary>
		/// Polls UI input shortcuts and refreshes dirty UI nodes.
		/// </summary>
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
