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
        }


        public void AddItem(int id)
        {
            if(_itemDict.TryGetValue(id, out int count))
            {
                if(count + 1 < maxStack)
                {
                     _itemDict[id] = count + 1;
                }
                else
                {
                    GD.Print("Unable to add item to inventory. max stack exceeded.");
                    DropItem(id);
                }
               
            }
            else
            {
                _itemDict.Add(id, 1);
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
            if(snapshot is List<int> items)
            {
                foreach(int i in items)
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

