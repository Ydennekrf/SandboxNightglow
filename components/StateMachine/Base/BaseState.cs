using System;
using Godot;
using System.Collections.Generic;
using System.Linq;



public partial class BaseState : Node
{
    [Export] public StateType StateId = StateType.Idle;

    // Drag‑and‑drop composable actions & transitions in the Inspector
    [Export] public Godot.Collections.Array<NodePath> ActionPaths = new();
    [Export] public Godot.Collections.Array<NodePath> TransitionPaths = new();

    private List<IStateAction> _actions = new();
    protected List<IStateTransition> _transitions = new();
    protected List<IStateAction>      _runtimeAdded = new();

    public string InputActionName { get; private set; } = "";
    public void   SetInputAction(string action) => InputActionName = action;

    protected Entity _ownerCache;

    public override void _Ready()
    {
        // Resolve paths only once
        _actions = ActionPaths
                   .Select(p => GetNode(p) as IStateAction)
                   .Where(a => a != null)
                   .ToList();

        _transitions = TransitionPaths
                       .Select(p => GetNode(p) as IStateTransition)
                       .Where(t => t != null)
                       .ToList();
    }

    public virtual void Enter(Entity owner, BaseState state)
    {
        
        _ownerCache = owner; 
        foreach (var a in _actions) a.Enter(owner, state);

        // Let the rest of the game know we changed state
        EventManager.I.Publish(GameEvent.StateEntered, StateId);
    }

    public virtual BaseState Tick(Entity owner, float delta, BaseState baseState)
    {
        foreach (var a in _actions) a.Execute(delta, owner, this);

        foreach (var t in _transitions)
        {
            if (t.ShouldTransition(owner) && t.Target != this)
            {
                return t.Target;
                
            }                   // first valid transition wins
        }

        return this; // stay here
    }

    public virtual void Exit(Entity owner)
    {
        foreach (var a in _actions) a.Exit(owner);

        _actions.RemoveAll(_runtimeAdded.Contains);
        _runtimeAdded.Clear();

        _ownerCache = null;
        EventManager.I.Publish(GameEvent.StateExited, StateId);
    }
        

        public void AddAction(IStateAction action, bool runEnter = false)
{
    if (action == null || _actions.Contains(action)) return;
    _actions.Add(action);
    _runtimeAdded.Add(action);

    // If this state is currently active, immediately run Enter()
    if (runEnter && IsInsideTree() && _ownerCache != null)
            action.Enter(_ownerCache, this);
}

public void RemoveAction(IStateAction action, bool runExit = false)
{
    if (action == null || !_actions.Remove(action)) return;

    if (runExit && IsInsideTree() && _ownerCache != null)
        action.Exit(_ownerCache);
}

public void AddTransition(IStateTransition tr)
{
    if (tr != null && !_transitions.Contains(tr))
        _transitions.Add(tr);
}

public void RemoveTransition(IStateTransition tr)
{
    _transitions.Remove(tr);
}
}