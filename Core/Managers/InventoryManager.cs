using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace ethra.V1
{
    public partial class InventoryManager :  ISaveable, IInventory
	{
		private string _saveKey = "Inventory";
        /// <summary>
        /// <id, count>
        /// </summary>
        private Dictionary<int, int> _itemDict;
        private int maxStack = 99;
        private readonly MasterRepository _db;

		public string SaveKey => _saveKey;

        public InventoryManager(MasterRepository db)
        {
            _db = db;
            _itemDict = new Dictionary<int, int>();
        }


        public bool AddItem(int id)
        {
            InventoryItem itemData = _db.GetItemFromRepo(id);
            if(itemData == null)
            {
                GD.Print($"Unable to add item to inventory. id:{id} not found in repo.");
                return false;
            }

            int maxAllowed = itemData.MaxStack > 0 ? itemData.MaxStack : maxStack;

            if(_itemDict.TryGetValue(id, out int count))
            {
                if(count + 1 <= maxAllowed)
                {
                     _itemDict[id] = count + 1;
                     return true;
                }
                else
                {
                    GD.Print($"Unable to add item to inventory. max stack exceeded for id:{id}.");
                    return false;
                }
               
            }
            else
            {
                _itemDict.Add(id, 1);
                return true;
            }
        }

        public object CaptureSnapshot()
        {
            List<int> items = new List<int>();
            foreach(int id in _itemDict.Keys)
            {
                int count = _itemDict[id];
                for(int i =0; i < count; i++)
                {
                    items.Add(id);
                }
            }
            return items.ToArray();
        }

        public void DropItem(int id)
        {
            throw new NotImplementedException();
            // this will require a spawner manager or something.
        }


        public void RestoreSnapshot(object snapshot)
        {
            if(snapshot is List<int> listItems)
            {
                foreach(int i in listItems)
                {
                    AddItem(i);
                }
            }
            else if(snapshot is int[] arrayItems)
            {
                foreach(int i in arrayItems)
                {
                    AddItem(i);
                }
            }
        }

        public void UseItem(int id)
        {
            IInventoryItem itemToUse = _db.GetItemFromRepo(id);

            if(itemToUse != null)
            {
                itemToUse.Use();
            }
            else
            {
                GD.Print($"Item not found in master repo id:{id}");
            }
            

            // at this point check what type of item to know if you consume it on use, and drop the count of the inventory and call
            //then set the UI as dirty forcing it to update.
        }


    }
}
