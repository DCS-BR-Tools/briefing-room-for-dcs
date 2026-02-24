using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    internal class Hold
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

            // Configure group Lua based on destination (Hold uses Destination property, not Location)
            var (groupLua, updatedDestination) = ObjectiveCreationHelpers.ConfigureGroupLuaForLocation(ctx, destinationPoint, targetBehaviorDB.Destination);
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

            ObjectiveCreationHelpers.ApplyProgressionSettings(ctx, targetGroupInfo.Value, true);
            ObjectiveCreationHelpers.AddUnlimitedFuelTask(ctx, targetGroupInfo.Value);

            if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense) && targetDB.UnitCategory == UnitCategory.Static)
                ObjectiveUtils.AddEmbeddedAirDefenseUnits(briefingRoom, ref ctx.Mission, targetDB, targetBehaviorDB, taskDB, ctx.ObjectiveCoordinates, ctx.GroupFlags, ctx.ExtraSettings);

            ObjectiveCreationHelpers.ConfigureExtraWaypoints(ctx, targetGroupInfo.Value, taskDB.IsEscort());

            var isStatic = ctx.ObjectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Static || 
                           ctx.ObjectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, ctx.ObjectiveName, isStatic);

            // Handle Hold-specific settings
            if (task.Task.StartsWith("Hold"))
            {
                var (holdSizeMeters, holdTimeSeconds) = GetHoldValues(briefingRoom.Database, task.TargetCount);
                ctx.LuaExtraSettings.Add("HoldSize", holdSizeMeters);
                ctx.LuaExtraSettings.Add("HoldSizeNm", (int)Math.Floor(holdSizeMeters * Toolbox.METERS_TO_NM));
                ctx.LuaExtraSettings.Add("HoldTime", holdTimeSeconds);
                ctx.LuaExtraSettings.Add("HoldTimeMins", holdTimeSeconds / 60);
                if (ctx.Mission.TemplateRecord.OptionsMission.Contains("MarkWaypoints"))
                    DrawingMaker.AddDrawing(ref ctx.Mission, $"Hold Zone {ctx.ObjectiveName}", DrawingType.Circle, ctx.ObjectiveCoordinates, "Radius".ToKeyValuePair(holdSizeMeters));
            }

            if (task.Task == "CaptureLocation")
            {
                if (!ctx.ExtraSettings.ContainsKey("GroupAirbaseID"))
                    throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "CaptureLocationNoAirbase");
                ctx.LuaExtraSettings.Add("GroupAirbaseID", ctx.ExtraSettings.GetValueOrDefault("GroupAirbaseID"));
            }

            ObjectiveCreationHelpers.AddBriefingItems(ctx, targetGroupInfo.Value, isStatic);
            ObjectiveCreationHelpers.AddBriefingRemarks(ctx, ObjectiveCreationHelpers.GetPluralIndex(targetGroupInfo.Value, isStatic));

            // Add hold superiority features if applicable
            HashSet<string> additionalFeatures = null;
            if (taskDB.IsHoldSuperiority())
            {
                additionalFeatures = [];
                var playerHasPlanes = ctx.Mission.TemplateRecord.PlayerFlightGroups.Any(x => briefingRoom.Database.GetEntry<DBEntryJSONUnit>(x.Aircraft).Category == UnitCategory.Plane) || ctx.Mission.TemplateRecord.AirbaseDynamicSpawn != DsAirbase.None;
                SetHoldSuperiorityFeatures(targetDB, ref additionalFeatures, playerHasPlanes);
            }

            ObjectiveCreationHelpers.AddOggFilesAndFeatures(ctx, targetGroupInfo.Value, additionalFeatures);

            // Update ref parameters
            mission = ctx.Mission;
            objectiveCoordinates = ctx.ObjectiveCoordinates;

            return ObjectiveCreationHelpers.FinalizeObjective(ctx, targetGroupInfo.Value);
        }

        private static void SetHoldSuperiorityFeatures(DBEntryObjectiveTarget targetDB, ref HashSet<string> featureList, bool playerHasPlanes)
        {
            switch (targetDB.UnitCategory)
            {
                case UnitCategory.Plane:
                    featureList.Add("HiddenEnemyCAPAttackingObj");
                    break;
                case UnitCategory.Helicopter:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) featureList.Add("HiddenEnemyCASAttackingObj");
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyCAPAttackingObj");
                    if (Toolbox.RollChance(AmountNR.High)) featureList.Add("HiddenEnemyHeloAttackingObj");
                    break;
                case UnitCategory.Ship:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) featureList.Add("HiddenEnemyCASAttackingObj");
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyCAPAttackingObj");
                    if (Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyHeloAttackingObj");
                    if (Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyShipAttackingObj");
                    break;
                default:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) featureList.Add("HiddenEnemyCASAttackingObj");
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyCAPAttackingObj");
                    if (Toolbox.RollChance(AmountNR.Average)) featureList.Add("HiddenEnemyHeloAttackingObj");
                    if (Toolbox.RollChance(AmountNR.VeryHigh)) featureList.Add("HiddenEnemyGroundAttackingObj");
                    break;
            }
        }

        private static (int holdSizeMeters, int holdTimeSeconds) GetHoldValues(IDatabase database, Amount targetAmount) =>
            ((int)Math.Floor(database.Common.HoldSizesInNauticalMiles[targetAmount].GetValue() * Toolbox.NM_TO_METERS),
             database.Common.HoldTimesInMinutes[targetAmount].GetValue() * 60);
    }
}