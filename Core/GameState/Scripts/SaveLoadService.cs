

using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using System;
using System.IO;
using System.Windows.Markup;
using System.Collections.Generic;

namespace ethra.V1
{
    public class SaveLoadService : ISaveLoadService
    {
        //=========== FIelds and Properties ==============//

        private ISaveRegistry _savLod;


        //constructor
        public SaveLoadService(ISaveRegistry svld) => _savLod = svld;
        
         private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true
        };

        public async Task LoadGameAsync(int id)
        {
            
            string path = GetSavePath(id);
            var absPath = ProjectSettings.GlobalizePath(path);
            if (!File.Exists(absPath))
            {
                GD.PrintErr($"[Load] Missing save: {absPath}");
                
            }
            string json = File.ReadAllText(absPath);
            SaveGame save = JsonSerializer.Deserialize<SaveGame>(json)!;
            Restore(save);
        }

        /// <summary>
        /// gets the save slot ID by returning the max number value for the file signiture save_{val} return val
        /// </summary>
        /// <returns></returns>
        public int GetNextSaveSlot()
        {
                EnsureSaveFolder();

                var savesAbs = ProjectSettings.GlobalizePath("user://saves");
                if (!Directory.Exists(savesAbs))
                    return 1;

                var files = Directory.GetFiles(savesAbs, "save_*.json");

                int max = 0;
                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f); // save_#
                    var parts = name.Split('_');

                    if (parts.Length == 2 && int.TryParse(parts[1], out int id))
                        max = Math.Max(max, id);
                }

                return max + 1;
        }


        /// <summary>
        /// pass the id of the save file to the service to 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task SaveGameAsync(int id)
        {
            EnsureSaveFolder();
            string path = GetSavePath(id);

            




        }
        
        private void Restore(SaveGame save)
        {
            foreach (ISaveable s in _savLod.All)
            {
                object dto = s.SaveKey switch
                {
                    "Combat" => save.combat,
                    "GameState" => save.gameState,
                    "Inventory" => save.inventory,
                    "Player" => save.player,
                    _ => null
                };

                if(dto != null)
                {
                    s.RestoreSnapshot(dto);
                }
            }
        }

           private static void EnsureSaveFolder()
        {
            var savesAbs = ProjectSettings.GlobalizePath("user://saves");
            DirAccess.MakeDirRecursiveAbsolute(savesAbs);
        }

            private static string GetSavePath(int slotId) => $"user://saves/save_{slotId}.json";

    }
}