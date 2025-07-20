using Godot;


public partial class StatTracker : TextureProgressBar
{

    [Export] StatType StatToTrack { get; set; }
    [Export] Entity _owner { get; set; }




    public override void _EnterTree()
    {
        EventManager.I.Subscribe<StatChange>(GameEvent.StatChange, OnStatChange);

        CallDeferred(nameof(InitialFill));
    }

        


private void InitialFill()
{
    MaxValue = (double)_owner.Data.EntityStats[StatToTrack].maxVal;
    Value    = _owner.Data.EntityStats[StatToTrack].Value;
}

    private void OnStatChange(StatChange e)
    {

        if (e.stat.type == StatToTrack)
        {
            if (_owner == e.owner)
            {
                Value = e.owner.Data.GetValue(e.stat.type);
            }
        }
    }

    // public override void _Process(double delta)
    // {
    //     if (_owner.Data.EntityStats.ContainsKey(StatToTrack))
    //     {
    //         Value = _owner.Data.EntityStats[StatToTrack].Value;
    //     }
    // }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<StatChange>(GameEvent.StatChange, OnStatChange);
    }
}