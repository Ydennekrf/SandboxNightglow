// using Godot;

// public static class DialogueUtility
// {
//     public static void Execute(NodeAction action)  // for node‑level actions
//     {

//     }

//     public static void Execute(ChoiceAction action) // for choice actions
//     {
//         switch (action)
//         {
//             case OpenStoreAction:
//                 GD.Print("Open Store (placeholder)");
//                 break;

//             case AdjustFriendshipAction f:
//                 WorldStateManager.I.AdjustFriendship(f.TargetNpcId, f.Delta);
//                 break;
//             case StartQuestAction s:
//                 GD.Print($"Start quest: {s.QuestId}");
//                 // QuestSystem.I.BeginQuest(s.QuestId);
//                 break;
//         }
//     }
// }
