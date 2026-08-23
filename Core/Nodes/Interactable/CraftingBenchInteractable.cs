using Godot;
using Game.Interact;

namespace ethra.V1
{
	public partial class CraftingBenchInteractable : Area2D, IInteractionPromptSource
	{
		[Export] public string StationType { get; set; } = "Workbench";
		[Export] public string InteractionVerb { get; set; } = "Craft";
		[Export] public string InteractionPromptText { get; set; } = "Press E to Craft";
		[Export] public int InteractionPriority { get; set; } = 1;

		private PlayerNode _player;

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

			GameManager gameManager = GameManager.Instance;
			if (gameManager?.UI == null)
			{
				GD.PushWarning("CraftingBenchInteractable: UIManager is unavailable.");
				return;
			}

			gameManager.UI.OpenCraftingPanel(StationType);
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
	}
}
