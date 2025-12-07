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
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class PlayerFlightGroups
    {

        internal static void GeneratePlayerFlightGroup(
            IBriefingRoom briefingRoom,
            ref DCSMission mission,
            MissionTemplateFlightGroupRecord flightGroup
            )
        {
            var airbase = mission.PlayerAirbase;
            var flightWaypoints = new List<Waypoint>(mission.Waypoints);
            var groupStartingCoords = mission.PlayerAirbase.Coordinates;

            var package = mission.TemplateRecord.AircraftPackages.FirstOrDefault(x => x.FlightGroupIndexes.Contains(flightGroup.Index));
            if (package is not null)
            {
                var aircraftPackages = mission.TemplateRecord.AircraftPackages;
                var missionPackage = mission.StrikePackages.First(x => x.RecordIndex == aircraftPackages.IndexOf(package));
                flightWaypoints = missionPackage.Waypoints;
                airbase = missionPackage.Airbase;
                groupStartingCoords = missionPackage.Airbase.Coordinates;
            }
            var unitDB = (DBEntryAircraft)briefingRoom.Database.GetEntry<DBEntryJSONUnit>(flightGroup.Aircraft);

            // Not an unit, or not a player-controllable unit, abort.
            if ((unitDB == null) || !unitDB.PlayerControllable)
                throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "PlayerFlightNotFound", flightGroup.Aircraft);
            if (unitDB.MinimumRunwayLengthFt > 0 && airbase.RunwayLengthFt < unitDB.MinimumRunwayLengthFt)
                briefingRoom.PrintTranslatableWarning("RunwayTooShort", airbase.UIDisplayName.Get(mission.LangKey), airbase.RunwayLengthFt, unitDB.UIDisplayName, unitDB.MinimumRunwayLengthFt);

            List<int> parkingSpotIDsList = new();
            List<Coordinates> parkingSpotCoordinatesList = new();
            var groupLuaFile = "AircraftPlayer";
            var carrierUnitID = 0;
            string carrierName = null;
            var side = flightGroup.Hostile ? Side.Enemy : Side.Ally;
            var country = flightGroup.Country;
            var payload = flightGroup.Payload;
            var extraSettings = new Dictionary<string, object>();
            GroupFlags GroupFlags = flightGroup.AIWingmen ? GroupFlags.FirstUnitIsClient : 0;
            DCSSkillLevel skillLevel = flightGroup.AIWingmen ? Toolbox.RandomFrom(DCSSkillLevel.High, DCSSkillLevel.Excellent) : DCSSkillLevel.Client;
            var atcRadioFrequency = 0d;
            if (airbase.ATC != null)
                _ = double.TryParse(airbase.ATC.Split("/")[0], out atcRadioFrequency);

            if (!string.IsNullOrEmpty(flightGroup.Carrier) && mission.CarrierDictionary.ContainsKey(flightGroup.Carrier) && !flightGroup.Hostile) // Carrier take off
            {
                var carrier = mission.CarrierDictionary[flightGroup.Carrier];
                if (
                    !carrier.GroupInfo.UnitDB.Families.Intersect([UnitFamily.ShipCarrierCATOBAR, UnitFamily.ShipCarrierSTOBAR]).Any() &&
                    carrier.GroupInfo.UnitDB.Families.Contains(UnitFamily.ShipCarrierSTOVL) &&
                    flightGroup.Carrier != "LHA_Tarawa")
                {
                    extraSettings.AddIfKeyUnused("Speed", 0);
                    GroupFlags = 0;
                    skillLevel = DCSSkillLevel.Client;
                    if (flightGroup.Aircraft == "AV8BNA")
                        payload = "EMPTY";
                }
                groupLuaFile = "AircraftPlayerCarrier";
                carrierUnitID = carrier.GroupInfo.DCSGroup.Units[0].UnitId;
                carrierName = carrier.GroupInfo.UnitDB.UIDisplayName.Get(mission.LangKey);
                if (flightGroup.StartLocation != PlayerStartLocation.Air)
                {
                    bool isPlane = unitDB.Category == UnitCategory.Plane;
                    var spotOffset = (isPlane ? carrier.TotalPlaneSpotCount : carrier.TotalHelicopterSpotCount) - (isPlane ? carrier.RemainingPlaneSpotCount : carrier.RemainingHelicopterSpotCount);
                    for (int i = spotOffset; i < flightGroup.Count + spotOffset; i++)
                    {
                        parkingSpotIDsList.Add(i + 1);
                        parkingSpotCoordinatesList.Add(carrier.GroupInfo.Coordinates);
                    }
                    if (isPlane)
                        carrier.RemainingPlaneSpotCount -= flightGroup.Count;
                    else
                        carrier.RemainingHelicopterSpotCount -= flightGroup.Count;
                }
                groupStartingCoords = carrier.GroupInfo.Coordinates;
                atcRadioFrequency = carrier.GroupInfo.Frequency / 1000000.0;
            }
            else if (flightGroup.Hostile)
            {
                var coalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, side, true);
                var (hostileAirbase, hostileParkingSpotIDsList, hostileParkingSpotCoordinatesList) = SpawnPointSelector.GetAirbaseAndParking(briefingRoom, mission, mission.ObjectivesCenter, flightGroup.Count, coalition.Value, unitDB);
                parkingSpotIDsList = hostileParkingSpotIDsList;
                parkingSpotCoordinatesList = hostileParkingSpotCoordinatesList;
                groupStartingCoords = hostileParkingSpotCoordinatesList.First();
                airbase = hostileAirbase;
                if (airbase.ATC != null)
                    _ = double.TryParse(airbase.ATC.Split("/")[0], out atcRadioFrequency);

                if (country == Country.CombinedJointTaskForcesBlue || country == Country.CombinedJointTaskForcesRed)
                    country = coalition == Coalition.Blue ? Country.CombinedJointTaskForcesBlue : Country.CombinedJointTaskForcesRed;
                mission.MapData.AddIfKeyUnused($"AIRBASE_Enemy_NAME_{hostileAirbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { hostileAirbase.Coordinates.ToArray() });
            }
            else // Land airbase take off
            {
                var parkingSpots = SpawnPointSelector.GetFreeParkingSpots(briefingRoom, ref mission, airbase.DCSID, flightGroup.Count, unitDB);
                parkingSpotIDsList = parkingSpots.Select(x => x.DCSID).ToList();
                parkingSpotCoordinatesList = parkingSpots.Select(x => x.Coordinates).ToList();
                groupStartingCoords = parkingSpotCoordinatesList.First();
            }



            if (!string.IsNullOrEmpty(flightGroup.OverrideRadioFrequency))
            {
                extraSettings.AddIfKeyUnused("RadioBand", (int)flightGroup.OverrideRadioBand);
                extraSettings.AddIfKeyUnused("RadioFrequency", GeneratorTools.GetRadioFrequency(double.Parse(flightGroup.OverrideRadioFrequency, CultureInfo.InvariantCulture)));
                extraSettings.AddIfKeyUnused("RadioFrequencyDouble", double.Parse(flightGroup.OverrideRadioFrequency, CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(flightGroup.OverrideCallsignName))
            {
                extraSettings.AddIfKeyUnused("OverrideCallsignName", flightGroup.OverrideCallsignName);
                extraSettings.AddIfKeyUnused("OverrideCallsignNumber", flightGroup.OverrideCallsignNumber);
            }

            extraSettings.AddIfKeyUnused("Payload", payload);
            extraSettings.AddIfKeyUnused("Skill", skillLevel.ToString());
            extraSettings.AddIfKeyUnused("PlayerStartingAction", GeneratorTools.GetPlayerStartingAction(flightGroup.StartLocation));
            extraSettings.AddIfKeyUnused("PlayerStartingType", GeneratorTools.GetPlayerStartingType(flightGroup.StartLocation));
            extraSettings.AddIfKeyUnused("Country", country);
            extraSettings.AddIfKeyUnused("InitialWPName", briefingRoom.Database.Common.Names.WPInitialName.Get(mission.LangKey));
            extraSettings.AddIfKeyUnused("FinalWPName", briefingRoom.Database.Common.Names.WPFinalName.Get(mission.LangKey));
            extraSettings.AddIfKeyUnused("LinkUnit", carrierUnitID);
            extraSettings.AddIfKeyUnused("MissionAirbaseX", groupStartingCoords.X);
            extraSettings.AddIfKeyUnused("MissionAirbaseY", groupStartingCoords.Y);
            extraSettings.AddIfKeyUnused("MissionAirbaseID", airbase.DCSID);
            extraSettings.AddIfKeyUnused("Livery", flightGroup.Livery);
            if (atcRadioFrequency > 0)
                extraSettings.AddIfKeyUnused("AirbaseRadioFrequency", atcRadioFrequency);
            extraSettings.AddIfKeyUnused("AirbaseRadioModulation", 0);

            if (flightGroup.StartLocation == PlayerStartLocation.Air)
            {
                groupLuaFile = "AircraftPlayerAir";
                groupStartingCoords = groupStartingCoords.CreateNearRandom(50, 200);
            }
            else
            {
                extraSettings.AddIfKeyUnused("ParkingID", parkingSpotIDsList);
                extraSettings.AddIfKeyUnused("UnitCoords", parkingSpotCoordinatesList);
            }

            var task = GetTaskType(mission.TemplateRecord.Objectives, extraSettings, package);

            GroupInfo? groupInfo = UnitGenerator.AddUnitGroup(
                briefingRoom,
                ref mission,
                Enumerable.Repeat(flightGroup.Aircraft, flightGroup.Count).ToList(), side, unitDB.Families[0],
                groupLuaFile, "Aircraft", groupStartingCoords,
                GroupFlags,
                extraSettings
                );

            if (!groupInfo.HasValue)
            {
                briefingRoom.PrintTranslatableWarning("FailedToSpawnPlayerFlightGroup");
                return;
            }

            groupInfo.Value.DCSGroup.Waypoints.InsertRange(1, flightWaypoints.Where(x => !x.AircraftIgnore).Select(x => x.ToDCSWaypoint(unitDB, task)).ToList());


            SaveFlightGroup(ref mission, groupInfo, flightGroup, unitDB, carrierName ?? airbase.Name, task);
            SaveWaypointsToBriefing(
                briefingRoom.Database,
                ref mission,
                groupStartingCoords,
                flightWaypoints,
                mission.TemplateRecord.OptionsMission.Contains("ImperialUnitsForBriefing"),
                groupInfo,
                mission.TheaterDB);

            var mapWaypoints = flightWaypoints.Select(x => x.Coordinates.ToArray()).ToList();
            mapWaypoints = mapWaypoints.Prepend(groupStartingCoords.ToArray()).ToList();
            mapWaypoints.Add(groupStartingCoords.ToArray());
            mission.MapData.Add($"UNIT_{side}_PLAYER_{groupInfo.Value.DCSGroup.GroupId}", new List<double[]> { mapWaypoints.First() });
            mission.MapData.Add($"ROUTE_{groupInfo.Value.DCSGroup.GroupId}", mapWaypoints);
        }

        private static void SaveFlightGroup(ref DCSMission mission, GroupInfo? groupInfo, MissionTemplateFlightGroupRecord flightGroup, DBEntryJSONUnit unitDB, string homeBase, DCSTask task)
        {
            mission.Briefing.AddItem(DCSMissionBriefingItemType.FlightGroup,
                $"{groupInfo.Value.Name}(P)\t" +
                $"{flightGroup.Count}× {unitDB.UIDisplayName.Get(mission.LangKey)}\t" +
                $"{GeneratorTools.FormatRadioFrequency(groupInfo.Value.Frequency)}\t" +
                $"{(flightGroup.Payload == "default" ? task : flightGroup.Payload)}\t" +
                $"{homeBase}");
            for (int i = 0; i < flightGroup.Count; i++)
                mission.AppendValue("SCRIPTCLIENTPILOTNAMES", $"\"{groupInfo.Value.Name} {i + 1}\",");
        }

        private static void SaveWaypointsToBriefing(IDatabase database, ref DCSMission mission, Coordinates initialCoordinates, List<Waypoint> waypoints, bool useImperialSystem, GroupInfo? groupInfo, DBEntryTheater theaterDB)
        {
            double totalDistance = 0;
            Coordinates lastWP = initialCoordinates;

            // Add first (takeoff) and last (landing) waypoints to get a complete list of all waypoints
            List<Waypoint> allWaypoints =
            [
                new Waypoint(database.Common.Names.WPInitialName.Get(mission.LangKey).ToUpper(), initialCoordinates),
                .. waypoints,
                new Waypoint(database.Common.Names.WPFinalName.Get(mission.LangKey).ToUpper(), initialCoordinates),
            ];
            mission.Briefing.AddItem(DCSMissionBriefingItemType.Waypoint, $"\t{groupInfo.Value.Name}\t");

            List<string> waypointTextRows = new();
            foreach (Waypoint waypoint in allWaypoints)
            {
                double distanceFromLast = waypoint.Coordinates.GetDistanceFrom(lastWP);
                double heading = waypoint.Coordinates.GetHeadingFrom(lastWP);
                double magHeading = theaterDB.GetMagneticHeading(heading);
                totalDistance += distanceFromLast;
                lastWP = waypoint.Coordinates;

                string waypointText =
                    allWaypoints.IndexOf(waypoint) + ": " + waypoint.Name + "\t" +
                    (useImperialSystem ? $"{distanceFromLast * Toolbox.METERS_TO_NM:F0} nm" : $"{distanceFromLast / 1000.0:F0} Km") + "\t" +
                    (useImperialSystem ? $"{totalDistance * Toolbox.METERS_TO_NM:F0} nm" : $"{totalDistance / 1000.0:F0} Km") + '\t' +
                    $"{heading} ({magHeading})";

                mission.Briefing.AddItem(DCSMissionBriefingItemType.Waypoint, waypointText);
                waypointTextRows.Add(waypointText);
            }
            mission.Briefing.FlightBriefings.Add(new DCSMissionFlightBriefing
            {
                Name = groupInfo.Value.Name,
                Type = groupInfo.Value.UnitDB.DCSID,
                Waypoints = waypointTextRows
            });
        }

        private static DCSTask GetTaskType(List<MissionTemplateObjectiveRecord> objectives, Dictionary<string, object> extraSettings, MissionTemplatePackageRecord package)
        {
            var objs = objectives;
            if (package != null)
                objs = objs.Where((x, i) => package.ObjectiveIndexes.Contains(i)).ToList();
            var task = objectives.Select(x => new List<DCSTask> { AssignTask(x) }.Concat(x.SubTasks.Select(y => AssignTask(y)).ToList())).SelectMany(x => x).ToList().GroupBy(x => x).MaxBy(g => g.Count()).ToList().First();
            extraSettings.AddIfKeyUnused("DCSTask", task);
            return task;
        }

        private static DCSTask AssignTask(MissionTemplateSubTaskRecord objective)
        {
            if (objective.Task.StartsWith("Transport") || objective.Task.StartsWith("LandNear") || objective.Task.StartsWith("Extract"))
                return DCSTask.Transport;
            if (objective.Task.StartsWith("FlyNear"))
                return DCSTask.Reconnaissance;
            if (objective.Target.StartsWith("Ship"))
                return DCSTask.AntishipStrike;
            if (objective.Target.StartsWith("Helicopter") || objective.Target.StartsWith("Plane"))
                return DCSTask.CAP;
            if (objective.Target.StartsWith("Air"))
                return DCSTask.SEAD;
            return DCSTask.CAS;
        }
    }
}
