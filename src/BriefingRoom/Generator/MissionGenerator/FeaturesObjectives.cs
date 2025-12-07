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
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal class FeaturesObjectives :Features<DBEntryFeatureObjective>
    {

        internal static void GenerateMissionFeature(IBriefingRoom briefingRoom, ref DCSMission mission, string featureID, string objectiveName, int objectiveIndex, GroupInfo objectiveTarget, Side objectiveTargetSide, ObjectiveOption[] objectiveOptions, Coordinates? overrideCoords = null)
        {   
            var objCoords = overrideCoords.HasValue ? overrideCoords.Value : objectiveTarget.Coordinates;
            DBEntryFeatureObjective featureDB = briefingRoom.Database.GetEntry<DBEntryFeatureObjective>(featureID);
            if (featureDB == null) // Feature doesn't exist
            {
                briefingRoom.PrintTranslatableWarning("ObjectiveFeatureNotFound", featureID);
                return;
            }

            Coordinates? coordinates = null;
            Coordinates? coordinates2 = null;
            Dictionary<string, object> extraSettings = new(StringComparer.InvariantCultureIgnoreCase);
            var flags = featureDB.UnitGroupFlags;
            if (flags.HasFlag(FeatureUnitGroupFlags.Intercept) && objectiveTarget.DCSGroup.Waypoints.Count > 1) {
                var lerp = new MinMaxD(0.05,.95).GetValue();
                objCoords = Coordinates.Lerp(objectiveTarget.DCSGroup.Waypoints.First().Coordinates, objectiveTarget.DCSGroup.Waypoints.Last().Coordinates, lerp);
                extraSettings.AddIfKeyUnused("TimeQueueTime",  (int)Math.Floor(60*lerp));
            }
    
            if (flags.HasFlag(FeatureUnitGroupFlags.SpawnOnObjective))
            {
                coordinates = objCoords.CreateNearRandom(featureDB.UnitGroupSpawnDistance * .75, featureDB.UnitGroupSpawnDistance * 1.5); //UnitGroupSpawnDistance treated as Meters here rather than NM
                if (
                    !(featureDB.UnitGroupValidSpawnPoints.Contains(SpawnPointType.Sea) || featureDB.UnitGroupValidSpawnPoints.Contains(SpawnPointType.Air)) &&
                    SpawnPointSelector.CheckInSea(mission.TheaterDB,coordinates.Value))
                {
                    briefingRoom.PrintTranslatableWarning("CannotSpawnObjectiveFeature", $"{objectiveName}: {featureDB.UIDisplayName.Get(mission.LangKey)}");
                    return;
                }
            }
            else if (FeatureHasUnitGroup(featureDB))
            {
                Coordinates? spawnPoint =
                    SpawnPointSelector.GetRandomSpawnPoint(
                        briefingRoom.Database,
                        ref mission,
                        featureDB.UnitGroupValidSpawnPoints, objCoords,
                        new MinMaxD(featureDB.UnitGroupSpawnDistance * .75, featureDB.UnitGroupSpawnDistance * 1.5),
                        nearFrontLineFamily: flags.HasFlag(FeatureUnitGroupFlags.UseFrontLine) ? featureDB.UnitGroupFamilies.First() : null);

                if (!spawnPoint.HasValue)
                {
                    briefingRoom.PrintTranslatableWarning("NoSpawnPointForObjectiveFeature", $"{objectiveName}: {featureDB.UIDisplayName.Get(mission.LangKey)}");
                    return;
                }

                coordinates = spawnPoint;
            }

            if (coordinates.HasValue)
                coordinates2 = coordinates.Value + Coordinates.CreateRandom(10, 20) * Toolbox.NM_TO_METERS;

            if (flags.HasFlag(FeatureUnitGroupFlags.MoveToObjective))
                coordinates2 = objCoords;


           
            extraSettings.AddIfKeyUnused("ObjectiveName", objectiveName);
            extraSettings.AddIfKeyUnused("ObjectiveIndex", objectiveIndex + 1);
            extraSettings.AddIfKeyUnused("ObjectiveGroupID", objectiveTarget.GroupID);
            extraSettings.AddIfKeyUnused("ObjectiveUnitCategory", objectiveTarget.UnitDB.Category);
            extraSettings.AddIfKeyUnused("ObjectiveUnitUncontrolled", objectiveTarget.DCSGroup.Uncontrolled);

            if (featureID == "TargetDesignationLaser")
            {
                var laserCode = mission.TemplateRecord.OptionsMission.Contains("SingleLaserCode") || mission.TemplateRecord.OptionsMission.Contains("FC3LaserCode") ? (mission.TemplateRecord.OptionsMission.Contains("FC3LaserCode") ? 1113 : 1688) : mission.GetNextLaserCode();
                extraSettings.AddIfKeyUnused("LASERCODE", laserCode);
                mission.Briefing.AddItem(DCSMissionBriefingItemType.JTAC, $"{objectiveName}\t{laserCode}");
            }

            GroupInfo? groupInfo = AddMissionFeature(
                briefingRoom,
                featureDB, ref mission,
                coordinates, coordinates2,
                ref extraSettings, objectiveTargetSide, objectiveOptions.Contains(ObjectiveOption.HideTarget),
                FeaturesAsTargets: objectiveOptions.Contains(ObjectiveOption.FeaturesAsTargets));

            AddBriefingRemarkFromFeature(featureDB, ref mission, false, groupInfo, extraSettings);
        }
    }
}
