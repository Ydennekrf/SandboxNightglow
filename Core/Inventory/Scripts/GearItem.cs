
using SQLitePCL;

namespace ethra.V1
{
    public class GearItem : InventoryItem
    {
        private string _slotName;
        private bool _isEquipped;

        private string _elemental;

        private int _gearLevel;

        public bool IsEquipped {get{return _isEquipped;} private set{_isEquipped = value;}}
        public string Elemental {get{return _elemental;} private set{_elemental = value;}}
        public int GearLevel {get{return _gearLevel;} private set{if(value > 0){_gearLevel = value;}}}


        public override void Use()
        {
            base.Use();

            if (_isEquipped)
            {
                // if equipped then remove effects
                foreach(ItemEffects effect in Effects)
                {
                if(effect is IEffect e)
                    {
                        e.RemoveItemEffect();
                    } 
                }

                _isEquipped = false;
                
            }
            else
            {

               foreach(ItemEffects effect in Effects)
                {
                if(effect is IEffect e)
                    {
                        e.ResolveItemEffect();
                    } 
                } 

                _isEquipped = true;
            }
            
        }

    }
}