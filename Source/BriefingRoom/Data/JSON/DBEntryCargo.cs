﻿/*
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
    internal class DBEntryCargo : DBEntryJSONUnit
    {
        
        internal string ShapeName { get; init; }

        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCultureIgnoreCase);
            var data = JsonConvert.DeserializeObject<List<Static>>(File.ReadAllText(filepath));
            foreach (var @static in data)
            {
                var id = @static.type;
                itemMap.Add(id, new DBEntryCargo
                {
                    ID = id,
                    UIDisplayName = new LanguageString(@static.displayName),
                    DCSID = @static.type,
                    Countries = new List<Country>{Country.ALL},
                    ShapeName = @static.shapeName,
                    Families = new UnitFamily[]{UnitFamily.Cargo},
                    Operational = new List<Template.Decade>{Template.Decade.Decade1940, Template.Decade.Decade2020},
                });
            }

            return itemMap;
        }

        public DBEntryCargo(){}
    }
}
