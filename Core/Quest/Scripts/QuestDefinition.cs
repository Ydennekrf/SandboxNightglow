using System.Collections.Generic;

namespace ethra.V1
{
    /// <summary>
    /// Static quest definition loaded from JSON by MasterRepository.
    /// </summary>
    /// <remarks>
    /// QuestId and objective IDs must stay stable. Runtime quest progress is stored separately in
    /// QuestRuntimeState and owned by QuestManager.
    /// </remarks>
    public class QuestDefinition
    {
        public string QuestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StartingObjectiveId { get; set; } = string.Empty;
        public int ExperienceReward { get; set; }
        public List<QuestObjectiveDefinition> Objectives { get; set; } = new();
        public List<string> Rewards { get; set; } = new();
    }
}
