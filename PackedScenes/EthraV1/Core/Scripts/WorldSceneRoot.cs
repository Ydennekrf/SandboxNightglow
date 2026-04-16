using Godot;
using System;
using System.Collections.Generic;

public partial class WorldSceneRoot : Node2D
{
		[Export] public NodePath MapPath { get; set; } = "Map";
		[Export] public NodePath EntitiesPath { get; set; } = "Entities";
		[Export] public NodePath SpawnPointsPath { get; set; } = "Entities/SpawnPoints";
		[Export] public NodePath InteractablesPath { get; set; } = "Interactables";

		public Node2D Map => GetNode<Node2D>(MapPath);
		public Node2D Entities => GetNode<Node2D>(EntitiesPath);
		public Node2D SpawnPoints => GetNode<Node2D>(SpawnPointsPath);
		public Node2D Interactables => GetNode<Node2D>(InteractablesPath);

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
}
