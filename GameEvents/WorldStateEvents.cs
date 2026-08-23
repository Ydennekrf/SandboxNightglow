/// <summary>Published by GameStateManager when a boolean world flag changes.</summary>
public readonly record struct GameFlagChangedEvent(string Key, bool Value);
/// <summary>Published by GameStateManager when an integer world value changes.</summary>
public readonly record struct GameIntChangedEvent(string Key, int Value);
/// <summary>Published by GameStateManager when a string world value changes.</summary>
public readonly record struct GameStringChangedEvent(string Key, string Value);
/// <summary>Published by GameStateManager when an area is marked explored.</summary>
public readonly record struct AreaExploredEvent(string AreaId);
/// <summary>Published by GameStateManager when an NPC/person friendship value changes.</summary>
public readonly record struct FriendshipChangedEvent(string PersonId, int Value);
/// <summary>Published by GameStateManager when the current save location changes.</summary>
public readonly record struct LocationChangedEvent(CurrentLocationDto Location);
/// <summary>Published by GameStateManager when the lightweight day/phase changes.</summary>
public readonly record struct TimeOfDayChangedEvent(TimeOfDayDto TimeOfDay);
