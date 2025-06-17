using System.Collections.Generic;
public class DialogueNode
{
    public string NodeId { get; init; }
    public string Speaker { get; init; }
    public string PortraitPath { get; init; }   // optional
    public string Text { get; init; }
    public List<DialogueChoice> Choices { get; init; } = new();
    public List<NodeAction> Actions { get; init; } = new(); // fire on entry
}