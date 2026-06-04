using System.Collections.Generic;

namespace ethra.V1
{
    public class QuestRuntimeState
    {
        public string QuestId { get; set; } = string.Empty;
        public QuestStatus Status { get; set; } = QuestStatus.NotStarted;
        public string CurrentObjectiveId { get; set; } = string.Empty;
        public Dictionary<string, QuestObjectiveRuntimeState> ObjectiveProgress { get; set; } = new();
    }
}
