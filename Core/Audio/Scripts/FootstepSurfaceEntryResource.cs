using Godot;

namespace ethra.V1;

/// <summary>
/// Maps a terrain surface identifier to a stable audio sound ID.
/// </summary>
[GlobalClass]
public partial class FootstepSurfaceEntryResource : Resource
{
	[Export] public string SurfaceId { get; set; } = string.Empty;
	[Export] public string SoundId { get; set; } = string.Empty;
}
