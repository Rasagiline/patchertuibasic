using System.Collections;

namespace Loadout_Patcher_Enhanced
{
    public class Map
    {
        public struct LoadoutMap
        {
            public string Id;
            public string FullMapName;
            public string FullMapNameAlt;
            public string BaseMap;
            public string DayNight;
            public string GameMode;
        }

        private static LoadoutMap startingMap;

        public static void SetStartingMap(LoadoutMap map)
        {
            startingMap = map;
        }

        public static LoadoutMap GetStartingMap()
        {
            return startingMap;
        }

        public static string MapOrMapAltDecider()
        {
            bool mapAltOrMap = GetMapWithoutInteractions();
            string chosenMap;

            if (mapAltOrMap)
            {
                chosenMap = GetStartingMap().FullMapNameAlt;
            }
            else
            {
                chosenMap = GetStartingMap().FullMapName;
            }
            return chosenMap;
        }

        private static bool mapWithoutInteractions = false;

        public static bool GetMapWithoutInteractions()
        {
            return mapWithoutInteractions;
        }

        public static string FetchMatchingAliasMap(string baseMap)
        {
            string? matchingAliasMap;
            Hashtable mapNames = Map.GetBaseMapsAndAliases();
            Hashtable mapNamesNight = Map.GetBaseMapsAndAliasesNight();

            if (mapNamesNight.ContainsKey(baseMap) == true)
            {
                matchingAliasMap = (string)mapNamesNight[baseMap]!;
            }
            else if (mapNames.ContainsKey(baseMap) == true)
            {
                matchingAliasMap = (string)mapNames[baseMap]!;
            }
            else
            {
                matchingAliasMap = "";
            }

            return matchingAliasMap!;
        }

        public static Hashtable GetBaseMapsAndAliases()
        {
            return baseMapsAndAliases;
        }

        // Display-names
        private readonly static Hashtable baseMapsAndAliases = new Hashtable()
        {
            {"drillcavern", "Drill Cavern"},
            {"drillcavern_beta_kc", "Drill Cavern (Beta)"},
            {"fath_705", "Four Points"},
            {"fissure", "Fissure"},
            {"gliese_581", "Trailer Park"},
            {"greenroom", "Greenroom"},
            {"level_three", "The Brewery"},
            {"locomotiongym", "LocomotionGym"},
            {"projectx", "Project X"},
            {"shattered", "Shattered"},
            {"shooting_gallery_solo", "Shooting Gallery"},
            {"spires", "Spires"},
            {"sploded", "Sploded (Alpha)"},
            {"test_territorycontrol", "Test"},
            {"thefreezer", "The Freezer"},
            {"thepit_pj", "The Pit"},
            {"tower", "CommTower"},
            {"trailerpark_agt", "Trailer Park (Ranked)"},
            {"truckstop2", "Shipping Yard (Alpha)"},
            {"two_port", "Two Ports (Alpha)"}
        };

        private readonly static Hashtable baseMapsAndAliasesNight = new Hashtable()
        {
            {"drillcavern_night", "Drill Cavern at Night"},
            {"fissurenight", "Fissure at Night"},
            {"gliese_581_night", "Trailer Park at Night"}
        };

        public static Hashtable GetBaseMapsAndAliasesNight()
        {
            return baseMapsAndAliasesNight;
        }

        public static string FetchMatchingAliasGameMode(string gameMode)
        {
            string? matchingAliasGameMode;
            Hashtable gameModeTranslations = Map.GetMapSuffixesAndAliases();

            if (gameModeTranslations.ContainsKey(gameMode) == true)
            {
                matchingAliasGameMode = (string)gameModeTranslations[gameMode]!;
            }
            else
            {
                matchingAliasGameMode = "";
            }

            return matchingAliasGameMode!;
        }

        private readonly static Hashtable mapSuffixesAndAliases = new Hashtable()
        {
            {"art_master", "None"},
            {"botwave", "Hold Your Pole"},
            {"botwaves", "Hold Your Pole"},
            {"cpr", "Blitz"},
            {"cpr_bots", "Special variant of Blitz"},
            {"ctf", "Jackhammer"},
            {"ctp", "Extraction (Alpha) - test stage"},
            {"domination", "Domination"},
            {"geo", "None"},
            {"kc", "Deathsnatch"},
            {"kc_bots", "Special variant of Deathsnatch"},
            {"mashup", "Annihilation (Alpha) - test stage"},
            {"mu", "Annihilation"},
            {"none", "None"},
            {"pj", "Unknown"},
            {"rr", "Extraction"},
            {"solo", "Solo"},
            {"tc", "Unknown"},
            {"tdm", "Deathsnatch (Alpha) - test stage"},
            {"territorycontrol", "Unknown"}
        };

        public static Hashtable GetMapSuffixesAndAliases()
        {
            return mapSuffixesAndAliases;
        }

    }
}
