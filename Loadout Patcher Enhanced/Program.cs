using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static System.Net.WebRequestMethods;

namespace Loadout_Patcher_Enhanced
{
    public class Program
    {
        static void Main(string[] args)
        {
            string newEndpoint = "";
            string newMap = "";
            bool breakOnFreshStart = false;

            string readMemoryEndpointString = ""; // neutral naming (uberent, ues, or matchmaking)
            string? readMemoryUberentString;
            string readMemoryUesString = "";
            string readMemoryMatchmakingString = "";
            string readMemoryMapString = "";

            string defaultMap = "shooting_gallery_solo";

            string? matchingAliasMap;
            string? matchingAliasGameMode;

            Map.LoadoutMap startingMap = new Map.LoadoutMap
            {
                Id = "1511501517",
                FullMapName = "shooting_gallery_solo",
                FullMapNameAlt = "Shooting_Gallery_Solo",
                BaseMap = "shooting_gallery_solo",
                DayNight = "day",
                GameMode = "solo",
            };

            bool patcherReset;
            bool patched;

            string discordInviteLink = "https://discord.gg/QQMzZt9";

        // TODO: Remove the console settings
#pragma warning disable CA1416 // Validate platform compatibility
        Console.WindowWidth = 200;
            Console.WindowHeight = 60;
#pragma warning restore CA1416 // Validate platform compatibility
            Console.Title = "Loadout Reloaded Patcher Enhanced";

            // Save a file
            var saveFile = new FileSave.SaveFileInfo();
            saveFile.WebApiEndpoints = new List<string>(0) { "api.loadout.rip" };
            FileSave.SaveFile(saveFile);

            // Create a canvas
            var canvas = new Canvas(100,2);

            ColorIn(canvas);

            // Create a Text object with bold and centered text
            var text = new Markup("[bold][white]>>>>>>>>>>>>>>>>>>>>>>>>>>> Loadout Reloaded Patcher <<<<<<<<<<<<<<<<<<<<<<<<<<<[/][/]");
            // Center the text in the terminal window
            text.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text);

            var text2 = new Markup("[bold][white]>>>>>>>>>>>>>>>>>>>>>> Made by: Rasagiline (Reloaded Team) <<<<<<<<<<<<<<<<<<<<<[/][/]");
            // Center the text in the terminal window
            text2.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text2);

            ColorOut(canvas);

            AnsiConsole.MarkupLine("[bold][white]> Reading the endpoint ...[/][/]");

            // Load a file
            FileSave.LoadFile();

            // We use the endpoint in string[0]
            newEndpoint = Processing.WebApiEndpoints[0];

            /* Wait for the Loadout.exe process and continue once we get access */
            AnsiConsole.MarkupLine("[bold][white]> Waiting for Loadout.exe ... (Loadout Beta is not supported)[/][/]");

            while (true)
            {
                Processing.FetchProcesses("Loadout.exe");
                if (Processing.LoadoutProcess != null && Processing.LoadoutParentProcess != null)
                {
                    if (Processing.LoadoutProcess.ProcessName == "Loadout")
                    {
                        // Console.WriteLine("> " + loadoutStandardProcess + " " + loadoutParentProcess);
                        if (Processing.LoadoutParentProcess.ProcessName == "SmartSteamLoader")
                        {
                            // sseUser = true;
                            // Console.WriteLine("> The SmartSteamEmu Launcher is being used on Loadout.");
                        }
                        // This can be used to find out if Loadout has just been started or has been started some time ago
                        // Subtraction minimizes the chance of time manipulation
                        //uptimeInSec = DateTime.Now.Subtract(loadoutStandardProcess.StartTime).TotalSeconds;
                        Processing.UptimeInSec = Processing.LoadoutProcess.TotalProcessorTime.TotalSeconds;
                        break;
                    }
                }
                else { breakOnFreshStart = true; }
                Thread.Sleep(10);
            }

            ColorIn(canvas);

