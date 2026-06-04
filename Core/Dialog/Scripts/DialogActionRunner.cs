using Godot;

namespace ethra.V1
{
    public static class DialogActionRunner
    {
        public const string StoreStubActionId = "store_stub";
        public const string StartQuestActionId = "start_quest";
        public const string CompleteQuestStubActionId = "complete_quest_stub";
        public const string UpdateFriendshipStubActionId = "update_friendship_stub";

        public static void Run(DialogChoice choice, string npcName)
        {
            if (choice == null || string.IsNullOrWhiteSpace(choice.ActionId))
            {
                return;
            }

            switch (choice.ActionId)
            {
                case StoreStubActionId:
                    GD.Print($"[StoreStub] Opening store screen for NPC: {npcName}");
                    break;
                case StartQuestActionId:
                    StartQuest(choice.ActionPayload);
                    break;
                case CompleteQuestStubActionId:
                    GD.Print($"[QuestStub] Completing quest from dialog. NPC: {npcName}; Payload: {choice.ActionPayload}");
                    break;
                case UpdateFriendshipStubActionId:
                    GD.Print($"[FriendshipStub] Updating friendship from dialog. NPC: {npcName}; Payload: {choice.ActionPayload}");
                    break;
                default:
                    GD.PushWarning($"DialogActionRunner: unknown dialog action '{choice.ActionId}'.");
                    break;
            }
        }

        public static bool IsKnownAction(string actionId)
        {
            return string.IsNullOrWhiteSpace(actionId)
                || actionId == StoreStubActionId
                || actionId == StartQuestActionId
                || actionId == CompleteQuestStubActionId
                || actionId == UpdateFriendshipStubActionId;
        }

        private static void StartQuest(string questId)
        {
            if (GameManager.Instance?.Quest == null)
            {
                GD.PushWarning($"[Quest] Cannot start quest '{questId}': QuestManager is not available.");
                return;
            }

            GameManager.Instance.Quest.StartQuest(questId);
        }
    }
}
