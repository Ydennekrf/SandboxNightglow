using Godot;
using System.Collections.Generic;

namespace ethra.V1;

/// <summary>
/// Resolves terrain surface identifiers from configured TileMapLayer custom data.
/// </summary>
public partial class TileSurfaceResolver : Node
{
	private readonly List<TileMapLayer> _surfaceLayers = new();

	[Export] public Godot.Collections.Array<NodePath> SurfaceLayerPaths { get; set; } = new();
	[Export] public string CustomDataKey { get; set; } = "surface";
	[Export] public string DefaultSurfaceId { get; set; } = "default";

	public override void _Ready()
	{
		CacheSurfaceLayers();
	}

	/// <summary>
	/// Returns the first valid surface identifier at a world-space position.
	/// </summary>
	public string GetSurfaceAt(Vector2 globalPosition)
	{
		foreach (TileMapLayer layer in _surfaceLayers)
		{
			if (layer == null || !GodotObject.IsInstanceValid(layer))
			{
				continue;
			}

			Vector2 localPosition = layer.ToLocal(globalPosition);
			Vector2I cell = layer.LocalToMap(localPosition);
			TileData tileData = layer.GetCellTileData(cell);
			if (tileData == null)
			{
				continue;
			}

			Variant value = tileData.GetCustomData(CustomDataKey);
			string surface = value.VariantType == Variant.Type.Nil ? string.Empty : value.ToString();
			if (!string.IsNullOrWhiteSpace(surface))
			{
				return surface.Trim();
			}
		}

		return string.IsNullOrWhiteSpace(DefaultSurfaceId) ? "default" : DefaultSurfaceId.Trim();
	}

	private void CacheSurfaceLayers()
	{
		_surfaceLayers.Clear();
		foreach (NodePath path in SurfaceLayerPaths)
		{
			if (path == null || string.IsNullOrWhiteSpace(path.ToString()))
			{
				continue;
			}

			TileMapLayer layer = GetNodeOrNull<TileMapLayer>(path);
			if (layer == null)
			{
				GD.PushWarning($"[TileSurfaceResolver] Missing TileMapLayer at path '{path}'.");
				continue;
			}

			_surfaceLayers.Add(layer);
		}

		if (_surfaceLayers.Count == 0)
		{
			GD.PushWarning("[TileSurfaceResolver] No surface layers are configured. Footsteps will use the default surface.");
		}
	}
}
