using Godot;
using System.Collections.Generic;

public static class WeaponRegistry
{
    /// Key = ItemId used by InventoryComponent  (e.g. "rusty_short_sword")
    /// Val = PackedScene path                  (e.g. "res://Weapons/RustyShortSword.tscn")
    public static readonly Dictionary<StringName, string> WeaponPaths = new()
    {
        { "rusty_short_sword", "res://Weapons/RustyShortSword.tscn" },
        { "iron_axe",          "res://Weapons/IronAxe.tscn"        }
        // add more here...
    };

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

    /// Load + cache so we don’t hit disk repeatedly
    private static readonly Dictionary<StringName, PackedScene> _cache = new();

    public static PackedScene? Load(StringName itemId)
    {
        if (_cache.TryGetValue(itemId, out var cached))
            return cached;

        if (!WeaponPaths.TryGetValue(itemId, out var path))
        {
            GD.PrintErr($"WeaponRegistry: no entry for ItemId '{itemId}'");
            return null;
        }

        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene != null) _cache[itemId] = scene;
        return scene;
    }
}