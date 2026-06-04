using Godot;

namespace ethra.V1
{
    public static class DialogConditionRunner
    {
        public const string DebugTrueConditionId = "debug_true";
        public const string DebugFalseConditionId = "debug_false";
        public const string NpcFriendshipGreaterThanStubConditionId = "npc_friendship_greater_than_stub";
        public const string QuestCompleteStubConditionId = "quest_complete_stub";
        public const string ItemInInventoryStubConditionId = "item_in_inventory_stub";

        public static bool IsMet(DialogChoice choice, string npcName)
        {
            if (choice == null || string.IsNullOrWhiteSpace(choice.ConditionId))
            {
                return true;
            }

            return choice.ConditionId switch
            {
                DebugTrueConditionId => true,
                DebugFalseConditionId => false,
                NpcFriendshipGreaterThanStubConditionId => HandleUnavailableStub(choice, npcName, "NPC friendship"),
                QuestCompleteStubConditionId => HandleUnavailableStub(choice, npcName, "quest completion"),
                ItemInInventoryStubConditionId => HandleUnavailableStub(choice, npcName, "inventory item"),
                _ => HandleUnknownCondition(choice, npcName)
            };
        }

        public static bool IsKnownCondition(string conditionId)
        {
            return string.IsNullOrWhiteSpace(conditionId)
                || conditionId == DebugTrueConditionId
                || conditionId == DebugFalseConditionId
                || conditionId == NpcFriendshipGreaterThanStubConditionId
                || conditionId == QuestCompleteStubConditionId
                || conditionId == ItemInInventoryStubConditionId;
        }

        private static bool HandleUnavailableStub(DialogChoice choice, string npcName, string conditionName)
        {
            GD.Print(
                $"[DialogConditionStub] '{conditionName}' condition is known but no backing system is wired yet. NPC: {npcName}; Choice: {choice.ChoiceText}; Payload: {choice.ConditionPayload}");
            return false;
        }

        private static bool HandleUnknownCondition(DialogChoice choice, string npcName)
        {
            GD.PushWarning(
                $"DialogConditionRunner: unknown condition '{choice.ConditionId}' on choice '{choice.ChoiceText}' for NPC '{npcName}'. Choice will be hidden.");
            return false;
        }
    }
}
