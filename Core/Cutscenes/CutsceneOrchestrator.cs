using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class CutsceneOrchestrator : Node
    {
        [ExportGroup("Playback")]
        [Export] public string CutsceneId { get; set; } = string.Empty;
        [Export] public bool PlayOnReady { get; set; } = false;
        [Export] public bool CanSkip { get; set; } = true;
        [Export] public bool DisablePlayerControlDuringCutscene { get; set; } = true;
        [Export] public bool RestorePlayerControlOnEnd { get; set; } = true;
        [Export] public NodePath StepsRootPath { get; set; }
        [Export] public bool DebugLogging { get; set; } = false;

        private CutsceneContext _context;
        private PlayerNode _lockedPlayer;

        public bool IsPlaying { get; private set; }

        public override void _Ready()
        {
            if (PlayOnReady)
            {
                CallDeferred(nameof(Play));
            }
        }

        public async void Play()
        {
            await PlayAsync();
        }

        public void Stop()
        {
            _context?.RequestCancel();
        }

        public void Skip()
        {
            if (!CanSkip)
            {
                return;
            }

            _context?.RequestCancel();
        }

        private async Task PlayAsync()
        {
            if (IsPlaying)
            {
                GD.PushWarning($"{nameof(CutsceneOrchestrator)} '{Name}' is already playing.");
                return;
            }

            List<CutsceneStepNode> steps = CollectSteps();
            if (steps.Count == 0)
            {
                GD.PushWarning($"{nameof(CutsceneOrchestrator)} '{Name}' has no CutsceneStepNode children.");
                return;
            }

            IsPlaying = true;
            _context = new CutsceneContext(this);
            string eventId = string.IsNullOrWhiteSpace(CutsceneId) ? Name : CutsceneId;
            GameManager.Instance?.Publish(GameEvent.CutsceneStarted, new CutsceneStartedEvent(eventId));

            if (DisablePlayerControlDuringCutscene)
            {
                await LockPlayerControlAsync();
            }

            bool wasSkipped = false;
            try
            {
                foreach (CutsceneStepNode step in steps)
                {
                    if (_context.IsCancellationRequested)
                    {
                        wasSkipped = true;
                        break;
                    }

                    _context.Log($"Executing step '{step.Name}'.");
                    Task stepTask = step.ExecuteAsync(_context);
                    if (step.IsBlocking)
                    {
                        await stepTask;
                    }
                }
            }
            finally
            {
                wasSkipped = wasSkipped || _context.IsCancellationRequested;
                if (RestorePlayerControlOnEnd)
                {
                    RestorePlayerControl();
                }

                GameManager.Instance?.Publish(GameEvent.CutsceneEnded, new CutsceneEndedEvent(eventId, wasSkipped));
                IsPlaying = false;
                _context = null;
            }
        }

        private List<CutsceneStepNode> CollectSteps()
        {
            Node root = ResolveStepsRoot();
            return root.GetChildren()
                .OfType<CutsceneStepNode>()
                .ToList();
        }

        private Node ResolveStepsRoot()
        {
            if (!CutsceneStepNode.IsEmptyNodePath(StepsRootPath))
            {
                Node configured = GetNodeOrNull<Node>(StepsRootPath);
                if (configured != null)
                {
                    return configured;
                }

                GD.PushWarning($"{nameof(CutsceneOrchestrator)} '{Name}': StepsRootPath '{StepsRootPath}' was not found; using orchestrator children.");
            }

            return this;
        }

        private async Task LockPlayerControlAsync()
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                _lockedPlayer = GetTree()?.GetFirstNodeInGroup("Player") as PlayerNode;
                if (_lockedPlayer != null)
                {
                    break;
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            if (_lockedPlayer == null)
            {
                GD.PushWarning($"{nameof(CutsceneOrchestrator)} '{Name}': no PlayerNode in group 'Player' was found for control lock.");
                return;
            }

            _lockedPlayer.SetCutsceneControlled(true);
        }

        private void RestorePlayerControl()
        {
            if (_lockedPlayer == null || !IsInstanceValid(_lockedPlayer))
            {
                _lockedPlayer = null;
                return;
            }

            _lockedPlayer.SetCutsceneControlled(false);
            _lockedPlayer = null;
        }

        public override string[] _GetConfigurationWarnings()
        {
            if (GetChildren().OfType<CutsceneStepNode>().Any() || !CutsceneStepNode.IsEmptyNodePath(StepsRootPath))
            {
                return System.Array.Empty<string>();
            }

            return new[] { "Add CutsceneStepNode children or set StepsRootPath." };
        }
    }
}
