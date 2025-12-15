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
using BriefingRoom4DCS.Generator.Mission.Objectives;
using BriefingRoom4DCS.Generator.UnitMaker;
using BriefingRoom4DCS.Mission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
{
    internal abstract class Features<T> where T : DBEntryFeature
    {

        protected static GroupInfo? AddMissionFeature(IBriefingRoom briefingRoom, T featureDB, ref DCSMission mission, Coordinates? coordinates, Coordinates? coordinates2, ref Dictionary<string, object> extraSettings, Side? objectiveTargetSide = null, bool hideEnemy = false, bool missionLevelFeature = false, bool FeaturesAsTargets = false)
        {
            // Add secondary coordinates (destination point) to the extra settings

            // Feature unit group
            GroupInfo? groupInfo = null;
            if (FeatureHasUnitGroup(featureDB))
            {
                if (!coordinates2.HasValue) coordinates2 = coordinates; // No destination point? Use initial point
                extraSettings.AddIfKeyUnused("GroupX2", coordinates2.Value.X);
                extraSettings.AddIfKeyUnused("GroupY2", coordinates2.Value.Y);
                var TACANStr = mission.GetTACANSettingsFromFeature(featureDB, ref extraSettings); // Add specific settings for this feature (TACAN frequencies, etc)
                var coordinatesValue = coordinates.Value;
                GroupFlags groupFlags = 0;
                var flags = featureDB.UnitGroupFlags;
                if (flags.HasFlag(FeatureUnitGroupFlags.Immortal))
                    groupFlags |= GroupFlags.Immortal;

                if (flags.HasFlag(FeatureUnitGroupFlags.StaticAircraft))
                    groupFlags |= GroupFlags.StaticAircraft;

                if (flags.HasFlag(FeatureUnitGroupFlags.Inert))
                    groupFlags |= GroupFlags.Inert;

                if (flags.HasFlag(FeatureUnitGroupFlags.Invisible))
                    groupFlags |= GroupFlags.Invisible;


                if (flags.HasFlag(FeatureUnitGroupFlags.ImmediateAircraftActivation))
                    groupFlags |= GroupFlags.ImmediateAircraftSpawn;

                if (flags.HasFlag(FeatureUnitGroupFlags.TimedAircraftActivation))
                    groupFlags |= GroupFlags.TimedAircraftSpawn;

                if (mission.TemplateRecord.MissionFeatures.Contains("ContextScrambleStart"))
                    groupFlags |= GroupFlags.ScrambleStart;

                if (flags.HasFlag(FeatureUnitGroupFlags.RadioAircraftActivation))
                    groupFlags |= GroupFlags.RadioAircraftSpawn;

                if (flags.HasFlag(FeatureUnitGroupFlags.LowUnitVariation))
                    groupFlags |= GroupFlags.LowUnitVariation;

                Side groupSide = Side.Enemy;
                if (flags.HasFlag(FeatureUnitGroupFlags.Friendly)) groupSide = Side.Ally;
                else if (flags.HasFlag(FeatureUnitGroupFlags.Neutral)) groupSide = Side.Neutral;
                else if (flags.HasFlag(FeatureUnitGroupFlags.SameSideAsTarget) && objectiveTargetSide.HasValue) groupSide = objectiveTargetSide.Value;

                if (hideEnemy && groupSide == Side.Enemy)
                    groupFlags |= GroupFlags.AlwaysHidden;

                extraSettings.AddIfKeyUnused("DCSTask", featureDB.UnitGroupTask);

                var groupLua = featureDB.UnitGroupLuaGroup;
                var unitCount = featureDB.UnitGroupSize.GetValue();
                var luaUnit = featureDB.UnitGroupLuaUnit;
                List<string> units = [];
                List<DBEntryJSONUnit> unitDBs = [];
                if (Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Any(x => featureDB.UnitGroupFamilies.Contains(x)))
                {
                    var locationType = Toolbox.RandomFrom(Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Intersect(featureDB.UnitGroupFamilies).Select(x => Constants.THEATER_TEMPLATE_LOCATION_MAP[x]).ToList());
                    var templateLocation = SpawnPointSelector.GetNearestTemplateLocation(ref mission, locationType, coordinatesValue, true);
                    if (templateLocation.HasValue)
                    {
                        coordinatesValue = templateLocation.Value.Coordinates;
                        (units, unitDBs) = UnitGenerator.GetUnitsForTemplateLocation(briefingRoom, ref mission, templateLocation.Value, groupSide, featureDB.UnitGroupFamilies.ToList(), ref extraSettings);
                        if (units.Count == 0)
                            SpawnPointSelector.RecoverTemplateLocation(ref mission, templateLocation.Value.Coordinates);
                        else
                        {
                            extraSettings.Remove("GroupX2");
                            extraSettings.Remove("GroupY2");
                        }
                    }
                }
                if (units.Count == 0)
                {
                    (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, featureDB.UnitGroupFamilies.ToList(), unitCount, groupSide, groupFlags, ref extraSettings, featureDB.UnitGroupAllowStatic);
                }
                if (units.Count == 0)
                {
                    SpawnPointSelector.RecoverSpawnPoint(ref mission, coordinatesValue);
                    throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "NoUnitsFoundForMissionFeature", featureDB.ID);
                }
                var unitDB = unitDBs.First();
                var unitFamily = unitDB.Families.First();
                string airbaseName = null;
                try
                {
                    airbaseName = SetAirbase(briefingRoom, featureDB, ref mission, unitDB, groupSide, ref coordinatesValue, coordinates2.Value, unitCount, ref extraSettings);
                }
                catch (BriefingRoomException)
                {
                    SpawnPointSelector.RecoverSpawnPoint(ref mission, coordinatesValue);
                    throw;
                }

                if (flags.HasFlag(FeatureUnitGroupFlags.DestinationAirbase))
                {
                    var coalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, groupSide, true).Value;
                    var (airbase, _, _) = SpawnPointSelector.GetAirbaseAndParking(
                   briefingRoom,
                   mission, coordinates2.Value, unitCount,
                   coalition,
                   (DBEntryAircraft)unitDB, [(int)extraSettings.GetValueOrDefault("GroupAirbaseID", -1)]);
                    extraSettings["ENDAIRBASEID"] = airbase.DCSID;
                    extraSettings["GroupX2"] = airbase.Coordinates.X;
                    extraSettings["GroupY2"] = airbase.Coordinates.Y;
                    coordinates2 = airbase.Coordinates;
                }

                if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.FireWithinThreatRange))
                    SetFiringCoordinates(ref mission, coordinatesValue, unitDB, ref extraSettings);

                if (featureDB.UnitGroupTask == DCSTask.Escort)
                {
                    var objectiveUnitCategory = (UnitCategory)extraSettings.GetValueOrDefault("ObjectiveUnitCategory", UnitCategory.Static);
                    var objectiveUnitUncontrolled = (bool)extraSettings.GetValueOrDefault("ObjectiveUnitUncontrolled", false);
                    if (featureDB.ID == "EnemyCAP" && objectiveUnitCategory == UnitCategory.Plane && !objectiveUnitUncontrolled)
                        groupLua = "AircraftEscort";

                    if (featureDB.ID == "EnemyHeloEscort" && new List<UnitCategory> { UnitCategory.Infantry, UnitCategory.Ship, UnitCategory.Vehicle, UnitCategory.Helicopter }.Contains(objectiveUnitCategory) && !objectiveUnitUncontrolled)
                        groupLua = "AircraftEscort";
                }

                groupInfo = UnitGenerator.AddUnitGroup(
                    briefingRoom,
                    ref mission,
                    units,
                    groupSide,
                    unitFamily,
                    groupLua, luaUnit,
                    coordinatesValue, groupFlags,
                    extraSettings);

                if (FeaturesAsTargets && flags.HasFlag(FeatureUnitGroupFlags.ObjectiveTargetable) && groupSide == objectiveTargetSide)
                    ObjectiveUtils.AssignTargetSuffix(ref groupInfo, (string)extraSettings.GetValueOrDefault("ObjectiveName"), unitFamily.GetUnitCategory() == UnitCategory.Static || unitFamily.GetUnitCategory() == UnitCategory.Cargo);

                SetCarrier(ref mission, featureDB, groupSide, ref groupInfo);
                SetSupportingTargetGroupName(ref groupInfo, flags, extraSettings);
                if (
                    groupSide == Side.Ally &&
                    groupInfo.HasValue &&
                    groupInfo.Value.UnitDB != null &&
                    groupInfo.Value.UnitDB.IsAircraft &&
                    !flags.HasFlag(FeatureUnitGroupFlags.StaticAircraft))
                    mission.Briefing.AddItem(DCSMissionBriefingItemType.FlightGroup,
                            $"{groupInfo.Value.Name.Split("-")[0]}{(featureDB.GetDBEntryInfo().Category.Get("en") == "Direct Support" ? "(DS)" : "")}\t" +
                            $"{unitCount}× {groupInfo.Value.UnitDB.UIDisplayName.Get(mission.LangKey)}\t" +
                            $"{GeneratorTools.FormatRadioFrequency(groupInfo.Value.Frequency)}{TACANStr}\t" +
                            $"{featureDB.UnitGroupTask}" +
                            (airbaseName != null ? $"\t{airbaseName}" : "") +
                            (airbaseName != null ? $"\t{airbaseName}" : ""));
                if (!groupInfo.Value.UnitDB.IsAircraft)
                    mission.MapData.Add($"UNIT-{groupInfo.Value.UnitDB.Families[0]}-{groupSide}-{groupInfo.Value.GroupID}", new List<double[]> { groupInfo.Value.Coordinates.ToArray() });

                if (featureDB.ExtraGroups.Max > 1)
                    SpawnExtraGroups(briefingRoom, featureDB, ref mission, groupSide, groupFlags, coordinatesValue, coordinates2.Value, extraSettings);
            }

            // Feature Lua script
            string featureLua = "";

            // Adds the features' group ID to the briefingRoom.mission.missionFeatures.groupsID table
            if (missionLevelFeature)
            {
                featureLua += $"briefingRoom.mission.missionFeatures.groupNames.{GeneratorTools.LowercaseFirstCharacter(featureDB.ID)} = \"{(groupInfo.HasValue ? groupInfo.Value.Name : 0)}\"\n";
                featureLua += $"briefingRoom.mission.missionFeatures.unitNames.{GeneratorTools.LowercaseFirstCharacter(featureDB.ID)} = {{{(groupInfo.HasValue ? string.Join(",", groupInfo.Value.UnitNames.Select(x => $"\"{x}\"")) : "")}}}\n";
            }

            if (!string.IsNullOrEmpty(featureDB.IncludeLuaSettings)) featureLua = featureDB.IncludeLuaSettings + "\n";
            foreach (string luaFile in featureDB.IncludeLua)
            {
                var fileLua = Toolbox.ReadAllTextIfFileExists(Path.Combine(featureDB.SourceLuaDirectory, luaFile));
                if (fileLua.StartsWith("-- BR SINGLETON FLAG"))
                { // Script should be used only once in the app and should be ordered infront of all feature scripts
                    mission.AppendSingletonValue(luaFile, "ScriptSingletons", fileLua);
                    continue;
                }
                featureLua += fileLua + "\n";
            }
            foreach (KeyValuePair<string, object> extraSetting in extraSettings)
                GeneratorTools.ReplaceKey(ref featureLua, extraSetting.Key, extraSetting.Value);
            if (groupInfo.HasValue)
                GeneratorTools.ReplaceKey(ref featureLua, "FeatureGroupID", groupInfo.Value.GroupID);

            if (featureDB is DBEntryFeatureObjective) mission.AppendValue("ScriptObjectivesFeatures", featureLua);
            else mission.AppendValue("ScriptMissionFeatures", featureLua);

            // Add feature ogg files
            foreach (string oggFile in featureDB.IncludeOgg)
                mission.AddMediaFile($"l10n/DEFAULT/{oggFile}", Path.Combine(BRPaths.INCLUDE_OGG, oggFile));

            if (!String.IsNullOrEmpty(featureDB.IncludeOggFolder))
                mission.AddMediaFolder(featureDB.IncludeOggFolder, Path.Combine(BRPaths.INCLUDE_OGG, featureDB.IncludeOggFolder));

            return groupInfo;
        }

        protected static void AddBriefingRemarkFromFeature(T featureDB, ref DCSMission mission, bool useEnemyRemarkIfAvailable, GroupInfo? groupInfo, Dictionary<string, object> stringReplacements)
        {
            string remarkString;
            if (useEnemyRemarkIfAvailable && !string.IsNullOrEmpty(featureDB.BriefingRemarks[(int)Side.Enemy].Get(mission.LangKey)))
                remarkString = featureDB.BriefingRemarks[(int)Side.Enemy].Get(mission.LangKey);
            else
                remarkString = featureDB.BriefingRemarks[(int)Side.Ally].Get(mission.LangKey);
            if (string.IsNullOrEmpty(remarkString)) return; // No briefing remarks for this feature

            string remark = Toolbox.RandomFrom(remarkString.Split(";"));
            foreach (KeyValuePair<string, object> stringReplacement in stringReplacements)
                GeneratorTools.ReplaceKey(ref remark, stringReplacement.Key, stringReplacement.Value.ToString());

            if (groupInfo.HasValue)
            {
                GeneratorTools.ReplaceKey(ref remark, "GroupName", groupInfo.Value.Name);
                GeneratorTools.ReplaceKey(ref remark, "GroupFrequency", GeneratorTools.FormatRadioFrequency(groupInfo.Value.Frequency));
                GeneratorTools.ReplaceKey(ref remark, "GroupUnitName", groupInfo.Value.UnitDB.UIDisplayName);
            }

            mission.Briefing.AddItem(DCSMissionBriefingItemType.Remark, remark, featureDB is DBEntryFeatureMission);
        }



        internal static bool FeatureHasUnitGroup(T featureDB)
        {
            return (featureDB.UnitGroupFamilies.Length > 0) &&
                 !string.IsNullOrEmpty(featureDB.UnitGroupLuaGroup) &&
                 !string.IsNullOrEmpty(featureDB.UnitGroupLuaUnit);
        }

        private static void SpawnExtraGroups(IBriefingRoom briefingRoom, T featureDB, ref DCSMission mission, Side groupSide, GroupFlags groupFlags, Coordinates coordinates, Coordinates coordinates2, Dictionary<string, object> extraSettings)
        {
            var flags = featureDB.UnitGroupFlags;
            foreach (var i in Enumerable.Range(1, featureDB.ExtraGroups.GetValue()))
            {
                if (flags.HasFlag(FeatureUnitGroupFlags.MoveAnyWhere))
                {
                    coordinates = coordinates.CreateNearRandom(50 * Toolbox.NM_TO_METERS, 100 * Toolbox.NM_TO_METERS);
                    coordinates2 = coordinates.CreateNearRandom(50 * Toolbox.NM_TO_METERS, 100 * Toolbox.NM_TO_METERS);
                    if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.DestinationSpawnPoint))
                    {
                        var coordinates2Spawn = SpawnPointSelector.GetNearestSpawnPoint(mission, featureDB.UnitGroupValidSpawnPoints, coordinates);
                        if (!coordinates2Spawn.HasValue)
                        {
                            briefingRoom.PrintTranslatableWarning("NoExtraGroupSpawnPoint", featureDB.UIDisplayName.Get(mission.LangKey));
                            continue;
                        }
                        coordinates2 = coordinates2Spawn.Value;
                    }
                    extraSettings["GroupX2"] = coordinates2.X;
                    extraSettings["GroupY2"] = coordinates2.Y;
                }
                var groupLua = featureDB.UnitGroupLuaGroup;
                var unitCount = featureDB.UnitGroupSize.GetValue();
                var luaUnit = featureDB.UnitGroupLuaUnit;
                Coordinates? spawnCoords = null;
                extraSettings.Remove("TemplatePositionMap");
                List<string> units = [];
                List<DBEntryJSONUnit> unitDBs = [];
                if (Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Any(x => featureDB.UnitGroupFamilies.Contains(x)))
                {
                    var locationType = Toolbox.RandomFrom(Constants.THEATER_TEMPLATE_LOCATION_MAP.Keys.Intersect(featureDB.UnitGroupFamilies).Select(x => Constants.THEATER_TEMPLATE_LOCATION_MAP[x]).ToList());
                    var templateLocation = SpawnPointSelector.GetNearestTemplateLocation(ref mission, locationType, coordinates, true);
                    if (templateLocation.HasValue)
                    {
                        spawnCoords = templateLocation.Value.Coordinates;
                        (units, unitDBs) = UnitGenerator.GetUnitsForTemplateLocation(briefingRoom, ref mission, templateLocation.Value, groupSide, featureDB.UnitGroupFamilies.ToList(), ref extraSettings);
                        if (units.Count == 0)
                            SpawnPointSelector.RecoverTemplateLocation(ref mission, templateLocation.Value.Coordinates);
                        else
                        {
                            extraSettings.Remove("GroupX2");
                            extraSettings.Remove("GroupY2");
                        }
                    }
                }

                var unitFamily = unitDBs.Any() ? unitDBs.First().Families.First() : featureDB.UnitGroupFamilies.First();
                if (units.Count == 0)
                {
                    (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, featureDB.UnitGroupFamilies.ToList(), unitCount, groupSide, groupFlags, ref extraSettings, featureDB.UnitGroupAllowStatic);
                    if (units.Count == 0)
                    {
                        throw new BriefingRoomException(briefingRoom.Database, mission.LangKey, "NoUnitsFoundForMissionFeature", featureDB.ID);
                    }
                    unitFamily = unitDBs.First().Families.First();
                    if (flags.HasFlag(FeatureUnitGroupFlags.ExtraGroupsNearby))
                        spawnCoords = SpawnPointSelector.GetNearestSpawnPoint(mission, featureDB.UnitGroupValidSpawnPoints, coordinates);
                    else
                    {
                        var spawnDistance = new MinMaxD(0, 5);
                        if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.SpawnAnywhere))
                            spawnDistance = new MinMaxD(0, 100);
                        else if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.AwayFromMissionArea))
                            spawnDistance = new MinMaxD(50, 100);
                        spawnCoords = SpawnPointSelector.GetRandomSpawnPoint(
                            briefingRoom.Database,
                            ref mission,
                            featureDB.UnitGroupValidSpawnPoints, coordinates,
                            spawnDistance,
                            coalition: GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, groupSide),
                            nearFrontLineFamily: featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.UseFrontLine) ? unitFamily : null
                            );
                    }
                    if (!spawnCoords.HasValue)
                    {
                        briefingRoom.PrintTranslatableWarning("NoExtraGroupSpawnPoint", featureDB.UIDisplayName.Get(mission.LangKey));
                        continue;
                    }
                }

                var unitDB = unitDBs.First();
                string airbaseName = null;
                coordinates = spawnCoords.Value;
                try
                {
                    airbaseName = SetAirbase(briefingRoom, featureDB, ref mission, unitDB, groupSide, ref coordinates, coordinates2, unitCount, ref extraSettings);
                }
                catch (BriefingRoomRawException)
                {
                    SpawnPointSelector.RecoverSpawnPoint(ref mission, coordinates);
                    continue;
                }

                if (flags.HasFlag(FeatureUnitGroupFlags.DestinationAirbase))
                {
                    var coalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, groupSide, true).Value;
                    var (airbase, _, _) = SpawnPointSelector.GetAirbaseAndParking(
                   briefingRoom,
                   mission, coordinates2, unitCount,
                   coalition,
                   (DBEntryAircraft)unitDB, [(int)extraSettings.GetValueOrDefault("GroupAirbaseID", -1)]);
                    extraSettings["ENDAIRBASEID"] = airbase.DCSID;
                    extraSettings["GroupX2"] = airbase.Coordinates.X;
                    extraSettings["GroupY2"] = airbase.Coordinates.Y;
                    coordinates2 = airbase.Coordinates;
                }

                if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.FireWithinThreatRange))
                    SetFiringCoordinates(ref mission, coordinates, unitDB, ref extraSettings);

                var groupInfo = UnitGenerator.AddUnitGroup(
                    briefingRoom,
                    ref mission,
                    units,
                    groupSide,
                    unitFamily,
                    groupLua, luaUnit,
                    coordinates, groupFlags,
                    extraSettings);

                SetCarrier(ref mission, featureDB, groupSide, ref groupInfo);
                SetSupportingTargetGroupName(ref groupInfo, flags, extraSettings);


                if (
                   groupSide == Side.Ally &&
                   groupInfo.HasValue &&
                   groupInfo.Value.UnitDB != null &&
                   groupInfo.Value.UnitDB.IsAircraft &&
                   !flags.HasFlag(FeatureUnitGroupFlags.StaticAircraft))
                    mission.Briefing.AddItem(DCSMissionBriefingItemType.FlightGroup,
                            $"{groupInfo.Value.Name.Split("-")[0]}\t" +
                            $"{unitCount}× {groupInfo.Value.UnitDB.UIDisplayName.Get(mission.LangKey)}\t" +
                            $"{GeneratorTools.FormatRadioFrequency(groupInfo.Value.Frequency)}\t" +
                            $"{featureDB.UnitGroupTask}" +
                            (airbaseName != null ? $"\t{airbaseName}" : "") +
                            (airbaseName != null ? $"{airbaseName}" : ""));
                if (!groupInfo.Value.UnitDB.IsAircraft)
                    mission.MapData.Add($"UNIT-{groupInfo.Value.UnitDB.Families[0]}-{groupSide}-{groupInfo.Value.GroupID}", new List<double[]> { groupInfo.Value.Coordinates.ToArray() });
            }
        }

        private static string SetAirbase(IBriefingRoom briefingRoom, T featureDB, ref DCSMission mission, DBEntryJSONUnit unitDB, Side groupSide, ref Coordinates coordinates, Coordinates coordinates2, int unitCount, ref Dictionary<string, object> extraSettings)
        {
            if ((mission.TemplateRecord.MissionFeatures.Contains("ContextGroundStartAircraft") || featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.GroundStart)) && unitDB.IsAircraft && featureDB.UnitGroupLuaGroup != "AircraftEscort")
            {
                var coalition = GeneratorTools.GetSpawnPointCoalition(mission.TemplateRecord, groupSide, true).Value;
                var (airbase, parkingSpotIDsList, parkingSpotCoordinatesList) = SpawnPointSelector.GetAirbaseAndParking(
                    briefingRoom,
                    mission, coordinates, unitCount,
                    coalition,
                    (DBEntryAircraft)unitDB);
                coordinates = airbase.Coordinates;
                extraSettings["ParkingID"] = parkingSpotIDsList;
                extraSettings["GroupAirbaseID"] = airbase.DCSID;
                mission.PopulatedAirbaseIds[coalition].Add(airbase.DCSID);
                extraSettings["UnitCoords"] = parkingSpotCoordinatesList;

                var midPoint = Coordinates.Lerp(coordinates, coordinates2, 0.4);
                extraSettings.AddIfKeyUnused("GroupMidX", midPoint.X);
                extraSettings.AddIfKeyUnused("GroupMidY", midPoint.Y);
                mission.MapData.AddIfKeyUnused($"AIRBASE_AI_{groupSide}_NAME_{airbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { airbase.Coordinates.ToArray() });
                return airbase.Name;
            }
            return null;
        }

        private static void SetCarrier(ref DCSMission mission, T featureDB, Side side, ref GroupInfo? groupInfo)
        {

            if (
                side == Side.Enemy ||
                (!mission.TemplateRecord.MissionFeatures.Contains("ContextGroundStartAircraft") && featureDB.ID != "FriendlyStaticAircraftCarrier")
                || !groupInfo.Value.UnitDB.Families.Intersect(new[] { UnitFamily.PlaneCATOBAR, UnitFamily.PlaneSTOBAR, UnitFamily.PlaneSTOVL }).Any()
            )
                return;
            bool isPlane = groupInfo.Value.UnitDB.Category == UnitCategory.Plane;
            UnitFamily targetFamily = UnitFamily.ShipCarrierSTOVL;
            if (groupInfo.Value.UnitDB.Families.Contains(UnitFamily.PlaneCATOBAR))
                targetFamily = UnitFamily.ShipCarrierCATOBAR;
            if (groupInfo.Value.UnitDB.Families.Contains(UnitFamily.PlaneSTOBAR))
                targetFamily = UnitFamily.ShipCarrierSTOBAR;
            var unitCount = groupInfo.Value.DCSGroup.Units.Count;
            var carrierPool = mission.CarrierDictionary.Where(x =>
                    x.Value.GroupInfo.UnitDB.Families.Contains(targetFamily) &&
                    (isPlane ? x.Value.RemainingPlaneSpotCount : x.Value.RemainingHelicopterSpotCount) >= unitCount
                ).ToDictionary(x => x.Key, x => x.Value);

            if (carrierPool.Count == 0)
                return;

            var carrier = Toolbox.RandomFrom(carrierPool.Values.ToArray());
            groupInfo.Value.DCSGroup.Waypoints[0].LinkUnit = carrier.GroupInfo.DCSGroup.Units[0].UnitId;
            groupInfo.Value.DCSGroup.Waypoints[0].HelipadId = carrier.GroupInfo.DCSGroup.Units[0].UnitId;
            groupInfo.Value.DCSGroup.Waypoints[0].X = (float)carrier.GroupInfo.Coordinates.X;
            groupInfo.Value.DCSGroup.Waypoints[0].Y = (float)carrier.GroupInfo.Coordinates.Y;
            groupInfo.Value.DCSGroup.X = (float)carrier.GroupInfo.Coordinates.X;
            groupInfo.Value.DCSGroup.Y = (float)carrier.GroupInfo.Coordinates.Y;
            groupInfo.Value.DCSGroup.Name = groupInfo.Value.DCSGroup.Name.Replace("-STATIC-", ""); // Remove Static code if on carrier as we can't replace it automatically
            if (isPlane)
                carrier.RemainingPlaneSpotCount -= unitCount;
            else
                carrier.RemainingHelicopterSpotCount -= unitCount;
        }

        private static void SetSupportingTargetGroupName(ref GroupInfo? groupInfo, FeatureUnitGroupFlags flags, Dictionary<string, object> extraSettings)
        {
            if (flags.HasFlag(FeatureUnitGroupFlags.SupportingTarget))
                groupInfo.Value.DCSGroups.ForEach(x => x.Name += $"-STGT-{extraSettings["ObjectiveName"]}");
        }

        private static Coordinates GetFiringCoordinates(ref DCSMission mission, Coordinates coordinates, DBEntryJSONUnit unitDB)
        {
            var angle = SpawnPointSelector.GetDirToFrontLine(ref mission, coordinates);
            return Coordinates.FromAngleAndDistance(coordinates, new MinMaxD(unitDB.ThreatRangeMin, unitDB.ThreatRange), new MinMaxD(angle - 45, angle + 45).GetValue());
        }

        private static void SetFiringCoordinates(ref DCSMission mission, Coordinates coordinates, DBEntryJSONUnit unitDB, ref Dictionary<string, object> extraSettings)
        {
            var timeInterval = 720;
            var minTime = 0;
            var maxTime = timeInterval;

            for (int i = 0; i < 5; i++)
            {
                var coords = GetFiringCoordinates(ref mission, coordinates, unitDB);
                extraSettings[$"FireX{i + 1}"] = coords.X;
                extraSettings[$"FireY{i + 1}"] = coords.Y;
                extraSettings[$"FireTime{i + 1}"] = new MinMaxI(minTime, maxTime).GetValue();
                minTime += timeInterval;
                maxTime += timeInterval;
            }
        }

    }
}
