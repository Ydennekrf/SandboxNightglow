


namespace ethra.V1
{
    public class SaveGame
    {
        public CombatSave combat { get; }
        public InventorySave inventory { get; }
        public EntitySave player { get; }
        public GameStateSave gameState { get; }
        
    }
}