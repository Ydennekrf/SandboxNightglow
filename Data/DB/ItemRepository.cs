using Microsoft.Data.Sqlite;
using Godot;
using System.Collections.Generic;

public sealed class ItemRepository
{
    private readonly string _dbPath;

    public ItemRepository(string dbPath)
    {
        _dbPath = dbPath;           
    }

    /// Loads every row once on start-up – perfect for object pools.
    public IEnumerable<ItemRecord> LoadAll()
    {
        var list = new List<ItemRecord>();

        // Godot needs a normal OS path, not res://, so use ProjectSettings.GlobalizePath if needed.
        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Items;";
        using var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            list.Add(new ItemRecord(
                rdr.GetString(0),  // ItemId
                rdr.GetString(1),  // Name
                rdr.GetString(2),  // Description
                rdr.GetInt32(3),   // Value
                rdr.GetString(4),  // Icon path
                rdr.GetInt32(5),   // StackSize
                rdr.GetInt32(6),   // Rarity int
                rdr.GetString(7),  // LootTable
                rdr.GetString(8),   // Scene path
                rdr.GetInt32(9)     //ItemType int
            ));
        }
        return list;
    }
}