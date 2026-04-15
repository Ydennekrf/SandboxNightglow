using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ethra.V1
{
    public abstract class InventoryItem : IInventoryItem, IResolveable
    {
        private int _id;
        private string _name;
        private int _value;
        private string description;
        private string _rarity;
        private List<ItemEffects> _effects;
        private int _resolveOrder = 15;
        private Entity _owner;

        public int Id => _id;
        public string Name => _name;
        public int Value => _value;
        public string Description => description;
        public string Rarity => _rarity;
        public List<ItemEffects> Effects => _effects;
        public Entity Owner => _owner;

        public int ResolveOrder => _resolveOrder;



        public ItemInfo GetInfo()
        {
            string[] effectList = new string[12];

            foreach(ItemEffects effect in Effects)
            {
                effectList.Append(effect.ToString());
            }
            return new ItemInfo
            {
                Name = Name,
                Value = Value.ToString(),
                Description = Description,
                Rarity = Rarity,
                Effects = effectList
            };
        }

        public void Resolve()
        {
            throw new System.NotImplementedException();
        }

        public void Resolve(object obj)
        {
            throw new System.NotImplementedException();
        }

        public virtual void Use()
        {
            GD.Print($"===Item Used=== : {Name} on {_owner.Name}");
        }
    }
}