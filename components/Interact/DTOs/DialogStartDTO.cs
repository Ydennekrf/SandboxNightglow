using Game.Interact;
public sealed class DialogueStartDTO
{
    public IInteractable Target { get; init; }   // the NPC (or chest, etc.)
    public Entity Initiator { get; init; }   // usually the Player
}