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
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGenerator
    {

        private static readonly List<MissionStageName> STAGE_ORDER = new List<MissionStageName>{
            MissionStageName.Situation,
            MissionStageName.Airbase,
            MissionStageName.WorldPreload,
            MissionStageName.Objective,
            MissionStageName.Carrier,
            MissionStageName.PlayerFlightGroups,
            MissionStageName.CAPResponse,
            MissionStageName.AirDefense,
            MissionStageName.MissionFeatures
        };

        internal static DCSMission Generate(string langKey, MissionTemplateRecord template)
        {
            // -- START INITIALIZATION
            // Check for missing entries in the database
            GeneratorTools.CheckDBForMissingEntry<DBEntryCoalition>(template.ContextCoalitionBlue);
            GeneratorTools.CheckDBForMissingEntry<DBEntryCoalition>(template.ContextCoalitionRed);
            GeneratorTools.CheckDBForMissingEntry<DBEntryWeatherPreset>(template.EnvironmentWeatherPreset, true);
            GeneratorTools.CheckDBForMissingEntry<DBEntryTheater>(template.ContextTheater);
            if (!template.PlayerFlightGroups.Any(x => !x.Hostile))
                throw new BriefingRoomException(langKey, "NoFullyHostilePlayers");

            Toolbox.CheckObjectiveProgressionLogic(template, langKey);

            var mission = new DCSMission(langKey, template);


            Toolbox.SetMinMaxTheaterCoords(ref mission);


            // Copy values from the template
            mission.SetValue("BriefingTheater", mission.TheaterDB.UIDisplayName.Get(langKey));
            mission.SetValue("BriefingAllyCoalition", mission.CoalitionsDB[(int)template.ContextPlayerCoalition].UIDisplayName.Get(langKey));
            mission.SetValue("BriefingEnemyCoalition", mission.CoalitionsDB[(int)template.ContextPlayerCoalition.GetEnemy()].UIDisplayName.Get(langKey));
            mission.SetValue("EnableAudioRadioMessages", !template.OptionsMission.Contains("RadioMessagesTextOnly"));
            mission.SetValue("BDASetting", template.OptionsMission.Contains("NoBDA") ? "NONE" : (template.OptionsMission.Contains("TargetOnlyBDA") ? "TARGETONLY" : "ALL"));
            mission.SetValue("LuaPlayerCoalition", $"coalition.side.{template.ContextPlayerCoalition.ToString().ToUpper()}");
            mission.SetValue("LuaEnemyCoalition", $"coalition.side.{template.ContextPlayerCoalition.GetEnemy().ToString().ToUpper()}");
            mission.SetValue("TheaterID", mission.TheaterDB.DCSID);
            mission.SetValue("AircraftActivatorCurrentQueue", ""); // Just to make sure aircraft groups spawning queues are empty
            mission.SetValue("AircraftActivatorReserveQueue", "");
            mission.SetValue("AircraftActivatorIsResponsive", template.MissionFeatures.Contains("ImprovementsResponsiveAircraftActivator"));
            mission.SetValue("MissionPlayerSlots", template.GetPlayerSlotsCount() == 1 ? BriefingRoom.Translate(langKey, "SinglePlayerMission") : $"{template.GetPlayerSlotsCount()}{BriefingRoom.Translate(langKey, "XPlayerMission")}");
            mission.SetValue("CaCmdBlu", template.CombinedArmsCommanderBlue);
            mission.SetValue("CaCmdRed", template.CombinedArmsCommanderRed);
            mission.SetValue("CaJTACBlu", template.CombinedArmsJTACBlue);
            mission.SetValue("CaJTACRed", template.CombinedArmsJTACRed);
            mission.SetValue("CaJTACRed", template.CombinedArmsJTACRed);
            mission.SetValue("CaPilotControl", template.OptionsMission.Contains("CombinedArmsPilotControl"));
            mission.SetValue("EndMissionAutomatically", template.OptionsMission.Contains("EndMissionAutomatically"));
            mission.SetValue("EndMissionOnCommand", template.OptionsMission.Contains("EndMissionOnCommand"));
            mission.SetValue("InstantStart", template.PlayerFlightGroups.Any(x => x.StartLocation == PlayerStartLocation.Air));
            mission.SetValue("ShowMapMarkers", template.OptionsMission.Contains("MarkWaypoints") ? "true" : "false");
            mission.SetValue("DropOffDistanceMeters", Database.Instance.Common.DropOffDistanceMeters);


            foreach (string oggFile in Database.Instance.Common.CommonOGG)
                mission.AddMediaFile($"l10n/DEFAULT/{Toolbox.AddMissingFileExtension(oggFile, ".ogg")}", Path.Combine(BRPaths.INCLUDE_OGG, Toolbox.AddMissingFileExtension(oggFile, ".ogg")));


            mission.CoalitionsCountries = MissionGeneratorCountries.GenerateCountries(ref mission);

            BriefingRoom.PrintToLog("Generating mission date and time...");
            var month = MissionGeneratorDateTime.GenerateMissionDate(ref mission);
            MissionGeneratorDateTime.GenerateMissionTime(ref mission, month);
            mission.SaveStage(MissionStageName.Initialization);

            MissionStageName? nextStage = MissionStageName.Situation;
            int triesLeft = 5;
            int fallbackSteps = 1;
            MissionStageName? lastErrorStage = null;
            do
            {
                try
                {
                    BriefingRoom.PrintToLog($"Stage: {nextStage}");
                    switch (nextStage)
                    {
                        case MissionStageName.Situation:
                            SituationStage(ref mission);
                            break;
                        case MissionStageName.Airbase:
                            AirbaseStage(ref mission);
                            break;
                        case MissionStageName.WorldPreload:
                            WorldPreloadStage(ref mission);
                            break;
                        case MissionStageName.Objective:
                            ObjectiveStage(ref mission);
                            break;
                        case MissionStageName.Carrier:
                            CarrierStage(ref mission);
                            break;
                        case MissionStageName.PlayerFlightGroups:
                            PlayerFlightsStage(ref mission);
                            break;
                        case MissionStageName.CAPResponse:
                            CAPStage(ref mission);
                            break;
                        case MissionStageName.AirDefense:
                            AirDefenseStage(ref mission);
                            break;
                        case MissionStageName.MissionFeatures:
                            MissionFeaturesStage(ref mission);
                            nextStage = null;
                            break;
                        default:
                            nextStage = null;
                            break;
                    }
                    if (nextStage.HasValue)
                    {
                        nextStage = STAGE_ORDER[STAGE_ORDER.IndexOf(nextStage.Value) + 1];
                    }
                }
                catch (BriefingRoomException err)
                {
                    var currentStageIndex = STAGE_ORDER.IndexOf(nextStage.Value);
                    BriefingRoom.PrintToLog($"Failed on stage: {STAGE_ORDER[currentStageIndex]} => {err.Message}");
                    var revertStageCount = 1;
                    if (triesLeft > 0)
                        triesLeft--;
                    else
                    {
                        if (lastErrorStage == nextStage)
                            fallbackSteps++;
                        else
                            fallbackSteps = 1;
                        revertStageCount += fallbackSteps;
                        var fallbackStageIndex = currentStageIndex - fallbackSteps;
                        if (fallbackStageIndex <= 0)
                            throw new BriefingRoomException(langKey, "FailGeneration", err, err.Message);
                        lastErrorStage = nextStage;
                        nextStage = STAGE_ORDER[fallbackStageIndex];
                        BriefingRoom.PrintToLog($"Falling Back to Stage: {nextStage}");
                        triesLeft = 5;
                    }
                    mission.RevertStage(revertStageCount);

                }
            } while (nextStage.HasValue);



            foreach (string mediaFile in mission.GetMediaFileNames())
            {
                if (!mediaFile.ToLower().EndsWith(".ogg")) continue;
                mission.AppendValue("MapResourcesFiles", $"[\"ResKey_Snd_{Path.GetFileNameWithoutExtension(mediaFile)}\"] = \"{Path.GetFileName(mediaFile)}\",\n");
            }

            BriefingRoom.PrintToLog("Generating unitLua...");
            mission.SetValue("CountriesBlue", UnitMaker.GetUnitsLuaTable(ref mission, Coalition.Blue));
            mission.SetValue("CountriesRed", UnitMaker.GetUnitsLuaTable(ref mission, Coalition.Red));
            mission.SetValue("CountriesNeutral", UnitMaker.GetUnitsLuaTable(ref mission, Coalition.Neutral));
            mission.SetValue("RequiredModules", UnitMaker.GetRequiredModules(ref mission));
            mission.SetValue("RequiredModulesBriefing", UnitMaker.GetRequiredModulesBriefing(ref mission));
            mission.SetValue("Drawings", DrawingMaker.GetLuaDrawings(ref mission));
            mission.SetValue("Zones", ZoneMaker.GetLuaZones(ref mission));


            BriefingRoom.PrintToLog("Generating briefing...");
            var missionName = GeneratorTools.GenerateMissionName(mission.LangKey, template.BriefingMissionName);
            mission.Briefing.Name = missionName;
            mission.SetValue("MISSIONNAME", missionName);

            MissionGeneratorBriefing.GenerateMissionBriefingDescription(ref mission, template, mission.ObjectiveTargetUnitFamilies, mission.SituationDB);
            mission.SetValue("DescriptionText", mission.Briefing.GetBriefingAsRawText(ref mission, "\\\n"));
            mission.SetValue("EditorNotes", mission.Briefing.GetEditorNotes(mission.LangKey, "\\\n"));


            BriefingRoom.PrintToLog("Generating options...");
            MissionGeneratorOptions.GenerateForcedOptions(ref mission, template);

            BriefingRoom.PrintToLog("Generating warehouses...");
            MissionGeneratorWarehouses.GenerateWarehouses(ref mission, mission.CarrierDictionary);

            return mission;
        }

        private static void SituationStage(ref DCSMission mission)
        {
            var theaterID = mission.TemplateRecord.ContextTheater.ToLower();
            mission.SituationDB = Toolbox.RandomFrom(
                Database.Instance.GetAllEntries<DBEntrySituation>()
                    .Where(x => x.Theater == theaterID)
                    .ToArray()
                );
            if (mission.TemplateRecord.ContextSituation.StartsWith(mission.TemplateRecord.ContextTheater))
                mission.SituationDB = Database.Instance.GetEntry<DBEntrySituation>(mission.TemplateRecord.ContextSituation);
            mission.SetValue("BriefingSituation", mission.TemplateRecord.SpawnAnywhere ? "None" : mission.SituationDB.UIDisplayName.Get(mission.LangKey));
            mission.SetValue("BriefingSituationId", mission.TemplateRecord.SpawnAnywhere ? "None" : mission.SituationDB.ID);
            mission.AirbaseDB = mission.SituationDB.GetAirbases(mission.TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions"));


            DrawingMaker.AddTheaterZones(ref mission);

            if (mission.TheaterDB.SpawnPoints is not null)
            {
                var situationDB = mission.SituationDB;
                mission.SpawnPoints.AddRange(mission.TheaterDB.SpawnPoints.Where(x => UnitMakerSpawnPointSelector.CheckNotInNoSpawnCoords(situationDB, x.Coordinates)).ToList());
                mission.TemplateLocations.AddRange(mission.TheaterDB.TemplateLocations.Where(x => UnitMakerSpawnPointSelector.CheckNotInNoSpawnCoords(situationDB, x.Coordinates)).ToList());
            }

            var theaterDB = mission.TheaterDB;
            var brokenSP = mission.SpawnPoints.Where(x => UnitMakerSpawnPointSelector.CheckInSea(theaterDB, x.Coordinates)).ToList();
            if (brokenSP.Count > 0)
                throw new BriefingRoomException(mission.LangKey, "SpawnPointsInSea", string.Join("\n", brokenSP.Select(x => $"[{x.Coordinates.X},{x.Coordinates.Y}],{x.PointType}").ToList()));

            var brokenTL = mission.TemplateLocations.Where(x => UnitMakerSpawnPointSelector.CheckInSea(theaterDB, x.Coordinates)).ToList();
            if (brokenTL.Count > 0)
                throw new BriefingRoomException(mission.LangKey, "TemplateLocationsInSea", string.Join("\n", brokenTL.Select(x => $"[{x.Coordinates.X},{x.Coordinates.Y}],{x.LocationType}").ToList()));

            foreach (DBEntryAirbase airbase in mission.AirbaseDB)
            {
                if (airbase.ParkingSpots.Length < 1) continue;
                if (mission.AirbaseParkingSpots.ContainsKey(airbase.DCSID)) continue;
                mission.AirbaseParkingSpots.Add(airbase.DCSID, airbase.ParkingSpots.ToList());
            }

            mission.SaveStage(MissionStageName.Situation);
        }

        private static void AirbaseStage(ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Setting up airbases...");
            var requiredRunway = mission.TemplateRecord.PlayerFlightGroups.Select(x => ((DBEntryAircraft)Database.Instance.GetEntry<DBEntryJSONUnit>(x.Aircraft)).MinimumRunwayLengthFt).Max();
            mission.PlayerAirbase = MissionGeneratorAirbases.SelectStartingAirbase(mission.TemplateRecord.FlightPlanTheaterStartingAirbase, ref mission, requiredRunway: requiredRunway);
            mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Add(mission.PlayerAirbase.DCSID);
            if (mission.PlayerAirbase.DCSID > 0)
            {
                mission.MapData.Add($"AIRBASE_HOME_NAME_{mission.PlayerAirbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { mission.PlayerAirbase.Coordinates.ToArray() });
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{mission.PlayerAirbase.UIDisplayName.Get(mission.LangKey)}\t{mission.PlayerAirbase.Runways}\t{mission.PlayerAirbase.ATC}\t{mission.PlayerAirbase.ILS}\t{mission.PlayerAirbase.TACAN}");
            }
            MissionGeneratorAirbases.SelectStartingAirbaseForPackages(ref mission);
            MissionGeneratorAirbases.SetupAirbasesCoalitions(ref mission);
            ZoneMaker.AddAirbaseZones(ref mission, mission.TemplateRecord.MissionFeatures, mission.PlayerAirbase, mission.StrikePackages);
            mission.SetValue("PlayerAirbaseName", mission.PlayerAirbase.Name);
            mission.SetValue("PlayerAirbaseId", mission.PlayerAirbase.ID);
            mission.SetValue("MissionAirbaseX", mission.PlayerAirbase.Coordinates.X);
            mission.SetValue("MissionAirbaseY", mission.PlayerAirbase.Coordinates.Y);
            mission.SaveStage(MissionStageName.Airbase);
        }

        private static void WorldPreloadStage(ref DCSMission mission)
        {
            // DCS Hack to render local area near player airbase
            var extraSettings = new Dictionary<string, object>
            {
                { "NAME", "Hank the Hack" }
            };
            var (units, unitDBs) = UnitMaker.GetUnits(ref mission, new List<UnitFamily> { UnitFamily.HelicopterUtility }, 1, Side.Ally, new UnitMakerGroupFlags(), ref extraSettings, true);
            List<DBEntryAirbaseParkingSpot> parkingSpots = null;
            try
            {
                parkingSpots = UnitMakerSpawnPointSelector.GetFreeParkingSpots(
                        ref mission,
                        mission.PlayerAirbase.DCSID,
                        1, (DBEntryAircraft)unitDBs.First(), false, mission.TemplateRecord.GetPlayerSlotsCount());
            }
            catch (Exception)
            {
                // Do nothing
            }
            finally
            {
                if (parkingSpots is not null)
                {
                    extraSettings.Add("GroupAirbaseID", mission.PlayerAirbase.DCSID);
                    extraSettings.Add("ParkingID", parkingSpots.Select(x => x.DCSID).ToList());
                    extraSettings.Add("UnitCoords", parkingSpots.Select(x => x.Coordinates).ToList());
                    UnitMaker.AddUnitGroup(
                        ref mission,
                        units, Side.Ally, UnitFamily.HelicopterUtility,
                        "AircraftUncontrolled", "Aircraft",
                        parkingSpots.First().Coordinates,
                        UnitMakerGroupFlags.Invisible | UnitMakerGroupFlags.Inert | UnitMakerGroupFlags.Immortal,
                        extraSettings);
                }
                else
                    UnitMaker.AddUnitGroup(
                        ref mission,
                        units, Side.Ally, UnitFamily.HelicopterUtility,
                        "AircraftUncontrolledGround", "Aircraft",
                        mission.PlayerAirbase.Coordinates.CreateNearRandom(300, 500),
                        UnitMakerGroupFlags.Invisible | UnitMakerGroupFlags.Inert | UnitMakerGroupFlags.Immortal,
                        extraSettings);
            }
            mission.SaveStage(MissionStageName.WorldPreload);
        }

        private static void ObjectiveStage(ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating objectives...");
            var lastObjectiveCoordinates = mission.PlayerAirbase.Coordinates;
            mission.ObjectiveGroupedWaypoints = new List<List<List<Waypoint>>>();
            var i = 0;
            foreach (var objectiveTemplate in mission.TemplateRecord.Objectives)
            {
                var (objectiveCoords, waypointGroup) = MissionGeneratorObjectives.GenerateObjective(
                    mission,
                    objectiveTemplate, lastObjectiveCoordinates,
                    ref i);
                lastObjectiveCoordinates = objectiveCoords;
                mission.ObjectiveGroupedWaypoints.Add(waypointGroup);
                i++;
            }
            mission.ObjectivesCenter = (mission.ObjectiveCoordinates.Count == 0) ? mission.PlayerAirbase.Coordinates : Coordinates.Sum(mission.ObjectiveCoordinates) / mission.ObjectiveCoordinates.Count;
            mission.SetValue("MissionCenterX", mission.ObjectivesCenter.X);
            mission.SetValue("MissionCenterY", mission.ObjectivesCenter.Y);
            mission.SaveStage(MissionStageName.Objective);
        }

        private static void CarrierStage(ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating mission weather...");
            var turbulenceFromWeather = MissionGeneratorWeather.GenerateWeather(ref mission);
            (mission.WindSpeedAtSeaLevel, mission.WindDirectionAtSeaLevel) = MissionGeneratorWeather.GenerateWind(ref mission, turbulenceFromWeather);

            BriefingRoom.PrintToLog("Generating carrier groups...");
            MissionGeneratorCarrierGroup.GenerateCarrierGroup(ref mission);
            mission.AverageInitialPosition = mission.PlayerAirbase.Coordinates;
            if (mission.CarrierDictionary.Count > 0) mission.AverageInitialPosition = (mission.AverageInitialPosition + mission.CarrierDictionary.First().Value.UnitMakerGroupInfo.Coordinates) / 2.0;

            // Generate extra flight plan info
            MissionGeneratorFlightPlan.GenerateBullseyes(ref mission);
            MissionGeneratorFlightPlan.GenerateObjectiveWPCoordinatesLua(ref mission);
            MissionGeneratorFlightPlan.GenerateAircraftPackageWaypoints(ref mission);
            MissionGeneratorFlightPlan.GenerateIngressAndEgressWaypoints(ref mission);
            MissionGeneratorFlightPlan.GenerateBullseyeWaypoint(ref mission);
            MissionGeneratorFrontLine.GenerateFrontLine(ref mission);

            foreach (var waypoint in mission.Waypoints)
            {
                mission.MapData.AddIfKeyUnused($"WAYPOINT_{waypoint.Name}", new List<double[]> { waypoint.Coordinates.ToArray() });
            }
            mission.SaveStage(MissionStageName.Carrier);
        }

        private static void PlayerFlightsStage(ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating player flight groups...");
            foreach (var templateFlightGroup in mission.TemplateRecord.PlayerFlightGroups)
                MissionGeneratorPlayerFlightGroups.GeneratePlayerFlightGroup(ref mission, templateFlightGroup);
            mission.SaveStage(MissionStageName.PlayerFlightGroups);
        }

        private static void CAPStage(ref DCSMission mission)
        {
            MissionGeneratorCombatAirPatrols.GenerateCAP(ref mission);
            mission.SaveStage(MissionStageName.CAPResponse);
        }

        private static void AirDefenseStage(ref DCSMission mission)
        {
            MissionGeneratorAirDefense.GenerateAirDefense(ref mission);
            mission.SaveStage(MissionStageName.AirDefense);
        }

        private static void MissionFeaturesStage(ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating mission features...");
            mission.AppendValue("ScriptMissionFeatures", ""); // Just in case there's no features
            foreach (var templateFeature in mission.TemplateRecord.MissionFeatures)
                MissionGeneratorFeaturesMission.GenerateMissionFeature(ref mission, templateFeature);
            mission.SaveStage(MissionStageName.MissionFeatures);
        }


        internal static DCSMission GenerateRetryable(string langKey, MissionTemplate template)
        {
            var templateRecord = new MissionTemplateRecord(template);
            var mission = Policy
                .HandleResult<DCSMission>(x => x.IsExtremeDistance(template, out double distance))
                .Or<BriefingRoomException>(x =>
                {
                    BriefingRoom.PrintTranslatableWarning(langKey, "RecoverableError", x.Message);
                    return true;
                })
                .Retry(3)
                .Execute(() => Generate(langKey, templateRecord));

            if (mission.IsExtremeDistance(template, out double distance))
                BriefingRoom.PrintTranslatableWarning(langKey, "ExcessDistance", Math.Round(distance, 2));

            return mission;
        }
    }
}
