


namespace ethra.V1
{
    public interface IEntity
    {
        /// <summary>
        /// used to initialize a new Entity and register them within the Entity Manager
        /// </summary>
        void Initialize();

        

        /// <summary>
        /// use the entity ID to get their current Stats every entity will call this differently
        /// get the refrences from the Entity manager
        /// </summary>
        /// <param name="ID"></param>
        abstract void SetStats(int ID);

        /// <summary>
        ///  send the event to the dialog manager to update the current dialog with the Entity who is calling it and the dialog node they require
        /// </summary>
        /// <param name="EntityID">the entity that is calling for the update</param>
        /// <param name="NodeID">the Dialog Node from that Entity</param>
        //
        void UpdateDialog(int EntityID, int NodeID);

        /// <summary>
        /// Checks if there are any collisions happening on the Entity and resolves them by type.
        /// </summary>
        void CheckCollision();
    }
}