#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace ethra.V1
{
	public enum ItemSeedValidationSeverity
	{
		Error,
		Warning
	}

	public sealed class ItemSeedValidationResult
	{
		public ItemSeedValidationSeverity Severity { get; init; }
		public ItemSeedCsvRow Row { get; init; }
		public string SourcePath { get; init; } = string.Empty;
		public string ItemId { get; init; } = string.Empty;
		public string Message { get; init; } = string.Empty;
	}

	public sealed class ItemSeedValidationService
	{
		private static readonly Regex StableIdRegex = new("^[a-z0-9._-]+$", RegexOptions.Compiled);
		private static readonly HashSet<string> NumericHeaders = new(StringComparer.OrdinalIgnoreCase)
		{
			"id", "item_id", "sell_value", "max_stack", "upgrade_rune_slots", "effect_power", "status_stacks"
		};

		private static readonly HashSet<string> StatusIds = new(StringComparer.OrdinalIgnoreCase)
		{
			"status.knockback",
			"status.stun",
			"status.armor_break",
			"status.cold",
			"status.burn",
			"status.poison",
			"status.mana_burn",
			"status.silence",
			"status.blind",
			"status.thorns"
		};

		private static readonly HashSet<string> StatusTriggers = new(StringComparer.OrdinalIgnoreCase)
		{
			"on_equip",
			"on_use",
			"on_hit"
		};

		private static readonly HashSet<string> PathHeaders = new(StringComparer.OrdinalIgnoreCase)
		{
			"icon_path",
			"weapon_up_draw_path",
			"weapon_down_draw_path",
			"weapon_up_stow_path",
			"weapon_down_stow_path",
			"combo_profile_path"
		};

		private static readonly HashSet<string> Elements = new(StringComparer.OrdinalIgnoreCase)
		{
			"Electricity", "Ice", "Fire", "Acid", "Darkness"
		};

		private static readonly HashSet<string> MagicShapes = new(StringComparer.OrdinalIgnoreCase)
		{
			"ProjectileBolt", "Linear", "Cone", "CircleWaveAwayFromPlayer"
		};

		public List<ItemSeedValidationResult> Validate(IReadOnlyList<ItemSeedCsvFile> files)
		{
			List<ItemSeedValidationResult> results = new();
			ValidateDuplicateIds(files, results);

			foreach (ItemSeedCsvFile file in files)
			{
				foreach (ItemSeedCsvRow row in file.Rows.Where(row => !row.IsDeleted))
				{
					ValidateRow(row, results);
				}
			}

			return results;
		}

		private static void ValidateDuplicateIds(IReadOnlyList<ItemSeedCsvFile> files, List<ItemSeedValidationResult> results)
		{
			Dictionary<string, List<ItemSeedCsvRow>> byId = new(StringComparer.OrdinalIgnoreCase);
			foreach (ItemSeedCsvFile file in files)
			{
				string idHeader = ItemSeedCsvService.FindFirstHeader(file.Headers, "id");
				if (string.IsNullOrWhiteSpace(idHeader))
				{
					continue;
				}

				foreach (ItemSeedCsvRow row in file.Rows.Where(row => !row.IsDeleted))
				{
					string id = row.GetValue(idHeader).Trim();
					if (string.IsNullOrWhiteSpace(id))
					{
						continue;
					}

					if (!byId.TryGetValue(id, out List<ItemSeedCsvRow> rows))
					{
						rows = new List<ItemSeedCsvRow>();
						byId[id] = rows;
					}

					rows.Add(row);
				}
			}

			foreach (var pair in byId.Where(pair => pair.Value.Count > 1))
			{
				foreach (ItemSeedCsvRow row in pair.Value)
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Duplicate item id '{pair.Key}' across loaded CSV data."));
				}
			}
		}

		private static void ValidateRow(ItemSeedCsvRow row, List<ItemSeedValidationResult> results)
		{
			ItemSeedCsvFile file = row.SourceFile;
			string idHeader = ItemSeedCsvService.FindIdHeader(file.Headers);
			string id = string.IsNullOrWhiteSpace(idHeader) ? string.Empty : row.GetValue(idHeader).Trim();

			if (string.IsNullOrWhiteSpace(idHeader))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "CSV has no recognized id/item_id column."));
			}
			else if (string.IsNullOrWhiteSpace(id))
			{
				results.Add(Build(ItemSeedValidationSeverity.Error, row, "Missing item id."));
			}
			else if (!int.TryParse(id, out _) && !StableIdRegex.IsMatch(id))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, $"Item id '{id}' is not numeric and does not match stable-id characters."));
			}

			string nameHeader = ItemSeedCsvService.FindFirstHeader(file.Headers, "name", "display_name", "displayName");
			if (!string.IsNullOrWhiteSpace(nameHeader) && string.IsNullOrWhiteSpace(row.GetValue(nameHeader)))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Missing display name/name."));
			}

			foreach (string header in file.Headers)
			{
				string value = row.GetValue(header).Trim();
				if (string.IsNullOrWhiteSpace(value))
				{
					continue;
				}

				if (NumericHeaders.Contains(header) && !int.TryParse(value, out _))
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Column '{header}' expects a whole number."));
				}

				if (header.Equals("status_chance", StringComparison.OrdinalIgnoreCase)
					&& (!float.TryParse(value, out float chance) || chance < 0f || chance > 1f))
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, "Column 'status_chance' expects a decimal from 0 to 1."));
				}

				if (header.Equals("status_duration", StringComparison.OrdinalIgnoreCase)
					&& (!float.TryParse(value, out float duration) || duration < 0f))
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, "Column 'status_duration' expects a non-negative number."));
				}

				if (PathHeaders.Contains(header) && !ResourceExists(value))
				{
					results.Add(Build(ItemSeedValidationSeverity.Warning, row, $"Resource path in '{header}' does not exist: {value}"));
				}
			}

			string category = row.GetValue("category").Trim();
			if (file.Headers.Contains("category", StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(category))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Missing required category."));
			}

			if (file.Headers.Contains("icon_path", StringComparer.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(row.GetValue("icon_path")))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Missing icon path."));
			}

			ValidateTypeSpecific(row, category, results);
			ValidateItemEffectRow(row, results);
		}

		private static void ValidateItemEffectRow(ItemSeedCsvRow row, List<ItemSeedValidationResult> results)
		{
			if (!row.SourceFile.Headers.Contains("effect_type", StringComparer.OrdinalIgnoreCase))
			{
				return;
			}

			string effectType = row.GetValue("effect_type").Trim();
			if (effectType.Equals("status", StringComparison.OrdinalIgnoreCase))
			{
				string statusId = row.GetValue("status_id").Trim();
				string trigger = row.GetValue("status_trigger").Trim();
				if (!StatusIds.Contains(statusId))
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Unknown or missing status_id '{statusId}'."));
				}

				if (!StatusTriggers.Contains(trigger))
				{
					results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Unknown or missing status_trigger '{trigger}'."));
				}

				return;
			}

			if ((effectType.Equals("plus", StringComparison.OrdinalIgnoreCase) || effectType.Equals("minus", StringComparison.OrdinalIgnoreCase))
				&& (string.IsNullOrWhiteSpace(row.GetValue("effect_stat")) || string.IsNullOrWhiteSpace(row.GetValue("effect_power"))))
			{
				results.Add(Build(ItemSeedValidationSeverity.Error, row, "Stat effect rows require effect_stat and effect_power."));
			}
		}

		private static void ValidateTypeSpecific(ItemSeedCsvRow row, string category, List<ItemSeedValidationResult> results)
		{
			if (category.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
			{
				RequirePath(row, "weapon_up_draw_path", results);
				RequirePath(row, "weapon_down_draw_path", results);
				RequirePath(row, "weapon_up_stow_path", results);
				RequirePath(row, "weapon_down_stow_path", results);
				RequirePath(row, "combo_profile_path", results);
			}

			if (category.Equals("Armor", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(row.GetValue("subtype")))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Armor is missing subtype/equipment slot."));
			}

			if (category.Equals("Rune", StringComparison.OrdinalIgnoreCase))
			{
				string runeKind = row.GetValue("rune_kind").Trim();
				if (string.IsNullOrWhiteSpace(runeKind))
				{
					results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Rune is missing rune_kind."));
				}

				if (runeKind.Equals("Elemental", StringComparison.OrdinalIgnoreCase))
				{
					string element = row.GetValue("rune_element").Trim();
					string shape = row.GetValue("magic_shape").Trim();
					if (!Elements.Contains(element))
					{
						results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Elemental rune has unsupported or missing element '{element}'."));
					}

					if (!MagicShapes.Contains(shape))
					{
						results.Add(Build(ItemSeedValidationSeverity.Error, row, $"Elemental rune has unsupported or missing magic shape '{shape}'."));
					}
				}
			}

			if (category.Equals("Consumable", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(row.GetValue("subtype")))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Consumable is missing subtype/effect family."));
			}

			if (category.Equals("Crafting", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(row.GetValue("subtype")))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, "Crafting/material item is missing subtype/material family."));
			}
		}

		private static void RequirePath(ItemSeedCsvRow row, string header, List<ItemSeedValidationResult> results)
		{
			if (!row.SourceFile.Headers.Contains(header, StringComparer.OrdinalIgnoreCase))
			{
				return;
			}

			string value = row.GetValue(header).Trim();
			if (string.IsNullOrWhiteSpace(value))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, $"Weapon is missing required asset path '{header}'."));
			}
			else if (!ResourceExists(value))
			{
				results.Add(Build(ItemSeedValidationSeverity.Warning, row, $"Weapon asset path '{header}' does not exist: {value}"));
			}
		}

		private static bool ResourceExists(string path)
		{
			return !string.IsNullOrWhiteSpace(path) && (ResourceLoader.Exists(path) || FileAccess.FileExists(path));
		}

		private static ItemSeedValidationResult Build(ItemSeedValidationSeverity severity, ItemSeedCsvRow row, string message)
		{
			IReadOnlyList<string> headers = row.SourceFile != null ? row.SourceFile.Headers : Array.Empty<string>();
			string idHeader = ItemSeedCsvService.FindIdHeader(headers);
			return new ItemSeedValidationResult
			{
				Severity = severity,
				Row = row,
				SourcePath = row.SourceFile?.Path ?? string.Empty,
				ItemId = string.IsNullOrWhiteSpace(idHeader) ? string.Empty : row.GetValue(idHeader),
				Message = message
			};
		}
	}
}
#endif
