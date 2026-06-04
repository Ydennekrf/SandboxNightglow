namespace ethra.V1
{
    public class QuestObjectiveDefinition
    {
        public string ObjectiveId { get; set; } = string.Empty;
        public QuestObjectiveType ObjectiveType { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public int RequiredCount { get; set; } = 1;
        public string AreaId { get; set; } = string.Empty;
        public float TimeLimitSeconds { get; set; }
        public string PersonId { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty;
        public int GoldAmount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
