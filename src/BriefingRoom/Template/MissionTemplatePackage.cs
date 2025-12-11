/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Template
{

    public sealed class MissionTemplatePackage : MissionTemplateGroup
    {
        public List<int> FlightGroupIndexes { get; set; }
        public List<int> ObjectiveIndexes { get; set; }
        public string StartingAirbase { get { return StartingAirbase_; } set { StartingAirbase_ = Database.CheckID<DBEntryAirbase>(value, allowEmptyStr: true, allowedValues: new List<string>{"home", "homeDest"}); } }
        private string StartingAirbase_;
        public string DestinationAirbase { get { return DestinationAirbase_; } set { DestinationAirbase_ = Database.CheckID<DBEntryAirbase>(value, allowEmptyStr: true, allowedValues: new List<string>{"home", "homeDest", "strike"}); } }
        private string DestinationAirbase_;

        public MissionTemplatePackage(IDatabase database): base(database)
        {
            Clear();
        }

        public MissionTemplatePackage(IDatabase database, List<int> flightGroupIndexes, List<int> objectiveIndexes, string startingAirbase, string destinationAirbase): base(database)
        {
            FlightGroupIndexes = flightGroupIndexes;
            ObjectiveIndexes = objectiveIndexes;
            StartingAirbase = startingAirbase;
            DestinationAirbase = destinationAirbase;
        }

        internal MissionTemplatePackage(IDatabase database, INIFile ini, string section, string key): base(database)
        {
            Clear();

            FlightGroupIndexes = ini.GetValueArray<int>(section, $"{key}.FlightGroupIndexes").ToList();
            ObjectiveIndexes = ini.GetValueArray<int>(section, $"{key}.ObjectiveIndexes").ToList();
            StartingAirbase = ini.GetValue(section, $"{key}.StartingAirbase", StartingAirbase);
            DestinationAirbase = ini.GetValue(section, $"{key}.DestinationAirbase", DestinationAirbase);
        }

        private void Clear()
        {
            FlightGroupIndexes = new();
            ObjectiveIndexes = new();
            StartingAirbase = "home";
            DestinationAirbase = "home";
        }

        internal void SaveToFile(INIFile ini, string section, string key)
        {
            ini.SetValueArray(section, $"{key}.FlightGroupIndexes", FlightGroupIndexes.Select(x => x.ToString()).ToArray());
            ini.SetValueArray(section, $"{key}.ObjectiveIndexes", ObjectiveIndexes.Select(x => x.ToString()).ToArray());
            ini.SetValue(section, $"{key}.StartingAirbase", StartingAirbase);
            ini.SetValue(section, $"{key}.DestinationAirbase", DestinationAirbase);
        }


    }
}
