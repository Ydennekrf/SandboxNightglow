
namespace Game.Interact
{
	public interface IInteractable : IInteractionPromptSource
	{
		void BeginInteraction(DialogueStartDTO data);
	}
}
