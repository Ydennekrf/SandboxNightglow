// // this manager class will handle compiling all the items in a dictionary 
// // dictionary<itemID, ITEM>

using Godot;
using System.Collections.Generic;
using System.Data.Common;

public partial class InventoryManager : Node
{
    public static InventoryManager I { get; private set; }

    private readonly Dictionary<string, InventoryItem> _db = new();

    public override void _EnterTree()
    {


        if (I != null && I != this)
        {
            QueueFree();
            return;
        }
        I = this;

        foreach (var file in DirAccess.GetFilesAt("res://Data/Items/"))
            if (file.EndsWith(".tres") || file.EndsWith(".res"))
                LoadItem($"res://Data/Items/{file}");



        WeaponRegistry.LoadAll();

        GD.Print($"[InventoryManager] Loaded {_db.Count} item defs");
    }

    public InventoryItem Get(string id) => _db[id];

    /* ─────────────────────────── Helpers ─────────────────────────── */
    private void LoadItem(string path)
    {
        var data = ResourceLoader.Load<InventoryItem>(path);
        if (data == null)
            GD.PushWarning($"InventoryManager: failed to load {path}");
        else
            _db[data.ItemId] = data;
    }

    public void AddItem(InventoryItem item)
    {
        if (_db.ContainsKey(item.ItemId)) return;

        _db.Add(item.ItemId, item);
    }
}