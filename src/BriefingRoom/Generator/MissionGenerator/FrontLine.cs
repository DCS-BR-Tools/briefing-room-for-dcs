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
    internal class FrontLine
    {
        internal static void GenerateFrontLine(IDatabase database, ref DCSMission mission)
        {

            if (mission.FrontLine.Count > 0 || mission.TemplateRecord.OptionsMission.Contains("SpawnAnywhere") || mission.TemplateRecord.ContextSituation == "None" || mission.TemplateRecord.OptionsMission.Contains("NoFrontLine"))
                return;
            var frontLineDB = database.Common.FrontLine;
            var frontLineCenter = Coordinates.Lerp(mission.PlayerAirbase.Coordinates, mission.ObjectivesCenter, GetObjectiveLerpBias(database, mission.LangKey, mission.TemplateRecord, frontLineDB));

            var objectiveHeading = mission.PlayerAirbase.Coordinates.GetHeadingFrom(mission.ObjectivesCenter);
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

            mission.MapData.Add("FRONTLINE", frontLineList.Select(x => x.ToArray()).ToList());
            mission.SetFrontLine(frontLineList, mission.PlayerAirbase.Coordinates, mission.TemplateRecord.ContextPlayerCoalition);
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

        private static double GetObjectiveLerpBias(IDatabase database, string langKey, MissionTemplateRecord template, DBCommonFrontLine frontLineDB)
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

            var lerpDistance = frontLineDB.BaseObjectiveBiasRange.GetValue();
            var bias = friendlySideObjectivesCount - enemySideObjectivesCount;
            if (bias < 1)
                lerpDistance += bias * frontLineDB.EnemyObjectiveBias;
            else
                lerpDistance += bias * frontLineDB.FriendlyObjectiveBias;

            return double.Min(double.Max(lerpDistance, frontLineDB.ObjectiveBiasLimits.Min), frontLineDB.ObjectiveBiasLimits.Max);
        }

    }
}
