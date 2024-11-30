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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorCombatAirPatrols
    {

        internal static void GenerateCAP(ref DCSMission mission)
        {
            var commonCAPDB = Database.Instance.Common.CAP;
            foreach (Coalition coalition in Toolbox.GetEnumValues<Coalition>())
            {
                if (coalition == Coalition.Neutral) // Skip Neutural
                    continue;

                bool ally = coalition == mission.TemplateRecord.ContextPlayerCoalition;

                Side side = ally ? Side.Ally : Side.Enemy;
                AmountNR capAmount = ally ? mission.TemplateRecord.SituationFriendlyAirForce.Get() : mission.TemplateRecord.SituationEnemyAirForce.Get();
                Coordinates flyPathtoObjectives = (mission.ObjectivesCenter - mission.AverageInitialPosition).Normalize() * Toolbox.NM_TO_METERS * commonCAPDB.MinDistanceFromOpposingPoint; // TODO: distance according to decade
                Coordinates centerPoint = mission.ObjectivesCenter;
                if (ally) centerPoint -= flyPathtoObjectives;
                else centerPoint += flyPathtoObjectives;

                Coordinates opposingPoint = mission.ObjectivesCenter;

                CreateCAPGroups(
                    mission,
                    side, coalition, capAmount,
                    centerPoint, opposingPoint, mission.ObjectivesCenter);

            }

        }

        private static void CreateCAPGroups(
            DCSMission mission, Side side,
            Coalition coalition, AmountNR capAmount, Coordinates centerPoint,
            Coordinates opposingPoint, Coordinates destination)
        {
            var commonCAPDB = Database.Instance.Common.CAP;
            DBCommonCAPLevel capLevelDB = commonCAPDB.CAPLevels[(int)capAmount];

            int unitsLeftToSpawn = capLevelDB.UnitCount.GetValue();
            if (unitsLeftToSpawn < 1) return;  // No groups to add, no need to go any further

            do
            {
                int groupSize = Toolbox.RandomFrom(commonCAPDB.GroupSize);
                groupSize = Math.Min(unitsLeftToSpawn, groupSize);
                unitsLeftToSpawn -= groupSize;

                // Find spawn point at the proper distance from the objective(s), but not to close from starting airbase
                Coordinates? spawnPoint =
                    UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                        ref mission,
                        new SpawnPointType[] { SpawnPointType.Air },
                        centerPoint,
                        commonCAPDB.DistanceFromCenter,
                        opposingPoint,
                        new MinMaxD(commonCAPDB.MinDistanceFromOpposingPoint, 99999),
                        GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, side));

                // No spawn point found, stop here.
                if (!spawnPoint.HasValue)
                {
                    throw new BriefingRoomException(mission.LangKey,"NoCAPSpawnPoint", coalition);
                }

                Coordinates groupDestination = destination + Coordinates.CreateRandom(10, 20) * Toolbox.NM_TO_METERS;

                var extraSettings = new Dictionary<string, object>{
                    {"DCSTask", DCSTask.CAP},
                    {"GroupX2", groupDestination.X},
                    {"GroupY2", groupDestination.Y}
                };

                var spawnpointCoordinates = spawnPoint.Value;

                UnitMakerGroupFlags groupFlags = 0;

                if (Toolbox.RandomChance(4))
                    groupFlags |= UnitMakerGroupFlags.ImmediateAircraftSpawn;

                if (mission.TemplateRecord.MissionFeatures.Contains("ContextScrambleStart"))
                    groupFlags |= UnitMakerGroupFlags.ScrambleStart;

                var (units, unitDBs) = UnitMaker.GetUnits(ref mission,commonCAPDB.UnitFamilies.ToList(), groupSize, side, groupFlags, ref extraSettings, false);
                if(units.Count == 0)
                    return;
                var unitDB = (DBEntryAircraft)unitDBs.First();
                if (mission.TemplateRecord.MissionFeatures.Contains("ContextGroundStartAircraft"))
                {
                    try {
                        var (airbase, parkingSpotIDsList, parkingSpotCoordinatesList) = UnitMakerSpawnPointSelector.GetAirbaseAndParking(mission, spawnPoint.Value, groupSize, coalition, unitDB);
                        spawnpointCoordinates = airbase.Coordinates;
                        extraSettings.AddIfKeyUnused("ParkingID", parkingSpotIDsList);
                        extraSettings.AddIfKeyUnused("GroupAirbaseID", airbase.DCSID);
                        mission.PopulatedAirbaseIds[coalition].Add(airbase.DCSID);
                        extraSettings.AddIfKeyUnused("UnitCoords", parkingSpotCoordinatesList);
                        mission.MapData.AddIfKeyUnused($"AIRBASE_AI_{side}_${airbase.Name}", new List<double[]> { airbase.Coordinates.ToArray() });
                    } catch (BriefingRoomException e)
                    {
                        BriefingRoom.PrintTranslatableWarning(mission.LangKey, "CAPCannotBeSpawnedAirport", e.Message);
                    }

                }


                UnitMakerGroupInfo? groupInfo = UnitMaker.AddUnitGroup(ref mission,units, side, unitDB.Families.First(), commonCAPDB.LuaGroup, commonCAPDB.LuaUnit, spawnpointCoordinates, groupFlags, extraSettings);

                if (!groupInfo.HasValue) // Failed to generate a group
                    BriefingRoom.PrintTranslatableWarning(mission.LangKey, "FailedToFindCAPUnits", mission.LangKey, coalition);

                SetCarrier(ref mission, side, ref groupInfo);

                groupInfo.Value.DCSGroup.Waypoints = DCSWaypoint.CreateExtraWaypoints(ref mission, groupInfo.Value.DCSGroup.Waypoints, groupInfo.Value.UnitDB.Families.First());

            } while (unitsLeftToSpawn > 0);
        }

        private static void SetCarrier(ref DCSMission mission, Side side, ref UnitMakerGroupInfo? groupInfo)
        {
            if (
                side == Side.Enemy ||
                !mission.TemplateRecord.MissionFeatures.Contains("ContextGroundStartAircraft") ||
                 true
            )
                return;

            UnitFamily targetFamily = UnitFamily.ShipCarrierSTOVL;
            if (groupInfo.Value.UnitDB.Families.Contains(UnitFamily.PlaneCATOBAR))
                targetFamily = UnitFamily.ShipCarrierCATOBAR;
            if (groupInfo.Value.UnitDB.Families.Contains(UnitFamily.PlaneSTOBAR))
                targetFamily = UnitFamily.ShipCarrierSTOBAR;
            var unitCount = groupInfo.Value.DCSGroup.Units.Count;
            var carrierPool = mission.CarrierDictionary.Where(x =>
                    x.Value.UnitMakerGroupInfo.UnitDB.Families.Contains(targetFamily) &&
                    x.Value.RemainingPlaneSpotCount >= unitCount
                ).ToDictionary(x => x.Key, x => x.Value);

            if (carrierPool.Count == 0)
                return;

            var carrier = Toolbox.RandomFrom(carrierPool.Values.ToArray());
            groupInfo.Value.DCSGroup.Waypoints[0].LinkUnit = carrier.UnitMakerGroupInfo.DCSGroup.Units[0].UnitId;
            groupInfo.Value.DCSGroup.Waypoints[0].HelipadId = carrier.UnitMakerGroupInfo.DCSGroup.Units[0].UnitId;
            groupInfo.Value.DCSGroup.Waypoints[0].X = (float)carrier.UnitMakerGroupInfo.Coordinates.X;
            groupInfo.Value.DCSGroup.Waypoints[0].Y = (float)carrier.UnitMakerGroupInfo.Coordinates.Y;
            groupInfo.Value.DCSGroup.X = (float)carrier.UnitMakerGroupInfo.Coordinates.X;
            groupInfo.Value.DCSGroup.Y = (float)carrier.UnitMakerGroupInfo.Coordinates.Y;
            carrier.RemainingPlaneSpotCount -= unitCount;
        }
    }
}
