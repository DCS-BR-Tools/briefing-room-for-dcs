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

namespace BriefingRoom4DCS.Template
{
    internal sealed record MissionTemplateFlightGroupRecord
    {
        internal string Aircraft { get; init; }
        internal bool AIWingmen { get; init; }
        internal bool Hostile { get; init; }
        internal string Carrier { get; init; }
        internal int Count { get; init; }
        internal Country Country { get; init; }
        internal string Payload { get; init; }
        internal PlayerStartLocation StartLocation { get; init; }
        internal string StartingAirbase { get; init; } = "home";
        internal List<int> ObjectiveIndexes { get; init; }
        internal string Livery { get; init; } = "default";
        internal string OverrideRadioFrequency { get; init; } = "";
        internal RadioModulation OverrideRadioBand { get; init; } = RadioModulation.AM;
        internal string OverrideCallsignName { get; set; } = "";
        internal int OverrideCallsignNumber { get; set; } = 1;

        internal int Index { get; init; }

        internal MissionTemplateFlightGroupRecord(MissionTemplateFlightGroup flightGroup, int _index)
        {
            Aircraft = flightGroup.Aircraft;
            AIWingmen = flightGroup.AIWingmen;
            Hostile = flightGroup.Hostile;
            Carrier = flightGroup.Carrier;
            Count = flightGroup.Count;
            Country = flightGroup.Country;
            Payload = flightGroup.Payload;
            StartLocation = flightGroup.StartLocation;
            Livery = flightGroup.Livery;
            OverrideRadioFrequency = flightGroup.OverrideRadioFrequency;
            OverrideRadioBand = flightGroup.OverrideRadioBand;
            OverrideCallsignName = flightGroup.OverrideCallsignName;
            OverrideCallsignNumber = flightGroup.OverrideCallsignNumber;
            Index = _index;
        }
    }
}
