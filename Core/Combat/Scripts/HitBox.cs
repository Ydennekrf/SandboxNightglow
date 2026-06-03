using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public partial class HitBox : Area2D
    {
        [Export] public float DamageAmount { get; set; } = 1f;
        [Export] public string DamageType { get; set; } = "Physical";
        [Export] public string AbilityId { get; set; } = "HitBox";

        public Entity SourceEntity { get; private set; }
        public AttackPayloadPacket CurrentPacket { get; private set; }
        public bool IsActive { get; private set; }

        private readonly HashSet<Entity> _hitTargets = new();

        public override void _Ready()
        {
            AreaEntered += OnAreaEntered;
        }

        public void Bind(Entity source)
        {
            SourceEntity = source;
        }

        public void ConfigureAttack(AttackPayloadPacket packet)
        {
            CurrentPacket = packet;
            if (packet?.Payload == null)
            {
                return;
            }

            DamageAmount = Mathf.Max(1f, packet.Payload.BasePower);
            DamageType = packet.Payload.DamageType;
            AbilityId = string.IsNullOrWhiteSpace(packet.Payload.DeliveryShapeId)
                ? "HitBox"
                : packet.Payload.DeliveryShapeId;
        }

        public void Activate()
        {
            IsActive = true;
            _hitTargets.Clear();
            ScanOverlappingHurtBoxes();
        }

        public void Deactivate()
        {
            IsActive = false;
            _hitTargets.Clear();
        }

        private void OnAreaEntered(Area2D area)
        {
            if (!IsActive)
            {
                return;
            }

            TryApplyTo(area);
        }

        private void ScanOverlappingHurtBoxes()
        {
            foreach (Area2D area in GetOverlappingAreas())
            {
                TryApplyTo(area);
            }
        }

        private void TryApplyTo(Area2D area)
        {
            HurtBox hurtBox = area as HurtBox ?? area.GetParentOrNull<HurtBox>();
            if (hurtBox == null || !hurtBox.HasDamageableOwner)
            {
                return;
            }

            Entity target = hurtBox.OwnerEntity;
            if (target == null || target == SourceEntity || _hitTargets.Contains(target))
            {
                return;
            }

            CombatManager combat = GameManager.Instance?.Combat;
            if (combat == null || !combat.CanHit(SourceEntity, target, AbilityId))
            {
                return;
            }

            int before = ((IStats)target).CurHP;
            if (CurrentPacket?.Payload != null)
            {
                combat.ResolveAttackPayloadHit(CurrentPacket, target, out _);
            }
            else
            {
                combat.DealDamage(target, DamageAmount, DamageType, SourceEntity);
            }
            int after = ((IStats)target).CurHP;
            int actualDamage = Mathf.Max(0, before - after);

            _hitTargets.Add(target);

            if (actualDamage <= 0)
            {
                return;
            }

            if (CurrentPacket?.Payload == null)
            {
                hurtBox.ShowDamagePopup(actualDamage);
                CombatFeedbackBus.EmitHitResolved(SourceEntity, target, actualDamage, false, DamageType, string.Empty);
            }
        }
    }
}
