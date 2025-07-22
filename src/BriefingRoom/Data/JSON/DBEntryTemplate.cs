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

        internal string Name { get; init; }
        public List<DBEntryTemplateGroup> Groups { get; init; }
        internal List<UnitFamily> TargetFamilies { get; init; }
        internal List<string> Modules { get; init; }
        internal bool LowPolly { get; init; } = false;
        internal bool Immovable { get; init; } = false;

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<JSON.Template>>(File.ReadAllText(filepath));
            foreach (var template in data)
            {
                var id = template.name;
                var templateUnits = template.groups.SelectMany(x => x.units).ToList();
                var definedUnits = templateUnits.Where(x => x.isSpecificType).Select(x => Database.Instance.GetEntry<DBEntryJSONUnit>(x.originalType)).Distinct().ToList();
                var flexibleUnitFamilies = templateUnits.Where(x => !x.isSpecificType).SelectMany(x => x.unitFamilies).Distinct().Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToList();
                var definedUnitFamilies = definedUnits.SelectMany(x => x.Families).Distinct().ToList();
                var entry = new DBEntryTemplate
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryTemplate)), id, "name", template.name),
                    TargetFamilies = flexibleUnitFamilies.Concat(definedUnitFamilies).Distinct().ToList(),
                    Groups = template.groups.Select(x => new DBEntryTemplateGroup
                    {
                        Coordinates = new Coordinates(x.coords),
                        Units = x.units.Select(y => new DBEntryTemplateUnit
                        {
                            Coordinates = new Coordinates(y.coords),
                            DCSID = y.isSpecificType ? y.originalType : null,
                            UnitFamilies = y.unitFamilies.Select(z => (UnitFamily)Enum.Parse(typeof(UnitFamily), z, true)).ToList(),
                            Heading = y.heading,
                            IsScenery = y.isScenery,
                        }).ToList()
                    }).ToList(),
                    Modules = definedUnits.Select(x => x.Module).Where(x => !string.IsNullOrEmpty(x) || !DBEntryDCSMod.CORE_MODS.Contains(x)).Distinct().ToList(),
                    LowPolly = definedUnits.Any(x => x.LowPolly),
                    Immovable = template.immovable
                };

                var units = definedUnits.Where(x => !string.IsNullOrEmpty(x.DCSID) && Database.Instance.GetEntry<DBEntryJSONUnit>(x.DCSID) == null).Select(x => x.DCSID).ToList();
                if (units.Count > 0)
                {
                    BriefingRoom.PrintToLog($"{id} has units not in data: {string.Join(',', units)}", LogMessageErrorLevel.Warning);
                    continue;
                }

                itemMap.Add(id, entry);
            }

            return itemMap;
        }

        public DBEntryTemplate() { }
    }

    public readonly struct DBEntryTemplateGroup
    {
        public DBEntryTemplateGroup()
        {
        }
        public Coordinates Coordinates { get; init; }

        public List<DBEntryTemplateUnit> Units { get; init; }
    }

    public readonly struct DBEntryTemplateUnit
    {
        public DBEntryTemplateUnit()
        {
        }

        public double Heading { get; init; }
        public Coordinates Coordinates { get; init; }
        public List<UnitFamily> UnitFamilies { get; init; } = new List<UnitFamily>();
        public bool IsScenery { get; init; } = false;
        public string DCSID { get; init; } = null;
    }
}
