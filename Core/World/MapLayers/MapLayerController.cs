using Godot;
using System.Collections.Generic;

namespace ethra.V1;

public partial class MapLayerController : Node
{
    [Export] public NodePath LayerRootPath { get; set; } = "Map";
    [Export] public bool AutoFindLayersById { get; set; } = true;
    [Export] public bool ApplyDefaultsOnReady { get; set; } = true;
    [Export] public Godot.Collections.Array<MapLayerEntry> Layers { get; set; } = [];
    [Export] public Godot.Collections.Array<MapLayerState> States { get; set; } = [];

    private readonly Dictionary<string, LayerRuntimeEntry> _layersById = new();
    private readonly Dictionary<string, MapLayerState> _statesById = new();
    private readonly Dictionary<ulong, CollisionObjectDefaults> _collisionDefaultsByInstanceId = new();

    public override void _Ready()
    {
        RebuildCache();

        if (ApplyDefaultsOnReady)
        {
            RestoreDefaultMapLayerState();
        }
    }

    public void RebuildCache()
    {
        _layersById.Clear();
        _statesById.Clear();
        DebugLog.Info("MapLayer", $"Rebuilding cache. layers={Layers.Count} states={States.Count}", this);

        foreach (MapLayerEntry entry in Layers)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.LayerId))
            {
                GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: skipped layer entry with no LayerId.");
                continue;
            }

            Node node = ResolveLayerNode(entry);
            if (node == null)
            {
                GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: layer '{entry.LayerId}' could not resolve path '{entry.LayerPath}'.");
                continue;
            }

            _layersById[entry.LayerId] = new LayerRuntimeEntry(entry, node);
            CacheCollisionDefaults(node);
            DebugLog.Info("MapLayer", $"Layer cached id='{entry.LayerId}' path='{node.GetPath()}' visual={entry.IsVisualLayer} collision={entry.IsCollisionLayer}", this);
        }

        foreach (MapLayerState state in States)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.StateId))
            {
                continue;
            }

            _statesById[state.StateId] = state;
            DebugLog.Info("MapLayer", $"State cached id='{state.StateId}' visible=[{JoinIds(state.VisibleLayerIds)}] hidden=[{JoinIds(state.HiddenLayerIds)}] collisionOn=[{JoinIds(state.CollisionEnabledLayerIds)}] collisionOff=[{JoinIds(state.CollisionDisabledLayerIds)}]", this);
        }
    }

    public void SetLayerVisible(string layerId, bool visible)
    {
        if (!TryGetLayer(layerId, out LayerRuntimeEntry layer))
        {
            return;
        }

        if (layer.Node is CanvasItem canvasItem)
        {
            canvasItem.Visible = visible;
            return;
        }

        if (layer.Node is Node2D node2D)
        {
            node2D.Visible = visible;
            return;
        }

        GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: layer '{layerId}' is not a visual CanvasItem.");
    }

    public void SetLayerCollisionEnabled(string layerId, bool enabled)
    {
        if (!TryGetLayer(layerId, out LayerRuntimeEntry layer))
        {
            return;
        }

        SetCollisionEnabledRecursive(layer.Node, enabled);
    }

    public void ApplyMapLayerState(string stateId)
    {
        if (!_statesById.TryGetValue(stateId, out MapLayerState state))
        {
            DebugLog.Warning("MapLayer", $"State '{stateId}' was not found.", this);
            GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: state '{stateId}' was not found.");
            return;
        }

        DebugLog.Info("MapLayer", $"Applying state '{stateId}'.", this);

        foreach (string layerId in state.VisibleLayerIds)
        {
            SetLayerVisible(layerId, true);
        }

        foreach (string layerId in state.HiddenLayerIds)
        {
            SetLayerVisible(layerId, false);
        }

        foreach (string layerId in state.CollisionEnabledLayerIds)
        {
            SetLayerCollisionEnabled(layerId, true);
        }

        foreach (string layerId in state.CollisionDisabledLayerIds)
        {
            SetLayerCollisionEnabled(layerId, false);
        }

        foreach (MapLayerFadeOperation fade in state.FadeOperations)
        {
            if (fade == null)
            {
                continue;
            }

            FadeLayer(fade.LayerId, fade.TargetAlpha, fade.DurationSeconds, fade.HideWhenTransparent);
        }
    }

    public void RestoreDefaultMapLayerState()
    {
        DebugLog.Info("MapLayer", "Restoring default map layer state.", this);
        foreach (LayerRuntimeEntry layer in _layersById.Values)
        {
            if (layer.Config.IsVisualLayer)
            {
                SetLayerAlpha(layer.Node, 1f);
                SetLayerVisible(layer.Config.LayerId, layer.Config.DefaultVisible);
            }

            if (layer.Config.IsCollisionLayer)
            {
                SetLayerCollisionEnabled(layer.Config.LayerId, layer.Config.DefaultCollisionEnabled);
            }
        }
    }

    public void FadeLayer(string layerId, float targetAlpha, float durationSeconds)
    {
        FadeLayer(layerId, targetAlpha, durationSeconds, true);
    }

    public void FadeLayer(string layerId, float targetAlpha, float durationSeconds, bool hideWhenTransparent)
    {
        if (!TryGetLayer(layerId, out LayerRuntimeEntry layer))
        {
            return;
        }

        if (layer.Node is not CanvasItem canvasItem)
        {
            GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: layer '{layerId}' cannot fade because it is not a CanvasItem.");
            return;
        }

        targetAlpha = Mathf.Clamp(targetAlpha, 0f, 1f);
        durationSeconds = Mathf.Max(0f, durationSeconds);

        canvasItem.Visible = true;

        if (durationSeconds <= 0f)
        {
            Color modulate = canvasItem.Modulate;
            modulate.A = targetAlpha;
            canvasItem.Modulate = modulate;
            if (hideWhenTransparent && targetAlpha <= 0f)
            {
                canvasItem.Visible = false;
            }
            return;
        }

        Tween tween = CreateTween();
        tween.TweenProperty(canvasItem, "modulate:a", targetAlpha, durationSeconds);
        if (hideWhenTransparent && targetAlpha <= 0f)
        {
            tween.TweenCallback(Callable.From(() => canvasItem.Visible = false));
        }
    }

    private bool TryGetLayer(string layerId, out LayerRuntimeEntry layer)
    {
        if (_layersById.TryGetValue(layerId, out layer))
        {
            return true;
        }

        GD.PushWarning($"{nameof(MapLayerController)} on {GetPath()}: layer '{layerId}' was not found.");
        DebugLog.Warning("MapLayer", $"Layer '{layerId}' was not found.", this);
        return false;
    }

    private Node ResolveLayerNode(MapLayerEntry entry)
    {
        Node root = ResolveLayerRoot();

        if (!IsEmptyNodePath(entry.LayerPath))
        {
            Node node = root?.GetNodeOrNull<Node>(entry.LayerPath) ?? GetNodeOrNull<Node>(entry.LayerPath);
            if (node != null)
            {
                return node;
            }
        }

        if (!AutoFindLayersById || root == null)
        {
            return null;
        }

        return FindDescendantByName(root, entry.LayerId);
    }

    private Node ResolveLayerRoot()
    {
        if (IsEmptyNodePath(LayerRootPath))
        {
            return this;
        }

        Node root = GetNodeOrNull<Node>(LayerRootPath);
        if (root != null)
        {
            return root;
        }

        Node parent = GetParent();
        return parent?.GetNodeOrNull<Node>(LayerRootPath);
    }

    private static Node FindDescendantByName(Node root, string name)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child.Name == name)
            {
                return child;
            }

            Node match = FindDescendantByName(child, name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private void SetLayerAlpha(Node node, float alpha)
    {
        if (node is not CanvasItem canvasItem)
        {
            return;
        }

        Color modulate = canvasItem.Modulate;
        modulate.A = Mathf.Clamp(alpha, 0f, 1f);
        canvasItem.Modulate = modulate;
    }

    private void SetCollisionEnabledRecursive(Node node, bool enabled)
    {
        if (node is CollisionShape2D shape)
        {
            shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !enabled);
        }
        else if (node is CollisionPolygon2D polygon)
        {
            polygon.SetDeferred(CollisionPolygon2D.PropertyName.Disabled, !enabled);
        }
        else if (node is CollisionObject2D collisionObject)
        {
            ApplyCollisionObjectEnabled(collisionObject, enabled);
        }

        foreach (Node child in node.GetChildren())
        {
            SetCollisionEnabledRecursive(child, enabled);
        }
    }

    private void CacheCollisionDefaults(Node node)
    {
        if (node is CollisionObject2D collisionObject)
        {
            ulong id = collisionObject.GetInstanceId();
            _collisionDefaultsByInstanceId.TryAdd(
                id,
                new CollisionObjectDefaults(collisionObject.CollisionLayer, collisionObject.CollisionMask));
        }

        foreach (Node child in node.GetChildren())
        {
            CacheCollisionDefaults(child);
        }
    }

    private void ApplyCollisionObjectEnabled(CollisionObject2D collisionObject, bool enabled)
    {
        ulong id = collisionObject.GetInstanceId();
        if (!_collisionDefaultsByInstanceId.TryGetValue(id, out CollisionObjectDefaults defaults))
        {
            defaults = new CollisionObjectDefaults(collisionObject.CollisionLayer, collisionObject.CollisionMask);
            _collisionDefaultsByInstanceId[id] = defaults;
        }

        if (enabled)
        {
            collisionObject.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, defaults.CollisionLayer);
            collisionObject.SetDeferred(CollisionObject2D.PropertyName.CollisionMask, defaults.CollisionMask);
        }
        else
        {
            collisionObject.SetDeferred(CollisionObject2D.PropertyName.CollisionLayer, 0);
            collisionObject.SetDeferred(CollisionObject2D.PropertyName.CollisionMask, 0);
        }
    }

    private static bool IsEmptyNodePath(NodePath path)
    {
        return path == null || string.IsNullOrWhiteSpace(path.ToString());
    }

    private static string JoinIds(string[] ids)
    {
        return ids == null ? string.Empty : string.Join(",", ids);
    }

    private sealed record LayerRuntimeEntry(MapLayerEntry Config, Node Node);
    private readonly record struct CollisionObjectDefaults(uint CollisionLayer, uint CollisionMask);
}
