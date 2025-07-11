using Godot;
using System;
using System.Linq;

public partial class DialogPanel : Control
{

    [Export] private NodePath PortraitPath;
    [Export] private NodePath NamePath;
    [Export] private NodePath TextPath;
    [Export] private NodePath ChoiceBoxPath;
    [Export] private PackedScene ChoiceButtonScene; // a Theme’d Button

    private TextureRect     _portrait;
    private Label           _name;
    private RichTextLabel   _text;
    private VBoxContainer   _choiceBox;

    public override void _Ready()
    {
        _portrait  = GetNode<TextureRect>(PortraitPath);
        _name      = GetNode<Label>(NamePath);
        _text      = GetNode<RichTextLabel>(TextPath);
        _choiceBox = GetNode<VBoxContainer>(ChoiceBoxPath);
    }

    public void SetSpeaker(string speaker, string portraitPath)
    {
        _name.Text = speaker;
        _portrait.Texture = !string.IsNullOrEmpty(portraitPath)
            ? GD.Load<Texture2D>(portraitPath)
            : null;
    }

    public void SetText(string text) => _text.Text = text;

    public void BuildChoices(System.Collections.Generic.IEnumerable<DialogueChoice> choices)
    {
        _choiceBox.QueueFreeChildren();

        foreach (var c in choices)
        {
            var btn = ChoiceButtonScene.Instantiate<Button>();
            btn.Text = c.Text;
            btn.Pressed += () =>
                {
                    EventManager.I.Publish(GameEvent.DialogChoiceSelected, c.ChoiceId);
                };

            _choiceBox.AddChild(btn);
        }

        // If no choices (end node), show a default "Continue" to close.
        if (!choices.Any())
        {
            var btn = ChoiceButtonScene.Instantiate<Button>();
            btn.Text = "Continue";
                        btn.Pressed += () =>
                {
                    EventManager.I.Publish(GameEvent.DialogChoiceSelected, "");
                };
            _choiceBox.AddChild(btn);
        }

        Show();
    }
}