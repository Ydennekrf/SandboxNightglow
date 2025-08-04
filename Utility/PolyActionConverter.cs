// using System;
// using System.Collections.Generic;
// using System.Text.Json;
// using System.Text.Json.Serialization;

// public class PolymorphicActionConverter<TBase> : JsonConverter<TBase>
//     where TBase : class
// {
//     private static readonly Dictionary<string, Type> _typeMap = new()
//     {
//         ["StartQuest"]       = typeof(StartQuestAction),
//         ["OpenStore"]        = typeof(OpenStoreAction),
//         ["AdjustFriendship"] = typeof(AdjustFriendshipAction)
//         // add more here as you create new action classes
//     };

//     public override TBase Read(ref Utf8JsonReader reader,
//                                Type typeToConvert,
//                                JsonSerializerOptions options)
//     {
//         using var doc  = JsonDocument.ParseValue(ref reader);
//         var root       = doc.RootElement;

//         if (!root.TryGetProperty("type", out var typeProp))
//             throw new JsonException("Action missing required 'type' field.");

//         var typeKey = typeProp.GetString();
//         if (typeKey is null || !_typeMap.TryGetValue(typeKey, out var targetType))
//             throw new JsonException($"Unknown action type '{typeKey}'.");

//         // Deserialize into the **concrete** type, then cast to TBase
//         var json      = root.GetRawText();
//         var instance  = (TBase)JsonSerializer.Deserialize(json, targetType, options)!;
//         return instance;
//     }

//     public override void Write(Utf8JsonWriter writer,
//                                TBase value,
//                                JsonSerializerOptions options)
//         => JsonSerializer.Serialize(writer, value, value.GetType(), options);
// }