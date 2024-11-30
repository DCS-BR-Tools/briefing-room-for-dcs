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
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryJSONUnit : DBEntry
    {
        internal string DCSID { get; init; }
        internal Dictionary<Country, List<string>> Liveries { get; init; } = new Dictionary<Country, List<string>> { };
        internal Dictionary<Country, (Decade start, Decade end)> Operators { get; init; }
        internal string Module { get; init; }
        internal UnitCategory Category { get { return Families[0].GetUnitCategory(); } }
        internal bool IsAircraft { get { return Category.IsAircraft(); } }
        internal UnitFamily[] Families { get; init; }
        internal bool LowPolly { get; init; } = false;
        internal bool Immovable { get; init; } = false;
        internal string Shape { get; init; }
        internal int DetectionRange { get; init; }
        internal int ThreatRangeMin { get; init; }
        internal int ThreatRange { get; init; }

        override internal DatabaseEntryInfo GetDBEntryInfo()
        {
            var category = DBEntryDCSMod.CORE_MODS.Contains(Module) || string.IsNullOrEmpty(Module) ? new LanguageString(Category.ToString()) : new LanguageString($"{Category.ToString()} - {Module}");
            return new DatabaseEntryInfo(ID, UIDisplayName, category, new LanguageString(Module));
        }


        protected override bool OnLoad(string o)
        {
            throw new NotImplementedException();
        }


        public DBEntryJSONUnit() { }

        internal static List<Decade> GetOperationalPeriod(Dictionary<Country, Template.Decade[]> iniOperators)
        {
            Decade min = Decade.Decade2020;
            Decade max = Decade.Decade1940;
            foreach (var value in iniOperators.Values)
            {
                if (value[0] < min)
                    min = value[0];
                if (value[1] > min)
                    max = value[1];
            }
            return new List<Decade> { min, max };
        }

        internal static Dictionary<Country, (Decade start, Decade end)> GetOperationalCountries(Unit unit)
        {
            return unit.Operators.ToDictionary(x => (Country)Enum.Parse(typeof(Country), x.Key.Replace(" ", ""), true), x =>(start: (Decade)x.Value[0], end: (Decade)x.Value[1]));
        }
    }
}
