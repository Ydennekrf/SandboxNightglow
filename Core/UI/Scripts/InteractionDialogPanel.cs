using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ethra.V1
{
    public readonly struct InteractionDialogChoice
    {
        public InteractionDialogChoice(string text, Action selected)
        {
            Text = text;
            Selected = selected;
        }

        public string Text { get; }
        public Action Selected { get; }
    }

    public partial class InteractionDialogPanel : Control
    {
        public event Action DialogOpened;
        public event Action DialogClosed;

        [Export] public NodePath PanelPath { get; set; } = "Panel";
        [Export] public NodePath SpeakerLabelPath { get; set; } = "Panel/MarginContainer/VBoxContainer/SpeakerLabel";
        [Export] public NodePath BodyLabelPath { get; set; } = "Panel/MarginContainer/VBoxContainer/BodyLabel";
        [Export] public NodePath ChoiceBoxPath { get; set; } = "Panel/MarginContainer/VBoxContainer/ChoiceBox";

        private Control _panel;
        private Label _speakerLabel;
        private Label _bodyLabel;
        private VBoxContainer _choiceBox;

        public bool IsOpen => Visible;

        public override void _Ready()
        {
            _panel = GetNodeOrNull<Control>(PanelPath);
            _speakerLabel = GetNodeOrNull<Label>(SpeakerLabelPath);
            _bodyLabel = GetNodeOrNull<Label>(BodyLabelPath);
            _choiceBox = GetNodeOrNull<VBoxContainer>(ChoiceBoxPath);

            if (_panel == null || _speakerLabel == null || _bodyLabel == null || _choiceBox == null)
            {
                GD.PushError("InteractionDialogPanel: required panel, label, or choice nodes are missing.");
            }

            HideDialog();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!Visible)
            {
                return;
            }

            if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
            {
                HideDialog();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventMouseButton mouseButton
                && mouseButton.Pressed
                && mouseButton.ButtonIndex == MouseButton.Left
                && _panel != null
                && !_panel.GetGlobalRect().HasPoint(mouseButton.GlobalPosition))
            {
                HideDialog();
                GetViewport().SetInputAsHandled();
            }
        }

        public void ShowDialog(string speaker, string body, IEnumerable<InteractionDialogChoice> choices)
        {
            if (_speakerLabel == null || _bodyLabel == null || _choiceBox == null)
            {
                return;
            }

            _speakerLabel.Text = speaker;
            _bodyLabel.Text = body;

            foreach (Node child in _choiceBox.GetChildren())
            {
                _choiceBox.RemoveChild(child);
                child.QueueFree();
            }

            List<InteractionDialogChoice> choiceList = choices?.ToList() ?? new List<InteractionDialogChoice>();
            foreach (InteractionDialogChoice choice in choiceList)
            {
                Button button = new()
                {
                    Text = choice.Text,
                    CustomMinimumSize = new Vector2(160f, 28f),
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };

                button.Pressed += () => choice.Selected?.Invoke();
                _choiceBox.AddChild(button);
            }

            bool wasOpen = Visible;
            Visible = true;

            if (!wasOpen)
            {
                DialogOpened?.Invoke();
            }

            if (_choiceBox.GetChildCount() > 0 && _choiceBox.GetChild(0) is Control firstChoice)
            {
                firstChoice.GrabFocus();
            }
        }

        public void HideDialog()
        {
            bool wasOpen = Visible;
            Visible = false;

            if (wasOpen)
            {
                DialogClosed?.Invoke();
            }
        }
    }
}
