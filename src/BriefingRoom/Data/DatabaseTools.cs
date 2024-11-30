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
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal static class DatabaseTools
    {
        internal static SpawnPointType[] CheckSpawnPoints(params SpawnPointType[] spawnPointTypes)
        {
            List<SpawnPointType> spawnPointsList = new List<SpawnPointType>(spawnPointTypes).Distinct().ToList();
            if (spawnPointsList.Count == 0) spawnPointsList.AddRange(Toolbox.GetEnumValues<SpawnPointType>()); // No spawn point type means all spawn point types

            // LandSmall implies LandLarge and LandMedium, LandMedium implies LandLarge (larger spots can handle smaller units)
            if (spawnPointsList.Contains(SpawnPointType.LandSmall)) spawnPointsList.AddRange(new SpawnPointType[] { SpawnPointType.LandMedium, SpawnPointType.LandLarge });
            if (spawnPointsList.Contains(SpawnPointType.LandMedium)) spawnPointsList.Add(SpawnPointType.LandLarge);

            return spawnPointsList.Distinct().ToArray();
        }
    }
}
