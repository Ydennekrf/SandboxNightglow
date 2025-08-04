// using Godot;
// using System;
// using System.Linq;

// public partial class DialogManager : Node
// {
//     public static DialogManager I { get; private set; }

//     [Export] public PackedScene DialoguePanelScene;

//     private IDialogProvider _provider;
//     private DialogueGraph     _graph;
//     private DialogNodeRecord  _currentNode;
//     private DialogPanel       _panel;
//     private Entity            _player;          // ← store once per conversation

//     // ────────────────────────────────────────────────────────
//     // LIFECYCLE
//     // ────────────────────────────────────────────────────────
//     public override void _EnterTree()
//     {
//         if (I != null && I != this)
//         {
//             QueueFree();
//             return;
//         }

//         I = this;
//         _provider = new JsonDialogueProvider();
//         EventManager.I.Subscribe<string>(GameEvent.DialogChoiceSelected, OnChoice);
//     }

//     public override void _ExitTree()
//     {
//         EventManager.I.Unsubscribe<string>(GameEvent.DialogChoiceSelected, OnChoice);
//     }

//     // ────────────────────────────────────────────────────────
//     // PUBLIC API
//     // ────────────────────────────────────────────────────────
//     public void PlayDialog(string dialogId, object dialogTarget, Entity player)
//     {
//         _player      = player;                     // keep the reference
//         _graph       = _provider.LoadGraph(dialogId);
//         _currentNode = _graph.Nodes.First(n => n.NodeId == "greet");

//         if (_panel == null)
//         {
//             _panel = DialoguePanelScene.Instantiate<DialogPanel>();
//             AddChild(_panel);
//         }
//         else
//         {
//             _panel.Show();                         // reuse existing panel
//         }

//         ShowNode();
//     }

//     // ────────────────────────────────────────────────────────
//     // PRIVATE HELPERS
//     // ────────────────────────────────────────────────────────
//     private void ShowNode()
//     {
//         GD.Print(WorldStateManager.I.GetOrCreate("npc_example").Friendship);
//         var text = _currentNode.Text.Replace("{playerName}", _player.Data.EntityName);

//         _panel.SetSpeaker(_currentNode.Speaker, _currentNode.PortraitPath);
//         _panel.SetText(text);

//         // Node‑level actions (fire immediately)
//         foreach (var act in _currentNode.Actions)
//             DialogueUtility.Execute(act);

//         _panel.BuildChoices(_currentNode.Choices);
//     }

//     private void OnChoice(string choiceId)
//     {
//         // empty choiceId (from “Continue”) or no match ⇒ end dialogue
//         var choice = _currentNode.Choices.FirstOrDefault(c => c.ChoiceId == choiceId);
//         if (choice == null)
//         {
//             Finish();
//             return;
//         }

//         // Choice‑level actions
//         foreach (var act in choice.Actions)
//             ActionExecutor.Run(act, _graph.Id);

//         if (string.IsNullOrEmpty(choice.NextNodeId))
//         {
//             Finish();
//             return;
//         }

//         _currentNode = _graph.Nodes.First(n => n.NodeId == choice.NextNodeId);
//         ShowNode();                                // reuse stored _player
//     }

//     private void Finish()
//     {
//         _panel.Hide();                             // or _panel.QueueFree() if you prefer
//         EventManager.I.Publish(GameEvent.DialogEnded);
//     }
// }


using Godot;
using System.Linq;

public partial class DialogManager : Node
{
    public static DialogManager I { get; private set; }

    [Export] public PackedScene DialoguePanelScene;

    private DialogueGraph    _graph;
    private DialogNodeRecord _currentNode;
    private DialogPanel      _panel;
    private Entity           _player;

    private const string FallbackStart = "greet";

    public override void _EnterTree()
    {
        if (I != null && I != this) { QueueFree(); return; }
        I = this;
        EventManager.I.Subscribe<string>(GameEvent.DialogChoiceSelected, OnChoice);
    }

    public override void _ExitTree()
    {
        EventManager.I.Unsubscribe<string>(GameEvent.DialogChoiceSelected, OnChoice);
    }

    public void PlayDialog(string npcId, object _, Entity player)
    {
        _player = player;

        _graph = DialogueRepository.GetGraph(npcId);            // ← your static helper
        if (_graph == null) { GD.PushError($"No dialog: {npcId}"); return; }

        var start = string.IsNullOrEmpty(_graph.StartNodeId) ? FallbackStart
                                                             : _graph.StartNodeId;
        _currentNode = _graph.Nodes.First(n => n.NodeId == start);

        if (_panel == null) { _panel = DialoguePanelScene.Instantiate<DialogPanel>(); AddChild(_panel); }
        else _panel.Show();

        ShowNode();
    }

    private void ShowNode()
    {
        var text = _currentNode.Text.Replace("{playerName}", _player.Data.EntityName);
        _panel.SetSpeaker(_currentNode.Speaker, _currentNode.Portrait);
        _panel.SetText(text);

        foreach (var act in _currentNode.Actions)
            ActionExecutor.Run(act, _graph.Id);   // uses your existing class

        _panel.BuildChoices(_currentNode.Choices);
    }

    private void OnChoice(string choiceId)
    {
        var choice = _currentNode.Choices.FirstOrDefault(c => c.ChoiceId == choiceId);
        if (choice == null) { Finish(); return; }

        foreach (var act in choice.Actions)
            ActionExecutor.Run(act, _graph.Id);

        if (string.IsNullOrEmpty(choice.NextNodeId)) { Finish(); return; }

        _currentNode = _graph.Nodes.First(n => n.NodeId == choice.NextNodeId);
        ShowNode();
    }

    private void Finish()
    {
        _panel.Hide();
        EventManager.I.Publish(GameEvent.DialogEnded);
    }
}
