using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
    public abstract class InventoryItem : IInventoryItem, IResolveable
    {
        protected int _id;
        protected string _name;
        protected int _value;
        protected string _description;
        protected string _rarity;
        protected string _category;
        protected string _subtype;
        protected int _maxStack = 99;
        protected List<ItemEffects> _effects;
        protected int _resolveOrder = 15;
        protected Entity _owner;

        public int Id => _id;
        public string Name => _name;
        public int Value => _value;
        public string Description => _description;
        public string Rarity => _rarity;
        public string Category => _category;
        public string Subtype => _subtype;
        public int MaxStack => _maxStack;
        public List<ItemEffects> Effects => _effects;
        public Entity Owner => _owner;

        public int ResolveOrder => _resolveOrder;

        protected InventoryItem()
        {
            _effects = new List<ItemEffects>();
        }

        protected InventoryItem(int id, string name, int value, string description, string rarity, List<ItemEffects> effects = null, string category = "", string subtype = "", int maxStack = 99)
        {
            _id = id;
            _name = name;
            _value = value;
            _description = description;
            _rarity = rarity;
            _category = category;
            _subtype = subtype;
            _maxStack = maxStack > 0 ? maxStack : 99;
            _effects = effects ?? new List<ItemEffects>();
        }

        public ItemInfo GetInfo()
        {
            List<string> effectList = new List<string>();

            foreach (ItemEffects effect in Effects)
            {
                effectList.Add(effect.ToString());
            }
            return new ItemInfo
            {
                Name = Name,
                Value = Value.ToString(),
                Description = Description,
                Rarity = Rarity,
                Effects = effectList.ToArray()
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
            string ownerName = _owner != null ? _owner.Name : "None";
            GD.Print($"===Item Used=== : {Name} on {ownerName}");
        }
    }
}
