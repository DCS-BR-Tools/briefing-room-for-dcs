using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
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
            var ctx = new ObjectiveContext
            {
                BriefingRoom = briefingRoom,
                Task = task,
                TaskDB = taskDB,
                TargetDB = targetDB,
                TargetBehaviorDB = targetBehaviorDB,
                ObjectiveOptions = objectiveOptions,
                Mission = mission,
                FeaturesID = featuresID,
                ObjectiveIndex = objectiveIndex,
                ObjectiveCoordinates = objectiveCoordinates
            };
            ctx.InitializeUnitData();

            // Get units with validation
            ObjectiveCreationHelpers.GetUnitsWithValidation(ctx);

            // Get transport origin and destination
            var (originAirbase, unitCoordinates) = ObjectiveTransportUtils.GetTransportOrigin(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, objectiveCoordinates);
            var (_, destinationPoint) = ObjectiveTransportUtils.GetTransportDestination(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, task.TransportDistance, originAirbase?.DCSID ?? -1);
            ctx.ObjectiveCoordinates = destinationPoint;
            ctx.UnitCoordinates = unitCoordinates;

            ctx.ExtraSettings.Add("playerCanDrive", false);

            GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
                ref ctx.Mission,
                ctx.Units,
                taskDB.TargetSide,
                ctx.ObjectiveTargetUnitFamily,
                briefingRoom.Database.GetEntry<DBEntryObjectiveTargetBehavior>("Idle").GroupLua[(int)targetDB.DCSUnitCategory], 
                ctx.LuaUnit,
                unitCoordinates,
                ctx.GroupFlags,
                ctx.ExtraSettings);

            if (!targetGroupInfo.HasValue)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToGenerateGroupObjective");

            ObjectiveCreationHelpers.ApplyProgressionSettings(ctx, targetGroupInfo.Value);
            ObjectiveCreationHelpers.AddEmbarkTask(ctx, targetGroupInfo.Value, unitCoordinates);

            ctx.Mission.ObjectiveCoordinates.Add(ctx.ObjectiveCoordinates);

            ctx.ObjectiveName = ctx.Mission.WaypointNameGenerator.GetWaypointName();

            // Add pickup waypoint
            var cargoWaypoint = ObjectiveCreationHelpers.GenerateObjectiveWaypoint(briefingRoom.Database, ref ctx.Mission, task, unitCoordinates, unitCoordinates, $"{ctx.ObjectiveName} Pickup", scriptIgnore: true);
            ctx.Mission.Waypoints.Add(cargoWaypoint);
            ctx.ObjectiveWaypoints.Add(cargoWaypoint);

            ctx.Mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{ctx.ObjectiveName}");
            var isStatic = ctx.ObjectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, ctx.ObjectiveName, isStatic);
            
            // Add triggers based on cargo type
            if (isStatic)
            {
                var zoneId = ZoneMaker.AddZone(ref ctx.Mission, $"Cargo {ctx.ObjectiveName} DZ Trigger", ctx.ObjectiveCoordinates, briefingRoom.Database.Common.DropOffDistanceMeters);
                foreach (var group in targetGroupInfo.Value.DCSGroups)
                    TriggerMaker.AddCargoTrigger(ref ctx.Mission, zoneId, group.Units[0].UnitId, group.Units[0].Name, objectiveIndex);
            }
            else
            {
                var zoneId = ZoneMaker.AddZone(ref ctx.Mission, $"Troops {ctx.ObjectiveName} DZ Trigger", ctx.ObjectiveCoordinates, briefingRoom.Database.Common.DropOffDistanceMeters);
                foreach (var group in targetGroupInfo.Value.DCSGroups)
                    foreach (var unit in group.Units)
                        TriggerMaker.AddTransportTrigger(ref ctx.Mission, zoneId, unit.UnitId, unit.Name, objectiveIndex);
            }

            var pluralIndex = ObjectiveCreationHelpers.GetPluralIndex(targetGroupInfo.Value, isStatic);
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(ctx.Mission.LangKey), ctx.Mission).Replace("\"", "''");
            var unitDisplayName = targetGroupInfo.Value.UnitDB.UIDisplayName;
            
            ObjectiveCreationHelpers.CreateTaskString(briefingRoom.Database, ref ctx.Mission, pluralIndex, ref taskString, ctx.ObjectiveName, ctx.ObjectiveTargetUnitFamily, unitDisplayName, task, ctx.LuaExtraSettings);
            ObjectiveCreationHelpers.CreateLua(ref ctx.Mission, targetDB, taskDB, objectiveIndex, ctx.ObjectiveName, targetGroupInfo, taskString, task, ctx.LuaExtraSettings);

            ObjectiveCreationHelpers.AddBriefingRemarks(ctx, pluralIndex);
            ObjectiveCreationHelpers.AddOggFilesAndFeatures(ctx, targetGroupInfo.Value);

            // Update ref parameters
            mission = ctx.Mission;
            objectiveCoordinates = ctx.ObjectiveCoordinates;

            return ObjectiveCreationHelpers.FinalizeObjective(ctx, targetGroupInfo.Value);
        }
    }
}