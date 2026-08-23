


using System.Threading.Tasks;
using System.Collections.Generic;

namespace ethra.V1
{
    public interface ISaveLoadService
    {
        Task SaveGameAsync(int id);

        Task LoadGameAsync(int id);

        IReadOnlyList<SaveSlotInfo> GetSaveSlotInfos();

        SaveSlotInfo GetSaveSlotInfo(int slot);
    }
}
