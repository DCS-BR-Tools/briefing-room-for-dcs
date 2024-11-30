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
using BriefingRoom4DCS.Generator;

namespace BriefingRoom4DCS.Template
{

    internal sealed class DCSMissionStrikePackage
    {
        internal List<Waypoint> Waypoints { get; set; }
        internal DBEntryAirbase Airbase { get; }
        internal int RecordIndex { get; }

        internal DCSMissionStrikePackage(int recordIndex, DBEntryAirbase airbase)
        {
            RecordIndex = recordIndex;
            Airbase = airbase;
        }
    }
}
