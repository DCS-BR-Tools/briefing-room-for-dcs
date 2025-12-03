// ESCORT MISSION OBJECTIVES
// Escort a group of units from A to B with potential threats along the way

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    internal class Escort
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

            (units, unitDBs) = UnitGenerator.GetUnits(ref mission, objectiveTargetUnitFamilies, unitCount, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic);
            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            if (units.Count == 0 || unitDBs.Count == 0)
                throw new BriefingRoomException(mission.LangKey, "NoUnitsForTimePeriod", taskDB.TargetSide, objectiveTargetUnitFamily);
            var unitDB = unitDBs.First();
            if (Constants.AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && targetDB.UnitCategory.IsAircraft())
                objectiveCoordinates = ObjectiveUtils.PlaceInAirbase(ref mission, extraSettings, targetBehaviorDB, objectiveCoordinates, unitCount, unitDB);

            var (originAirbaseId, unitCoordinates) = ObjectiveUtils.GetTransportOrigin(ref mission, targetBehaviorDB.Location, objectiveCoordinates, true, objectiveTargetUnitFamily.GetUnitCategory());
            var (airbaseId, destinationPoint) = ObjectiveUtils.GetTransportDestination(ref mission, targetBehaviorDB.Destination, unitCoordinates, task.TransportDistance, originAirbaseId, true, objectiveTargetUnitFamily.GetUnitCategory(), targetBehaviorDB.ID.StartsWith("ToFrontLine"));
            extraSettings.Add("EndAirbaseId", airbaseId);
            objectiveCoordinates = destinationPoint;

            extraSettings.Add("playerCanDrive", false);

            extraSettings["GroupX2"] = objectiveCoordinates.X;
            extraSettings["GroupY2"] = objectiveCoordinates.Y;
            groupFlags |= GroupFlags.RadioAircraftSpawn;


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
            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            if(airbaseId > 0 && mission.AirbaseDB.Find(x => x.DCSID == airbaseId).Coalition == GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Enemy, true) &&  objectiveTargetUnitFamily.GetUnitCategory().IsAircraft())
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
            GroupInfo? VIPGroupInfo = UnitGenerator.AddUnitGroup(
                ref mission,
                units,
                taskDB.TargetSide,
                objectiveTargetUnitFamily,
                groupLua, luaUnit,
                unitCoordinates,
                groupFlags,
                extraSettings);

            if (!VIPGroupInfo.HasValue) // Failed to generate target group
                throw new BriefingRoomException(mission.LangKey, "FailedToGenerateGroupObjective");


            VIPGroupInfo.Value.DCSGroups.ForEach((grp) =>
            {
                grp.LateActivation = true;
                grp.Visible = task.ProgressionActivation ? task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable) : true;
            });


            if (targetDB.UnitCategory.IsAircraft())
                VIPGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Insert(0, new DCSWrappedWaypointTask("SetUnlimitedFuel", new Dictionary<string, object> { { "value", true } }));
            // Setup Threats
            // Assume static threats just exist for now
            var playerHasPlanes = mission.TemplateRecord.PlayerFlightGroups.Any(x => Database.Instance.GetEntry<DBEntryJSONUnit>(x.Aircraft).Category == UnitCategory.Plane) || mission.TemplateRecord.AirbaseDynamicSpawn != DsAirbase.None;
            switch (targetDB.UnitCategory)
            {
                case UnitCategory.Plane:
                case UnitCategory.Helicopter:
                    CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAP");
                    break;
                case UnitCategory.Ship:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAS"); }
                    if (Toolbox.RollChance(AmountNR.Average)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Helo"); }
                    if (Toolbox.RollChance(AmountNR.Low)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Ship"); }
                    break;
                default:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAS"); }
                    if (Toolbox.RollChance(AmountNR.Average)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Helo"); }
                    if (Toolbox.RollChance(AmountNR.VeryHigh)) { CreateThreat(ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Ground"); }
                    break;
            }




            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            if(targetBehaviorDB.ID.EndsWith("OnRoads") && targetDB.UnitCategory.IsGroundMoving())
            {
              BriefingRoom.PrintTranslatableWarning(mission.LangKey, "EscortOnRoadsImperfect", objectiveName);
            }

            var objectiveWaypoints = new List<Waypoint>();
            var cargoWaypoint = ObjectiveUtils.GenerateObjectiveWaypoint(ref mission, task, unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", scriptIgnore: true);
            mission.Waypoints.Add(cargoWaypoint);
            objectiveWaypoints.Add(cargoWaypoint);
            ObjectiveUtils.AssignTargetSuffix(ref VIPGroupInfo, objectiveName, false);
            var luaExtraSettings = new Dictionary<string, object>();
            mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{objectiveName}");
            var length = VIPGroupInfo.Value.UnitNames.Length;
            var pluralIndex = length == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");
            var unitDisplayName = VIPGroupInfo.Value.UnitDB.UIDisplayName;
            ObjectiveUtils.CreateTaskString(ref mission, pluralIndex, ref taskString, objectiveName, objectiveTargetUnitFamily, unitDisplayName, task, luaExtraSettings);
            ObjectiveUtils.CreateLua(ref mission, targetDB, taskDB, objectiveIndex, objectiveName, VIPGroupInfo, taskString, task, luaExtraSettings);

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
            // SetEscortFeatures(targetDB, ref featureList, playerHasPlanes);


            foreach (string featureID in featureList)
                FeaturesObjectives.GenerateMissionFeature(ref mission, featureID, objectiveName, objectiveIndex, VIPGroupInfo.Value, taskDB.TargetSide, objectiveOptions, overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            mission.ObjectiveCoordinates.Add(objectiveCoordinates);
            var zoneId = ZoneMaker.AddZone(ref mission, $"Escort End Zone {objectiveName}", objectiveCoordinates, VIPGroupInfo.Value.UnitDB.IsAircraft ? Database.Instance.Common.DropOffDistanceMeters * 10 : Database.Instance.Common.DropOffDistanceMeters);
            TriggerMaker.AddEscortEndTrigger(ref mission, zoneId, VIPGroupInfo.Value.GroupID, objectiveIndex);
            var objCoords = objectiveCoordinates;
            var furthestWaypoint = VIPGroupInfo.Value.DCSGroup.Waypoints.Aggregate(objectiveCoordinates, (furthest, x) => objCoords.GetDistanceFrom(x.Coordinates) > objCoords.GetDistanceFrom(furthest) ? x.Coordinates : furthest);
            var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(ref mission, task, objectiveCoordinates, furthestWaypoint, objectiveName, VIPGroupInfo.Value.DCSGroups.Select(x => x.GroupId).ToList(), hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            if (!VIPGroupInfo.Value.UnitDB.IsAircraft)
                mission.MapData.Add($"UNIT-{VIPGroupInfo.Value.UnitDB.Families[0]}-{taskDB.TargetSide}-{VIPGroupInfo.Value.GroupID}", new List<double[]> { VIPGroupInfo.Value.Coordinates.ToArray() });
            return objectiveWaypoints;

        }

        private static void CreateThreat(ref DCSMission mission, Coordinates unitCoordinates, Coordinates objectiveCoordinates, GroupInfo? VIPGroupInfo, string type)

        {
            var (threatMinMax, threatUnitFamilies, groupLua, unitLua, unitCount, validSpawns, spawnDistance) = getThreatValues(type);
            var threatExtraSettings = new Dictionary<string, object>
            {
                { "ObjectiveGroupID", VIPGroupInfo.Value.GroupID }
            };
            var zoneCoords = Coordinates.Lerp(unitCoordinates, objectiveCoordinates, threatMinMax.GetValue());
            threatExtraSettings["GroupX2"] = zoneCoords.X;
            threatExtraSettings["GroupY2"] = zoneCoords.Y;
            var groupFlags = GroupFlags.RadioAircraftSpawn;
            var (threatUnits, threatUnitDBs) = UnitGenerator.GetUnits(ref mission, threatUnitFamilies, unitCount.GetValue(), Side.Enemy, groupFlags, ref threatExtraSettings, false);
            var spawnPoint = SpawnPointSelector.GetRandomSpawnPoint(ref mission, validSpawns, zoneCoords, spawnDistance, coalition: GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Enemy));
            if (!spawnPoint.HasValue || threatUnits.Count == 0 || threatUnitDBs.Count == 0)
            {
                BriefingRoom.PrintToLog($"Failed to create threat for escort mission objective at {zoneCoords}. No valid spawn point or units found.");
                return;
            }
            GroupInfo? threatGroupInfo = UnitGenerator.AddUnitGroup(
                ref mission,
                threatUnits,
                Side.Enemy,
                threatUnitDBs.First().Families.First(),
                groupLua, unitLua,
                spawnPoint.Value,
                GroupFlags.RadioAircraftSpawn,
                threatExtraSettings);
            var zoneId = ZoneMaker.AddZone(ref mission, $"Threat Trig {threatGroupInfo.Value.Name} attacking {VIPGroupInfo.Value.Name}", zoneCoords, VIPGroupInfo.Value.UnitDB.Category.IsAircraft() ? 3000 : 1524);
            TriggerMaker.AddEscortThreatTrigger(ref mission, zoneId, VIPGroupInfo.Value.GroupID, threatGroupInfo.Value.GroupID);
        }

        private static Tuple<MinMaxD, List<UnitFamily>, string, string, MinMaxI, SpawnPointType[], MinMaxD> getThreatValues(string type)
        {
            switch (type)
            {
                case "CAP":
                    return Tuple.Create(
                        new MinMaxD(0.1, 0.9),
                        new List<UnitFamily> { UnitFamily.PlaneFighter, UnitFamily.PlaneInterceptor },
                        "AircraftCAPAttacking",
                        "Aircraft",
                        new MinMaxI(1, 4),
                        new[] { SpawnPointType.Air },
                        new MinMaxD(20, 60));
                case "CAS":
                    return Tuple.Create(
                        new MinMaxD(0.15, 0.8),
                        new List<UnitFamily> { UnitFamily.PlaneAttack, UnitFamily.PlaneStrike },
                        "AircraftCASAttacking",
                        "Aircraft",
                        new MinMaxI(1, 4),
                        new[] { SpawnPointType.Air },
                        new MinMaxD(10, 40));
                case "Helo":
                    return Tuple.Create(
                        new MinMaxD(0.02, 0.7),
                        new List<UnitFamily> { UnitFamily.HelicopterAttack },
                        "AircraftCASAttacking",
                        "Aircraft",
                        new MinMaxI(1, 4),
                        new[] { SpawnPointType.Air },
                        new MinMaxD(10, 20));
                case "Ship":
                    return Tuple.Create(
                        new MinMaxD(0.05, 0.6),
                        new List<UnitFamily> { UnitFamily.ShipCruiser, UnitFamily.ShipFrigate, UnitFamily.ShipSpeedboat, UnitFamily.ShipSubmarine },
                        "ShipAttacking",
                        "Ship",
                        new MinMaxI(1, 4),
                        new[] { SpawnPointType.Sea },
                        new MinMaxD(30, 60));
                case "Ground":
                default:
                    return Tuple.Create(
                        new MinMaxD(0.05, 0.7),
                        new List<UnitFamily> { UnitFamily.VehicleAPC, UnitFamily.VehicleMBT, UnitFamily.Infantry },
                        "VehicleAttackingUncontrolled",
                        "Vehicle",
                        new MinMaxI(1, 10),
                        new[] { SpawnPointType.LandMedium, SpawnPointType.LandLarge },
                        new MinMaxD(5, 20));
            }
        }
    }
}