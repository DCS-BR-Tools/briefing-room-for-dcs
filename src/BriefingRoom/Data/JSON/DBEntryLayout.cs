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
    internal class DBEntryLayout : DBEntry
    {

        internal List<DBEntryTemplateUnit> Units { get; init; }
        internal List<UnitCategory> Categories { get; init; }
        internal int MinUnits { get; init; }

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<BriefingRoom4DCS.Data.JSON.Layout>>(File.ReadAllText(filepath));
            foreach (var layout in data)
            {
                var id = layout.name;

                itemMap.Add(id, new DBEntryLayout
                {
                    ID = id,
                    UIDisplayName = new LanguageString(LangDB, GetLanguageClassName(typeof(DBEntryLayout)), id, "name",layout.name),
                    Categories = layout.categories.Select(x => (UnitCategory)Enum.Parse(typeof(UnitCategory), x, true)).ToList(),
                    MinUnits = layout.minUnits,
                    Units = layout.units.Select(x => new DBEntryTemplateUnit
                    {
                        DCoordinates = new Coordinates(x.dx, x.dy),
                        Heading = x.heading
                    }).ToList()
                });
            }

            return itemMap;
        }

        public DBEntryLayout() { }
    }
}
