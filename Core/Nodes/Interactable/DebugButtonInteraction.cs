using Godot;
using Game.Interact;

namespace ethra.V1
{
	public partial class DebugButtonInteraction : Area2D, IPlayerInteractable
	{
		[Export] public string InteractionVerb { get; set; } = "Activate";
		[Export] public string InteractionPromptText { get; set; } = "Press E to Activate";
		[Export] public int InteractionPriority { get; set; } = 0;
		[Export] public string NotificationText { get; set; } = "Debug button activated.";
		[Export] public NodePath EnemySpawnMarkerPath { get; set; }
		[Export] public string SpawnNotificationText { get; set; } = "Debug enemy spawned.";

		private PlayerNode _player;
		private int _spawnCount;
		private ulong _lastInteractionFrame;

		public bool CanInteract => _player != null;

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;
			BodyExited += OnBodyExited;
			AreaEntered += OnAreaEntered;
			AreaExited += OnAreaExited;
		}

		public override void _Process(double delta)
		{
			if (_player == null || GameManager.Instance?.UI?.BlocksGameplayInput == true || !Input.IsActionJustPressed("Interact"))
			{
				return;
			}

			BeginInteraction(_player);
		}

		public void BeginInteraction(PlayerNode player)
		{
			if (player == null || !CanInteract)
			{
				return;
			}

			ulong currentFrame = Engine.GetProcessFrames();
			if (_lastInteractionFrame == currentFrame)
			{
				return;
			}

			_lastInteractionFrame = currentFrame;

			Node2D spawnedEnemy = SpawnDebugEnemy();
			string notificationText = spawnedEnemy != null ? SpawnNotificationText : NotificationText;
			string floatingText = spawnedEnemy != null ? "Enemy Spawned" : "Activated";

			GameManager.Instance?.Publish(
				GameEvent.NotificationRequested,
				new NotificationRequest(notificationText, NotificationType.Info));
			GameManager.Instance?.Publish(
				GameEvent.FloatingTextRequested,
				new FloatingTextRequest
				{
					Text = floatingText,
					WorldTarget = this,
					Type = FloatingTextType.Debug
				});
			GD.Print($"[InteractionDebug] {notificationText}");
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is PlayerNode player)
			{
				_player = player;
				PublishPrompt(InteractionPromptText);
			}
		}

		private void OnBodyExited(Node2D body)
		{
			if (body == _player)
			{
				_player = null;
				PublishPrompt(string.Empty);
			}
		}

		private void OnAreaEntered(Area2D area)
		{
			if (area.GetParent() is PlayerNode player)
			{
				_player = player;
				PublishPrompt(InteractionPromptText);
			}
		}

		private void OnAreaExited(Area2D area)
		{
			if (area.GetParent() == _player)
			{
				_player = null;
				PublishPrompt(string.Empty);
			}
		}

		private void PublishPrompt(string text)
		{
			GameManager.Instance?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(text, this));
		}

		private Node2D SpawnDebugEnemy()
		{
			EnemySpawnMarker marker = ResolveSpawnMarker();
			if (marker == null)
			{
				return null;
			}

			_spawnCount++;
			marker.SpawnedEnemyName = $"ButtonSpawnEnemy{_spawnCount}";
			return marker.SpawnEnemy();
		}

		private EnemySpawnMarker ResolveSpawnMarker()
		{
			if (EnemySpawnMarkerPath != null && !EnemySpawnMarkerPath.IsEmpty)
			{
				EnemySpawnMarker configured = GetNodeOrNull<EnemySpawnMarker>(EnemySpawnMarkerPath);
				if (configured != null)
				{
					return configured;
				}

				GD.PushWarning($"{Name}: EnemySpawnMarkerPath '{EnemySpawnMarkerPath}' did not resolve.");
			}

			return GetTree()?.CurrentScene?.FindChild("EnemySpawnMarker", true, false) as EnemySpawnMarker;
		}
	}
}
