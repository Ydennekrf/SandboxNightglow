using Godot;
using System.Collections.Generic;

namespace ethra.V1;

/// <summary>
/// Reuses Godot audio player nodes for short-lived one-shot playback.
/// </summary>
public sealed class AudioPlayerPool
{
	private readonly Node _poolRoot;
	private readonly Stack<AudioStreamPlayer> _players1D = new();
	private readonly Stack<AudioStreamPlayer2D> _players2D = new();

	public AudioPlayerPool(Node owner)
	{
		_poolRoot = new Node { Name = "AudioPlayerPool" };
		owner.AddChild(_poolRoot);
	}

	public Node Rent(bool positional, Node parent)
	{
		Node safeParent = parent ?? _poolRoot;
		Node player;

		if (positional)
		{
			player = _players2D.Count > 0 ? _players2D.Pop() : new AudioStreamPlayer2D();
		}
		else
		{
			player = _players1D.Count > 0 ? _players1D.Pop() : new AudioStreamPlayer();
		}

		MoveToParent(player, safeParent);
		return player;
	}

	public void Return(Node player)
	{
		if (player == null || !GodotObject.IsInstanceValid(player))
		{
			return;
		}

		if (player is AudioStreamPlayer player1D)
		{
			player1D.Stop();
			player1D.Stream = null;
			player1D.Bus = "Master";
			player1D.VolumeDb = 0f;
			player1D.PitchScale = 1f;
			MoveToPool(player1D);
			_players1D.Push(player1D);
			return;
		}

		if (player is AudioStreamPlayer2D player2D)
		{
			player2D.Stop();
			player2D.Stream = null;
			player2D.Bus = "Master";
			player2D.VolumeDb = 0f;
			player2D.PitchScale = 1f;
			player2D.Position = Vector2.Zero;
			MoveToPool(player2D);
			_players2D.Push(player2D);
		}
	}

	private void MoveToPool(Node player)
	{
		MoveToParent(player, _poolRoot);
	}

	private static void MoveToParent(Node player, Node parent)
	{
		Node oldParent = player.GetParent();
		oldParent?.RemoveChild(player);
		parent.AddChild(player);
	}
}
