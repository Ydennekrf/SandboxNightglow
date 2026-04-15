using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	public partial class PlayerNode : CharacterBody2D
	{
		private Player _player;
		private AnimationPlayer _anim;
		private GameManager _gm;
		private string _lastAnim = "";

		#region Sprites

	
		public Sprite2D _wepUpDraw;
		public Sprite2D _wepDownDraw;
		public Sprite2D _wepUpStow;
		public Sprite2D _wepDownStow;
		public Sprite2D _hair;
		public Sprite2D _clothes;
		public Sprite2D _body;
		public Sprite2D _notify;
		public Sprite2D _overlay;

		#endregion


		public override void _Ready()
		{
			_anim = GetNodeOrNull<AnimationPlayer>("Actions");
			_gm = GameManager.Instance;


		}

		public void Bind(Player player)
		{
			_player = player ?? throw new ArgumentNullException(nameof(player));
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			_player.MoveInput = Input.GetVector("Left", "Right", "Up", "Down");
			_player.Tick(dt);

			Velocity = _player.DesiredVelocity;

			MoveAndSlide();




			var animName = _player.RequestedAnimation;
			if (_anim != null && !string.IsNullOrEmpty(animName) && animName != _lastAnim)
			{
				if (_anim.HasAnimation(animName))
				{
					_anim.Play(animName);
					_lastAnim = animName;
				}
				else
				{
					GD.PushWarning($"Animation not found: '{animName}'");
				}
			}
		}
	}
}
