using System.Collections.Generic;

namespace ethra.V1
{
    public class QuestDefinition
    {
        public string QuestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StartingObjectiveId { get; set; } = string.Empty;
        public List<QuestObjectiveDefinition> Objectives { get; set; } = new();
        public List<string> Rewards { get; set; } = new();
    }
}
