using Godot;

namespace ethra.V1
{
	public partial class FloatingTextLabel : Label
	{
		private float _duration = 0.85f;
		private float _riseDistance = 22f;
		private float _elapsed;
		private Vector2 _startPosition;
		private Color _startColor = Colors.White;

		public void Configure(string text, Color color, float durationSeconds, float riseDistance)
		{
			Text = text;
			_startColor = color;
			Modulate = color;
			_duration = Mathf.Max(0.05f, durationSeconds);
			_riseDistance = riseDistance;
		}

		public override void _Ready()
		{
			_startPosition = Position;
			HorizontalAlignment = HorizontalAlignment.Center;
			VerticalAlignment = VerticalAlignment.Center;
			MouseFilter = MouseFilterEnum.Ignore;
			ZIndex = 100;
		}

		public override void _Process(double delta)
		{
			_elapsed += (float)delta;
			float t = Mathf.Clamp(_elapsed / _duration, 0f, 1f);

			Position = _startPosition + new Vector2(0f, -_riseDistance * t);
			Modulate = new Color(_startColor.R, _startColor.G, _startColor.B, 1f - t);

			if (_elapsed >= _duration)
			{
				QueueFree();
			}
		}
	}
}
