using System.Collections.Generic;
public class DialogueNode
{
    public string NodeId { get; init; }
    public string Speaker { get; init; }
    public string PortraitPath { get; init; }   // optional
    public string Text { get; init; }
    public IList<ChoiceRecord> Choices { get; init; }
    public IList<ActionRecord> Actions { get; init; }
}