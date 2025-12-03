using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    internal class Hold
    {

        internal static List<Waypoint> CreateObjective(
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
            List<string> units = [];
            List<DBEntryJSONUnit> unitDBs = [];
            var (luaUnit, unitCount, unitCountMinMax, objectiveTargetUnitFamilies, groupFlags) = ObjectiveUtils.GetUnitData(task, targetDB, targetBehaviorDB, objectiveOptions);
            if (Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Any(x => objectiveTargetUnitFamilies.Contains(x)) && targetBehaviorDB.IsStatic)
            {
                var locationType = Toolbox.RandomFrom(Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Intersect(objectiveTargetUnitFamilies).Select(x => Constants.THEATER_TEMPLATE_LOCATION_MAP[x]).ToList());
                var templateLocation = SpawnPointSelector.GetNearestTemplateLocation(ref mission, locationType, objectiveCoordinates, true);
                if (templateLocation.HasValue)
                {
                    objectiveCoordinates = templateLocation.Value.Coordinates;
                    (units, unitDBs) = UnitGenerator.GetUnitsForTemplateLocation(ref mission, templateLocation.Value, taskDB.TargetSide, objectiveTargetUnitFamilies, ref extraSettings);
                    if (units.Count == 0)
                        SpawnPointSelector.RecoverTemplateLocation(ref mission, templateLocation.Value.Coordinates);
                }
            }

            var isInverseTransportWayPoint = false;
            if (units.Count == 0)
                (units, unitDBs) = UnitGenerator.GetUnits(ref mission, objectiveTargetUnitFamilies, unitCount, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic);
            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            if (units.Count == 0 || unitDBs.Count == 0)
                throw new BriefingRoomException(mission.LangKey, "NoUnitsForTimePeriod", taskDB.TargetSide, objectiveTargetUnitFamily);
            var unitDB = unitDBs.First();
            if (Constants.AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && targetDB.UnitCategory.IsAircraft())
                objectiveCoordinates = ObjectiveUtils.PlaceInAirbase(ref mission, extraSettings, targetBehaviorDB, objectiveCoordinates, unitCount, unitDB);

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
                destinationPoint = ObjectiveUtils.GetNearestSpawnCoordinates(ref mission, destinationPoint, targetDB.ValidSpawnPoints, false);


            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            if (targetBehaviorDB.Destination == DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase)
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
            else if (targetBehaviorDB.Destination == DBEntryObjectiveTargetBehaviorLocation.Airbase)
            {
                var targetCoalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, taskDB.TargetSide, forceSide: true);
                var destinationAirbase = mission.AirbaseDB.Where(x => x.Coalition == targetCoalition.Value).OrderBy(x => destinationPoint.GetDistanceFrom(x.Coordinates)).First();
                destinationPoint = destinationAirbase.Coordinates;
                extraSettings.Add("EndAirbaseId", destinationAirbase.DCSID);
                mission.PopulatedAirbaseIds[targetCoalition.Value].Add(destinationAirbase.DCSID);
            }
            else if (groupLua.StartsWith("AircraftOrbiting"))
            {
                destinationPoint = objectiveCoordinates;
            }

            if (!targetBehaviorDB.IsStatic)
            {
                extraSettings.Add("GroupX2", destinationPoint.X);
                extraSettings.Add("GroupY2", destinationPoint.Y);
            }

            extraSettings.Add("playerCanDrive", false);

            var unitCoordinates = objectiveCoordinates;
            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            var objectiveWaypoints = new List<Waypoint>();
            if (taskDB.UICategory.ContainsValue("Transport"))
            {
                if (targetBehaviorDB.ID.StartsWith("RelocateToNewPosition"))
                {
                    Coordinates? spawnPoint = SpawnPointSelector.GetRandomSpawnPoint(
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
                    var coords = targetBehaviorDB.Destination == DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase ? mission.PlayerAirbase.Coordinates : unitCoordinates;
                    var (_, _, spawnPoints) = SpawnPointSelector.GetAirbaseAndParking(mission, coords, 1, GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Ally, true).Value, (DBEntryAircraft)Database.Instance.GetEntry<DBEntryJSONUnit>("Mi-8MT"));
                    if (spawnPoints.Count == 0) // Failed to generate target group
                        throw new BriefingRoomException(mission.LangKey, "FailedToFindCargoSpawn");
                    unitCoordinates = spawnPoints.First();
                }
                if (targetBehaviorDB.ID.StartsWith("RecoverToBase") || (taskDB.IsEscort() & !targetBehaviorDB.ID.StartsWith("ToFrontLine")))
                {
                    (unitCoordinates, objectiveCoordinates) = (objectiveCoordinates, unitCoordinates);
                    isInverseTransportWayPoint = true;
                }
                var cargoWaypoint = ObjectiveUtils.GenerateObjectiveWaypoint(ref mission, task, unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", scriptIgnore: true);
                mission.Waypoints.Add(cargoWaypoint);
                objectiveWaypoints.Add(cargoWaypoint);
                
                // Units shouldn't really move from pickup point if not escorted.
                extraSettings.Remove("GroupX2");
                extraSettings.Remove("GroupY2");
                groupLua = Database.Instance.GetEntry<DBEntryObjectiveTargetBehavior>("Idle").GroupLua[(int)targetDB.DCSUnitCategory];
    
            }

            if (
                objectiveTargetUnitFamily.GetUnitCategory().IsAircraft() &&
                !groupFlags.HasFlag(GroupFlags.RadioAircraftSpawn) &&
                !Constants.AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorDB.Location)
                )
            {
                if (task.ProgressionActivation)
                    groupFlags |= GroupFlags.ProgressionAircraftSpawn;
                else
                    groupFlags |= GroupFlags.ImmediateAircraftSpawn;
            }

            GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
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
                targetGroupInfo.Value.DCSGroups.ForEach((grp) =>
                {
                    grp.LateActivation = true;
                    grp.Visible = task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable);
                });
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
                ObjectiveUtils.AddEmbeddedAirDefenseUnits(ref mission, targetDB, targetBehaviorDB, taskDB, objectiveCoordinates, groupFlags, extraSettings);

            targetGroupInfo.Value.DCSGroup.Waypoints = taskDB.IsEscort() || targetBehaviorDB.ID.Contains("OnRoad") || targetBehaviorDB.ID.Contains("Idle") ? targetGroupInfo.Value.DCSGroup.Waypoints : DCSWaypoint.CreateExtraWaypoints(ref mission, targetGroupInfo.Value.DCSGroup.Waypoints, targetGroupInfo.Value.UnitDB.Families.First());

            var isStatic = objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Static || objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, objectiveName, isStatic);
            var luaExtraSettings = new Dictionary<string, object>();
            if (task.Task.StartsWith("Hold"))
            {
                var (holdSizeMeters, holdTimeSeconds) = GetHoldValues(task.TargetCount);
                luaExtraSettings.Add("HoldSize", holdSizeMeters);
                luaExtraSettings.Add("HoldSizeNm", (int)Math.Floor(holdSizeMeters * Toolbox.METERS_TO_NM));
                luaExtraSettings.Add("HoldTime", holdTimeSeconds);
                luaExtraSettings.Add("HoldTimeMins", holdTimeSeconds / 60);
                if (mission.TemplateRecord.OptionsMission.Contains("MarkWaypoints"))
                    DrawingMaker.AddDrawing(ref mission, $"Hold Zone {objectiveName}", DrawingType.Circle, objectiveCoordinates, "Radius".ToKeyValuePair(holdSizeMeters));
            }
            if (task.Task == "CaptureLocation")
            {
                if (!extraSettings.ContainsKey("GroupAirbaseID"))
                    throw new BriefingRoomException(mission.LangKey, "CaptureLocationNoAirbase");

                luaExtraSettings.Add("GroupAirbaseID", extraSettings.GetValueOrDefault("GroupAirbaseID"));
            }

            mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{objectiveName}");
            var length = isStatic ? targetGroupInfo.Value.DCSGroups.Count : targetGroupInfo.Value.UnitNames.Length;
            var pluralIndex = length == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");
            var unitDisplayName = targetGroupInfo.Value.UnitDB.UIDisplayName;
            ObjectiveUtils.CreateTaskString(ref mission, pluralIndex, ref taskString, objectiveName, objectiveTargetUnitFamily, unitDisplayName, task, luaExtraSettings);
            ObjectiveUtils.CreateLua(ref mission, targetDB, taskDB, objectiveIndex, objectiveName, targetGroupInfo, taskString, task, luaExtraSettings);

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
            var playerHasPlanes = mission.TemplateRecord.PlayerFlightGroups.Any(x => Database.Instance.GetEntry<DBEntryJSONUnit>(x.Aircraft).Category == UnitCategory.Plane) || mission.TemplateRecord.AirbaseDynamicSpawn != DsAirbase.None;

            if (taskDB.IsHoldSuperiority())
                SetHoldSuperiorityFeatures(targetDB, ref featureList, playerHasPlanes);

            foreach (string featureID in featureList)
                FeaturesObjectives.GenerateMissionFeature(ref mission, featureID, objectiveName, objectiveIndex, targetGroupInfo.Value, taskDB.TargetSide, objectiveOptions, overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            mission.ObjectiveCoordinates.Add(isInverseTransportWayPoint ? unitCoordinates : objectiveCoordinates);
            var objCoords = objectiveCoordinates;
            var furthestWaypoint = targetGroupInfo.Value.DCSGroup.Waypoints.Aggregate(objectiveCoordinates, (furthest, x) => objCoords.GetDistanceFrom(x.Coordinates) > objCoords.GetDistanceFrom(furthest) ? x.Coordinates : furthest);
            var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(ref mission, task, objectiveCoordinates, furthestWaypoint, objectiveName, targetGroupInfo.Value.DCSGroups.Select(x => x.GroupId).ToList(), hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            if (!targetGroupInfo.Value.UnitDB.IsAircraft)
                mission.MapData.Add($"UNIT-{targetGroupInfo.Value.UnitDB.Families[0]}-{taskDB.TargetSide}-{targetGroupInfo.Value.GroupID}", new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() });
            return objectiveWaypoints;

        }

        private static void SetHoldSuperiorityFeatures(DBEntryObjectiveTarget targetDB, ref HashSet<string> featureList, bool playerHasPlanes)
        {
            switch (targetDB.UnitCategory)
            {
                case UnitCategory.Plane:
                    featureList.Add("HiddenEnemyCAPAttackingObj");
                    break;
                case UnitCategory.Helicopter:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyCASAttackingObj"); }
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyCAPAttackingObj"); }
                    if (Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyHeloAttackingObj"); }
                    break;
                case UnitCategory.Ship:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyCASAttackingObj"); }
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyCAPAttackingObj"); }
                    if (Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyHeloAttackingObj"); }
                    if (Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyShipAttackingObj"); }
                    break;
                default:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { featureList.Add("HiddenEnemyCASAttackingObj"); }
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyCAPAttackingObj"); }
                    if (Toolbox.RollChance(AmountNR.Average)) { featureList.Add("HiddenEnemyHeloAttackingObj"); }
                    if (Toolbox.RollChance(AmountNR.VeryHigh)) { featureList.Add("HiddenEnemyGroundAttackingObj"); }
                    break;
            }
        }

        private static Tuple<int, int> GetHoldValues(Amount targetAmount) =>
       new(
           (int)Math.Floor(Database.Instance.Common.HoldSizesInNauticalMiles[targetAmount].GetValue() * Toolbox.NM_TO_METERS),
           Database.Instance.Common.HoldTimesInMinutes[targetAmount].GetValue() * 60
       );
    }
}