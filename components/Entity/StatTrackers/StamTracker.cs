using Godot;


public partial class StamTracker : TextureProgressBar
{

    [Export] public StatType TargetToTrack;
    [Export] public Entity owner;

    public override void _Ready()
    {

        EventManager.I.Subscribe<StatChange>(GameEvent.StatChange, OnStatChanged);

    }

    private void OnStatChanged(StatChange stat)
    {
       
        if (stat.stat.type == TargetToTrack && stat.owner == owner)
        {
             GD.Print($"Stat Change EVent Recieved!{stat.stat.type} new Value is: {stat.stat.Value}");
                MaxValue = (double)stat.stat.maxVal;
                Value    = (double)stat.stat.Value;

        }
        
    }
}