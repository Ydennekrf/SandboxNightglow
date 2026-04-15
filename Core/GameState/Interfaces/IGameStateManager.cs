


using System.Collections.Generic;

namespace ethra.V1
{
    public interface IGameStateManager
    {
        Player GetPlayer();

        NPC GetNPC(string name);

        bool SetPlayer(Player player);

    }
}