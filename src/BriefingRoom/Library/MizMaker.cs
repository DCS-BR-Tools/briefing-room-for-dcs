/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BriefingRoom4DCS
{
    internal partial class MizMaker
    {

        internal static byte[] ExportToMizBytes(IDatabase database, DCSMission mission, MissionTemplate template)
        {
            Dictionary<string, byte[]> MizFileEntries = new();
            mission.SetValue("TrigScriptRules", ""); // Reset these so we can append to them properly.

            AddStringValueToEntries(MizFileEntries, $"{BRPaths.MIZ_SCRIPTS}briefing.html", mission.Briefing.GetBriefingAsHTML(database, mission, true));
            mission.AppendValue("MapResourcesFiles", $"[\"ResKey_Snd_briefing_html\"] = \"briefing.html\",\n");

            AddStringValueToEntries(MizFileEntries, $"{BRPaths.MIZ_SCRIPTS}credits.txt", $"Generated with BriefingRoom for DCS World (https://akaagar.itch.io/briefing-room-for-dcs) {BriefingRoom.VERSION} ({BriefingRoom.BUILD_VERSION})");
            mission.AppendValue("MapResourcesFiles", $"[\"ResKey_Snd_credits_txt\"] = \"credits.txt\",\n");

            AddStringValueToEntries(MizFileEntries, "BR_purity_seal.txt", "The Omnissiah has blessed this Miz!\nFor it has not been corrupted by the forces of ED\n :D");
            if (template != null)
            {
                AddStringValueToEntries(MizFileEntries, $"{BRPaths.MIZ_SCRIPTS}template.brt", Encoding.ASCII.GetString(template.GetIniBytes()));
                mission.AppendValue("MapResourcesFiles", $"[\"ResKey_Snd_template\"] = \"template.brt\",\n");
            }
            AddLuaFileToEntries(database, MizFileEntries, "options", "Options.lua", null);
            AddStringValueToEntries(MizFileEntries, "theatre", mission.GetValue("TheaterID"));
            AddLuaFileToEntries(database, MizFileEntries, "warehouses", "Warehouses.lua", mission);

            AddLuaFileToEntries(database, MizFileEntries, $"{BRPaths.MIZ_SCRIPTS}dictionary", "Dictionary.lua", mission);

            // Standard scripting components
            new List<string> {
            "MIST.lua",
            "LuaExtensions.lua",
            "DCSExtensions.lua",
            "Init.lua",
            "RadioManager.lua",
            "AircraftActivator.lua",
            "TransportManager.lua",
            "EventHandler.lua",
            "MissionInit.lua",
            "F10Menu.lua",
            "ObjectiveTables.lua",
            "MissionTriggers.lua",
            "ObjectiveFeatures.lua",
            "MissionFeatures.lua",
            }.ForEach(fileName =>
            {
                AddScriptFileToEntries(database, MizFileEntries, fileName, $"Mission/Script/{fileName}", mission);
            });

            Directory.EnumerateFiles(Path.Combine(BRPaths.INCLUDE_LUA, "Mission/Script/MissionTriggers")).ToList().ForEach(fileName =>
            {
                var fileNameOnly = Path.GetFileName(fileName);
                AddScriptFileToEntries(database, MizFileEntries, $"triggers/{fileNameOnly}", $"Mission/Script/MissionTriggers/{fileNameOnly}", mission);
            });

            Directory.EnumerateFiles(Path.Combine(BRPaths.INCLUDE_LUA, "Mission/Script/ObjectiveFeatures")).ToList().ForEach(fileName =>
            {
                var fileNameOnly = Path.GetFileName(fileName);
                AddScriptFileToEntries(database, MizFileEntries, $"objectiveFeatures/{fileNameOnly}", $"Mission/Script/ObjectiveFeatures/{fileNameOnly}", mission);
            });

            mission.ScriptFileSet.ToList().ForEach(fileName =>
            {
                var fileNameOnly = Path.GetFileName(fileName);
                AddScriptFileToEntries(database, MizFileEntries,  $"missionFeatures/{fileNameOnly}", $"MissionFeatures/{fileNameOnly}", mission);
            });

            new List<string> {
            "MissionTriggersInit.lua",
            "ObjectiveFeaturesInit.lua",
            "Start.lua",
            }.ForEach(fileName =>
            {
                AddScriptFileToEntries(database, MizFileEntries, fileName, $"Mission/Script/{fileName}", mission);
            });

            // TODO: Extra ones as needed
            // AddLuaFileToEntries(database, MizFileEntries, "{sourceFileName}.lua", sourceFile, mission);

            // After all scripts
            AddLuaFileToEntries(database, MizFileEntries, $"{BRPaths.MIZ_SCRIPTS}mapResource", "MapResource.lua", mission);
            AddLuaFileToEntries(database, MizFileEntries, "mission", "Mission.lua", mission);


            foreach (string mediaFile in mission.GetMediaFileNames())
            {
                byte[] fileBytes = mission.GetMediaFile(mediaFile);
                if (fileBytes == null) continue;
                MizFileEntries.Add(mediaFile, fileBytes);
            }

            return Toolbox.ZipData(database, mission.LangKey, MizFileEntries);
        }

        private static bool AddScriptFileToEntries(IDatabase database, Dictionary<string, byte[]> mizFileEntries, string mizPath, string originPath, DCSMission mission = null)
        {
            if (AddLuaFileToEntries(database, mizFileEntries, $"{BRPaths.MIZ_SCRIPTS}{mizPath}", originPath, mission))
            {
                var resKey = $"ResKey_Script_{Path.GetFileNameWithoutExtension(mizPath)}";
                mission.AppendValue("MapResourcesFiles", $"[\"{resKey}\"] = \"{mizPath}\",\n");
                mission.AppendValue("TrigScriptActions", $"a_do_script_file(getValueResourceByKey(\"{resKey}\"));\n");
                var rulesIndex = Regex.Matches(mission.GetValue("TrigScriptRules"), "predicate").Count + 1;
                mission.AppendValue("TrigScriptRules", $@"
                [{rulesIndex}] = 
                {{
                    [""file""] = ""{resKey}"",
                    [""predicate""] = ""a_do_script_file"",
                }},");

                return true;
            }
            return false;
        }

        private static bool AddLuaFileToEntries(IDatabase database, Dictionary<string, byte[]> mizFileEntries, string mizEntryKey, string sourceFile, DCSMission mission = null)
        {
            if (string.IsNullOrEmpty(mizEntryKey) || mizFileEntries.ContainsKey(mizEntryKey) || string.IsNullOrEmpty(sourceFile)) return false;
            sourceFile = Path.Combine(BRPaths.INCLUDE_LUA, sourceFile);
            if (!File.Exists(sourceFile)) return false;

            string luaContent = File.ReadAllText(sourceFile);
            if (mission != null) // A mission was provided, do the required replacements in the file.
                luaContent = mission.ReplaceValues(luaContent);
            luaContent = database.Language.ReplaceValues(mission != null ? mission.LangKey : "en", luaContent);
            luaContent = UnassignedRegex().Replace(luaContent, "0");
            mizFileEntries.Add(mizEntryKey, Encoding.UTF8.GetBytes(luaContent));
            return true;
        }

        private static bool AddStringValueToEntries(Dictionary<string, byte[]> mizFileEntries, string mizEntryKey, string stringValue)
        {
            if (string.IsNullOrEmpty(mizEntryKey) || mizFileEntries.ContainsKey(mizEntryKey)) return false;
            mizFileEntries.Add(mizEntryKey, Encoding.UTF8.GetBytes(stringValue));
            return true;
        }

        [GeneratedRegex("\\$\\w*?\\$")]
        private static partial Regex UnassignedRegex();
    }
}