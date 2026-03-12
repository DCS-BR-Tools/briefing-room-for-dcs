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

using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class FrontLine
    {
        internal static void GenerateFrontLine(IDatabase database, ref DCSMission mission)
        {
            if (mission.TemplateRecord.ContextCustomFrontLine.Count > 0)
            {
                mission.MapData.Add("FRONTLINE", mission.TemplateRecord.ContextCustomFrontLine.Select(x => x.ToArray()).ToList());
                mission.SetFrontLine(mission.TemplateRecord.ContextCustomFrontLine.Select(x => new Coordinates(x[0], x[1])).ToList(), mission.PlayerAirbase.Coordinates, mission.TemplateRecord.ContextPlayerCoalition);
                return;
            }
            if (mission.FrontLine.Count == 0 && mission.SituationDB.Frontline.Count > 0 && !mission.TemplateRecord.ContextSituationIgnoresFrontLine)
            {
                mission.MapData.Add("FRONTLINE", mission.SituationDB.Frontline.Select(x => x.ToArray()).ToList());
                mission.SetFrontLine(mission.SituationDB.Frontline, mission.PlayerAirbase.Coordinates, mission.TemplateRecord.ContextPlayerCoalition);
                return;
            }
            if (mission.FrontLine.Count > 0 || mission.TemplateRecord.OptionsMission.Contains("SpawnAnywhere") || mission.TemplateRecord.ContextSituation == "None" || mission.TemplateRecord.OptionsMission.Contains("NoFrontLine"))
                return;

            var frontLineList = CreateFrontLine(database, ref mission);
            mission.MapData.Add("FRONTLINE", frontLineList.Select(x => x.ToArray()).ToList());
            mission.SetFrontLine(frontLineList, mission.PlayerAirbase.Coordinates, mission.TemplateRecord.ContextPlayerCoalition);
        }


        private static List<Coordinates> CreateFrontLine(IDatabase database, ref DCSMission mission)
        {
            var frontLineDB = database.Common.FrontLine;
            var bias = GetObjectiveBias(database, mission.LangKey, mission.TemplateRecord, frontLineDB);

            Coordinates? centerPoint = null;
            var objHints = mission.TemplateRecord.Objectives.Where(x => x.CoordinatesHint.ToString() != "0,0").Select(x => x.CoordinatesHint).ToList();
            if (objHints.Count > 0)
            {
                centerPoint = Coordinates.Sum(objHints) / objHints.Count;
            }
            else
            {
                centerPoint = GetRandomCenterCoordinates(
                    database,
                    ref mission,
                    GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, bias > 0 ? Side.Ally : Side.Enemy));
            }

            var frontLineCenter = Coordinates.Lerp(mission.PlayerAirbase.Coordinates, centerPoint.Value, GetObjectiveLerpBias(bias, frontLineDB));
            var objectiveHeading = mission.PlayerAirbase.Coordinates.GetHeadingFrom(centerPoint.Value);
            var angleVariance = frontLineDB.AngleVarianceRange + objectiveHeading;
            var frontLineList = new List<Coordinates> { frontLineCenter };

            var blueZones = mission.SituationDB.GetBlueZones(false);
            var redZones = mission.SituationDB.GetRedZones(false);
            var biasZones = ShapeManager.IsPosValid(frontLineCenter, blueZones) ? blueZones : (ShapeManager.IsPosValid(frontLineCenter, redZones) ? redZones : Toolbox.RandomFrom(blueZones, redZones));

            for (int i = 0; i < frontLineDB.SegmentsPerSide; i++)
            {
                frontLineList.Insert(0, CreatePoint(frontLineDB, frontLineList, angleVariance, biasZones, true));
                frontLineList.Add(CreatePoint(frontLineDB, frontLineList, angleVariance, biasZones, false));
            }
            return frontLineList;
        }

        private static Coordinates CreatePoint(DBCommonFrontLine frontLineDB, List<Coordinates> frontLineList, MinMaxD angleVariance, List<List<Coordinates>> biasPoints, bool preCenter)
        {
            var angle = angleVariance.GetValue();
            if (preCenter)
                angle -= 180;
            var refPoint = preCenter ? frontLineList.First() : frontLineList.Last();
            var point = Coordinates.FromAngleAndDistance(refPoint, frontLineDB.LinePointSeparationRange * Toolbox.NM_TO_METERS, angle);
            if (biasPoints.Count > 0)
            {
                var nearest = biasPoints.Select(x => ShapeManager.GetNearestPointBorder(point, x)).MinBy(x => x.Item1).Item2;
                point = Coordinates.Lerp(point, nearest, frontLineDB.BorderBiasRange.GetValue());
            }
            return point;
        }

        private static double GetObjectiveBias(IDatabase database, string langKey, MissionTemplateRecord template, DBCommonFrontLine frontLineDB)
        {
            var friendlySideObjectivesCount = 0;
            var enemySideObjectivesCount = 0;

            template.Objectives.ForEach(x =>
            {
                if (ObjectiveGenerator.GetObjectiveData(database, langKey, x).taskDB.TargetSide == Side.Ally)
                    friendlySideObjectivesCount++;
                else
                    enemySideObjectivesCount++;
            });
            return friendlySideObjectivesCount - enemySideObjectivesCount;
        }

        private static double GetObjectiveLerpBias(double bias, DBCommonFrontLine frontLineDB)
        {
            var lerpDistance = frontLineDB.BaseObjectiveBiasRange.GetValue();
            if (bias < 1)
                lerpDistance += bias * frontLineDB.EnemyObjectiveBias;
            else
                lerpDistance += bias * frontLineDB.FriendlyObjectiveBias;

            return double.Min(double.Max(lerpDistance, frontLineDB.ObjectiveBiasLimits.Min), frontLineDB.ObjectiveBiasLimits.Max);
        }

        private static Coordinates? GetRandomCenterCoordinates(IDatabase database, ref DCSMission mission, Coalition? coalition)
        {
            Coordinates? spawnPoint = SpawnPointSelector.GetRandomSpawnPoint(
                database,
                ref mission,
                Constants.LAND_SPAWNS.ToArray(),
                mission.PlayerAirbase.Coordinates,
                mission.TemplateRecord.FlightPlanObjectiveDistance,
                coalition: coalition);

            return spawnPoint;
        }

    }
}
