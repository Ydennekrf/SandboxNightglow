using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public abstract partial class CutsceneStepNode : Node
    {
        [ExportGroup("Cutscene Step")]
        [Export] public string StepId { get; set; } = string.Empty;
        [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
        [Export] public bool IsBlocking { get; set; } = true;

        public abstract Task ExecuteAsync(CutsceneContext context);

        protected Node ResolveConfiguredNode(NodePath path)
        {
            if (IsEmptyNodePath(path))
            {
                return null;
            }

            return GetNodeOrNull<Node>(path);
        }

        protected T ResolveConfiguredNode<T>(NodePath path) where T : Node
        {
            if (IsEmptyNodePath(path))
            {
                return null;
            }

            return GetNodeOrNull<T>(path);
        }

        protected async Task WaitSeconds(float seconds)
        {
            SceneTree tree = GetTree();
            if (tree == null || seconds <= 0f)
            {
                return;
            }

            await ToSignal(tree.CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        }

        internal static bool IsEmptyNodePath(NodePath path)
        {
            return path == null || path.IsEmpty || string.IsNullOrWhiteSpace(path.ToString());
        }

        protected void Warn(string message)
        {
            GD.PushWarning($"{GetType().Name} '{Name}': {message}");
        }
    }
}
