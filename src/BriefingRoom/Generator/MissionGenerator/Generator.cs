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
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator.Mission
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

        internal static DCSMission Generate(IBriefingRoom briefingRoom, MissionTemplateRecord template)
        {
            // -- START INITIALIZATION
            // Check for missing entries in the database
            GeneratorTools.CheckDBForMissingEntry<DBEntryCoalition>(briefingRoom.Database, template.ContextCoalitionBlue);
            GeneratorTools.CheckDBForMissingEntry<DBEntryCoalition>(briefingRoom.Database, template.ContextCoalitionRed);
            GeneratorTools.CheckDBForMissingEntry<DBEntryWeatherPreset>(briefingRoom.Database, template.EnvironmentWeatherPreset, true);
            GeneratorTools.CheckDBForMissingEntry<DBEntryTheater>(briefingRoom.Database, template.ContextTheater);
            if (!template.PlayerFlightGroups.Any(x => !x.Hostile))
                throw new BriefingRoomException(briefingRoom.Database, briefingRoom.LanguageKey, "NoFullyHostilePlayers");

            Toolbox.CheckObjectiveProgressionLogic(briefingRoom.Database, template, briefingRoom.LanguageKey);

            var mission = new DCSMission(briefingRoom.Database, briefingRoom.LanguageKey, template);


            Toolbox.SetMinMaxTheaterCoords(briefingRoom.Database, ref mission);


            // Copy values from the template
            mission.SetValue("BriefingTheater", mission.TheaterDB.UIDisplayName.Get(briefingRoom.LanguageKey));
            mission.SetValue("BriefingAllyCoalition", mission.CoalitionsDB[(int)template.ContextPlayerCoalition].UIDisplayName.Get(briefingRoom.LanguageKey));
            mission.SetValue("BriefingEnemyCoalition", mission.CoalitionsDB[(int)template.ContextPlayerCoalition.GetEnemy()].UIDisplayName.Get(briefingRoom.LanguageKey));
            mission.SetValue("EnableAudioRadioMessages", !template.OptionsMission.Contains("RadioMessagesTextOnly"));
            mission.SetValue("BDASetting", template.OptionsMission.Contains("NoBDA") ? "NONE" : (template.OptionsMission.Contains("TargetOnlyBDA") ? "TARGETONLY" : "ALL"));
            mission.SetValue("LuaPlayerCoalition", $"coalition.side.{template.ContextPlayerCoalition.ToString().ToUpper()}");
            mission.SetValue("LuaEnemyCoalition", $"coalition.side.{template.ContextPlayerCoalition.GetEnemy().ToString().ToUpper()}");
            mission.SetValue("TheaterID", mission.TheaterDB.DCSID);
            mission.SetValue("AircraftActivatorCurrentQueue", ""); // Just to make sure aircraft groups spawning queues are empty
            mission.SetValue("AircraftActivatorReserveQueue", "");
            mission.SetValue("AircraftActivatorIsResponsive", template.MissionFeatures.Contains("ImprovementsResponsiveAircraftActivator"));
            mission.SetValue("MissionPlayerSlots", mission.IsSinglePlayerMission ? briefingRoom.Translate("SinglePlayerMission") : $"{template.GetPlayerSlotsCount()}{briefingRoom.Translate("XPlayerMission")}");
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
            mission.SetValue("DropOffDistanceMeters", briefingRoom.Database.Common.DropOffDistanceMeters);
            mission.SetValue("NextTrigIndex", 4); // Just to make sure we start from the correct trigger index
            mission.SetValue("TrigActions", "");
            mission.SetValue("TrigFuncs", "");
            mission.SetValue("TrigFlags", "");
            mission.SetValue("TrigConditions", "");
            mission.SetValue("TrigRules", "");


            foreach (string oggFile in briefingRoom.Database.Common.CommonOGG)
                mission.AddMediaFile($"l10n/DEFAULT/{Toolbox.AddMissingFileExtension(oggFile, ".ogg")}", Path.Combine(BRPaths.INCLUDE_OGG, Toolbox.AddMissingFileExtension(oggFile, ".ogg")));


            mission.CoalitionsCountries = Countries.GenerateCountries(briefingRoom.Database, ref mission);

            BriefingRoom.PrintToLog("Generating mission date and time...");
            var month = Temporal.GenerateMissionDate(ref mission);
            Temporal.GenerateMissionTime(ref mission, month);
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
                            SituationStage(briefingRoom.Database, ref mission);
                            break;
                        case MissionStageName.Airbase:
                            AirbaseStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.WorldPreload:
                            WorldPreloadStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.Objective:
                            ObjectiveStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.Carrier:
                            CarrierStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.PlayerFlightGroups:
                            PlayerFlightsStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.CAPResponse:
                            CAPStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.AirDefense:
                            AirDefenseStage(briefingRoom, ref mission);
                            break;
                        case MissionStageName.MissionFeatures:
                            MissionFeaturesStage(briefingRoom, ref mission);
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
                catch (BriefingRoomRawException err)
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
                            throw new BriefingRoomException(briefingRoom.Database, briefingRoom.LanguageKey, "FailGeneration", err, err.Message);
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
            mission.SetValue("CountriesBlue", UnitGenerator.GetUnitsLuaTable(ref mission, Coalition.Blue));
            mission.SetValue("CountriesRed", UnitGenerator.GetUnitsLuaTable(ref mission, Coalition.Red));
            mission.SetValue("CountriesNeutral", UnitGenerator.GetUnitsLuaTable(ref mission, Coalition.Neutral));
            mission.SetValue("RequiredModules", UnitGenerator.GetRequiredModules(ref mission));
            mission.SetValue("RequiredModulesBriefing", UnitGenerator.GetRequiredModulesBriefing(ref mission));
            mission.SetValue("Drawings", DrawingMaker.GetLuaDrawings(ref mission));
            mission.SetValue("Zones", ZoneMaker.GetLuaZones(ref mission));


            BriefingRoom.PrintToLog("Generating briefing...");
            var missionName = GeneratorTools.GenerateMissionName(briefingRoom.Database, mission.LangKey, template.BriefingMissionName);
            mission.Briefing.Name = missionName;
            mission.SetValue("MISSIONNAME", missionName);

            Briefing.GenerateMissionBriefingDescription(briefingRoom.Database, ref mission, template, mission.ObjectiveTargetUnitFamilies, mission.SituationDB);
            mission.SetValue("DescriptionText", mission.Briefing.GetBriefingAsRawText(briefingRoom.Database, ref mission, "\\\n"));
            mission.SetValue("EditorNotes", mission.Briefing.GetEditorNotes(briefingRoom.Database, mission.LangKey, "\\\n"));


            BriefingRoom.PrintToLog("Generating options...");
            Options.GenerateForcedOptions(ref mission, template);

            BriefingRoom.PrintToLog("Generating warehouses...");
            Warehouses.GenerateWarehouses(ref mission, mission.CarrierDictionary);

            return mission;
        }

        private static void SituationStage(IDatabase database, ref DCSMission mission)
        {
            var theaterID = mission.TemplateRecord.ContextTheater.ToLower();
            mission.SituationDB = Toolbox.RandomFrom(
                database.GetAllEntries<DBEntrySituation>()
                    .Where(x => x.Theater == theaterID)
                    .ToArray()
                );
            if (mission.TemplateRecord.ContextSituation.StartsWith(mission.TemplateRecord.ContextTheater))
                mission.SituationDB = database.GetEntry<DBEntrySituation>(mission.TemplateRecord.ContextSituation);
            mission.SetValue("BriefingSituation", mission.TemplateRecord.SpawnAnywhere ? "None" : mission.SituationDB.UIDisplayName.Get(mission.LangKey));
            mission.SetValue("BriefingSituationId", mission.TemplateRecord.SpawnAnywhere ? "None" : mission.SituationDB.ID);
            mission.AirbaseDB = mission.SituationDB.GetAirbases(database, mission.TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions"));


            DrawingMaker.AddTheaterZones(ref mission);

            if (mission.TheaterDB.SpawnPoints is not null)
            {
                var situationDB = mission.SituationDB;
                mission.SpawnPoints.AddRange(mission.TheaterDB.SpawnPoints.Where(x => SpawnPointSelector.CheckNotInNoSpawnCoords(situationDB, x.Coordinates)).ToList());
                mission.TemplateLocations.AddRange(mission.TheaterDB.TemplateLocations.Where(x => SpawnPointSelector.CheckNotInNoSpawnCoords(situationDB, x.Coordinates)).ToList());
            }

            var theaterDB = mission.TheaterDB;
            var brokenSP = mission.SpawnPoints.Where(x => SpawnPointSelector.CheckInSea(theaterDB, x.Coordinates)).ToList();
            if (brokenSP.Count > 0)
                throw new BriefingRoomException(database, mission.LangKey, "SpawnPointsInSea", string.Join("\n", brokenSP.Select(x => $"[{x.Coordinates.X},{x.Coordinates.Y}],{x.PointType}").ToList()));

            var brokenTL = mission.TemplateLocations.Where(x => SpawnPointSelector.CheckInSea(theaterDB, x.Coordinates)).ToList();
            if (brokenTL.Count > 0)
                throw new BriefingRoomException(database, mission.LangKey, "TemplateLocationsInSea", string.Join("\n", brokenTL.Select(x => $"[{x.Coordinates.X},{x.Coordinates.Y}],{x.LocationType}").ToList()));

            foreach (DBEntryAirbase airbase in mission.AirbaseDB)
            {
                if (airbase.ParkingSpots.Length < 1) continue;
                if (mission.AirbaseParkingSpots.ContainsKey(airbase.DCSID)) continue;
                mission.AirbaseParkingSpots.Add(airbase.DCSID, airbase.ParkingSpots.ToList());
            }

            mission.SaveStage(MissionStageName.Situation);
        }

        private static void AirbaseStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Setting up airbases...");
            mission.PlayerAirbase = Airbases.SelectStartingAirbase(briefingRoom.Database, mission.TemplateRecord.FlightPlanTheaterStartingAirbase, ref mission);
            mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Add(mission.PlayerAirbase.DCSID);
            if (mission.PlayerAirbase.DCSID > 0)
            {
                mission.MapData.Add($"AIRBASE_HOME_NAME_{mission.PlayerAirbase.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { mission.PlayerAirbase.Coordinates.ToArray() });
                mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{mission.PlayerAirbase.UIDisplayName.Get(mission.LangKey)}\t{mission.PlayerAirbase.Runways}\t{mission.PlayerAirbase.ATC}\t{mission.PlayerAirbase.ILS}\t{mission.PlayerAirbase.TACAN}");
            }
            if (mission.TemplateRecord.FlightPlanTheaterDestinationAirbase == "home")
                mission.PlayerAirbaseDestination = mission.PlayerAirbase;
            else
            {
                mission.PlayerAirbaseDestination = Airbases.SelectStartingAirbase(briefingRoom.Database, mission.TemplateRecord.FlightPlanTheaterDestinationAirbase, ref mission);
                mission.PopulatedAirbaseIds[mission.TemplateRecord.ContextPlayerCoalition].Add(mission.PlayerAirbaseDestination.DCSID);
                if (mission.PlayerAirbaseDestination.DCSID > 0)
                {
                    mission.MapData.Add($"AIRBASE_DEST_NAME_{mission.PlayerAirbaseDestination.UIDisplayName.Get(mission.LangKey)}", new List<double[]> { mission.PlayerAirbaseDestination.Coordinates.ToArray() });
                    mission.Briefing.AddItem(DCSMissionBriefingItemType.Airbase, $"{mission.PlayerAirbaseDestination.UIDisplayName.Get(mission.LangKey)}\t{mission.PlayerAirbaseDestination.Runways}\t{mission.PlayerAirbaseDestination.ATC}\t{mission.PlayerAirbaseDestination.ILS}\t{mission.PlayerAirbaseDestination.TACAN}");
                }
            }

            Airbases.SelectStartingAirbaseForPackages(briefingRoom.Database, ref mission);
            Airbases.SetupAirbasesCoalitions(ref mission);
            ZoneMaker.AddAirbaseZones(briefingRoom, ref mission);
            mission.SetValue("PlayerAirbaseName", mission.PlayerAirbase.Name);
            mission.SetValue("PlayerAirbaseId", mission.PlayerAirbase.ID);
            mission.SetValue("MissionAirbaseX", mission.PlayerAirbase.Coordinates.X);
            mission.SetValue("MissionAirbaseY", mission.PlayerAirbase.Coordinates.Y);

            mission.SetValue("PlayerAirbaseDestinationId", mission.PlayerAirbaseDestination.ID);
            mission.SetValue("MissionAirbase2X", mission.PlayerAirbaseDestination.Coordinates.X);
            mission.SetValue("MissionAirbase2Y", mission.PlayerAirbaseDestination.Coordinates.Y);
            mission.SaveStage(MissionStageName.Airbase);
        }

        private static void WorldPreloadStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            if (mission.TemplateRecord.ContextDecade < Decade.Decade1960) // Helicopters were not available in DCS until 1960
            {
                BriefingRoom.PrintToLog("Skipping world preload stage for helicopters.");
                mission.SaveStage(MissionStageName.WorldPreload);
                return;
            }
            // DCS Hack to render local area near player airbase
            var extraSettings = new Dictionary<string, object>
            {
                { "NAME", "Hank the Hack" }
            };
            var (units, unitDBs) = UnitGenerator.GetUnits(briefingRoom, ref mission, new List<UnitFamily> { UnitFamily.HelicopterUtility }, 1, Side.Ally, new GroupFlags(), ref extraSettings, true);
            List<DBEntryAirbaseParkingSpot> parkingSpots = null;
            if (unitDBs.Count == 0)
            {
                mission.SaveStage(MissionStageName.WorldPreload);
                return;
            }
            try
            {
                parkingSpots = SpawnPointSelector.GetFreeParkingSpots(
                        briefingRoom,
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
                    UnitGenerator.AddUnitGroup(
                        briefingRoom,
                        ref mission,
                        units, Side.Ally, UnitFamily.HelicopterUtility,
                        "AircraftUncontrolled", "Aircraft",
                        parkingSpots.First().Coordinates,
                        GroupFlags.Invisible | GroupFlags.Inert | GroupFlags.Immortal,
                        extraSettings);
                }
                else
                    UnitGenerator.AddUnitGroup(
                        briefingRoom,
                        ref mission,
                        units, Side.Ally, UnitFamily.HelicopterUtility,
                        "AircraftUncontrolledGround", "Aircraft",
                        mission.PlayerAirbase.Coordinates.CreateNearRandom(300, 500),
                        GroupFlags.Invisible | GroupFlags.Inert | GroupFlags.Immortal,
                        extraSettings);
            }
            mission.SaveStage(MissionStageName.WorldPreload);
        }

        private static void ObjectiveStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating objectives...");
            var lastObjectiveCoordinates = mission.PlayerAirbase.Coordinates;
            mission.ObjectiveGroupedWaypoints = new List<List<List<Waypoint>>>();
            var i = 0;
            foreach (var objectiveTemplate in mission.TemplateRecord.Objectives)
            {
                var (objectiveCoords, waypointGroup) = ObjectiveGenerator.GenerateObjective(
                    briefingRoom,
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

        private static void CarrierStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating mission weather...");
            var turbulenceFromWeather = Weather.GenerateWeather(briefingRoom.Database, ref mission);
            (mission.WindSpeedAtSeaLevel, mission.WindDirectionAtSeaLevel) = Weather.GenerateWind(briefingRoom, ref mission, turbulenceFromWeather);

            BriefingRoom.PrintToLog("Generating carrier groups...");
            CarrierGroup.GenerateCarrierGroup(briefingRoom, ref mission);
            mission.AverageInitialPosition = mission.PlayerAirbase.Coordinates;
            if (mission.CarrierDictionary.Count > 0) mission.AverageInitialPosition = (mission.AverageInitialPosition + mission.CarrierDictionary.First().Value.GroupInfo.Coordinates) / 2.0;

            // Generate extra flight plan info
            FlightPlan.GenerateBullseyes(ref mission);
            FlightPlan.GenerateObjectiveWPCoordinatesLua(ref mission);
            FlightPlan.GenerateAircraftPackageWaypoints(briefingRoom.Database, ref mission);
            FlightPlan.GenerateIngressAndEgressWaypoints(briefingRoom.Database, ref mission);
            FlightPlan.GenerateBullseyeWaypoint(briefingRoom.Database, ref mission);
            FrontLine.GenerateFrontLine(briefingRoom.Database, ref mission);

            foreach (var waypoint in mission.Waypoints)
            {
                mission.MapData.AddIfKeyUnused($"WAYPOINT_{waypoint.Name}", new List<double[]> { waypoint.Coordinates.ToArray() });
            }
            mission.SaveStage(MissionStageName.Carrier);
        }

        private static void PlayerFlightsStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating player flight groups...");
            foreach (var templateFlightGroup in mission.TemplateRecord.PlayerFlightGroups)
                PlayerFlightGroups.GeneratePlayerFlightGroup(briefingRoom, ref mission, templateFlightGroup);
            mission.SaveStage(MissionStageName.PlayerFlightGroups);
        }

        private static void CAPStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            CombatAirPatrols.GenerateCAP(briefingRoom, ref mission);
            mission.SaveStage(MissionStageName.CAPResponse);
        }

        private static void AirDefenseStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            AirDefense.GenerateAirDefense(briefingRoom, ref mission);
            mission.SaveStage(MissionStageName.AirDefense);
        }

        private static void MissionFeaturesStage(IBriefingRoom briefingRoom, ref DCSMission mission)
        {
            BriefingRoom.PrintToLog("Generating mission features...");
            mission.AppendValue("ScriptMissionFeatures", ""); // Just in case there's no features
            foreach (var templateFeature in mission.TemplateRecord.MissionFeatures)
                FeaturesMission.GenerateMissionFeature(briefingRoom, ref mission, templateFeature);
            mission.SaveStage(MissionStageName.MissionFeatures);
        }


        internal static DCSMission GenerateRetryable(IBriefingRoom briefingRoom, MissionTemplate template)
        {
            var templateRecord = new MissionTemplateRecord(briefingRoom.Database, template);
            var mission = Policy
                .HandleResult<DCSMission>(x => x.IsExtremeDistance(briefingRoom, template, out double distance))
                .Or<BriefingRoomException>(x =>
                {
                    briefingRoom.PrintTranslatableWarning("RecoverableError", x.Message);
                    return true;
                })
                .Retry(3)
                .Execute(() => Generate(briefingRoom, templateRecord));
            if (mission.IsExtremeDistance(briefingRoom, template, out double distance))
                briefingRoom.PrintTranslatableWarning("ExcessDistance", Math.Round(distance, 2));

            return mission;
        }
    }
}
