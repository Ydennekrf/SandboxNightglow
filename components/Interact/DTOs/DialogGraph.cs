using System.Collections.Generic;
public class DialogueGraph
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; }
    public List<DialogueNode> Nodes { get; init; } = new();
}