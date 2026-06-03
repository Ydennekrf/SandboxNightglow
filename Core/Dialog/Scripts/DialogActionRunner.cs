using Godot;

namespace ethra.V1
{
    public static class DialogActionRunner
    {
        public const string StoreStubActionId = "store_stub";

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
                default:
                    GD.PushWarning($"DialogActionRunner: unknown dialog action '{choice.ActionId}'.");
                    break;
            }
        }
    }
}
