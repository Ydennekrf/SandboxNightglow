using Godot;
using System.Collections.Generic;
using System.Linq;


public partial class WorldStateManager : Node
{
    // items this singleton still requires
    // a loop to handle the time of day
    // quest flags
    public static WorldStateManager I { get; private set; }

    public override void _EnterTree()
    {
        if (I != null && I != this)
        {
            QueueFree();                // ensure singleton
            return;
        }
        I = this;
    }


    // collection for npcs refrences
    private readonly Dictionary<string, NpcStateDto> _npcs = new();

    /// Gets the exsisting NPC Data or creates a new refrence to it
    public NpcStateDto GetOrCreate(string npcId)
    {
        if (!_npcs.ContainsKey(npcId))
        {
            _npcs[npcId] = new NpcStateDto { Id = npcId, Friendship = 0 };
        }
        return _npcs[npcId];
    }

    /// <summary>
    /// Utility called by AdjustFriendshipAction (dialogue system).
    /// </summary>
    public void AdjustFriendship(string npcId, int delta)
    {
        var state = GetOrCreate(npcId);
        state.Friendship += delta;
    }


    // used for saving the world state, if any values that are required to be saved get added, they must be added here.
    public WorldStateDto ToDto()
        => new() { Npcs = _npcs.Values.ToList() };

    // handles loading the world state DTO and putting it into practice
    public void LoadFromDto(WorldStateDto dto)
    {
        _npcs.Clear();
        foreach (var npc in dto.Npcs)
            _npcs[npc.Id] = npc;
    }
}