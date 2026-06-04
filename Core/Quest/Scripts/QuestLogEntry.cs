namespace ethra.V1
{
    public class QuestLogEntry
    {
        public string QuestId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public QuestStatus Status { get; set; }
        public string CurrentObjectiveId { get; set; } = string.Empty;
        public string CurrentObjectiveDescription { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public int RequiredCount { get; set; } = 1;
        public bool CurrentObjectiveCompleted { get; set; }
    }
}
