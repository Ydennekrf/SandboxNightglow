using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1{
	/// <summary>
    /// Owns persistent world metadata such as player name, location, flags, counters, friendship, and play time.
    /// </summary>
    /// <remarks>
    /// Inventory and entity snapshots are owned by their dedicated managers. GameStateManager stores
    /// cross-system world facts and keeps legacy NPC friendship shapes synchronized for older save paths.
    /// </remarks>
	public partial class GameStateManager : IGameStateManager, ISaveable, IResolveable
{
		private string CurrentSceneName { get; set; }
		private Vector2 PlayerPos { get; set; }
        private Player _player;
        private string _saveKey = "GameState";
        private int _resolveOrder = 5;
        private WorldStateDto _worldState = new();

		public string SaveKey => _saveKey;

        public int ResolveOrder => _resolveOrder;
        public string PlayerName => ResolvePlayerName();
        public double GameTimePlayedSeconds => Math.Max(0d, _worldState.GameTimePlayedSeconds);
        public string CurrentSceneId => GetCurrentLocation().SceneId;
        public string CurrentScenePath => CurrentSceneId;
        public string CurrentSceneDisplayName => ResolveSceneDisplayName();
        public string CurrentSpawnId => GetCurrentLocation().SpawnId;
        public string CurrentAreaId => GetCurrentLocation().AreaId;
        public string CurrentMainStoryQuestId => _worldState.CurrentMainStoryQuestId ?? string.Empty;
        public string CurrentMainStoryQuestName => ResolveMainStoryQuestName();
        public string TimePhase => GetTimeOfDay().Phase;
        public int DayNumber => GetTimeOfDay().Day;
        public IReadOnlyList<string> ExploredAreaIds => (_worldState.ExploredAreaIds ?? new List<string>()).AsReadOnly();
        public IReadOnlyDictionary<string, int> NpcFriendshipScores => _worldState.FriendshipByPersonId ?? new Dictionary<string, int>();
        public IReadOnlyDictionary<string, bool> WorldFlags => _worldState.Flags ?? new Dictionary<string, bool>();
        public IReadOnlyDictionary<string, int> IntegerValues => _worldState.IntValues ?? new Dictionary<string, int>();
        public IReadOnlyDictionary<string, string> StringValues => _worldState.StringValues ?? new Dictionary<string, string>();


        private Dictionary<string, NPCData> _npcData;

		/// <summary>
		/// Returns the current single-player model stored in game state.
		/// </summary>
		public Player GetPlayer()
		{
			return _player;
		}

        /// <summary>
        /// Stores the active player model and mirrors the player name into persistent world state.
        /// </summary>
        public bool SetPlayer(Player player)
        {
            _player = player;

            if(_player != null)
            {
                SetPlayerName(_player.Name);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Finds a registered NPC by display/runtime name.
        /// </summary>
        public NPC GetNPC(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return GameManager.Instance?.registeredNPCs?
                .FirstOrDefault(npc => npc != null && string.Equals(npc.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sets a boolean world flag and publishes GameFlagChanged when the value changes.
        /// </summary>
        public void SetFlag(string key, bool value = true)
        {
            if (!TryNormalizeKey(key, out string normalizedKey))
            {
                return;
            }

            bool changed = !_worldState.Flags.TryGetValue(normalizedKey, out bool existing) || existing != value;
            _worldState.Flags[normalizedKey] = value;

            if (changed)
            {
                Log($"Set flag: {normalizedKey} = {value.ToString().ToLowerInvariant()}");
                PublishWorldStateEvent(GameEvent.GameFlagChanged, new GameFlagChangedEvent(normalizedKey, value));
            }
        }

        public bool GetFlag(string key)
        {
            return TryNormalizeKey(key, out string normalizedKey)
                && _worldState.Flags.TryGetValue(normalizedKey, out bool value)
                && value;
        }

        public void ClearFlag(string key)
        {
            if (!TryNormalizeKey(key, out string normalizedKey))
            {
                return;
            }

            if (_worldState.Flags.Remove(normalizedKey))
            {
                Log($"Set flag: {normalizedKey} = false");
                PublishWorldStateEvent(GameEvent.GameFlagChanged, new GameFlagChangedEvent(normalizedKey, false));
            }
        }

        public void SetInt(string key, int value)
        {
            if (!TryNormalizeKey(key, out string normalizedKey))
            {
                return;
            }

            bool changed = !_worldState.IntValues.TryGetValue(normalizedKey, out int existing) || existing != value;
            _worldState.IntValues[normalizedKey] = value;

            if (changed)
            {
                Log($"Set int: {normalizedKey} = {value}");
                PublishWorldStateEvent(GameEvent.GameIntChanged, new GameIntChangedEvent(normalizedKey, value));
            }
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return TryNormalizeKey(key, out string normalizedKey)
                && _worldState.IntValues.TryGetValue(normalizedKey, out int value)
                    ? value
                    : defaultValue;
        }

        public int AddInt(string key, int delta)
        {
            int value = GetInt(key) + delta;
            SetInt(key, value);
            return value;
        }

        public void SetString(string key, string value)
        {
            if (!TryNormalizeKey(key, out string normalizedKey))
            {
                return;
            }

            string safeValue = value ?? string.Empty;
            bool changed = !_worldState.StringValues.TryGetValue(normalizedKey, out string existing) || existing != safeValue;
            _worldState.StringValues[normalizedKey] = safeValue;

            if (changed)
            {
                Log($"Set string: {normalizedKey} = {safeValue}");
                PublishWorldStateEvent(GameEvent.GameStringChanged, new GameStringChangedEvent(normalizedKey, safeValue));
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            return TryNormalizeKey(key, out string normalizedKey)
                && _worldState.StringValues.TryGetValue(normalizedKey, out string value)
                    ? value
                    : defaultValue;
        }

        public void MarkAreaExplored(string areaId)
        {
            if (!TryNormalizeKey(areaId, out string normalizedAreaId))
            {
                return;
            }

            if (HasAreaBeenExplored(normalizedAreaId))
            {
                return;
            }

            _worldState.ExploredAreaIds.Add(normalizedAreaId);
            SetFlag($"area.{normalizedAreaId}.explored", true);
            Log($"Area explored: {normalizedAreaId}");
            PublishWorldStateEvent(GameEvent.AreaExplored, new AreaExploredEvent(normalizedAreaId));
        }

        public bool HasAreaBeenExplored(string areaId)
        {
            return TryNormalizeKey(areaId, out string normalizedAreaId)
                && _worldState.ExploredAreaIds.Any(id => string.Equals(id, normalizedAreaId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Sets the persistent scene/spawn/area IDs for saves and location UI.
        /// </summary>
        public void SetCurrentLocation(string sceneId, string spawnId = "", string areaId = "")
        {
            SetCurrentLocationWithDisplayName(sceneId, sceneId, spawnId, areaId);
        }

        /// <summary>
        /// Sets persistent location IDs plus a display name, following docs/stable-ids.md for authored IDs.
        /// </summary>
        public void SetCurrentLocationWithDisplayName(string sceneId, string sceneDisplayName, string spawnId = "", string areaId = "")
        {
            string safeSceneId = sceneId ?? string.Empty;
            string safeDisplayName = string.IsNullOrWhiteSpace(sceneDisplayName) ? safeSceneId : sceneDisplayName.Trim();
            CurrentLocationDto next = new()
            {
                SceneId = safeSceneId,
                SpawnId = spawnId ?? string.Empty,
                AreaId = areaId ?? string.Empty
            };

            CurrentLocationDto current = _worldState.CurrentLocation ?? new CurrentLocationDto();
            bool changed = current.SceneId != next.SceneId
                || current.SpawnId != next.SpawnId
                || current.AreaId != next.AreaId;

            _worldState.CurrentLocation = next;
            _worldState.CurrentSceneDisplayName = safeDisplayName;
            _worldState.StringValues["player.current_scene"] = next.SceneId;
            _worldState.StringValues["player.current_scene_display_name"] = safeDisplayName;
            _worldState.StringValues["player.current_spawn"] = next.SpawnId;
            _worldState.StringValues["player.current_area"] = next.AreaId;
            CurrentSceneName = next.SceneId;

            if (changed)
            {
                Log($"Current location: scene={next.SceneId} spawn={next.SpawnId} area={next.AreaId}");
                PublishWorldStateEvent(GameEvent.LocationChanged, new LocationChangedEvent(CopyLocation(next)));
            }
        }

        public CurrentLocationDto GetCurrentLocation()
        {
            return CopyLocation(_worldState.CurrentLocation ?? new CurrentLocationDto());
        }

        public int GetFriendship(string personId)
        {
            return TryNormalizeKey(personId, out string normalizedPersonId)
                && _worldState.FriendshipByPersonId.TryGetValue(normalizedPersonId, out int value)
                    ? value
                    : 0;
        }

        public void SetFriendship(string personId, int value)
        {
            if (!TryNormalizeKey(personId, out string normalizedPersonId))
            {
                return;
            }

            bool changed = !_worldState.FriendshipByPersonId.TryGetValue(normalizedPersonId, out int existing) || existing != value;
            _worldState.FriendshipByPersonId[normalizedPersonId] = value;
            SyncLegacyNpcState(normalizedPersonId, value);

            if (changed)
            {
                Log($"Friendship changed: {normalizedPersonId} = {value}");
                PublishWorldStateEvent(GameEvent.FriendshipChanged, new FriendshipChangedEvent(normalizedPersonId, value));
            }
        }

        public int AddFriendship(string personId, int delta)
        {
            int value = GetFriendship(personId) + delta;
            SetFriendship(personId, value);
            return value;
        }

        public void SetTimeOfDay(int day, string phase)
        {
            int safeDay = Math.Max(1, day);
            string safePhase = string.IsNullOrWhiteSpace(phase) ? "Morning" : phase.Trim();
            TimeOfDayDto current = _worldState.TimeOfDay ?? new TimeOfDayDto();
            bool changed = current.Day != safeDay || current.Phase != safePhase;

            _worldState.TimeOfDay = new TimeOfDayDto
            {
                Day = safeDay,
                Phase = safePhase
            };
            _worldState.IntValues["time.day"] = safeDay;
            _worldState.StringValues["time.phase"] = safePhase;

            if (changed)
            {
                Log($"Time of day: Day {safeDay} - {safePhase}");
                PublishWorldStateEvent(GameEvent.TimeOfDayChanged, new TimeOfDayChangedEvent(GetTimeOfDay()));
            }
        }

        public TimeOfDayDto GetTimeOfDay()
        {
            return CopyTimeOfDay(_worldState.TimeOfDay ?? new TimeOfDayDto());
        }

        public void SetPlayerName(string playerName)
        {
            string safeName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
            _worldState.PlayerName = safeName;
            _worldState.StringValues["player.name"] = safeName;
        }

        public void SetMainStoryQuest(string questId, string questName)
        {
            _worldState.CurrentMainStoryQuestId = questId ?? string.Empty;
            _worldState.CurrentMainStoryQuestName = string.IsNullOrWhiteSpace(questName)
                ? "No main quest"
                : questName.Trim();
            _worldState.StringValues["quest.main_story.id"] = _worldState.CurrentMainStoryQuestId;
            _worldState.StringValues["quest.main_story.name"] = _worldState.CurrentMainStoryQuestName;
        }

        public void AddGameTimePlayed(double seconds)
        {
            if (seconds <= 0d)
            {
                return;
            }

            _worldState.GameTimePlayedSeconds = Math.Max(0d, _worldState.GameTimePlayedSeconds + seconds);
            _worldState.IntValues["time.played_seconds"] = (int)Math.Floor(_worldState.GameTimePlayedSeconds);
        }

        /// <summary>
        /// Captures a deep-copy world state DTO for save serialization.
        /// </summary>
        public WorldStateDto CaptureWorldStateSnapshot()
        {
            NormalizeRequiredMetadata();
            return CopyWorldState(_worldState);
        }

        /// <summary>
        /// Restores world state from a save DTO and rebuilds legacy compatibility fields.
        /// </summary>
        public void RestoreWorldStateSnapshot(WorldStateDto snapshot)
        {
            _worldState = CopyWorldState(snapshot ?? new WorldStateDto());
            NormalizeRequiredMetadata();
            RebuildFriendshipFromLegacyNpcList();
            SyncLegacyNpcListFromFriendship();
            Log("World state restored.");
        }

        //==========ISavable==========//
        public object CaptureSnapshot()
        {
            NormalizeRequiredMetadata();
            return new GameStateSave
            {
                WorldState = CaptureWorldStateSnapshot()
            };
        }

        public void RestoreSnapshot(object snapshot)
        {
            switch (snapshot)
            {
                case GameStateSave save:
                    RestoreWorldStateSnapshot(save.WorldState);
                    break;
                case WorldStateDto worldState:
                    RestoreWorldStateSnapshot(worldState);
                    break;
            }
        }

        //=======IResolveable=======//
        public void Resolve()
        {
            
        }

        public void Resolve(object obj)
        {
            if (obj is double delta)
            {
                AddGameTimePlayed(delta);
            }
        }

        private static bool TryNormalizeKey(string key, out string normalizedKey)
        {
            normalizedKey = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedKey))
            {
                return true;
            }

            GD.PushWarning("[GameState] Ignored empty world state key.");
            return false;
        }

        private void SyncLegacyNpcState(string personId, int friendship)
        {
            _worldState.Npcs ??= new List<NpcStateDto>();
            NpcStateDto npcState = _worldState.Npcs.FirstOrDefault(npc =>
                npc != null && string.Equals(npc.Id, personId, StringComparison.OrdinalIgnoreCase));

            if (npcState == null)
            {
                _worldState.Npcs.Add(new NpcStateDto { Id = personId, Friendship = friendship });
                return;
            }

            npcState.Friendship = friendship;
        }

        private void RebuildFriendshipFromLegacyNpcList()
        {
            _worldState.FriendshipByPersonId ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (_worldState.Npcs == null)
            {
                return;
            }

            foreach (NpcStateDto npc in _worldState.Npcs)
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.Id))
                {
                    continue;
                }

                if (!_worldState.FriendshipByPersonId.ContainsKey(npc.Id))
                {
                    _worldState.FriendshipByPersonId[npc.Id] = npc.Friendship;
                }
            }
        }

        private void SyncLegacyNpcListFromFriendship()
        {
            _worldState.Npcs = _worldState.FriendshipByPersonId
                .Select(kvp => new NpcStateDto { Id = kvp.Key, Friendship = kvp.Value })
                .ToList();
        }

        private static WorldStateDto CopyWorldState(WorldStateDto source)
        {
            source ??= new WorldStateDto();

            return new WorldStateDto
            {
                PlayerName = string.IsNullOrWhiteSpace(source.PlayerName) ? "Player" : source.PlayerName,
                GameTimePlayedSeconds = Math.Max(0d, source.GameTimePlayedSeconds),
                CurrentSceneDisplayName = source.CurrentSceneDisplayName ?? string.Empty,
                CurrentMainStoryQuestId = source.CurrentMainStoryQuestId ?? string.Empty,
                CurrentMainStoryQuestName = string.IsNullOrWhiteSpace(source.CurrentMainStoryQuestName) ? "No main quest" : source.CurrentMainStoryQuestName,
                Flags = new Dictionary<string, bool>(source.Flags ?? new Dictionary<string, bool>(), StringComparer.OrdinalIgnoreCase),
                IntValues = new Dictionary<string, int>(source.IntValues ?? new Dictionary<string, int>(), StringComparer.OrdinalIgnoreCase),
                StringValues = new Dictionary<string, string>(source.StringValues ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase),
                ExploredAreaIds = new List<string>(source.ExploredAreaIds ?? new List<string>()),
                FriendshipByPersonId = new Dictionary<string, int>(source.FriendshipByPersonId ?? new Dictionary<string, int>(), StringComparer.OrdinalIgnoreCase),
                CurrentLocation = CopyLocation(source.CurrentLocation ?? new CurrentLocationDto()),
                TimeOfDay = CopyTimeOfDay(source.TimeOfDay ?? new TimeOfDayDto()),
                QuestStateRefs = new Dictionary<string, string>(source.QuestStateRefs ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase),
                Npcs = (source.Npcs ?? new List<NpcStateDto>())
                    .Where(npc => npc != null)
                    .Select(npc => new NpcStateDto { Id = npc.Id ?? string.Empty, Friendship = npc.Friendship })
                    .ToList()
            };
        }

        private static CurrentLocationDto CopyLocation(CurrentLocationDto source)
        {
            source ??= new CurrentLocationDto();
            return new CurrentLocationDto
            {
                SceneId = source.SceneId ?? string.Empty,
                SpawnId = source.SpawnId ?? string.Empty,
                AreaId = source.AreaId ?? string.Empty
            };
        }

        private static TimeOfDayDto CopyTimeOfDay(TimeOfDayDto source)
        {
            source ??= new TimeOfDayDto();
            return new TimeOfDayDto
            {
                Day = Math.Max(1, source.Day),
                Phase = string.IsNullOrWhiteSpace(source.Phase) ? "Morning" : source.Phase
            };
        }

        private string ResolvePlayerName()
        {
            if (!string.IsNullOrWhiteSpace(_worldState.PlayerName))
            {
                return _worldState.PlayerName;
            }

            return string.IsNullOrWhiteSpace(_player?.Name) ? "Player" : _player.Name;
        }

        private string ResolveSceneDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(_worldState.CurrentSceneDisplayName))
            {
                return _worldState.CurrentSceneDisplayName;
            }

            return CurrentSceneId;
        }

        private string ResolveMainStoryQuestName()
        {
            return string.IsNullOrWhiteSpace(_worldState.CurrentMainStoryQuestName)
                ? "No main quest"
                : _worldState.CurrentMainStoryQuestName;
        }

        private void NormalizeRequiredMetadata()
        {
            SetPlayerName(ResolvePlayerName());

            CurrentLocationDto location = _worldState.CurrentLocation ?? new CurrentLocationDto();
            _worldState.CurrentLocation = CopyLocation(location);
            _worldState.CurrentSceneDisplayName = ResolveSceneDisplayName();
            _worldState.StringValues["player.current_scene"] = _worldState.CurrentLocation.SceneId;
            _worldState.StringValues["player.current_scene_display_name"] = _worldState.CurrentSceneDisplayName;
            _worldState.StringValues["player.current_spawn"] = _worldState.CurrentLocation.SpawnId;
            _worldState.StringValues["player.current_area"] = _worldState.CurrentLocation.AreaId;

            _worldState.GameTimePlayedSeconds = Math.Max(0d, _worldState.GameTimePlayedSeconds);
            _worldState.IntValues["time.played_seconds"] = (int)Math.Floor(_worldState.GameTimePlayedSeconds);

            TimeOfDayDto time = GetTimeOfDay();
            _worldState.TimeOfDay = time;
            _worldState.IntValues["time.day"] = time.Day;
            _worldState.StringValues["time.phase"] = time.Phase;

            SetMainStoryQuest(CurrentMainStoryQuestId, CurrentMainStoryQuestName);
        }

        private static void PublishWorldStateEvent<T>(GameEvent evt, T payload)
        {
            GameManager.Instance?.Publish(evt, payload);
        }

        private static void Log(string message)
        {
            GD.Print($"[GameState] {message}");
        }
    }
}
