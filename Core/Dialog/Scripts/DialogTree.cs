using System.Collections.Generic;

namespace ethra.V1
{
    public class DialogTree
    {
        public string TreeId { get; set; } = string.Empty;
        public string StartingNodeId { get; set; } = string.Empty;
        public List<DialogNode> Nodes { get; set; } = new();
    }

    public class DialogNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string SpeakerName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<DialogChoice> Choices { get; set; } = new();
    }

    public class DialogChoice
    {
        public string ChoiceText { get; set; } = string.Empty;
        public string NextNodeId { get; set; } = string.Empty;
        public bool EndsDialog { get; set; }
        public string ActionId { get; set; } = string.Empty;
        public string ActionPayload { get; set; } = string.Empty;
    }
}
