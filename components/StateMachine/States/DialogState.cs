using Godot;
using System.Threading.Tasks;
using Game.Interact;

public partial class ConversationState : BaseState
{
    private readonly IInteractable _target;
    private Entity _owner;

    public ConversationState(IInteractable t) => _target = t;

    public override void Enter(Entity owner, BaseState _)
    {
        if (owner is Player p) p.SetPhysicsProcess(false);
        _owner = owner;

         EventManager.I.Subscribe(GameEvent.DialogEnded, OnFinished);

        EventManager.I.Publish(GameEvent.DialogStarted, new DialogueStartDTO { Target = _target, Initiator = owner } );

       
    }

    public override void Exit(Entity owner)
    {
        if (owner is Player p) p.SetPhysicsProcess(true);

        
    }

    private void OnFinished()
    {
        EventManager.I.Unsubscribe(GameEvent.DialogEnded, OnFinished);
        _owner.fsm.PopState();
    }

    // Tick does nothing; we don’t leave until the signal fires.
    public override BaseState Tick(Entity owner, float delta, BaseState b) => this;
}