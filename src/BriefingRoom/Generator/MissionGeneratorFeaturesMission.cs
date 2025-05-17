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

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorFeaturesMission : MissionGeneratorFeatures<DBEntryFeatureMission>
    {

        internal static void GenerateMissionFeature(ref DCSMission mission, string featureID)
        {
            DBEntryFeatureMission featureDB = Database.Instance.GetEntry<DBEntryFeatureMission>(featureID);
            if (featureDB == null) // Feature doesn't exist
            {
                BriefingRoom.PrintTranslatableWarning(mission.LangKey, "MissionFeatureNotFound", featureID);
                return;
            }
            Coalition coalition = featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.Friendly) ? mission.TemplateRecord.ContextPlayerCoalition : mission.TemplateRecord.ContextPlayerCoalition.GetEnemy();

            if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.ForEachAirbase))
            {
                ForEachAirbase(ref mission, featureID, featureDB, coalition);
                return;
            }

            if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.ForEachCarrier))
            {
                ForEachCarrier(ref mission, featureDB);
                return;
            }

            if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.ForEachFOB))
            {
                ForEachFob(ref mission, featureID, featureDB);
                return;
            }
            Coordinates? spawnPoint = null;
            Coordinates? coordinates2 = null;
            if (FeatureHasUnitGroup(featureDB))
            {
                var unitFamily = Toolbox.RandomFrom(featureDB.UnitGroupFamilies);
                var useFrontLine = mission.FrontLine.Count > 0 && featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.UseFrontLine);
                Coordinates pointSearchCenter = useFrontLine ? mission.FrontLine[(int)Math.Floor((double)mission.FrontLine.Count / 2)] : Coordinates.Lerp(mission.AverageInitialPosition, mission.ObjectivesCenter, featureDB.UnitGroupSpawnDistance);
                spawnPoint =
                    UnitMakerSpawnPointSelector.GetRandomSpawnPoint(
                        ref mission,
                        featureDB.UnitGroupValidSpawnPoints, pointSearchCenter,
                        featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.AwayFromMissionArea) ? new MinMaxD(50, 100) : new MinMaxD(0, 25),
                        coalition: (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.IgnoreBorders) || featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.Neutral)) ? null : coalition,
                        nearFrontLineFamily: useFrontLine ? unitFamily : null
                        );
                if (!spawnPoint.HasValue) // No spawn point found
                {
                    throw new BriefingRoomException(mission.LangKey, "NoSpawnPointForMissionFeature", featureID);
                }


                var goPoint = spawnPoint.Value;

                if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.MoveTowardObjectives))
                    goPoint = mission.ObjectivesCenter;
                else if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.MoveAnyWhere))
                    goPoint = goPoint.CreateNearRandom(50 * Toolbox.NM_TO_METERS, 100 * Toolbox.NM_TO_METERS);
                else if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.MoveTowardPlayerBase))
                    goPoint = mission.AverageInitialPosition;

                coordinates2 = goPoint + Coordinates.CreateRandom(5, 20) * Toolbox.NM_TO_METERS;
            }
            Dictionary<string, object> extraSettings = new();
            UnitMakerGroupInfo? groupInfo = AddMissionFeature(featureDB, ref mission, spawnPoint, coordinates2, ref extraSettings, missionLevelFeature: true);

            AddBriefingRemarkFromFeature(featureDB, ref mission, false, groupInfo, extraSettings);
        }

        private static void ForEachAirbase(ref DCSMission mission, string featureID, DBEntryFeatureMission featureDB, Coalition coalition)
        {
            var activeAirbases = mission.PopulatedAirbaseIds[coalition];
            var theaterId = mission.TheaterID.ToLower();
            var airbases = Database.Instance.GetAllEntries<DBEntryAirbase>().Where(x => x.Theater == theaterId && activeAirbases.Contains(x.DCSID)).ToList();
            foreach (DBEntryAirbase airbase in airbases)
            {

                Coordinates? spawnPoint = UnitMakerSpawnPointSelector.GetNearestSpawnPoint(ref mission, featureDB.UnitGroupValidSpawnPoints, airbase.Coordinates);
                if (!spawnPoint.HasValue) // No spawn point found
                {
                    throw new BriefingRoomException(mission.LangKey, "NoSpawnPointForMissionFeature", featureID);
                }

                Dictionary<string, object> extraSettings = new() { { "TACAN_NAME", airbase.UIDisplayName.Get(mission.LangKey) } };
                UnitMakerGroupInfo? groupInfo = AddMissionFeature(featureDB, ref mission, spawnPoint.Value, spawnPoint.Value, ref extraSettings);

                AddBriefingRemarkFromFeature(featureDB, ref mission, false, groupInfo, extraSettings);
                if (featureID == "TacanAirbases")
                    AppendTacanToBriefing(ref mission, airbase.UIDisplayName.Get(mission.LangKey), extraSettings);

            }
        }

        private static void ForEachCarrier(ref DCSMission mission, DBEntryFeatureMission featureDB)
        {
            var carriers = mission.CarrierDictionary
                .Where(x => !x.Value.UnitMakerGroupInfo.UnitDB.Families.Contains(UnitFamily.FOB))
                .Select(x => x.Value)
                .ToList();
            foreach (var carrier in carriers)
            {

                var coordinates = carrier.UnitMakerGroupInfo.Coordinates + Coordinates.CreateRandom(30, 100);
                Dictionary<string, object> extraSettings = new() { { "CarrierGroupId", carrier.UnitMakerGroupInfo.GroupID } };
                UnitMakerGroupInfo? groupInfo = AddMissionFeature(featureDB, ref mission, coordinates, coordinates, ref extraSettings);

                AddBriefingRemarkFromFeature(featureDB, ref mission, false, groupInfo, extraSettings);

            }
        }

        private static void ForEachFob(ref DCSMission mission, string featureID, DBEntryFeatureMission featureDB)
        {
            var fobs = mission.CarrierDictionary
                .Where(x => x.Value.UnitMakerGroupInfo.UnitDB.Families.Contains(UnitFamily.FOB))
                .Select(x => x.Value)
                .ToList();
            foreach (var fob in fobs)
            {

                var coordinates = fob.UnitMakerGroupInfo.Coordinates + Coordinates.CreateRandom(150, 400);
                Dictionary<string, object> extraSettings = new() { { "TACAN_NAME", fob.UnitMakerGroupInfo.Name.Replace("FOB ", "") } };
                UnitMakerGroupInfo? groupInfo = AddMissionFeature(featureDB, ref mission, coordinates, coordinates, ref extraSettings);

                AddBriefingRemarkFromFeature(featureDB, ref mission, false, groupInfo, extraSettings);
                if (featureID == "TacanFOBs")
                    AppendTacanToBriefing(ref mission, fob.UnitMakerGroupInfo.Name, extraSettings);
            }
        }

        private static void AppendTacanToBriefing(ref DCSMission mission, string name, Dictionary<string, object> extraSettings)
        {
            var airbaseBriefingItems = mission.Briefing.GetItems(DCSMissionBriefingItemType.Airbase);
            var airbaseIdx = airbaseBriefingItems.FindIndex(x => x.StartsWith(name));
            if (airbaseIdx == -1)
                return;
            var airbaseStr = airbaseBriefingItems[airbaseIdx];
            airbaseStr += $"{(!airbaseStr.EndsWith("\t") ? "/" : "")}{extraSettings.GetValueOrDefault("TACANChannel", "")}X";
            mission.Briefing.UpdateItem(DCSMissionBriefingItemType.Airbase, airbaseIdx, airbaseStr);
        }
    }

}