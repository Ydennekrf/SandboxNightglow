using Godot;
using System.Collections.Generic;
using System.Text;

namespace ethra.V1
{
    public partial class QuestLogPanel : VBoxContainer
    {
        [Export] public RichTextLabel QuestListLabel { get; set; }
        [Export] public bool ShowCompletedQuests { get; set; } = true;

        public override void _Ready()
        {
            GameManager.Instance?.Subscribe<QuestChangedEvent>(GameEvent.QuestStarted, OnQuestChanged);
            GameManager.Instance?.Subscribe<QuestChangedEvent>(GameEvent.QuestUpdated, OnQuestChanged);
            GameManager.Instance?.Subscribe<QuestChangedEvent>(GameEvent.QuestCompleted, OnQuestChanged);
            Refresh();
        }

        public override void _ExitTree()
        {
            GameManager.Instance?.Unsubscribe<QuestChangedEvent>(GameEvent.QuestStarted, OnQuestChanged);
            GameManager.Instance?.Unsubscribe<QuestChangedEvent>(GameEvent.QuestUpdated, OnQuestChanged);
            GameManager.Instance?.Unsubscribe<QuestChangedEvent>(GameEvent.QuestCompleted, OnQuestChanged);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationVisibilityChanged && Visible)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (QuestListLabel == null)
            {
                GD.PushWarning("QuestLogPanel: QuestListLabel is not assigned.");
                return;
            }

            IReadOnlyList<QuestLogEntry> entries = GameManager.Instance?.Quest?.GetQuestLogEntries(ShowCompletedQuests);
            if (entries == null || entries.Count == 0)
            {
                QuestListLabel.Text = "No quests tracked.";
                return;
            }

            StringBuilder builder = new();
            foreach (QuestLogEntry entry in entries)
            {
                AppendEntry(builder, entry);
            }

            QuestListLabel.Text = builder.ToString().TrimEnd();
        }

        private static void AppendEntry(StringBuilder builder, QuestLogEntry entry)
        {
            builder.AppendLine($"{entry.Title} [{entry.Status}]");

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                builder.AppendLine(entry.Description);
            }

            if (!string.IsNullOrWhiteSpace(entry.CurrentObjectiveDescription))
            {
                builder.Append("Objective: ");
                builder.Append(entry.CurrentObjectiveDescription);

                if (entry.RequiredCount > 1 || entry.CurrentCount > 0)
                {
                    builder.Append($" ({entry.CurrentCount}/{entry.RequiredCount})");
                }

                builder.AppendLine();
            }

            builder.AppendLine();
        }

        private void OnQuestChanged(QuestChangedEvent payload)
        {
            Refresh();
        }
    }
}
