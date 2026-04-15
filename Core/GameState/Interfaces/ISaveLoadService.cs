


using System.Threading.Tasks;

namespace ethra.V1
{
    public interface ISaveLoadService
    {
        Task SaveGameAsync(int id);

        Task LoadGameAsync(int id);
    }
}