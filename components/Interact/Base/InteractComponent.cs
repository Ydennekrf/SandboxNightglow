using Godot;
using System.Collections.Generic;
using Game.Interact;

public partial class InteractComponent : Area2D
{
    [Export] public string InputAction = "Interact";
    private Entity _player;

    // keeps track of every interactable currently inside the bubble
    private readonly HashSet<IInteractable> _candidates = new();

    public override void _Ready()
    {
        _player = GetParent<Player>();

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("Interact") && _player.fsm._current is not ConversationState)
        {

            GD.Print($"Candidates: {_candidates.Count}");
            if (_candidates.Count == 0) return;

            // pick the closest interactable (simple heuristic)
            IInteractable nearest = null;
            float bestDist = float.MaxValue;

            foreach (var cand in _candidates)
            {
                if (cand is Node2D node)
                {
                    var d = node.GlobalPosition.DistanceTo(_player.GlobalPosition);
                    if (d < bestDist) { bestDist = d; nearest = cand; }
                }
            }

              if (nearest is IDialogueProvider)
    {
                // NPCs, talking chests, etc.
                _player.fsm.PushState(new ConversationState(nearest));
            }
            else
            {
                // Pickups, levers, doors, etc. – run immediately
                nearest.BeginInteraction(new DialogueStartDTO
                {
                    Target    = nearest,
                    Initiator = _player
                });
            }
        }

    }

    private void OnBodyEntered(Node body)
    {

        if (body is IInteractable ia)
        {
            _candidates.Add(ia);
        }
    }
    private void OnAreaEntered(Area2D area)
    {
        if (area is IInteractable target)
            _candidates.Add(target);   // whatever your list is called
    }

    private void OnBodyExited(Node body)
    {
        GD.Print("Exited");
        if (body is IInteractable ia) _candidates.Remove(ia);
    }
    private void OnAreaExited(Area2D area) {
        if (area is IInteractable ia) _candidates.Remove(ia);
    }
}
