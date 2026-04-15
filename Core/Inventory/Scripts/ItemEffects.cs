

namespace ethra.V1
{
    public abstract class ItemEffects
    {

       private string _effectName;
       private int _effectPower;

       private Entity _owner;

       public string EffectName {get{return _effectName; } set{ _effectName = value; }}

       public int EffectPower {get {return _effectPower;} private set {_effectPower = value;}}

       public Entity Owner => _owner;

       public ItemEffects(string name, int power, Entity owner)
        {
            _effectName = name;
            _effectPower = power;
            _owner = owner;
        }

        
    }
}