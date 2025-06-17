using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime container for all non‑player data that needs to persist:
/// • NPC friendship      (int)
/// • Later: quest flags, reputation, shop prices, etc.
/// 
/// This node should exist exactly once.  Add it to Autoload **or**
/// make sure the main scene instantiates it before gameplay starts.
/// </summary>
public partial class WorldStateManager : Node
{
    /* -------------------------------------------------------------------- */
    #region  Singleton boilerplate
    /* -------------------------------------------------------------------- */

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

    /* -------------------------------------------------------------------- */
    #endregion
    /* -------------------------------------------------------------------- */

    /// <summary>
    /// Fast lookup by NPC id.  Only the changed fields live here—static
    /// data like portraits or dialogue paths stay in JSON definition files.
    /// </summary>
    private readonly Dictionary<string, NpcStateDto> _npcs = new();

    /* -------------------------------------------------------------------- */
    #region  Public API used by gameplay
    /* -------------------------------------------------------------------- */

    /// <summary>
    /// Always returns a valid state object—creates it on first call.
    /// </summary>
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

    /* -------------------------------------------------------------------- */
    #endregion
    /* -------------------------------------------------------------------- */
    #region  DTO conversion (used later for save / load)
    /* -------------------------------------------------------------------- */

    public WorldStateDto ToDto()
        => new() { Npcs = _npcs.Values.ToList() };

    /// <summary>Replace runtime state from DTO (load).</summary>
    public void LoadFromDto(WorldStateDto dto)
    {
        _npcs.Clear();
        foreach (var npc in dto.Npcs)
            _npcs[npc.Id] = npc;
    }

    #endregion
}