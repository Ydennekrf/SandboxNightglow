// // this manager class will handle compiling all the items in a dictionary 
// // dictionary<itemID, ITEM>

using Godot;
using System.Collections.Generic;
using System.Data.Common;

public partial class InventoryManager : Node
{
    public static InventoryManager I { get; private set; }

    private readonly Dictionary<string, InventoryItem> _db = new();
    private Dictionary<string, PackedScene> GearCache = new();

    [Export(PropertyHint.File, "*.db")] 
    private string _dbFile = "res://Data/DB/GameData.db";


    public override void _EnterTree()
    {


        if (I != null && I != this)
        {
            QueueFree();
            return;
        }
        I = this;

        InitFromDatabase();

        GD.Print($"[InventoryManager] Loaded {_db.Count} item defs");
    }

    private void InitFromDatabase()
    {
        var repo = new ItemRepository(ProjectSettings.GlobalizePath(_dbFile));

        foreach (var rec in repo.LoadAll())
        {
            // Convert the pure record → the Godot Resource you already use.
            var res = new InventoryItem
            {
                ItemId          = rec.ItemId,
                ItemName        = rec.ItemName,
                ItemDescription = rec.ItemDescription,
                ItemValue       = rec.ItemValue,
                IconSprite      = GD.Load<Texture2D>(rec.ItemIcon),
                ItemStackSize   = rec.ItemStackSize,
                Rarity          = (ItemRarity)rec.ItemRarity,
                itemType        = (ItemType)rec.ItemType,
                AssetPath       = rec.ItemScenePath
            };
            AddItem(res);
        }

        GD.Print($"Loaded {_db.Count} items from SQLite.");
    }

    // this method is used for getting any equippable item, first it checks if its already cached if yes return the loaded scene
    // if no then load the scene and add it to the cache.
    // if this runs into performance issues consider adding a check to only keep a set amount of gear cached so for long play times it doesnt get too muich bloat.
    public PackedScene GetGear(InventoryItem item)
    {
        if (GearCache.ContainsKey(item.ItemId))
        {
            return GearCache[item.ItemId];
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(item.AssetPath);
        if (scene == null)
        {
            GD.PushError($"WeaponRegistry: failed to load PackedScene at {item.AssetPath}");
            return null;
        }

        GearCache[item.ItemId] = scene;

        return scene;
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