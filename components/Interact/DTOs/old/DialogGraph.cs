using System.Collections.Generic;
public class DialogueGraph
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; }
    public string StartNodeId { get; init; }
    public List<DialogNodeRecord> Nodes { get; init; } = new();
}