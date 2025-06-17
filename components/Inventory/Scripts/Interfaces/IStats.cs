using System.Collections.Generic;

public interface IStats
{
    Dictionary<StatType, Stat> Stats { get; set; }

    Stat GetStat(StatType type) => Stats[type];

    bool TryGetStat(StatType type, out Stat stat) =>  Stats.TryGetValue(type, out stat);

}