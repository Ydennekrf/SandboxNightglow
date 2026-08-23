using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ethra.V1;
using Godot;
using Game.Interact;

namespace ethra.V1
{
	public partial class ButtonActivatedDoorway : Node2D, IInteractionPromptSource
	{

		[Export] public NodePath ButtonAssembly { get; set; }
		[Export] public NodePath GapBlockerAssembly { get; set; }
		[Export] public NodePath SceneTransferObject {  get; set; }

		[Export] public string TargetSceneKey { get; set; }
		[Export] public string TargetSpawnName { get; set; }

		[Export] public bool RequireInteract { get; set; }
		[Export] public NodePath AnimationPlayerPath { get; set; }
		[Export] public string ActivateAnimName { get; set; }
		[Export] public string InteractionVerb { get; set; } = "Activate";
		[Export] public string InteractionPromptText { get; set; } = "Press E to Activate";
		[Export] public int InteractionPriority { get; set; } = 0;
		public bool CanInteract => RequireInteract && !_activated;



		private bool _playerInside;
		private Area2D _button;
		private StaticBody2D _gapBlocker;
		private Area2D _sceneTransfer;
		private bool _activated;
		private AnimationPlayer _anim;

		public override void _Ready()
		{
			_anim = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
			_button = GetNodeOrNull<Area2D>(ButtonAssembly);
			_gapBlocker = GetNodeOrNull<StaticBody2D>(GapBlockerAssembly);
			_sceneTransfer = GetNodeOrNull<Area2D>(SceneTransferObject);

			if (_button == null)
				GD.PushError("Button Activated Doorway: Button Area not found.");

			if (_gapBlocker == null)
				GD.PushError("Button Activated Doorway: gap blocker not found.");

			if (_anim == null) GD.PushError("Button Activated Doorway: animation player not found.");
			if (ActivateAnimName == null) GD.PushError("Button Activated Doorway: no animation name found.");
			if (TargetSceneKey == null) GD.PushError("Button Activated Doorway: no target scene found.");
			if (TargetSpawnName == null) GD.PushError("Button Activated Doorway: No target spawn point found.");
			if (!_anim.HasAnimation(ActivateAnimName)) GD.PushError("Button Activated Doorway: animation player does not have this animation.");

			if (_sceneTransfer == null) GD.PushError("Button Activated Doorway: scene transfer object not found.");

			_gapBlocker.SetDeferred(CollisionObject2D.PropertyName.DisableMode, false);

			_button.BodyEntered += OnBodyEntered;
			_button.BodyExited += OnBodyExited;

			_sceneTransfer.BodyEntered += OnSceneTransfer;

		}

		public override void _Process(double delta)
		{
			if(!_playerInside || _activated || !RequireInteract || GameManager.Instance?.UI?.BlocksGameplayInput == true) return;

			if (Input.IsActionJustPressed("Interact"))
				Activate();
		}

		private void OnBodyEntered(Node2D body)
		{
			if(body is PlayerNode)
			{
				_playerInside = true;
				if (RequireInteract && !_activated)
				{
					PublishPrompt(InteractionPromptText);
				}
				if (!RequireInteract && !_activated)
					Activate();
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

		private void Activate()
		{
			if (_activated) return;
			_activated = true;
			PublishPrompt(string.Empty);

			_anim.Play(ActivateAnimName);
			_anim.AnimationFinished += OnAnimationFinished;
		}

		private void OnAnimationFinished(StringName animName)
		{
			if (animName != ActivateAnimName) return;

			_anim.AnimationFinished -= OnAnimationFinished;
			OpenGapNow();
		}

		private void OpenGapNow()
		{
			_gapBlocker.SetDeferred(CollisionObject2D.PropertyName.DisableMode, true);
		}

		private void OnSceneTransfer(Node2D body)
		{
			if (!_activated) return;
			if (body is not PlayerNode) return;

			if (string.IsNullOrEmpty(TargetSceneKey))
			{
				GD.PushWarning("ButtonActivatedDoorway: TargetSceneKey is empty.");
				return;
			}

			GameManager gm = GameManager.Instance;
			if (gm == null)
			{
				GD.PushError("ButtonActivatedDoorway: GameManager.Instance is null.");
				return;
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
