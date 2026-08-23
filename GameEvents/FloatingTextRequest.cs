using Godot;

/// <summary>
/// Visual category for floating world/screen text requests.
/// </summary>
public enum FloatingTextType
{
	Generic,
	Damage,
	Healing,
	Pickup,
	Debug
}

/// <summary>
/// UI event payload requesting temporary floating text.
/// </summary>
/// <remarks>
/// Gameplay systems publish this through GameManager events. FloatingTextManager owns presentation,
/// positioning, lifetime, and animation.
/// </remarks>
public sealed class FloatingTextRequest
{
	public string Text { get; init; } = string.Empty;
	public Node2D WorldTarget { get; init; }
	public Vector2 WorldPosition { get; init; }
	public bool HasWorldPosition { get; init; }
	public FloatingTextType Type { get; init; } = FloatingTextType.Generic;
	public Color? TextColor { get; init; }
	public Vector2 ScreenOffset { get; init; } = new(0f, -28f);
	public float DurationSeconds { get; init; } = 0.85f;
	public float RiseDistance { get; init; } = 22f;

	/// <summary>
	/// Creates a request anchored to a moving world target.
	/// </summary>
	public static FloatingTextRequest AtTarget(string text, Node2D target, FloatingTextType type = FloatingTextType.Generic)
	{
		return new FloatingTextRequest
		{
			Text = text,
			WorldTarget = target,
			Type = type
		};
	}

	/// <summary>
	/// Creates a request anchored to a fixed world position.
	/// </summary>
	public static FloatingTextRequest AtWorldPosition(string text, Vector2 worldPosition, FloatingTextType type = FloatingTextType.Generic)
	{
		return new FloatingTextRequest
		{
			Text = text,
			WorldPosition = worldPosition,
			HasWorldPosition = true,
			Type = type
		};
	}
}
