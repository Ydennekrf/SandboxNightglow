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
            Node anchor = ResolvePopupAnchor();
            var popup = new DamagePopupLabel
            {
                Text = amount.ToString(),
                Position = new Vector2(-12f, -10f),
                Size = new Vector2(24f, 16f),
                Modulate = new Color(1f, 0.92f, 0.35f, 1f)
            };

            anchor.AddChild(popup);
            EmitSignal(SignalName.DamagePopupShown, amount);
        }

        private Node ResolvePopupAnchor()
        {
            if (PopupAnchorPath != null && !PopupAnchorPath.IsEmpty)
            {
                Node configured = GetNodeOrNull<Node>(PopupAnchorPath);
                if (configured != null)
                {
                    return configured;
                }
            }

            return GetParent() ?? this;
        }
    }
}
