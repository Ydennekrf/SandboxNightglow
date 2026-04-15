

namespace ethra.V1
{
    public class MinusStat : ItemEffects, IEffect
    {

        private string _statKey;

        public string StatKey{get{return _statKey;} private set{_statKey = value;}}
        public MinusStat(string targetStat, int power, string name, Entity owner) : base(name, power, owner)
        {
            _statKey = targetStat;
        }
        public void RemoveItemEffect()
        {
            throw new System.NotImplementedException();
        }

        public void ResolveItemEffect()
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"+ {EffectPower} {StatKey}";
        }
    }
}