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
using System.IO;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryStatic : DBEntryJSONUnit
    {

        internal string ShapeName { get; init; }
        internal int ParkingSpots { get; init; }

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<Static>>(File.ReadAllText(filepath));
            var supportData = JsonConvert.DeserializeObject<List<BRInfo>>(File.ReadAllText($"{filepath.Replace(".json", "")}BRInfo.json")).ToDictionary(x => x.type, x => x);

            foreach (var @static in data)
            {
                var id = @static.type;
                if (!supportData.ContainsKey(id))
                {
                    BriefingRoom.PrintToLog($"Static missing {id} in info data", LogMessageErrorLevel.Warning);
                    continue;
                }
                var supportInfo = supportData[id];
                itemMap.Add(id, new DBEntryStatic
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryStatic)), id, "displayName", @static.displayName),
                    DCSID = @static.type,
                    Operators = new Dictionary<Country, (Template.Decade start, Template.Decade end)> { { Country.ALL, (Template.Decade.Decade1940, Template.Decade.Decade2020) } },
                    ShapeName = @static.shapeName,
                    Families = supportInfo.families.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToArray(),
                    LowPolly = supportInfo.lowPolly,
                    Module = @static.module,
                    ParkingSpots = @static.numParking
                });
            }

            missingDCSDataWarnings(supportData, itemMap, "Static");

            return itemMap;
        }

        public DBEntryStatic() { }
    }
}
