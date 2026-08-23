namespace ethra.V1
{
	public readonly struct AudioPlaybackHandle
	{
		public static readonly AudioPlaybackHandle Invalid = new(-1, string.Empty, AudioCategory.Entity);

		public AudioPlaybackHandle(int handleId, string soundId, AudioCategory category)
		{
			HandleId = handleId;
			SoundId = soundId ?? string.Empty;
			Category = category;
		}

		public int HandleId { get; }
		public string SoundId { get; }
		public AudioCategory Category { get; }
		public bool IsValid => HandleId >= 0 && !string.IsNullOrWhiteSpace(SoundId);
	}
}
