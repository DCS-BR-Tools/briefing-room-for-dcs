/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;

namespace BriefingRoom4DCS.Data
{
    public class DatabaseLanguage
    {
        public Dictionary<string, LanguageString> LangMap { get; private set; }

        public DatabaseLanguage() { }

        public void Load()
        {
            LangMap = new Dictionary<string, LanguageString>();
            BriefingRoom.PrintToLog("Loading language global settings...");
            string directory = Path.Combine(BRPaths.DATABASE, "Language");
            if (!Directory.Exists(directory))
                throw new Exception($"Directory {directory} not found.");

            foreach (string filePath in Directory.EnumerateFiles(directory, "*.ini", SearchOption.AllDirectories))
            {
                string id = Path.GetFileNameWithoutExtension(filePath).Replace(",", "").Trim(); // No commas in file names, so we don't break comma-separated arrays


                LoadValues(filePath);
                BriefingRoom.PrintToLog($"Loaded {filePath.Replace(BRPaths.DATABASE, "")} \"{id}\"");
            }
        }

        private void LoadValues(string filePath)
        {
            var ini = new INIFile(filePath);
            var parts = Path.GetFileNameWithoutExtension(filePath).Split('.');
            var lang = parts.Length > 0 ? parts[0].ToLower() : "en";
            foreach (var section in ini.GetSections())
            {
                foreach (var key in ini.GetKeysInSection(section, true))
                {
                    var upperKey = key.ToUpper();
                    if (LangMap.ContainsKey(upperKey))
                    {
                         LangMap[upperKey] = ini.AddLangStrings(section, key, LangMap[upperKey], lang);
                        continue;
                    }
                    LangMap.Add(upperKey, ini.GetLangStrings(section, key, lang));
                }
            }
        }

        public string ReplaceValues(string langKey, string rawText)
        {
            if (rawText == null) return null;

            foreach (KeyValuePair<string, LanguageString> keyPair in LangMap)
                rawText = rawText.Replace($"$LANG_{keyPair.Key.ToUpper()}$", keyPair.Value.Get(langKey));

            return rawText;
        }

        public string Translate(string langKey, string key)
        {
            var searchKey = key.ToUpper();
            if (!LangMap.ContainsKey(searchKey))
            {
                Console.WriteLine($"ERR_Missing_Translation_key:{langKey}:{key} (map size: {LangMap.Keys.Count})");
                return $"ERR_Missing_Translation_key:{langKey}:{key}";
            }
            return LangMap[searchKey].Get(langKey);
        }

    }
}