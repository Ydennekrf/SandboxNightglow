using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	public partial class PlayerNode : CharacterBody2D
	{
		private const bool DebugWeaponVisuals = true;
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
			_wepUpDraw = GetNodeOrNull<Sprite2D>("Sprites/WepUpDraw");
			_wepDownDraw = GetNodeOrNull<Sprite2D>("Sprites/WepDownDraw");
			_wepUpStow = GetNodeOrNull<Sprite2D>("Sprites/WepUpStow");
			_wepDownStow = GetNodeOrNull<Sprite2D>("Sprites/WepDownStow");
			_hair = GetNodeOrNull<Sprite2D>("Sprites/Hair");
			_clothes = GetNodeOrNull<Sprite2D>("Sprites/Clothes");
			_body = GetNodeOrNull<Sprite2D>("Sprites/Body");
			_overlay = GetNodeOrNull<Sprite2D>("Sprites/Overlay");

			if (_gm != null)
			{
				ApplyWeaponSprites(_gm.WepUpDraw, _gm.WepDownDraw, _gm.WepUpStow, _gm.WepDownStow);
			}
		}

		public void Bind(Player player)
		{
			_player = player ?? throw new ArgumentNullException(nameof(player));
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			_player.MoveInput = Input.GetVector("Left", "Right", "Up", "Down");
			_player.RunPressed = Input.IsActionPressed("Run");
			_player.DodgePressed = Input.IsActionJustPressed("Dodge");
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

		public void ApplyWeaponSprites(Texture2D upDraw, Texture2D downDraw, Texture2D upStow, Texture2D downStow)
		{
			if (_wepUpDraw != null)
			{
				_wepUpDraw.Texture = upDraw;
			}

			if (_wepDownDraw != null)
			{
				_wepDownDraw.Texture = downDraw;
			}

			if (_wepUpStow != null)
			{
				_wepUpStow.Texture = upStow;
			}

			if (_wepDownStow != null)
			{
				_wepDownStow.Texture = downStow;
			}

			if (DebugWeaponVisuals)
			{
				GD.Print(
					$"PlayerNode.ApplyWeaponSprites: upDraw={TextureLabel(upDraw)}, downDraw={TextureLabel(downDraw)}, upStow={TextureLabel(upStow)}, downStow={TextureLabel(downStow)}");
			}
		}

		private static string TextureLabel(Texture2D texture)
		{
			if (texture == null)
			{
				return "<null>";
			}

			return string.IsNullOrWhiteSpace(texture.ResourcePath) ? "<runtime>" : texture.ResourcePath;
		}
	}
}
