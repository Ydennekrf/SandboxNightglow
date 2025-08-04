
using System.Collections.Generic;

public record DialogNodeRecord(
    string NodeId, string NpcId, string Speaker, string Portrait, string Text,
    IList<ChoiceRecord> Choices, IList<ActionRecord>  Actions);
