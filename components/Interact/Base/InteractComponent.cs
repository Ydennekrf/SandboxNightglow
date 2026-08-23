using Godot;
using System.Collections.Generic;
using Game.Interact;

public partial class InteractComponent : Area2D
{
    private const string DefaultInputAction = "Interact";

    [Export] public string InputAction = "Interact";

    private Entity _legacyPlayer;
    private Node2D _playerNode;
    private readonly HashSet<IInteractionPromptSource> _candidates = new();
    private IInteractionPromptSource _currentPromptTarget;

    public override void _Ready()
    {
        InputAction = ResolveInputAction();

        Node parent = GetParent();
        _legacyPlayer = parent as Entity;
        _playerNode = parent as Node2D;

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        string inputAction = ResolveInputAction();
        if (!@event.IsActionPressed(inputAction))
        {
            return;
        }

        if (ethra.V1.GameManager.Instance?.UI?.BlocksGameplayInput == true)
        {
            return;
        }

        if (_legacyPlayer?.fsm?._current is ConversationState)
        {
            return;
        }

        IInteractionPromptSource nearest = PickBestInteractable();
        if (nearest == null)
        {
            return;
        }

        if (_playerNode is ethra.V1.PlayerNode playerNode && nearest is IPlayerInteractable playerInteractable)
        {
            playerInteractable.BeginInteraction(playerNode);
            return;
        }

        if (_legacyPlayer == null)
        {
            return;
        }

        if (nearest is IDialogueProvider)
        {
            _legacyPlayer.fsm.PushState(new ConversationState((IInteractable)nearest));
            return;
        }

        if (nearest is IInteractable legacyInteractable)
        {
            legacyInteractable.BeginInteraction(new DialogueStartDTO
            {
                Target = legacyInteractable,
                Initiator = _legacyPlayer
            });
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body is IInteractionPromptSource interactable)
        {
            _candidates.Add(interactable);
            RefreshPrompt();
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is IInteractionPromptSource interactable)
        {
            _candidates.Add(interactable);
            RefreshPrompt();
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is IInteractionPromptSource interactable)
        {
            _candidates.Remove(interactable);
            RefreshPrompt();
        }
    }

    private void OnAreaExited(Area2D area)
    {
        if (area is IInteractionPromptSource interactable)
        {
            _candidates.Remove(interactable);
            RefreshPrompt();
        }
    }

    private IInteractionPromptSource PickBestInteractable()
    {
        IInteractionPromptSource best = null;
        int bestPriority = int.MinValue;
        float bestDistance = float.MaxValue;

        foreach (IInteractionPromptSource candidate in _candidates)
        {
            if (!candidate.CanInteract || candidate is not Node2D node)
            {
                continue;
            }

            int priority = candidate.InteractionPriority;
            float distance = _playerNode == null
                ? 0f
                : node.GlobalPosition.DistanceTo(_playerNode.GlobalPosition);

            if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
            {
                bestPriority = priority;
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private void RefreshPrompt()
    {
        IInteractionPromptSource selected = PickBestInteractable();
        if (selected == _currentPromptTarget)
        {
            return;
        }

        _currentPromptTarget = selected;
        PublishPrompt(selected?.InteractionPromptText ?? string.Empty, selected as Node);
    }

    private static void PublishPrompt(string promptText, Node source)
    {
        ethra.V1.GameManager gameManager = ethra.V1.GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(promptText, source));
            return;
        }

        EventManager.I?.Publish(GameEvent.InteractionPromptChanged, new InteractionPromptChanged(promptText, source));
    }

    private string ResolveInputAction()
    {
        return string.IsNullOrWhiteSpace(InputAction) ? DefaultInputAction : InputAction;
    }
}
