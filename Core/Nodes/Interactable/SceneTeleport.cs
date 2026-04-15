using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace ethra.V1
{
	public partial class SceneTeleport : Area2D
	{

		[Export] public NodePath WorldRootPath { get; set; }
		[Export] public string TargetSpawnName { get; set; }
		[Export] public bool RequireInteract { get; set; } = false;

		private WorldSceneRoot _world;
		private bool _playerInside;

		public override void _Ready()
		{
			_world = GetNodeOrNull<WorldSceneRoot>(WorldRootPath);

			BodyEntered += OnBodyEntered;

			BodyExited += OnBodyExited;
		}

		public override void _Process(double delta)
		{
			if (!RequireInteract || !_playerInside) return;

			if (Input.IsActionJustPressed("Interact"))
				TeleportPlayer();
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is PlayerNode)
			{
				_playerInside = true;
				if (!RequireInteract) TeleportPlayer();
			}
		}

		private void OnBodyExited(Node2D body)
		{
			if (body is PlayerNode)
				_playerInside = false;
		}

		private void TeleportPlayer()
		{
			if (_world == null)
			{
				GD.PushError("DoorTeleport: WorldSceneRoot not found (check WorldRootPath).");
				return;
			}

			var spawn = _world.GetSpawn(TargetSpawnName);
			if (spawn == null)
			{
				GD.PushError($"DoorTeleport: Spawn '{TargetSpawnName}' not found.");
				return;
			}

			var playerNode = GetTree().CurrentScene.GetNodeOrNull<PlayerNode>("Entities/Player/PlayerNode");
			if (playerNode == null)
			{
				// If your player node path differs, update this lookup (or export it).
				GD.PushError("DoorTeleport: PlayerNode not found.");
				return;
			}

			playerNode.GlobalPosition = spawn.GlobalPosition;
			playerNode.Velocity = Vector2.Zero; // stop physics carry
		}
	}
}
