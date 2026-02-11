using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    /// <summary>
    /// Core utility methods for objective generation including unit data, spawn handling, and validation.
    /// </summary>
    /// <remarks>
    /// Related classes:
    /// - <see cref="ObjectiveContext"/> - Context data container
    /// - <see cref="ObjectiveTransportUtils"/> - Transport origin/destination utilities
    /// - <see cref="ObjectiveCreationHelpers"/> - Briefing, Lua, and creation helpers
    /// </remarks>
    internal static class ObjectiveUtils
    {
        internal static (string luaUnit, int unitCount, MinMaxI unitCountMinMax, List<UnitFamily> objectiveTargetUnitFamily, GroupFlags groupFlags) GetUnitData(MissionTemplateSubTaskRecord task, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, ObjectiveOption[] objectiveOptions)
        {
            GroupFlags groupFlags = 0;
            if (objectiveOptions.Contains(ObjectiveOption.Invisible)) groupFlags |= GroupFlags.Invisible;
            if (objectiveOptions.Contains(ObjectiveOption.ShowTarget)) groupFlags = GroupFlags.NeverHidden;
            else if (objectiveOptions.Contains(ObjectiveOption.HideTarget)) groupFlags = GroupFlags.AlwaysHidden;
            if (objectiveOptions.Contains(ObjectiveOption.EmbeddedAirDefense)) groupFlags |= GroupFlags.EmbeddedAirDefense;
            List<UnitFamily> unitFamilies;
            switch (true)
            {
                case true when targetDB.ID == "VehicleAny":
                    unitFamilies = Toolbox.RandomFrom(Constants.MIXED_VEHICLE_SETS);
                    break;
                case true when targetDB.ID == "InfantryAny":
                    unitFamilies = Toolbox.RandomFrom(Constants.MIXED_INFANTRY_SETS);
                    break;
                case true when targetDB.ID == "GroundAny":
                    unitFamilies = Toolbox.RandomFrom(Constants.MIXED_INFANTRY_SETS).Concat(Toolbox.RandomFrom(Constants.MIXED_VEHICLE_SETS)).ToList();
                    break;
                default:
                    unitFamilies = [Toolbox.RandomFrom(targetDB.UnitFamilies)];
                    break;
            }

            return (targetBehaviorDB.UnitLua[(int)targetDB.DCSUnitCategory],
                targetDB.UnitCount[(int)task.TargetCount].GetValue(),
                targetDB.UnitCount[(int)task.TargetCount],
                unitFamilies,
                groupFlags
            );
        }

        internal static Coordinates PlaceInAirbase(IBriefingRoom briefingRoom, ref DCSMission mission, Dictionary<string, object> extraSettings, DBEntryObjectiveTargetBehavior targetBehaviorDB, Coordinates objectiveCoordinates, int unitCount, DBEntryJSONUnit unitDB, bool Friendly = false)
        {
            int airbaseID = 0;
            var parkingSpotIDsList = new List<int>();
            var parkingSpotCoordinatesList = new List<Coordinates>();
            var enemyCoalition = mission.TemplateRecord.ContextPlayerCoalition.GetEnemy();
            var allyCoalition = mission.TemplateRecord.ContextPlayerCoalition;
            var playerAirbaseDCSID = mission.PlayerAirbase.DCSID;
            var spawnAnywhere = mission.TemplateRecord.SpawnAnywhere;
            var targetAirbaseOptions =
                (from DBEntryAirbase airbaseDB in mission.AirbaseDB
                 where airbaseDB.DCSID != playerAirbaseDCSID && (spawnAnywhere || (airbaseDB.Coalition == (Friendly ? allyCoalition : enemyCoalition)))
                 select airbaseDB).OrderBy(x => x.Coordinates.GetDistanceFrom(objectiveCoordinates));

            BriefingRoomRawException exception = null;
            foreach (var targetAirbase in targetAirbaseOptions)
            {
                try
                {
                    airbaseID = targetAirbase.DCSID;
                    var parkingSpots = SpawnPointSelector.GetFreeParkingSpots(
                        briefingRoom,
                        ref mission,
                        targetAirbase.DCSID,
                        unitCount, (DBEntryAircraft)unitDB,
                        targetBehaviorDB.Location == DBEntryObjectiveTargetBehaviorLocation.AirbaseParkingNoHardenedShelter);

                    parkingSpotIDsList = parkingSpots.Select(x => x.DCSID).ToList();
                    parkingSpotCoordinatesList = parkingSpots.Select(x => x.Coordinates).ToList();

                    extraSettings.Add("GroupAirbaseID", airbaseID);
                    extraSettings.Add("ParkingID", parkingSpotIDsList);
                    extraSettings.Add("UnitCoords", parkingSpotCoordinatesList);
                    return Toolbox.RandomFrom(parkingSpotCoordinatesList);
                }
                catch (BriefingRoomRawException e)
                {
                    exception = e;
                    throw;
                }
            }
            throw exception;
        }

        internal static void AddEmbeddedAirDefenseUnits(IBriefingRoom briefingRoom, ref DCSMission mission, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB, Coordinates objectiveCoordinates, GroupFlags groupFlags, Dictionary<string, object> extraSettings)
        {
            // Static targets (aka buildings) need to have their "embedded" air defenses spawned in another group
            var airDefenseUnits = GeneratorTools.GetEmbeddedAirDefenseUnits(briefingRoom, mission.TemplateRecord, taskDB.TargetSide, UnitCategory.Static);

            if (airDefenseUnits.Count > 0)
                UnitGenerator.AddUnitGroup(
                    briefingRoom,
                    ref mission,
                    airDefenseUnits,
                    taskDB.TargetSide, UnitFamily.VehicleAAA,
                    targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory], targetBehaviorDB.UnitLua[(int)targetDB.DCSUnitCategory],
                    objectiveCoordinates + Coordinates.CreateRandom(100, 500),
                    groupFlags,
                    extraSettings);
        }

        internal static Coordinates GetNearestSpawnCoordinates(IDatabase database, ref DCSMission mission, Coordinates coreCoordinates, SpawnPointType[] validSpawnPoints, bool remove = true)
        {
            Coordinates? spawnPoint = SpawnPointSelector.GetNearestSpawnPoint(
                mission,
                validSpawnPoints,
                coreCoordinates, remove);

            if (!spawnPoint.HasValue)
                throw new BriefingRoomException(database, mission.LangKey, "FailedToLaunchNearbyObjective", String.Join(",", validSpawnPoints.Select(x => x.ToString()).ToList()));

            Coordinates objectiveCoordinates = spawnPoint.Value;
            return objectiveCoordinates;
        }

        internal static void AssignTargetSuffix(ref GroupInfo? targetGroupInfo, string objectiveName, Boolean isStatic)
        {
            var i = 0;
            targetGroupInfo.Value.DCSGroups.ForEach(x =>
            {
                x.Name += $"{(i == 0 ? "" : i)}-TGT-{objectiveName}";
                if (isStatic) x.Units.ForEach(u => u.Name += $"{(i == 0 ? "" : i)}-TGT-{objectiveName}");
                i++;
            });
        }

        internal static (DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB, ObjectiveOption[] objectiveOptions, DBEntryObjectivePreset presetDB) GetCustomObjectiveData(IDatabase database, string langKey, MissionTemplateSubTaskRecord objectiveTemplate)
        {
            var targetDB = database.GetEntry<DBEntryObjectiveTarget>(objectiveTemplate.Target);
            var targetBehaviorDB = database.GetEntry<DBEntryObjectiveTargetBehavior>(objectiveTemplate.TargetBehavior);
            var taskDB = database.GetEntry<DBEntryObjectiveTask>(objectiveTemplate.Task);
            var objectiveOptions = objectiveTemplate.Options.ToArray();
            DBEntryObjectivePreset presetDB = null;

            if (objectiveTemplate.HasPreset)
            {
                presetDB = database.GetEntry<DBEntryObjectivePreset>(objectiveTemplate.Preset);
                if (presetDB != null)
                {
                    targetDB = database.GetEntry<DBEntryObjectiveTarget>(Toolbox.RandomFrom(presetDB.Targets));
                    targetBehaviorDB = database.GetEntry<DBEntryObjectiveTargetBehavior>(Toolbox.RandomFrom(presetDB.TargetsBehaviors));
                    taskDB = database.GetEntry<DBEntryObjectiveTask>(presetDB.Task);
                    objectiveOptions = presetDB.Options.Concat(objectiveTemplate.Options).Distinct().ToArray();
                }
            }

            ObjectiveNullCheck(database, langKey, targetDB, targetBehaviorDB, taskDB);
            return (targetDB, targetBehaviorDB, taskDB, objectiveOptions, presetDB);
        }

        internal static void ObjectiveNullCheck(IDatabase database, string langKey, DBEntryObjectiveTarget targetDB, DBEntryObjectiveTargetBehavior targetBehaviorDB, DBEntryObjectiveTask taskDB)
        {
            if (targetDB == null) throw new BriefingRoomException(database, langKey, "TargetNotFound", targetDB.UIDisplayName);
            if (targetBehaviorDB == null) throw new BriefingRoomException(database, langKey, "BehaviorNotFound", targetBehaviorDB.UIDisplayName);
            if (taskDB == null) throw new BriefingRoomException(database, langKey, "TaskNotFound", taskDB.UIDisplayName);
            if (!taskDB.ValidUnitCategories.Contains(targetDB.UnitCategory))
                throw new BriefingRoomException(database, langKey, "TaskTargetsInvalid", taskDB.UIDisplayName, targetDB.UnitCategory);
        }
    }
}
