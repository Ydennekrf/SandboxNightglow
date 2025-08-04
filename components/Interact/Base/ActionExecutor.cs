using Godot;
using System.Text.Json;

public static class ActionExecutor
{

    /// <summary>
    /// need to set the actionType in the Action Record properly, currently it is not being found.
    /// it needs to match the ActionType in the Enum so just look through the record in the SQL and it should work.
    /// </summary>
    /// <param name="act"></param>
    /// <param name="npcId"></param>
    public static void Run(ActionRecord act, string npcId)
    {
        switch (act.Type)
        {
            case ActionType.OpenStore:
                //EventManager.I.Publish(GameEvent.OpenStore, npcId);
                break;

            case ActionType.StartQuest:
                var questId = JsonDocument.Parse(act.JsonData)
                                          .RootElement.GetProperty("questId").GetString();
                //QuestSystem.I.StartQuest(questId);
                break;

            case ActionType.AdjustFriendship:
                var delta = JsonDocument.Parse(act.JsonData)
                                        .RootElement.GetProperty("delta").GetInt32();
                WorldStateManager.I.AdjustFriendship(npcId, delta);
                break;

            default:
                GD.PushWarning($"Unhandled Dialogue Action: {act.Type}");
                break;
        }
    }
}
