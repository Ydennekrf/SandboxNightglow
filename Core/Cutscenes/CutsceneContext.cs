using Godot;
using System.Collections.Generic;

namespace ethra.V1
{
    public sealed class CutsceneContext
    {
        private readonly Dictionary<string, Node> _spawnedEntities = new();

        public CutsceneContext(CutsceneOrchestrator orchestrator)
        {
            Orchestrator = orchestrator;
            GameManager = GameManager.Instance;
        }

        public CutsceneOrchestrator Orchestrator { get; }
        public GameManager GameManager { get; }
        public bool IsCancellationRequested { get; private set; }

        public void RequestCancel()
        {
            IsCancellationRequested = true;
        }

        public void RegisterSpawnedEntity(string id, Node entity)
        {
            if (string.IsNullOrWhiteSpace(id) || entity == null)
            {
                return;
            }

            _spawnedEntities[id.Trim()] = entity;
        }

        public Node GetSpawnedEntity(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return _spawnedEntities.TryGetValue(id.Trim(), out Node entity) ? entity : null;
        }

        public void Log(string message)
        {
            if (Orchestrator?.DebugLogging == true && !string.IsNullOrWhiteSpace(message))
            {
                GD.Print($"[Cutscene] {message}");
            }
        }
    }
}
