using System.Collections.Generic;
/// <summary>
/// Legacy-compatible serialized NPC relationship state.
/// </summary>
/// <remarks>
/// GameStateManager mirrors FriendshipByPersonId into this shape so older save/dialog paths remain readable.
/// </remarks>
public class NpcStateDto
{
    public string Id { get; set; }
    public int Friendship { get; set; }
    // extend later (quest flags, reputation, etc.)
}
