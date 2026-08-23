using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Game.Interact;

namespace ethra.V1
{
	public partial class SceneTransfer : Area2D, IInteractionPromptSource
	{
		[Export] public string TargetSceneKey { get; set; }
		[Export] public string TargetSpawnName { get; set; }
		[Export] public bool RequireInteract { get; set; } = false;
		[Export] public string TransitionMessage { get; set; } = string.Empty;
		[Export] public string InteractionVerb { get; set; } = "Travel";
		[Export] public string InteractionPromptText { get; set; } = "Press E to Travel";
		[Export] public int InteractionPriority { get; set; } = 0;
		public bool CanInteract => RequireInteract && !_transitioning;

		private bool _playerInside;
		private bool _transitioning;

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;
			BodyExited += OnBodyExited;
		}

		public override void _Process(double delta)
		{
			if (!RequireInteract || !_playerInside || _transitioning || GameManager.Instance?.UI?.BlocksGameplayInput == true)
			{
				return;
			}

			if (Input.IsActionJustPressed("Interact"))
			{
				Transfer();
			}
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is not PlayerNode) return;

			_playerInside = true;
			if (RequireInteract)
			{
				PublishPrompt(InteractionPromptText);
			}
			if (!RequireInteract)
			{
				Transfer();
			}
		}

		private void OnBodyExited(Node2D body)
		{
			if (body is PlayerNode)
			{
				_playerInside = false;
				PublishPrompt(string.Empty);
			}
		}

		private void Transfer()
		{
			if (_transitioning)
			{
				return;
			}

			_transitioning = true;
			PublishPrompt(string.Empty);

			var gm = GameManager.Instance;
			if (gm == null)
			{
				GD.PushError("SceneExit: GameManager.Instance was null.");
				_transitioning = false;
				return;
			}

			if (!string.IsNullOrWhiteSpace(TransitionMessage))
			{
				GD.Print(TransitionMessage);
				gm.Publish(GameEvent.NotificationRequested, new NotificationRequest(TransitionMessage, NotificationType.Info));
			}

			gm.Scene.GoToScene(TargetSceneKey);

			gm.CallDeferred(nameof(GameManager.SpawnPlayerAtMarker), TargetSpawnName);
		}

		private void PublishPrompt(string text)
		{
			GameManager.Instance?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(text, this));
		}
	}
}
