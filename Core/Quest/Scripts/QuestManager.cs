using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public partial class QuestManager
    {
        private readonly MasterRepository _db;
        private readonly Dictionary<string, QuestRuntimeState> _questStates = new();

        public QuestManager(MasterRepository db)
        {
            _db = db;
        }

        public IReadOnlyDictionary<string, QuestRuntimeState> QuestStates => _questStates;

        public IReadOnlyList<QuestLogEntry> GetQuestLogEntries(bool includeCompleted = true)
        {
            List<QuestLogEntry> entries = new();

            foreach (QuestRuntimeState state in _questStates.Values)
            {
                if (!includeCompleted && state.Status == QuestStatus.Completed)
                {
                    continue;
                }

                QuestDefinition definition = _db?.GetQuestDefinitionFromRepo(state.QuestId);
                if (definition == null)
                {
                    continue;
                }

                entries.Add(BuildQuestLogEntry(definition, state));
            }

            return entries
                .OrderBy(entry => entry.Status == QuestStatus.Completed)
                .ThenBy(entry => entry.Title)
                .ToList();
        }

        public void Initialize()
        {
            GameManager.Instance?.Subscribe<NpcSpokenToQuestEvent>(GameEvent.NpcSpokenTo, OnNpcSpokenTo);
            GameManager.Instance?.Subscribe<ItemCollectedQuestEvent>(GameEvent.PickupItem, OnItemCollected);
        }

        public void Shutdown()
        {
            GameManager.Instance?.Unsubscribe<NpcSpokenToQuestEvent>(GameEvent.NpcSpokenTo, OnNpcSpokenTo);
            GameManager.Instance?.Unsubscribe<ItemCollectedQuestEvent>(GameEvent.PickupItem, OnItemCollected);
        }

        public bool StartQuest(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                GD.PushWarning("[Quest] Cannot start quest: quest id is empty.");
                return false;
            }

            QuestDefinition definition = _db?.GetQuestDefinitionFromRepo(questId);
            if (definition == null)
            {
                GD.PushWarning($"[Quest] Cannot start quest: definition not found for '{questId}'.");
                return false;
            }

            if (_questStates.TryGetValue(questId, out QuestRuntimeState existing))
            {
                if (existing.Status == QuestStatus.Active)
                {
                    GD.Print($"[Quest] Quest already active: {questId}");
                    return false;
                }

                if (existing.Status == QuestStatus.Completed)
                {
                    GD.Print($"[Quest] Quest already completed: {questId}");
                    return false;
                }
            }

            QuestRuntimeState state = CreateRuntimeState(definition);
            state.Status = QuestStatus.Active;
            _questStates[questId] = state;

            GD.Print($"[Quest] Started quest: {questId}");
            PublishQuestChanged(GameEvent.QuestStarted, state.QuestId, state.CurrentObjectiveId);
            return true;
        }

        private QuestRuntimeState CreateRuntimeState(QuestDefinition definition)
        {
            QuestRuntimeState state = new QuestRuntimeState
            {
                QuestId = definition.QuestId,
                Status = QuestStatus.NotStarted,
                CurrentObjectiveId = ResolveStartingObjectiveId(definition)
            };

            foreach (QuestObjectiveDefinition objective in definition.Objectives ?? Enumerable.Empty<QuestObjectiveDefinition>())
            {
                if (string.IsNullOrWhiteSpace(objective.ObjectiveId))
                {
                    continue;
                }

                state.ObjectiveProgress[objective.ObjectiveId] = new QuestObjectiveRuntimeState
                {
                    ObjectiveId = objective.ObjectiveId
                };
            }

            return state;
        }

        private static string ResolveStartingObjectiveId(QuestDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.StartingObjectiveId))
            {
                return definition.StartingObjectiveId;
            }

            return definition.Objectives?.FirstOrDefault()?.ObjectiveId ?? string.Empty;
        }

        private static QuestLogEntry BuildQuestLogEntry(QuestDefinition definition, QuestRuntimeState state)
        {
            QuestObjectiveDefinition objective = GetCurrentObjective(definition, state)
                ?? definition.Objectives?.FirstOrDefault();

            QuestObjectiveRuntimeState progress = null;
            if (objective != null)
            {
                state.ObjectiveProgress.TryGetValue(objective.ObjectiveId, out progress);
            }

            return new QuestLogEntry
            {
                QuestId = definition.QuestId,
                Title = string.IsNullOrWhiteSpace(definition.Title) ? definition.QuestId : definition.Title,
                Description = definition.Description,
                Status = state.Status,
                CurrentObjectiveId = objective?.ObjectiveId ?? string.Empty,
                CurrentObjectiveDescription = objective?.Description ?? string.Empty,
                CurrentCount = progress?.CurrentCount ?? 0,
                RequiredCount = objective == null ? 1 : Math.Max(1, objective.RequiredCount),
                CurrentObjectiveCompleted = progress?.IsCompleted ?? state.Status == QuestStatus.Completed
            };
        }

        private void OnNpcSpokenTo(NpcSpokenToQuestEvent payload)
        {
            if (string.IsNullOrWhiteSpace(payload.NpcId))
            {
                return;
            }

            UpdateActiveObjectives(objective =>
                objective.ObjectiveType == QuestObjectiveType.SpeakToNpc
                && string.Equals(objective.TargetId, payload.NpcId, StringComparison.OrdinalIgnoreCase),
                1);
        }

        private void OnItemCollected(ItemCollectedQuestEvent payload)
        {
            if (payload.ItemId <= 0 || payload.Quantity <= 0)
            {
                return;
            }

            string itemId = payload.ItemId.ToString();
            UpdateActiveObjectives(objective =>
                objective.ObjectiveType == QuestObjectiveType.CollectItem
                && string.Equals(objective.TargetId, itemId, StringComparison.OrdinalIgnoreCase),
                payload.Quantity);
        }

        private void UpdateActiveObjectives(Func<QuestObjectiveDefinition, bool> matches, int amount)
        {
            foreach (QuestRuntimeState state in _questStates.Values.ToList())
            {
                if (state.Status != QuestStatus.Active)
                {
                    continue;
                }

                QuestDefinition definition = _db.GetQuestDefinitionFromRepo(state.QuestId);
                QuestObjectiveDefinition objective = GetCurrentObjective(definition, state);
                if (objective == null || !matches(objective))
                {
                    continue;
                }

                AddProgress(definition, state, objective, amount);
            }
        }

        private static QuestObjectiveDefinition GetCurrentObjective(QuestDefinition definition, QuestRuntimeState state)
        {
            if (definition?.Objectives == null || string.IsNullOrWhiteSpace(state.CurrentObjectiveId))
            {
                return null;
            }

            return definition.Objectives.FirstOrDefault(objective => objective.ObjectiveId == state.CurrentObjectiveId);
        }

        private void AddProgress(QuestDefinition definition, QuestRuntimeState state, QuestObjectiveDefinition objective, int amount)
        {
            if (!state.ObjectiveProgress.TryGetValue(objective.ObjectiveId, out QuestObjectiveRuntimeState progress))
            {
                progress = new QuestObjectiveRuntimeState { ObjectiveId = objective.ObjectiveId };
                state.ObjectiveProgress[objective.ObjectiveId] = progress;
            }

            if (progress.IsCompleted)
            {
                return;
            }

            int required = Math.Max(1, objective.RequiredCount);
            progress.CurrentCount = Math.Min(required, progress.CurrentCount + Math.Max(1, amount));

            if (progress.CurrentCount < required)
            {
                PublishQuestChanged(GameEvent.QuestUpdated, state.QuestId, objective.ObjectiveId);
                return;
            }

            CompleteObjective(definition, state, progress);
        }

        private void CompleteObjective(QuestDefinition definition, QuestRuntimeState state, QuestObjectiveRuntimeState progress)
        {
            progress.IsCompleted = true;
            GD.Print($"[Quest] Objective completed: {progress.ObjectiveId}");
            PublishQuestChanged(GameEvent.QuestUpdated, state.QuestId, progress.ObjectiveId);

            QuestObjectiveDefinition nextObjective = GetNextObjective(definition, progress.ObjectiveId);
            if (nextObjective != null)
            {
                state.CurrentObjectiveId = nextObjective.ObjectiveId;
                return;
            }

            state.Status = QuestStatus.Completed;
            state.CurrentObjectiveId = string.Empty;
            GD.Print($"[Quest] Completed quest: {state.QuestId}");
            PublishQuestChanged(GameEvent.QuestCompleted, state.QuestId, progress.ObjectiveId);
        }

        private static QuestObjectiveDefinition GetNextObjective(QuestDefinition definition, string completedObjectiveId)
        {
            if (definition?.Objectives == null)
            {
                return null;
            }

            for (int i = 0; i < definition.Objectives.Count - 1; i++)
            {
                if (definition.Objectives[i].ObjectiveId == completedObjectiveId)
                {
                    return definition.Objectives[i + 1];
                }
            }

            return null;
        }

        private static void PublishQuestChanged(GameEvent evt, string questId, string objectiveId)
        {
            GameManager.Instance?.Publish(evt, new QuestChangedEvent(questId, objectiveId));
        }
    }
}
