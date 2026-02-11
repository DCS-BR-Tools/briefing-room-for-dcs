using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Mission.DCSLuaObjects;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    /// <summary>
    /// Helper methods for objective creation including briefing, Lua generation, and unit group configuration.
    /// </summary>
    internal static class ObjectiveCreationHelpers
    {
        #region Briefing and Lua Generation

        /// <summary>
        /// Creates the Lua table and triggers for an objective.
        /// </summary>
        internal static void CreateLua(
            ref DCSMission mission, 
            DBEntryObjectiveTarget targetDB, 
            DBEntryObjectiveTask taskDB, 
            int objectiveIndex, 
            string objectiveName, 
            GroupInfo? targetGroupInfo, 
            string taskString, 
            MissionTemplateSubTaskRecord task, 
            Dictionary<string, object> extraSettings)
        {
            string objectiveLua = BuildObjectiveLuaTable(
                objectiveIndex, objectiveName, targetGroupInfo.Value.Name, 
                targetDB, taskDB, taskString, task);
            
            mission.AppendValue("ScriptObjectives", objectiveLua);

            foreach (var completionTriggerLua in taskDB.CompletionTriggersLua)
            {
                string triggerLua = Toolbox.ReadAllTextIfFileExists(
                    Path.Combine(BRPaths.INCLUDE_LUA_OBJECTIVETRIGGERS, completionTriggerLua));
                GeneratorTools.ReplaceKey(ref triggerLua, "ObjectiveIndex", objectiveIndex + 1);
                
                foreach (var extraSetting in extraSettings)
                    if (extraSetting.Value is not Array)
                        GeneratorTools.ReplaceKey(ref triggerLua, extraSetting.Key, extraSetting.Value);
                
                mission.AppendValue("ScriptObjectivesTriggers", triggerLua);
            }
        }

        private static string BuildObjectiveLuaTable(
            int objectiveIndex, 
            string objectiveName, 
            string groupName,
            DBEntryObjectiveTarget targetDB, 
            DBEntryObjectiveTask taskDB, 
            string taskString, 
            MissionTemplateSubTaskRecord task)
        {
            var progressionCondition = !string.IsNullOrEmpty(task.ProgressionOverrideCondition) 
                ? task.ProgressionOverrideCondition 
                : string.Join(
                    task.ProgressionDependentIsAny ? " or " : " and ", 
                    task.ProgressionDependentTasks.Select(x => x + 1));

            return $@"briefingRoom.mission.objectives[{objectiveIndex + 1}] = {{ complete = false, failed = false, groupName = ""{groupName}"", hideTargetCount = false, name = ""{objectiveName}"", targetCategory = Unit.Category.{targetDB.UnitCategory.ToLuaName()}, taskType = ""{taskDB.ID}"", task = ""{taskString}"", unitsCount = #dcsExtensions.getUnitNamesByGroupNameSuffixExcludeScenery(""-TGT-{objectiveName}""), unitNames = dcsExtensions.getUnitNamesByGroupNameSuffixExcludeScenery(""-TGT-{objectiveName}""), progressionHidden = {(task.ProgressionActivation ? "true" : "false")}, progressionHiddenBrief = {(task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief) ? "true" : "false")}, progressionCondition = ""{progressionCondition}"", startMinutes = {(task.ProgressionActivation ? "-1" : "0")}, f10MenuText = ""$LANG_OBJECTIVE$ {objectiveName}"", f10Commands = {{}}}}
";
        }

        /// <summary>
        /// Creates the task string for the mission briefing.
        /// </summary>
        internal static void CreateTaskString(
            IDatabase database, 
            ref DCSMission mission, 
            int pluralIndex, 
            ref string taskString, 
            string objectiveName, 
            UnitFamily objectiveTargetUnitFamily, 
            LanguageString unitDisplayName, 
            MissionTemplateSubTaskRecord task, 
            Dictionary<string, object> extraSettings)
        {
            if (string.IsNullOrEmpty(taskString)) 
                taskString = "Complete objective $OBJECTIVENAME$";
            
            GeneratorTools.ReplaceKey(ref taskString, "ObjectiveName", objectiveName);
            GeneratorTools.ReplaceKey(ref taskString, "UnitFamily", 
                database.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily].Get(mission.LangKey).Split(",")[pluralIndex]);
            GeneratorTools.ReplaceKey(ref taskString, "UnitDisplayName", unitDisplayName.Get(mission.LangKey));
            
            foreach (var extraSetting in extraSettings)
                if (extraSetting.Value is not Array)
                    GeneratorTools.ReplaceKey(ref taskString, extraSetting.Key, extraSetting.Value);
            
            if (!task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief))
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Task, taskString);
        }

        /// <summary>
        /// Generates a waypoint for an objective.
        /// </summary>
        internal static Waypoint GenerateObjectiveWaypoint(
            IDatabase database, 
            ref DCSMission mission, 
            MissionTemplateSubTaskRecord objectiveTemplate, 
            Coordinates objectiveCoordinates, 
            Coordinates objectiveDestinationCoordinates, 
            string objectiveName, 
            List<int> groupIds = null, 
            bool scriptIgnore = false, 
            bool hiddenMapMarker = false)
        {
            var (targetDB, targetBehaviorDB, taskDB, objectiveOptions, _) = 
                ObjectiveUtils.GetCustomObjectiveData(database, mission.LangKey, objectiveTemplate);
            
            if (targetDB == null) 
                throw new BriefingRoomException(database, mission.LangKey, "TargetNotFound", targetDB.UIDisplayName);

            var waypointCoordinates = CalculateWaypointCoordinates(
                database, ref mission, objectiveCoordinates, objectiveDestinationCoordinates, 
                objectiveName, targetDB, targetBehaviorDB, taskDB, objectiveOptions);
            
            bool onGround = !targetDB.UnitCategory.IsAircraft() || 
                           Constants.AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorDB.Location);
            bool isPickup = objectiveName.EndsWith("Pickup");
            
            return new Waypoint(
                isPickup ? $"P-{objectiveName}" : objectiveName, 
                waypointCoordinates, onGround, groupIds, scriptIgnore, 
                objectiveTemplate.Options.Contains(ObjectiveOption.NoAircraftWaypoint), 
                hiddenMapMarker);
        }

        private static Coordinates CalculateWaypointCoordinates(
            IDatabase database,
            ref DCSMission mission,
            Coordinates objectiveCoordinates,
            Coordinates objectiveDestinationCoordinates,
            string objectiveName,
            DBEntryObjectiveTarget targetDB,
            DBEntryObjectiveTargetBehavior targetBehaviorDB,
            DBEntryObjectiveTask taskDB,
            ObjectiveOption[] objectiveOptions)
        {
            var waypointCoordinates = objectiveCoordinates;
            bool isPickup = objectiveName.EndsWith("Pickup");
            bool onGround = !targetDB.UnitCategory.IsAircraft() || 
                           Constants.AIR_ON_GROUND_LOCATIONS.Contains(targetBehaviorDB.Location);

            if (objectiveOptions.Contains(ObjectiveOption.InaccurateWaypoint) && 
                (!taskDB.UICategory.ContainsValue("Transport") || isPickup))
            {
                waypointCoordinates += Coordinates.CreateRandom(3.0, 6.0) * Toolbox.NM_TO_METERS;
                if (mission.TemplateRecord.OptionsMission.Contains("MarkWaypoints"))
                    DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", 
                        DrawingType.Circle, waypointCoordinates, 
                        "Radius".ToKeyValuePair(6.0 * Toolbox.NM_TO_METERS));
            }
            else if (taskDB.UICategory.ContainsValue("Transport"))
            {
                var dist = database.Common.DropOffDistanceMeters;
                if (taskDB.IsEscort() && !onGround)
                    dist = dist * 10;
                DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", 
                    DrawingType.Circle, waypointCoordinates, "Radius".ToKeyValuePair(dist));
            }
            else if (targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.Patrolling)
            {
                DrawingMaker.AddDrawing(ref mission, $"Target Zone {objectiveName}", 
                    DrawingType.Circle, waypointCoordinates, 
                    "Radius".ToKeyValuePair(objectiveDestinationCoordinates.GetDistanceFrom(objectiveCoordinates)));
            }

            return waypointCoordinates;
        }

        #endregion

        #region Template Location and Unit Retrieval

        /// <summary>
        /// Tries to get units from a template location. Returns true if template location was used.
        /// </summary>
        internal static bool TryGetTemplateLocationUnits(ObjectiveContext ctx)
        {
            if (!Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Any(x => ctx.ObjectiveTargetUnitFamilies.Contains(x)) || 
                !ctx.TargetBehaviorDB.IsStatic)
                return false;

            var locationType = Toolbox.RandomFrom(Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys
                .Intersect(ctx.ObjectiveTargetUnitFamilies)
                .Select(x => Constants.THEATER_TEMPLATE_LOCATION_MAP[x]).ToList());
            var templateLocation = SpawnPointSelector.GetNearestTemplateLocation(
                ref ctx.Mission, locationType, ctx.ObjectiveCoordinates, true);
            
            if (!templateLocation.HasValue)
                return false;

            ctx.ObjectiveCoordinates = templateLocation.Value.Coordinates;
            (ctx.Units, ctx.UnitDBs) = UnitGenerator.GetUnitsForTemplateLocation(
                ctx.BriefingRoom, ref ctx.Mission, templateLocation.Value, 
                ctx.TaskDB.TargetSide, ctx.ObjectiveTargetUnitFamilies, ref ctx.ExtraSettings);
            
            if (ctx.Units.Count == 0)
                SpawnPointSelector.RecoverTemplateLocation(ref ctx.Mission, templateLocation.Value.Coordinates);
            
            return ctx.Units.Count > 0;
        }

        /// <summary>
        /// Gets units with validation, throws if none found.
        /// </summary>
        internal static void GetUnitsWithValidation(ObjectiveContext ctx)
        {
            if (ctx.Units.Count == 0)
            {
                (ctx.Units, ctx.UnitDBs) = UnitGenerator.GetUnits(
                    ctx.BriefingRoom, ref ctx.Mission, ctx.ObjectiveTargetUnitFamilies, 
                    ctx.UnitCount, ctx.TaskDB.TargetSide, ctx.GroupFlags, 
                    ref ctx.ExtraSettings, ctx.TargetBehaviorDB.IsStatic);
            }
            
            if (ctx.Units.Count == 0 || ctx.UnitDBs.Count == 0)
                throw new BriefingRoomException(ctx.BriefingRoom.Database, ctx.Mission.LangKey, 
                    "NoUnitsForTimePeriod", ctx.TaskDB.TargetSide, ctx.ObjectiveTargetUnitFamily);
        }

        #endregion

        #region Destination and Movement Configuration

        /// <summary>
        /// Calculates a destination point for moving unit groups based on unit category.
        /// </summary>
        internal static Coordinates CalculateDestinationPoint(ObjectiveContext ctx)
        {
            var offset = ctx.TargetDB.UnitCategory switch
            {
                UnitCategory.Plane => Coordinates.CreateRandom(30, 60),
                UnitCategory.Helicopter => Coordinates.CreateRandom(10, 20),
                _ => ctx.ObjectiveTargetUnitFamily == UnitFamily.InfantryMANPADS || 
                     ctx.ObjectiveTargetUnitFamily == UnitFamily.Infantry 
                        ? Coordinates.CreateRandom(1, 5) 
                        : Coordinates.CreateRandom(5, 10)
            };
            
            var destinationPoint = ctx.ObjectiveCoordinates + (offset * Toolbox.NM_TO_METERS);
            
            if (ctx.TargetDB.DCSUnitCategory == DCSUnitCategory.Vehicle)
                destinationPoint = ObjectiveUtils.GetNearestSpawnCoordinates(
                    ctx.BriefingRoom.Database, ref ctx.Mission, 
                    destinationPoint, ctx.TargetDB.ValidSpawnPoints, false);
            
            return destinationPoint;
        }

        /// <summary>
        /// Configures group Lua script name and destination point based on target behavior location.
        /// </summary>
        internal static (string groupLua, Coordinates destinationPoint) ConfigureGroupLuaForLocation(
            ObjectiveContext ctx, 
            Coordinates destinationPoint,
            DBEntryObjectiveTargetBehaviorLocation locationToCheck)
        {
            var groupLua = ctx.TargetBehaviorDB.GroupLua[(int)ctx.TargetDB.DCSUnitCategory];
            
            if (locationToCheck == DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase)
            {
                destinationPoint = ctx.Mission.PlayerAirbase.ParkingSpots.Length > 1 
                    ? Toolbox.RandomFrom(ctx.Mission.PlayerAirbase.ParkingSpots).Coordinates 
                    : ctx.Mission.PlayerAirbase.Coordinates;
                    
                if (ctx.ObjectiveTargetUnitFamily.GetUnitCategory().IsAircraft() && ctx.TaskDB.TargetSide == Side.Enemy)
                    groupLua = GetAttackingAircraftGroupLua(ctx.ObjectiveTargetUnitFamily, groupLua);
            }
            else if (locationToCheck == DBEntryObjectiveTargetBehaviorLocation.Airbase)
            {
                var targetCoalition = GeneratorTools.GetSpawnPointCoalition(
                    ctx.Mission.TemplateRecord, ctx.TaskDB.TargetSide, forceSide: true);
                var destinationAirbase = ctx.Mission.AirbaseDB
                    .Where(x => x.Coalition == targetCoalition.Value)
                    .OrderBy(x => destinationPoint.GetDistanceFrom(x.Coordinates))
                    .First();
                destinationPoint = destinationAirbase.Coordinates;
                ctx.ExtraSettings.Add("EndAirbaseId", destinationAirbase.DCSID);
                ctx.Mission.PopulatedAirbaseIds[targetCoalition.Value].Add(destinationAirbase.DCSID);
            }
            else if (groupLua.StartsWith("AircraftOrbiting"))
            {
                destinationPoint = ctx.ObjectiveCoordinates;
            }
            
            return (groupLua, destinationPoint);
        }

        /// <summary>
        /// Gets the appropriate attacking aircraft group Lua based on unit family.
        /// </summary>
        internal static string GetAttackingAircraftGroupLua(UnitFamily unitFamily, string defaultLua) =>
            unitFamily switch
            {
                UnitFamily.PlaneAttack => "AircraftBomb",
                UnitFamily.PlaneBomber => "AircraftBomb",
                UnitFamily.PlaneStrike => "AircraftBomb",
                UnitFamily.PlaneFighter => "AircraftCAP",
                UnitFamily.PlaneInterceptor => "AircraftCAP",
                UnitFamily.HelicopterAttack => "AircraftBomb",
                _ => defaultLua
            };

        /// <summary>
        /// Adds destination coordinates to extra settings if not static.
        /// </summary>
        internal static void AddDestinationToSettings(ObjectiveContext ctx, Coordinates destinationPoint)
        {
            if (!ctx.TargetBehaviorDB.IsStatic)
            {
                ctx.ExtraSettings.Add("GroupX2", destinationPoint.X);
                ctx.ExtraSettings.Add("GroupY2", destinationPoint.Y);
            }
        }

        #endregion

        #region Group Configuration

        /// <summary>
        /// Adds appropriate aircraft spawn flags based on context.
        /// </summary>
        internal static void AddAircraftSpawnFlags(ObjectiveContext ctx)
        {
            if (ctx.ObjectiveTargetUnitFamily.GetUnitCategory().IsAircraft() &&
                !ctx.GroupFlags.HasFlag(GroupFlags.RadioAircraftSpawn) &&
                !Constants.AIR_ON_GROUND_LOCATIONS.Contains(ctx.TargetBehaviorDB.Location))
            {
                ctx.GroupFlags |= ctx.Task.ProgressionActivation 
                    ? GroupFlags.ProgressionAircraftSpawn 
                    : GroupFlags.ImmediateAircraftSpawn;
            }
        }

        /// <summary>
        /// Applies progression settings to the target group (late activation, visibility).
        /// </summary>
        internal static void ApplyProgressionSettings(ObjectiveContext ctx, GroupInfo targetGroupInfo, bool checkTransport = false)
        {
            if (ctx.Task.ProgressionActivation)
            {
                targetGroupInfo.DCSGroups.ForEach(grp =>
                {
                    grp.LateActivation = true;
                    grp.Visible = ctx.Task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable);
                });
            }
            else if (checkTransport && 
                     ctx.Mission.TemplateRecord.MissionFeatures.Contains("ContextScrambleStart") && 
                     !ctx.TaskDB.UICategory.ContainsValue("Transport"))
            {
                targetGroupInfo.DCSGroup.LateActivation = false;
            }
        }

        /// <summary>
        /// Adds unlimited fuel task to aircraft.
        /// </summary>
        internal static void AddUnlimitedFuelTask(ObjectiveContext ctx, GroupInfo targetGroupInfo)
        {
            if (ctx.TargetDB.UnitCategory.IsAircraft())
            {
                targetGroupInfo.DCSGroup.Waypoints.First().Tasks.Insert(0, 
                    new DCSWrappedWaypointTask("SetUnlimitedFuel", new Dictionary<string, object> { { "value", true } }));
            }
        }

        /// <summary>
        /// Adds embark to transport task for infantry units.
        /// </summary>
        internal static void AddEmbarkTask(ObjectiveContext ctx, GroupInfo targetGroupInfo, Coordinates unitCoordinates)
        {
            if (ctx.TargetDB.UnitCategory == UnitCategory.Infantry)
            {
                var pos = unitCoordinates.CreateNearRandom(new MinMaxD(5, 50));
                targetGroupInfo.DCSGroup.Waypoints.First().Tasks.Add(new DCSWaypointTask("EmbarkToTransport",
                    new Dictionary<string, object>
                    {
                        { "x", pos.X },
                        { "y", pos.Y },
                        { "zoneRadius", ctx.BriefingRoom.Database.Common.DropOffDistanceMeters }
                    }, _auto: false));
            }
        }

        /// <summary>
        /// Configures extra waypoints based on target behavior.
        /// </summary>
        internal static void ConfigureExtraWaypoints(ObjectiveContext ctx, GroupInfo targetGroupInfo, bool isEscort = false)
        {
            var shouldSkipExtraWaypoints = isEscort || 
                                           ctx.TargetBehaviorDB.ID.Contains("OnRoad") || 
                                           ctx.TargetBehaviorDB.ID.Contains("Idle");
            
            if (!shouldSkipExtraWaypoints)
            {
                targetGroupInfo.DCSGroup.Waypoints = DCSWaypoint.CreateExtraWaypoints(
                    ref ctx.Mission, targetGroupInfo.DCSGroup.Waypoints, targetGroupInfo.UnitDB.Families.First());
            }
        }

        #endregion

        #region Briefing Items

        /// <summary>
        /// Adds briefing items (target group name, task string, Lua).
        /// </summary>
        internal static void AddBriefingItems(ObjectiveContext ctx, GroupInfo targetGroupInfo, bool isStatic)
        {
            ctx.Mission.Briefing.AddItem(DCSMissionBriefingItemType.TargetGroupName, $"-TGT-{ctx.ObjectiveName}");
            
            var length = isStatic ? targetGroupInfo.DCSGroups.Count : targetGroupInfo.UnitNames.Length;
            var pluralIndex = length == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(
                ctx.TaskDB.BriefingTask[pluralIndex].Get(ctx.Mission.LangKey), ctx.Mission).Replace("\"", "''");
            var unitDisplayName = targetGroupInfo.UnitDB.UIDisplayName;
            
            CreateTaskString(ctx.BriefingRoom.Database, ref ctx.Mission, pluralIndex, ref taskString, 
                ctx.ObjectiveName, ctx.ObjectiveTargetUnitFamily, unitDisplayName, ctx.Task, ctx.LuaExtraSettings);
            CreateLua(ref ctx.Mission, ctx.TargetDB, ctx.TaskDB, ctx.ObjectiveIndex, ctx.ObjectiveName, 
                targetGroupInfo, taskString, ctx.Task, ctx.LuaExtraSettings);
        }

        /// <summary>
        /// Adds briefing remarks if available.
        /// </summary>
        internal static void AddBriefingRemarks(ObjectiveContext ctx, int pluralIndex)
        {
            var remarksString = ctx.TaskDB.BriefingRemarks.Get(ctx.Mission.LangKey);
            if (!string.IsNullOrEmpty(remarksString))
            {
                string remark = Toolbox.RandomFrom(remarksString.Split(";"));
                GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", ctx.ObjectiveName);
                GeneratorTools.ReplaceKey(ref remark, "DropOffDistanceMeters", 
                    ctx.BriefingRoom.Database.Common.DropOffDistanceMeters.ToString());
                GeneratorTools.ReplaceKey(ref remark, "UnitFamily", 
                    ctx.BriefingRoom.Database.Common.Names.UnitFamilies[(int)ctx.ObjectiveTargetUnitFamily]
                        .Get(ctx.Mission.LangKey).Split(",")[pluralIndex]);
                ctx.Mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
            }
        }

        #endregion

        #region Features and Media

        /// <summary>
        /// Adds OGG files and generates objective features.
        /// </summary>
        internal static void AddOggFilesAndFeatures(
            ObjectiveContext ctx, 
            GroupInfo targetGroupInfo, 
            HashSet<string> additionalFeatures = null)
        {
            foreach (string oggFile in ctx.TaskDB.IncludeOgg)
                ctx.Mission.AddMediaFile($"{BRPaths.MIZ_RESOURCES_OGG}{oggFile}", 
                    Path.Combine(BRPaths.INCLUDE_OGG, oggFile));

            ctx.Mission.AppendValue("ScriptObjectivesFeatures", "");
            var featureList = ctx.TaskDB.RequiredFeatures.Concat(ctx.FeaturesID).ToHashSet();
            
            if (additionalFeatures != null)
                featureList.UnionWith(additionalFeatures);

            var overrideCoords = ctx.TargetBehaviorDB.ID.StartsWith("ToFrontLine") 
                ? ctx.ObjectiveCoordinates 
                : (Coordinates?)null;
            
            foreach (string featureID in featureList)
                FeaturesObjectives.GenerateMissionFeature(ctx.BriefingRoom, ref ctx.Mission, featureID, 
                    ctx.ObjectiveName, ctx.ObjectiveIndex, targetGroupInfo, ctx.TaskDB.TargetSide, 
                    ctx.ObjectiveOptions, overrideCoords: overrideCoords);
        }

        #endregion

        #region Finalization

        /// <summary>
        /// Finalizes the objective by adding waypoints, map data, etc.
        /// </summary>
        internal static List<Waypoint> FinalizeObjective(
            ObjectiveContext ctx, 
            GroupInfo targetGroupInfo, 
            bool addUnitMapData = true, 
            Coordinates? altObjectiveCoords = null)
        {
            var objectiveCoords = altObjectiveCoords ?? ctx.ObjectiveCoordinates;
            ctx.Mission.ObjectiveCoordinates.Add(objectiveCoords);
            
            var furthestWaypoint = targetGroupInfo.DCSGroup.Waypoints.Aggregate(objectiveCoords, 
                (furthest, x) => objectiveCoords.GetDistanceFrom(x.Coordinates) > objectiveCoords.GetDistanceFrom(furthest) 
                    ? x.Coordinates : furthest);
            
            var waypoint = GenerateObjectiveWaypoint(ctx.BriefingRoom.Database, ref ctx.Mission, ctx.Task, 
                objectiveCoords, furthestWaypoint, ctx.ObjectiveName, 
                targetGroupInfo.DCSGroups.Select(x => x.GroupId).ToList(), 
                hiddenMapMarker: ctx.Task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            
            ctx.Mission.Waypoints.Add(waypoint);
            ctx.ObjectiveWaypoints.Add(waypoint);
            ctx.Mission.MapData.Add($"OBJECTIVE_AREA_{ctx.ObjectiveIndex}", 
                new List<double[]> { waypoint.Coordinates.ToArray() });
            ctx.Mission.ObjectiveTargetUnitFamilies.Add(ctx.ObjectiveTargetUnitFamily);
            
            if (addUnitMapData && !targetGroupInfo.UnitDB.IsAircraft)
                ctx.Mission.MapData.Add(
                    $"UNIT-{targetGroupInfo.UnitDB.Families[0]}-{ctx.TaskDB.TargetSide}-{targetGroupInfo.GroupID}", 
                    new List<double[]> { targetGroupInfo.Coordinates.ToArray() });
            
            return ctx.ObjectiveWaypoints;
        }

        /// <summary>
        /// Gets the plural index based on unit count.
        /// </summary>
        internal static int GetPluralIndex(GroupInfo targetGroupInfo, bool isStatic) =>
            (isStatic ? targetGroupInfo.DCSGroups.Count : targetGroupInfo.UnitNames.Length) == 1 ? 0 : 1;

        #endregion
    }
}
