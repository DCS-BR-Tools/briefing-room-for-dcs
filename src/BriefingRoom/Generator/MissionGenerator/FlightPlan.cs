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
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class FlightPlan
    {
        private const double BULLSEYE_DISTANCE_MIN = 20.0;

        private const double BULLSEYE_DISTANCE_MAX = 40.0;


        internal static void GenerateBullseyes(ref DCSMission mission)
        {
            mission.SetValue("BullseyeBlueX", mission.ObjectivesCenter.X + GetBullseyeRandomDistance());
            mission.SetValue("BullseyeBlueY", mission.ObjectivesCenter.Y + GetBullseyeRandomDistance());
            mission.SetValue("BullseyeRedX", mission.ObjectivesCenter.X + GetBullseyeRandomDistance());
            mission.SetValue("BullseyeRedY", mission.ObjectivesCenter.Y + GetBullseyeRandomDistance());
        }

        internal static void GenerateAircraftPackageWaypoints(IDatabase database, ref DCSMission mission)
        {
            foreach (var package in mission.TemplateRecord.AircraftPackages)
            {
                var aircraftPackages = mission.TemplateRecord.AircraftPackages;
                var missionPackage = mission.StrikePackages.First(x => x.RecordIndex == aircraftPackages.IndexOf(package));
                missionPackage.Waypoints = mission.ObjectiveGroupedWaypoints
                    .SelectMany(x => x)
                    .Where((v, i) => package.ObjectiveIndexes.Contains(i))
                    .SelectMany(x => x)
                    .ToList();
                GenerateIngressAndEgressWaypoints(database, ref mission);

                foreach (var waypoint in missionPackage.Waypoints)
                {
                    mission.MapData.AddIfKeyUnused($"WAYPOINT_{waypoint.Name}", new List<double[]> { waypoint.Coordinates.ToArray() });
                }
            }
        }


        internal static void GenerateIngressAndEgressWaypoints(IDatabase database, ref DCSMission mission)
        {
            if (!mission.TemplateRecord.MissionFeatures.Contains("IngressEgressWaypoints"))
                return;

            BriefingRoom.PrintToLog($"Generating ingress and egress waypoints...");

            double flightPathLength = (mission.ObjectivesCenter - mission.AverageInitialPosition).GetLength();
            double ingressDeviation = Math.Max(4.0, flightPathLength * .15);
            Coordinates baseIngressPosition = mission.AverageInitialPosition + (mission.ObjectivesCenter - mission.AverageInitialPosition) * .7f;

            mission.Waypoints.Insert(0,
                new Waypoint(
                    $"{database.Common.Names.WPIngressName.Get(mission.LangKey).ToUpper()}_{mission.WaypointNameGenerator.GetWaypointName()}",
                    baseIngressPosition + Coordinates.CreateRandom(ingressDeviation * 0.9, ingressDeviation * 1.1)));

            mission.Waypoints.Add(
                new Waypoint(
                    $"{database.Common.Names.WPEgressName.Get(mission.LangKey).ToUpper()}_{mission.WaypointNameGenerator.GetWaypointName()}",
                    baseIngressPosition + Coordinates.CreateRandom(ingressDeviation * 0.9, ingressDeviation * 1.1)));
        }

        internal static void GenerateBullseyeWaypoint(IDatabase database, ref DCSMission mission)
        {
            if (!mission.TemplateRecord.MissionFeatures.Contains("BullseyeWaypoint"))
                return;

            BriefingRoom.PrintToLog($"Generating (blue) bullseye waypoint...");

            double bullseyeBlueX = double.Parse(mission.GetValue("BullseyeBlueX"));
            double bullseyeBlueY = double.Parse(mission.GetValue("BullseyeBlueY"));

            Coordinates bullseyeBlueCoordinates = new Coordinates(bullseyeBlueX, bullseyeBlueY);

            mission.Waypoints.Add(
                new Waypoint(
                    $"{database.Common.Names.WPBullseyeName.Get(mission.LangKey).ToUpper()}_{mission.WaypointNameGenerator.GetWaypointName()}",
                    bullseyeBlueCoordinates));
        }

        internal static void GenerateObjectiveWPCoordinatesLua(ref DCSMission mission)
        {
            var scriptWaypoints = mission.Waypoints.Where(x => !x.ScriptIgnore).ToList();
            for (int i = 0; i < scriptWaypoints.Count; i++)
            {
                mission.AppendValue("ScriptObjectives",
                    $"briefingRoom.mission.objectives[{i + 1}].waypoint = {scriptWaypoints[i].Coordinates.ToLuaTable()}\n");
            }
            if (mission.TemplateRecord.OptionsMission.Contains("MarkWaypoints"))
                foreach (var waypoint in mission.Waypoints)
                {
                    if(!waypoint.HiddenMapMarker)
                        DrawingMaker.AddDrawing(ref mission,waypoint.Name, DrawingType.TextBox, waypoint.Coordinates + new Coordinates(-100, 0), "Text".ToKeyValuePair(waypoint.Name));
                }

            foreach (var waypoint in mission.Waypoints)
            {
                mission.MapData.AddIfKeyUnused($"WAYPOINT_{waypoint.Name}", new List<double[]> { waypoint.Coordinates.ToArray() });
            }
        }

        private static double GetBullseyeRandomDistance()
        {
            return Toolbox.RandomDouble(BULLSEYE_DISTANCE_MIN, BULLSEYE_DISTANCE_MAX) * Toolbox.NM_TO_METERS;
        }
    }
}