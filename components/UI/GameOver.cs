using Godot;
using System;

public partial class GameOver : Node
{

	[Export] Button LoadButton { get; set; }
	[Export] Button QuitButton { get; set; }

	private Player _player { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		EventManager.I.Subscribe<DieEvent>(GameEvent.Died, OnDied);
		LoadButton.Pressed += LoadGame;
		QuitButton.Pressed += QuitGame;

		var bg = GetNode<TextureRect>("Background");
		var snapshot = GameManager.Instance.LastDeathSnapshot;

		if (snapshot != null)
			bg.Texture = snapshot;

			
			var tween = CreateTween();
        tween.TweenProperty(bg, "modulate:a", 0.5f, 0.8f) // fade to 50% alpha
             .SetTrans(Tween.TransitionType.Quad);
	}

	private void LoadGame()
	{
		GameManager.Instance.StartLoadGame(_player.SaveSlotId);
	}

	private void QuitGame()
	{
		GetTree().Quit();
	}

	private void OnDied(DieEvent e)
	{
		if(e.killed is Player p)
		_player = p;
	}
}
