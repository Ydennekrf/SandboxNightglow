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
        if (ethra.V1.GameManager.Instance?.GameState != null)
        {
            return new NpcStateDto
            {
                Id = npcId,
                Friendship = ethra.V1.GameManager.Instance.GameState.GetFriendship(npcId)
            };
        }

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
        if (ethra.V1.GameManager.Instance?.GameState != null)
        {
            ethra.V1.GameManager.Instance.GameState.AddFriendship(npcId, delta);
            return;
        }

        var state = GetOrCreate(npcId);
        state.Friendship += delta;
    }


    // used for saving the world state, if any values that are required to be saved get added, they must be added here.
    public WorldStateDto ToDto()
    {
        if (ethra.V1.GameManager.Instance?.GameState != null)
        {
            return ethra.V1.GameManager.Instance.GameState.CaptureWorldStateSnapshot();
        }

        return new() { Npcs = _npcs.Values.ToList() };
    }

    // handles loading the world state DTO and putting it into practice
    public void LoadFromDto(WorldStateDto dto)
    {
        if (ethra.V1.GameManager.Instance?.GameState != null)
        {
            ethra.V1.GameManager.Instance.GameState.RestoreWorldStateSnapshot(dto);
            return;
        }

        _npcs.Clear();
        if (dto?.Npcs == null)
        {
            return;
        }

        foreach (var npc in dto.Npcs)
            _npcs[npc.Id] = npc;
    }
}
