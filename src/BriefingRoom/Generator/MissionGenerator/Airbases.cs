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

using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class Airbases
    {
        internal static void SelectStartingAirbaseForPackages(IDatabase database, ref DCSMission mission)
        {
            var missionPackages = new List<DCSMissionStrikePackage>();
            foreach (var package in mission.TemplateRecord.AircraftPackages)
            {
                var airbase = mission.PlayerAirbase;
                var packageParkingDemand = GetParkingDemandForFlightGroupIndexes(database, mission.TemplateRecord.PlayerFlightGroups, package.FlightGroupIndexes);
                if (package.StartingAirbase == "home" && package.DestinationAirbase == "home")
                {
                    missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), mission.PlayerAirbase, mission.PlayerAirbaseDestination));
                    continue;
                }
                else if (package.StartingAirbase == "home")
                {
                    airbase = mission.PlayerAirbase;
                }
                else if (package.StartingAirbase == "homeDest")
                {
                    airbase = mission.PlayerAirbaseDestination;
                }
                else if (package.StartingAirbase != "home")
                    airbase = GetStrikeAirbase(database, ref mission, ref missionPackages, package.StartingAirbase, packageParkingDemand);


                if (package.DestinationAirbase == "home")
                {
                    missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), airbase, mission.PlayerAirbase));
                    continue;
                }
                else if (package.DestinationAirbase == "homeDest")
                {
                    missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), airbase, mission.PlayerAirbaseDestination));
                    continue;
                }
                else if (package.DestinationAirbase == "strike")
                {
                    missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), airbase, airbase));
                    continue;
                }

                var destinationAirbase = GetStrikeAirbase(database, ref mission, ref missionPackages, package.DestinationAirbase, packageParkingDemand);
                missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), airbase, destinationAirbase));
            }
            mission.StrikePackages.AddRange(missionPackages);
        }

        private const double RANDOM_DESTINATION_DISTANCE_MULTIPLIER = 1.3;
        private const double RANDOM_DESTINATION_MIN_DISTANCE_NM = 15.0;

        internal static DBEntryAirbase SelectStartingAirbase(IDatabase database, string selectedAirbaseID, ref DCSMission mission, int requiredParkingSpots = 0, IReadOnlyCollection<SpawnPointSelector.ParkingDemand> parkingDemand = null)
        {
            if (!string.IsNullOrEmpty(selectedAirbaseID))
                return mission.AirbaseDB.FirstOrDefault(x => x.ID == selectedAirbaseID) ?? throw new BriefingRoomException(database, mission.LangKey, "AirbaseNotFoundForPlayer", selectedAirbaseID);

            var opts = GetCandidatePlayerAirbases(database, mission, requiredParkingSpots, parkingDemand);
            if (opts.Count == 0)
                return GetCarrierFallbackAirbaseOrThrow(database, mission);

            return Toolbox.RandomFrom(opts);
        }

        internal static DBEntryAirbase SelectDestinationAirbase(IDatabase database, string selectedAirbaseID, ref DCSMission mission)
        {
            if (!string.IsNullOrEmpty(selectedAirbaseID))
                return mission.AirbaseDB.FirstOrDefault(x => x.ID == selectedAirbaseID) ?? throw new BriefingRoomException(database, mission.LangKey, "AirbaseNotFoundForPlayer", selectedAirbaseID);

            var opts = GetCandidatePlayerAirbases(database, mission);
            if (opts.Count == 0)
                return GetCarrierFallbackAirbaseOrThrow(database, mission);

            var playerAirbase = mission.PlayerAirbase;
            double maxDistanceMeters = GetRandomDestinationMaxDistanceNM(database, mission.TemplateRecord) * Toolbox.NM_TO_METERS;
            var origin = playerAirbase.Coordinates;
            var withinRangeAlternates = opts
                .Where(x => x.DCSID != playerAirbase.DCSID && x.Coordinates.GetDistanceFrom(origin) <= maxDistanceMeters)
                .ToList();
            if (withinRangeAlternates.Count > 0)
                return Toolbox.RandomFrom(withinRangeAlternates);

            if (opts.Any(x => x.DCSID == playerAirbase.DCSID))
                return playerAirbase;

            return opts.OrderBy(x => x.Coordinates.GetDistanceFrom(origin)).First();
        }

        private static List<DBEntryAirbase> GetCandidatePlayerAirbases(
            IDatabase database,
            DCSMission mission,
            int requiredParkingSpots = 0,
            IReadOnlyCollection<SpawnPointSelector.ParkingDemand> parkingDemand = null)
        {
            parkingDemand ??= GetParkingDemandForFlightGroups(database, mission.TemplateRecord.PlayerFlightGroups);

            if (requiredParkingSpots == 0)
                requiredParkingSpots = parkingDemand.Sum(x => x.UnitCount);

            var templateRecord = mission.TemplateRecord;
            var theaterDB = mission.TheaterDB;

            return mission.AirbaseDB.Where(x =>
                    x.ParkingSpots.Length >= requiredParkingSpots &&
                    (x.Coalition == templateRecord.ContextPlayerCoalition || templateRecord.SpawnAnywhere) &&
                    (!MissionPrefersShoreAirbase(database, templateRecord) || IsNearWater(x.Coordinates, theaterDB)) &&
                    (parkingDemand.Count == 0 || SpawnPointSelector.CanSatisfyParkingDemand(mission, x.DCSID, parkingDemand)))
                .ToList();
        }

        private static DBEntryAirbase GetCarrierFallbackAirbaseOrThrow(IDatabase database, DCSMission mission)
        {
            if (!mission.TemplateRecord.PlayerFlightGroups.Any(x => string.IsNullOrEmpty(x.Carrier)))
                return new DBEntryAirbase(Coordinates.GetCenter(mission.SituationDB.GetBlueZones(mission.TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions")).First().ToArray()));

            throw new BriefingRoomException(database, mission.LangKey, "NoPlayerAirbaseSpawnPoint");
        }

        private static double GetRandomDestinationMaxDistanceNM(IDatabase database, MissionTemplateRecord templateRecord)
        {
            bool hasRotorcraft = templateRecord.PlayerFlightGroups
                .Where(x => !x.Hostile)
                .Select(x => database.GetEntry<DBEntryJSONUnit>(x.Aircraft))
                .Any(x => x != null && x.Category == UnitCategory.Helicopter);

            double baseMaxNM = hasRotorcraft
                ? Math.Min(templateRecord.FlightPlanObjectiveDistance.Max, templateRecord.FlightPlanObjectiveSeparation.Max)
                : templateRecord.FlightPlanObjectiveDistance.Max;

            return Math.Max(RANDOM_DESTINATION_MIN_DISTANCE_NM, baseMaxNM * RANDOM_DESTINATION_DISTANCE_MULTIPLIER);
        }

        private static bool MissionPrefersShoreAirbase(IDatabase database, MissionTemplateRecord template)
        {
            // If any objective target is a ship, return true
            foreach (var objective in template.Objectives)
                if (database.EntryExists<DBEntryObjectiveTarget>(objective.Target) &&
                    (database.GetEntry<DBEntryObjectiveTarget>(objective.Target).UnitCategory == UnitCategory.Ship))
                    return true;

            // If any flight group takes off from a carrier, return true
            foreach (var flightGroup in template.PlayerFlightGroups)
                if (!string.IsNullOrEmpty(flightGroup.Carrier) && !flightGroup.Carrier.StartsWith("FOB"))
                    return true;

            return false;
        }

        internal static void SetupAirbasesCoalitions(ref DCSMission mission)
        {
            // Select all airbases for this theater
            var situationAirbases = mission.AirbaseDB;

            foreach (DBEntryAirbase airbase in situationAirbases)
            {
                var coalition = airbase.DCSID == mission.PlayerAirbase.DCSID || airbase.DCSID == mission.PlayerAirbaseDestination.DCSID || mission.StrikePackages.Any(x => x.StartAirbase.DCSID == airbase.DCSID) ? mission.TemplateRecord.ContextPlayerCoalition : airbase.Coalition;
                airbase.Coalition = coalition;
                mission.SetAirbase(airbase.DCSID, coalition);
            }
        }

        private static bool IsNearWater(Coordinates coords, DBEntryTheater theaterDB)
        {
            return theaterDB.WaterCoordinates.Any(x => ShapeManager.GetDistanceFromShape(coords, x) * Toolbox.METERS_TO_NM < 50);
        }

        private static List<SpawnPointSelector.ParkingDemand> GetParkingDemandForFlightGroupIndexes(IDatabase database, IReadOnlyCollection<MissionTemplateFlightGroupRecord> flightGroups, IEnumerable<int> flightGroupIndexes)
        {
            var indexSet = flightGroupIndexes.ToHashSet();
            var packageFlights = flightGroups.Where(x => indexSet.Contains(x.Index));
            return GetParkingDemandForFlightGroups(database, packageFlights);
        }

        private static List<SpawnPointSelector.ParkingDemand> GetParkingDemandForFlightGroups(IDatabase database, IEnumerable<MissionTemplateFlightGroupRecord> flightGroups)
        {
            return flightGroups
                .Where(x =>
                    !x.Hostile &&
                    string.IsNullOrEmpty(x.Carrier) &&
                    x.StartLocation != PlayerStartLocation.Air &&
                    x.Count > 0)
                .GroupBy(x => x.Aircraft)
                .Select(x => new
                {
                    Aircraft = database.GetEntry<DBEntryJSONUnit>(x.Key) as DBEntryAircraft,
                    UnitCount = x.Sum(flight => flight.Count)
                })
                .Where(x => x.Aircraft is not null && x.UnitCount > 0)
                .Select(x => new SpawnPointSelector.ParkingDemand(x.Aircraft!, x.UnitCount))
                .ToList();
        }

        private static DBEntryAirbase GetStrikeAirbase(IDatabase database, ref DCSMission mission, ref List<DCSMissionStrikePackage> missionPackages, string airbaseId, IReadOnlyCollection<SpawnPointSelector.ParkingDemand> parkingDemand)
        {
            var airbase = SelectStartingAirbase(database, airbaseId, ref mission, parkingDemand: parkingDemand);
            if (!missionPackages.Any(x => x.StartAirbase.ID == airbase.ID))
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{airbase.UIDisplayName.Get(mission.LangKey)}{(string.IsNullOrEmpty(airbase.ICAO) ? "" : $"<br />{airbase.ICAO}")}\t{airbase.Runways}\t{airbase.ATC}\t{airbase.ILS}\t{airbase.TACAN}");
            mission.MapData.AddIfKeyUnused($"AIRBASE_NAME_{airbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { airbase.Coordinates.ToArray() });
            mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Add(airbase.DCSID);
            return airbase;
        }
    }
}
