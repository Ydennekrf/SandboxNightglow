using ethra.V1;

namespace Game.Interact
{
    public interface IPlayerInteractable : IInteractionPromptSource
    {
        void BeginInteraction(PlayerNode player);
    }
}
