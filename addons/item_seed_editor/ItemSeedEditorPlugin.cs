#if TOOLS
using Godot;

namespace ethra.V1
{
	[Tool]
	public partial class ItemSeedEditorPlugin : EditorPlugin
	{
		private ItemSeedEditorDock _dock;

		public override string _GetPluginName() => "Item Seed Editor";

		public override void _EnterTree()
		{
			_dock = new ItemSeedEditorDock
			{
				Name = "Item Seed Editor",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				SizeFlagsVertical = Control.SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(420, 620)
			};
			_dock.SetAnchorsPreset(Control.LayoutPreset.FullRect);

			AddControlToDock(DockSlot.RightBl, _dock);
		}

		public override void _ExitTree()
		{
			if (_dock == null)
			{
				return;
			}

			RemoveControlFromDocks(_dock);
			_dock.QueueFree();
			_dock = null;
		}
	}
}
#endif
