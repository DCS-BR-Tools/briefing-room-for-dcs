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
        private static readonly Dictionary<string, string> TRANSPORT_CARGO_DICT = new Dictionary<string, string>
        {
            { "weapons.bombs.BEER_BOMB", "Beer Bomb" },
            { "weapons.bombs.CBU_87", "CBU-87 Bombs"},
            { "weapons.bombs.Mk_83", "Mk-83 Bombs"},
            { "weapons.bombs.Mk_84", "Mk-84 Bombs"},
            { "weapons.bombs.RN-24", "RN-24 470kg Nuclear Bomb"},
            { "weapons.bombs.RN-28", "RN-28 260kg Nuclear Bomb"},
        };
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
            if (airbase == null)
            {
                if (targetDB.UnitCategory == UnitCategory.Static)
                {
                    throw new NotImplementedException("TransportDynamicCargo FOB is not implemented.");
                }
                else if (targetDB.UnitCategory == UnitCategory.Ship)
                    objectiveCoordinates = ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, objectiveCoordinates, targetDB.ValidSpawnPoints);
            }

            var objectiveTargetUnitFamily = objectiveTargetUnitFamilies.First();
            (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, objectiveTargetUnitFamilies, 1, taskDB.TargetSide, groupFlags, ref extraSettings, targetBehaviorDB.IsStatic);
            extraSettings.Add("playerCanDrive", false);

            GroupInfo? targetGroupInfo = UnitGenerator.AddUnitGroup(
            briefingRoom,
            ref mission,
            units,
            taskDB.TargetSide,
            objectiveTargetUnitFamily,
            briefingRoom.Database.GetEntry<DBEntryObjectiveTargetBehavior>("Idle").GroupLua[(int)targetDB.DCSUnitCategory], luaUnit,
            objectiveCoordinates,
            groupFlags,
            extraSettings);


            mission.ObjectiveCoordinates.Add(objectiveCoordinates);
            var objectiveName = mission.WaypointNameGenerator.GetWaypointName();
            var isStatic = objectiveTargetUnitFamily.GetUnitCategory() == UnitCategory.Cargo;
            ObjectiveUtils.AssignTargetSuffix(ref targetGroupInfo, objectiveName, isStatic);

            if (airbase != null)
            {
                mission.SetValue($"Airbase{airbase.DCSID}LimitedMunitions", true);
            }
            else if (targetDB.UnitCategory == UnitCategory.Ship)
            {
                mission.CarrierDictionary.Add(objectiveName, new CarrierGroupInfo(targetGroupInfo.Value, 1, 1, mission.TemplateRecord.ContextPlayerCoalition, false));
            }
            var objectiveWaypoints = new List<Waypoint>();

            // Choose item and count based on settings
            var requiredCount = unitCountMinMax.GetValue();
            var pluralIndex = requiredCount == 1 ? 0 : 1;
            var taskString = GeneratorTools.ParseRandomString(taskDB.BriefingTask[pluralIndex].Get(mission.LangKey), mission).Replace("\"", "''");
            var itemName = Toolbox.RandomFrom(TRANSPORT_CARGO_DICT.Keys.ToList());
            var luaExtraSettings = new Dictionary<string, object>
            {
                { "AirbaseName", airbase != null ? airbase.Name : targetGroupInfo.Value.UnitNames[0] },
                { "ItemName", itemName },
                { "RequiredCount", requiredCount + 100  }
            };
            var unitDisplayName = new LanguageString(TRANSPORT_CARGO_DICT[itemName]);
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

            // Add feature ogg files
            foreach (string oggFile in taskDB.IncludeOgg)
                mission.AddMediaFile($"l10n/DEFAULT/{oggFile}", Path.Combine(BRPaths.INCLUDE_OGG, oggFile));


            // Add objective features Lua for this objective
            mission.AppendValue("ScriptObjectivesFeatures", ""); // Just in case there's no features

            foreach (string featureID in taskDB.RequiredFeatures.Concat(featuresID).ToHashSet())
                FeaturesObjectives.GenerateMissionFeature(briefingRoom, ref mission, featureID, objectiveName, objectiveIndex, targetGroupInfo.Value, taskDB.TargetSide, objectiveOptions, overrideCoords: targetBehaviorDB.ID.StartsWith("ToFrontLine") ? objectiveCoordinates : null);

            var cargoWaypoint = ObjectiveUtils.GenerateObjectiveWaypoint(briefingRoom.Database, ref mission, task, unitCoordinates, unitCoordinates, $"{objectiveName} Pickup", scriptIgnore: true);
            mission.Waypoints.Add(cargoWaypoint);
            objectiveWaypoints.Add(cargoWaypoint);

            var objCoords = objectiveCoordinates;
            mission.ObjectiveTargetUnitFamilies.Add(objectiveTargetUnitFamily);
            var waypoint = ObjectiveUtils.GenerateObjectiveWaypoint(briefingRoom.Database, ref mission, task, objectiveCoordinates, objectiveCoordinates, objectiveName, hiddenMapMarker: task.ProgressionOptions.Contains(ObjectiveProgressionOption.ProgressionHiddenBrief));
            mission.Waypoints.Add(waypoint);
            objectiveWaypoints.Add(waypoint);
            mission.MapData.Add($"OBJECTIVE_AREA_{objectiveIndex}", new List<double[]> { waypoint.Coordinates.ToArray() });
            return objectiveWaypoints;

        }
    }
}