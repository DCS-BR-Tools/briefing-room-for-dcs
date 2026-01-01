using System;
using System.Collections.Generic;
using System.IO;
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
            // throw new NotImplementedException("TransportDynamicCargo objective is not yet implemented.");
            var extraSettings = new Dictionary<string, object>();
            List<string> units = [];
            List<DBEntryJSONUnit> unitDBs = [];
            var (luaUnit, unitCount, unitCountMinMax, objectiveTargetUnitFamilies, groupFlags) = ObjectiveUtils.GetUnitData(task, targetDB, targetBehaviorDB, objectiveOptions);

            var (originAirbase, unitCoordinates) = ObjectiveUtils.GetTransportOrigin(briefingRoom, ref mission, targetBehaviorDB.Location, objectiveCoordinates);
            var (airbase, destinationPoint) = ObjectiveUtils.GetTransportDestination(briefingRoom, ref mission, targetBehaviorDB.Location, targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, task.TransportDistance, originAirbase?.DCSID ?? -1);
            objectiveCoordinates = destinationPoint;
            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            unitCount = 1;
            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            if (airbase == null)
            {
                if (objectiveTargetUnitFamily == UnitFamily.StaticStructureOffshore)
                    objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, objectiveCoordinates, [SpawnPointType.Sea]);
                else if (targetDB.UnitCategory == UnitCategory.Ship)
                {
                    objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, objectiveCoordinates, targetDB.ValidSpawnPoints);
                    var shipDest = SpawnPointSelector.GetRandomSpawnPoint(briefingRoom.Database, ref mission, targetDB.ValidSpawnPoints, objectiveCoordinates, new MinMaxD(5, 50));
                    if (shipDest == null)
                        throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "NoValidSpawnPointsForObjectiveTarget", targetDB.ID);
                    extraSettings.Add("GroupX2", shipDest.Value.X);
                    extraSettings.Add("GroupY2", shipDest.Value.Y);
                    groupLua = "ShipPatrolling";
                    unitCount = new MinMaxI(1, 3).GetValue();
                }
                else
                    objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, objectiveCoordinates, [SpawnPointType.LandLarge]);
            }
            GroupInfo? targetGroupInfo = null;
            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            if (targetDB.UnitCategory == UnitCategory.Static)
            {
                var radioFrequency = 127.5;
                extraSettings = new Dictionary<string, object>{
                    {"HeliportCallsignId", 6},
                    {"HeliportModulation", (int)RadioModulation.AM},
                    {"HeliportFrequency", GeneratorTools.FormatRadioFrequency(radioFrequency)},
                    {"RadioBand", (int)RadioModulation.AM},
                    {"RadioFrequency", GeneratorTools.GetRadioFrequency(radioFrequency)},
                    {"playerCanDrive", false}};
                if (objectiveTargetUnitFamily == UnitFamily.StaticStructureOffshore)
                {
                    targetGroupInfo = UnitGenerator.AddUnitGroup(
                         briefingRoom,
                         ref mission,
                         [Toolbox.RandomFrom(new List<string> { "Oil rig", "Gas platform" })],
                         taskDB.TargetSide,
                         objectiveTargetUnitFamily,
                         "Static", "StaticFOB",
                         objectiveCoordinates,
                         0,
                         extraSettings);
                }
                else
                {
                    var fobTemplate = briefingRoom.Database.GetEntry<DBEntryTemplate>("FOB_Berlin");
                    targetGroupInfo =
                    UnitGenerator.AddUnitGroupTemplate(
                        briefingRoom,
                        ref mission,
                        fobTemplate, Side.Ally,
                        "Static", "StaticFOB",
                        objectiveCoordinates, 0,
                        extraSettings
                        );
                }
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{targetGroupInfo.Value.Name}\t\t{GeneratorTools.FormatRadioFrequency(radioFrequency)}\t\t");
                mission.MapData.Add($"FOB_{objectiveName}", new List<double[]> { targetGroupInfo.Value.Coordinates.ToArray() });
            }
            else
            {
                (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, objectiveTargetUnitFamilies, unitCount, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic, forceTryTemplate: targetDB.UnitCategory == UnitCategory.Ship);
                extraSettings.Add("playerCanDrive", false);
                targetGroupInfo = UnitGenerator.AddUnitGroup(
                 briefingRoom,
                 ref mission,
                 units,
                 taskDB.TargetSide,
                 objectiveTargetUnitFamily,
                 groupLua, luaUnit,
                 objectiveCoordinates,
                 groupFlags,
                 extraSettings);
            }


            mission.ObjectiveCoordinates.Add(objectiveCoordinates);
            var isStatic = objectiveTargetUnitFamily.GetUnitCategory() != UnitCategory.Ship;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, objectiveName, isStatic);

            if (airbase != null)
            {
                mission.SetValue($"Airbase{airbase.DCSID}LimitedMunitions", true);
            }
            else if (targetDB.UnitCategory != UnitCategory.Cargo)
            {
                mission.CarrierDictionary.Add(objectiveName, new CarrierGroupInfo(targetGroupInfo.Value, 1, 1, mission.TemplateRecord.ContextPlayerCoalition, false));
            }
            var objectiveWaypoints = new List<Waypoint>();

            // Choose item and count based on settings
            var requiredOverallCount = task.TargetCount switch
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
                { "AirbaseName", airbase != null ? airbase.Name : targetGroupInfo.Value.UnitNames[0] },
            };
            var itemKeys = briefingRoom.Database.GetAllEntriesIDs<DBEntryDynamicCargo>().ToList();
            int i = 0;
            do
            {
                if (i > 0)
                    objectiveIndex++;
                var itemKey = Toolbox.RandomFrom(itemKeys);
                itemKeys.Remove(itemKey);
                var luaExtraSettings = luaExtraSettingsBase.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.InvariantCultureIgnoreCase);
                var requiredCount = new MinMaxI(1, requiredOverallCount).GetValue();
                requiredOverallCount -= requiredCount;
                luaExtraSettings.Add("ItemName", itemKey);
                luaExtraSettings.Add("RequiredCount", requiredCount);
                var unitDisplayName = briefingRoom.Database.GetEntry<DBEntryDynamicCargo>(itemKey).UIDisplayName;
                var pluralIndex = requiredOverallCount == 1 ? 0 : 1;
                var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");
                ObjectiveUtils.CreateTaskString(briefingRoom.Database, ref mission, pluralIndex, ref taskString, objectiveName, objectiveTargetUnitFamily, unitDisplayName, task, luaExtraSettings);
                ObjectiveUtils.CreateLua(ref mission, targetDB, taskDB, objectiveIndex, objectiveName, targetGroupInfo.Value, taskString, task, luaExtraSettings);
                // Add briefing remarks for this objective task
                var remarksString = taskDB.BriefingRemarks.Get(mission.LangKey);
                if (!string.IsNullOrEmpty(remarksString))
                {
                    string remark = Toolbox.RandomFrom(remarksString.Split(";"));
                    GeneratorTools.ReplaceKey(ref remark, "ObjectiveName", objectiveName);
                    GeneratorTools.ReplaceKey(ref remark, "UnitFamily", briefingRoom.Database.Common.Names.UnitFamilies[(int)objectiveTargetUnitFamily].Get(mission.LangKey).Split(",")[pluralIndex]);
                    mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark);
                }
                i++;
            } while (requiredOverallCount > 0 && itemKeys.Count > 0);

            // Add feature ogg files
            foreach (string oggFile in taskDB.IncludeOgg)
                mission.AddMediaFile($"l10n/DEFAULT/{oggFile}", Path.Combine(BRPaths.INCLUDE_OGG, oggFile));


            // Add objective features Lua for this objective
            mission.AppendValue("ScriptObjectivesFeatures", ""); // Just in case there's no features

            foreach (string featureID in taskDB.RequiredFeatures.Concat(featuresID).ToHashSet())
                FeaturesObjectives.GenerateMissionFeature(briefingRoom, ref mission, featureID, objectiveName, objectiveIndex, targetGroupInfo.Value, taskDB.TargetSide, objectiveOptions, overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(briefingRoom.Database, ref mission, task, objectiveCoordinates, objectiveCoordinates, objectiveName, hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            return objectiveWaypoints;

        }
    }
}