using Godot;

namespace ethra.V1;

/// <summary>
/// Authored lookup table for terrain surface IDs used by footstep playback.
/// </summary>
[GlobalClass]
public partial class FootstepSurfaceLibraryResource : Resource
{
	[Export] public string DefaultSurfaceId { get; set; } = "default";
	[Export] public string DefaultSoundId { get; set; } = "sound.player.footstep";
	[Export] public Godot.Collections.Array<FootstepSurfaceEntryResource> Surfaces { get; set; } = new();
}
