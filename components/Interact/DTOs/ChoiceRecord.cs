using System.Collections.Generic;

public record ChoiceRecord(
    string ChoiceId, string NodeId, int SortIndex, string Text,
    string? NextNodeId, IList<ActionRecord> Actions);