// ESCORT MISSION OBJECTIVES
// Escort a group of units from A to B with potential threats along the way

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
    internal class Escort
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

            // Get transport origin and destination for escort
            var (originAirbase, unitCoordinates) = ObjectiveTransportUtils.GetTransportOrigin(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, objectiveCoordinates, true, ctx.ObjectiveTargetUnitFamily.GetUnitCategory());
            if (Constants.AIRBASE_LOCATIONS.Contains(targetBehaviorDB.Location) && targetDB.UnitCategory.IsAircraft())
                unitCoordinates = ObjectiveUtils.PlaceInAirbase(briefingRoom, ref ctx.Mission, ctx.ExtraSettings, targetBehaviorDB, unitCoordinates, ctx.UnitCount, ctx.UnitDBs.First(), true);
            
            var (airbase, destinationPoint) = ObjectiveTransportUtils.GetTransportDestination(briefingRoom, ref ctx.Mission, targetBehaviorDB.Location, targetBehaviorDB.Destination, unitCoordinates, objectiveCoordinates, task.TransportDistance, originAirbase?.DCSID ?? -1, true, ctx.ObjectiveTargetUnitFamily.GetUnitCategory(), targetBehaviorDB.ID.StartsWith("ToFrontLine"));
            if (airbase != null)
                ctx.ExtraSettings.Add("EndAirbaseId", airbase.DCSID);
            ctx.ObjectiveCoordinates = destinationPoint;
            ctx.UnitCoordinates = unitCoordinates;

            ctx.ExtraSettings.Add("playerCanDrive", false);
            ctx.ExtraSettings["GroupX2"] = ctx.ObjectiveCoordinates.X;
            ctx.ExtraSettings["GroupY2"] = ctx.ObjectiveCoordinates.Y;
            ctx.GroupFlags |= GroupFlags.RadioAircraftSpawn;

            ObjectiveCreationHelpers.AddAircraftSpawnFlags(ctx);

            var groupLua = targetBehaviorDB.GroupLua[(int)targetDB.DCSUnitCategory];
            if (originAirbase != null)
                ctx.ExtraSettings["HotStart"] = true;
            
            if (airbase != null && ctx.ObjectiveTargetUnitFamily.GetUnitCategory().IsAircraft())
            {
                groupLua = "AircraftLanding";
                if (ctx.Mission.AirbaseDB.Find(x => x.DCSID == airbase.DCSID).Coalition == GeneratorTools.GetSpawnPointCoalition(ctx.Mission.TemplateRecord, Side.Enemy, true))
                    groupLua = ObjectiveCreationHelpers.GetAttackingAircraftGroupLua(ctx.ObjectiveTargetUnitFamily, groupLua);
            }
            else if (ctx.ObjectiveTargetUnitFamily == UnitFamily.HelicopterTransport || ctx.ObjectiveTargetUnitFamily == UnitFamily.HelicopterUtility)
            {
                groupLua = "HeloCombatDrop";
            }

            GroupInfo? VIPGroupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
                ref ctx.Mission,
                ctx.Units,
                taskDB.TargetSide,
                ctx.ObjectiveTargetUnitFamily,
                groupLua, ctx.LuaUnit,
                unitCoordinates,
                ctx.GroupFlags,
                ctx.ExtraSettings);

            if (!VIPGroupInfo.HasValue)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToGenerateGroupObjective");

            VIPGroupInfo.Value.DCSGroups.ForEach(grp =>
            {
                grp.LateActivation = true;
                grp.Visible = task.ProgressionActivation ? task.ProgressionOptions.Contains(ObjectiveProgressionOption.PreProgressionSpottable) : true;
            });

            ObjectiveCreationHelpers.AddUnlimitedFuelTask(ctx, VIPGroupInfo.Value);

            // Setup Threats based on unit category
            var playerHasPlanes = ctx.Mission.TemplateRecord.PlayerFlightGroups.Any(x => briefingRoom.Database.GetEntry<DBEntryJSONUnit>(x.Aircraft).Category == UnitCategory.Plane) || ctx.Mission.TemplateRecord.AirbaseDynamicSpawn != DsAirbase.None;
            CreateThreats(briefingRoom, ref ctx.Mission, unitCoordinates, ctx.ObjectiveCoordinates, VIPGroupInfo, targetDB.UnitCategory, playerHasPlanes);

            ctx.ObjectiveName = ctx.Mission.WaypointNameGenerator.GetWaypointName();
            if (targetBehaviorDB.ID.EndsWith("OnRoads") && targetDB.UnitCategory.IsGroundMoving())
                briefingRoom.PrintTranslatableWarning("EscortOnRoadsImperfect", ctx.ObjectiveName);

            // Add pickup waypoint
            var cargoWaypoint = ObjectiveCreationHelpers.GenerateObjectiveWaypoint(briefingRoom.Database, ref ctx.Mission, task, unitCoordinates, unitCoordinates, $"{ctx.ObjectiveName} Pickup", scriptIgnore: true);
            ctx.Mission.Waypoints.Add(cargoWaypoint);
            ctx.ObjectiveWaypoints.Add(cargoWaypoint);

            ObjectiveUtils.AssignTargetSuffix(ref VIPGroupInfo, ctx.ObjectiveName, false);
            ObjectiveCreationHelpers.AddBriefingItems(ctx, VIPGroupInfo.Value, false);
            ObjectiveCreationHelpers.AddBriefingRemarks(ctx, ObjectiveCreationHelpers.GetPluralIndex(VIPGroupInfo.Value, false));
            ObjectiveCreationHelpers.AddOggFilesAndFeatures(ctx, VIPGroupInfo.Value);

            ctx.Mission.ObjectiveCoordinates.Add(ctx.ObjectiveCoordinates);
            
            // Add escort end trigger zone
            var zoneId = ZoneMaker.AddZone(ref ctx.Mission, $"Escort End Zone {ctx.ObjectiveName}", ctx.ObjectiveCoordinates, VIPGroupInfo.Value.UnitDB.IsAircraft ? briefingRoom.Database.Common.DropOffDistanceMeters * 10 : briefingRoom.Database.Common.DropOffDistanceMeters);
            TriggerMaker.AddEscortEndTrigger(ref ctx.Mission, zoneId, VIPGroupInfo.Value.GroupID, objectiveIndex);

            // Update ref parameters
            mission = ctx.Mission;
            objectiveCoordinates = ctx.ObjectiveCoordinates;

            return ObjectiveCreationHelpers.FinalizeObjective(ctx, VIPGroupInfo.Value);
        }

        private static void CreateThreats(IBriefingRoom briefingRoom, ref DCSMission mission, Coordinates unitCoordinates, Coordinates objectiveCoordinates, GroupInfo? VIPGroupInfo, UnitCategory unitCategory, bool playerHasPlanes)
        {
            switch (unitCategory)
            {
                case UnitCategory.Plane:
                case UnitCategory.Helicopter:
                    CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAP");
                    break;
                case UnitCategory.Ship:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAS");
                    if (Toolbox.RollChance(AmountNR.Average)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Helo");
                    if (Toolbox.RollChance(AmountNR.Low)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Ship");
                    break;
                default:
                    if (playerHasPlanes && Toolbox.RollChance(AmountNR.High)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "CAS");
                    if (Toolbox.RollChance(AmountNR.Average)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Helo");
                    if (Toolbox.RollChance(AmountNR.VeryHigh)) CreateThreat(briefingRoom, ref mission, unitCoordinates, objectiveCoordinates, VIPGroupInfo, "Ground");
                    break;
            }
        }

        private static void CreateThreat(IBriefingRoom briefingRoom, ref DCSMission mission, Coordinates unitCoordinates, Coordinates objectiveCoordinates, GroupInfo? VIPGroupInfo, string type)
        {
            var (threatMinMax, threatUnitFamilies, groupLua, unitLua, unitCount, validSpawns, spawnDistance) = GetThreatValues(type);
            var threatExtraSettings = new Dictionary<string, object>
            {
                { "ObjectiveGroupID", VIPGroupInfo.Value.GroupID }
            };
            var zoneCoords = Coordinates.Lerp(unitCoordinates, objectiveCoordinates, threatMinMax.GetValue());
            threatExtraSettings["GroupX2"] = zoneCoords.X;
            threatExtraSettings["GroupY2"] = zoneCoords.Y;
            var groupFlags = GroupFlags.RadioAircraftSpawn;
            var (threatUnits, threatUnitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, threatUnitFamilies, unitCount.GetValue(), Side.Enemy, groupFlags, ref threatExtraSettings, false);
            var spawnPoint = SpawnPointSelector.GetRandomSpawnPoint(briefingRoom.Database, ref mission, validSpawns, zoneCoords, spawnDistance, coalition: GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, Side.Enemy));
            if (!spawnPoint.HasValue || threatUnits.Count == 0 || threatUnitDBs.Count == 0)
            {
                BriefingRoom.PrintToLog($"Failed to create threat for escort mission objective at {zoneCoords}. No valid spawn point or units found.");
                return;
            }
            GroupInfo? threatGroupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
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

        private static (MinMaxD, List<UnitFamily>, string, string, MinMaxI, SpawnPointType[], MinMaxD) GetThreatValues(string type) =>
            type switch
            {
                "CAP" => (new MinMaxD(0.1, 0.9), new List<UnitFamily> { UnitFamily.PlaneFighter, UnitFamily.PlaneInterceptor }, "AircraftCAPAttacking", "Aircraft", new MinMaxI(1, 4), new[] { SpawnPointType.Air }, new MinMaxD(20, 60)),
                "CAS" => (new MinMaxD(0.15, 0.8), new List<UnitFamily> { UnitFamily.PlaneAttack, UnitFamily.PlaneStrike }, "AircraftCASAttacking", "Aircraft", new MinMaxI(1, 4), new[] { SpawnPointType.Air }, new MinMaxD(10, 40)),
                "Helo" => (new MinMaxD(0.02, 0.7), new List<UnitFamily> { UnitFamily.HelicopterAttack }, "AircraftCASAttacking", "Aircraft", new MinMaxI(1, 4), new[] { SpawnPointType.Air }, new MinMaxD(10, 20)),
                "Ship" => (new MinMaxD(0.05, 0.6), new List<UnitFamily> { UnitFamily.ShipCruiser, UnitFamily.ShipFrigate, UnitFamily.ShipSpeedboat, UnitFamily.ShipSubmarine }, "ShipAttacking", "Ship", new MinMaxI(1, 4), new[] { SpawnPointType.Sea }, new MinMaxD(30, 60)),
                _ => (new MinMaxD(0.05, 0.7), new List<UnitFamily> { UnitFamily.VehicleAPC, UnitFamily.VehicleMBT, UnitFamily.Infantry }, "VehicleAttackingUncontrolled", "Vehicle", new MinMaxI(1, 10), new[] { SpawnPointType.LandMedium, SpawnPointType.LandLarge }, new MinMaxD(5, 20))
            };
    }
}