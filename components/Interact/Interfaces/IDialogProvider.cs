public interface IDialogueProvider
{
    /// <summary>
    /// Load a dialogue graph by its Id (e.g. "npc_example").
    /// Implementations may cache results internally.
    /// </summary>
    DialogueGraph LoadGraph(string graphId);
}