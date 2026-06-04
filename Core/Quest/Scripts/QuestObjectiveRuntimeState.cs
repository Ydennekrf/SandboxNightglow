namespace ethra.V1
{
    public class QuestObjectiveRuntimeState
    {
        public string ObjectiveId { get; set; } = string.Empty;
        public int CurrentCount { get; set; }
        public bool IsCompleted { get; set; }
    }
}
