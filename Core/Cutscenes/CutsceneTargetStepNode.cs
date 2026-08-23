using Godot;

namespace ethra.V1
{
    public abstract partial class CutsceneTargetStepNode : CutsceneStepNode
    {
        [ExportGroup("Target")]
        [Export] public NodePath TargetEntityPath { get; set; }
        [Export] public string TargetEntityId { get; set; } = string.Empty;

        protected Node ResolveTarget(CutsceneContext context)
        {
            Node target = ResolveConfiguredNode(TargetEntityPath);
            if (target != null)
            {
                return target;
            }

            target = context?.GetSpawnedEntity(TargetEntityId);
            if (target != null)
            {
                return target;
            }

            if (!string.IsNullOrWhiteSpace(TargetEntityId))
            {
                target = GetTree()?.GetFirstNodeInGroup(TargetEntityId);
            }

            return target;
        }
    }
}
