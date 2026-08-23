using System.Collections.Generic;

/// <summary>
/// Serializable world-state snapshot owned by GameStateManager.
/// </summary>
/// <remarks>
/// This DTO stores persistent world facts only: location, flags, counters, friendship, time, and quest refs.
/// Inventory, entity, and combat snapshots are serialized by their own managers.
/// </remarks>
public class WorldStateDto
{
    public string PlayerName { get; set; } = "Player";
    public double GameTimePlayedSeconds { get; set; }
    public string CurrentSceneDisplayName { get; set; } = string.Empty;
    public string CurrentMainStoryQuestId { get; set; } = string.Empty;
    public string CurrentMainStoryQuestName { get; set; } = "No main quest";
    public Dictionary<string, bool> Flags { get; set; } = new();
    public Dictionary<string, int> IntValues { get; set; } = new();
    public Dictionary<string, string> StringValues { get; set; } = new();
    public List<string> ExploredAreaIds { get; set; } = new();
    public Dictionary<string, int> FriendshipByPersonId { get; set; } = new();
    public CurrentLocationDto CurrentLocation { get; set; } = new();
    public TimeOfDayDto TimeOfDay { get; set; } = new();
    public Dictionary<string, string> QuestStateRefs { get; set; } = new();

    // Legacy shape used by the older WorldStateManager path. Keep it populated for compatibility.
    public List<NpcStateDto> Npcs { get; set; } = new();
}

/// <summary>
/// Serializable stable location reference used for save restoration and UI display.
/// </summary>
public class CurrentLocationDto
{
    public string SceneId { get; set; } = string.Empty;
    public string SpawnId { get; set; } = string.Empty;
    public string AreaId { get; set; } = string.Empty;
}

/// <summary>
/// Serializable day/phase state for lightweight world time tracking.
/// </summary>
public class TimeOfDayDto
{
    public int Day { get; set; } = 1;
    public string Phase { get; set; } = "Morning";
}
