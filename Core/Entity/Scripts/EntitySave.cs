using System.Collections.Generic;

namespace ethra.V1
{
    public class EntitySave
    {
        public PlayerSnapshot Player { get; set; } = new();
    }

    public class PlayerSnapshot
    {
        public string PlayerName { get; set; } = "Player";
        public string SceneId { get; set; } = "LabScene";
        public string SpawnId { get; set; } = "NewGameSpawn";
        public Vector2Snapshot Position { get; set; } = new();
        public Dictionary<string, int> Stats { get; set; } = new();
        public int Level { get; set; } = 1;
        public int CurrentExperience { get; set; }
        public int TotalExperience { get; set; }
        public int SkillPoints { get; set; }
        public int AbilityPoints
        {
            get => SkillPoints;
            set => SkillPoints = value;
        }
        public AbilityPathSave AbilityPath { get; set; } = new();
    }

    public class Vector2Snapshot
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
}
