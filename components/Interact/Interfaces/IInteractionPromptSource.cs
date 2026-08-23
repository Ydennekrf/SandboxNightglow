namespace Game.Interact
{
	public interface IInteractionPromptSource
	{
		string InteractionVerb => "Interact";
		string InteractionPromptText => $"Press E to {InteractionVerb}";
		int InteractionPriority => 0;
		bool CanInteract => true;
	}
}
