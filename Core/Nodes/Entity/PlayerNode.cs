using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	public partial class PlayerNode : CharacterBody2D
	{
		private const bool DebugWeaponVisuals = true;
		private const bool DebugCombatInput = true;
		private const bool DebugCombatFeedback = true;
		private Player _player;
		private AnimationPlayer _anim;
		private GameManager _gm;
		private HitBox _attackHitBox;
		private CollisionShape2D _attackHitBoxShape;
		private HurtBox _hurtBox;
		private TileSurfaceResolver _surfaceResolver;
		private AttackPayloadPacket _pendingAttackPacket;
		private AttackPayloadPacket _activeHitBoxPacket;
		private string _lastAnim = "";
		private AttackOverlayMode _lastOverlayMode = AttackOverlayMode.None;
		private bool _interactionLocked;
		private bool _cutsceneControlled;
		private bool _harvestVisualActive;
		private bool _prevWepUpDrawVisible;
		private bool _prevWepDownDrawVisible;
		private bool _prevWepUpStowVisible;
		private bool _prevWepDownStowVisible;
		private Texture2D _pickaxeToolTexture;
		private Texture2D _axeToolTexture;
		private int _attackHitBoxActivationToken;
		private readonly HashSet<string> _missingAnimationWarnings = new();

		[Export] public NodePath HarvestToolSpritePath { get; set; } = "Sprites/Mining";
		[ExportGroup("Combat Tuning")]
		[Export] public float AttackHitBoxOffset { get; set; } = 24f;
		[Export] public float AttackHitBoxActiveSeconds { get; set; } = 0.10f;

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
		public Sprite2D _harvestTool;

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
			_harvestTool = GetNodeOrNull<Sprite2D>(HarvestToolSpritePath);
			_attackHitBox = GetNodeOrNull<HitBox>("AttackHitBox");
			_attackHitBoxShape = GetNodeOrNull<CollisionShape2D>("AttackHitBox/CollisionShape2D");
			_hurtBox = GetNodeOrNull<HurtBox>("HurtBox");
			CombatFeedbackBus.PayloadQueued += OnPayloadQueued;
			CombatFeedbackBus.ActiveWindowChanged += OnActiveWindowChanged;
			GameManager.Instance?.Subscribe<DialogStartedEvent>(GameEvent.DialogStarted, OnDialogStarted);
			GameManager.Instance?.Subscribe<DialogEndedEvent>(GameEvent.DialogEnded, OnDialogEnded);
			GameManager.Instance?.Subscribe<DialogNodeChangedEvent>(GameEvent.DialogNodeChanged, OnDialogNodeChanged);

			if (_gm != null)
			{
				ApplyWeaponSprites(_gm.WepUpDraw, _gm.WepDownDraw, _gm.WepUpStow, _gm.WepDownStow);
				ApplyHarvestToolSprites(_gm.HarvestPickaxeSprite, _gm.HarvestAxeSprite);
			}

			if (_harvestTool != null)
			{
				_harvestTool.Visible = false;
				_harvestTool.Texture = null;
			}
		}

		public override void _ExitTree()
		{
			CombatFeedbackBus.PayloadQueued -= OnPayloadQueued;
			CombatFeedbackBus.ActiveWindowChanged -= OnActiveWindowChanged;
			GameManager.Instance?.Unsubscribe<DialogStartedEvent>(GameEvent.DialogStarted, OnDialogStarted);
			GameManager.Instance?.Unsubscribe<DialogEndedEvent>(GameEvent.DialogEnded, OnDialogEnded);
			GameManager.Instance?.Unsubscribe<DialogNodeChangedEvent>(GameEvent.DialogNodeChanged, OnDialogNodeChanged);
		}

		public void Bind(Player player)
		{
			_player = player ?? throw new ArgumentNullException(nameof(player));
			if (_attackHitBox != null)
			{
				_attackHitBox.Bind(_player);
			}

			if (_hurtBox != null)
			{
				_hurtBox.Bind(_player);
			}
		}

		public void SetInteractionLocked(bool locked)
		{
			_interactionLocked = locked;

			if (_player == null)
			{
				return;
			}

			if (locked)
			{
				ClearInputState();
				_player.DesiredVelocity = Vector2.Zero;
				Velocity = Vector2.Zero;
			}
		}

		public void SetCutsceneControlled(bool controlled)
		{
			_cutsceneControlled = controlled;
			SetInteractionLocked(controlled);

			if (controlled)
			{
				ClearInputState();
				Velocity = Vector2.Zero;
				if (_player != null)
				{
					_player.DesiredVelocity = Vector2.Zero;
				}
			}
		}

		public async void ActivateHitBox()
		{
			if (_attackHitBox == null || _player == null)
			{
				return;
			}

			if (_pendingAttackPacket?.Payload?.OverlayMode == AttackOverlayMode.Magic)
			{
				return;
			}

			if (_attackHitBox.IsActive && ReferenceEquals(_activeHitBoxPacket, _pendingAttackPacket))
			{
				return;
			}

			int activationToken = ++_attackHitBoxActivationToken;
			ApplyHitboxProfile(_pendingAttackPacket?.HitboxProfile);
			_attackHitBox.Bind(_player);
			_attackHitBox.ConfigureAttack(_pendingAttackPacket);
			_attackHitBox.Activate();
			_activeHitBoxPacket = _pendingAttackPacket;

			float profileDuration = _pendingAttackPacket?.HitboxProfile?.ActiveDurationSeconds ?? 0f;
			float activeSeconds = Mathf.Max((float)GetPhysicsProcessDeltaTime(), profileDuration > 0f ? profileDuration : AttackHitBoxActiveSeconds);
			await ToSignal(GetTree().CreateTimer(activeSeconds), SceneTreeTimer.SignalName.Timeout);
			if (activationToken != _attackHitBoxActivationToken)
			{
				return;
			}

			_attackHitBox.Deactivate();
			_activeHitBoxPacket = null;
		}

		public void DeactivateHitBox()
		{
			_attackHitBoxActivationToken++;
			_attackHitBox?.Deactivate();
			_activeHitBoxPacket = null;
		}

		public void PlayIdleSound() => PlayPlayerAnimationSound("sound.player.idle");

		public void PlayWalkSound() => PlayPlayerAnimationSound("sound.player.walk");

		public void PlayRunSound() => PlayPlayerAnimationSound("sound.player.run");

		public void PlayFootstepSound()
		{
			string surfaceId = _surfaceResolver?.GetSurfaceAt(GlobalPosition) ?? "default";
			if (GameManager.Instance?.Audio != null)
			{
				GameManager.Instance.Audio.PlayFootstep(surfaceId, GlobalPosition);
				return;
			}

			PlayPlayerAnimationSound("sound.player.footstep");
		}

		public void PlayAttackSound()
		{
			string soundId = _player?.CurrentAttackOverlay == AttackOverlayMode.Magic
				|| _pendingAttackPacket?.Payload?.OverlayMode == AttackOverlayMode.Magic
					? "sound.player.magic_attack"
					: "sound.player.attack";
			PlayPlayerAnimationSound(soundId);
		}

		public void PlayMeleeAttackSound() => PlayPlayerAnimationSound("sound.player.attack");

		public void PlayMagicAttackSound() => PlayPlayerAnimationSound("sound.player.magic_attack");

		public void PlayDodgeSound() => PlayPlayerAnimationSound("sound.player.dodge");

		public void PlayHurtSound() => PlayPlayerAnimationSound("sound.player.hurt");

		public void PlayHarvestSound() => PlayPlayerAnimationSound("sound.player.harvest");

		public void PlayMineSound() => PlayPlayerAnimationSound("sound.player.mine");

		public void PlayChopSound() => PlayPlayerAnimationSound("sound.player.chop");

		public void PlayDialogSound() => PlayPlayerAnimationSound("sound.player.dialog");

		public void PlayPlayerAnimationSound(string soundId)
		{
			if (string.IsNullOrWhiteSpace(soundId))
			{
				return;
			}

			GameManager.Instance?.Audio?.PlayEntity(soundId, this);
		}

		/// <summary>
		/// Supplies the current world terrain resolver used by animation footstep callbacks.
		/// </summary>
		public void SetSurfaceResolver(TileSurfaceResolver surfaceResolver)
		{
			_surfaceResolver = surfaceResolver;
		}

		public override void _PhysicsProcess(double delta)
		{
			float dt = (float)delta;

			if (_cutsceneControlled)
			{
				ClearInputState();
				Velocity = Vector2.Zero;
				if (_player != null)
				{
					_player.DesiredVelocity = Vector2.Zero;
				}
				return;
			}

			if (_player == null)
			{
				return;
			}

			bool gameplayInputBlocked = IsGameplayInputBlocked();
			if (gameplayInputBlocked)
			{
				ClearInputState();
			}
			else
			{
				_player.MoveInput = Input.GetVector("Left", "Right", "Up", "Down");
				_player.RunPressed = Input.IsActionPressed("Run");
				_player.DodgePressed = Input.IsActionJustPressed("Dodge");
				_player.MeleePressed = Input.IsActionJustPressed("Attack");
				_player.MagicPressed = Input.IsActionJustPressed("MagicAttack");
				_player.AttackPressed = _player.MeleePressed || _player.MagicPressed;
				_player.MeleeHeld = Input.IsActionPressed("Attack");
				_player.MagicHeld = Input.IsActionPressed("MagicAttack");
				_player.AttackHeld = _player.MeleeHeld || _player.MagicHeld;
			}

			if (_player.AttackPressed)
			{
				_player.PendingAttackInput = _player.MagicPressed ? AttackInputType.Magic : AttackInputType.Melee;
				if (DebugCombatInput)
				{
					GD.Print($"PlayerNode: attack input queued type={_player.PendingAttackInput} phase={_player.AttackPhase}");
				}
			}
			_player.Tick(dt);
			ApplyAttackOverlay(_player.CurrentAttackOverlay);

			Vector2 desiredVelocity = _player.DesiredVelocity;
			if (_gm?.Combat != null)
			{
				desiredVelocity = _gm.Combat.ResolveMovementVelocity(_player, desiredVelocity);
			}

			if (gameplayInputBlocked)
			{
				desiredVelocity = Vector2.Zero;
				_player.DesiredVelocity = Vector2.Zero;
			}

			Velocity = desiredVelocity;

			MoveAndSlide();




			var animName = _player.RequestedAnimation;
			PlayRequestedAnimation(animName);
		}

		public bool RequestHarvest(PlayerHarvestRequest request)
		{
			return _player?.RequestHarvest(request) == true;
		}

		public void BeginHarvestVisuals(HarvestToolType toolType)
		{
			if (!_harvestVisualActive)
			{
				_prevWepUpDrawVisible = _wepUpDraw?.Visible ?? false;
				_prevWepDownDrawVisible = _wepDownDraw?.Visible ?? false;
				_prevWepUpStowVisible = _wepUpStow?.Visible ?? false;
				_prevWepDownStowVisible = _wepDownStow?.Visible ?? false;
			}

			_harvestVisualActive = true;
			SetWeaponStowed(true);
			ShowHarvestTool(toolType);
		}

		public void EndHarvestVisuals()
		{
			if (!_harvestVisualActive)
			{
				return;
			}

			_harvestVisualActive = false;
			RestoreWeaponVisibility();
			if (_harvestTool != null)
			{
				_harvestTool.Visible = false;
			}
		}

		private void ClearInputState()
		{
			if (_player == null)
			{
				return;
			}

			_player.MoveInput = Vector2.Zero;
			_player.RunPressed = false;
			_player.DodgePressed = false;
			_player.MeleePressed = false;
			_player.MagicPressed = false;
			_player.AttackPressed = false;
			_player.MeleeHeld = false;
			_player.MagicHeld = false;
			_player.AttackHeld = false;
		}

		private bool IsGameplayInputBlocked()
		{
			return _interactionLocked || _gm?.UI?.BlocksGameplayInput == true;
		}


		private void ApplyAttackOverlay(AttackOverlayMode mode)
		{
			if (_overlay == null)
			{
				return;
			}

			// Temporary visual toggle until dedicated melee/magic overlay textures are wired.
			_overlay.Visible = mode == AttackOverlayMode.Magic;

			if (DebugCombatFeedback && mode != _lastOverlayMode)
			{
				GD.Print($"PlayerNode: overlay mode changed {_lastOverlayMode} -> {mode}");
				_lastOverlayMode = mode;
			}
		}

		private void OnPayloadQueued(AttackPayloadPacket packet)
		{
			if (packet?.Source != _player)
			{
				return;
			}

			_pendingAttackPacket = packet;
		}

		private void OnActiveWindowChanged(Player player, bool isOpen, float elapsed, float duration)
		{
			if (player != _player)
			{
				return;
			}

			if (isOpen)
			{
				ActivateHitBox();
			}
			else
			{
				DeactivateHitBox();
			}
		}

		private void OnDialogStarted(DialogStartedEvent evt)
		{
			_player?.SetDialogActive(true);
		}

		private void OnDialogEnded(DialogEndedEvent evt)
		{
			_player?.SetDialogActive(false);
		}

		private void OnDialogNodeChanged(DialogNodeChangedEvent evt)
		{
			if (evt.IsPlayerSpeaker)
			{
				_player?.RequestDialogAnimation(evt.AnimationKey);
			}
		}

		private void PlayRequestedAnimation(string animName)
		{
			if (_anim == null || string.IsNullOrEmpty(animName) || animName == _lastAnim)
			{
				return;
			}

			if (_anim.HasAnimation(animName))
			{
				_anim.Play(animName);
				_lastAnim = animName;
				return;
			}

			if (_missingAnimationWarnings.Add(animName))
			{
				GD.PushWarning($"Animation not found: '{animName}'");
			}

			_lastAnim = animName;
		}

		private void ApplyHitboxProfile(HitboxProfileResource profile)
		{
			PositionAttackHitBox(profile);
			ApplyHitboxShape(profile);
		}

		private void PositionAttackHitBox(HitboxProfileResource profile = null)
		{
			if (_attackHitBox == null || _player == null)
			{
				return;
			}

			if (profile == null)
			{
				float offset = Mathf.Max(1f, AttackHitBoxOffset);
				_attackHitBox.Position = _player.Facing switch
				{
					FacingDirection.Up => new Vector2(0f, -offset),
					FacingDirection.Down => new Vector2(0f, offset),
					FacingDirection.Left => new Vector2(-offset, 0f),
					FacingDirection.Right => new Vector2(offset, 0f),
					_ => new Vector2(0f, offset)
				};
				return;
			}

			if (profile.DirectionalBehavior == HitboxDirectionalBehavior.FixedOffset)
			{
				_attackHitBox.Position = profile.Offset;
				return;
			}

			Vector2 forward = _player.Facing switch
			{
				FacingDirection.Up => Vector2.Up,
				FacingDirection.Down => Vector2.Down,
				FacingDirection.Left => Vector2.Left,
				FacingDirection.Right => Vector2.Right,
				_ => Vector2.Down
			};
			Vector2 side = new(-forward.Y, forward.X);
			_attackHitBox.Position = forward * profile.Offset.X + side * profile.Offset.Y;
		}

		private void ApplyHitboxShape(HitboxProfileResource profile)
		{
			if (_attackHitBoxShape == null || profile == null)
			{
				return;
			}

			_attackHitBoxShape.Shape = profile.ShapeType == HitboxShapeType.Circle
				? new CircleShape2D { Radius = Mathf.Max(1f, profile.Radius) }
				: new RectangleShape2D { Size = new Vector2(Mathf.Max(1f, profile.Size.X), Mathf.Max(1f, profile.Size.Y)) };
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

		public void ApplyHarvestToolSprites(Texture2D pickaxe, Texture2D axe)
		{
			_pickaxeToolTexture = pickaxe;
			_axeToolTexture = axe;

			if (DebugWeaponVisuals)
			{
				GD.Print($"PlayerNode.ApplyHarvestToolSprites: pickaxe={TextureLabel(pickaxe)}, axe={TextureLabel(axe)}");
			}
		}

		private void SetWeaponStowed(bool stowed)
		{
			if (_wepUpDraw != null)
			{
				_wepUpDraw.Visible = !stowed;
			}

			if (_wepDownDraw != null)
			{
				_wepDownDraw.Visible = !stowed;
			}

			if (_wepUpStow != null)
			{
				_wepUpStow.Visible = stowed;
			}

			if (_wepDownStow != null)
			{
				_wepDownStow.Visible = stowed;
			}
		}

		private void RestoreWeaponVisibility()
		{
			if (_wepUpDraw != null)
			{
				_wepUpDraw.Visible = _prevWepUpDrawVisible;
			}

			if (_wepDownDraw != null)
			{
				_wepDownDraw.Visible = _prevWepDownDrawVisible;
			}

			if (_wepUpStow != null)
			{
				_wepUpStow.Visible = _prevWepUpStowVisible;
			}

			if (_wepDownStow != null)
			{
				_wepDownStow.Visible = _prevWepDownStowVisible;
			}
		}

		private void ShowHarvestTool(HarvestToolType toolType)
		{
			if (_harvestTool == null)
			{
				return;
			}

			Texture2D texture = toolType switch
			{
				HarvestToolType.Pickaxe => _pickaxeToolTexture,
				HarvestToolType.Axe => _axeToolTexture,
				_ => null
			};

			_harvestTool.Texture = texture;

			if (_body != null)
			{
				_harvestTool.Hframes = _body.Hframes;
				_harvestTool.Vframes = _body.Vframes;
			}

			_harvestTool.Visible = toolType != HarvestToolType.None && _harvestTool.Texture != null;
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
