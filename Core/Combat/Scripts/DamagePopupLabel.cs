using Godot;

namespace ethra.V1
{
    public partial class DamagePopupLabel : Label
    {
        private const float LifetimeSeconds = 0.65f;
        private const float RiseDistance = 18f;

        private float _elapsed;
        private Vector2 _startPosition;

        public override void _Ready()
        {
            _startPosition = Position;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta)
        {
            _elapsed += (float)delta;
            float t = Mathf.Clamp(_elapsed / LifetimeSeconds, 0f, 1f);

            Position = _startPosition + new Vector2(0f, -RiseDistance * t);
            Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1f - t);

            if (_elapsed >= LifetimeSeconds)
            {
                QueueFree();
            }
        }
    }
}
