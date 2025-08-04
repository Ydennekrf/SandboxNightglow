using System.Collections.Generic;
public class DialogueChoice
{
    public string ChoiceId { get; init; }
    public string Text { get; init; }
    public string? NextNodeId { get; init; }
    public List<ChoiceAction> Actions { get; init; } = new(); // fire on click
}