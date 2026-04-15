

using System.Collections.Generic;

namespace ethra.V1
{
    public interface IBoss
    {
        Dictionary<int, BossPhase> phaseDict { get; set; } 
        void ExecutePhase(int phaseID);
    }
}