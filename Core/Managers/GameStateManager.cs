using Godot;
using System;
using System.Collections.Generic;

namespace ethra.V1{
	/// <summary>
    /// tracks all things in the current game world from the player's inventory, NPCs and their firendshipscores, Quests and how they were completed, WOrld Events and their respective feature flags
    /// </summary>
	public partial class GameStateManager : IGameStateManager, ISaveable, IResolveable
{
		private string CurrentSceneName { get; set; }
		private Vector2 PlayerPos { get; set; }
        private Player _player;
        private string _saveKey = "GameState";
        private int _resolveOrder = 5;

		public string SaveKey => _saveKey;

        public int ResolveOrder => _resolveOrder;


        // <string npc name, NPCData>

        private Dictionary<string, NPCData> _npcData;

		/// <summary>
		/// returns the game's current player, for multiplayer this will require a search feature.
		/// </summary>
		/// <returns></returns>
		public Player GetPlayer()
		{
			return _player;
		}

        public bool SetPlayer(Player player)
        {
            _player = player;

            if(_player != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public NPC GetNPC(string name)
        {
            throw new NotImplementedException();
        }
        //==========ISavable==========//
        public object CaptureSnapshot()
        {
            throw new NotImplementedException();
        }

        public void RestoreSnapshot(object snapshot)
        {
            throw new NotImplementedException();
        }

        //=======IResolveable=======//
        public void Resolve()
        {
            
        }

        public void Resolve(object obj)
        {
            
        }
    }
}

