

namespace ethra.V1
{
    public interface IInventory
    {
        
        void UseItem(int id);

        bool AddItem(int id);

        void DropItem(int id);


    }
}
