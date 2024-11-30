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
using System.IO;
using System.Linq;


namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorObjectives
    {
        private static readonly List<DBEntryObjectiveTargetBehaviorLocation> AIRBASE_LOCATIONS = new()
        {
            DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbase,
            DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbaseParking,
            DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbaseParkingNoHardenedShelter,
        };

        private static readonly List<DBEntryObjectiveTargetBehaviorLocation> AIR_ON_GROUND_LOCATIONS = new()
        {
            DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbaseParking,
            DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbaseParkingNoHardenedShelter
        };

        private static readonly List<SpawnPointType> LAND_SPAWNS = new()
        {
            SpawnPointType.LandSmall,
            SpawnPointType.LandMedium,
            SpawnPointType.LandLarge,
        };

        private static readonly List<List<UnitFamily>> MIXED_VEHICLE_SETS = new()
        {
            new List<UnitFamily> {UnitFamily.VehicleAPC},
            new List<UnitFamily> {UnitFamily.VehicleAPC, UnitFamily.VehicleAPC, UnitFamily.VehicleMBT},
            new List<UnitFamily> {UnitFamily.VehicleAPC, UnitFamily.VehicleAPC, UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleArtillery},
            new List<UnitFamily> {UnitFamily.VehicleArtillery,UnitFamily.VehicleArtillery,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleMBT},
            new List<UnitFamily> {UnitFamily.VehicleMBT,UnitFamily.VehicleMBT,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleMissile},
            new List<UnitFamily> {UnitFamily.VehicleMissile,UnitFamily.VehicleMissile,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleTransport,UnitFamily.VehicleTransport,UnitFamily.VehicleAPC,},
        };


        internal static Tuple<Coordinates, List<List<Waypoint>>> GenerateObjective(
            DCSMission mission,
            MissionTemplateObjectiveRecord task,
            Coordinates lastCoordinates,
            ref int objectiveIndex)
        {
            var waypointList = new List<List<Waypoint>>();
            var (featuresID, targetDB, targetBehaviorDB, taskDB, objectiveOptions) = GetObjectiveData(mission.LangKey, task);
            var useHintCoordinates = task.CoordinatesHint.ToString() != "0,0";
            lastCoordinates = useHintCoordinates ? task.CoordinatesHint : lastCoordinates;
            var objectiveCoordinates = GetSpawnCoordinates(ref mission, lastCoordinates, mission.PlayerAirbase, targetDB, useHintCoordinates);


            waypointList.Add(CreateObjective(
                task,
                taskDB,
                targetDB,
                targetBehaviorDB,
                ref objectiveIndex,
                ref objectiveCoordinates,
                objectiveOptions,
                ref mission,
                featuresID));

            var preValidSpawns = targetDB.ValidSpawnPoints.ToList();

            foreach (var subTasks in task.SubTasks)
            {
                objectiveIndex++;
                waypointList.Add(GenerateSubTask(
                    mission,
                    subTasks,
                    objectiveCoordinates,
                    preValidSpawns, targetBehaviorDB.Location,
                    featuresID, ref objectiveIndex));

            }
            return new(objectiveCoordinates, waypointList);
        }

        private static List<Waypoint> GenerateSubTask(
            DCSMission mission,
            MissionTemplateSubTaskRecord task,
            Coordinates coreCoordinates,
            List<SpawnPointType> preValidSpawns,
            DBEntryObjectiveTargetBehaviorLocation mainObjLocation,
            string[] featuresID,
            ref int objectiveIndex)
        {
            var (targetDB, targetBehaviorDB, taskDB, objectiveOptions, _) = GetCustomObjectiveData(mission.LangKey, task);

            preValidSpawns.AddRange(targetDB.ValidSpawnPoints);
            if (preValidSpawns.Contains(SpawnPointType.Sea) && preValidSpawns.Any(x => LAND_SPAWNS.Contains(x)))
                throw new BriefingRoomException(mission.LangKey, "LandSeaSubMix");
            if (AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && !AIRBASE_LOCATIONS.Contains(mainObjLocation))
                throw new BriefingRoomException(mission.LangKey, "AirbaseSubMix");
            var objectiveCoords = GetNearestSpawnCoordinates(ref mission, coreCoordinates, targetDB);
            return CreateObjective(
                task,
                taskDB,
                targetDB,
                targetBehaviorDB,
                ref objectiveIndex,
                ref objectiveCoords,
                objectiveOptions,
                ref mission,
                featuresID);
        }

        private static List<Waypoint> CreateObjective(
            MissionTemplateSubTaskRecord task,
            DBEntryObjectiveTask taskDB,
            DBEntryObjectiveTarget targetDB,
            DBEntryObjectiveTargetBehavior targetBehaviorDB,
            ref int objectiveIndex,
            ref Coordinates objectiveCoordinates,
            ObjectiveOption[] objectiveOptions,
            ref DCSMission mission,
            string[] featuresID)
        {
            var extraSettings = new Dictionary<string, object>();
            var (luaUnit, unitCount, unitCountMinMax, objectiveTargetUnitFamilies, groupFlags) = GetUnitData(task, targetDB, targetBehaviorDB, objectiveOptions);
            var isInverseTransportWayPoint = false;
            var (units, unitDBs) = UnitMaker.GetUnits(ref mission, objectiveTargetUnitFamilies, unitCount, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic);
            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            if (units.Count == 0 || unitDBs.Count == 0)
                throw new BriefingRoomException(mission.LangKey, "NoUnitsForTimePeriod", taskDB.TargetSide, objectiveTargetUnitFamily);
            var unitDB = unitDBs.First();
            if (AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && targetDB.UnitCategory.IsAircraft())
                objectiveCoordinates = PlaceInAirbase(ref mission, extraSettings, targetBehaviorDB, objectiveCoordinates, unitCount, unitDB);

            // Set destination point for moving unit groups
            Coordinates destinationPoint = objectiveCoordinates +
                (
                    targetDB.UnitCategory switch
                    {
                        UnitCategory.Plane => Coordinates.CreateRandom(30, 60),
                        UnitCategory.Helicopter => Coordinates.CreateRandom(10, 20),
                        _ => objectiveTargetUnitFamily == UnitFamily.InfantryMANPADS || objectiveTargetUnitFamily == UnitFamily.Infantry ? Coordinates.CreateRandom(1, 5) : Coordinates.CreateRandom(5, 10)
                    } * Toolbox.NM_TO_METERS
                );
            if (targetDB.DCSUnitCategory == DCSUnitCategory.Vehicle)
                destinationPoint = GetNearestSpawnCoordinates(ref mission, destinationPoint, targetDB, false);


            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            if (targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.GoToPlayerAirbase)
            {
                destinationPoint = mission.PlayerAirbase.ParkingSpots.Length > 1 ? Toolbox.RandomFrom(mission.PlayerAirbase.ParkingSpots).Coordinates : mission.PlayerAirbase.Coordinates;
                if (objectiveTargetUnitFamily.GetUnitCategory().IsAircraft() && taskDB.TargetSide == Side.Enemy)
                {
                    groupLua = objectiveTargetUnitFamily switch
                    {
                        UnitFamily.PlaneAttack => "AircraftBomb",
                        UnitFamily.PlaneBomber => "AircraftBomb",
                        UnitFamily.PlaneStrike => "AircraftBomb",
                        UnitFamily.PlaneFighter => "AircraftCAP",
                        UnitFamily.PlaneInterceptor => "AircraftCAP",
                        UnitFamily.HelicopterAttack => "AircraftBomb",
                        _ => groupLua
                    };
                }
            }
            else if (targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.GoToAirbase)
            {
                var targetCoalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, taskDB.TargetSide, forceSide: true);
                var destinationAirbase = mission.AirbaseDB.Where(x => x.Coalition == targetCoalition.Value).OrderBy(x => destinationPoint.GetDistanceFrom(x.Coordinates)).First();
                destinationPoint = destinationAirbase.Coordinates;
                extraSettings.Add("EndAirbaseId", destinationAirbase.DCSID);
                mission.PopulatedAirbaseIds[targetCoalition.Value].Add(destinationAirbase.DCSID);
            }

            extraSettings.Add("GroupX2", destinationPoint.X);
            extraSettings.Add("GroupY2", destinationPoint.Y);
            extraSettings.Add("playerCanDrive", false);
            extraSettings.Add("NoCM", true);

            var unitCoordinates = objectiveCoordinates;
            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            var objectiveWaypoints = new List<Waypoint>();
            if (taskDB.UICategory.ContainsValue("Transport"))
            {
                if (targetBehaviorDB.ID.StartsWith("RelocateToNewPosition"))
                {
                    Coordinates? spawnPoint = UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                    ref mission,
                    targetDB.ValidSpawnPoints,
                    objectiveCoordinates,
                    mission.TemplateRecord.FlightPlanObjectiveSeparation,
                    coalition: GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Ally));
                    if (!spawnPoint.HasValue) // Failed to generate target group
                        throw new BriefingRoomException(mission.LangKey, "FailedToFindCargoSpawn");
                    unitCoordinates = spawnPoint.Value;
                }
                else
                {
                    var coords = targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.GoToPlayerAirbase ? mission.PlayerAirbase.Coordinates : unitCoordinates;
                    var (_, _, spawnPoints) = UnitMakerSpawnPointSelector.GetAirbaseAndParking(mission, coords, 1, GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Ally, true).Value, (DBEntryAircraft)Database.Instance.GetEntry<DBEntryJSONUnit>("Mi-8MT"));
                    if (spawnPoints.Count == 0) // Failed to generate target group
                        throw new BriefingRoomException(mission.LangKey, "FailedToFindCargoSpawn");
                    unitCoordinates = spawnPoints.First();
                }
                if (targetBehaviorDB.ID.StartsWith("RecoverToBase") || (taskDB.IsEscort() & !targetBehaviorDB.ID.StartsWith("ToFrontLine")))
                {
                    (unitCoordinates, objectiveCoordinates) = (objectiveCoordinates, unitCoordinates);
                    isInverseTransportWayPoint = true;
                }
                var cargoWaypoint = GenerateObjectiveWaypoint(ref mission, task, unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", scriptIgnore: true);
                mission.Waypoints.Add(cargoWaypoint);
                objectiveWaypoints.Add(cargoWaypoint);
                if (taskDB.IsEscort())
                {
                    extraSettings["GroupX2"] = objectiveCoordinates.X;
                    extraSettings["GroupY2"] = objectiveCoordinates.Y;
                    groupFlags |= UnitMakerGroupFlags.RadioAircraftSpawn;
                }
                else
                {
                    // Units shouldn't really move from pickup point if not escorted.
                    extraSettings.Remove("GroupX2");
                    extraSettings.Remove("GroupY2");
                    groupLua = Database.Instance.GetEntry<DBEntryObjectiveTargetBehavior>("Idle").GroupLua[(int)targetDB.DCSUnitCategory];
                }
            }

            if (
                objectiveTargetUnitFamily.GetUnitCategory().IsAircraft() &&
                !groupFlags.HasFlag(UnitMakerGroupFlags.RadioAircraftSpawn) &&
                !AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorDB.Location)
                )
            {
                if (task.ProgressionActivation)
                    groupFlags |= UnitMakerGroupFlags.ProgressionAircraftSpawn;
                else
                    groupFlags |= UnitMakerGroupFlags.ImmediateAircraftSpawn;
            }

            UnitMakerGroupInfo? targetGroupInfo = UnitMaker.AddUnitGroup(
                ref mission,
                units,
                taskDB.TargetSide,
                objectiveTargetUnitFamily,
                groupLua, luaUnit,
                unitCoordinates,
                groupFlags,
                extraSettings);

            if (!targetGroupInfo.HasValue) // Failed to generate target group
                throw new BriefingRoomException(mission.LangKey, "FailedToGenerateGroupObjective");

            if (mission.TemplateRecord.MissionFeatures.Contains("ContextScrambleStart") && !taskDB.UICategory.ContainsValue("Transport"))
                targetGroupInfo.Value.DCSGroup.LateActivation = false;

            if (task.ProgressionActivation)
            {
                targetGroupInfo.Value.DCSGroup.LateActivation = true;
                targetGroupInfo.Value.DCSGroup.Visible = task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable);
            }

            if (targetDB.UnitCategory.IsAircraft())
                targetGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Insert(0, new DCSWrappedWaypointTask("SetUnlimitedFuel", new Dictionary<string, object> { { "value", true } }));

            if (targetDB.UnitCategory == UnitCategory.Infantry && taskDB.UICategory.ContainsValue("Transport"))
            {
                var pos = unitCoordinates.CreateNearRandom(new MinMaxD(5, 50));
                targetGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Add(new DCSWaypointTask("EmbarkToTransport", new Dictionary<string, object>{
                    {"x", pos.X},
                    { "y", pos.Y},
                    {"zoneRadius", Database.Instance.Common.DropOffDistanceMeters}
                    }, _auto: false));

            }

            if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense) && (targetDB.UnitCategory == UnitCategory.Static))
                AddEmbeddedAirDefenseUnits(ref mission, targetDB, targetBehaviorDB, taskDB, objectiveCoordinates, groupFlags, extraSettings);

            targetGroupInfo.Value.DCSGroup.Waypoints = taskDB.IsEscort() || targetBehaviorDB.ID.Contains("OnRoad") || targetBehaviorDB.ID.Contains("Idle") ? targetGroupInfo.Value.DCSGroup.Waypoints : DCSWaypoint.CreateExtraWaypoints(ref mission, targetGroupInfo.Value.DCSGroup.Waypoints, targetGroupInfo.Value.UnitDB.Families.First());

            // Assign target suffix
            var i = 0;
            var isStatic = objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Static || objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            targetGroupInfo.Value.DCSGroups.ForEach(x =>
            {
                x.Name += $"{(i == 0 ? "" : i)}-TGT-{objectiveName}";
                if (isStatic) x.Units.ForEach(u => u.Name += $"{(i == 0 ? "" : i)}-TGT-{objectiveName}");
                i++;
            });
            mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{objectiveName}");
            var length = isStatic ? targetGroupInfo.Value.DCSGroups.Count : targetGroupInfo.Value.UnitNames.Length;
            var pluralIndex = length == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");
            CreateTaskString(ref mission, pluralIndex, ref taskString, objectiveName, objectiveTargetUnitFamily, task);
            CreateLua(ref mission, targetDB, taskDB, objectiveIndex, objectiveName, targetGroupInfo, taskString, task);

            // Add briefing remarks for this objective task
            var remarksString = taskDB.BriefingRemarks.Get(mission.LangKey);
            if (!string.IsNullOrEmpty(remarksString))
            {
                string remark = Toolbox.RandomFrom(remarksString.Split(";"));
                GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", objectiveName);
                GeneratorTools.ReplaceKey(ref remark, "DropOffDistanceMeters", Database.Instance.Common.DropOffDistanceMeters.ToString());
                GeneratorTools.ReplaceKey(ref remark, "UnitFamily", Database.Instance.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily].Get(mission.LangKey).Split(",")[pluralIndex]);
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
            }

            // Add feature ogg files
            foreach (string oggFile in taskDB.IncludeOgg)
                mission.AddMediaFile($"l10n/DEFAULT/{oggFile}", Path.Combine(BRPaths.INCLUDE_OGG, oggFile));


            // Add objective features Lua for this objective
            mission.AppendValue("ScriptObjectivesFeatures", ""); // Just in case there's no features
            var featureList = taskDB.RequiredFeatures.Concat(featuresID).ToHashSet();
            if (taskDB.IsEscort())
            {
                var playerHasPlanes = mission.TemplateRecord.PlayerFlightGroups.Any(x => Database.Instance.GetEntry<DBEntryJSONUnit>(x.Aircraft).Category == UnitCategory.Plane);
                switch (targetDB.UnitCategory)
                {
                    case UnitCategory.Plane:
                    case UnitCategory.Helicopter:
                        featureList.Add("HiddenEnemyCAPAttackingObj");
                        break;
                    case UnitCategory.Ship:
                        if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyCASAttackingObj"); }
                        if (Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyHeloAttackingObj"); }
                        if (Toolbox.RollChance(AmountNR.Low)) { featureList.Add("HiddenEnemyShipAttackingObj"); }
                        break;
                    default:
                        if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyCASAttackingObj"); }
                        if (Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyHeloAttackingObj"); }
                        if (Toolbox.RollChance(AmountNR.VeryHigh)) { featureList.Add("HiddenEnemyGroundAttackingObj"); }
                        break;
                }
            }
            foreach (string featureID in featureList)
                MissionGeneratorFeaturesObjectives.GenerateMissionFeature(ref mission, featureID, objectiveName, objectiveIndex, targetGroupInfo.Value, taskDB.TargetSide, objectiveOptions.Contains(ObjectiveOption.HideTarget), overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            mission.ObjectiveCoordinates.Add(isInverseTransportWayPoint ? unitCoordinates : objectiveCoordinates);
            var objCoords = objectiveCoordinates;
            var furthestWaypoint = targetGroupInfo.Value.DCSGroup.Waypoints.Aggregate(objectiveCoordinates, (furthest, x) => objCoords.GetDistanceFrom(x.Coordinates) > objCoords.GetDistanceFrom(furthest) ? x.Coordinates : furthest);
            var waypoint = GenerateObjectiveWaypoint(ref mission, task, objectiveCoordinates, furthestWaypoint, objectiveName, targetGroupInfo.Value.GroupID, hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            if (!targetGroupInfo.Value.UnitDB.IsAircraft)
                mission.MapData.Add($"UNIT-{targetGroupInfo.Value.UnitDB.Families[0]}-{taskDB.TargetSide}-{targetGroupInfo.Value.GroupID}", new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() });
            return objectiveWaypoints;
        }

        private static (string luaUnit, int unitCount, MinMaxI unitCountMinMax, List<UnitFamily> objectiveTargetUnitFamily, UnitMakerGroupFlags groupFlags) GetUnitData(MissionTemplateSubTaskRecord task, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, ObjectiveOption[] objectiveOptions)
        {
            UnitMakerGroupFlags groupFlags = 0;
            if (objectiveOptions.Contains(ObjectiveOption.Invisible)) groupFlags |= UnitMakerGroupFlags.Invisible;
            if (objectiveOptions.Contains(ObjectiveOption.ShowTarget)) groupFlags = UnitMakerGroupFlags.NeverHidden;
            else if (objectiveOptions.Contains(ObjectiveOption.HideTarget)) groupFlags = UnitMakerGroupFlags.AlwaysHidden;
            if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense)) groupFlags |= UnitMakerGroupFlags.EmbeddedAirDefense;
            return (targetBehaviorDB.UnitLua[(int)targetDB.DCSUnitCategory],
                targetDB.UnitCount[(int)task.TargetCount].GetValue(),
                targetDB.UnitCount[(int)task.TargetCount],
                targetDB.ID == "VehicleAny" ? Toolbox.RandomFrom(MIXED_VEHICLE_SETS) : [Toolbox.RandomFrom(targetDB.UnitFamilies)],
                groupFlags
            );
        }

        private static Coordinates PlaceInAirbase(ref DCSMission mission, Dictionary<string, object> extraSettings, DBEntryObjectiveTargetBehavior targetBehaviorDB, Coordinates objectiveCoordinates, int unitCount, DBEntryJSONUnit unitDB)
        {
            int airbaseID = 0;
            var parkingSpotIDsList = new List<int>();
            var parkingSpotCoordinatesList = new List<Coordinates>();
            var enemyCoalition = mission.TemplateRecord.ContextPlayerCoalition.GetEnemy();
            var playerAirbaseDCSID = mission.PlayerAirbase.DCSID;
            var spawnAnywhere = mission.TemplateRecord.SpawnAnywhere;
            var targetAirbaseOptions =
                (from DBEntryAirbase airbaseDB in mission.AirbaseDB
                 where airbaseDB.DCSID != playerAirbaseDCSID && (spawnAnywhere || airbaseDB.Coalition == enemyCoalition)
                 select airbaseDB).OrderBy(x => x.Coordinates.GetDistanceFrom(objectiveCoordinates));

            BriefingRoomException exception = null;
            foreach (var targetAirbase in targetAirbaseOptions)
            {
                try
                {
                    airbaseID = targetAirbase.DCSID;
                    var parkingSpots = UnitMakerSpawnPointSelector.GetFreeParkingSpots(
                        ref mission,
                        targetAirbase.DCSID,
                        unitCount, (DBEntryAircraft)unitDB,
                        targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.SpawnOnAirbaseParkingNoHardenedShelter);

                    parkingSpotIDsList = parkingSpots.Select(x => x.DCSID).ToList();
                    parkingSpotCoordinatesList = parkingSpots.Select(x => x.Coordinates).ToList();

                    extraSettings.Add("GroupAirbaseID", airbaseID);
                    extraSettings.Add("ParkingID", parkingSpotIDsList);
                    extraSettings.Add("UnitCoords", parkingSpotCoordinatesList);
                    return Toolbox.RandomFrom(parkingSpotCoordinatesList);
                }
                catch (BriefingRoomException e)
                {
                    exception = e;
                    throw;
                }
            }
            throw exception;
        }

        private static Coordinates GetSpawnCoordinates(ref DCSMission mission, Coordinates lastCoordinates, DBEntryAirbase playerAirbase, DBEntryObjectiveTarget targetDB, bool usingHint)
        {
            Coordinates? spawnPoint = UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                ref mission,
                targetDB.ValidSpawnPoints,
                playerAirbase.Coordinates,
                usingHint ? Toolbox.ANY_RANGE : mission.TemplateRecord.FlightPlanObjectiveDistance,
                lastCoordinates,
                usingHint ? Toolbox.HINT_RANGE : mission.TemplateRecord.FlightPlanObjectiveSeparation,
                GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Enemy));

            if (!spawnPoint.HasValue)
                throw new BriefingRoomException(mission.LangKey, "FailedToSpawnObjectiveGroup", String.Join(", ", targetDB.ValidSpawnPoints.Select(x => x.ToString()).ToList()));

            Coordinates objectiveCoordinates = spawnPoint.Value;
            return objectiveCoordinates;
        }

        internal static (string[] featuresID, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB, ObjectiveOption[] objectiveOptions) GetObjectiveData(string langKey, MissionTemplateObjectiveRecord objectiveTemplate)
        {
            var (targetDB, targetBehaviorDB, taskDB, objectiveOptions, presetDB) = GetCustomObjectiveData(langKey, objectiveTemplate);
            var featuresID = (objectiveTemplate.HasPreset ? presetDB.Features.Concat(objectiveTemplate.Features.ToArray()) : objectiveTemplate.Features).Distinct().ToArray();

            ObjectiveNullCheck(langKey, targetDB, targetBehaviorDB, taskDB);
            return (featuresID, targetDB, targetBehaviorDB, taskDB, objectiveOptions);
        }

        private static (DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB, ObjectiveOption[] objectiveOptions, DBEntryObjectivePreset presetDB) GetCustomObjectiveData(string langKey, MissionTemplateSubTaskRecord objectiveTemplate)
        {
            var targetDB = Database.Instance.GetEntry<DBEntryObjectiveTarget>(objectiveTemplate.Target);
            var targetBehaviorDB = Database.Instance.GetEntry<DBEntryObjectiveTargetBehavior>(objectiveTemplate.TargetBehavior);
            var taskDB = Database.Instance.GetEntry<DBEntryObjectiveTask>(objectiveTemplate.Task);
            var objectiveOptions = objectiveTemplate.Options.ToArray();
            DBEntryObjectivePreset presetDB = null;

            if (objectiveTemplate.HasPreset)
            {
                presetDB = Database.Instance.GetEntry<DBEntryObjectivePreset>(objectiveTemplate.Preset);
                if (presetDB != null)
                {
                    targetDB = Database.Instance.GetEntry<DBEntryObjectiveTarget>(Toolbox.RandomFrom(presetDB.Targets));
                    targetBehaviorDB = Database.Instance.GetEntry<DBEntryObjectiveTargetBehavior>(Toolbox.RandomFrom(presetDB.TargetsBehaviors));
                    taskDB = Database.Instance.GetEntry<DBEntryObjectiveTask>(presetDB.Task);
                    objectiveOptions = presetDB.Options.Concat(objectiveTemplate.Options).Distinct().ToArray();
                }
            }

            ObjectiveNullCheck(langKey, targetDB, targetBehaviorDB, taskDB);
            return (targetDB, targetBehaviorDB, taskDB, objectiveOptions, presetDB);
        }

        private static void ObjectiveNullCheck(string langKey, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB)
        {
            if (targetDB == null) throw new BriefingRoomException(langKey, "TargetNotFound", targetDB.UIDisplayName);
            if (targetBehaviorDB == null) throw new BriefingRoomException(langKey, "BehaviorNotFound", targetBehaviorDB.UIDisplayName);
            if (taskDB == null) throw new BriefingRoomException(langKey, "TaskNotFound", taskDB.UIDisplayName);
            if (!taskDB.ValidUnitCategories.Contains(targetDB.UnitCategory))
                throw new BriefingRoomException(langKey, "TaskTargetsInvalid", taskDB.UIDisplayName, targetDB.UnitCategory);
        }


        private static void AddEmbeddedAirDefenseUnits(ref DCSMission mission, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB, Coordinates objectiveCoordinates, UnitMakerGroupFlags groupFlags, Dictionary<string, object> extraSettings)
        {
            // Static targets (aka buildings) need to have their "embedded" air defenses spawned in another group
            var airDefenseUnits = GeneratorTools.GetEmbeddedAirDefenseUnits(mission.LangKey, mission.TemplateRecord, taskDB.TargetSide, UnitCategory.Static);

            if (airDefenseUnits.Count > 0)
                UnitMaker.AddUnitGroup(
                    ref mission,
                    airDefenseUnits,
                    taskDB.TargetSide, UnitFamily.VehicleAAA,
                    targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory], targetBehaviorDB.UnitLua[(int)targetDB.DCSUnitCategory],
                    objectiveCoordinates + Coordinates.CreateRandom(100, 500),
                    groupFlags,
                    extraSettings);
        }

        private static void CreateLua(ref DCSMission mission, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTask taskDB, int objectiveIndex, string objectiveName, UnitMakerGroupInfo? targetGroupInfo, string taskString, MissionTemplateSubTaskRecord task)
        {
            // Add Lua table for this objective
            string objectiveLua = $"briefingRoom.mission.objectives[{objectiveIndex + 1}] = {{ ";
            objectiveLua += $"complete = false, ";
            objectiveLua += $"failed = false, ";
            objectiveLua += $"groupName = \"{targetGroupInfo.Value.Name}\", ";
            objectiveLua += $"hideTargetCount = false, ";
            objectiveLua += $"name = \"{objectiveName}\", ";
            objectiveLua += $"targetCategory = Unit.Category.{targetDB.UnitCategory.ToLuaName()}, ";
            objectiveLua += $"taskType = \"{taskDB.ID}\", ";
            objectiveLua += $"task = \"{taskString}\", ";
            objectiveLua += $"unitsCount = #dcsExtensions.getUnitNamesByGroupNameSuffix(\"-TGT-{objectiveName}\"), ";
            objectiveLua += $"unitNames = dcsExtensions.getUnitNamesByGroupNameSuffix(\"-TGT-{objectiveName}\"), ";
            objectiveLua += $"progressionHidden = {(task.ProgressionActivation ? "true" : "false")},";
            objectiveLua += $"progressionHiddenBrief = {(task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief) ? "true" : "false")},";
            objectiveLua += $"progressionCondition = \"{(!string.IsNullOrEmpty(task.ProgressionOverrideCondition) ? task.ProgressionOverrideCondition : string.Join(task.ProgressionDependentIsAny ? " or " : " and ", task.ProgressionDependentTasks.Select(x => x  + 1).ToList()))}\", ";
            objectiveLua += $"f10MenuText = \"$LANG_OBJECTIVE$ {objectiveName}\",";
            objectiveLua += $"f10Commands = {{}}";
            objectiveLua += "}\n";

            // Add F10 sub-menu for this objective
            mission.AppendValue("ScriptObjectives", objectiveLua);

            // Add objective trigger Lua for this objective
            foreach (var CompletionTriggerLua in taskDB.CompletionTriggersLua)
            {
                string triggerLua = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_LUA_OBJECTIVETRIGGERS, CompletionTriggerLua));
                GeneratorTools.ReplaceKey(ref triggerLua, "ObjectiveIndex", objectiveIndex + 1);
                mission.AppendValue("ScriptObjectivesTriggers", triggerLua);
            }
        }

        private static void CreateTaskString(ref DCSMission mission, int pluralIndex, ref string taskString, string objectiveName, UnitFamily objectiveTargetUnitFamily, MissionTemplateSubTaskRecord task)
        {
            // Get tasking string for the briefing
            if (string.IsNullOrEmpty(taskString)) taskString = "Complete objective $OBJECTIVENAME$";
            GeneratorTools.ReplaceKey(ref taskString, "ObjectiveName", objectiveName);
            GeneratorTools.ReplaceKey(ref taskString, "UnitFamily", Database.Instance.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily].Get(mission.LangKey).Split(",")[pluralIndex]);
            if (!task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief))
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Task, taskString);
        }

        private static Waypoint GenerateObjectiveWaypoint(ref DCSMission mission, MissionTemplateSubTaskRecord objectiveTemplate, Coordinates objectiveCoordinates, Coordinates ObjectiveDestinationCoordinates, string objectiveName, int groupId = 0, bool scriptIgnore = false, bool hiddenMapMarker = false)
        {
            var (targetDB, targetBehaviorDB, taskDB, objectiveOptions, presetDB) = GetCustomObjectiveData(mission.LangKey, objectiveTemplate);
            var targetBehaviorLocation = targetBehaviorDB.Location;
            if (targetDB == null) throw new BriefingRoomException(mission.LangKey, "TargetNotFound", targetDB.UIDisplayName);

            Coordinates waypointCoordinates = objectiveCoordinates;
            bool onGround = !targetDB.UnitCategory.IsAircraft() || AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorLocation); // Ground targets = waypoint on the ground

            if (objectiveOptions.Contains(ObjectiveOption.InaccurateWaypoint) && (!taskDB.UICategory.ContainsValue("Transport") || objectiveName.EndsWith("Pickup")))
            {
                waypointCoordinates += Coordinates.CreateRandom(3.0, 6.0) * Toolbox.NM_TO_METERS;
                if (mission.TemplateRecord.OptionsMission.Contains("MarkWaypoints"))
                    DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", DrawingType.Circle, waypointCoordinates, "Radius".ToKeyValuePair(6.0 * Toolbox.NM_TO_METERS));
            }
            else if (taskDB.UICategory.ContainsValue("Transport"))
                DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", DrawingType.Circle, waypointCoordinates, "Radius".ToKeyValuePair(Database.Instance.Common.DropOffDistanceMeters));
            else if (targetBehaviorLocation == DBEntryObjectiveTargetBehaviorLocation.Patrolling)
                DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", DrawingType.Circle, waypointCoordinates, "Radius".ToKeyValuePair(ObjectiveDestinationCoordinates.GetDistanceFrom(objectiveCoordinates)));
            return new Waypoint(objectiveName, waypointCoordinates, onGround, groupId, scriptIgnore, objectiveTemplate.Options.Contains(ObjectiveOption.NoAircraftWaypoint), hiddenMapMarker);
        }

        //----------------SUB TASK SUPPORT FUNCTIONS-------------------------------

        private static Coordinates GetNearestSpawnCoordinates(ref DCSMission mission, Coordinates coreCoordinates, DBEntryObjectiveTarget targetDB, bool remove = true)
        {
            Coordinates? spawnPoint = UnitMakerSpawnPointSelector.GetNearestSpawnPoint(
                ref mission,
                targetDB.ValidSpawnPoints,
                coreCoordinates, remove);

            if (!spawnPoint.HasValue)
                throw new BriefingRoomException(mission.LangKey, "FailedToLaunchNearbyObjective", String.Join(",", targetDB.ValidSpawnPoints.Select(x => x.ToString()).ToList()));

            Coordinates objectiveCoordinates = spawnPoint.Value;
            return objectiveCoordinates;
        }
    }
}
