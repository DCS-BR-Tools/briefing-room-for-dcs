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
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorAirDefense
    {

        internal static void GenerateAirDefense(ref DCSMission mission)
        {
            foreach (Coalition coalition in Toolbox.GetEnumValues<Coalition>())
            {
                bool ally = coalition == mission.TemplateRecord.ContextPlayerCoalition;

                Side side = ally ? Side.Ally : Side.Enemy;
                AmountNR airDefenseAmount = ally ? mission.TemplateRecord.SituationFriendlyAirDefense.Get() : mission.TemplateRecord.SituationEnemyAirDefense.Get();
                Coordinates centerPoint = ally ? mission.AverageInitialPosition : mission.ObjectivesCenter;
                Coordinates opposingPoint = ally ? mission.ObjectivesCenter : mission.AverageInitialPosition;

                var knockDownCount = 0; // If failed to spawn unit at higher level air defense then we should add the count of groups to the next level down.
                foreach (AirDefenseRange airDefenseRange in Toolbox.GetEnumValues<AirDefenseRange>().Reverse())
                    knockDownCount = CreateAirDefenseGroups(ref mission, side, coalition, airDefenseAmount, knockDownCount, airDefenseRange, centerPoint, opposingPoint);
            }
        }

        private static int CreateAirDefenseGroups(
           ref DCSMission mission, Side side, Coalition coalition,
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
                    validSpawnPoints = new SpawnPointType[] { SpawnPointType.LandSmall, SpawnPointType.LandMedium, SpawnPointType.LandLarge };
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
                var unitCount = 1;
                var forceTryTemplate = false;
                if (airDefenseRange == AirDefenseRange.ShortRangeBattery)
                {
                    unitCount = Toolbox.RandomMinMax(2, 5);
                    forceTryTemplate = Toolbox.RandomChance(2);
                }
                var extraSetting = new Dictionary<string, object>();
                var (units, _) = UnitMaker.GetUnits(ref mission, unitFamilies, unitCount, side, 0, ref extraSetting, true, forceTryTemplate: forceTryTemplate, allowDefaults: false);
                if (units.Count == 0)
                {
                    return groupCount - i;
                }
                // Find spawn point at the proper distance
                Coordinates? spawnPoint =
                    UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                        ref mission,
                        validSpawnPoints,
                        centerPoint,
                        commonAirDefenseDB.DistanceFromCenter[(int)side, (int)airDefenseRange],
                        opposingPoint,
                        new MinMaxD(commonAirDefenseDB.MinDistanceFromOpposingPoint[(int)side, (int)airDefenseRange], 99999),
                        GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, side),
                        unitFamilies.First());

                // No spawn point found, stop here.
                if (!spawnPoint.HasValue)
                {
                    BriefingRoom.PrintTranslatableWarning(mission.LangKey, "NoSpawnPointForAirDefense",airDefenseRange);
                    return groupCount - i;
                }

                UnitMakerGroupInfo? groupInfo = UnitMaker.AddUnitGroup(
                    ref mission,
                        units, side, unitFamilies.First(),
                        "Vehicle", "Vehicle",
                        spawnPoint.Value,
                        0,
                        extraSetting);

                if (mission.TemplateRecord.OptionsMission.Contains("HideAntiAirMFD") && side.Equals(Side.Enemy))
                    groupInfo.Value.DCSGroup.HiddenOnMFD = true;

                if (!groupInfo.HasValue)
                {
                    UnitMakerSpawnPointSelector.RecoverSpawnPoint(ref mission, spawnPoint.Value);
                    return groupCount - i;
                }


                mission.MapData.Add($"UNIT-{groupInfo.Value.UnitDB.Families[0]}-{side}-{groupInfo.Value.GroupID}", new List<double[]> { groupInfo.Value.Coordinates.ToArray() });
            }
            return 0;
        }
    }
}
