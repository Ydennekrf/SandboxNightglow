using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ethra.V1
{
	public partial class MasterRepository
	{
		public enum RepoLoadType
		{
			Items,
			ItemEffects,
			Dialog,
			Entity
		}

		//=========== fields and Properties ==================//
		// this scene repo is for game levels
		private Dictionary<string, PackedScene> _sceneRepo;

		private Dictionary<int, InventoryItem> _itemRepo;
		private Dictionary<string, DialogTree> _dialogTreeRepo;
		private Dictionary<string, QuestDefinition> _questDefinitionRepo;

		public MasterRepository()
		{
			_sceneRepo = new Dictionary<string, PackedScene>();
			_itemRepo = new Dictionary<int, InventoryItem>();
			_dialogTreeRepo = new Dictionary<string, DialogTree>();
			_questDefinitionRepo = new Dictionary<string, QuestDefinition>();
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

		public void FillSQLRepo(string path)
		{
			// the item, dialog and Entity objects will be stored in an SQLite DB the path will determine which table to pull form
			// likely use a switch statement that will parse the response, create the object type and add it to the proper dictionary
		}

		public void FillCsvRepo(string path, RepoLoadType loadType, IEnumerable<string> requiredHeaders = null)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				GD.PushError("FillCsvRepo: csv path is empty.");
				return;
			}

			if (!FileAccess.FileExists(path))
			{
				GD.PushError($"FillCsvRepo: file does not exist: {path}");
				return;
			}

			var rows = ReadCsvRows(path);
			if (rows.Count == 0)
			{
				GD.PushError($"FillCsvRepo: no rows found in csv: {path}");
				return;
			}

			var headers = rows[0];
			if (!ValidateHeaders(headers, requiredHeaders))
			{
				GD.PushError($"FillCsvRepo: required headers missing for {loadType} csv: {path}");
				return;
			}

				switch (loadType)
				{
					case RepoLoadType.Items:
						LoadItemsFromCsv(headers, rows, path);
						break;
					case RepoLoadType.ItemEffects:
						LoadItemEffectsFromCsv(headers, rows, path);
						break;
					case RepoLoadType.Dialog:
						GD.Print($"FillCsvRepo: dialog loader not implemented yet. source={path}");
						break;
				case RepoLoadType.Entity:
					GD.Print($"FillCsvRepo: entity loader not implemented yet. source={path}");
					break;
			}
		}

		public void FillDialogTreeRepo(string rootPath)
		{
			if (string.IsNullOrWhiteSpace(rootPath))
			{
				GD.PushError("FillDialogTreeRepo: rootPath is empty.");
				return;
			}

			if (!rootPath.EndsWith("/"))
				rootPath += "/";

			if (!DirAccess.DirExistsAbsolute(rootPath))
			{
				GD.PushError($"FillDialogTreeRepo: directory does not exist: {rootPath}");
				return;
			}

			_dialogTreeRepo.Clear();
			int loaded = 0;
			int skipped = 0;

			using var dir = DirAccess.Open(rootPath);
			if (dir == null)
			{
				GD.PushError($"FillDialogTreeRepo: failed to open directory: {rootPath}");
				return;
			}

			dir.ListDirBegin();
			while (true)
			{
				string entry = dir.GetNext();
				if (string.IsNullOrEmpty(entry))
					break;

				if (entry is "." or ".." || dir.CurrentIsDir())
					continue;

				if (!entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
					continue;

				if (TryLoadDialogTree(rootPath + entry, out DialogTree tree))
				{
					_dialogTreeRepo[tree.TreeId] = tree;
					loaded++;
				}
				else
				{
					skipped++;
				}
			}
			dir.ListDirEnd();

			GD.Print($"FillDialogTreeRepo: loaded={loaded} skipped={skipped} source={rootPath}");
		}

		public void FillQuestDefinitionRepo(string rootPath)
		{
			if (string.IsNullOrWhiteSpace(rootPath))
			{
				GD.PushError("FillQuestDefinitionRepo: rootPath is empty.");
				return;
			}

			if (!rootPath.EndsWith("/"))
				rootPath += "/";

			if (!DirAccess.DirExistsAbsolute(rootPath))
			{
				GD.PushError($"FillQuestDefinitionRepo: directory does not exist: {rootPath}");
				return;
			}

			_questDefinitionRepo.Clear();
			int loaded = 0;
			int skipped = 0;

			using var dir = DirAccess.Open(rootPath);
			if (dir == null)
			{
				GD.PushError($"FillQuestDefinitionRepo: failed to open directory: {rootPath}");
				return;
			}

			dir.ListDirBegin();
			while (true)
			{
				string entry = dir.GetNext();
				if (string.IsNullOrEmpty(entry))
					break;

				if (entry is "." or ".." || dir.CurrentIsDir())
					continue;

				if (!entry.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
					continue;

				if (TryLoadQuestDefinition(rootPath + entry, out QuestDefinition definition))
				{
					_questDefinitionRepo[definition.QuestId] = definition;
					loaded++;
				}
				else
				{
					skipped++;
				}
			}
			dir.ListDirEnd();

			GD.Print($"FillQuestDefinitionRepo: loaded={loaded} skipped={skipped} source={rootPath}");
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

		public DialogTree GetDialogTreeFromRepo(string treeId)
		{
			if (string.IsNullOrWhiteSpace(treeId))
			{
				return null;
			}

			return _dialogTreeRepo.TryGetValue(treeId, out DialogTree tree) ? tree : null;
		}

		public QuestDefinition GetQuestDefinitionFromRepo(string questId)
		{
			if (string.IsNullOrWhiteSpace(questId))
			{
				return null;
			}

			return _questDefinitionRepo.TryGetValue(questId, out QuestDefinition definition) ? definition : null;
		}

		private bool TryLoadDialogTree(string path, out DialogTree tree)
		{
			tree = null;
			string json = FileAccess.GetFileAsString(path);
			if (string.IsNullOrWhiteSpace(json))
			{
				GD.PushWarning($"TryLoadDialogTree: dialog file was empty: {path}");
				return false;
			}

			try
			{
				tree = JsonSerializer.Deserialize<DialogTree>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});
			}
			catch (JsonException ex)
			{
				GD.PushWarning($"TryLoadDialogTree: failed to parse {path}: {ex.Message}");
				return false;
			}

			return ValidateDialogTree(tree, path);
		}

		private bool TryLoadQuestDefinition(string path, out QuestDefinition definition)
		{
			definition = null;
			string json = FileAccess.GetFileAsString(path);
			if (string.IsNullOrWhiteSpace(json))
			{
				GD.PushWarning($"TryLoadQuestDefinition: quest file was empty: {path}");
				return false;
			}

			try
			{
				definition = JsonSerializer.Deserialize<QuestDefinition>(json, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true,
					Converters = { new JsonStringEnumConverter() }
				});
			}
			catch (JsonException ex)
			{
				GD.PushWarning($"TryLoadQuestDefinition: failed to parse {path}: {ex.Message}");
				return false;
			}

			return ValidateQuestDefinition(definition, path);
		}

		private bool ValidateDialogTree(DialogTree tree, string sourcePath)
		{
			if (tree == null)
			{
				GD.PushWarning($"ValidateDialogTree: parsed null dialog tree from {sourcePath}.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(tree.TreeId))
			{
				GD.PushWarning($"ValidateDialogTree: dialog tree in {sourcePath} is missing TreeId.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(tree.StartingNodeId))
			{
				GD.PushWarning($"ValidateDialogTree: dialog tree '{tree.TreeId}' is missing StartingNodeId.");
				return false;
			}

			if (tree.Nodes == null || tree.Nodes.Count == 0)
			{
				GD.PushWarning($"ValidateDialogTree: dialog tree '{tree.TreeId}' has no nodes.");
				return false;
			}

			HashSet<string> nodeIds = new HashSet<string>();
			foreach (DialogNode node in tree.Nodes)
			{
				if (string.IsNullOrWhiteSpace(node.NodeId))
				{
					GD.PushWarning($"ValidateDialogTree: dialog tree '{tree.TreeId}' has a node with empty NodeId.");
					return false;
				}

				nodeIds.Add(node.NodeId);
			}

			if (!nodeIds.Contains(tree.StartingNodeId))
			{
				GD.PushWarning($"ValidateDialogTree: dialog tree '{tree.TreeId}' starts at missing node '{tree.StartingNodeId}'.");
				return false;
			}

			foreach (DialogNode node in tree.Nodes)
			{
				if (node.Choices == null)
				{
					node.Choices = new List<DialogChoice>();
					continue;
				}

				foreach (DialogChoice choice in node.Choices)
				{
					if (!string.IsNullOrWhiteSpace(choice.NextNodeId) && !nodeIds.Contains(choice.NextNodeId))
					{
						GD.PushWarning($"ValidateDialogTree: choice '{choice.ChoiceText}' in tree '{tree.TreeId}' points to missing node '{choice.NextNodeId}'.");
						return false;
					}
				}
			}

			return true;
		}

		private bool ValidateQuestDefinition(QuestDefinition definition, string sourcePath)
		{
			if (definition == null)
			{
				GD.PushWarning($"ValidateQuestDefinition: parsed null quest definition from {sourcePath}.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(definition.QuestId))
			{
				GD.PushWarning($"ValidateQuestDefinition: quest definition in {sourcePath} is missing QuestId.");
				return false;
			}

			if (definition.Objectives == null || definition.Objectives.Count == 0)
			{
				GD.PushWarning($"ValidateQuestDefinition: quest '{definition.QuestId}' has no objectives.");
				return false;
			}

			HashSet<string> objectiveIds = new HashSet<string>();
			foreach (QuestObjectiveDefinition objective in definition.Objectives)
			{
				if (string.IsNullOrWhiteSpace(objective.ObjectiveId))
				{
					GD.PushWarning($"ValidateQuestDefinition: quest '{definition.QuestId}' has an objective with empty ObjectiveId.");
					return false;
				}

				if (!objectiveIds.Add(objective.ObjectiveId))
				{
					GD.PushWarning($"ValidateQuestDefinition: quest '{definition.QuestId}' has duplicate objective '{objective.ObjectiveId}'.");
					return false;
				}
			}

			if (!string.IsNullOrWhiteSpace(definition.StartingObjectiveId) && !objectiveIds.Contains(definition.StartingObjectiveId))
			{
				GD.PushWarning($"ValidateQuestDefinition: quest '{definition.QuestId}' starts at missing objective '{definition.StartingObjectiveId}'.");
				return false;
			}

			return true;
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

		private bool ValidateHeaders(IReadOnlyList<string> headers, IEnumerable<string> requiredHeaders)
		{
			if (requiredHeaders == null)
			{
				return true;
			}

			HashSet<string> headerSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string header in headers)
			{
				headerSet.Add(header.Trim());
			}

			foreach (string required in requiredHeaders)
			{
				if (!headerSet.Contains(required))
				{
					GD.PushError($"FillCsvRepo: missing required header '{required}'.");
					return false;
				}
			}

			return true;
		}

		private List<string[]> ReadCsvRows(string path)
		{
			List<string[]> rows = new List<string[]>();

			using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PushError($"FillCsvRepo: unable to open file {path}");
				return rows;
			}

			while (!file.EofReached())
			{
				string line = file.GetLine();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				rows.Add(ParseCsvLine(line));
			}

			return rows;
		}

		private string[] ParseCsvLine(string line)
		{
			List<string> values = new List<string>();
			StringBuilder token = new StringBuilder();
			bool inQuotes = false;

			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];

				if (c == '"')
				{
					bool isEscapedQuote = inQuotes && i + 1 < line.Length && line[i + 1] == '"';
					if (isEscapedQuote)
					{
						token.Append('"');
						i++;
					}
					else
					{
						inQuotes = !inQuotes;
					}
					continue;
				}

				if (c == ',' && !inQuotes)
				{
					values.Add(token.ToString().Trim());
					token.Clear();
					continue;
				}

				token.Append(c);
			}

			values.Add(token.ToString().Trim());
			return values.ToArray();
		}

		private void LoadItemsFromCsv(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows, string sourcePath)
		{
			Dictionary<string, int> headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < headers.Count; i++)
			{
				headerIndex[headers[i].Trim()] = i;
			}

			_itemRepo.Clear();
			int loaded = 0;
			int skipped = 0;

			for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
			{
				string[] row = rows[rowIndex];

				if (!TryGetInt(headerIndex, row, "id", out int id))
				{
					skipped++;
					GD.PushError($"LoadItemsFromCsv: row {rowIndex + 1} missing/invalid id.");
					continue;
				}

				string name = GetString(headerIndex, row, "name");
				if (string.IsNullOrWhiteSpace(name))
				{
					skipped++;
					GD.PushError($"LoadItemsFromCsv: row {rowIndex + 1} has empty name for id {id}.");
					continue;
				}

					string description = GetString(headerIndex, row, "description");
					string rarity = GetString(headerIndex, row, "rarity");
						string category = GetString(headerIndex, row, "category");
						string subtype = GetString(headerIndex, row, "subtype");
						string iconPath = GetString(headerIndex, row, "icon_path");
						string weaponUpDrawPath = GetString(headerIndex, row, "weapon_up_draw_path");
						string weaponDownDrawPath = GetString(headerIndex, row, "weapon_down_draw_path");
						string weaponUpStowPath = GetString(headerIndex, row, "weapon_up_stow_path");
						string weaponDownStowPath = GetString(headerIndex, row, "weapon_down_stow_path");
						string comboProfilePath = GetString(headerIndex, row, "combo_profile_path");
						int value = GetIntOrDefault(headerIndex, row, "sell_value", 0);
						int maxStack = GetIntOrDefault(headerIndex, row, "max_stack", 99);

					if (_itemRepo.ContainsKey(id))
					{
					skipped++;
					GD.PushError($"LoadItemsFromCsv: duplicate item id {id} at row {rowIndex + 1}.");
					continue;
				}

					InventoryItem item = CreateInventoryItem(
						id,
						name,
						value,
								description,
								rarity,
								category,
								subtype,
								maxStack,
								new List<ItemEffects>(),
								iconPath,
								weaponUpDrawPath,
								weaponDownDrawPath,
								weaponUpStowPath,
								weaponDownStowPath,
								comboProfilePath);
							_itemRepo.Add(id, item);
							loaded++;
						}

				GD.Print($"LoadItemsFromCsv: loaded={loaded} skipped={skipped} source={sourcePath}");
			}

		private InventoryItem CreateInventoryItem(
				int id,
				string name,
				int value,
				string description,
				string rarity,
				string category,
				string subtype,
				int maxStack,
				List<ItemEffects> effects,
				string iconPath,
				string weaponUpDrawPath,
				string weaponDownDrawPath,
				string weaponUpStowPath,
				string weaponDownStowPath,
				string comboProfilePath)
			{
				if (string.Equals(category, "Crafting", StringComparison.OrdinalIgnoreCase))
				{
					return new CraftingItem(id, name, value, description, rarity, subtype, maxStack, effects, iconPath);
				}

				if (string.Equals(category, "Consumable", StringComparison.OrdinalIgnoreCase))
				{
					return new ConsumeItem(id, name, value, description, rarity, subtype, maxStack, effects, iconPath);
				}

				if (string.Equals(category, "Armor", StringComparison.OrdinalIgnoreCase))
				{
					return new ArmorItem(id, name, value, description, rarity, subtype, maxStack, effects, iconPath);
				}

				if (string.Equals(category, "Weapon", StringComparison.OrdinalIgnoreCase))
				{
					return new WeaponItem(
						id,
						name,
						value,
						description,
						rarity,
						maxStack,
						effects,
						iconPath,
						weaponUpDrawPath,
						weaponDownDrawPath,
						weaponUpStowPath,
						weaponDownStowPath,
						comboProfilePath);
				}

				return new BasicInventoryItem(id, name, value, description, rarity, effects, category: category, subtype: subtype, maxStack: maxStack, iconPath: iconPath);
			}

		private void LoadItemEffectsFromCsv(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows, string sourcePath)
		{
			Dictionary<string, int> headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < headers.Count; i++)
			{
				headerIndex[headers[i].Trim()] = i;
			}

			int loaded = 0;
			int skipped = 0;

			for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
			{
				string[] row = rows[rowIndex];
				if (!TryGetInt(headerIndex, row, "item_id", out int itemId))
				{
					skipped++;
					GD.PushError($"LoadItemEffectsFromCsv: row {rowIndex + 1} missing/invalid item_id.");
					continue;
				}

				if (!_itemRepo.TryGetValue(itemId, out InventoryItem item))
				{
					skipped++;
					GD.PushError($"LoadItemEffectsFromCsv: item {itemId} not found in item repo.");
					continue;
				}

				string effectType = GetString(headerIndex, row, "effect_type");
				string effectStat = GetString(headerIndex, row, "effect_stat");
				int effectPower = GetIntOrDefault(headerIndex, row, "effect_power", 0);

				ItemEffects effect = BuildEffect(effectType, effectStat, effectPower);
				if (effect == null)
				{
					skipped++;
					continue;
				}

				item.Effects.Add(effect);
				loaded++;
			}

			GD.Print($"LoadItemEffectsFromCsv: loaded={loaded} skipped={skipped} source={sourcePath}");
		}

		private ItemEffects BuildEffect(string effectType, string effectStat, int effectPower)
		{
			if (string.IsNullOrWhiteSpace(effectType) || string.IsNullOrWhiteSpace(effectStat) || effectPower == 0)
			{
				return null;
			}

			string effectName = $"{effectType}:{effectStat}";

			if (string.Equals(effectType, "plus", StringComparison.OrdinalIgnoreCase))
			{
				return new PlusStat(effectStat, effectPower, effectName, null);
			}
			if (string.Equals(effectType, "minus", StringComparison.OrdinalIgnoreCase))
			{
				return new MinusStat(effectStat, effectPower, effectName, null);
			}

			return null;
		}

		private string GetString(Dictionary<string, int> headerIndex, string[] row, string headerName)
		{
			if (!headerIndex.TryGetValue(headerName, out int index))
			{
				return string.Empty;
			}

			if (index < 0 || index >= row.Length)
			{
				return string.Empty;
			}

			return row[index].Trim();
		}

		private bool TryGetInt(Dictionary<string, int> headerIndex, string[] row, string headerName, out int value)
		{
			value = 0;
			string raw = GetString(headerIndex, row, headerName);
			return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}

		private int GetIntOrDefault(Dictionary<string, int> headerIndex, string[] row, string headerName, int defaultValue)
		{
			if (TryGetInt(headerIndex, row, headerName, out int parsed))
			{
				return parsed;
			}

			return defaultValue;
		}
	}
}
