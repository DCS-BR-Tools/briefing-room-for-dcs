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
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Template;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryWeaponByDecade: DBEntry
    {

        internal Decade StartDecade { get; private set; } = Decade.Decade1940;
        internal Decade StartDecadeGuess { get; private set; } = Decade.Decade1940;

        protected override bool OnLoad(string iniFilePath)
        {
            throw new NotImplementedException();
        }

        internal static Dictionary<string, DBEntry> LoadJSON(string filepath, DatabaseLanguage LangDB)
        {
            var itemMap = new Dictionary<string, DBEntry>(StringComparer.InvariantCulture);
            var data = JsonConvert.DeserializeObject<List<WeaponByDecade>>(File.ReadAllText(filepath));
            foreach (var weapon in data)
            {
                var id = weapon.clsid;
                itemMap.Add(id, new DBEntryWeaponByDecade()
                {
                    ID = id,
                   StartDecade = weapon.decade != null ? (Decade)weapon.decade : (Decade)weapon.decadeGuess
                });
            }

            return itemMap;
        }

        public DBEntryWeaponByDecade() { }
    }
}
