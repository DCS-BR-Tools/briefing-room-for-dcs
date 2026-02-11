using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    internal class Basic
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

            // Try template location first
            ObjectiveCreationHelpers.TryGetTemplateLocationUnits(ctx);

            // Get units if not from template
            ObjectiveCreationHelpers.GetUnitsWithValidation(ctx);

            var unitDB = ctx.UnitDBs.First();
            if (Constants.AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && targetDB.UnitCategory.IsAircraft())
                ctx.ObjectiveCoordinates = ObjectiveUtils.PlaceInAirbase(briefingRoom, ref ctx.Mission, ctx.ExtraSettings, targetBehaviorDB, ctx.ObjectiveCoordinates, ctx.UnitCount, unitDB);

            // Calculate destination point
            var destinationPoint = ObjectiveCreationHelpers.CalculateDestinationPoint(ctx);

            // Configure group Lua based on target behavior location
            var (groupLua, updatedDestination) = ObjectiveCreationHelpers.ConfigureGroupLuaForLocation(ctx, destinationPoint, targetBehaviorDB.Location);
            destinationPoint = updatedDestination;

            ObjectiveCreationHelpers.AddDestinationToSettings(ctx, destinationPoint);
            ctx.ExtraSettings.Add("playerCanDrive", false);

            ctx.ObjectiveName = ctx.Mission.WaypointNameGenerator.GetWaypointName();
            ObjectiveCreationHelpers.AddAircraftSpawnFlags(ctx);

            GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
                ref ctx.Mission,
                ctx.Units,
                taskDB.TargetSide,
                ctx.ObjectiveTargetUnitFamily,
                groupLua, ctx.LuaUnit,
                ctx.UnitCoordinates,
                ctx.GroupFlags,
                ctx.ExtraSettings);

            if (!targetGroupInfo.HasValue)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToGenerateGroupObjective");

            if (ctx.Mission.TemplateRecord.MissionFeatures.Contains("ContextScrambleStart"))
                targetGroupInfo.Value.DCSGroup.LateActivation = false;

            ObjectiveCreationHelpers.ApplyProgressionSettings(ctx, targetGroupInfo.Value);
            ObjectiveCreationHelpers.AddUnlimitedFuelTask(ctx, targetGroupInfo.Value);

            if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense) && targetDB.UnitCategory == UnitCategory.Static)
                ObjectiveUtils.AddEmbeddedAirDefenseUnits(briefingRoom, ref ctx.Mission, targetDB, targetBehaviorDB, taskDB, ctx.ObjectiveCoordinates, ctx.GroupFlags, ctx.ExtraSettings);

            ObjectiveCreationHelpers.ConfigureExtraWaypoints(ctx, targetGroupInfo.Value);

            var isStatic = ctx.ObjectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Static || 
                           ctx.ObjectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, ctx.ObjectiveName, isStatic);

            if (task.Task == "CaptureLocation")
            {
                if (!ctx.ExtraSettings.ContainsKey("GroupAirbaseID"))
                    throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "CaptureLocationNoAirbase");
                ctx.LuaExtraSettings.Add("GroupAirbaseID", ctx.ExtraSettings.GetValueOrDefault("GroupAirbaseID"));
            }

            ObjectiveCreationHelpers.AddBriefingItems(ctx, targetGroupInfo.Value, isStatic);
            ObjectiveCreationHelpers.AddBriefingRemarks(ctx, ObjectiveCreationHelpers.GetPluralIndex(targetGroupInfo.Value, isStatic));
            ObjectiveCreationHelpers.AddOggFilesAndFeatures(ctx, targetGroupInfo.Value);

            // Update ref parameters
            mission = ctx.Mission;
            objectiveCoordinates = ctx.ObjectiveCoordinates;

            return ObjectiveCreationHelpers.FinalizeObjective(ctx, targetGroupInfo.Value);
        }
    }
}


