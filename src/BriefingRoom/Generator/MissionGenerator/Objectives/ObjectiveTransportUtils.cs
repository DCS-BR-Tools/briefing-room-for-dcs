using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission.Objectives
{
    /// <summary>
    /// Utility methods for transport objective origin/destination handling.
    /// </summary>
    internal static class ObjectiveTransportUtils
    {
#nullable enable

        #region Transport Origin/Destination
        
        /// <summary>
        /// Gets the origin location for transport objectives based on behavior location setting.
        /// </summary>
        internal static (DBEntryAirbase? airbase, Coordinates coords) GetTransportOrigin(
            IBriefingRoom briefingRoom,
            ref DCSMission mission,
            DBEntryObjectiveTargetBehaviorLocation location,
            Coordinates objectiveCoordinates,
            bool isEscort = false,
            UnitCategory? unitCategory = null)
        {
            return location switch
            {
                DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase => 
                    GetAirbaseCargoSpot(briefingRoom, ref mission, mission.PlayerAirbase.Coordinates),
                    
                DBEntryObjectiveTargetBehaviorLocation.Airbase => 
                    GetAirbaseCargoSpot(briefingRoom, ref mission, objectiveCoordinates, mission.PlayerAirbase.DCSID),
                    
                DBEntryObjectiveTargetBehaviorLocation.NearAirbase => 
                    GetNearAirbaseOrigin(briefingRoom, ref mission, objectiveCoordinates, isEscort, unitCategory),
                    
                _ => (null, objectiveCoordinates)
            };
        }

        private static (DBEntryAirbase? airbase, Coordinates coords) GetNearAirbaseOrigin(
            IBriefingRoom briefingRoom,
            ref DCSMission mission,
            Coordinates objectiveCoordinates,
            bool isEscort,
            UnitCategory? unitCategory)
        {
            var (airbaseDB, coords) = GetAirbaseCargoSpot(briefingRoom, ref mission, objectiveCoordinates);
            coords = ObjectiveUtils.GetNearestSpawnCoordinates(
                briefingRoom.Database, ref mission, coords, 
                GetSpawnPointTypes(isEscort, unitCategory), true);
            return (airbaseDB, coords);
        }

        /// <summary>
        /// Gets the destination location for transport objectives based on origin and destination behavior settings.
        /// </summary>
        internal static (DBEntryAirbase? airbase, Coordinates coords) GetTransportDestination(
            IBriefingRoom briefingRoom,
            ref DCSMission mission,
            DBEntryObjectiveTargetBehaviorLocation originBehavior,
            DBEntryObjectiveTargetBehaviorLocation destinationBehavior,
            Coordinates originCoordinates,
            Coordinates objectiveCoordinates,
            MinMaxD transportDistance,
            int originAirbaseId,
            bool isEscort = false,
            UnitCategory? unitCategory = null,
            bool enemyAllowed = false)
        {
            var transportDistanceRange = GetEffectiveTransportDistance(transportDistance, isEscort, unitCategory);
            var spawnTypes = GetSpawnPointTypes(isEscort, unitCategory);

            return (destinationBehavior, originBehavior) switch
            {
                (DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase, _) => 
                    GetAirbaseCargoSpot(briefingRoom, ref mission, mission.PlayerAirbase.Coordinates),
                    
                (DBEntryObjectiveTargetBehaviorLocation.Airbase, DBEntryObjectiveTargetBehaviorLocation.Default) => 
                    GetAirbaseCargoSpotWithin(briefingRoom, mission, objectiveCoordinates, transportDistanceRange, originAirbaseId, enemyAllowed),
                    
                (DBEntryObjectiveTargetBehaviorLocation.Airbase, DBEntryObjectiveTargetBehaviorLocation.PlayerAirbase) or
                (DBEntryObjectiveTargetBehaviorLocation.Airbase, DBEntryObjectiveTargetBehaviorLocation.Airbase) => 
                    GetAirbaseCargoSpotWithin(briefingRoom, mission, originCoordinates, transportDistanceRange, originAirbaseId, enemyAllowed),
                    
                (DBEntryObjectiveTargetBehaviorLocation.Default, DBEntryObjectiveTargetBehaviorLocation.Default) => // Relocate
                    (null, ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, 
                        originCoordinates.CreateRandomNM(transportDistanceRange), spawnTypes, false)),
                    
                (DBEntryObjectiveTargetBehaviorLocation.Default, _) => 
                    (null, ObjectiveUtils.GetNearestSpawnCoordinates(briefingRoom.Database, ref mission, 
                        objectiveCoordinates, spawnTypes, false)),
                    
                _ => throw new BriefingRoomRawException(
                    $"Unsupported transport destination behavior: {destinationBehavior} origin: {originBehavior}")
            };
        }

        private static MinMaxD GetEffectiveTransportDistance(MinMaxD transportDistance, bool isEscort, UnitCategory? unitCategory)
        {
            if (transportDistance.Max != 0)
                return transportDistance;
                
            if (isEscort)
                return GetStandardMoveDistanceRange(unitCategory ?? UnitCategory.Plane);
                
            return new MinMaxD(1, 100); // Default transport distance range
        }

        #endregion

        #region Spawn Point Types

        /// <summary>
        /// Gets appropriate spawn point types based on escort status and unit category.
        /// </summary>
        internal static SpawnPointType[] GetSpawnPointTypes(bool isEscort = false, UnitCategory? unitCategory = null)
        {
            if (!isEscort || !unitCategory.HasValue)
                return [SpawnPointType.LandLarge, SpawnPointType.LandMedium];

            if (unitCategory.Value.IsAircraft())
                return [SpawnPointType.Air];
                
            if (unitCategory.Value == UnitCategory.Ship)
                return [SpawnPointType.Sea];
                
            return [SpawnPointType.LandLarge, SpawnPointType.LandMedium];
        }

        /// <summary>
        /// Gets standard movement distance range for a unit category.
        /// </summary>
        internal static MinMaxD GetStandardMoveDistanceRange(UnitCategory unitCategory) =>
            unitCategory switch
            {
                UnitCategory.Plane => new MinMaxD(30, 60),
                UnitCategory.Ship => new MinMaxD(15, 40),
                UnitCategory.Helicopter => new MinMaxD(10, 20),
                UnitCategory.Infantry => new MinMaxD(1, 5),
                _ => new MinMaxD(5, 10)
            };

        #endregion

        #region Airbase Cargo Spots

        /// <summary>
        /// Gets a cargo spawn spot at the nearest suitable airbase.
        /// </summary>
        internal static (DBEntryAirbase? airbase, Coordinates coords) GetAirbaseCargoSpot(
            IBriefingRoom briefingRoom, 
            ref DCSMission mission, 
            Coordinates coords, 
            int ignoreAirbaseId = -1, 
            bool enemyAllowed = false)
        {
            var side = enemyAllowed ? Toolbox.RandomFrom(new[] { Side.Enemy, Side.Ally }) : Side.Ally;
            var targetCoalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, side, true);
            
            var playerHasPlaneTransport = mission.TemplateRecord.PlayerFlightGroups
                .Any(x => briefingRoom.Database.GetEntry<DBEntryJSONUnit>(x.Aircraft).Families.Contains(UnitFamily.PlaneTransport));
            
            var aircraftType = playerHasPlaneTransport ? "An-26B" : "Mi-8MT";
            var parkingResult = SpawnPointSelector.GetAirbaseAndParking(
                briefingRoom, mission, coords, 1, targetCoalition, 
                (DBEntryAircraft)briefingRoom.Database.GetEntry<DBEntryJSONUnit>(aircraftType), 
                new[] { ignoreAirbaseId });
            var airbaseDB = parkingResult.Item1;
            var spawnPoints = parkingResult.Item3;
            
            if (spawnPoints.Count == 0)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToFindCargoSpawn");
            
            var airbaseCoords = Toolbox.RandomFrom(spawnPoints);
            if (targetCoalition.HasValue)
                mission.PopulatedAirbaseIds[targetCoalition.Value].Add(airbaseDB.DCSID);
            
            return (airbaseDB, airbaseCoords);
        }

        /// <summary>
        /// Gets a cargo spawn spot at an airbase within the specified distance range.
        /// </summary>
        internal static (DBEntryAirbase? airbase, Coordinates coords) GetAirbaseCargoSpotWithin(
            IBriefingRoom briefingRoom, 
            DCSMission mission, 
            Coordinates searchCenterCoords, 
            MinMaxD distanceRange, 
            int ignoreAirbaseId = -1, 
            bool enemyAllowed = false)
        {
            var side = enemyAllowed ? Toolbox.RandomFrom(new[] { Side.Enemy, Side.Ally }) : Side.Ally;
            var coalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, side, false);
            
            var airbaseOptions = mission.AirbaseDB
                .Where(a => a.DCSID != ignoreAirbaseId && 
                           (coalition == null || coalition == Coalition.Neutral || a.Coalition == coalition) && 
                           distanceRange.Contains(searchCenterCoords.GetDistanceFrom(a.Coordinates) * Toolbox.METERS_TO_NM))
                .ToList();
            
            if (airbaseOptions.Count == 0)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "FailedToFindCargoSpawn");
            
            var airbase = Toolbox.RandomFrom(airbaseOptions);
            return GetAirbaseCargoSpot(briefingRoom, ref mission, airbase.Coordinates, ignoreAirbaseId, enemyAllowed);
        }

        #endregion
    }
}
