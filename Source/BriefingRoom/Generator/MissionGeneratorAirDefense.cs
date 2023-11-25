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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorAirDefense
    {

        internal static void GenerateAirDefense(MissionTemplateRecord template, DCSMission mission, UnitMaker unitMaker, Coordinates averageInitialPosition, Coordinates objectivesCenter)
        {
            foreach (Coalition coalition in Toolbox.GetEnumValues<Coalition>())
            {
                bool ally = coalition == template.ContextPlayerCoalition;

                Side side = ally ? Side.Ally : Side.Enemy;
                AmountNR airDefenseAmount = ally ? template.SituationFriendlyAirDefense.Get() : template.SituationEnemyAirDefense.Get();
                Coordinates centerPoint = ally ? averageInitialPosition : objectivesCenter;
                Coordinates opposingPoint = ally ? objectivesCenter : averageInitialPosition;

                var knockDownCount = 0; // If failed to spawn unit at higher level air defense then we should add the count of groups to the next level down.
                foreach (AirDefenseRange airDefenseRange in Toolbox.GetEnumValues<AirDefenseRange>().Reverse())
                    knockDownCount = CreateAirDefenseGroups(template, mission, unitMaker, side, coalition, airDefenseAmount, knockDownCount, airDefenseRange, centerPoint, opposingPoint);
            }
        }

        private static int CreateAirDefenseGroups(
            MissionTemplateRecord template, DCSMission mission, UnitMaker unitMaker, Side side, Coalition coalition,
            AmountNR airDefenseAmount, int knockDownCount, AirDefenseRange airDefenseRange,
            Coordinates centerPoint, Coordinates opposingPoint)
        {
            var airDefenseInt = (int)airDefenseAmount;
            var commonAirDefenseDB = Database.Instance.Common.AirDefense;
            DBCommonAirDefenseLevel airDefenseLevelDB = commonAirDefenseDB.AirDefenseLevels[airDefenseInt];

            int groupCount = airDefenseLevelDB.GroupsInArea[(int)airDefenseRange].GetValue() + knockDownCount;
            if (groupCount < 1) return 0;  // No groups to add, no need to go any further

            List<UnitFamily> unitFamilies;
            SpawnPointType[] validSpawnPoints;
            switch (airDefenseRange)
            {
                case AirDefenseRange.EWR:
                    unitFamilies = new List<UnitFamily> { UnitFamily.VehicleEWR };
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandSmall, SpawnPointType.LandMedium };
                    break;
                case AirDefenseRange.LongRange:
                    unitFamilies = new List<UnitFamily> { UnitFamily.VehicleSAMLong };
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandLarge };
                    break;
                case AirDefenseRange.MediumRange:
                    unitFamilies = new List<UnitFamily> { UnitFamily.VehicleSAMMedium };
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandLarge };
                    break;
                case AirDefenseRange.ShortRangeBattery:
                    unitFamilies = new List<UnitFamily> { UnitFamily.VehicleAAAStatic, UnitFamily.VehicleAAA, UnitFamily.InfantryMANPADS };
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandLarge, SpawnPointType.LandMedium };
                    break;
                default: // case AirDefenseRange.ShortRange:
                    unitFamilies = new List<UnitFamily> { UnitFamily.VehicleAAA, UnitFamily.VehicleAAAStatic, UnitFamily.InfantryMANPADS, UnitFamily.VehicleSAMShort, UnitFamily.VehicleSAMShort, UnitFamily.VehicleSAMShortIR, UnitFamily.VehicleSAMShortIR };
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandSmall, SpawnPointType.LandMedium, SpawnPointType.LandLarge };
                    break;
            }

            for (int i = 0; i < groupCount; i++)
            {
                var unitFamily = Toolbox.RandomFrom(unitFamilies);
                // Find spawn point at the proper distance
                Coordinates? spawnPoint =
                    unitMaker.SpawnPointSelector.GetRandomSpawnPoint(
                        validSpawnPoints,
                        centerPoint,
                        commonAirDefenseDB.DistanceFromCenter[(int)side, (int)airDefenseRange],
                        opposingPoint,
                        new MinMaxD(commonAirDefenseDB.MinDistanceFromOpposingPoint[(int)side, (int)airDefenseRange], 99999),
                        GeneratorTools.GetSpawnPointCoalition(template, side),
                        unitFamily);

                // No spawn point found, stop here.
                if (!spawnPoint.HasValue)
                {
                    BriefingRoom.PrintToLog($"No spawn point found for {airDefenseRange} air defense unit groups", LogMessageErrorLevel.Warning);
                    return groupCount -i;
                }
                var unitCount = 1;
                var forceTryTemplate = false;
                if (airDefenseRange == AirDefenseRange.ShortRangeBattery)
                {
                    unitCount = Toolbox.RandomMinMax(2, 5);
                    forceTryTemplate = Toolbox.RandomChance(2);
                }

                UnitMakerGroupInfo? groupInfo = unitMaker.AddUnitGroup(
                        unitFamily, unitCount, side,
                        "Vehicle", "Vehicle",
                        spawnPoint.Value,
                        0,
                        new Dictionary<string, object>(),
                        true,
                        forceTryTemplate: forceTryTemplate);

                if (!groupInfo.HasValue){
                    BriefingRoom.PrintToLog(
                        $"Failed to add {airDefenseRange} air defense unit group for {coalition} coalition.",
                        LogMessageErrorLevel.Warning);
                    unitMaker.SpawnPointSelector.RecoverSpawnPoint(spawnPoint.Value);
                    return groupCount -i;
                }
                mission.MapData.Add($"UNIT-{groupInfo.Value.UnitDB.Families[0]}-{side}-{groupInfo.Value.GroupID}", new List<double[]> { groupInfo.Value.Coordinates.ToArray() });
            }
            return 0;
        }
    }
}
