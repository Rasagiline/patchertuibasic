using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Loadout_Patcher_Enhanced
{
    public class FileSave
    {
        public class SaveFileInfo
        {
            public List<string> WebApiEndpoints { get; set; }
        }

        //public static string Endpoint { get; set; }

        public static void SaveFile(SaveFileInfo saveFile)
        {
            try
            {
                // Serialize the object and save to file
                File.WriteAllText("saveFile.json", JsonSerializer.Serialize(saveFile));
                AnsiConsole.MarkupLine($"[bold][white]> File saved successfully.[/][/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold][white]> An error occurred: {ex.Message}[/][/]");
            }
        }

        public static void LoadFile()
        {
            try
            {
                // Read JSON content from file
                string json = File.ReadAllText("saveFile.json");

                // Deserialize JSON string to SaveFileInfo object
                SaveFileInfo saveFileInfo = JsonSerializer.Deserialize<SaveFileInfo>(json);

                Processing.WebApiEndpoints = saveFileInfo.WebApiEndpoints;

                // Output the deserialized object
                AnsiConsole.MarkupLine($"[bold][white]> Endpoint: {saveFileInfo.WebApiEndpoints[0]}[/][/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[bold][white]> An error occurred: {ex.Message}[/][/]");
            }
        }
    }
}
