using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Godot;
using System.Linq;

public static class DialogueRepository
{
    private static string DbPath => ProjectSettings.GlobalizePath("res://Data/DB/GameData.db");

    /* Load the entire dialogue tree for one NPC. */
    public static DialogueGraph GetGraph(string npcId)
    {
        var nodes   = new Dictionary<string, DialogNodeRecord>();
        var choices = new Dictionary<string, List<ChoiceRecord>>();

        using var c = new SqliteConnection($"Data Source={DbPath};Mode=ReadOnly");
        c.Open();

        /* 1. pull nodes */
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM DialogNode WHERE npc_id=$n;";
            cmd.Parameters.AddWithValue("$n", npcId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                nodes[r.GetString(0)] = new DialogNodeRecord(
                    r.GetString(0), r.GetString(1), r.GetString(2),
                    r.GetString(3), r.GetString(4), new List<ChoiceRecord>(), new List<ActionRecord>());
                    
        }

        /* 2. choices + actions (single JOIN)  */
        const string sql = """
        SELECT c.choice_id,c.node_id,c.sort_index,c.text,c.next_node_id,
               a.action_id,a.type,a.json_data
        FROM Choice c
        LEFT JOIN ChoiceAction ca ON ca.choice_id=c.choice_id
        LEFT JOIN Action a        ON ca.action_id=a.action_id
        WHERE c.node_id IN (SELECT node_id FROM DialogNode WHERE npc_id=$n)
        ORDER BY c.sort_index;
        """;
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$n", npcId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                /* make / get choice list */
                if (!nodes.TryGetValue(r.GetString(1), out var node)) continue;
                var chList = node.Choices;

                /* first time for this choice? */
                if ( !node.Choices.Any(ch => ch.ChoiceId == r.GetString(0)) )
                    {
                        node.Choices.Add(new ChoiceRecord(
                            r.GetString(0),                 // choice_id
                            r.GetString(1),                 // node_id
                            r.GetInt32(2),                  // sort_index
                            r.GetString(3),                 // text
                            r.IsDBNull(4) ? null : r.GetString(4),  // next_node_id
                            new List<ActionRecord>()        // empty action list
                        ));
                    }

                /* attach action (nullable) */
                if (!r.IsDBNull(5))
                    chList[^1].Actions.Add(new ActionRecord(
                        r.GetInt32(5),
                        (ActionType)r.GetInt32(6),
                        r.GetString(7)));
            }
        }
        return new DialogueGraph { Id = npcId, Nodes = new List<DialogNodeRecord>(nodes.Values) };
    }
}
