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
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class Airbases
    {
        internal static void SelectStartingAirbaseForPackages(IDatabase database, ref DCSMission mission)
        {
            var missionPackages = new List<DCSMissionStrikePackage>();
            foreach (var package in mission.TemplateRecord.AircraftPackages)
            {
                if (package.StartingAirbase == "home")
                {
                    missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), mission.PlayerAirbase));
                    continue;
                }
                var flights = mission.TemplateRecord.PlayerFlightGroups.Where((v, i) => package.FlightGroupIndexes.Contains(i));
                var requiredSpots = flights.Sum(x => x.Count);
                var requiredRunway = flights.Select(x => ((DBEntryAircraft)database.GetEntry<DBEntryJSONUnit>(x.Aircraft)).MinimumRunwayLengthFt).Max();
                var airbase = SelectStartingAirbase(database, package.StartingAirbase, ref mission, requiredSpots, requiredRunway);

                if (!missionPackages.Any(x => x.Airbase.ID == airbase.ID))
                    mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{airbase.UIDisplayName.Get(mission.LangKey)}\t{airbase.Runways}\t{airbase.ATC}\t{airbase.ILS}\t{airbase.TACAN}");
                mission.MapData.AddIfKeyUnused($"AIRBASE_NAME_{airbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { airbase.Coordinates.ToArray() });
                missionPackages.Add(new DCSMissionStrikePackage(mission.TemplateRecord.AircraftPackages.IndexOf(package), airbase));
                mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Add(airbase.DCSID);
            }
            mission.StrikePackages.AddRange(missionPackages);
        }

        internal static DBEntryAirbase SelectStartingAirbase(IDatabase database, string selectedAirbaseID, ref DCSMission mission, int requiredParkingSpots = 0, int requiredRunway = 0)
        {
            // Get total number of required parking spots for flight groups
            if (requiredParkingSpots == 0)
                requiredParkingSpots = mission.TemplateRecord.PlayerFlightGroups.Sum(x => x.Count);
            // Select all airbases for this theater
            var airbases = mission.AirbaseDB;
            // If a particular airbase name has been specified and an airbase with this name exists, pick it
            if (!string.IsNullOrEmpty(selectedAirbaseID))
            {
                var airbase = mission.AirbaseDB.FirstOrDefault(x => x.ID == selectedAirbaseID) ?? throw new BriefingRoomException(database, mission.LangKey, "AirbaseNotFoundForPlayer", selectedAirbaseID);
                return airbase;
            }

            var templateRecord = mission.TemplateRecord;
            var theaterDB =  mission.TheaterDB;

            var opts = airbases.Where(x =>
                    x.ParkingSpots.Length >= requiredParkingSpots &&
                    (x.Coalition == templateRecord.ContextPlayerCoalition || templateRecord.SpawnAnywhere) &&
                    x.RunwayLengthFt > requiredRunway &&
                    (!MissionPrefersShoreAirbase(database, templateRecord) || IsNearWater(x.Coordinates, theaterDB))
                    ).ToList();

            if (opts.Count == 0)
                if (!mission.TemplateRecord.PlayerFlightGroups.Any(x => string.IsNullOrEmpty(x.Carrier)))
                    return new DBEntryAirbase(Coordinates.GetCenter(mission.SituationDB.GetBlueZones(mission.TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions")).First().ToArray()));
                else
                    throw new BriefingRoomException(database, mission.LangKey, "NoPlayerAirbaseSpawnPoint");
            return Toolbox.RandomFrom(opts);
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
                var coalition = airbase.DCSID == mission.PlayerAirbase.DCSID || mission.StrikePackages.Any(x => x.Airbase.DCSID == airbase.DCSID) ? mission.TemplateRecord.ContextPlayerCoalition : airbase.Coalition;
                airbase.Coalition = coalition;
                mission.SetAirbase(airbase.DCSID, coalition);
            }
        }

        private static bool IsNearWater(Coordinates coords, DBEntryTheater theaterDB)
        {
            return theaterDB.WaterCoordinates.Any(x => ShapeManager.GetDistanceFromShape(coords, x) * Toolbox.METERS_TO_NM < 50);
        }
    }
}
