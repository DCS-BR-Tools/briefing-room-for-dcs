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

using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryDefaultUnitList : DBEntry
    {
        internal Dictionary<UnitFamily, Dictionary<Decade, List<string>>> DefaultUnits { get; private set; }

        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            DefaultUnits = new Dictionary<UnitFamily, Dictionary<Decade, List<string>>>();
            foreach (UnitFamily fam in Enum.GetValues(typeof(UnitFamily)))
            {
                var subDict = new Dictionary<Decade, List<string>>();
                var prevDecade = Decade.Decade1940;
                foreach (Decade decade in Enum.GetValues(typeof(Decade)))
                {
                    var unitList = ini.GetValueList<string>($"{decade}", $"{fam}");
                    string[] invalidUnits;
                    string[] units = GetValidDBEntryIDs<DBEntryJSONUnit>(unitList, out invalidUnits);
                    foreach (string u in invalidUnits)
                        BriefingRoom.PrintToLog($"Unit \"{u}\" not found in default unit list \"{ID}\".", LogMessageErrorLevel.Warning);

                    if (units.Length == 0)
                    {
                        if (decade == Decade.Decade2020 && subDict[prevDecade].Count() == 0)
                            throw new BriefingRoomException("en", $"Default unit list \"{ID}\" has no unit for family \"{fam}\"");
                        if(decade != Decade.Decade1940)
                            units = subDict[prevDecade].ToArray();
                    };

                    subDict.Add(decade, units.ToList());
                    prevDecade = decade;
                }
                DefaultUnits.Add(fam, subDict);
            }
            return true;
        }
    }
}
