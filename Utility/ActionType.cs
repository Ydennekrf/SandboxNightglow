public enum ActionType : int
{
    None = 0,

    /* Dialogue-related */
    OpenStore          = 1,
    StartQuest         = 2,
    CompleteQuest      = 3,
    AdjustFriendship   = 4,
    GiveItem           = 5,
    TakeItem           = 6,
    PlayCutscene       = 7,

    //  Add more here ↓ as you grow the game
}