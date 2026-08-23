using Godot;

namespace ethra.V1;

/// <summary>
/// Data-driven catalog of stable sound IDs and their playable stream variants.
/// </summary>
[GlobalClass]
public partial class AudioLibraryResource : Resource
{
	[Export] public Godot.Collections.Array<AudioSoundDefinitionResource> Sounds { get; set; } = new();
}
