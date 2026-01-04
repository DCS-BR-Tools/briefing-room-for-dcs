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
    internal class Transport
    {
        internal static List<Waypoint> CreateObjective(
            IBriefingRoom briefingRoom,
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
            (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, objectiveTargetUnitFamilies, unitCount, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic);
            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            if (units.Count == 0 || unitDBs.Count == 0)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "NoUnitsForTimePeriod", taskDB.TargetSide, objectiveTargetUnitFamily);
            var unitDB = unitDBs.First();

            var (originAirbase, unitCoordinates) = ObjectiveUtils.GetTransportOrigin(briefingRoom, ref mission, targetBehaviorDB.Location, objectiveCoordinates);
            var (_, destinationPoint) = ObjectiveUtils.GetTransportDestination(briefingRoom, ref mission, targetBehaviorDB.Location, targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, task.TransportDistance, originAirbase?.DCSID ?? -1);
            objectiveCoordinates = destinationPoint;

            extraSettings.Add("playerCanDrive", false);

            GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
                ref mission,
                units,
                taskDB.TargetSide,
                objectiveTargetUnitFamily,
                briefingRoom.Database.GetEntry<DBEntryObjectiveTargetBehavior>("Idle").GroupLua[(int)targetDB.DCSUnitCategory], luaUnit,
                unitCoordinates,
                groupFlags,
                extraSettings);

            if (!targetGroupInfo.HasValue) // Failed to generate target group
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToGenerateGroupObjective");
            if (task.ProgressionActivation)
            {
                targetGroupInfo.Value.DCSGroups.ForEach((grp) =>
                {
                    grp.LateActivation = true;
                    grp.Visible = task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable);
                });
            }

            if (targetDB.UnitCategory == UnitCategory.Infantry)
            {
                var pos = unitCoordinates.CreateNearRandom(new MinMaxD(5, 50));
                targetGroupInfo.Value.DCSGroup.Waypoints.First().Tasks.Add(new DCSWaypointTask("EmbarkToTransport", new Dictionary<string, object>{
                    {"x", pos.X},
                    { "y", pos.Y},
                    {"zoneRadius", briefingRoom.Database.Common.DropOffDistanceMeters}
                    }, _auto: false));

            }
            mission.ObjectiveCoordinates.Add(objectiveCoordinates);


            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            var objectiveWaypoints = new List<Waypoint>();

            var cargoWaypoint = ObjectiveUtils.GenerateObjectiveWaypoint(briefingRoom.Database, ref mission, task, unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", scriptIgnore: true);
            mission.Waypoints.Add(cargoWaypoint);
            objectiveWaypoints.Add(cargoWaypoint);

            mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{objectiveName}");
            var isStatic = objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, objectiveName, isStatic);
            if (isStatic)
            {
                var zoneId = ZoneMaker.AddZone(ref mission, $"Cargo {objectiveName} DZ Trigger", objectiveCoordinates, briefingRoom.Database.Common.DropOffDistanceMeters);
                foreach (var group in targetGroupInfo.Value.DCSGroups)
                    TriggerMaker.AddCargoTrigger(ref mission, zoneId, group.Units[0].UnitId, group.Units[0].Name, objectiveIndex);
            } else
            {
                var zoneId = ZoneMaker.AddZone(ref mission, $"Troops {objectiveName} DZ Trigger", objectiveCoordinates, briefingRoom.Database.Common.DropOffDistanceMeters);
                foreach (var group in targetGroupInfo.Value.DCSGroups)
                    foreach (var unit in group.Units)
                        TriggerMaker.AddTransportTrigger(ref mission, zoneId, unit.UnitId, unit.Name, objectiveIndex);
            }
            var length = isStatic ? targetGroupInfo.Value.DCSGroups.Count : targetGroupInfo.Value.UnitNames.Length;
            var pluralIndex = length == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");

            var luaExtraSettings = new Dictionary<string, object>();
            var unitDisplayName = targetGroupInfo.Value.UnitDB.UIDisplayName;
            ObjectiveUtils.CreateTaskString(briefingRoom.Database, ref mission, pluralIndex, ref taskString, objectiveName, objectiveTargetUnitFamily, unitDisplayName, task, luaExtraSettings);
            ObjectiveUtils.CreateLua(ref mission, targetDB, taskDB, objectiveIndex, objectiveName, targetGroupInfo, taskString, task, luaExtraSettings);

            // Add briefing remarks for this objective task
            var remarksString = taskDB.BriefingRemarks.Get(mission.LangKey);
            if (!string.IsNullOrEmpty(remarksString))
            {
                string remark = Toolbox.RandomFrom(remarksString.Split(";"));
                GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", objectiveName);
                GeneratorTools.ReplaceKey(ref remark, "DropOffDistanceMeters", briefingRoom.Database.Common.DropOffDistanceMeters.ToString());
                GeneratorTools.ReplaceKey(ref remark, "UnitFamily", briefingRoom.Database.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily].Get(mission.LangKey).Split(",")[pluralIndex]);
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
            }

            // Add feature ogg files
            foreach (string oggFile in taskDB.IncludeOgg)
                mission.AddMediaFile($"{BRPaths.MIZ_RESOURCES_OGG}{oggFile}", Path.Combine(BRPaths.INCLUDE_OGG, oggFile));


            // Add objective features Lua for this objective
            mission.AppendValue("ScriptObjectivesFeatures", ""); // Just in case there's no features

            foreach (string featureID in taskDB.RequiredFeatures.Concat(featuresID).ToHashSet())
                FeaturesObjectives.GenerateMissionFeature( briefingRoom, ref mission, featureID, objectiveName, objectiveIndex, targetGroupInfo.Value, taskDB.TargetSide, objectiveOptions, overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            var objCoords = objectiveCoordinates;
            var furthestWaypoint = targetGroupInfo.Value.DCSGroup.Waypoints.Aggregate(objectiveCoordinates, (furthest, x) => objCoords.GetDistanceFrom(x.Coordinates) > objCoords.GetDistanceFrom(furthest) ? x.Coordinates : furthest);
            var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(briefingRoom.Database, ref mission, task, objectiveCoordinates, furthestWaypoint, objectiveName, targetGroupInfo.Value.DCSGroups.Select(x => x.GroupId).ToList(), hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            if (!targetGroupInfo.Value.UnitDB.IsAircraft)
                mission.MapData.Add($"UNIT-{targetGroupInfo.Value.UnitDB.Families[0]}-{taskDB.TargetSide}-{targetGroupInfo.Value.GroupID}", new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() });
            return objectiveWaypoints;

        }
    }
}