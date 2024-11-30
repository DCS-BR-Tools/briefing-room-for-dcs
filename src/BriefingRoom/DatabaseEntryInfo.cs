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

using System.Collections.Generic;

namespace BriefingRoom4DCS
{
    public readonly struct DatabaseEntryInfo
    {
        public LanguageString Category { get; }

        public LanguageString Description { get; }

        public string ID { get; }

        public LanguageString Name { get; }

        public DatabaseEntryInfo(string id, LanguageString name, LanguageString category, LanguageString description)
        {
            ID = id;
            Name = name;
            Category = category;
            Description = description;
        }

        public string GetNameAndDescription(string langKey, string separator = " - ", bool upperCaseName = false)
        {
            string casedName = upperCaseName ? Name.Get(langKey).ToUpper() : Name.Get(langKey);
            var description = Description.Get(langKey);
            if (string.IsNullOrEmpty(description)) return casedName;
            return $"{casedName}{separator}{description}";
        }
    }
}
