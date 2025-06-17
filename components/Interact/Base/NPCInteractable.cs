using System;
using Godot;
using Game.Interact;

[Tool]  // let you set the DialogueId in the editor
public partial class NPCInteractable : CharacterBody2D, IInteractable
{
    [Export] public string DialogueId = "npc_example";

    public override void _Ready()
    {
        EventManager.I.Subscribe<DialogueStartDTO>(GameEvent.DialogStarted, BeginInteraction);
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<DialogueStartDTO>(GameEvent.DialogStarted, BeginInteraction);
    }


    public void BeginInteraction(DialogueStartDTO data)
    {
        
        if (this == data.Target)
        {
            DialogManager.I.PlayDialog(DialogueId, this, data.Initiator);
        }

    }
}