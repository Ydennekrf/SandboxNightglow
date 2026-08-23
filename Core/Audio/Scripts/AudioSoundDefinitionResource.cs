using Godot;

namespace ethra.V1;

/// <summary>
/// Authored sound definition used by <see cref="AudioLibraryResource"/>.
/// </summary>
[GlobalClass]
public partial class AudioSoundDefinitionResource : Resource
{
	[Export] public string SoundId { get; set; } = string.Empty;
	[Export] public string DisplayName { get; set; } = string.Empty;
	[Export] public AudioCategory Category { get; set; } = AudioCategory.Entity;
	[Export] public Godot.Collections.Array<AudioStream> Streams { get; set; } = new();
	[Export] public string BusName { get; set; } = string.Empty;
	[Export] public float VolumeDb { get; set; } = 0f;
	[Export] public float PitchScale { get; set; } = 1f;
	[Export] public float RandomPitchMin { get; set; } = 1f;
	[Export] public float RandomPitchMax { get; set; } = 1f;
	[Export] public bool Loop { get; set; }
	[Export] public int MaxInstances { get; set; } = 0;
	[Export] public bool IsPositional { get; set; }
	[Export] public float DefaultFadeSeconds { get; set; } = 0.5f;
	[Export(PropertyHint.MultilineText)] public string Notes { get; set; } = string.Empty;
}
