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
    internal class DBEntryCar : DBEntryJSONUnit
    {

        internal string DCSCategory { get; init; }

        protected override bool OnLoad(string iniFilePath)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = new List<Car>();
            var text = File.ReadAllText(filepath);
            try
            {
                data = JsonConvert.DeserializeObject<List<Car>>(text);
            }
            catch (JsonSerializationException)
            {
                var legacyData = JsonConvert.DeserializeObject<List<CarLegacy>>(text);
                data = legacyData.Select(x => x.getCar()).ToList();
            }
            var supportData = JsonConvert.DeserializeObject<List<BRInfo>>(File.ReadAllText(filepath.Replace(".json", "BRInfo.json"))).ToDictionary(x => x.type, x => x);
            foreach (var car in data)
            {
                var id = car.type;
                if (!supportData.ContainsKey(id))
                {
                    BriefingRoom.PrintToLog($"Car missing \"{car.module}\" \"{id}\" info data", LogMessageErrorLevel.Warning);
                    continue;
                }
                var infoData = supportData[id];

                itemMap.Add(id, new DBEntryCar
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryCar)), id, "displayName",car.displayName),
                    DCSID = car.type,
                    Liveries = car.paintSchemes.ToDictionary(pair => (Country)Enum.Parse(typeof(Country), pair.Key.Replace(" ", ""), true), pair => pair.Value.Select(x => Tuple.Create(x[0], x[1])).ToList()),
                    Operators = GetOperationalCountries(car),
                    DCSCategory = car.category,
                    Module = car.module,
                    Shape = car.shape,
                    Families = infoData.families.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToArray(),
                    LowPolly = infoData.lowPolly,
                    Immovable = infoData.immovable,
                    DetectionRange = car.detectionRange,
                    ThreatRangeMin = car.threatRangeMin,
                    ThreatRange = car.threatRange
                });
            }

            missingDCSDataWarnings(supportData, itemMap, "Car");

            return itemMap;
        }

        public DBEntryCar() { }
    }
}
