

namespace ethra.V1
{
    /// <summary>
    /// This Effect will give the Entity +Stats
    /// </summary>
    public class PlusStat : ItemEffects, IEffect
    {
        private string _statKey;

        public string StatKey{get{return _statKey;} private set{_statKey = value;}}
        public PlusStat(string targetStat, int power, string name, Entity owner) : base(name, power, owner)
        {
            _statKey = targetStat;
        }
        public void RemoveItemEffect()
        {
            if (Owner is not IStats stats)
            {
                return;
            }

            ModifyStat(stats, -EffectPower);
        }

        public void ResolveItemEffect()
        {
            if (Owner is not IStats stats)
            {
                return;
            }

            ModifyStat(stats, EffectPower);
        }

        private void ModifyStat(IStats stats, int delta)
        {
            switch (StatKey.ToLowerInvariant())
            {
                case "maxhp":
                    stats.MaxHP += delta;
                    break;
                case "maxmana":
                    stats.MaxMana += delta;
                    break;
                case "strength":
                case "str":
                    stats.Strength += delta;
                    break;
                case "dexterity":
                case "dex":
                    stats.Dexterity += delta;
                    break;
                case "intelligence":
                case "int":
                    stats.Intelligence += delta;
                    break;
                case "spirit":
                case "spi":
                    stats.Spirit += delta;
                    break;
                case "vitality":
                case "vit":
                    stats.Vitality += delta;
                    break;
                case "luck":
                case "luk":
                    stats.Luck += delta;
                    break;
            }
        }

        public override string ToString()
        {
            return $"+ {EffectPower} {StatKey}";
        }
    }
}
