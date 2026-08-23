using Godot;

namespace ethra.V1
{
    
    public partial class EnemySpawnMarker : Marker2D
    {
        [Export] public PackedScene EnemyScene;
        [Export] public EnemyDefinitionResource EnemyDefinitionOverride;
        [Export] public NodePath EnemyParentPath = "../Enemies";
        [Export] public string SpawnedEnemyName = "TestEnemy";

        
        [Export(PropertyHint.Enum, "Active,Timed,Proximity")] public int SpawnModeValue = (int)EnemySpawnMode.Timed;
        [Export] public float SpawnDelaySeconds = 1f;
        [Export] public float SpawnProximityDistance = 96f;
        [Export] public float PatrolRadius = 96f;
        [Export] public float DetectionRange = 128f;
        [Export] public float LeashRange = 192f;
        [Export] public float AttackRange = 28f;

        public EnemySpawnMode SpawnMode => (EnemySpawnMode)SpawnModeValue;

        public Node2D SpawnEnemy()
        {
            if (EnemyScene == null)
            {
                GD.PushError($"{Name}: EnemyScene is not assigned.");
                return null;
            }

            Node2D enemy = EnemyScene.Instantiate<Node2D>();
            enemy.Name = string.IsNullOrWhiteSpace(SpawnedEnemyName) ? "Enemy" : SpawnedEnemyName;
            enemy.GlobalPosition = GlobalPosition;

            if (enemy is TestEnemyNode testEnemy)
            {
                if (EnemyDefinitionOverride != null)
                {
                    testEnemy.EnemyDefinition = EnemyDefinitionOverride;
                }

                testEnemy.ApplyEnemyDefinition();
                testEnemy.SpawnModeValue = SpawnModeValue;
                testEnemy.SpawnDelaySeconds = SpawnDelaySeconds;
                testEnemy.SpawnProximityDistance = SpawnProximityDistance;
                testEnemy.PatrolRadius = PatrolRadius;
                testEnemy.DetectionRange = DetectionRange;
                testEnemy.LeashRange = LeashRange;
                testEnemy.AttackRange = AttackRange;
            }

            Node parent = ResolveEnemyParent();
            parent.AddChild(enemy);
            return enemy;
        }

        private Node ResolveEnemyParent()
        {
            if (EnemyParentPath != null && !EnemyParentPath.IsEmpty)
            {
                Node configured = GetNodeOrNull<Node>(EnemyParentPath);
                if (configured != null)
                {
                    return configured;
                }
            }

            return GetParent() ?? GetTree().CurrentScene;
        }
    }
}
