using System;
using Godot;
using System.Linq;
using System.Collections.Generic;


public partial class StateMachine : Node
{
    public BaseState _current;
    [Export] public NodePath InitialState;
    private Entity owner;

    private readonly Stack<BaseState> _stack = new();

    public override void _Ready()
    {
        // listen for the equipmentChange Event
        _current = GetNode<BaseState>(InitialState);
        owner = GetOwner<Entity>();

        _current?.Enter(owner, _current);
    }

    public override void _PhysicsProcess(double delta)
    {

        Advance((float)delta);
    }

    private void Advance(float d)
    {
        var next = _current?.Tick(owner, d, _current);
        if (next != null && next != _current)
        {
            GD.Print($"FSM Transition: {_current.Name} → {next.Name}");
            _current.Exit(owner);
            _current = next;
            _current.Enter(owner, _current);
        }
    }

    public BaseState GetState(StateType id)
    {
        return GetChildren().OfType<BaseState>()
                            .FirstOrDefault(s => s.StateId == id);
    }

    public BaseState CurrentState => _current;


    public void PushState(BaseState next)
    {
        if (next == null || next == _current) return;

        _stack.Push(_current);
        _current.Exit(owner);

        _current = next;
        _current.Enter(owner, _current);
    }

    public void PopState()
    {
        if (_stack.Count == 0) return;

        _current.Exit(owner);

        _current = _stack.Pop();
        _current.Enter(owner, _current);
    }
}