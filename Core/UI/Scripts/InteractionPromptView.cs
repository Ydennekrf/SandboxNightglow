using Godot;
using System.Collections.Generic;
using Game.Interact;

namespace ethra.V1
{
	public partial class InteractionPromptView : Control
	{
		[Export] public NodePath PromptPanelPath { get; set; } = "PromptPanel";
		[Export] public NodePath PromptLabelPath { get; set; } = "PromptPanel/PromptLabel";

		private readonly Dictionary<Node, string> _activePrompts = new();
		private PanelContainer _panel;
		private Label _label;

		public override void _Ready()
		{
			MouseFilter = MouseFilterEnum.Ignore;
			SetAnchorsPreset(LayoutPreset.FullRect);
			ResolveNodes();
			HidePrompt();

			GameManager.Instance?.Subscribe<InteractionPromptChanged>(GameEvent.InteractionPromptChanged, OnPromptChanged);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Subscribe<InteractionPromptChanged>(GameEvent.InteractionPromptChanged, OnPromptChanged);
			}
		}

		public override void _ExitTree()
		{
			GameManager.Instance?.Unsubscribe<InteractionPromptChanged>(GameEvent.InteractionPromptChanged, OnPromptChanged);
			if (global::EventManager.I != null)
			{
				global::EventManager.I.Unsubscribe<InteractionPromptChanged>(GameEvent.InteractionPromptChanged, OnPromptChanged);
			}
		}

		private void BuildView()
		{
			if (_panel != null && _label != null)
			{
				return;
			}

			_panel = new PanelContainer
			{
				CustomMinimumSize = new Vector2(180f, 34f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			_panel.SetAnchorsPreset(LayoutPreset.CenterBottom);
			_panel.OffsetLeft = -90f;
			_panel.OffsetTop = -78f;
			_panel.OffsetRight = 90f;
			_panel.OffsetBottom = -44f;

			_label = new Label
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = MouseFilterEnum.Ignore
			};

			_panel.AddChild(_label);
			AddChild(_panel);
		}

		private void ResolveNodes()
		{
			_panel = GetNodeOrNull<PanelContainer>(PromptPanelPath);
			_label = GetNodeOrNull<Label>(PromptLabelPath);

			if (_panel == null || _label == null)
			{
				GD.PushWarning("InteractionPromptView: packed scene node paths are incomplete; building fallback prompt view.");
				BuildView();
			}
		}

		private void OnPromptChanged(InteractionPromptChanged prompt)
		{
			if (prompt.Source != null)
			{
				if (string.IsNullOrWhiteSpace(prompt.PromptText))
				{
					_activePrompts.Remove(prompt.Source);
				}
				else
				{
					_activePrompts[prompt.Source] = prompt.PromptText;
				}

				UpdateSelectedPrompt();
				return;
			}

			if (string.IsNullOrWhiteSpace(prompt.PromptText))
			{
				HidePrompt();
				return;
			}

			_activePrompts.Clear();
			_label.Text = prompt.PromptText;
			_panel.Visible = true;
		}

		private void UpdateSelectedPrompt()
		{
			Node best = null;
			string bestText = string.Empty;
			int bestPriority = int.MinValue;
			float bestDistanceSquared = float.PositiveInfinity;
			Node2D player = GetTree()?.GetFirstNodeInGroup("Player") as Node2D;

			foreach ((Node source, string text) in _activePrompts)
			{
				if (source == null || !IsInstanceValid(source) || string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				int priority = source is IInteractionPromptSource promptSource
					? promptSource.InteractionPriority
					: 0;
				float distanceSquared = ResolveDistanceSquared(source, player);

				if (priority > bestPriority || (priority == bestPriority && distanceSquared < bestDistanceSquared))
				{
					best = source;
					bestText = text;
					bestPriority = priority;
					bestDistanceSquared = distanceSquared;
				}
			}

			if (best == null)
			{
				HidePrompt();
				return;
			}

			_label.Text = bestText;
			_panel.Visible = true;
		}

		private static float ResolveDistanceSquared(Node source, Node2D player)
		{
			if (player == null || source is not Node2D node)
			{
				return float.PositiveInfinity;
			}

			return player.GlobalPosition.DistanceSquaredTo(node.GlobalPosition);
		}

		private void HidePrompt()
		{
			if (_panel != null)
			{
				_panel.Visible = false;
			}
		}
	}
}
