/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using Shrulik.NKDBush;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.UnitMaker
{
    internal static class SpawnPointSelector
    {
        private const int MAX_RADIUS_SEARCH_ITERATIONS = 15;

        internal static List<DBEntryAirbaseParkingSpot> GetFreeParkingSpots(IBriefingRoom briefingRoom,ref DCSMission mission, int airbaseID, int unitCount, DBEntryAircraft aircraftDB, bool requiresOpenAirParking = false, int reservedSpots = 0)
        {

            if (!mission.AirbaseParkingSpots.ContainsKey(airbaseID))
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "AirbaseNotFound", airbaseID);
            var airbaseDB = mission.AirbaseDB.First(x => x.DCSID == airbaseID);
            var parkingSpots = new List<DBEntryAirbaseParkingSpot>();
            DBEntryAirbaseParkingSpot? lastSpot = null;
            for (int i = 0; i < unitCount; i++)
            {
                var viableSpots = FilterAndSortSuitableSpots(briefingRoom, mission.AirbaseParkingSpots[airbaseID].ToArray(), aircraftDB, requiresOpenAirParking, lastSpot);
                if (viableSpots.Count <= reservedSpots) throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "AirbaseNotEnoughParkingSpots", airbaseDB.UIDisplayName.Get(mission.LangKey));
                var parkingSpot = viableSpots.First();
                lastSpot = parkingSpot;
                mission.AirbaseParkingSpots[airbaseID].Remove(parkingSpot);
                parkingSpots.Add(parkingSpot);
            }

            return parkingSpots;
        }

        internal static Coordinates? GetNearestSpawnPoint(
            DCSMission mission,
            SpawnPointType[] validTypes,
            Coordinates origin, bool remove = true)
        {
            if (validTypes.Contains(SpawnPointType.Air) || validTypes.Contains(SpawnPointType.Sea))
                return GetAirOrSeaCoordinates(mission, validTypes, origin, new MinMaxD(1, 30));

            var sp = mission.SpawnPoints.Where(x => validTypes.Contains(x.PointType)).Aggregate((acc, x) => origin.GetDistanceFrom(x.Coordinates) < origin.GetDistanceFrom(acc.Coordinates) ? x : acc);
            if (remove)
            {
                mission.SpawnPoints.Remove(sp);
                mission.UsedSpawnPoints.Add(sp);
            }
            return sp.Coordinates;
        }

        internal static Coordinates? GetRandomSpawnPoint(
            IDatabase database,
            ref DCSMission mission,
            SpawnPointType[] validTypes,
            Coordinates distanceOrigin1, MinMaxD distanceFrom1,
            Coordinates? distanceOrigin2 = null, MinMaxD? distanceFrom2 = null,
            Coalition? coalition = null,
            UnitFamily? nearFrontLineFamily = null)
        {
            if (validTypes.Contains(SpawnPointType.Air) || validTypes.Contains(SpawnPointType.Sea))
                return GetAirOrSeaCoordinates(mission, validTypes, distanceOrigin1, distanceFrom1, distanceOrigin2, distanceFrom2, coalition);
            return GetLandCoordinates(database, mission, validTypes, distanceOrigin1, distanceFrom1, distanceOrigin2, distanceFrom2, coalition, nearFrontLineFamily);
        }

        private static Coordinates? GetLandCoordinates(
            IDatabase database,
            DCSMission mission,
            SpawnPointType[] validTypes,
            Coordinates distanceOrigin1, MinMaxD distanceFrom1,
            Coordinates? distanceOrigin2 = null, MinMaxD? distanceFrom2 = null,
            Coalition? coalition = null,
            UnitFamily? nearFrontLineFamily = null,
            bool nested = false
        )
        {
            var useFrontLine = nearFrontLineFamily.HasValue && mission.FrontLine.Count > 0 && Constants.NEAR_FRONT_LINE_CATEGORIES.Contains(nearFrontLineFamily.Value.GetUnitCategory());
            var validSP = from DBEntryTheaterSpawnPoint pt in mission.SpawnPoints where validTypes.Contains(pt.PointType) select pt;
            Coordinates?[] distanceOrigin = [distanceOrigin1, distanceOrigin2];
            MinMaxD?[] distanceFrom = [distanceFrom1, distanceFrom2];
            for (int i = 0; i < 2; i++)
            {
                if (!validSP.Any()) break;
                if (!distanceFrom[i].HasValue || !distanceOrigin[i].HasValue) continue;

                var borderLimit = (double)mission.TemplateRecord.BorderLimit;
                Coordinates origin = distanceOrigin[i].Value;
                var searchRange = distanceFrom[i].Value * Toolbox.NM_TO_METERS; // convert distance to meters

                IEnumerable<DBEntryTheaterSpawnPoint> validSPInRange;

                int iterationsLeft = MAX_RADIUS_SEARCH_ITERATIONS;

                var validSPArray = validSP.ToArray();
                var index = new KDBush<DBEntryTheaterSpawnPoint>(validSPArray, p => p.Coordinates.X, p => p.Coordinates.Y);
                do
                {
                    var within = index.Within(origin.X, origin.Y, searchRange.Max).Select(x => validSPArray[x]);
                    validSPInRange = (from DBEntryTheaterSpawnPoint s in within
                                      where
                                        searchRange.Contains(origin.GetDistanceFrom(s.Coordinates)) &&
                                        CheckNotInHostileCoords(ref mission, s.Coordinates, coalition) &&
                                        (useFrontLine ? CheckNotFarFromFrontLine(database, ref mission, s.Coordinates, nearFrontLineFamily.Value, coalition) : CheckNotFarFromBorders(ref mission, s.Coordinates, borderLimit, coalition))
                                      select s);
                    searchRange = new MinMaxD(searchRange.Min * 0.95, searchRange.Max * 1.05);
                    if (iterationsLeft < MAX_RADIUS_SEARCH_ITERATIONS * 0.3)
                        borderLimit *= 1.05;
                    iterationsLeft--;
                } while ((!validSPInRange.Any()) && (iterationsLeft > 0));
                validSP = validSPInRange;
            }

            if (!validSP.Any())
                return !coalition.HasValue && (useFrontLine || nested) ? null : GetLandCoordinates(database, mission, validTypes, distanceOrigin1, distanceFrom1, distanceOrigin2, distanceFrom2, null, nearFrontLineFamily, true);
            DBEntryTheaterSpawnPoint selectedSpawnPoint = Toolbox.RandomFrom(validSP.ToArray());
            mission.SpawnPoints.Remove(selectedSpawnPoint); // Remove spawn point so it won't be used again;
            mission.UsedSpawnPoints.Add(selectedSpawnPoint);
            return selectedSpawnPoint.Coordinates;
        }

        private static Coordinates? GetAirOrSeaCoordinates(
            DCSMission mission,
            SpawnPointType[] validTypes,
            Coordinates distanceOrigin1, MinMaxD distanceFrom1,
            Coordinates? distanceOrigin2 = null, MinMaxD? distanceFrom2 = null,
            Coalition? coalition = null)
        {
            var searchRange = distanceFrom1 * Toolbox.NM_TO_METERS;
            var borderLimit = (double)mission.TemplateRecord.BorderLimit; ;
            MinMaxD? secondSearchRange = null;
            if (distanceOrigin2.HasValue && distanceFrom2.HasValue)
            {
                secondSearchRange = distanceFrom2.Value * Toolbox.NM_TO_METERS;
            }

            var iterations = 0;
            do
            {
                var coordOptionsLinq = Enumerable.Range(0, 300)
                    .Select(x => Coordinates.CreateRandom(distanceOrigin1, searchRange))
                    .Where(x => CheckNotInHostileCoords(ref mission, x, coalition) && CheckNotInNoSpawnCoords(mission.SituationDB, x) && CheckNotFarFromBorders(ref mission, x, borderLimit, coalition));

                if (secondSearchRange.HasValue)
                    coordOptionsLinq = coordOptionsLinq.Where(x => secondSearchRange.Value.Contains(distanceOrigin2.Value.GetDistanceFrom(x)));

                if (validTypes.First() == SpawnPointType.Sea) //sea position
                    coordOptionsLinq = coordOptionsLinq.Where(x => CheckInSea(mission.TheaterDB, x));

                var coordOptions = coordOptionsLinq.ToList();
                if (coordOptions.Count > 0)
                    return Toolbox.RandomFrom(coordOptions);

                searchRange = new MinMaxD(searchRange.Min * 0.95, searchRange.Max * 1.15);

                if (secondSearchRange.HasValue)
                    secondSearchRange = new MinMaxD(secondSearchRange.Value.Min * 0.95, secondSearchRange.Value.Max * 1.05);

                if (iterations > MAX_RADIUS_SEARCH_ITERATIONS * 0.66)
                    borderLimit *= 1.05;

                iterations++;
            } while (iterations < MAX_RADIUS_SEARCH_ITERATIONS);

            return null;
        }

        internal static Tuple<DBEntryAirbase, List<int>, List<Coordinates>> GetAirbaseAndParking(
            IBriefingRoom briefingRoom,
                    DCSMission mission, Coordinates coordinates,
                    int unitCount, Coalition coalition, DBEntryAircraft aircraftDB, int[] excludeIds = null)
        {
                    if (excludeIds == null)
                        excludeIds = [];
                    var targetAirbaseOptions =
                        (from DBEntryAirbase airbaseDB in mission.AirbaseDB
                         where !excludeIds.Contains(airbaseDB.DCSID) && (coalition == Coalition.Neutral || airbaseDB.Coalition == coalition) && mission.AirbaseParkingSpots.ContainsKey(airbaseDB.DCSID) && ValidateAirfieldParking(mission.AirbaseParkingSpots[airbaseDB.DCSID], aircraftDB.Families.First(), unitCount)
                         select airbaseDB).OrderBy(x => x.Coordinates.GetDistanceFrom(coordinates));

            if (!targetAirbaseOptions.Any()) throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "No airbase found for aircraft.");

            List<DBEntryAirbaseParkingSpot> parkingSpots;
            foreach (var airbase in targetAirbaseOptions)
            {
                try
                {
                    parkingSpots = GetFreeParkingSpots(briefingRoom, ref mission, airbase.DCSID, unitCount, aircraftDB);
                }
                catch (BriefingRoomException)
                {
                    continue;
                }

                return Tuple.Create(airbase, parkingSpots.Select(x => x.DCSID).ToList(), parkingSpots.Select(x => x.Coordinates).ToList());
            }
            throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "No airbase found with sufficient parking spots.");
        }

        internal static void RecoverSpawnPoint(ref DCSMission mission, Coordinates coords)
        {
            var usedSP = mission.UsedSpawnPoints.Find(x => x.Coordinates.X == coords.X && x.Coordinates.Y == x.Coordinates.Y);
            if (usedSP.Coordinates.ToString() == Coordinates.Zero.ToString())
                return;
            mission.SpawnPoints.Add(usedSP);
        }

        internal static DBEntryTheaterTemplateLocation? GetRandomTemplateLocation(
           DCSMission mission,
           TheaterTemplateLocationType locationType,
           Coordinates distanceOrigin1, MinMaxD distanceFrom1,
           Coordinates? distanceOrigin2 = null, MinMaxD? distanceFrom2 = null,
           Coalition? coalition = null,
           bool nested = false
       )
        {
            var validTL = from DBEntryTheaterTemplateLocation pt in mission.TemplateLocations where pt.LocationType == locationType select pt;
            Coordinates?[] distanceOrigin = [distanceOrigin1, distanceOrigin2];
            MinMaxD?[] distanceFrom = [distanceFrom1, distanceFrom2];
            for (int i = 0; i < 2; i++)
            {
                if (!validTL.Any()) break;
                if (!distanceFrom[i].HasValue || !distanceOrigin[i].HasValue) continue;

                var borderLimit = (double)mission.TemplateRecord.BorderLimit;
                Coordinates origin = distanceOrigin[i].Value;
                var searchRange = distanceFrom[i].Value * Toolbox.NM_TO_METERS; // convert distance to meters

                IEnumerable<DBEntryTheaterTemplateLocation> validTLInRange;

                int iterationsLeft = MAX_RADIUS_SEARCH_ITERATIONS;

                var validTLArray = validTL.ToArray();
                var index = new KDBush<DBEntryTheaterTemplateLocation>(validTLArray, p => p.Coordinates.X, p => p.Coordinates.Y);
                do
                {
                    var within = index.Within(origin.X, origin.Y, searchRange.Max).Select(x => validTLArray[x]);
                    validTLInRange = (from DBEntryTheaterTemplateLocation s in within
                                      where
                                        searchRange.Contains(origin.GetDistanceFrom(s.Coordinates)) &&
                                        CheckNotInHostileCoords(ref mission, s.Coordinates, coalition)
                                      select s);
                    searchRange = new MinMaxD(searchRange.Min * 0.95, searchRange.Max * 1.05);
                    if (iterationsLeft < MAX_RADIUS_SEARCH_ITERATIONS * 0.3)
                        borderLimit *= 1.05;
                    iterationsLeft--;
                } while ((!validTLInRange.Any()) && (iterationsLeft > 0));
                validTL = validTLInRange;
            }

            if (!validTL.Any())
                return !coalition.HasValue && nested ? null : GetRandomTemplateLocation(mission, locationType, distanceOrigin1, distanceFrom1, distanceOrigin2, distanceFrom2, null, true);
            var selectedTemplateLocation = Toolbox.RandomFrom(validTL.ToArray());
            mission.TemplateLocations.Remove(selectedTemplateLocation);
            mission.UsedTemplateLocations.Add(selectedTemplateLocation);
            return selectedTemplateLocation;
        }

        internal static DBEntryTheaterTemplateLocation? GetNearestTemplateLocation(
           ref DCSMission mission,
           TheaterTemplateLocationType locationType,
           Coordinates origin, bool remove = true)
        {
            var options = mission.TemplateLocations.Where(x => x.LocationType == locationType).ToList();
            if (!options.Any())
                return null;
            var tl = options.Aggregate((acc, x) => origin.GetDistanceFrom(x.Coordinates) < origin.GetDistanceFrom(acc.Coordinates) ? x : acc);
            if (origin.GetDistanceFrom(tl.Coordinates) > (mission.TemplateRecord.FlightPlanObjectiveSeparation.Max * Toolbox.NM_TO_METERS))
                return null;
            if (remove)
            {
                mission.TemplateLocations.Remove(tl);
                mission.UsedTemplateLocations.Add(tl);
            }
            return tl;
        }

        internal static void RecoverTemplateLocation(ref DCSMission mission, Coordinates coords)
        {
            var usedTL = mission.UsedTemplateLocations.Find(x => x.Coordinates.X == coords.X && x.Coordinates.Y == x.Coordinates.Y);
            if (usedTL.Coordinates.ToString() == Coordinates.Zero.ToString())
                return;
            mission.UsedTemplateLocations.Add(usedTL);
        }

        internal static double GetDirToFrontLine(ref DCSMission mission, Coordinates coords)
        {
            if (mission.FrontLine.Count == 0)
                return Toolbox.RandomAngle();
            var nearestFrontLinePoint = ShapeManager.GetNearestPointBorder(coords, mission.FrontLine);
            return nearestFrontLinePoint.Item2.GetHeadingFrom(coords);
        }

        private static List<DBEntryAirbaseParkingSpot> FilterAndSortSuitableSpots(IBriefingRoom briefingRoom, DBEntryAirbaseParkingSpot[] parkingspots, DBEntryAircraft aircraftDB, bool requiresOpenAirParking, DBEntryAirbaseParkingSpot? lastParkingSpot)
        {
            if (parkingspots.Any(x => x.Height == 0))
            {
                briefingRoom.PrintTranslatableWarning("UsingSimplifedParking");
                return FilterAndSortSuitableSpotsSimple(parkingspots, aircraftDB.Families.First(), requiresOpenAirParking);
            }
            var category = aircraftDB.Families.First().GetUnitCategory();
            var opts = parkingspots.Where(x =>
                aircraftDB.Height < x.Height
                && aircraftDB.Length < x.Length
                && aircraftDB.Width < x.Width
                && (!requiresOpenAirParking || x.ParkingType != ParkingSpotType.HardenedAirShelter)
                && (
                    (category == UnitCategory.Helicopter) ? (x.ParkingType != ParkingSpotType.AirplaneOnly || x.ParkingType != ParkingSpotType.HardenedAirShelter || x.ParkingType != ParkingSpotType.SmallAirplane) : (x.ParkingType != ParkingSpotType.HelicopterOnly)
                    )
             )
             .OrderBy(x => x.ParkingType)
             .ThenBy(x => x.Length * x.Width * x.Height);

            if (lastParkingSpot.HasValue)
                opts = opts.ThenBy(x => x.Coordinates.GetDistanceFrom(lastParkingSpot.Value.Coordinates));
            return opts.ToList();
        }

        private static List<DBEntryAirbaseParkingSpot> FilterAndSortSuitableSpotsSimple(DBEntryAirbaseParkingSpot[] parkingspots, UnitFamily unitFamily, bool requiresOpenAirParking)
        {
            var validTypes = new List<ParkingSpotType>{
                ParkingSpotType.OpenAirSpawn,
                ParkingSpotType.HardenedAirShelter,
                ParkingSpotType.AirplaneOnly,
                ParkingSpotType.SmallAirplane
            };

            if (unitFamily.GetUnitCategory() == UnitCategory.Helicopter)
                validTypes = new List<ParkingSpotType>{
                    ParkingSpotType.OpenAirSpawn,
                    ParkingSpotType.HelicopterOnly,
                };
            else if (IsBunkerUnsuitable(unitFamily) || requiresOpenAirParking)
                validTypes = new List<ParkingSpotType>{
                    ParkingSpotType.OpenAirSpawn
                };

            return parkingspots.Where(x => validTypes.Contains(x.ParkingType)).OrderBy(x => x.ParkingType).ToList();
        }

        private static bool IsBunkerUnsuitable(UnitFamily unitFamily) =>
           Constants.LARGE_AIRCRAFT.Contains(unitFamily) || unitFamily.GetUnitCategory() == UnitCategory.Helicopter;

        private static bool ValidateAirfieldParking(List<DBEntryAirbaseParkingSpot> parkingSpots, UnitFamily unitFamily, int unitCount)
        {
            var openSpots = parkingSpots.Count(X => X.ParkingType == ParkingSpotType.OpenAirSpawn);
            if (openSpots >= unitCount) //Is there just enough open spaces
                return true;

            // Helicopters
            if (unitFamily.GetUnitCategory() == UnitCategory.Helicopter)
                return parkingSpots.Count(X => X.ParkingType == ParkingSpotType.HelicopterOnly) + openSpots > unitCount;

            // Aircraft that can't use bunkers
            if (IsBunkerUnsuitable(unitFamily))
                return parkingSpots.Count(X => X.ParkingType == ParkingSpotType.AirplaneOnly || X.ParkingType == ParkingSpotType.SmallAirplane) + openSpots > unitCount;

            // Bunkerable aircraft
            return parkingSpots.Count(X => X.ParkingType == ParkingSpotType.HardenedAirShelter) + openSpots > unitCount;
        }

        private static bool CheckNotInHostileCoords(ref DCSMission mission, Coordinates coordinates, Coalition? coalition = null)
        {
            if (!coalition.HasValue)
                return true;

            var red = mission.SituationDB.GetRedZones(mission.InvertedCoalition);
            var blue = mission.SituationDB.GetBlueZones(mission.InvertedCoalition);

            return !ShapeManager.IsPosValid(coordinates, (coalition.Value == Coalition.Blue ? red : blue));
        }

        internal static bool CheckNotInNoSpawnCoords(DBEntrySituation situationDB, Coordinates coordinates)
        {
            if (situationDB.NoSpawnZones.Count == 0)
                return true;
            return !ShapeManager.IsPosValid(coordinates, situationDB.NoSpawnZones);
        }

        private static bool CheckNotFarFromBorders(ref DCSMission mission, Coordinates coordinates, double borderLimit, Coalition? coalition = null)
        {
            if (!coalition.HasValue)
                return true;

            var red = mission.SituationDB.GetRedZones(mission.InvertedCoalition);
            var blue = mission.SituationDB.GetBlueZones(mission.InvertedCoalition);

            var distanceLimit = Toolbox.NM_TO_METERS * borderLimit;
            var selectedZones = coalition.Value == Coalition.Blue ? blue : red;
            var distance = selectedZones.Min(x => ShapeManager.GetDistanceFromShape(coordinates, x));
            return distance < distanceLimit;

        }

        private static bool CheckNotFarFromFrontLine(IDatabase database, ref DCSMission mission, Coordinates coordinates, UnitFamily unitFamily, Coalition? coalition = null)
        {
            if (!coalition.HasValue)
                return true;
            var distance = ShapeManager.GetDistanceFromShape(coordinates, mission.FrontLine);
            var side = ShapeManager.GetSideOfLine(coordinates, mission.FrontLine);

            var onPlayerCoalition = coalition == mission.TemplateRecord.ContextPlayerCoalition;
            var onFriendlySideOfLine = (onPlayerCoalition && side == mission.PlayerSideOfFrontLine) || (!onPlayerCoalition && side != mission.PlayerSideOfFrontLine);

            var frontLineDB = database.Common.FrontLine;

            var onFriendlySideOfLineIndex = onFriendlySideOfLine ? 0 : 1;
            var distanceLimit = frontLineDB.DefaultUnitLimits[onFriendlySideOfLineIndex];
            if (frontLineDB.UnitLimits.ContainsKey(unitFamily))
                distanceLimit = frontLineDB.UnitLimits[unitFamily][onFriendlySideOfLineIndex];

            return distanceLimit.Contains(distance * Toolbox.METERS_TO_NM);

        }

        internal static bool CheckInSea(DBEntryTheater theaterDB, Coordinates coordinates)
        {
            return theaterDB.WaterCoordinates.Any(x => ShapeManager.IsPosValid(coordinates, x, theaterDB.WaterExclusionCoordinates));
        }

    }
}
