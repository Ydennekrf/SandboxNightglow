using Godot;

namespace ethra.V1
{
	public partial class FloatingTextManager : Control
	{
		[Export] public Vector2 DefaultLabelSize { get; set; } = new(128f, 24f);
		[Export] public PackedScene FloatingTextLabelScene { get; set; }

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Ignore;
			SetAnchorsPreset(LayoutPreset.FullRect);
			GameManager.Instance?.Subscribe<FloatingTextRequest>(GameEvent.FloatingTextRequested, ShowFloatingText);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Subscribe<FloatingTextRequest>(GameEvent.FloatingTextRequested, ShowFloatingText);
			}
		}

		public override void _ExitTree()
		{
			GameManager.Instance?.Unsubscribe<FloatingTextRequest>(GameEvent.FloatingTextRequested, ShowFloatingText);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Unsubscribe<FloatingTextRequest>(GameEvent.FloatingTextRequested, ShowFloatingText);
			}
		}

		public void ShowDamageNumber(int amount, Node2D target)
		{
			ShowFloatingText(new FloatingTextRequest
			{
				Text = amount.ToString(),
				WorldTarget = target,
				Type = FloatingTextType.Damage
			});
		}

		public void ShowPickupText(string text, Node2D target)
		{
			ShowFloatingText(new FloatingTextRequest
			{
				Text = text,
				WorldTarget = target,
				Type = FloatingTextType.Pickup,
				DurationSeconds = 1.1f
			});
		}

		public void ShowFloatingText(FloatingTextRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Text))
			{
				return;
			}

			Vector2 screenPosition = ResolveScreenPosition(request);
			Color color = request.TextColor ?? GetDefaultColor(request.Type);
			FloatingTextLabel label = CreateFloatingTextLabel();
			if (label == null)
			{
				return;
			}

			label.Position = screenPosition - DefaultLabelSize * 0.5f;
			label.Size = DefaultLabelSize;

			label.Configure(request.Text, color, request.DurationSeconds, request.RiseDistance);
			AddChild(label);
		}

		private FloatingTextLabel CreateFloatingTextLabel()
		{
			if (FloatingTextLabelScene != null)
			{
				FloatingTextLabel sceneLabel = FloatingTextLabelScene.InstantiateOrNull<FloatingTextLabel>();
				if (sceneLabel != null)
				{
					return sceneLabel;
				}

				GD.PushWarning("FloatingTextManager: FloatingTextLabelScene root is not a FloatingTextLabel; using fallback label.");
			}

			return new FloatingTextLabel();
		}

		private Vector2 ResolveScreenPosition(FloatingTextRequest request)
		{
			Vector2 screenPosition = Vector2.Zero;
			if (request.WorldTarget != null && IsInstanceValid(request.WorldTarget))
			{
				screenPosition = request.WorldTarget.GetGlobalTransformWithCanvas().Origin;
			}
			else if (request.HasWorldPosition)
			{
				screenPosition = GetViewport().GetCanvasTransform() * request.WorldPosition;
			}

			return screenPosition + request.ScreenOffset;
		}

		private static Color GetDefaultColor(FloatingTextType type)
		{
			return type switch
			{
				FloatingTextType.Damage => new Color(1f, 0.92f, 0.35f, 1f),
				FloatingTextType.Healing => new Color(0.45f, 1f, 0.55f, 1f),
				FloatingTextType.Pickup => new Color(0.85f, 1f, 0.65f, 1f),
				FloatingTextType.Debug => new Color(0.7f, 0.9f, 1f, 1f),
				_ => Colors.White
			};
		}
	}
}
