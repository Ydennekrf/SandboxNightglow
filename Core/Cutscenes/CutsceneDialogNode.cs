using Godot;
using System.Threading.Tasks;

namespace ethra.V1
{
    public partial class CutsceneDialogNode : CutsceneStepNode
    {
        [ExportGroup("Dialog")]
        [Export] public NodePath DialogPanelPath { get; set; }
        [Export] public string SpeakerName { get; set; } = "Speaker";
        [Export] public string SpeakerId { get; set; } = string.Empty;
        [Export] public Texture2D Portrait { get; set; }
        [Export(PropertyHint.File, "*.png,*.jpg,*.jpeg,*.webp,*.svg")] public string PortraitPath { get; set; } = string.Empty;
        [Export(PropertyHint.MultilineText)] public string Body { get; set; } = string.Empty;
        [Export] public bool WaitForPlayerInput { get; set; } = true;
        [Export] public float AutoAdvanceDelaySeconds { get; set; } = 0f;

        public override async Task ExecuteAsync(CutsceneContext context)
        {
            InteractionDialogPanel panel = ResolveDialogPanel();
            if (panel == null)
            {
                Warn("InteractionDialogPanel was not found; skipping dialog line.");
                return;
            }

            Texture2D portrait = ResolvePortrait();
            DialogManager dialog = context?.GameManager?.Dialog;
            if (dialog != null)
            {
                await dialog.PlayCutsceneLineAsync(
                    panel,
                    SpeakerName,
                    Body,
                    portrait,
                    WaitForPlayerInput,
                    AutoAdvanceDelaySeconds,
                    string.IsNullOrWhiteSpace(StepId) ? Name : StepId);
                return;
            }

            bool advanced = !WaitForPlayerInput;
            panel.ShowDialog(
                SpeakerName,
                Body,
                portrait,
                WaitForPlayerInput
                    ? new[] { new InteractionDialogChoice("Continue", () => advanced = true) }
                    : System.Array.Empty<InteractionDialogChoice>());

            if (AutoAdvanceDelaySeconds > 0f)
            {
                await WaitSeconds(AutoAdvanceDelaySeconds);
                advanced = true;
            }

            while (!advanced && context?.IsCancellationRequested != true)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            panel.HideDialog();
        }

        private InteractionDialogPanel ResolveDialogPanel()
        {
            InteractionDialogPanel configured = ResolveConfiguredNode<InteractionDialogPanel>(DialogPanelPath);
            if (configured != null)
            {
                return configured;
            }

            return GetTree()?.CurrentScene?.GetNodeOrNull<InteractionDialogPanel>("UI/InteractionDialogPanel");
        }

        private Texture2D ResolvePortrait()
        {
            if (Portrait != null)
            {
                return Portrait;
            }

            if (string.IsNullOrWhiteSpace(PortraitPath))
            {
                return null;
            }

            Resource resource = ResourceLoader.Load(PortraitPath);
            if (resource is Texture2D texture)
            {
                return texture;
            }

            Warn($"PortraitPath '{PortraitPath}' did not load as a Texture2D.");
            return null;
        }

        public override string[] _GetConfigurationWarnings()
        {
            return string.IsNullOrWhiteSpace(Body)
                ? new[] { "Body is empty, so this dialog step will show a blank line." }
                : System.Array.Empty<string>();
        }
    }
}
