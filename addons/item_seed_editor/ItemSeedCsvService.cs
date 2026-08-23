#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace ethra.V1
{
	public sealed class ItemSeedCsvFile
	{
		public string Path { get; set; } = string.Empty;
		public List<string> Headers { get; } = new();
		public List<ItemSeedCsvRow> Rows { get; } = new();
		public bool IsDirty => Rows.Any(row => row.IsDirty || row.IsDeleted);
	}

	public sealed class ItemSeedCsvRow
	{
		public ItemSeedCsvFile SourceFile { get; set; }
		public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
		public bool IsDirty { get; set; }
		public bool IsNew { get; set; }
		public bool IsDeleted { get; set; }

		public string GetValue(string header)
		{
			return !string.IsNullOrWhiteSpace(header) && Values.TryGetValue(header, out string value) ? value : string.Empty;
		}

		public void SetValue(string header, string value)
		{
			if (string.IsNullOrWhiteSpace(header))
			{
				return;
			}

			value ??= string.Empty;
			if (Values.TryGetValue(header, out string current) && current == value)
			{
				return;
			}

			Values[header] = value;
			IsDirty = true;
		}
	}

	public sealed class ItemSeedCsvService
	{
		public const string DataFolder = "res://Core/Inventory/Data/";
		private const string BackupFolder = "res://Core/Inventory/Data/backups/";

		public List<ItemSeedCsvFile> LoadAll()
		{
			List<ItemSeedCsvFile> files = new();
			using DirAccess dir = DirAccess.Open(DataFolder);
			if (dir == null)
			{
				GD.PushError($"ItemSeedCsvService: failed to open {DataFolder}.");
				return files;
			}

			dir.ListDirBegin();
			while (true)
			{
				string entry = dir.GetNext();
				if (string.IsNullOrEmpty(entry))
				{
					break;
				}

				if (dir.CurrentIsDir() || entry.StartsWith(".", StringComparison.Ordinal) || !entry.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				ItemSeedCsvFile file = LoadFile(DataFolder + entry);
				if (file != null)
				{
					files.Add(file);
				}
			}
			dir.ListDirEnd();

			return files.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public ItemSeedCsvFile LoadFile(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
			{
				GD.PushWarning($"ItemSeedCsvService: missing CSV file {path}.");
				return null;
			}

			string text = FileAccess.GetFileAsString(path);
			List<List<string>> records = ParseCsv(text);
			if (records.Count == 0)
			{
				GD.PushWarning($"ItemSeedCsvService: CSV file has no header row: {path}.");
				return null;
			}

			ItemSeedCsvFile file = new() { Path = path };
			foreach (string header in records[0])
			{
				file.Headers.Add(header.Trim());
			}

			for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
			{
				List<string> record = records[rowIndex];
				if (record.Count == 0 || record.All(string.IsNullOrWhiteSpace))
				{
					continue;
				}

				ItemSeedCsvRow row = new() { SourceFile = file };
				for (int column = 0; column < file.Headers.Count; column++)
				{
					string value = column < record.Count ? record[column] : string.Empty;
					row.Values[file.Headers[column]] = value;
				}

				file.Rows.Add(row);
			}

			return file;
		}

		public ItemSeedCsvRow AddRow(ItemSeedCsvFile file, string placeholderId)
		{
			if (file == null)
			{
				return null;
			}

			ItemSeedCsvRow row = new()
			{
				SourceFile = file,
				IsDirty = true,
				IsNew = true
			};

			foreach (string header in file.Headers)
			{
				row.Values[header] = string.Empty;
			}

			string idHeader = FindIdHeader(file.Headers);
			if (!string.IsNullOrWhiteSpace(idHeader))
			{
				row.Values[idHeader] = placeholderId;
			}

			string nameHeader = FindFirstHeader(file.Headers, "name", "display_name", "displayName");
			if (!string.IsNullOrWhiteSpace(nameHeader))
			{
				row.Values[nameHeader] = "New Item";
			}

			file.Rows.Add(row);
			return row;
		}

		public ItemSeedCsvRow DuplicateRow(ItemSeedCsvRow source)
		{
			if (source?.SourceFile == null)
			{
				return null;
			}

			ItemSeedCsvRow copy = new()
			{
				SourceFile = source.SourceFile,
				IsDirty = true,
				IsNew = true
			};

			foreach (string header in source.SourceFile.Headers)
			{
				copy.Values[header] = source.GetValue(header);
			}

			string idHeader = FindIdHeader(source.SourceFile.Headers);
			if (!string.IsNullOrWhiteSpace(idHeader))
			{
				string original = source.GetValue(idHeader);
				copy.Values[idHeader] = string.IsNullOrWhiteSpace(original) ? "item.new_item.copy" : $"{original}.copy";
			}

			source.SourceFile.Rows.Add(copy);
			return copy;
		}

		public Error SaveFile(ItemSeedCsvFile file, bool createBackup, out string backupPath)
		{
			backupPath = string.Empty;
			if (file == null || string.IsNullOrWhiteSpace(file.Path))
			{
				return Error.InvalidParameter;
			}

			if (createBackup && FileAccess.FileExists(file.Path))
			{
				Error backupError = CreateBackup(file.Path, out backupPath);
				if (backupError != Error.Ok)
				{
					return backupError;
				}
			}

			StringBuilder builder = new();
			builder.AppendLine(WriteCsvRecord(file.Headers));
			foreach (ItemSeedCsvRow row in file.Rows.Where(row => !row.IsDeleted))
			{
				List<string> values = new();
				foreach (string header in file.Headers)
				{
					values.Add(row.GetValue(header));
				}
				builder.AppendLine(WriteCsvRecord(values));
			}

			using FileAccess output = FileAccess.Open(file.Path, FileAccess.ModeFlags.Write);
			if (output == null)
			{
				return FileAccess.GetOpenError();
			}

			output.StoreString(builder.ToString());
			foreach (ItemSeedCsvRow row in file.Rows.ToList())
			{
				if (row.IsDeleted)
				{
					file.Rows.Remove(row);
					continue;
				}

				row.IsDirty = false;
				row.IsNew = false;
			}

			return Error.Ok;
		}

		public static string FindIdHeader(IReadOnlyList<string> headers)
		{
			return FindFirstHeader(headers, "id", "item_id", "itemid", "ItemId");
		}

		public static string FindFirstHeader(IReadOnlyList<string> headers, params string[] candidates)
		{
			foreach (string candidate in candidates)
			{
				string header = headers.FirstOrDefault(existing => string.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase));
				if (!string.IsNullOrWhiteSpace(header))
				{
					return header;
				}
			}

			return string.Empty;
		}

		private static Error CreateBackup(string sourcePath, out string backupPath)
		{
			backupPath = string.Empty;
			if (!DirAccess.DirExistsAbsolute(BackupFolder))
			{
				Error makeDirError = DirAccess.MakeDirRecursiveAbsolute(BackupFolder);
				if (makeDirError != Error.Ok)
				{
					return makeDirError;
				}
			}

			string fileName = sourcePath.GetFile().Replace(".csv", string.Empty, StringComparison.OrdinalIgnoreCase);
			backupPath = $"{BackupFolder}{fileName}.{DateTime.Now:yyyyMMdd_HHmmss}.bak.csv";
			return DirAccess.CopyAbsolute(sourcePath, backupPath);
		}

		private static string WriteCsvRecord(IEnumerable<string> values)
		{
			return string.Join(",", values.Select(EscapeCsvValue));
		}

		private static string EscapeCsvValue(string value)
		{
			value ??= string.Empty;
			bool mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
			if (!mustQuote)
			{
				return value;
			}

			return $"\"{value.Replace("\"", "\"\"")}\"";
		}

		private static List<List<string>> ParseCsv(string text)
		{
			List<List<string>> records = new();
			List<string> record = new();
			StringBuilder value = new();
			bool inQuotes = false;

			for (int index = 0; index < text.Length; index++)
			{
				char current = text[index];
				if (current == '"')
				{
					bool escapedQuote = inQuotes && index + 1 < text.Length && text[index + 1] == '"';
					if (escapedQuote)
					{
						value.Append('"');
						index++;
					}
					else
					{
						inQuotes = !inQuotes;
					}
					continue;
				}

				if (current == ',' && !inQuotes)
				{
					record.Add(value.ToString());
					value.Clear();
					continue;
				}

				if ((current == '\n' || current == '\r') && !inQuotes)
				{
					if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
					{
						index++;
					}

					record.Add(value.ToString());
					value.Clear();
					records.Add(record);
					record = new List<string>();
					continue;
				}

				value.Append(current);
			}

			if (value.Length > 0 || record.Count > 0)
			{
				record.Add(value.ToString());
				records.Add(record);
			}

			return records;
		}
	}
}
#endif
