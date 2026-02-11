using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    internal class TransportDynamicCargo
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

            // Get transport origin and destination
            var (originAirbase, unitCoordinates) = ObjectiveTransportUtils.GetTransportOrigin(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, objectiveCoordinates);
            var (airbase, destinationPoint) = ObjectiveTransportUtils.GetTransportDestination(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, task.TransportDistance, originAirbase?.DCSID ?? -1);
            ctx.ObjectiveCoordinates = destinationPoint;

            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            ctx.UnitCount = 1;

            // Handle special spawn point cases
            if (airbase == null)
                ctx.ObjectiveCoordinates = HandleSpecialSpawnPoints(ctx, targetDB, airbase, groupLua);

            GroupInfo? targetGroupInfo = CreateTargetGroup(ctx, targetDB, taskDB, airbase, groupLua, ref ctx.Mission);

            ctx.Mission.ObjectiveCoordinates.Add(ctx.ObjectiveCoordinates);
            ctx.ObjectiveName = ctx.Mission.WaypointNameGenerator.GetWaypointName();

            var isStatic = ctx.ObjectiveTargetUnitFamily.GetUnitCategory() != UnitCategory.Ship;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, ctx.ObjectiveName, isStatic);

            // Setup carrier/airbase capabilities
            if (airbase != null)
                ctx.Mission.SetValue($"Airbase{airbase.DCSID}LimitedMunitions", true);
            else if (targetDB.UnitCategory != UnitCategory.Cargo)
                ctx.Mission.CarrierDictionary.Add(ctx.ObjectiveName, new CarrierGroupInfo(targetGroupInfo.Value, 1, 1, ctx.Mission.TemplateRecord.ContextPlayerCoalition, false));

            // Process dynamic cargo items
            ProcessDynamicCargoItems(ctx, targetDB, taskDB, targetGroupInfo.Value, airbase, ref objectiveIndex);

            ObjectiveCreationHelpers.AddOggFilesAndFeatures(ctx, targetGroupInfo.Value);

            ctx.Mission.ObjectiveTargetUnitFamilies.Add(ctx.ObjectiveTargetUnitFamily);
            var waypoint = ObjectiveCreationHelpers.GenerateObjectiveWaypoint(briefingRoom.Database, ref ctx.Mission, task, ctx.ObjectiveCoordinates, ctx.ObjectiveCoordinates, ctx.ObjectiveName, hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            ctx.Mission.Waypoints.Add(waypoint);
            ctx.ObjectiveWaypoints.Add(waypoint);
            ctx.Mission.MapData.Add($"OBJECTIVE_AREA_{ctx.ObjectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });

            // Update ref parameters
            mission = ctx.Mission;
            objectiveCoordinates = ctx.ObjectiveCoordinates;

            return ctx.ObjectiveWaypoints;
        }

        private static Coordinates HandleSpecialSpawnPoints(ObjectiveContext ctx, DBEntryObjectiveTarget targetDB, DBEntryAirbase airbase, string groupLua)
        {
            var objectiveCoordinates = ctx.ObjectiveCoordinates;

            if (ctx.ObjectiveTargetUnitFamily == UnitFamily.StaticStructureOffshore)
            {
                objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(ctx.BriefingRoom.Database, ref ctx.Mission, objectiveCoordinates, [SpawnPointType.Sea]);
            }
            else if (targetDB.UnitCategory == UnitCategory.Ship)
            {
                objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(ctx.BriefingRoom.Database, ref ctx.Mission, objectiveCoordinates, targetDB.ValidSpawnPoints);
                var shipDest = SpawnPointSelector.GetRandomSpawnPoint(ctx.BriefingRoom.Database, ref ctx.Mission, targetDB.ValidSpawnPoints, objectiveCoordinates, new MinMaxD(5, 50));
                if (shipDest == null)
                    throw new BriefingRoomException(ctx.BriefingRoom.Database, ctx.Mission.LangKey, "NoValidSpawnPointsForObjectiveTarget", targetDB.ID);
                ctx.ExtraSettings.Add("GroupX2", shipDest.Value.X);
                ctx.ExtraSettings.Add("GroupY2", shipDest.Value.Y);
                ctx.UnitCount = new MinMaxI(1, 3).GetValue();
            }
            else
            {
                objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(ctx.BriefingRoom.Database, ref ctx.Mission, objectiveCoordinates, [SpawnPointType.LandLarge]);
            }

            return objectiveCoordinates;
        }

        private static GroupInfo? CreateTargetGroup(ObjectiveContext ctx, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTask taskDB, DBEntryAirbase airbase, string groupLua, ref DCSMission mission)
        {
            GroupInfo? targetGroupInfo;

            if (targetDB.UnitCategory == UnitCategory.Static || (targetDB.UnitCategory == UnitCategory.Cargo && airbase == null))
            {
                var radioFrequency = 127.5;
                var extraSettings = new Dictionary<string, object>
                {
                    { "HeliportCallsignId", 6 },
                    { "HeliportModulation", (int)RadioModulation.AM },
                    { "HeliportFrequency", GeneratorTools.FormatRadioFrequency(radioFrequency) },
                    { "RadioBand", (int)RadioModulation.AM },
                    { "RadioFrequency", GeneratorTools.GetRadioFrequency(radioFrequency) },
                    { "playerCanDrive", false }
                };

                if (ctx.ObjectiveTargetUnitFamily == UnitFamily.StaticStructureOffshore)
                {
                    targetGroupInfo = UnitGenerator.AddUnitGroup(
                        ctx.BriefingRoom,
                        ref mission,
                        [Toolbox.RandomFrom(new List<string> { "Oil rig", "Gas platform" })],
                        taskDB.TargetSide,
                        ctx.ObjectiveTargetUnitFamily,
                        "Static", "StaticFOB",
                        ctx.ObjectiveCoordinates,
                        0,
                        extraSettings);
                }
                else
                {
                    var fobTemplate = ctx.BriefingRoom.Database.GetEntry<DBEntryTemplate>("FOB_Berlin");
                    targetGroupInfo = UnitGenerator.AddUnitGroupTemplate(
                        ctx.BriefingRoom,
                        ref mission,
                        fobTemplate, Side.Ally,
                        "Static", "StaticFOB",
                        ctx.ObjectiveCoordinates, 0,
                        extraSettings);
                }

                mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{targetGroupInfo.Value.Name}\t\t{GeneratorTools.FormatRadioFrequency(radioFrequency)}\t\t");
                mission.MapData.Add($"FOB_{ctx.ObjectiveName}", new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() });
            }
            else
            {
                (ctx.Units, ctx.UnitDBs) = UnitGenerator.GetUnits(ctx.BriefingRoom, ref mission, ctx.ObjectiveTargetUnitFamilies, ctx.UnitCount, taskDB.TargetSide, ctx.GroupFlags, ref ctx.ExtraSettings, ctx.TargetBehaviorDB.IsStatic, forceTryTemplate: targetDB.UnitCategory == UnitCategory.Ship);
                ctx.ExtraSettings.Add("playerCanDrive", false);
                
                var actualGroupLua = targetDB.UnitCategory == UnitCategory.Ship ? "ShipPatrolling" : groupLua;
                
                targetGroupInfo = UnitGenerator.AddUnitGroup(
                    ctx.BriefingRoom,
                    ref mission,
                    ctx.Units,
                    taskDB.TargetSide,
                    ctx.ObjectiveTargetUnitFamily,
                    actualGroupLua, ctx.LuaUnit,
                    ctx.ObjectiveCoordinates,
                    ctx.GroupFlags,
                    ctx.ExtraSettings);
            }

            return targetGroupInfo;
        }

        private static void ProcessDynamicCargoItems(ObjectiveContext ctx, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTask taskDB, GroupInfo targetGroupInfo, DBEntryAirbase airbase, ref int objectiveIndex)
        {
            var requiredOverallCount = ctx.Task.TargetCount switch
            {
                Amount.VeryLow => Toolbox.RandomInt(1, 2),
                Amount.Low => Toolbox.RandomInt(3, 5),
                Amount.Average => Toolbox.RandomInt(6, 15),
                Amount.High => Toolbox.RandomInt(17, 30),
                Amount.VeryHigh => Toolbox.RandomInt(31, 100),
                _ => 1,
            };

            var luaExtraSettingsBase = new Dictionary<string, object>
            {
                { "AirbaseName", airbase != null ? airbase.Name : targetGroupInfo.UnitNames[0] },
            };

            var itemKeys = ctx.BriefingRoom.Database.GetAllEntriesIDs<DBEntryDynamicCargo>().ToList();
            int i = 0;

            do
            {
                if (i > 0)
                {
                    objectiveIndex++;
                    ctx.ObjectiveIndex = objectiveIndex;
                }

                var itemKey = Toolbox.RandomFrom(itemKeys);
                itemKeys.Remove(itemKey);

                var luaExtraSettings = luaExtraSettingsBase.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.InvariantCultureIgnoreCase);
                var requiredCount = new MinMaxI(1, requiredOverallCount).GetValue();
                requiredOverallCount -= requiredCount;

                luaExtraSettings.Add("ItemName", itemKey);
                luaExtraSettings.Add("RequiredCount", requiredCount);

                var unitDisplayName = ctx.BriefingRoom.Database.GetEntry<DBEntryDynamicCargo>(itemKey).UIDisplayName;
                var pluralIndex = requiredOverallCount == 1 ? 0 : 1;
                var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(ctx.Mission.LangKey), ctx.Mission).Replace("\"", "''");

                ObjectiveCreationHelpers.CreateTaskString(ctx.BriefingRoom.Database, ref ctx.Mission, pluralIndex, ref taskString, ctx.ObjectiveName, ctx.ObjectiveTargetUnitFamily, unitDisplayName, ctx.Task, luaExtraSettings);
                ObjectiveCreationHelpers.CreateLua(ref ctx.Mission, targetDB, taskDB, ctx.ObjectiveIndex, ctx.ObjectiveName, targetGroupInfo, taskString, ctx.Task, luaExtraSettings);

                // Add briefing remarks
                var remarksString = taskDB.BriefingRemarks.Get(ctx.Mission.LangKey);
                if (!string.IsNullOrEmpty(remarksString))
                {
                    string remark = Toolbox.RandomFrom(remarksString.Split(";"));
                    GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", ctx.ObjectiveName);
                    GeneratorTools.ReplaceKey(ref remark, "UnitFamily", ctx.BriefingRoom.Database.Common.Names.UnitFamilies[(int)ctx.ObjectiveTargetUnitFamily].Get(ctx.Mission.LangKey).Split(",")[pluralIndex]);
                    ctx.Mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
                }

                i++;
            } while (requiredOverallCount > 0 && itemKeys.Count > 0);
        }
    }
}