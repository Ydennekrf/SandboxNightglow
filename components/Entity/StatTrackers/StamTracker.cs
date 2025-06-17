using Godot;


public partial class StamTracker : ProgressBar
{

    [Export] public StatType TargetToTrack;

    public override void _Ready()
    {

        EventManager.I.Subscribe<Stat>(GameEvent.StatChange, OnStatChanged);

    }

    private void OnStatChanged(Stat stat)
    {
       
        if (stat.type == TargetToTrack)
        {
             GD.Print($"Stat Change EVent Recieved!{stat.type} new Value is: {stat.Value}");
                MaxValue = (double)stat.maxVal;
                Value    = (double)stat.Value;

        }
        
    }
}