using Godot;

namespace ethra.V1
{
    public partial class HurtBox : Area2D
    {
        [Signal]
        public delegate void DamagePopupShownEventHandler(int amount);

        [Export] public NodePath PopupAnchorPath { get; set; }

        public Entity OwnerEntity { get; private set; }
        public bool HasDamageableOwner => OwnerEntity is IStats;
        public ulong LastPopupPhysicsFrame { get; private set; }

        public override void _Ready()
        {
            AddToGroup("Hurtbox");
        }

        public void Bind(Entity owner)
        {
            OwnerEntity = owner;
        }

        public void ShowDamagePopup(int amount)
        {
            LastPopupPhysicsFrame = Engine.GetPhysicsFrames();
            Node2D anchor = ResolvePopupAnchor();
            if (anchor != null)
            {
                GameManager.Instance?.Publish(
                    GameEvent.FloatingTextRequested,
                    new FloatingTextRequest
                    {
                        Text = amount.ToString(),
                        WorldTarget = anchor,
                        Type = FloatingTextType.Damage
                    });
            }

            EmitSignal(SignalName.DamagePopupShown, amount);
        }

        private Node2D ResolvePopupAnchor()
        {
            if (PopupAnchorPath != null && !PopupAnchorPath.IsEmpty)
            {
                Node2D configured = GetNodeOrNull<Node2D>(PopupAnchorPath);
                if (configured != null)
                {
                    return configured;
                }
            }

            return GetParent<Node2D>() ?? this;
        }
    }
}
