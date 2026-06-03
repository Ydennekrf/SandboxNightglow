using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ethra.V1
{
    public partial class DialogGraphEditorTool : Control
    {
        private const string DefaultDataFolder = "res://Core/Dialog/Data";
        private const string DefaultFileName = "interaction_debug_merchant.json";

        private DialogTree _tree;
        private DialogNode _selectedNode;

        private LineEdit _folderField;
        private LineEdit _fileField;
        private Label _statusLabel;
        private DialogGraphCanvas _canvas;
        private VBoxContainer _inspector;

        public override void _Ready()
        {
            BuildUi();
            LoadTree();
        }

        private void BuildUi()
        {
            AnchorRight = 1f;
            AnchorBottom = 1f;

            VBoxContainer root = new()
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            AddChild(root);

            HBoxContainer toolbar = new();
            root.AddChild(toolbar);

            _folderField = new LineEdit
            {
                Text = DefaultDataFolder,
                CustomMinimumSize = new Vector2(260f, 0f)
            };
            toolbar.AddChild(_folderField);

            _fileField = new LineEdit
            {
                Text = DefaultFileName,
                CustomMinimumSize = new Vector2(240f, 0f)
            };
            toolbar.AddChild(_fileField);

            toolbar.AddChild(MakeButton("Load", LoadTree));
            toolbar.AddChild(MakeButton("Save", SaveTree));
            toolbar.AddChild(MakeButton("New", NewTree));
            toolbar.AddChild(MakeButton("Add Node", AddNode));
            toolbar.AddChild(MakeButton("Validate", ValidateCurrentTree));

            _statusLabel = new Label
            {
                Text = "Ready",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            toolbar.AddChild(_statusLabel);

            HSplitContainer split = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            root.AddChild(split);

            ScrollContainer scroll = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            split.AddChild(scroll);

            _canvas = new DialogGraphCanvas
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _canvas.NodeSelected += SelectNode;
            scroll.AddChild(_canvas);

            ScrollContainer inspectorScroll = new()
            {
                CustomMinimumSize = new Vector2(360f, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            split.AddChild(inspectorScroll);

            _inspector = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(340f, 0f)
            };
            inspectorScroll.AddChild(_inspector);
        }

        private Button MakeButton(string text, Action pressed)
        {
            Button button = new() { Text = text };
            button.Pressed += pressed;
            return button;
        }

        private void LoadTree()
        {
            string path = CurrentFilePath();
            if (!FileAccess.FileExists(path))
            {
                SetStatus($"File not found: {path}");
                return;
            }

            string json = FileAccess.GetFileAsString(path);
            try
            {
                _tree = JsonSerializer.Deserialize<DialogTree>(json, JsonOptions());
            }
            catch (JsonException ex)
            {
                SetStatus($"JSON parse failed: {ex.Message}");
                return;
            }

            if (_tree == null)
            {
                SetStatus("Loaded tree was null.");
                return;
            }

            _selectedNode = _tree.Nodes.FirstOrDefault(node => node.NodeId == _tree.StartingNodeId)
                ?? _tree.Nodes.FirstOrDefault();
            RefreshAll();
            SetStatus($"Loaded {path}");
        }

        private void SaveTree()
        {
            if (_tree == null)
            {
                SetStatus("No tree loaded.");
                return;
            }

            List<string> issues = ValidateTree(_tree);
            if (issues.Count > 0)
            {
                SetStatus($"Validation failed: {issues[0]}");
                return;
            }

            string path = CurrentFilePath();
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                SetStatus($"Could not open for write: {path}");
                return;
            }

            file.StoreString(JsonSerializer.Serialize(_tree, JsonOptions()));
            SetStatus($"Saved {path}");
        }

        private void NewTree()
        {
            string id = System.IO.Path.GetFileNameWithoutExtension(_fileField.Text);
            if (string.IsNullOrWhiteSpace(id))
            {
                id = "new_dialog_tree";
            }

            _tree = new DialogTree
            {
                TreeId = id,
                StartingNodeId = "start",
                Nodes = new List<DialogNode>
                {
                    new()
                    {
                        NodeId = "start",
                        SpeakerName = "NPC",
                        Text = "New dialog starts here.",
                        Choices = new List<DialogChoice>()
                    }
                }
            };

            _selectedNode = _tree.Nodes[0];
            RefreshAll();
            SetStatus("Created new tree.");
        }

        private void AddNode()
        {
            if (_tree == null)
            {
                NewTree();
                return;
            }

            string nodeId = MakeUniqueNodeId("node");
            DialogNode node = new()
            {
                NodeId = nodeId,
                SpeakerName = "NPC",
                Text = "New dialog node.",
                Choices = new List<DialogChoice>()
            };

            _tree.Nodes.Add(node);
            _selectedNode = node;
            RefreshAll();
            SetStatus($"Added node {nodeId}.");
        }

        private void SelectNode(string nodeId)
        {
            _selectedNode = _tree?.Nodes.FirstOrDefault(node => node.NodeId == nodeId);
            RefreshAll();
        }

        private void RenderInspector()
        {
            foreach (Node child in _inspector.GetChildren())
            {
                _inspector.RemoveChild(child);
                child.QueueFree();
            }

            if (_tree == null)
            {
                _inspector.AddChild(new Label { Text = "No dialog tree loaded." });
                return;
            }

            _inspector.AddChild(new Label { Text = "Tree" });
            LineEdit treeIdField = AddLineEdit("Tree ID", _tree.TreeId);
            treeIdField.TextChanged += value => _tree.TreeId = value;

            OptionButton startSelector = AddNodeSelector("Starting Node", _tree.StartingNodeId);
            startSelector.ItemSelected += index =>
            {
                _tree.StartingNodeId = startSelector.GetItemText((int)index);
                RefreshAll();
            };

            _inspector.AddChild(new HSeparator());

            if (_selectedNode == null)
            {
                _inspector.AddChild(new Label { Text = "Select a node." });
                return;
            }

            _inspector.AddChild(new Label { Text = "Selected Node" });

            LineEdit nodeIdField = AddLineEdit("Node ID", _selectedNode.NodeId);
            LineEdit speakerField = AddLineEdit("Speaker", _selectedNode.SpeakerName);
            TextEdit textField = AddTextEdit("Text", _selectedNode.Text);

            Button applyNodeButton = MakeButton("Apply Node Fields", () =>
            {
                string previousId = _selectedNode.NodeId;
                string nextId = nodeIdField.Text.Trim();
                if (string.IsNullOrWhiteSpace(nextId))
                {
                    SetStatus("Node ID cannot be empty.");
                    return;
                }

                if (previousId != nextId && _tree.Nodes.Any(node => node != _selectedNode && node.NodeId == nextId))
                {
                    SetStatus($"Node ID already exists: {nextId}");
                    return;
                }

                RenameNodeReferences(previousId, nextId);
                _selectedNode.NodeId = nextId;
                _selectedNode.SpeakerName = speakerField.Text;
                _selectedNode.Text = textField.Text;
                RefreshAll();
                SetStatus($"Applied node {nextId}.");
            });
            _inspector.AddChild(applyNodeButton);

            HBoxContainer nodeButtons = new();
            nodeButtons.AddChild(MakeButton("Set As Start", () =>
            {
                _tree.StartingNodeId = _selectedNode.NodeId;
                RefreshAll();
            }));
            nodeButtons.AddChild(MakeButton("Delete Node", DeleteSelectedNode));
            _inspector.AddChild(nodeButtons);

            _inspector.AddChild(new HSeparator());
            _inspector.AddChild(new Label { Text = "Choices" });
            _inspector.AddChild(MakeButton("Add Choice", AddChoice));

            for (int i = 0; i < _selectedNode.Choices.Count; i++)
            {
                AddChoiceEditor(_selectedNode.Choices[i], i);
            }
        }

        private LineEdit AddLineEdit(string label, string value)
        {
            _inspector.AddChild(new Label { Text = label });
            LineEdit field = new() { Text = value ?? string.Empty };
            _inspector.AddChild(field);
            return field;
        }

        private TextEdit AddTextEdit(string label, string value)
        {
            _inspector.AddChild(new Label { Text = label });
            TextEdit field = new()
            {
                Text = value ?? string.Empty,
                CustomMinimumSize = new Vector2(0f, 90f),
                WrapMode = TextEdit.LineWrappingMode.Boundary
            };
            _inspector.AddChild(field);
            return field;
        }

        private OptionButton AddNodeSelector(string label, string selectedNodeId)
        {
            _inspector.AddChild(new Label { Text = label });
            OptionButton selector = BuildNodeSelector(selectedNodeId, includeNone: false);
            _inspector.AddChild(selector);
            return selector;
        }

        private void AddChoiceEditor(DialogChoice choice, int index)
        {
            PanelContainer panel = new();
            VBoxContainer box = new();
            panel.AddChild(box);
            _inspector.AddChild(panel);

            box.AddChild(new Label { Text = $"Choice {index + 1}" });

            LineEdit textField = new() { Text = choice.ChoiceText };
            box.AddChild(new Label { Text = "Choice Text" });
            box.AddChild(textField);

            OptionButton targetSelector = BuildNodeSelector(choice.NextNodeId, includeNone: true);
            box.AddChild(new Label { Text = "Next Node" });
            box.AddChild(targetSelector);

            CheckBox endsDialog = new()
            {
                Text = "Ends Dialog",
                ButtonPressed = choice.EndsDialog
            };
            box.AddChild(endsDialog);

            OptionButton actionSelector = new();
            actionSelector.AddItem("");
            actionSelector.AddItem(DialogActionRunner.StoreStubActionId);
            int actionIndex = choice.ActionId == DialogActionRunner.StoreStubActionId ? 1 : 0;
            actionSelector.Select(actionIndex);
            box.AddChild(new Label { Text = "Action" });
            box.AddChild(actionSelector);

            LineEdit payloadField = new() { Text = choice.ActionPayload ?? string.Empty };
            box.AddChild(new Label { Text = "Action Payload" });
            box.AddChild(payloadField);

            HBoxContainer buttons = new();
            buttons.AddChild(MakeButton("Apply Choice", () =>
            {
                choice.ChoiceText = textField.Text;
                choice.NextNodeId = targetSelector.Selected <= 0 ? string.Empty : targetSelector.GetItemText(targetSelector.Selected);
                choice.EndsDialog = endsDialog.ButtonPressed;
                choice.ActionId = actionSelector.Selected <= 0 ? string.Empty : actionSelector.GetItemText(actionSelector.Selected);
                choice.ActionPayload = payloadField.Text;
                RefreshAll();
                SetStatus($"Applied choice {index + 1}.");
            }));
            buttons.AddChild(MakeButton("Remove", () =>
            {
                _selectedNode.Choices.Remove(choice);
                RefreshAll();
            }));
            box.AddChild(buttons);
        }

        private OptionButton BuildNodeSelector(string selectedNodeId, bool includeNone)
        {
            OptionButton selector = new();
            if (includeNone)
            {
                selector.AddItem("");
            }

            int selectedIndex = includeNone ? 0 : -1;
            foreach (DialogNode node in _tree.Nodes)
            {
                selector.AddItem(node.NodeId);
                int index = selector.ItemCount - 1;
                if (node.NodeId == selectedNodeId)
                {
                    selectedIndex = index;
                }
            }

            if (selectedIndex >= 0)
            {
                selector.Select(selectedIndex);
            }

            return selector;
        }

        private void AddChoice()
        {
            if (_selectedNode == null)
            {
                return;
            }

            _selectedNode.Choices.Add(new DialogChoice
            {
                ChoiceText = "New choice",
                EndsDialog = true
            });
            RefreshAll();
        }

        private void DeleteSelectedNode()
        {
            if (_tree == null || _selectedNode == null)
            {
                return;
            }

            if (_tree.Nodes.Count <= 1)
            {
                SetStatus("Cannot delete the only node.");
                return;
            }

            string deletedId = _selectedNode.NodeId;
            _tree.Nodes.Remove(_selectedNode);
            foreach (DialogNode node in _tree.Nodes)
            {
                foreach (DialogChoice choice in node.Choices)
                {
                    if (choice.NextNodeId == deletedId)
                    {
                        choice.NextNodeId = string.Empty;
                        choice.EndsDialog = true;
                    }
                }
            }

            if (_tree.StartingNodeId == deletedId)
            {
                _tree.StartingNodeId = _tree.Nodes[0].NodeId;
            }

            _selectedNode = _tree.Nodes[0];
            RefreshAll();
            SetStatus($"Deleted node {deletedId}.");
        }

        private void RenameNodeReferences(string previousId, string nextId)
        {
            if (previousId == nextId)
            {
                return;
            }

            if (_tree.StartingNodeId == previousId)
            {
                _tree.StartingNodeId = nextId;
            }

            foreach (DialogNode node in _tree.Nodes)
            {
                foreach (DialogChoice choice in node.Choices)
                {
                    if (choice.NextNodeId == previousId)
                    {
                        choice.NextNodeId = nextId;
                    }
                }
            }
        }

        private string MakeUniqueNodeId(string prefix)
        {
            int index = 1;
            string candidate;
            do
            {
                candidate = $"{prefix}_{index}";
                index++;
            }
            while (_tree.Nodes.Any(node => node.NodeId == candidate));

            return candidate;
        }

        private void RefreshAll()
        {
            _canvas.SetTree(_tree, _selectedNode?.NodeId);
            RenderInspector();
        }

        private void ValidateCurrentTree()
        {
            if (_tree == null)
            {
                SetStatus("No tree loaded.");
                return;
            }

            List<string> issues = ValidateTree(_tree);
            SetStatus(issues.Count == 0 ? "Validation passed." : $"Validation failed: {issues[0]}");
        }

        private List<string> ValidateTree(DialogTree tree)
        {
            List<string> issues = new();

            if (string.IsNullOrWhiteSpace(tree.TreeId))
            {
                issues.Add("Tree ID is empty.");
            }

            if (string.IsNullOrWhiteSpace(tree.StartingNodeId))
            {
                issues.Add("Starting node ID is empty.");
            }

            HashSet<string> ids = new();
            foreach (DialogNode node in tree.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.NodeId))
                {
                    issues.Add("A node has an empty ID.");
                    continue;
                }

                if (!ids.Add(node.NodeId))
                {
                    issues.Add($"Duplicate node ID: {node.NodeId}");
                }
            }

            if (!ids.Contains(tree.StartingNodeId))
            {
                issues.Add($"Starting node does not exist: {tree.StartingNodeId}");
            }

            foreach (DialogNode node in tree.Nodes)
            {
                foreach (DialogChoice choice in node.Choices)
                {
                    if (!string.IsNullOrWhiteSpace(choice.NextNodeId) && !ids.Contains(choice.NextNodeId))
                    {
                        issues.Add($"Choice '{choice.ChoiceText}' points to missing node '{choice.NextNodeId}'.");
                    }
                }
            }

            return issues;
        }

        private string CurrentFilePath()
        {
            string folder = _folderField.Text.Trim();
            string fileName = _fileField.Text.Trim();
            if (!folder.EndsWith("/"))
            {
                folder += "/";
            }

            return folder + fileName;
        }

        private void SetStatus(string message)
        {
            _statusLabel.Text = message;
            GD.Print($"[DialogGraphEditor] {message}");
        }

        private static JsonSerializerOptions JsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }
    }
}
