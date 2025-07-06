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
    foreach (var path in DirAccess.GetFilesAt("res://Data/Items/Weapons"))
    {
        if (!path.EndsWith(".tres")) continue;
            string filePath = $"res://Data/Items/Weapons/{path}";
            string itemId = filePath.GetFile().GetBaseName().ToSnakeCase();
            WeaponPaths[new StringName(itemId)] = path;
    }
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