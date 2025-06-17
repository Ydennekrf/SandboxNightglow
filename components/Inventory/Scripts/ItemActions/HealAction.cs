using Godot;

[GlobalClass]                               // shows up in “New Resource” dialog
public partial class HealAction : ItemAction
{
    [Export] public int HealAmount = 25;

    public override bool Execute(Entity user, InventoryComponent inv, InventoryItem _)
    {
        var stats = user.Data.EntityStats;


        var data   = user.Data;
        int current = data.GetValue(StatType.CurrentHealth);
        int max     = data.GetValue(StatType.MaxHealth);
        int missing = max - current;

        if (missing <= 0)
        {
            GD.Print("[Heal] HP already full potion not consumed");
            return false;                       // don’t consume if no heal
        }

        int heal = Mathf.Min(HealAmount, missing);
        stats[StatType.CurrentHealth].changeStatValue(heal);  // raises StatChange

        GD.Print($"[Heal] Restored {heal} HP");
        return true; 
    }
}