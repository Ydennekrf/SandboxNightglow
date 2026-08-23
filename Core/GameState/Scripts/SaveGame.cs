using System;

namespace ethra.V1
{
    public class SaveGame
    {
        public int SaveVersion { get; set; } = 1;
        public int SlotNumber { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public SaveMetadata Metadata { get; set; } = new();
        public EntitySave Player { get; set; } = new();
        public InventorySave Inventory { get; set; } = new();
        public GameStateSave GameState { get; set; } = new();
        public QuestSave Quest { get; set; } = new();
    }

    public class SaveMetadata
    {
        public string PlayerName { get; set; } = "Player";
        public double GameTimePlayedSeconds { get; set; }
        public string GameTimePlayed { get; set; } = "00:00:00";
        public string SceneName { get; set; } = string.Empty;
        public string CurrentMainQuestName { get; set; } = "No active main quest";
    }

    public class QuestSave
    {
        public bool RuntimeQuestSaveSupported { get; set; }
        public string CurrentMainStoryQuestId { get; set; } = string.Empty;
        public string CurrentMainStoryQuestName { get; set; } = "No active main quest";
    }
}
