using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
	public sealed class SoundDefinition
	{
		public string SoundId { get; set; } = string.Empty;
		public string DisplayName { get; set; } = string.Empty;

		public AudioCategory Category { get; set; } = AudioCategory.Entity;

		public List<AudioStream> Streams { get; } = new();
		public string BusName { get; set; } = string.Empty;
		public float VolumeDb { get; set; } = 0f;
		public float PitchScale { get; set; } = 1f;
		public float RandomPitchMin { get; set; } = 1f;
		public float RandomPitchMax { get; set; } = 1f;
		public bool Loop { get; set; }
		public int MaxInstances { get; set; } = 0;
		public bool IsPositional { get; set; }
		public float DefaultFadeSeconds { get; set; } = 0.5f;
		public string Notes { get; set; } = string.Empty;
	}
}