            var text3 = new Markup("[bold][white]-------------------------------> Loadout found! <-------------------------------[/][/]");
            // Center the text in the terminal window
            text3.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text3);

            ColorOut(canvas);

            /* Check and if necessary wait to make sure that we can enter the game and load common maps safely */
            AnsiConsole.MarkupLine("[bold][white]> Waiting for endpoints to be initialized ...[/][/]");

            for (int i = 30; i < 1600; i++)
            {
                /* Speed ranking (may vary): 1. [mapAddress]: 1470 ms, 2. [uesAddress]: 1550 ms, 4. [matchmakingAddress]: 2660 ms, 4. [uberentAddress]: 2660 ms
                     first start (may vary): 1. [mapAddress]: 1560 ms, 2. [uesAddress]: 1760 ms, 4. [matchmakingAddress]: 3830 ms, 4. [uberentAddress]: 3830 ms
                     longest duration measured: 4880 ms */
                readMemoryUberentString = Processing.CheckStringAtOffset(Processing.LoadoutProcess,
                    Processing.BasicEndpoints[0].Value, Processing.BasicEndpoints[0].Key);
                /* This is a chain reaction to keep checks at a minimum and receive no more than 1 (identical) error message in case of an error */
                if (readMemoryUberentString.Length > 0)
                {
                    readMemoryUesString = Processing.CheckStringAtOffset(Processing.LoadoutProcess,
                        Processing.BasicEndpoints[1].Value, Processing.BasicEndpoints[1].Key);

                    if (readMemoryUesString.Length > 0)
                    {
                        readMemoryMatchmakingString = Processing.CheckStringAtOffset(Processing.LoadoutProcess,
                            Processing.BasicEndpoints[2].Value, Processing.BasicEndpoints[2].Key);

                        if (readMemoryMatchmakingString.Length > 0)
                        {
                            readMemoryMapString = Processing.CheckStringAtOffset(Processing.LoadoutProcess,
                                Processing.MapAddress, defaultMap);
                        }
                    }
                }

                /* If all memory was successfully read, we move on */
                if (readMemoryUberentString.Length > 0 && readMemoryMatchmakingString.Length > 0 && readMemoryUesString.Length > 0 && readMemoryMapString.Length > 0)
                {
                    AnsiConsole.MarkupLine("[bold][white]> (Try " + Math.Ceiling((double)i / 400) + ") Endpoints are now initialized.[/][/]");
                    /* Output of each read memory string */
                    AnsiConsole.MarkupLine("[bold][white]> (" + readMemoryUberentString + ") (" + readMemoryUesString + ") (" + readMemoryMatchmakingString + ") (" + readMemoryMapString + ")[/][/]");
                    break;
                }
                else
                {
                    /* If Loadout has been started between 1470 ms and 3830 ms before the patcher was opened, we wait */
                    if (!String.IsNullOrEmpty(readMemoryUberentString) || !String.IsNullOrEmpty(readMemoryMatchmakingString) ||
                        !String.IsNullOrEmpty(readMemoryUesString) || !String.IsNullOrEmpty(readMemoryMapString))
                    {
                        /* Extra check for error codes */
                        if (Processing.GetLastErrorOfProcessMemory())
                        {
                            patcherReset = true;
                            patched = false;
                        }
                        else if (i < 35 && Processing.UptimeInSec < 5) { Task.Delay(2800); }
                        breakOnFreshStart = true;

                        /* If Loadout gets started after the patcher and the checks for initialized endpoints failed a few times, we wait */
                    }
                    else if (breakOnFreshStart = true && i == 34 && Processing.UptimeInSec < 5) { Task.Delay(3200); }
                    /* Waiting is critical here. We accelerate if necessary */
                    Task.Delay(1 + 19 / (i / 30));
                }
                if (i % 400 == 0) { AnsiConsole.MarkupLine("[bold][white]> (Try " + i / 400 + ") Endpoints are not initialized. This can happen on slow machines.[/][/]"); }
                /* After a good break, we secretly try it one more time and let the user continue in case the results were "false negative" */
                if (i == 1598)
                {
                    AnsiConsole.MarkupLine("[bold][white]> Endpoints are still not initialized! Continuing with risk ...[/][/]");
                    Task.Delay(2600);
                }
            }

            /* Let's patch the endpoints */

            /* patching the basic endpoints [uberentEndpoint], [uesEndpoint] and [matchmakingEndpoint] to be able to play the game */
            foreach (KeyValuePair<string, int> endpointToOverwriteAndAddress in Processing.BasicEndpoints)
            {
                readMemoryEndpointString = Processing.OverwriteStringAtOffset(Processing.LoadoutProcess, endpointToOverwriteAndAddress.Value,
                    endpointToOverwriteAndAddress.Key, newEndpoint);
                if (Processing.GetLastErrorOfProcessMemory())
                {
                    patcherReset = true;
                    patched = false;
                }
            }

            //Map.SetMapWithInteractions(true); // NoPickupsCheck can't be used here
            // Needed for the Map.MapOrMapAltDecider() method
            Map.SetStartingMap(startingMap);
            // && Map.MapOrMapAltDecider() != MainProperties.StartingMap.FullMapName
            /* We patch the default map with the starting map that was found in the save file. If not, we attempt to patch defaultMapReadMemory at least */
            if (!String.IsNullOrEmpty(Map.MapOrMapAltDecider()))
            {
                /* patching [DefaultMap] */
                /*
                MainProperties.ReadMemoryMapString = ProcessMemory.OverwriteStringAtOffset(ProcessHandling.LoadoutProcess, ProcessMemory.MapAddress, MainProperties.DefaultMap, Map.MapOrMapAltDecider(), true);
                if (ProcessMemory.GetLastErrorOfProcessMemory())
                {
                    MainProperties.PatcherReset = true;
                    MainProperties.Patched = false;
                }
                */

                // TODO: Influence mapWithHealthPickups in Map.cs

                /* patching [StartingMap] */
                readMemoryMapString = Processing.OverwriteStringAtOffset(Processing.LoadoutProcess, Processing.MapAddress, defaultMap, Map.MapOrMapAltDecider(), true);
                if (Processing.GetLastErrorOfProcessMemory())
                {
                    patcherReset = true;
                    patched = false;
                }

                ColorIn(canvas);

                var text4 = new Markup("[bold][white]-----------------------------> Map patching done! <-----------------------------[/][/]");
                // Center the text in the terminal window
                text4.Justification = Justify.Center;
                // Render the text to the console
                AnsiConsole.Write(text4);

                ColorOut(canvas);

                /* We fetch aliases from existing hashtables. */
                matchingAliasMap = Map.FetchMatchingAliasMap(startingMap.BaseMap);
                matchingAliasGameMode = Map.FetchMatchingAliasGameMode(startingMap.GameMode);

                AnsiConsole.MarkupLine("[bold][white]-> The following map has been set automatically.[/][/]");
                AnsiConsole.MarkupLine("[bold][white]-> Change the starting map if you experience errors.[/][/]");
                // This is the only place where MainProperties.DefaultMap must still be used!
                AnsiConsole.MarkupLine("[bold][white]-> The safest map is called: {0}[/][/]", defaultMap);
                AnsiConsole.MarkupLine("[bold][white]-> (= Complete) Starting map: {0}[/][/]", Map.MapOrMapAltDecider());
                if (!String.IsNullOrEmpty(matchingAliasMap)) { AnsiConsole.MarkupLine("[bold][white]-> (= Complete) Map known as: {0}[/][/]", matchingAliasMap); }
                if (!String.IsNullOrEmpty(matchingAliasGameMode)) { AnsiConsole.MarkupLine("[bold][white]-> (= Complete) Game mode is: {0}[/][/]", matchingAliasGameMode); }

                /* We make sure the starting map will be treated as the new map */
                newMap = Map.MapOrMapAltDecider();
            }
            else
            {
                /* patching [defaultMapReadMemory] */
                /* shooting_gallery_solo could give more options than Shooting_Gallery_Solo, allowing interactions */
                /* but shooting_gallery_solo and Shooting_Gallery_Solo are equal because it generally offers no interactions */
                readMemoryMapString = Processing.OverwriteStringAtOffset(Processing.LoadoutProcess, Processing.MapAddress, Processing.DefaultMapReadMemory, defaultMap, true);
                if (Processing.GetLastErrorOfProcessMemory())
                {
                    patcherReset = true;
                    patched = false;
                }
            }

            ColorIn(canvas);

            var text5 = new Markup("[bold][white]---------------------------> Endpoint patching done! <---------------------------[/][/]");
            // Center the text in the terminal window
            text5.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text5);
            patcherReset = false;

            var text6 = new Markup("[bold][white]-----------------------------> You are ready to go! <----------------------------[/][/]");
            // Center the text in the terminal window
            text6.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text6);
            patcherReset = false;

            ColorOut(canvas);
            ColorIn(canvas);

            var text7 = new Markup($"[bold][white]-------------> Join our Discord server: [link={discordInviteLink}]{discordInviteLink}[/] <-------------[/][/]");
            // Center the text in the terminal window
            text7.Justification = Justify.Center;
            // Render the text to the console
            AnsiConsole.Write(text7);

            ColorOut(canvas);















            Console.ReadKey();
        }

        public static void ColorIn(Canvas canvas)
        {
            // Draw some shapes
            for (var i = 0; i < canvas.Width; i++)
            {
                // Cross
                canvas.SetPixel(canvas.Width - i - 1, 0, Spectre.Console.Color.DarkSeaGreen3_1);
                canvas.SetPixel(canvas.Width - i - 1, 1, Spectre.Console.Color.DarkOliveGreen3);
            }

            // Render the canvas
            AnsiConsole.Write(canvas);
        }

        public static void ColorOut(Canvas canvas)
        {
            // Draw some shapes
            for (var i = 0; i < canvas.Width; i++)
            {
                // Cross
                canvas.SetPixel(canvas.Width - i - 1, 0, Spectre.Console.Color.DarkOliveGreen3);
                canvas.SetPixel(canvas.Width - i - 1, 1, Spectre.Console.Color.DarkSeaGreen3_1);
            }

            // Render the canvas
            AnsiConsole.Write(canvas);
        }
    }
}


















