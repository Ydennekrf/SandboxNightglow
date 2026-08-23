using System;
using Godot;
using Game.Interact;

[Tool]  // let you set the DialogueId in the editor
public partial class NPCInteractable : CharacterBody2D, IInteractable
{
    [Export] public string DialogueId = "npc_example";
    [Export] public string InteractionVerb { get; set; } = "Talk";
    [Export] public string InteractionPromptText { get; set; } = "Press E to Talk";
    [Export] public int InteractionPriority { get; set; } = 0;
    [Export] public bool CanInteract { get; set; } = true;

    public override void _Ready()
    {
        EventManager.I?.Subscribe<DialogueStartDTO>(GameEvent.DialogStarted, BeginInteraction);
    }

    public override void _ExitTree()
    {
        EventManager.I?.Unsubscribe<DialogueStartDTO>(GameEvent.DialogStarted, BeginInteraction);
    }


    public void BeginInteraction(DialogueStartDTO data)
    {
        
        if (this == data.Target)
        {
            if (DialogManager.I == null)
            {
                GD.PushWarning($"NPCInteractable: legacy DialogManager is unavailable for dialogue '{DialogueId}'.");
                return;
            }

            DialogManager.I.PlayDialog(DialogueId, this, data.Initiator);
        }

    }
}
