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
using BriefingRoom4DCS.Template;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryTemplate : DBEntry
    {

        internal string DCSCategory { get; init; }
        internal string Type { get; init; }
        internal Dictionary<Country, (Decade start, Decade end)> Countries { get; init; }
        internal List<DBEntryTemplateUnit> Units { get; init; }
        internal UnitFamily Family { get; init; }
        internal string Module { get; init; }

        protected override bool OnLoad(string iniFilePath)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(IDatabase database, string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<JSON.Template>>(File.ReadAllText(filepath));
            var supportData = JsonConvert.DeserializeObject<List<JSON.TemplateBRInfo>>(File.ReadAllText($"{filepath.Replace(".json", "")}BRInfo.json"))
                .ToDictionary(x => x.type, x => x);
            foreach (var template in data)
            {
                var id = template.name;
                if (!supportData.ContainsKey(id))
                {
                    BriefingRoom.PrintToLog($"Template {id} missing support data.", LogMessageErrorLevel.Warning);
                    continue;
                }
                var supportInfo = supportData[id];
                var defaultOperational = (start: (Decade)supportInfo.operational[0], end: (Decade)supportInfo.operational[1]);
                var extraCountries = supportInfo.extraOperators.ToDictionary(x => (Country)Enum.Parse(typeof(Country), x.Key.Replace(" ", ""), true), x => x.Value.Count > 0 ? (start: (Decade)x.Value[0], end: (Decade)x.Value[1]) : defaultOperational);
                var entry = new DBEntryTemplate
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryTemplate)), id, "name", template.name),
                    Type = template.type,
                    Countries = extraCountries,
                    Family = (UnitFamily)Enum.Parse(typeof(UnitFamily), supportInfo.family, true),
                    Units = template.units.Select(x => new DBEntryTemplateUnit
                    {
                        DCoordinates = new Coordinates(x.dx, x.dy),
                        DCSID = x.name,
                        Heading = x.heading
                    }).ToList(),
                    Module = supportInfo.module
                };

                var units = entry.Units.Where(x => database.GetEntry<DBEntryJSONUnit>(x.DCSID) == null).Select(x => x.DCSID).ToList();
                if (units.Count > 0)
                {
                    BriefingRoom.PrintToLog($"{id} has units not in data: {string.Join(',', units)}", LogMessageErrorLevel.Warning);
                    continue;
                }
                var missingModuleRefs = entry.Units.Select(x => database.GetEntry<DBEntryJSONUnit>(x.DCSID).Module).Where(x => !String.IsNullOrEmpty(x) && x != entry.Module && !DBEntryDCSMod.CORE_MODS.Contains(x)).Distinct().ToList();
                if (missingModuleRefs.Count > 0)
                {
                    BriefingRoom.PrintToLog($"{id} missing module refs in BRInfo data: {string.Join(',', missingModuleRefs)}", LogMessageErrorLevel.Warning);
                }

                itemMap.Add(id, entry);
            }

            missingDCSDataWarnings(supportData, itemMap, "Templates");

            return itemMap;
        }

        public DBEntryTemplate() { }
    }

    public class DBEntryTemplateUnit
    {
        public DBEntryTemplateUnit() { }
        public Coordinates DCoordinates { get; init; }
        public double Heading { get; init; }
        public string DCSID { get; init; }
    }
}
