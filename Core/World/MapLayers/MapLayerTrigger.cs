using Godot;

namespace ethra.V1;

public partial class MapLayerTrigger : Area2D
{
    [Export] public NodePath ControllerPath { get; set; }
    [Export] public string EnterStateId { get; set; } = string.Empty;
    [Export] public string ExitStateId { get; set; } = string.Empty;
    [Export] public bool RestoreDefaultOnExit { get; set; } = true;
    [Export] public bool AcceptPlayerGroupFallback { get; set; } = true;

    private MapLayerController _controller;
    private int _playerOverlapCount;

    public override void _Ready()
    {
        _controller = ResolveController();
        if (_controller == null)
        {
            GD.PushWarning($"{nameof(MapLayerTrigger)} on {GetPath()}: MapLayerController was not found.");
        }

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        DebugLog.Info("MapLayer", $"Trigger ready enter='{EnterStateId}' exit='{ExitStateId}' restoreDefault={RestoreDefaultOnExit} controller='{_controller?.GetPath().ToString() ?? "null"}'", this);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!IsPlayer(body))
        {
            return;
        }

        _playerOverlapCount++;
        DebugLog.Info("MapLayer", $"Player entered trigger body='{body.Name}' overlap={_playerOverlapCount} enter='{EnterStateId}'", this);
        if (_playerOverlapCount > 1)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(EnterStateId))
        {
            _controller?.CallDeferred(nameof(MapLayerController.ApplyMapLayerState), EnterStateId);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (!IsPlayer(body))
        {
            return;
        }

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        DebugLog.Info("MapLayer", $"Player exited trigger body='{body.Name}' overlap={_playerOverlapCount} exit='{ExitStateId}' restoreDefault={RestoreDefaultOnExit}", this);
        if (_playerOverlapCount > 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(ExitStateId))
        {
            _controller?.CallDeferred(nameof(MapLayerController.ApplyMapLayerState), ExitStateId);
        }
        else if (RestoreDefaultOnExit)
        {
            _controller?.CallDeferred(nameof(MapLayerController.RestoreDefaultMapLayerState));
        }
    }

    private bool IsPlayer(Node2D body)
    {
        return body is PlayerNode || (AcceptPlayerGroupFallback && body.IsInGroup("Player"));
    }

    private MapLayerController ResolveController()
    {
        if (!IsEmptyNodePath(ControllerPath))
        {
            MapLayerController configured = GetNodeOrNull<MapLayerController>(ControllerPath);
            if (configured != null)
            {
                return configured;
            }
        }

        Node current = this;
        while (current != null)
        {
            if (current is MapLayerController controller)
            {
                return controller;
            }

            MapLayerController childController = current.GetNodeOrNull<MapLayerController>("MapLayerController");
            if (childController != null)
            {
                return childController;
            }

            current = current.GetParent();
        }

        return null;
    }

    private static bool IsEmptyNodePath(NodePath path)
    {
        return path == null || string.IsNullOrWhiteSpace(path.ToString());
    }
}
