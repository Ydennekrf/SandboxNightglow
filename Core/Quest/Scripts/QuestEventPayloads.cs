namespace ethra.V1
{
    public readonly record struct NpcSpokenToQuestEvent(string NpcId);

    public readonly record struct ItemCollectedQuestEvent(int ItemId, int Quantity);

    public readonly record struct QuestChangedEvent(string QuestId, string ObjectiveId);
}
