public enum StatType
{
    MaxHealth,
    CurrentHealth,
    MaxMana,
    CurrentMana,
    MaxStamina,
    CurrentStamina,
    AttackPower,
    DefencePower,
    MagicPower,
    MagicDefencePower,
    MoveSpeed,
    AttackSpeed,
    MagicSpeed,
    Experience,
    None
    }



public class Stat
{



    public StatType type { get; set; }
    public int? maxVal { get; }
    public int Value { get; set; }

    public Entity owner { get; }

    public Stat(StatType x, int y, int z, Entity entity)
    {
        owner = entity;
        type = x;
        Value = y;
        maxVal = z;
    }

    public bool changeStatValue(int val)
    {
        // change the value of the stat by in inputted val
        // handles + or -
        // returns false if Value becomes less than 0
        // returns true if the function goes through

        if (Value + val < 0)
        {
            return false;
        }

        Value += val;
        EventManager.I.Publish(GameEvent.StatChange, new StatChange(owner, this));
        return true;
    }

    public void SetCurrent(int val)
    {
        if (val > maxVal) return;
        
        Value = val;
        EventManager.I.Publish(GameEvent.StatChange, new StatChange(owner, this));
    }

}