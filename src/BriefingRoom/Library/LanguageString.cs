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

using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS
{
    public class LanguageString : Dictionary<string, string>
    {
        public LanguageString() { }

        public LanguageString(DatabaseLanguage langDB, string classId, string id, string key, Dictionary<string, string> dict)
        {
            foreach (var item in dict)
            {
                this.Add(item.Key, item.Value);
            }
            this.AddOverrides(langDB, classId, id, key);
        }
        public LanguageString(string value)
        {
            this.Add("en", value);
        }

        public LanguageString(DatabaseLanguage langDB, string classId, string id, string key, string defaultValue)
        {
            this.Add("en", defaultValue);
            this.AddOverrides(langDB, classId, id, key);
        }

        internal void AddOverrides(DatabaseLanguage langDB, string classId, string id, string key)
        {
            var searchKey = $"{classId}.{id}.{key}".ToUpper();
            if (langDB.LangMap.ContainsKey(searchKey))
                langDB.LangMap.GetValueOrDefault(searchKey).ToList().ForEach(x => this.AddIfKeyUnused(x.Key, x.Value));
        }

        internal void AddOverrides(DatabaseLanguage langDB, string classId, string id, string section, string key)
        {
            var searchKey = $"{classId}.{id}.{section}.{key}".ToUpper();
            if (langDB.LangMap.ContainsKey(searchKey))
                langDB.LangMap.GetValueOrDefault(searchKey).ToList().ForEach(x => this.AddIfKeyUnused(x.Key, x.Value));
        }
        public string Get(string langKey)
        {
            if (this.ContainsKey(langKey)) return this[langKey];
            if (this.ContainsKey("en")) return this["en"];
            return $"";
        }

        public Dictionary<string, string> ToDictionary()
        {
            return this;
        }
    }
}
