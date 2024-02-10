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
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator;
using BriefingRoom4DCS.Mission.DCSLuaObjects;

namespace BriefingRoom4DCS.Mission
{
    public class CarrierUnitMakerGroupInfo
    {
        internal UnitMakerGroupInfo UnitMakerGroupInfo { get; }
        internal int RemainingSpotCount { get; set; }
        internal int TotalSpotCount { get; init; }

        internal Coalition Coalition { get; init; }

        internal CarrierUnitMakerGroupInfo(UnitMakerGroupInfo unitMakerGroupInfo, int remainingSpotCount, Coalition coalition)
        {
            UnitMakerGroupInfo = unitMakerGroupInfo;
            RemainingSpotCount = remainingSpotCount;
            TotalSpotCount = remainingSpotCount;
            Coalition = coalition;
        }
    }

    public class DCSMissionState
    {
        protected internal string UniqueID { get; protected set; }
        public DCSMissionBriefing Briefing { get; protected set; }
        protected internal Dictionary<string, List<double[]>> MapData { get; protected set; }
        protected internal Dictionary<string, string> Values { get; protected set; }
        protected internal Dictionary<string, object> MediaFiles { get; protected set; }
        protected internal HashSet<string> SingletonSet { get; protected set; }
        protected internal Dictionary<int, Coalition> Airbases { get; protected set; }
        protected internal Dictionary<Coalition, List<int>> PopulatedAirbaseIds { get; protected set; }
        protected internal List<string> LuaDrawings { get; protected set; }
        protected internal List<string> LuaZones { get; protected set; }
        internal int CTLDZoneCount { get; set; } = 1;
        protected internal int PrevLaserCode { get; protected set; } = 1687;
        protected internal int TACANIndex { get; protected set; } = 1;

        protected internal List<string> NATOCallsigns { get; protected set; }
        protected internal List<string> RussianCallsigns { get; protected set; }

        protected internal Dictionary<int, List<DBEntryAirbaseParkingSpot>> AirbaseParkingSpots { get; protected set; }
        protected internal List<DBEntryTheaterSpawnPoint> SpawnPoints { get; protected set; }
        protected internal List<DBEntryTheaterSpawnPoint> UsedSpawnPoints { get; protected set; }
        protected internal List<Coordinates> FrontLine { get; protected set; }
        protected internal bool PlayerSideOfFrontLine { get; protected set; }
        protected internal int GroupID { get; set; }
        protected internal int UnitID { get; set; }
        protected internal Dictionary<string, CarrierUnitMakerGroupInfo> CarrierDictionary { get; protected set; }
        protected internal List<string> ModUnits { get; protected set; }
        protected internal Dictionary<Country, Dictionary<DCSUnitCategory, List<DCSGroup>>> UnitLuaTables { get; protected set; }
        protected internal List<Waypoint> Waypoints { get; protected set; }

        internal DCSMissionState()
        {

        }

        internal DCSMissionState(MissionStageName stageName, DCSMission mission)
        {
            UniqueID = stageName.ToString();
            Briefing = new DCSMissionBriefing(mission.Briefing);
            MapData = mission.MapData.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            Values = mission.Values.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            MediaFiles = mission.MediaFiles.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            SingletonSet = mission.SingletonSet.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            Airbases = mission.Airbases.ToDictionary(x => x.Key, x => x.Value);
            PopulatedAirbaseIds = mission.PopulatedAirbaseIds.ToDictionary(x => x.Key, x => x.Value.ToList());
            LuaDrawings = mission.LuaDrawings.ToList();
            LuaZones = mission.LuaZones.ToList();
            CTLDZoneCount = mission.CTLDZoneCount;
            PrevLaserCode = mission.PrevLaserCode;
            TACANIndex = mission.TACANIndex;
            AirbaseParkingSpots = mission.AirbaseParkingSpots.ToDictionary(x => x.Key, x => x.Value.ToList());
            SpawnPoints = mission.SpawnPoints.ToList();
            UsedSpawnPoints = mission.UsedSpawnPoints.ToList();
            FrontLine = mission.FrontLine.ToList();
            PlayerSideOfFrontLine = mission.PlayerSideOfFrontLine;
            GroupID = mission.GroupID;
            UnitID = mission.UnitID;
            CarrierDictionary = mission.CarrierDictionary.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            ModUnits = mission.ModUnits.ToList();
            UnitLuaTables = mission.UnitLuaTables.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value.ToList()));
            Waypoints = mission.Waypoints.ToList();
        }
    }
}

