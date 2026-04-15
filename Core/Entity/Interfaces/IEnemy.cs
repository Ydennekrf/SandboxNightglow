

namespace ethra.V1
{
    public interface IEnemy
    {
        void DropLoot(int tableID);

        void GiveExperience(Player player);

        void Despawn(int time);


    }
}