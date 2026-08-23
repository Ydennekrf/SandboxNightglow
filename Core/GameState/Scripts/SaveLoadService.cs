using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ethra.V1
{
    public class SaveLoadService : ISaveLoadService
    {
        public const int MaxSaveSlots = 4;

        private readonly ISaveRegistry _saveRegistry;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public SaveLoadService(ISaveRegistry saveRegistry)
        {
            _saveRegistry = saveRegistry;
        }

        public async Task LoadGameAsync(int id)
        {
            if (!IsValidSlot(id))
            {
                return;
            }

            GD.Print($"[SaveLoad] Loading slot {id}...");
            string path = GetSavePath(id);
            string absPath = ProjectSettings.GlobalizePath(path);
            if (!File.Exists(absPath))
            {
                GD.Print($"[SaveLoad] No save found for slot {id}.");
                return;
            }

            string json = await File.ReadAllTextAsync(absPath);
            SaveGame save = JsonSerializer.Deserialize<SaveGame>(json, JsonOpts);
            if (save == null)
            {
                GD.PushError($"[SaveLoad] Failed to deserialize save slot {id}: {path}");
                return;
            }

            if (_saveRegistry is GameManager gameManager)
            {
                gameManager.RestoreLoadedSave(save);
            }
            else
            {
                RestoreRegisteredManagers(save);
            }
        }

        public int GetNextSaveSlot()
        {
            for (int slot = 1; slot <= MaxSaveSlots; slot++)
            {
                if (!SaveExists(slot))
                {
                    return slot;
                }
            }

            return MaxSaveSlots;
        }

        public async Task SaveGameAsync(int id)
        {
            if (!IsValidSlot(id))
            {
                return;
            }

            GD.Print($"[SaveLoad] Saving slot {id}...");
            EnsureSaveFolder();

            SaveGame save = BuildSaveGame(id);
            string path = GetSavePath(id);
            string absPath = ProjectSettings.GlobalizePath(path);
            string json = JsonSerializer.Serialize(save, JsonOpts);
            await File.WriteAllTextAsync(absPath, json);

            GD.Print($"[SaveLoad] Saved slot {id} to: {path}");
        }

        public SaveSlotInfo GetSaveSlotInfo(int slot)
        {
            if (!IsValidSlot(slot, logError: false))
            {
                return SaveSlotInfo.Empty(slot);
            }

            string path = GetSavePath(slot);
            string absPath = ProjectSettings.GlobalizePath(path);
            if (!File.Exists(absPath))
            {
                return SaveSlotInfo.Empty(slot);
            }

            try
            {
                string json = File.ReadAllText(absPath);
                SaveGame save = JsonSerializer.Deserialize<SaveGame>(json, JsonOpts);
                if (save == null)
                {
                    return SaveSlotInfo.Empty(slot);
                }

                SaveMetadata metadata = save.Metadata ?? BuildMetadata(save.GameState?.WorldState, save.Player?.Player);
                return new SaveSlotInfo
                {
                    SlotNumber = slot,
                    Occupied = true,
                    SavePath = path,
                    PlayerName = SafeText(metadata.PlayerName, "Player"),
                    GameTimePlayed = SafeText(metadata.GameTimePlayed, FormatPlayTime(metadata.GameTimePlayedSeconds)),
                    SceneName = SafeText(metadata.SceneName, "Unknown location"),
                    CurrentMainQuestName = SafeText(metadata.CurrentMainQuestName, "No active main quest"),
                    SavedAt = save.SavedAt
                };
            }
            catch (Exception ex)
            {
                GD.PushError($"[SaveLoad] Failed to read save slot {slot}: {ex.Message}");
                return SaveSlotInfo.Empty(slot);
            }
        }

        public IReadOnlyList<SaveSlotInfo> GetSaveSlotInfos()
        {
            List<SaveSlotInfo> infos = new();
            for (int slot = 1; slot <= MaxSaveSlots; slot++)
            {
                infos.Add(GetSaveSlotInfo(slot));
            }

            GD.Print("[SaveLoad] Refreshed save slot metadata.");
            return infos;
        }

        public static PlayerSnapshot CreatePlayerSnapshot(Player player)
        {
            GameStateManager gameState = GameManager.Instance?.GameState;
            PlayerSnapshot snapshot = new()
            {
                PlayerName = SafeText(player?.Name, gameState?.PlayerName ?? "Player"),
                SceneId = SafeText(gameState?.CurrentSceneId, "LabScene"),
                SpawnId = SafeText(gameState?.CurrentSpawnId, "NewGameSpawn"),
                Position = ResolveCurrentPlayerPosition()
            };

            if (player is IStats stats)
            {
                snapshot.Stats["MaxHP"] = stats.MaxHP;
                snapshot.Stats["CurHP"] = stats.CurHP;
                snapshot.Stats["MaxMana"] = stats.MaxMana;
                snapshot.Stats["CurMana"] = stats.CurMana;
                snapshot.Stats["Strength"] = stats.Strength;
                snapshot.Stats["Dexterity"] = stats.Dexterity;
                snapshot.Stats["Intelligence"] = stats.Intelligence;
                snapshot.Stats["Spirit"] = stats.Spirit;
                snapshot.Stats["Vitality"] = stats.Vitality;
                snapshot.Stats["Luck"] = stats.Luck;
            }

            if (player?.Progression != null)
            {
                snapshot.Level = player.Progression.Level;
                snapshot.CurrentExperience = player.Progression.CurrentExperience;
                snapshot.TotalExperience = player.Progression.TotalExperience;
                snapshot.SkillPoints = player.Progression.SkillPoints;
            }

            if (player?.AbilityPath != null)
            {
                snapshot.AbilityPath = player.AbilityPath.CaptureSnapshot();
            }

            return snapshot;
        }

        public static string GetSavePath(int slotId) => $"user://saves/save_{slotId}.json";

        public static string FormatPlayTime(double seconds)
        {
            TimeSpan played = TimeSpan.FromSeconds(Math.Max(0d, seconds));
            return $"{(int)played.TotalHours:D2}:{played.Minutes:D2}:{played.Seconds:D2}";
        }

        private SaveGame BuildSaveGame(int slot)
        {
            GameManager gameManager = _saveRegistry as GameManager;
            GameStateSave gameState = CaptureGameState();
            PlayerSnapshot player = CreatePlayerSnapshot(gameManager?.GameState?.GetPlayer());
            InventorySave inventory = CaptureInventory();

            if (!string.IsNullOrWhiteSpace(gameState.WorldState?.CurrentLocation?.SceneId))
            {
                player.SceneId = gameState.WorldState.CurrentLocation.SceneId;
                player.SpawnId = SafeText(gameState.WorldState.CurrentLocation.SpawnId, player.SpawnId);
            }

            SaveGame save = new()
            {
                SlotNumber = slot,
                SavedAt = DateTime.UtcNow,
                Player = new EntitySave { Player = player },
                Inventory = inventory,
                GameState = gameState,
                Quest = BuildQuestSave(gameState.WorldState)
            };

            save.Metadata = BuildMetadata(gameState.WorldState, player);
            return save;
        }

        private GameStateSave CaptureGameState()
        {
            foreach (ISaveable saveable in _saveRegistry.All)
            {
                if (saveable.SaveKey == "GameState" && saveable.CaptureSnapshot() is GameStateSave save)
                {
                    return save;
                }
            }

            return new GameStateSave();
        }

        private InventorySave CaptureInventory()
        {
            foreach (ISaveable saveable in _saveRegistry.All)
            {
                if (saveable.SaveKey == "Inventory" && saveable.CaptureSnapshot() is InventorySave save)
                {
                    return save;
                }
            }

            return new InventorySave();
        }

        private QuestSave BuildQuestSave(WorldStateDto worldState)
        {
            string questName = ResolveMainQuestName(worldState);
            return new QuestSave
            {
                RuntimeQuestSaveSupported = false,
                CurrentMainStoryQuestId = worldState?.CurrentMainStoryQuestId ?? string.Empty,
                CurrentMainStoryQuestName = questName
            };
        }

        private static SaveMetadata BuildMetadata(WorldStateDto worldState, PlayerSnapshot player)
        {
            double playedSeconds = Math.Max(0d, worldState?.GameTimePlayedSeconds ?? 0d);
            return new SaveMetadata
            {
                PlayerName = SafeText(worldState?.PlayerName, player?.PlayerName ?? "Player"),
                GameTimePlayedSeconds = playedSeconds,
                GameTimePlayed = FormatPlayTime(playedSeconds),
                SceneName = ResolveSceneName(worldState, player),
                CurrentMainQuestName = ResolveMainQuestName(worldState)
            };
        }

        private static string ResolveSceneName(WorldStateDto worldState, PlayerSnapshot player)
        {
            if (!string.IsNullOrWhiteSpace(worldState?.CurrentSceneDisplayName))
            {
                return worldState.CurrentSceneDisplayName;
            }

            if (!string.IsNullOrWhiteSpace(worldState?.CurrentLocation?.SceneId))
            {
                return worldState.CurrentLocation.SceneId;
            }

            return SafeText(player?.SceneId, "Unknown location");
        }

        private static string ResolveMainQuestName(WorldStateDto worldState)
        {
            IReadOnlyList<QuestLogEntry> activeQuests = GameManager.Instance?.Quest?.GetQuestLogEntries(false);
            if (activeQuests != null && activeQuests.Count > 0)
            {
                return SafeText(activeQuests[0].Title, "No active main quest");
            }

            return SafeText(worldState?.CurrentMainStoryQuestName, "No active main quest");
        }

        private void RestoreRegisteredManagers(SaveGame save)
        {
            foreach (ISaveable saveable in _saveRegistry.All)
            {
                object dto = saveable.SaveKey switch
                {
                    "GameState" => save.GameState,
                    "Inventory" => save.Inventory,
                    "Player" => save.Player,
                    _ => null
                };

                if (dto != null)
                {
                    saveable.RestoreSnapshot(dto);
                }
            }
        }

        private static Vector2Snapshot ResolveCurrentPlayerPosition()
        {
            PlayerNode playerNode = GameManager.Instance?.GetTree()?.GetFirstNodeInGroup("Player") as PlayerNode;
            Vector2 position = playerNode?.GlobalPosition ?? Vector2.Zero;
            return new Vector2Snapshot
            {
                X = position.X,
                Y = position.Y
            };
        }

        private static bool SaveExists(int slot)
        {
            return File.Exists(ProjectSettings.GlobalizePath(GetSavePath(slot)));
        }

        private static void EnsureSaveFolder()
        {
            string savesAbs = ProjectSettings.GlobalizePath("user://saves");
            DirAccess.MakeDirRecursiveAbsolute(savesAbs);
        }

        private static bool IsValidSlot(int slot, bool logError = true)
        {
            bool valid = slot >= 1 && slot <= MaxSaveSlots;
            if (!valid && logError)
            {
                GD.PushError($"[SaveLoad] Slot {slot} is out of range 1-{MaxSaveSlots}.");
            }

            return valid;
        }

        private static string SafeText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }

    public class SaveSlotInfo
    {
        public int SlotNumber { get; set; }
        public bool Occupied { get; set; }
        public string SavePath { get; set; } = string.Empty;
        public string PlayerName { get; set; } = "Player";
        public string GameTimePlayed { get; set; } = "00:00:00";
        public string SceneName { get; set; } = string.Empty;
        public string CurrentMainQuestName { get; set; } = "No active main quest";
        public DateTime SavedAt { get; set; }

        public static SaveSlotInfo Empty(int slot)
        {
            return new SaveSlotInfo
            {
                SlotNumber = slot,
                Occupied = false
            };
        }
    }
}
