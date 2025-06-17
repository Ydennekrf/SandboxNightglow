using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class PolymorphicItemConverter : JsonConverter<InventoryItem>
{
    public override InventoryItem Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        using var doc = JsonDocument.ParseValue(ref r);
        var root = doc.RootElement;
        var typeStr = root.GetProperty("itemType").GetString();

        Type target = typeStr switch
        {
            "Weapon" => typeof(WeaponItem),
            "Armor" => typeof(ArmorItem),
            "Trinket" => typeof(TrinketItem),
            "Instant" => typeof(InstantConsumable),
            "Buff" => typeof(BuffConsumable),
            "Permanent" => typeof(PermanentConsumable),
            "Reagent" => typeof(ReagentItem),
            "Quest" => typeof(QuestItem),
            _ => typeof(InventoryItem)
        };
        return (InventoryItem)JsonSerializer.Deserialize(root.GetRawText(), target, o);
    }
    public override void Write(Utf8JsonWriter w, InventoryItem v, JsonSerializerOptions o)
        => JsonSerializer.Serialize(w, v, v.GetType(), o);
}