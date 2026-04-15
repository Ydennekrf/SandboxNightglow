



using System;
using System.Collections.Generic;
using Godot;

namespace ethra.V1
{
	public partial class MasterRepository
{
	//=========== fields and Properties ==================//
	// this scene repo is for game levels
	private Dictionary<string, PackedScene> _sceneRepo;

	private Dictionary<int, InventoryItem> _itemRepo;


		public MasterRepository()
		{
			_sceneRepo = new Dictionary<string, PackedScene>();
			_itemRepo = new Dictionary<int, InventoryItem>();
		}


		// dialog repo is a dictionary with and int for id and a DialogNode class object <int, DialogNode>

		// item repo is a dictionary with an int for ID and a InventoryItem class object <int, InventoryItem>

		// Entity repo is a dictionary that is used to store all NPCs, Allies and Monsters essentially all non player Entitys
		// it uses a string as an ID and a Entity class object <string, Entity>

		// ============ public facing methods =================//

		public void FillSceneRepo(string rootPath)
	{
			if (string.IsNullOrWhiteSpace(rootPath))
			{
				GD.PushError("FillSceneRepo: rootPath is empty.");
				return;
			}

			if (!rootPath.EndsWith("/"))
				rootPath += "/";

			if (!DirAccess.DirExistsAbsolute(rootPath))
			{
				GD.PushError($"FillSceneRepo: directory does not exist: {rootPath}");
				return;
			}

			int added = 0;
			ScanDirRecursive(rootPath, ref added);
			GD.Print($"FillSceneRepo: loaded {added} scenes from {rootPath}");
		}


		private void ScanDirRecursive(string dirPath, ref int added)
		{
			using var dir = DirAccess.Open(dirPath);
			if (dir == null)
			{
				GD.PushError($"FillSceneRepo: failed to open directory: {dirPath}");
				return;
			}

			dir.ListDirBegin();
			while (true)
			{
				var entry = dir.GetNext();
				if (string.IsNullOrEmpty(entry))
					break;

				if (entry is "." or "..")
					continue;

				var fullPath = dirPath + entry;

				if (dir.CurrentIsDir())
				{
					ScanDirRecursive(fullPath + "/", ref added);
					continue;
				}

				if (!entry.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
					continue;

				var key = entry[..^5]; // remove ".tscn"
				var packed = ResourceLoader.Load<PackedScene>(fullPath);

				if (packed == null)
				{
					GD.PushError($"FillSceneRepo: failed to load PackedScene: {fullPath}");
					continue;
				}

				_sceneRepo[key] = packed;
				added++;
			}
			dir.ListDirEnd();
		}

		public void FillSQLRepo(string path)
	{
		// the item, dialog and Entity objects will be stored in an SQLite DB the path will determine which table to pull form
		// likely use a switch statement that will parse the response, create the object type and add it to the proper dictionary

	}
	
	public PackedScene GetSceneFromRepo(string sceneName)
		{
			if (string.IsNullOrWhiteSpace(sceneName)) return null;
			return _sceneRepo.TryGetValue(sceneName, out var data) ? data : null;

		}
	
	public InventoryItem GetItemFromRepo(int id)
		{
			return _itemRepo.TryGetValue(id, out var item) ? item : null;
		}

	   
	}
}
