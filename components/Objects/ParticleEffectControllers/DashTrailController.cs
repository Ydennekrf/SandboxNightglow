using Godot;

public partial class DashTrailController : GpuParticles2D
{
  
    [Export] public StateType DashStateId  = StateType.Dodge;


    public override void _Ready()
    {

        Emitting = false;

        EventManager.I.Subscribe<StateType>(GameEvent.StateEntered, OnStateEnter);
        EventManager.I.Subscribe<StateType>(GameEvent.StateExited,  OnStateExit);
    }

    private void OnStateEnter(StateType id)
    {
        if (id == DashStateId)
            Emitting = true;   // one‑shot will auto‑stop
    }

    private void OnStateExit(StateType id)
    {
        if (id == DashStateId)
            Emitting = false;  // safety, in case dash was interrupted
    }
}