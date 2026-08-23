using Godot;

namespace ethra.V1
{
	public partial class NotificationManager : Control
	{
		[Export] public float DefaultDurationSeconds { get; set; } = 2.4f;
		[Export] public int MaxVisibleNotifications { get; set; } = 4;
		[Export] public NodePath StackPath { get; set; } = "NotificationStack";

		private VBoxContainer _stack;

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Ignore;
			SetAnchorsPreset(LayoutPreset.FullRect);
			ResolveNodes();

			GameManager.Instance?.Subscribe<NotificationRequest>(GameEvent.NotificationRequested, ShowNotification);
			GameManager.Instance?.Subscribe<string>(GameEvent.ToastMessage, ShowInfoNotification);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Subscribe<NotificationRequest>(GameEvent.NotificationRequested, ShowNotification);
				global::EventManager.I.Subscribe<string>(GameEvent.ToastMessage, ShowInfoNotification);
			}
		}

		public override void _ExitTree()
		{
			GameManager.Instance?.Unsubscribe<NotificationRequest>(GameEvent.NotificationRequested, ShowNotification);
			GameManager.Instance?.Unsubscribe<string>(GameEvent.ToastMessage, ShowInfoNotification);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Unsubscribe<NotificationRequest>(GameEvent.NotificationRequested, ShowNotification);
				global::EventManager.I.Unsubscribe<string>(GameEvent.ToastMessage, ShowInfoNotification);
			}
		}

		public void ShowNotification(string message)
		{
			ShowNotification(new NotificationRequest(message, NotificationType.Info));
		}

		public void ShowNotification(string message, NotificationType type)
		{
			ShowNotification(new NotificationRequest(message, type));
		}

		public void ShowQuestNotification(string message) => ShowNotification(message, NotificationType.Quest);
		public void ShowSaveNotification(string message) => ShowNotification(message, NotificationType.Save);
		public void ShowErrorNotification(string message) => ShowNotification(message, NotificationType.Error);

		private void ShowInfoNotification(string message)
		{
			ShowNotification(new NotificationRequest(message, NotificationType.Info));
		}

		private void ShowNotification(NotificationRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Message) || _stack == null)
			{
				return;
			}

			int maxVisible = Mathf.Max(1, MaxVisibleNotifications);
			while (_stack.GetChildCount() >= maxVisible)
			{
				Node oldest = _stack.GetChildOrNull<Node>(0);
				if (oldest == null)
				{
					break;
				}

				oldest.QueueFree();
			}

			Label label = new()
			{
				Text = request.Message,
				CustomMinimumSize = new Vector2(240f, 28f),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = MouseFilterEnum.Ignore,
				Modulate = GetTypeColor(request.Type)
			};

			PanelContainer panel = new()
			{
				CustomMinimumSize = new Vector2(260f, 34f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			panel.AddChild(label);
			_stack.AddChild(panel);

			Tween tween = CreateTween();
			tween.TweenInterval(Mathf.Max(0.1f, DefaultDurationSeconds));
			tween.TweenProperty(panel, "modulate:a", 0.0f, 0.25f);
			tween.TweenCallback(Callable.From(panel.QueueFree));
		}

		private void BuildStack()
		{
			if (_stack != null)
			{
				return;
			}

			_stack = new VBoxContainer
			{
				MouseFilter = MouseFilterEnum.Ignore,
				Alignment = BoxContainer.AlignmentMode.Begin
			};
			_stack.SetAnchorsPreset(LayoutPreset.TopRight);
			_stack.OffsetLeft = -292f;
			_stack.OffsetTop = 16f;
			_stack.OffsetRight = -16f;
			_stack.OffsetBottom = 160f;
			AddChild(_stack);
		}

		private void ResolveNodes()
		{
			_stack = GetNodeOrNull<VBoxContainer>(StackPath);
			if (_stack == null)
			{
				GD.PushWarning("NotificationManager: packed scene stack path is invalid; building fallback notification stack.");
				BuildStack();
			}
		}

		private static Color GetTypeColor(NotificationType type)
		{
			return type switch
			{
				NotificationType.Quest => new Color(0.8f, 0.95f, 1f, 1f),
				NotificationType.Save => new Color(0.65f, 1f, 0.72f, 1f),
				NotificationType.Error => new Color(1f, 0.55f, 0.55f, 1f),
				NotificationType.Loot => new Color(1f, 0.95f, 0.58f, 1f),
				_ => Colors.White
			};
		}
	}
}
