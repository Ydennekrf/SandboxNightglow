using Godot;

namespace ethra.V1
{
    [GlobalClass]
    public partial class EnemyDefinitionResource : Resource
    {
        [ExportGroup("Identity")]
        [Export] public string EnemyName = "TestEnemy";
        [Export] public int ExperienceReward = 25;
        [Export] public bool DisableOnDefeat = true;

        [ExportGroup("Stats")]
        [Export] public int MaxHealth = 100;
        [Export] public int StartingHealth = 100;
        [Export] public int Strength = 1;
        [Export] public int Dexterity = 1;
        [Export] public int Intelligence = 1;
        [Export] public int Spirit = 1;
        [Export] public int Vitality = 1;
        [Export] public int Luck = 1;

        [ExportGroup("Spawn")]
        [Export(PropertyHint.Enum, "Active,Timed,Proximity")] public int SpawnModeValue = (int)EnemySpawnMode.Timed;
        [Export] public float SpawnDelaySeconds = 1f;
        [Export] public float SpawnProximityDistance = 96f;

        [ExportGroup("Idle And Patrol")]
        [Export] public float IdleDurationSeconds = 1.25f;
        [Export] public float PatrolDurationSeconds = 2.5f;
        [Export] public float PatrolRadius = 96f;
        [Export] public Vector2 PatrolCenterOffset = Vector2.Zero;
        [Export] public float PatrolMoveSpeed = 45f;

        [ExportGroup("Pursue")]
        [Export] public float PursueMoveSpeed = 80f;
        [Export] public float DetectionRange = 128f;
        [Export] public float LeashRange = 192f;

        [ExportGroup("Attack")]
        [Export] public float AttackRange = 28f;
        [Export] public float AttackDamage = 6f;
        [Export] public string AttackDamageType = "Physical";
        [Export] public string AttackAbilityId = "EnemyBasicAttack";
        [Export] public float AttackCooldownSeconds = 1.1f;
        [Export] public float AttackWindupSeconds = 0.16f;
        [Export] public float AttackDurationSeconds = 0.45f;
        [Export] public float AttackActiveDurationSeconds = 0.12f;

        [ExportGroup("Hurt")]
        [Export] public float HurtDurationSeconds = 0.25f;

        [ExportGroup("Animation Keys")]
        [Export] public string SpawnAnimationKey = "Spawn";
        [Export] public string IdleAnimationKey = "Idle";
        [Export] public string PatrolAnimationKey = "Patrol";
        [Export] public string PursueAnimationKey = "Pursue";
        [Export] public string AttackAnimationKey = "Attack";
        [Export] public string HurtAnimationKey = "Hurt";
        [Export] public string DieAnimationKey = "Die";
    }
}
