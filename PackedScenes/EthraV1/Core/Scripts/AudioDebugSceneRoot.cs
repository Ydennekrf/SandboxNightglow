using Godot;

namespace ethra.V1
{
	public partial class AudioDebugSceneRoot : Node2D
	{
		private Label _statusLabel;
		private AudioPlaybackHandle _sceneLoopHandle = AudioPlaybackHandle.Invalid;

		public override void _Ready()
		{
			BuildPanel();
			CallDeferred(nameof(PrintReadyStatus));
		}

		public override void _Input(InputEvent inputEvent)
		{
			if (inputEvent is not InputEventKey key || !key.Pressed || key.Echo)
			{
				return;
			}

			switch (key.Keycode)
			{
				case Key.Key1:
					PlayUi();
					break;
				case Key.Key2:
					PlayEntity();
					break;
				case Key.Key3:
					PlaySceneSound();
					break;
				case Key.Key4:
					PlayMusic();
					break;
				case Key.Key5:
					StopMusic();
					break;
				case Key.Key6:
					ToggleSceneLoop();
					break;
			}
		}

		private void BuildPanel()
		{
			CanvasLayer canvas = new() { Name = "UI" };
			AddChild(canvas);

			PanelContainer panel = new()
			{
				Name = "AudioDebugPanel",
				OffsetLeft = 24,
				OffsetTop = 24,
				OffsetRight = 420,
				OffsetBottom = 390
			};
			canvas.AddChild(panel);

			VBoxContainer root = new()
			{
				Name = "Root",
				CustomMinimumSize = new Vector2(360, 320)
			};
			panel.AddChild(root);

			root.AddChild(new Label { Text = "Audio Debug" });
			_statusLabel = new Label
			{
				Text = "Waiting for AudioManager...",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(320, 52)
			};
			root.AddChild(_statusLabel);

			AddButton(root, "Play UI Confirm (1)", PlayUi);
			AddButton(root, "Play Entity Attack (2)", PlayEntity);
			AddButton(root, "Play Scene Sound (3)", PlaySceneSound);
			AddButton(root, "Play Music (4)", PlayMusic);
			AddButton(root, "Stop Music (5)", StopMusic);
			AddButton(root, "Toggle Critter Loop (6)", ToggleSceneLoop);
			AddVolumeSlider(root, "Music Volume", AudioCategory.Music);
			AddVolumeSlider(root, "Scene Volume", AudioCategory.Scene);
			AddVolumeSlider(root, "Entity Volume", AudioCategory.Entity);
			AddVolumeSlider(root, "UI Volume", AudioCategory.UI);
		}

		private static void AddButton(VBoxContainer root, string text, System.Action pressed)
		{
			Button button = new()
			{
				Text = text,
				CustomMinimumSize = new Vector2(260, 32),
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			button.Pressed += pressed;
			root.AddChild(button);
		}

		private void AddVolumeSlider(VBoxContainer root, string labelText, AudioCategory category)
		{
			Label label = new() { Text = labelText };
			root.AddChild(label);

			HSlider slider = new()
			{
				MinValue = 0,
				MaxValue = 1,
				Step = 0.05,
				Value = 1,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			slider.ValueChanged += value =>
			{
				AudioManager.SetCategoryVolume(category, (float)value);
				SetStatus($"{category} volume: {value:0.00}");
			};
			root.AddChild(slider);
		}

		private void PrintReadyStatus()
		{
			int count = GameManager.Instance?.Audio?.Definitions?.Count ?? 0;
			SetStatus($"AudioManager ready. Definitions loaded: {count}. Missing streams should warn without crashing.");
		}

		private void PlayUi()
		{
			AudioManager.PlayUi("sound.ui.confirm");
			SetStatus("Requested sound.ui.confirm");
		}

		private void PlayEntity()
		{
			AudioManager.PlayEntity("sound.player.attack", this);
			SetStatus("Requested sound.player.attack at debug root");
		}

		private void PlaySceneSound()
		{
			AudioManager.Play("sound.world.door_open", GlobalPosition);
			SetStatus("Requested sound.world.door_open at debug root");
		}

		private void PlayMusic()
		{
			AudioManager.PlayMusic("music.combat_debug");
			SetStatus("Requested music.combat_debug");
		}

		private void StopMusic()
		{
			AudioManager.StopMusic();
			SetStatus("Requested StopMusic");
		}

		private void ToggleSceneLoop()
		{
			if (_sceneLoopHandle.IsValid)
			{
				GameManager.Instance?.Audio?.StopLoop(_sceneLoopHandle);
				_sceneLoopHandle = AudioPlaybackHandle.Invalid;
				SetStatus("Stopped sound.world.critters loop");
				return;
			}

			_sceneLoopHandle = AudioManager.PlaySceneSound("sound.world.critters", this);
			SetStatus("Requested sound.world.critters loop");
		}

		private void SetStatus(string message)
		{
			if (_statusLabel != null)
			{
				_statusLabel.Text = message;
			}

			GD.Print($"[AudioDebug] {message}");
		}
	}
}
