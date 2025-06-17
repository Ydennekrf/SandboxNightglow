using System.IO;
using System.Text.Json;
using Godot;

public class JsonDialogueProvider : IDialogueProvider
{
    private readonly JsonSerializerOptions _opts;

    public JsonDialogueProvider()
    {
        _opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new PolymorphicActionConverter<NodeAction>(),
                new PolymorphicActionConverter<ChoiceAction>()
            }
        };
    }

    public DialogueGraph LoadGraph(string graphId)
    {
        var path = $"res://Data/Dialog/{graphId}.json";
           var absPath = ProjectSettings.GlobalizePath(path);

        string json;
        using (var fa = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read))
        json = fa.GetAsText();                                  // Godot‑native read

        return JsonSerializer.Deserialize<DialogueGraph>(json, _opts);
    }
}