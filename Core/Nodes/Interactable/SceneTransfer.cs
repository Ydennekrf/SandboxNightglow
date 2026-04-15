using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace ethra.V1
{
	public partial class SceneTransfer : Area2D
	{
		[Export] public string TargetSceneKey { get; set; }
		[Export] public string TargetSpawnName { get; set; }

		public override void _Ready()
		{
			BodyEntered += OnBodyEntered;
		}

		private void OnBodyEntered(Node2D body)
		{
			if (body is not PlayerNode) return;

			var gm = GameManager.Instance;
			if (gm == null)
			{
				GD.PushError("SceneExit: GameManager.Instance was null.");
				return;
			}

			gm.Scene.GoToScene(TargetSceneKey);

			gm.CallDeferred(nameof(GameManager.SpawnPlayerAtMarker), TargetSpawnName);
		}
	}
}
