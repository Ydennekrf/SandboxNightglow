using Godot;
using System;
using System.Collections.Generic;
using ethra.V1;

public partial class WorldSceneRoot : Node2D
{
		[Export] public NodePath MapPath = "Map";
		[Export] public NodePath EntitiesPath = "Entities";
		[Export] public NodePath SpawnPointsPath = "Entities/SpawnPoints";
		[Export] public NodePath InteractablesPath = "Interactables";
		[Export] public NodePath SurfaceResolverPath = "TileSurfaceResolver";

		public Node2D Map => GetNodeWithFallback<Node2D>(MapPath, "Map");
		public Node2D Entities => GetNodeWithFallback<Node2D>(EntitiesPath, "Entities");
		public Node2D SpawnPoints => GetNodeWithFallback<Node2D>(SpawnPointsPath, "Entities/SpawnPoints");
		public Node2D Interactables => GetNodeWithFallback<Node2D>(InteractablesPath, "Interactables");
		public TileSurfaceResolver SurfaceResolver => GetOptionalNodeWithFallback<TileSurfaceResolver>(SurfaceResolverPath, "TileSurfaceResolver");

		public Marker2D GetSpawn(string name)
		{
			return SpawnPoints.GetNodeOrNull<Marker2D>(name);
		}

		public IEnumerable<Marker2D> GetAllSpawns()
		{
			foreach (var child in SpawnPoints.GetChildren())
				if (child is Marker2D m) yield return m;
		}
		
		public Node GetPlayerContainer() => Entities.GetNodeOrNull<Node>("Player");
		public Node GetNpcByName(string name) => Entities.GetNodeOrNull<Node>($"NPCs/{name}");
		public Node GetEnemyByName(string name) => Entities.GetNodeOrNull<Node>($"Enemies/{name}");	
		
		public IEnumerable<Node> GetNodesInGroup(string groupName)
		{

				foreach (var n in GetTree().GetNodesInGroup(groupName))
				{
					if (n is Node node && IsAncestorOf(node))
						yield return node;
				}
		}

		private T GetNodeWithFallback<T>(NodePath configuredPath, string fallbackPath) where T : Node
		{
			if (!IsEmptyNodePath(configuredPath))
			{
				T node = GetNodeOrNull<T>(configuredPath);
				if (node != null)
				{
					return node;
				}
			}

			return GetNode<T>(fallbackPath);
		}

		private T GetOptionalNodeWithFallback<T>(NodePath configuredPath, string fallbackPath) where T : Node
		{
			if (!IsEmptyNodePath(configuredPath))
			{
				T node = GetNodeOrNull<T>(configuredPath);
				if (node != null)
				{
					return node;
				}
			}

			return GetNodeOrNull<T>(fallbackPath);
		}

		private static bool IsEmptyNodePath(NodePath path)
		{
			return path == null || string.IsNullOrWhiteSpace(path.ToString());
		}
}
