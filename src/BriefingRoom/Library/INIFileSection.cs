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

namespace BriefingRoom4DCS
{
    internal class INIFileSection
    {
        internal bool Abstract { get; }

        internal string ParentSection { get; }

        private readonly Dictionary<string, string> Values;

        internal INIFileSection(bool isAbstract = false, string parentSection = null)
        {
            Abstract = isAbstract;
            ParentSection = parentSection?.ToLower();

            Values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        internal string this[string key]
        {
            get
            {
                return Values.ContainsKey(key) ? Values[key] : null;
            }
            set
            {
                key = key?.ToLower();

                if (Values.ContainsKey(key))
                    Values[key] = value;
                else
                    Values.Add(key, value);
            }
        }

        internal string[] Keys
        {
            get
            {
                return Values.Keys.ToArray();
            }
        }

        internal void Clear()
        {
            Values.Clear();
        }

        internal bool ContainsKey(string key)
        {
            return Values.ContainsKey(key);
        }
    }
}
