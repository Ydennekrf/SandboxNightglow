using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
	/// <summary>
	/// Central runtime audio service for stable sound IDs, category routing, music switching, and footstep playback.
	/// </summary>
	public partial class AudioManager : Node
	{
		private const string MasterBus = "Master";
		private const float SilentVolumeDb = -80f;
		private const string DefaultFootstepSoundId = "sound.player.footstep";

		private static AudioManager _active;

		private readonly Dictionary<string, SoundDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, int> _instanceCounts = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<int, Node> _activeHandles = new();
		private readonly Dictionary<int, SoundDefinition> _handleDefinitions = new();
		private readonly Dictionary<Node, SoundDefinition> _activePlayers = new();
		private readonly Dictionary<Node, Action> _finishedHandlers = new();
		private readonly Dictionary<string, string> _footstepSurfaceSoundIds = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<AudioCategory, float> _categoryVolumes = new();
		private readonly Dictionary<AudioCategory, bool> _categoryMuted = new();
		private readonly HashSet<string> _missingDefinitionWarnings = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> _missingStreamWarnings = new(StringComparer.OrdinalIgnoreCase);
		private readonly RandomNumberGenerator _rng = new();

		private AudioStreamPlayer _musicPlayer;
		private AudioPlayerPool _playerPool;
		private SoundDefinition _currentMusicDefinition;
		private string _currentMusicId = string.Empty;
		private Tween _musicTween;
		private int _nextHandleId;

		public static AudioManager Active => _active;
		public IReadOnlyDictionary<string, SoundDefinition> Definitions => _definitions;

		[ExportGroup("Audio Library")]
		[Export] public AudioLibraryResource AudioLibrary { get; set; }
		[Export] public FootstepSurfaceLibraryResource FootstepSurfaces { get; set; }

		[ExportGroup("Bus Names")]
		[Export] public string MusicBusName { get; set; } = "Music";
		[Export] public string SceneBusName { get; set; } = "Scene";
		[Export] public string EntityBusName { get; set; } = "Entity";
		[Export] public string UiBusName { get; set; } = "UI";

		public override void _Ready()
		{
			_rng.Randomize();
			_active = this;

			_musicPlayer = new AudioStreamPlayer
			{
				Name = "MusicPlayer",
				Bus = ResolveBusName(DefaultBusForCategory(AudioCategory.Music))
			};
			AddChild(_musicPlayer);

			_playerPool = new AudioPlayerPool(this);
			foreach (AudioCategory category in Enum.GetValues<AudioCategory>())
			{
				_categoryVolumes[category] = 1f;
				_categoryMuted[category] = false;
			}
		}

		public override void _ExitTree()
		{
			if (_active == this)
			{
				_active = null;
			}
		}

		/// <summary>
		/// Loads authored audio definitions and footstep surface mappings.
		/// </summary>
		public void Initialize()
		{
			LoadDefinitions();
			LoadFootstepSurfaces();
		}

		public AudioPlaybackHandle Play(string soundId)
		{
			return PlayInternal(soundId, null, null, null);
		}

		public AudioPlaybackHandle Play(string soundId, Vector2? worldPosition)
		{
			return PlayInternal(soundId, worldPosition, null, null);
		}

		public AudioPlaybackHandle PlayUi(string soundId)
		{
			return PlayInternal(soundId, null, null, AudioCategory.UI);
		}

		public AudioPlaybackHandle PlayEntity(string soundId, Node2D source = null)
		{
			return PlayInternal(soundId, null, source, AudioCategory.Entity);
		}

		public AudioPlaybackHandle PlaySceneSound(string soundId, Node2D source = null)
		{
			return PlayInternal(soundId, null, source, AudioCategory.Scene);
		}

		/// <summary>
		/// Plays a footstep sound for the provided terrain surface identifier.
		/// </summary>
		public AudioPlaybackHandle PlayFootstep(string surfaceId, Vector2 worldPosition)
		{
			return PlayInternal(ResolveFootstepSoundId(surfaceId), worldPosition, null, AudioCategory.Entity);
		}

		public void PlayMusic(string musicId, bool fade = true)
		{
			if (!TryGetDefinition(musicId, AudioCategory.Music, out SoundDefinition definition))
			{
				return;
			}

			if (string.Equals(_currentMusicId, definition.SoundId, StringComparison.OrdinalIgnoreCase)
				&& _musicPlayer?.Playing == true)
			{
				return;
			}

			AudioStream stream = SelectStream(definition);
			if (stream == null)
			{
				return;
			}

			float fadeSeconds = ResolveFadeSeconds(definition);
			if (!fade || _musicPlayer == null || !_musicPlayer.Playing || fadeSeconds <= 0f)
			{
				StartMusic(definition, stream, fadeIn: false);
				return;
			}

			KillMusicTween();
			_musicTween = CreateTween();
			_musicTween.TweenProperty(_musicPlayer, "volume_db", SilentVolumeDb, fadeSeconds);
			_musicTween.TweenCallback(Callable.From(() => StartMusic(definition, stream, fadeIn: true)));
		}

		public void StopMusic(bool fade = true)
		{
			if (_musicPlayer == null || !_musicPlayer.Playing)
			{
				_currentMusicId = string.Empty;
				_currentMusicDefinition = null;
				return;
			}

			float fadeSeconds = ResolveFadeSeconds(_currentMusicDefinition);
			if (!fade || fadeSeconds <= 0f)
			{
				ClearMusic();
				return;
			}

			KillMusicTween();
			_musicTween = CreateTween();
			_musicTween.TweenProperty(_musicPlayer, "volume_db", SilentVolumeDb, fadeSeconds);
			_musicTween.TweenCallback(Callable.From(ClearMusic));
		}

		public void StopLoop(string soundId)
		{
			if (string.IsNullOrWhiteSpace(soundId))
			{
				return;
			}

			foreach (int handleId in _handleDefinitions
				.Where(pair => string.Equals(pair.Value.SoundId, soundId, StringComparison.OrdinalIgnoreCase))
				.Select(pair => pair.Key)
				.ToArray())
			{
				StopLoop(handleId);
			}
		}

		public void StopLoop(AudioPlaybackHandle handle)
		{
			if (handle.IsValid)
			{
				StopLoop(handle.HandleId);
			}
		}

		public void StopAllCategory(AudioCategory category)
		{
			foreach (int handleId in _handleDefinitions
				.Where(pair => pair.Value.Category == category)
				.Select(pair => pair.Key)
				.ToArray())
			{
				StopLoop(handleId);
			}

			if (category == AudioCategory.Music)
			{
				StopMusic(fade: true);
			}
		}

		public void SetCategoryVolume(AudioCategory category, float volume)
		{
			_categoryVolumes[category] = Mathf.Clamp(volume, 0f, 1f);
			ApplyCategorySettings(category);
		}

		public void MuteCategory(AudioCategory category, bool muted)
		{
			_categoryMuted[category] = muted;
			ApplyCategorySettings(category);
		}

		public static AudioPlaybackHandle Play(string soundId, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.Play(soundId) ?? AudioPlaybackHandle.Invalid;
		}

		public static AudioPlaybackHandle Play(string soundId, Vector2? worldPosition, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.Play(soundId, worldPosition) ?? AudioPlaybackHandle.Invalid;
		}

		public static AudioPlaybackHandle PlayUi(string soundId, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.PlayUi(soundId) ?? AudioPlaybackHandle.Invalid;
		}

		public static AudioPlaybackHandle PlayEntity(string soundId, Node2D source, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.PlayEntity(soundId, source) ?? AudioPlaybackHandle.Invalid;
		}

		public static AudioPlaybackHandle PlaySceneSound(string soundId, Node2D source, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.PlaySceneSound(soundId, source) ?? AudioPlaybackHandle.Invalid;
		}

		public static AudioPlaybackHandle PlayFootstep(string surfaceId, Vector2 worldPosition, bool useActiveManager = true)
		{
			return GetActiveOrWarn()?.PlayFootstep(surfaceId, worldPosition) ?? AudioPlaybackHandle.Invalid;
		}

		public static void PlayMusic(string musicId, bool fade = true, bool useActiveManager = true)
		{
			GetActiveOrWarn()?.PlayMusic(musicId, fade);
		}

		public static void StopMusic(bool fade = true, bool useActiveManager = true)
		{
			GetActiveOrWarn()?.StopMusic(fade);
		}

		public static void StopLoop(string soundId, bool useActiveManager = true)
		{
			GetActiveOrWarn()?.StopLoop(soundId);
		}

		public static void SetCategoryVolume(AudioCategory category, float volume, bool useActiveManager = true)
		{
			GetActiveOrWarn()?.SetCategoryVolume(category, volume);
		}

		public static void MuteCategory(AudioCategory category, bool muted, bool useActiveManager = true)
		{
			GetActiveOrWarn()?.MuteCategory(category, muted);
		}

		private void LoadDefinitions()
		{
			_definitions.Clear();
			if (AudioLibrary == null)
			{
				GD.PushWarning("[AudioManager] AudioLibrary is not assigned. No sounds will be registered.");
				return;
			}

			foreach (AudioSoundDefinitionResource authored in AudioLibrary.Sounds)
			{
				if (authored == null)
				{
					continue;
				}

				SoundDefinition definition = new()
				{
					SoundId = authored.SoundId,
					DisplayName = authored.DisplayName,
					Category = authored.Category,
					BusName = authored.BusName,
					VolumeDb = authored.VolumeDb,
					PitchScale = authored.PitchScale,
					RandomPitchMin = authored.RandomPitchMin,
					RandomPitchMax = authored.RandomPitchMax,
					Loop = authored.Loop,
					MaxInstances = authored.MaxInstances,
					IsPositional = authored.IsPositional,
					DefaultFadeSeconds = authored.DefaultFadeSeconds,
					Notes = authored.Notes
				};

				foreach (AudioStream stream in authored.Streams)
				{
					if (stream != null)
					{
						definition.Streams.Add(stream);
					}
				}

				NormalizeDefinition(definition);
				if (string.IsNullOrWhiteSpace(definition.SoundId))
				{
					GD.PushWarning("[AudioManager] Skipping audio library entry with an empty sound id.");
					continue;
				}

				_definitions[definition.SoundId] = definition;
			}

			GD.Print($"[AudioManager] Registered {_definitions.Count} sounds from audio library.");
		}

		private void LoadFootstepSurfaces()
		{
			_footstepSurfaceSoundIds.Clear();
			if (FootstepSurfaces == null)
			{
				return;
			}

			foreach (FootstepSurfaceEntryResource entry in FootstepSurfaces.Surfaces)
			{
				if (entry == null || string.IsNullOrWhiteSpace(entry.SurfaceId) || string.IsNullOrWhiteSpace(entry.SoundId))
				{
					continue;
				}

				_footstepSurfaceSoundIds[entry.SurfaceId.Trim()] = entry.SoundId.Trim();
			}
		}

		private AudioPlaybackHandle PlayInternal(string soundId, Vector2? worldPosition, Node2D source, AudioCategory? categoryOverride)
		{
			if (!TryGetDefinition(soundId, categoryOverride, out SoundDefinition definition) || !CanStartInstance(definition))
			{
				return AudioPlaybackHandle.Invalid;
			}

			AudioStream stream = SelectStream(definition);
			if (stream == null)
			{
				return AudioPlaybackHandle.Invalid;
			}

			Node player = CreatePlayer(definition, source, worldPosition);
			ConfigurePlayer(player, definition, stream);

			int handleId = _nextHandleId++;
			AudioPlaybackHandle handle = new(handleId, definition.SoundId, definition.Category);
			TrackPlayer(handleId, player, definition);
			ConnectFinished(player, definition.Loop
				? () => RestartLoopIfActive(handleId)
				: () => CleanupPlayer(handleId));
			PlayPlayer(player);
			return handle;
		}

		private Node CreatePlayer(SoundDefinition definition, Node2D source, Vector2? worldPosition)
		{
			bool positional = definition.IsPositional || source != null || worldPosition.HasValue;
			Node player = _playerPool.Rent(positional, ResolvePlayerParent(positional, source));
			player.Name = $"Audio_{definition.SoundId.Replace('.', '_')}";

			if (player is AudioStreamPlayer2D player2D)
			{
				if (source != null)
				{
					player2D.Position = Vector2.Zero;
				}
				else
				{
					player2D.GlobalPosition = worldPosition ?? Vector2.Zero;
				}
			}

			return player;
		}

		private Node ResolvePlayerParent(bool positional, Node2D source)
		{
			if (!positional)
			{
				return this;
			}

			return source != null && IsInstanceValid(source)
				? source
				: GetTree()?.CurrentScene ?? this;
		}

		private bool TryGetDefinition(string soundId, AudioCategory? categoryOverride, out SoundDefinition definition)
		{
			definition = null;
			if (string.IsNullOrWhiteSpace(soundId))
			{
				GD.PushWarning("[AudioManager] Cannot play an empty sound id.");
				return false;
			}

			if (!_definitions.TryGetValue(soundId, out definition))
			{
				if (_missingDefinitionWarnings.Add(soundId))
				{
					GD.PushWarning($"[AudioManager] Missing sound definition: {soundId}");
				}
				return false;
			}

			if (categoryOverride.HasValue && definition.Category != categoryOverride.Value)
			{
				GD.PushWarning($"[AudioManager] Sound '{soundId}' is category {definition.Category}, but was requested as {categoryOverride.Value}. Playing with authored category.");
			}

			return true;
		}

		private AudioStream SelectStream(SoundDefinition definition)
		{
			if (definition?.Streams.Count > 0)
			{
				return definition.Streams[_rng.RandiRange(0, definition.Streams.Count - 1)];
			}

			WarnMissingStream(definition, "no streams assigned in the audio library");
			return null;
		}

		private void WarnMissingStream(SoundDefinition definition, string reason)
		{
			string key = definition?.SoundId ?? string.Empty;
			if (_missingStreamWarnings.Add(key))
			{
				GD.PushWarning($"[AudioManager] Sound '{key}' has no playable audio stream: {reason}.");
			}
		}

		private bool CanStartInstance(SoundDefinition definition)
		{
			if (definition.MaxInstances <= 0)
			{
				return true;
			}

			_instanceCounts.TryGetValue(definition.SoundId, out int current);
			if (current < definition.MaxInstances)
			{
				return true;
			}

			GD.PushWarning($"[AudioManager] MaxInstances reached for '{definition.SoundId}' ({definition.MaxInstances}).");
			return false;
		}

		private void ConfigurePlayer(Node player, SoundDefinition definition, AudioStream stream)
		{
			string bus = ResolveBusName(ResolveBusForDefinition(definition));
			float volumeDb = ResolvePlayerVolumeDb(definition);
			float pitchScale = ResolvePitch(definition);

			if (player is AudioStreamPlayer player1D)
			{
				player1D.Stream = stream;
				player1D.Bus = bus;
				player1D.VolumeDb = volumeDb;
				player1D.PitchScale = pitchScale;
				return;
			}

			if (player is AudioStreamPlayer2D player2D)
			{
				player2D.Stream = stream;
				player2D.Bus = bus;
				player2D.VolumeDb = volumeDb;
				player2D.PitchScale = pitchScale;
			}
		}

		private void ConnectFinished(Node player, Action callback)
		{
			DisconnectFinished(player);
			_finishedHandlers[player] = callback;

			if (player is AudioStreamPlayer player1D)
			{
				player1D.Finished += callback;
			}
			else if (player is AudioStreamPlayer2D player2D)
			{
				player2D.Finished += callback;
			}
		}

		private static void PlayPlayer(Node player)
		{
			if (player is AudioStreamPlayer player1D)
			{
				player1D.Play();
			}
			else if (player is AudioStreamPlayer2D player2D)
			{
				player2D.Play();
			}
		}

		private static void StopPlayer(Node player)
		{
			if (player is AudioStreamPlayer player1D)
			{
				player1D.Stop();
			}
			else if (player is AudioStreamPlayer2D player2D)
			{
				player2D.Stop();
			}
		}

		private static void SetPlayerVolume(Node player, float volumeDb)
		{
			if (player is AudioStreamPlayer player1D)
			{
				player1D.VolumeDb = volumeDb;
			}
			else if (player is AudioStreamPlayer2D player2D)
			{
				player2D.VolumeDb = volumeDb;
			}
		}

		private void TrackPlayer(int handleId, Node player, SoundDefinition definition)
		{
			_activeHandles[handleId] = player;
			_handleDefinitions[handleId] = definition;
			_activePlayers[player] = definition;
			_instanceCounts.TryGetValue(definition.SoundId, out int count);
			_instanceCounts[definition.SoundId] = count + 1;
		}

		private void CleanupPlayer(int handleId)
		{
			if (!_activeHandles.TryGetValue(handleId, out Node player))
			{
				return;
			}

			SoundDefinition definition = _handleDefinitions.GetValueOrDefault(handleId);
			_activeHandles.Remove(handleId);
			_handleDefinitions.Remove(handleId);
			_activePlayers.Remove(player);

			if (definition != null && _instanceCounts.TryGetValue(definition.SoundId, out int count))
			{
				_instanceCounts[definition.SoundId] = Mathf.Max(0, count - 1);
			}

			if (IsInstanceValid(player))
			{
				DisconnectFinished(player);
				_playerPool.Return(player);
			}
		}

		private void RestartLoopIfActive(int handleId)
		{
			if (_activeHandles.TryGetValue(handleId, out Node player) && IsInstanceValid(player))
			{
				PlayPlayer(player);
			}
		}

		private void StopLoop(int handleId)
		{
			if (!_activeHandles.TryGetValue(handleId, out Node player))
			{
				return;
			}

			if (IsInstanceValid(player))
			{
				StopPlayer(player);
			}
			CleanupPlayer(handleId);
		}

		private void DisconnectFinished(Node player)
		{
			if (!_finishedHandlers.TryGetValue(player, out Action handler))
			{
				return;
			}

			if (player is AudioStreamPlayer player1D)
			{
				player1D.Finished -= handler;
			}
			else if (player is AudioStreamPlayer2D player2D)
			{
				player2D.Finished -= handler;
			}

			_finishedHandlers.Remove(player);
		}

		private void StartMusic(SoundDefinition definition, AudioStream stream, bool fadeIn)
		{
			KillMusicTween();
			_currentMusicDefinition = definition;
			_currentMusicId = definition.SoundId;
			_musicPlayer.Stream = stream;
			_musicPlayer.Bus = ResolveBusName(ResolveBusForDefinition(definition));
			_musicPlayer.PitchScale = ResolvePitch(definition);
			_musicPlayer.VolumeDb = fadeIn ? SilentVolumeDb : ResolvePlayerVolumeDb(definition);
			_musicPlayer.Play();

			if (fadeIn)
			{
				_musicTween = CreateTween();
				_musicTween.TweenProperty(_musicPlayer, "volume_db", ResolvePlayerVolumeDb(definition), ResolveFadeSeconds(definition));
			}
		}

		private void ClearMusic()
		{
			_musicPlayer.Stop();
			_musicPlayer.Stream = null;
			_currentMusicId = string.Empty;
			_currentMusicDefinition = null;
		}

		private void KillMusicTween()
		{
			if (_musicTween != null && _musicTween.IsValid())
			{
				_musicTween.Kill();
			}
			_musicTween = null;
		}

		private void ApplyCategorySettings(AudioCategory category)
		{
			foreach ((Node player, SoundDefinition definition) in _activePlayers.ToArray())
			{
				if (definition.Category == category && IsInstanceValid(player))
				{
					SetPlayerVolume(player, ResolvePlayerVolumeDb(definition));
				}
			}

			if (_currentMusicDefinition?.Category == category && _musicPlayer != null)
			{
				_musicPlayer.VolumeDb = ResolvePlayerVolumeDb(_currentMusicDefinition);
			}
		}

		private float ResolvePlayerVolumeDb(SoundDefinition definition)
		{
			if (definition == null)
			{
				return 0f;
			}

			if (_categoryMuted.GetValueOrDefault(definition.Category))
			{
				return SilentVolumeDb;
			}

			float volume = _categoryVolumes.GetValueOrDefault(definition.Category, 1f);
			float categoryDb = volume <= 0f ? SilentVolumeDb : Mathf.LinearToDb(volume);
			return Mathf.Clamp(definition.VolumeDb + categoryDb, SilentVolumeDb, 24f);
		}

		private float ResolvePitch(SoundDefinition definition)
		{
			float pitch = definition.PitchScale <= 0f ? 1f : definition.PitchScale;
			float min = definition.RandomPitchMin <= 0f ? pitch : definition.RandomPitchMin;
			float max = definition.RandomPitchMax <= 0f ? pitch : definition.RandomPitchMax;
			if (!Mathf.IsEqualApprox(min, max))
			{
				pitch *= _rng.RandfRange(Mathf.Min(min, max), Mathf.Max(min, max));
			}

			return Mathf.Max(0.01f, pitch);
		}

		private string ResolveFootstepSoundId(string surfaceId)
		{
			string safeSurface = string.IsNullOrWhiteSpace(surfaceId)
				? FootstepSurfaces?.DefaultSurfaceId
				: surfaceId.Trim();

			if (!string.IsNullOrWhiteSpace(safeSurface)
				&& _footstepSurfaceSoundIds.TryGetValue(safeSurface, out string mappedSoundId))
			{
				return mappedSoundId;
			}

			return string.IsNullOrWhiteSpace(FootstepSurfaces?.DefaultSoundId)
				? DefaultFootstepSoundId
				: FootstepSurfaces.DefaultSoundId.Trim();
		}

		private static float ResolveFadeSeconds(SoundDefinition definition)
		{
			return Mathf.Max(0f, definition?.DefaultFadeSeconds ?? 0.5f);
		}

		private static string ResolveBusForDefinition(SoundDefinition definition)
		{
			return string.IsNullOrWhiteSpace(definition.BusName)
				? DefaultBusForCategory(definition.Category)
				: definition.BusName.Trim();
		}

		private static string DefaultBusForCategory(AudioCategory category)
		{
			AudioManager active = _active;
			if (active != null)
			{
				return category switch
				{
					AudioCategory.Music => active.MusicBusName,
					AudioCategory.Scene => active.SceneBusName,
					AudioCategory.UI => active.UiBusName,
					_ => active.EntityBusName
				};
			}

			return category switch
			{
				AudioCategory.Music => "Music",
				AudioCategory.Scene => "Scene",
				AudioCategory.UI => "UI",
				_ => "Entity"
			};
		}

		private string ResolveBusName(string requestedBus)
		{
			if (!string.IsNullOrWhiteSpace(requestedBus) && AudioServer.GetBusIndex(requestedBus) >= 0)
			{
				return requestedBus;
			}

			if (!string.IsNullOrWhiteSpace(requestedBus) && requestedBus != MasterBus)
			{
				GD.PushWarning($"[AudioManager] Audio bus '{requestedBus}' does not exist. Falling back to '{MasterBus}'.");
			}

			return MasterBus;
		}

		private static void NormalizeDefinition(SoundDefinition definition)
		{
			definition.SoundId = definition.SoundId.Trim();
			definition.DisplayName = definition.DisplayName?.Trim() ?? string.Empty;
			definition.BusName = definition.BusName?.Trim() ?? string.Empty;
			definition.PitchScale = definition.PitchScale <= 0f ? 1f : definition.PitchScale;
			definition.RandomPitchMin = definition.RandomPitchMin <= 0f ? 1f : definition.RandomPitchMin;
			definition.RandomPitchMax = definition.RandomPitchMax <= 0f ? 1f : definition.RandomPitchMax;
		}

		private static AudioManager GetActiveOrWarn()
		{
			if (_active != null)
			{
				return _active;
			}

			GD.PushWarning("[AudioManager] No active AudioManager is initialized yet.");
			return null;
		}
	}
}
