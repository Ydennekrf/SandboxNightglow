using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class CutsceneSpawnNode : CutsceneStepNode
    {
        [ExportGroup("Spawn")]
        [Export] public PackedScene EntityScene { get; set; }
        [Export] public NodePath SpawnParentPath { get; set; }
        [Export] public NodePath SpawnMarkerPath { get; set; }
        [Export] public Vector2 SpawnPosition { get; set; } = Vector2.Zero;
        [Export] public string SpawnedEntityId { get; set; } = string.Empty;
        [Export] public string AddToGroupName { get; set; } = string.Empty;
        [Export] public bool StoreSpawnedEntityForLaterSteps { get; set; } = true;

        public override Task ExecuteAsync(CutsceneContext context)
        {
            if (EntityScene == null)
            {
                Warn("EntityScene is not assigned; skipping spawn.");
                return Task.CompletedTask;
            }

            Node entity = EntityScene.Instantiate();
            if (!string.IsNullOrWhiteSpace(SpawnedEntityId))
            {
                entity.Name = SpawnedEntityId;
            }

            Node parent = ResolveSpawnParent();
            parent.AddChild(entity);

            if (entity is Node2D node2D)
            {
                Marker2D marker = ResolveConfiguredNode<Marker2D>(SpawnMarkerPath);
                if (marker != null)
                {
                    node2D.GlobalPosition = marker.GlobalPosition;
                }
                else
                {
                    node2D.GlobalPosition = SpawnPosition;
                    if (SpawnPosition == Vector2.Zero)
                    {
                        Warn("SpawnMarkerPath is empty or missing; spawned at Vector2.Zero fallback.");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(AddToGroupName))
            {
                entity.AddToGroup(AddToGroupName);
            }

            if (StoreSpawnedEntityForLaterSteps)
            {
                context?.RegisterSpawnedEntity(SpawnedEntityId, entity);
            }

            context?.Log($"Spawned '{entity.Name}' under '{parent.GetPath()}'.");
            return Task.CompletedTask;
        }

        private Node ResolveSpawnParent()
        {
            Node configured = ResolveConfiguredNode(SpawnParentPath);
            if (configured != null)
            {
                return configured;
            }

            Node currentScene = GetTree()?.CurrentScene;
            Node fallback = currentScene?.GetNodeOrNull<Node>("World/Entities/NPCs")
                ?? currentScene?.GetNodeOrNull<Node>("World/Entities")
                ?? currentScene
                ?? GetParent();

            if (fallback == null)
            {
                Warn("No spawn parent found; using this step node as parent.");
                return this;
            }

            if (IsEmptyNodePath(SpawnParentPath))
            {
                Warn($"SpawnParentPath is empty; using '{fallback.GetPath()}'.");
            }

            return fallback;
        }

        public override string[] _GetConfigurationWarnings()
        {
            return EntityScene == null
                ? new[] { "EntityScene is required." }
                : System.Array.Empty<string>();
        }
    }
}
