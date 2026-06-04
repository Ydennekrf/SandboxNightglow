using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ethra.V1
{
    [Tool]
    public partial class DialogGraphEditorTool : Control
    {
        private const string DefaultDataFolder = "res://Core/Dialog/Data";
        private const string DefaultFileName = "interaction_debug_merchant.json";
        private const string AnchorNameLeft = "left";
        private const string AnchorNameRight = "right";
        private const string AnchorNameTop = "top";
        private const string AnchorNameBottom = "bottom";

        private DialogTree _tree;
        private DialogNode _selectedNode;

        private LineEdit _folderField;
        private OptionButton _fileSelector;
        private Label _statusLabel;
        private Label _zoomLabel;
        private ScrollContainer _graphScroll;
        private DialogGraphCanvas _canvas;
        private VBoxContainer _inspector;
        private float _zoom = 1f;
        private bool _isNewUnsavedTree;

        public override void _Ready()
        {
            BuildUi();
            LoadTree();
        }

        private void BuildUi()
        {
            AnchorRight = 1f;
            AnchorBottom = 1f;
            CustomMinimumSize = new Vector2(560f, 520f);

            VBoxContainer root = new()
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                CustomMinimumSize = new Vector2(560f, 520f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            AddChild(root);

            HFlowContainer toolbar = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            root.AddChild(toolbar);

            _folderField = new LineEdit
            {
                Text = DefaultDataFolder,
                CustomMinimumSize = new Vector2(220f, 0f)
            };
            toolbar.AddChild(_folderField);

            _fileSelector = new OptionButton
            {
                CustomMinimumSize = new Vector2(220f, 0f)
            };
            toolbar.AddChild(_fileSelector);

            toolbar.AddChild(MakeButton("Refresh", RefreshFileList));
            toolbar.AddChild(MakeButton("Load", LoadTree));
            toolbar.AddChild(MakeButton("Save", SaveTree));
            toolbar.AddChild(MakeButton("New", NewTree));
            toolbar.AddChild(MakeButton("Add Node", AddNode));
            toolbar.AddChild(MakeButton("Auto Layout", AutoLayoutTree));
            toolbar.AddChild(MakeButton("Validate", ValidateCurrentTree));
            toolbar.AddChild(MakeButton("-", () => SetZoom(_zoom * 0.9f)));
            _zoomLabel = new Label
            {
                Text = "100%",
                CustomMinimumSize = new Vector2(48f, 0f),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            toolbar.AddChild(_zoomLabel);
            toolbar.AddChild(MakeButton("+", () => SetZoom(_zoom * 1.1f)));
            toolbar.AddChild(MakeButton("Reset", () => SetZoom(1f)));

            _statusLabel = new Label
            {
                Text = "Ready",
                CustomMinimumSize = new Vector2(220f, 0f),
                ClipText = true
            };
            toolbar.AddChild(_statusLabel);

            HSplitContainer split = new()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            root.AddChild(split);

            _graphScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(320f, 360f),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            split.AddChild(_graphScroll);

            _canvas = new DialogGraphCanvas
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _canvas.NodeSelected += SelectNode;
            _canvas.PanRequested += PanGraph;
            _canvas.ZoomRequested += factor => SetZoom(_zoom * factor);
            _graphScroll.AddChild(_canvas);

            ScrollContainer inspectorScroll = new()
            {
                CustomMinimumSize = new Vector2(300f, 0f),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            split.AddChild(inspectorScroll);

            _inspector = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(280f, 0f)
            };
            inspectorScroll.AddChild(_inspector);

            RefreshFileList();
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

            _isNewUnsavedTree = false;
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
            if (_isNewUnsavedTree && FileAccess.FileExists(path))
            {
                SetStatus($"New tree target already exists: {path}");
                return;
            }

            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                SetStatus($"Could not open for write: {path}");
                return;
            }

            file.StoreString(JsonSerializer.Serialize(_tree, JsonOptions()));
            _isNewUnsavedTree = false;
            string savedFileName = SelectedFileName();
            RefreshFileList(savedFileName);
            SetStatus($"Saved {path}");
        }

        private void NewTree()
        {
            string newFileName = MakeUniqueDialogFileName("new_dialog_tree");
            AddOrSelectFileName(newFileName);

            string id = System.IO.Path.GetFileNameWithoutExtension(newFileName);
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
                        GraphX = 0f,
                        GraphY = 0f,
                        Choices = new List<DialogChoice>()
                    }
                }
            };

            _isNewUnsavedTree = true;
            _selectedNode = _tree.Nodes[0];
            RefreshAll();
            SetStatus($"Created new unsaved tree: {newFileName}");
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
                GraphX = (_selectedNode?.GraphX ?? 0f) + 340f,
                GraphY = _selectedNode?.GraphY ?? NextFreeNodeY(),
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
            RenderInspector();
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
            treeIdField.TextChanged += OnTreeIdChanged;

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
            HBoxContainer positionFields = new();
            LineEdit graphXField = new() { Text = _selectedNode.GraphX.ToString("0.##"), CustomMinimumSize = new Vector2(90f, 0f) };
            LineEdit graphYField = new() { Text = _selectedNode.GraphY.ToString("0.##"), CustomMinimumSize = new Vector2(90f, 0f) };
            positionFields.AddChild(new Label { Text = "X" });
            positionFields.AddChild(graphXField);
            positionFields.AddChild(new Label { Text = "Y" });
            positionFields.AddChild(graphYField);
            _inspector.AddChild(new Label { Text = "Graph Position" });
            _inspector.AddChild(positionFields);

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
                if (float.TryParse(graphXField.Text, out float graphX))
                {
                    _selectedNode.GraphX = Mathf.Max(0f, graphX);
                }

                if (float.TryParse(graphYField.Text, out float graphY))
                {
                    _selectedNode.GraphY = Mathf.Max(0f, graphY);
                }

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
            AddActionItems(actionSelector);
            int actionIndex = FindItemIndex(actionSelector, choice.ActionId);
            actionSelector.Select(actionIndex);
            box.AddChild(new Label { Text = "Action" });
            box.AddChild(actionSelector);

            LineEdit payloadField = new() { Text = choice.ActionPayload ?? string.Empty };
            box.AddChild(new Label { Text = "Action Payload" });
            box.AddChild(payloadField);

            OptionButton conditionSelector = new();
            AddConditionItems(conditionSelector);
            int conditionIndex = FindItemIndex(conditionSelector, choice.ConditionId);
            conditionSelector.Select(conditionIndex);
            box.AddChild(new Label { Text = "Condition" });
            box.AddChild(conditionSelector);

            LineEdit conditionPayloadField = new() { Text = choice.ConditionPayload ?? string.Empty };
            box.AddChild(new Label { Text = "Condition Payload" });
            box.AddChild(conditionPayloadField);

            HBoxContainer anchorFields = new();
            OptionButton fromAnchorSelector = BuildAnchorSelector(choice.FromAnchor, AnchorNameRight);
            OptionButton toAnchorSelector = BuildAnchorSelector(choice.ToAnchor, AnchorNameLeft);
            anchorFields.AddChild(new Label { Text = "From" });
            anchorFields.AddChild(fromAnchorSelector);
            anchorFields.AddChild(new Label { Text = "To" });
            anchorFields.AddChild(toAnchorSelector);
            box.AddChild(new Label { Text = "Connection Anchors" });
            box.AddChild(anchorFields);

            HBoxContainer buttons = new();
            buttons.AddChild(MakeButton("Apply Choice", () =>
            {
                choice.ChoiceText = textField.Text;
                choice.NextNodeId = targetSelector.Selected <= 0 ? string.Empty : targetSelector.GetItemText(targetSelector.Selected);
                choice.EndsDialog = endsDialog.ButtonPressed;
                choice.ActionId = actionSelector.Selected <= 0 ? string.Empty : actionSelector.GetItemText(actionSelector.Selected);
                choice.ActionPayload = payloadField.Text;
                choice.ConditionId = conditionSelector.Selected <= 0 ? string.Empty : conditionSelector.GetItemText(conditionSelector.Selected);
                choice.ConditionPayload = conditionPayloadField.Text;
                choice.FromAnchor = fromAnchorSelector.GetItemText(fromAnchorSelector.Selected);
                choice.ToAnchor = toAnchorSelector.GetItemText(toAnchorSelector.Selected);
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

        private void OnTreeIdChanged(string value)
        {
            if (_tree == null)
            {
                return;
            }

            _tree.TreeId = value;

            if (!_isNewUnsavedTree)
            {
                return;
            }

            string safeBaseName = SanitizeFileBaseName(value);
            if (string.IsNullOrWhiteSpace(safeBaseName))
            {
                return;
            }

            ReplaceSelectedFileName(MakeUniqueDialogFileName(safeBaseName, ignoreSelectedFile: true));
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

                    if (!DialogConditionRunner.IsKnownCondition(choice.ConditionId))
                    {
                        issues.Add($"Choice '{choice.ChoiceText}' uses unknown condition '{choice.ConditionId}'.");
                    }

                    if (!DialogActionRunner.IsKnownAction(choice.ActionId))
                    {
                        issues.Add($"Choice '{choice.ChoiceText}' uses unknown action '{choice.ActionId}'.");
                    }
                }
            }

            return issues;
        }

        private string CurrentFilePath()
        {
            string folder = _folderField.Text.Trim();
            string fileName = SelectedFileName();
            return BuildFilePath(folder, fileName);
        }

        private static string BuildFilePath(string folder, string fileName)
        {
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

        private void AutoLayoutTree()
        {
            if (_tree == null)
            {
                SetStatus("No tree loaded.");
                return;
            }

            _canvas.AutoLayout();
            RefreshAll();
            SetStatus("Auto layout applied.");
        }

        private void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, 0.35f, 2.25f);
            _canvas?.SetZoom(_zoom);
            if (_zoomLabel != null)
            {
                _zoomLabel.Text = $"{Mathf.RoundToInt(_zoom * 100f)}%";
            }
        }

        private void PanGraph(Vector2 delta)
        {
            if (_graphScroll == null)
            {
                return;
            }

            _graphScroll.ScrollHorizontal = Mathf.Max(0, _graphScroll.ScrollHorizontal + Mathf.RoundToInt(delta.X));
            _graphScroll.ScrollVertical = Mathf.Max(0, _graphScroll.ScrollVertical + Mathf.RoundToInt(delta.Y));
        }

        private float NextFreeNodeY()
        {
            if (_tree?.Nodes == null || _tree.Nodes.Count == 0)
            {
                return 0f;
            }

            return _tree.Nodes.Max(node => node.GraphY) + 220f;
        }

        private void RefreshFileList()
        {
            RefreshFileList(SelectedFileName());
        }

        private void RefreshFileList(string preferredFileName)
        {
            if (_fileSelector == null)
            {
                return;
            }

            string previouslySelected = string.IsNullOrWhiteSpace(preferredFileName) ? SelectedFileName() : preferredFileName;
            _fileSelector.Clear();

            string folder = _folderField?.Text?.Trim() ?? DefaultDataFolder;
            DirAccess dir = DirAccess.Open(folder);
            if (dir == null)
            {
                _fileSelector.AddItem(DefaultFileName);
                SetStatus($"Folder not found: {folder}");
                return;
            }

            string[] files = dir.GetFiles();
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    _fileSelector.AddItem(file);
                }
            }

            if (_fileSelector.ItemCount == 0)
            {
                _fileSelector.AddItem(DefaultFileName);
            }

            int selectedIndex = FindItemIndex(_fileSelector, string.IsNullOrWhiteSpace(previouslySelected) ? DefaultFileName : previouslySelected);
            _fileSelector.Select(selectedIndex);
        }

        private string SelectedFileName()
        {
            if (_fileSelector == null || _fileSelector.ItemCount == 0)
            {
                return DefaultFileName;
            }

            int selected = Mathf.Clamp(_fileSelector.Selected, 0, _fileSelector.ItemCount - 1);
            return _fileSelector.GetItemText(selected);
        }

        private string MakeUniqueDialogFileName(string baseName, bool ignoreSelectedFile = false)
        {
            string safeBaseName = SanitizeFileBaseName(baseName);
            if (string.IsNullOrWhiteSpace(safeBaseName))
            {
                safeBaseName = "new_dialog_tree";
            }

            string folder = _folderField?.Text?.Trim() ?? DefaultDataFolder;
            int index = 0;
            while (true)
            {
                string candidate = index == 0 ? $"{safeBaseName}.json" : $"{safeBaseName}_{index}.json";
                string path = BuildFilePath(folder, candidate);
                if (!FileAccess.FileExists(path) && !FileSelectorContains(candidate, ignoreSelectedFile))
                {
                    return candidate;
                }

                index++;
            }
        }

        private void AddOrSelectFileName(string fileName)
        {
            if (_fileSelector == null)
            {
                return;
            }

            int existingIndex = FindItemIndex(_fileSelector, fileName);
            if (existingIndex == 0 && (_fileSelector.ItemCount == 0 || _fileSelector.GetItemText(0) != fileName))
            {
                _fileSelector.AddItem(fileName);
                existingIndex = _fileSelector.ItemCount - 1;
            }

            _fileSelector.Select(existingIndex);
        }

        private void ReplaceSelectedFileName(string fileName)
        {
            if (_fileSelector == null)
            {
                return;
            }

            int selected = Mathf.Clamp(_fileSelector.Selected, 0, Math.Max(0, _fileSelector.ItemCount - 1));
            if (_fileSelector.ItemCount == 0)
            {
                _fileSelector.AddItem(fileName);
                _fileSelector.Select(0);
                return;
            }

            _fileSelector.SetItemText(selected, fileName);
            _fileSelector.Select(selected);
        }

        private bool FileSelectorContains(string fileName, bool ignoreSelectedFile = false)
        {
            if (_fileSelector == null)
            {
                return false;
            }

            for (int i = 0; i < _fileSelector.ItemCount; i++)
            {
                if (ignoreSelectedFile && i == _fileSelector.Selected)
                {
                    continue;
                }

                if (_fileSelector.GetItemText(i) == fileName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string SanitizeFileBaseName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            char[] chars = new char[trimmed.Length];
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                chars[i] = char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_';
            }

            string collapsed = new string(chars);
            while (collapsed.Contains("__", StringComparison.Ordinal))
            {
                collapsed = collapsed.Replace("__", "_");
            }

            return collapsed.Trim('_');
        }

        private static void AddActionItems(OptionButton selector)
        {
            selector.AddItem("");
            selector.AddItem(DialogActionRunner.StoreStubActionId);
            selector.AddItem(DialogActionRunner.StartQuestActionId);
            selector.AddItem(DialogActionRunner.CompleteQuestStubActionId);
            selector.AddItem(DialogActionRunner.UpdateFriendshipStubActionId);
        }

        private static void AddConditionItems(OptionButton selector)
        {
            selector.AddItem("");
            selector.AddItem(DialogConditionRunner.DebugTrueConditionId);
            selector.AddItem(DialogConditionRunner.DebugFalseConditionId);
            selector.AddItem(DialogConditionRunner.NpcFriendshipGreaterThanStubConditionId);
            selector.AddItem(DialogConditionRunner.QuestCompleteStubConditionId);
            selector.AddItem(DialogConditionRunner.ItemInInventoryStubConditionId);
        }

        private static OptionButton BuildAnchorSelector(string selectedAnchor, string defaultAnchor)
        {
            OptionButton selector = new();
            selector.AddItem(AnchorNameLeft);
            selector.AddItem(AnchorNameRight);
            selector.AddItem(AnchorNameTop);
            selector.AddItem(AnchorNameBottom);
            selector.Select(FindItemIndex(selector, string.IsNullOrWhiteSpace(selectedAnchor) ? defaultAnchor : selectedAnchor));
            return selector;
        }

        private static int FindItemIndex(OptionButton selector, string value)
        {
            if (selector == null || selector.ItemCount == 0)
            {
                return 0;
            }

            for (int i = 0; i < selector.ItemCount; i++)
            {
                if (selector.GetItemText(i) == value)
                {
                    return i;
                }
            }

            return 0;
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
