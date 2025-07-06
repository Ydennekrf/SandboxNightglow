using Godot;
using System.Collections.Generic;

public static class WeaponRegistry
{
    /// Key = ItemId used by InventoryComponent  (e.g. "rusty_short_sword")
    /// Val = PackedScene path                  (e.g. "res://Weapons/RustyShortSword.tscn")
    /// 

    private static readonly Dictionary<string, PackedScene> WeaponCache = new();
    public static readonly Dictionary<StringName, string> WeaponPaths = new()
    {

    };

    public static void LoadAll()
    {
        WeaponPaths.Clear();

        foreach (var path in DirAccess.GetFilesAt("res://Data/Items/Weapons"))
        {
            if (!path.EndsWith(".tscn")) continue;

            string filePath = $"res://Data/Items/Weapons/{path}";
            PackedScene scene = GD.Load<PackedScene>(filePath);
            if (scene == null) { GD.PushError($"Bad scene: {filePath}"); continue; }

            if (scene.Instantiate() is not WeaponBase wb)
            {
                GD.PushError($"{path} doesn’t inherit WeaponBase");
                continue;
            }

            StringName id = string.IsNullOrEmpty(wb.ItemId) ? path.GetBaseName().ToSnakeCase() : wb.ItemId;

            WeaponPaths[id] = filePath;

            WeaponItem item = new WeaponItem
            {
                ItemId = id,
                ItemName = wb.ItemName,
                ItemStackSize = wb.ItemStackSize,
                IconSprite = wb.IconSprite,
                ItemValue = wb.ItemValue,
                ItemDescription = wb.ItemDescription,
                Rarity = wb.Rarity
            };

            InventoryManager.I.AddItem(item);
            wb.QueueFree();

        }
         GD.Print($"[WeaponRegistry] scenes: {WeaponPaths.Count}");
    }

    /// Optional editor check – call once in a unit-test or on project startup
    public static void ValidateAll()
    {
        foreach (var kv in WeaponPaths)
        {
            if (ResourceLoader.Load<PackedScene>(kv.Value) == null)
                GD.PushError($"WeaponRegistry: could not load '{kv.Key}' at {kv.Value}");
            else
                GD.Print($"WeaponRegistry: validated '{kv.Key}'");
        }
    }

    public static PackedScene? Load(StringName itemId)
    {
        // 1) Cached?
        if (WeaponCache.TryGetValue(itemId, out var scene))
            return scene;

        // 2) Path known?
        if (!WeaponPaths.TryGetValue(itemId, out var path))
        {
            GD.PushError($"WeaponRegistry: no path for ItemId '{itemId}'");
            return null;
        }

        // 3) Load & cache
        scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null)
        {
            GD.PushError($"WeaponRegistry: failed to load PackedScene at {path}");
            return null;
        }

        WeaponCache[itemId] = scene;
        return scene;
    }


 
}