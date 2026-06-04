#if TOOLS
using Godot;

namespace ethra.V1
{
    [Tool]
    public partial class DialogGraphEditorPlugin : EditorPlugin
    {
        private const string DockScenePath = "res://PackedScenes/Tools/DialogGraphEditor.tscn";

        private Control _dock;

        public override string _GetPluginName() => "Dialog Graph Editor";

        public override void _EnterTree()
        {
            PackedScene dockScene = ResourceLoader.Load<PackedScene>(DockScenePath);
            if (dockScene == null)
            {
                GD.PushWarning($"DialogGraphEditorPlugin: failed to load dock scene at {DockScenePath}.");
                return;
            }

            _dock = dockScene.Instantiate<Control>();
            _dock.Name = "Dialog Graph";
            AddControlToDock(DockSlot.RightUl, _dock);
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
