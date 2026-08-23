using Godot;

namespace ethra.V1
{
    public partial class MagicProjectile : Area2D
    {
        private const uint EnemyHurtBoxCollisionMask = 8;

        private AttackPayloadPacket _packet;
        private Vector2 _direction = Vector2.Right;
        private Vector2 _startPosition;
        private float _speed = 360f;
        private float _maxDistance = 288f;
        private bool _detonated;

        public override void _Ready()
        {
            CollisionLayer = 0;
            CollisionMask = EnemyHurtBoxCollisionMask;
            Monitoring = true;
            Monitorable = false;
            AreaEntered += OnAreaEntered;
        }

        public void Configure(AttackPayloadPacket packet, Vector2 origin, Vector2 direction)
        {
            _packet = packet;
            GlobalPosition = origin;
            _startPosition = origin;
            _direction = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Right;

            AttackPayloadResource payload = packet?.Payload;
            _speed = Mathf.Max(1f, payload?.ProjectileSpeed ?? _speed);
            _maxDistance = Mathf.Max(1f, payload?.ProjectileMaxDistance ?? _maxDistance);

            BuildCollision(Mathf.Max(1f, payload?.ProjectileRadius ?? 8f));
            BuildVisual(payload);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_detonated)
            {
                return;
            }

            GlobalPosition += _direction * _speed * (float)delta;
            ScanOverlappingHurtBoxes();
            if (GlobalPosition.DistanceTo(_startPosition) >= _maxDistance)
            {
                Detonate();
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            if (_detonated)
            {
                return;
            }

            HurtBox hurtBox = area as HurtBox ?? area.GetParentOrNull<HurtBox>();
            if (hurtBox == null || !hurtBox.HasDamageableOwner)
            {
                return;
            }

            Entity target = hurtBox.OwnerEntity;
            if (target == null || target == _packet?.Source)
            {
                return;
            }

            CombatManager combat = GameManager.Instance?.Combat;
            if (combat == null || !combat.CanHit(_packet.Source, target, _packet.Payload?.DeliveryShapeId ?? "ProjectileBolt"))
            {
                return;
            }

            combat.ResolveAttackPayloadHit(_packet, target, out _);
            Detonate();
        }

        private void ScanOverlappingHurtBoxes()
        {
            foreach (Area2D area in GetOverlappingAreas())
            {
                OnAreaEntered(area);
                if (_detonated)
                {
                    return;
                }
            }
        }

        private void BuildCollision(float radius)
        {
            CollisionShape2D shape = new()
            {
                Shape = new CircleShape2D { Radius = radius }
            };
            AddChild(shape);
        }

        private void BuildVisual(AttackPayloadResource payload)
        {
            ColorRect visual = new()
            {
                Color = MagicShapePreview.GetElementColor(ParseElement(payload?.ElementType)),
                Size = new Vector2(12f, 12f),
                Position = new Vector2(-6f, -6f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(visual);
        }

        private static ElementType ParseElement(string raw)
        {
            return !string.IsNullOrWhiteSpace(raw) && System.Enum.TryParse(raw, true, out ElementType element)
                ? element
                : ElementType.None;
        }

        private void Detonate()
        {
            _detonated = true;
            QueueFree();
        }
    }
}
