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
using BriefingRoom4DCS.Generator.Mission;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class CampaignGenerator
    {
        private static readonly string CAMPAIGN_LUA_TEMPLATE = Path.Combine(BRPaths.INCLUDE_LUA, "Campaign", "Campaign.lua");
        private static readonly string CAMPAIGN_STAGE_LUA_TEMPLATE = Path.Combine(BRPaths.INCLUDE_LUA, "Campaign", "CampaignStage.lua");

        internal static DCSCampaign Generate(IBriefingRoom briefingRoom, CampaignTemplate campaignTemplate)
        {
            DCSCampaign campaign = new()
            {
                Name = GeneratorTools.GenerateCampaignName(briefingRoom.Database, briefingRoom.LanguageKey, campaignTemplate.BriefingCampaignName)
            };

            DateTime date = GenerateCampaignDate(campaignTemplate);

            string previousSituationId = "";
            Coordinates previousObjectiveCenterCoords = new();
            string previousPlayerAirbaseId = "";
            string previousDestinationAirbaseId = "";

            var i = 0;
            var failedTries = 0;
            do
            {
                if (i > 0) date = IncrementDate(date);

                var template = CreateMissionTemplate(
                    briefingRoom.Database,
                    briefingRoom.LanguageKey,
                    campaignTemplate,
                    i,
                    (int)campaignTemplate.MissionsObjectiveCount,
                    previousSituationId,
                    failedTries > 1 ? new() : previousObjectiveCenterCoords,
                    failedTries > 2 ? "" : previousPlayerAirbaseId,
                    failedTries > 2 ? "" : previousDestinationAirbaseId
                    );

                try
                {
                    var mission = MissionGenerator.GenerateRetryable(briefingRoom, template);
                    mission.SetValue("DateDay", date.Day);
                    mission.SetValue("DateMonth", date.Month);
                    mission.SetValue("DateYear", date.Year);
                    mission.SetValue("BriefingDate", $"{date.Day:00}/{date.Month:00}/{date.Year:0000}");

                    campaign.AddMission(mission);

                    previousSituationId = mission.GetValue("BriefingSituationId");
                    previousObjectiveCenterCoords = new Coordinates(double.Parse(mission.GetValue("MissionCenterX"), CultureInfo.InvariantCulture), double.Parse(mission.GetValue("MissionCenterY"), CultureInfo.InvariantCulture));
                    previousPlayerAirbaseId = mission.GetValue("PlayerAirbaseId");
                    previousDestinationAirbaseId = mission.GetValue("PlayerAirbaseDestinationId");
                    i++;
                    failedTries = 0;
                }
                catch (BriefingRoomRawException err)
                {
                    if (failedTries >= 3)
                        throw new BriefingRoomException(briefingRoom.Database, briefingRoom.LanguageKey, "FailedToGenerateCampaignMission", err, i + 1);
                    failedTries++;
                    continue;
                }
            } while (campaign.Missions.Count < campaignTemplate.MissionsCount);

            if (campaign.MissionCount < 1) // No missions generated, something went very wrong.
                throw new BriefingRoomException(briefingRoom.Database, briefingRoom.LanguageKey, "CampaignNoValidMissions");

            campaign.CMPFile = GetCMPFile(campaignTemplate, campaign);

            return campaign;
        }

        private static DateTime GenerateCampaignDate(CampaignTemplate campaignTemplate)
        {
            int year = Toolbox.GetRandomYearFromDecade(campaignTemplate.ContextDecade);
            Month month = Toolbox.RandomFrom(Toolbox.GetEnumValues<Month>());
            int day = Toolbox.RandomMinMax(1, GeneratorTools.GetDaysPerMonth(month, year));

            DateTime date = new(year, (int)month + 1, day);
            return date;
        }

        private static string GetCMPFile(CampaignTemplate campaignTemplate, DCSCampaign campaign)
        {
            string lua = File.ReadAllText(CAMPAIGN_LUA_TEMPLATE);
            GeneratorTools.ReplaceKey(ref lua, "Name", campaign.Name);
            GeneratorTools.ReplaceKey(ref lua, "Description",
                $"This is a {campaignTemplate.ContextCoalitionBlue} vs {campaignTemplate.ContextCoalitionRed} randomly-generated campaign created by an early version of the campaign generator of BriefingRoom, a mission generator for DCS World ({BriefingRoom.WEBSITE_URL}).");
            GeneratorTools.ReplaceKey(ref lua, "Units", "");

            string stagesLua = "";
            var i = 1;
            foreach (var mission in campaign.Missions)
            {
                string nextStageLua = File.ReadAllText(CAMPAIGN_STAGE_LUA_TEMPLATE);
                GeneratorTools.ReplaceKey(ref nextStageLua, "Index", i);
                GeneratorTools.ReplaceKey(ref nextStageLua, "Name", $"Stage {i}");
                GeneratorTools.ReplaceKey(ref nextStageLua, "Description", $"");
                GeneratorTools.ReplaceKey(ref nextStageLua, "File", $"{i}_{mission.Briefing.Name}.miz");

                stagesLua += nextStageLua + "\r\n";
                i++;
            }
            GeneratorTools.ReplaceKey(ref lua, "Stages", stagesLua);

            return lua.Replace("\r\n", "\n");
        }

        private static MissionTemplate CreateMissionTemplate(
            IDatabase database,
            string langKey,
            CampaignTemplate campaignTemplate,
            int missionIndex, int missionCount,
            string previousSituationId, Coordinates previousObjectiveCenterCoords, string previousPlayerAirbaseId, string previousDestinationAirbaseId)
        {
            string weatherPreset = GetWeatherForMission(database, campaignTemplate.EnvironmentBadWeatherChance);
            MissionTemplate template = new(database)
            {
                BriefingMissionName = "",
                BriefingMissionDescription = "",

                ContextCoalitionBlue = campaignTemplate.ContextCoalitionBlue,
                ContextCoalitionRed = campaignTemplate.ContextCoalitionRed,
                ContextDecade = campaignTemplate.ContextDecade,
                ContextPlayerCoalition = campaignTemplate.ContextPlayerCoalition,
                ContextTheater = campaignTemplate.ContextTheater,
                ContextSituation = campaignTemplate.ContextSituation,

                EnvironmentSeason = Season.Random,
                EnvironmentTimeOfDay = GetTimeOfDayForMission(campaignTemplate.EnvironmentNightMissionChance),
                EnvironmentWeatherPreset = weatherPreset,
                EnvironmentWind = GetWindForMission(database, campaignTemplate.EnvironmentBadWeatherChance, weatherPreset),

                FlightPlanObjectiveDistanceMax = campaignTemplate.FlightPlanObjectiveDistanceMax,
                FlightPlanObjectiveDistanceMin = campaignTemplate.FlightPlanObjectiveDistanceMin,
                FlightPlanTheaterStartingAirbase = campaignTemplate.FlightPlanTheaterStartingAirbase,

                MissionFeatures = campaignTemplate.MissionFeatures.ToList(),

                Mods = campaignTemplate.Mods.ToList(),

                Objectives = new(),

                OptionsFogOfWar = campaignTemplate.OptionsFogOfWar,
                OptionsMission = campaignTemplate.OptionsMission.ToList(),
                OptionsRealism = campaignTemplate.OptionsRealism.ToList(),
                OptionsUnitBanList = campaignTemplate.OptionsUnitBanList.ToList(),

                PlayerFlightGroups = campaignTemplate.PlayerFlightGroups,

                SituationEnemySkill = GetPowerLevel(campaignTemplate.SituationEnemySkill, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount),
                SituationEnemyAirDefense = GetPowerLevel(campaignTemplate.SituationEnemyAirDefense, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount),
                SituationEnemyAirForce = GetPowerLevel(campaignTemplate.SituationEnemyAirForce, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount),

                SituationFriendlySkill = GetPowerLevel(campaignTemplate.SituationFriendlySkill, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount, true),
                SituationFriendlyAirDefense = GetPowerLevel(campaignTemplate.SituationFriendlyAirDefense, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount, true),
                SituationFriendlyAirForce = GetPowerLevel(campaignTemplate.SituationFriendlyAirForce, campaignTemplate.MissionsDifficultyVariation, missionIndex, missionCount, true),

                CombinedArmsCommanderBlue = campaignTemplate.CombinedArmsCommanderBlue,
                CombinedArmsCommanderRed = campaignTemplate.CombinedArmsCommanderRed,
                CombinedArmsJTACBlue = campaignTemplate.CombinedArmsJTACBlue,
                CombinedArmsJTACRed = campaignTemplate.CombinedArmsJTACRed,
            };

            if (!String.IsNullOrEmpty(previousPlayerAirbaseId))
            {
                var isNone = previousSituationId == "None";
                DBEntrySituation nextSituationDB;
                if (!isNone)
                {
                    var situationOptions = new List<string> { previousSituationId };
                    var previousSituationDB = database.GetEntry<DBEntrySituation>(previousSituationId);
                    if (!campaignTemplate.StaticSituation)
                        situationOptions.AddRange(previousSituationDB.RelatedSituations);
                    template.ContextSituation = Toolbox.RandomFrom(situationOptions.ToArray());
                    nextSituationDB = database.GetEntry<DBEntrySituation>(template.ContextSituation);
                }
                else
                {
                    template.ContextSituation = "None";
                    var searchTheater = template.ContextTheater.ToLower();
                    var randomSituations = database.GetAllEntries<DBEntrySituation>().Where(x => x.Theater == searchTheater).ToList();
                    nextSituationDB = Toolbox.RandomFrom(randomSituations);
                }

                var airbases = nextSituationDB.GetAirbases(database, template.OptionsMission.Contains("InvertCountriesCoalitions"));
                var previousPlayerAirbase = airbases.First(x => x.ID == previousPlayerAirbaseId);
                if (campaignTemplate.MissionsPersistentAirbases)
                {
                    template.FlightPlanTheaterStartingAirbase = previousPlayerAirbaseId;
                    var airbaseOptions = airbases.Where(x =>
                        (x.Coalition == template.ContextPlayerCoalition || isNone)
                        && x.Coordinates.GetDistanceFrom(previousPlayerAirbase.Coordinates) < (GetAirbaseVariationDistance(campaignTemplate.MissionsAirbaseVariationDistance) * Toolbox.NM_TO_METERS)
                     ).ToList();
                    if (airbaseOptions.Count > 0)
                        template.FlightPlanTheaterDestinationAirbase = Toolbox.RandomFrom(airbaseOptions).ID;
                }
                else
                {
                    var airbaseOptions = airbases.Where(x =>
                    (x.Coalition == template.ContextPlayerCoalition || isNone) &&
                    x.Coordinates.GetDistanceFrom(previousPlayerAirbase.Coordinates) < (GetAirbaseVariationDistance(campaignTemplate.MissionsAirbaseVariationDistance) * Toolbox.NM_TO_METERS)).ToList();
                    if (airbaseOptions.Count > 0)
                        template.FlightPlanTheaterStartingAirbase = Toolbox.RandomFrom(airbaseOptions).ID;
                }
            }


            int objectiveCount = GetObjectiveCountForMission(campaignTemplate.MissionsObjectiveCount);
            var i = 0;
            do
            {
                var obj = new MissionTemplateObjective(database, Toolbox.RandomFrom(campaignTemplate.MissionsObjectives), campaignTemplate.MissionTargetCount);
                i++;
                while (i < Toolbox.RandomInt(i, Math.Min(i + 5, objectiveCount)))
                {
                    obj.SubTasks.Add(new MissionTemplateSubTask(database, Toolbox.RandomFrom(campaignTemplate.MissionsObjectives), campaignTemplate.MissionTargetCount));
                    i++;
                }
                template.Objectives.Add(obj);
            } while (i < objectiveCount);

            if (campaignTemplate.MissionsProgression != AmountNR.None)
            {
                var progression = GetMissionProgressionChance(campaignTemplate.MissionsProgression);
                if (progression > 0)
                    ApplyProgression(database, langKey, progression, ref template);
            }

            if (!String.IsNullOrEmpty(previousPlayerAirbaseId))
                template.Objectives[0].CoordinateHint_ = previousObjectiveCenterCoords.CreateNearRandom(5 * Toolbox.NM_TO_METERS, GetObjectiveVariationDistance(campaignTemplate.MissionsObjectiveVariationDistance) * Toolbox.NM_TO_METERS);

            return template;
        }

        private static void ApplyProgression(IDatabase database, string langKey, int progressionChance, ref MissionTemplate template)
        {
            var trys = 1;
            while (trys < 10)
            {
                var taskIndex = 0;
                List<int> indexesSoFar = new();
                foreach (var objective in template.Objectives)
                {
                    if (taskIndex > 0 && Toolbox.RandomInt(100) < progressionChance)
                        ApplyProgressionToTask(objective, indexesSoFar);
                    indexesSoFar.Add(taskIndex);
                    taskIndex++;
                    foreach (var subTask in objective.SubTasks)
                    {
                        if (Toolbox.RandomInt(100) < progressionChance)
                            ApplyProgressionToTask(subTask, indexesSoFar);
                        indexesSoFar.Add(taskIndex);
                        taskIndex++;
                    }
                }
                try
                {
                    Toolbox.CheckObjectiveProgressionLogic(database, new MissionTemplateRecord(database, template), langKey);
                    break;
                }
                catch (BriefingRoomRawException)
                {
                    trys++;
                }
            }
        }

        private static void ApplyProgressionToTask(MissionTemplateSubTask task, List<int> indexesSoFar)
        {
            task.ProgressionDependentIsAny = Toolbox.RandomInt(100) < 50;
            for (var i = 0; i < Toolbox.RandomMinMax(0, 3); i++)
                task.ProgressionDependentTasks.Add(Toolbox.RandomFrom(indexesSoFar));
            task.ProgressionDependentTasks = task.ProgressionDependentTasks.Distinct().ToList();
            if (!task.ProgressionActivation) return;

            foreach (var val in Enum.GetValues<ObjectiveProgressionOption>())
            {
                if (Toolbox.RandomInt(100) < 50)
                    task.ProgressionOptions.Add(val);
            }

            if (Toolbox.RandomInt(100) < 50)
                task.Options.Add(ObjectiveOption.NoAircraftWaypoint);
        }

        private static int GetMissionProgressionChance(AmountNR missionsProgression)
        {
            return missionsProgression switch
            {
                AmountNR.VeryLow => 10,
                AmountNR.Low => 25,
                AmountNR.High => 75,
                AmountNR.VeryHigh => 100,
                AmountNR.None => 0,
                AmountNR.Random => Toolbox.RandomInt(100),
                _ => 50,// case Amount.Average
            };
        }

        private static double GetObjectiveVariationDistance(Amount objectiveVariationDistance)
        {
            return objectiveVariationDistance switch
            {
                Amount.VeryLow => 10,
                Amount.Low => 25,
                Amount.High => 75,
                Amount.VeryHigh => 100,
                _ => (double)50,// case Amount.Average
            };
        }

        private static double GetAirbaseVariationDistance(AmountN airbaseVariationDistance)
        {
            return airbaseVariationDistance switch
            {
                AmountN.None => 0,
                AmountN.VeryLow => 10,
                AmountN.Low => 25,
                AmountN.High => 75,
                AmountN.VeryHigh => 100,
                _ => (double)50,// case Amount.Average
            };
        }

        private static Wind GetWindForMission(IDatabase database, Amount badWeatherChance, string weatherPreset)
        {
            // Pick a max wind force
            var maxWind = badWeatherChance switch
            {
                Amount.VeryLow => Wind.Calm,
                Amount.Low => Wind.LightBreeze,
                Amount.High => Wind.ModerateBreeze,
                Amount.VeryHigh => Wind.StrongBreeze,
                _ => Wind.ModerateBreeze,
            };

            // Select a random wind force
            Wind wind = (Wind)Toolbox.RandomMinMax((int)Wind.Calm, (int)maxWind);

            // Makes the wind stronger if the weather preset is classified as "bad weather"
            if (database.GetEntry<DBEntryWeatherPreset>(weatherPreset).BadWeather)
                wind += Toolbox.RandomMinMax(0, 2);

            return (Wind)Toolbox.Clamp((int)wind, (int)Wind.Calm, (int)Wind.Storm);
        }

        private static AmountR GetPowerLevel(AmountR amount, CampaignDifficultyVariation variation, int missionIndex, int missionsCount, bool reverseVariation = false)
        {
            if (amount == AmountR.Random) return AmountR.Random;
            if (variation == CampaignDifficultyVariation.Steady) return amount;

            double campaignProgress = missionIndex / (double)(Math.Max(2, missionsCount) - 1.0);

            double amountOffset = 0;
            switch (variation)
            {
                case CampaignDifficultyVariation.ConsiderablyEasier: amountOffset = -3.5; break;
                case CampaignDifficultyVariation.MuchEasier: amountOffset = -2.25; break;
                case CampaignDifficultyVariation.SomewhatEasier: amountOffset = -1.5; break;
                case CampaignDifficultyVariation.SomewhatHarder: amountOffset = 1.5; break;
                case CampaignDifficultyVariation.MuchHarder: amountOffset = 2.25; break;
                case CampaignDifficultyVariation.ConsiderablyHarder: amountOffset = 3.5; break;
            }
            double amountDouble = (double)amount + amountOffset * campaignProgress;
            if (reverseVariation) amountDouble = -amountDouble;

            return (AmountR)Toolbox.Clamp((int)amountDouble, (int)AmountR.VeryLow, (int)AmountR.VeryHigh);
        }

        private static AmountNR GetPowerLevel(AmountNR amount, CampaignDifficultyVariation variation, int missionIndex, int missionsCount, bool reverseVariation = false)
        {
            if (amount == AmountNR.None || amount == AmountNR.Random) return amount;
            if (variation == CampaignDifficultyVariation.Steady) return amount;

            double campaignProgress = missionIndex / (double)(Math.Max(2, missionsCount) - 1.0);

            double amountOffset = 0;
            switch (variation)
            {
                case CampaignDifficultyVariation.ConsiderablyEasier: amountOffset = -3.5; break;
                case CampaignDifficultyVariation.MuchEasier: amountOffset = -2.25; break;
                case CampaignDifficultyVariation.SomewhatEasier: amountOffset = -1.5; break;
                case CampaignDifficultyVariation.SomewhatHarder: amountOffset = 1.5; break;
                case CampaignDifficultyVariation.MuchHarder: amountOffset = 2.25; break;
                case CampaignDifficultyVariation.ConsiderablyHarder: amountOffset = 3.5; break;
            }
            double amountDouble = (double)amount + amountOffset * campaignProgress;
            if (reverseVariation) amountDouble = -amountDouble;

            return (AmountNR)Toolbox.Clamp((int)amountDouble, (int)AmountNR.VeryLow, (int)AmountNR.VeryHigh);
        }

        private static string GetWeatherForMission(IDatabase database, Amount badWeatherChance)
        {
            // Chance to have bad weather
            var chance = badWeatherChance switch
            {
                Amount.VeryLow => 0,
                Amount.Low => 10,
                Amount.High => 40,
                Amount.VeryHigh => 60,
                _ => 25,
            };

            // Pick a random weather preset matching the good/bad weather chance
            string weather =
                (from DBEntryWeatherPreset weatherDB
                 in database.GetAllEntries<DBEntryWeatherPreset>()
                 where weatherDB.BadWeather == (Toolbox.RandomInt(100) < chance)
                 select weatherDB.ID).OrderBy(x => Toolbox.RandomInt()).FirstOrDefault();

            // Just to make sure weather ID is not null
            if (weather == null)
                return Toolbox.RandomFrom(database.GetAllEntriesIDs<DBEntryWeatherPreset>());

            return weather;
        }

        private static TimeOfDay GetTimeOfDayForMission(Amount nightMissionChance)
        {
            var chance = nightMissionChance switch
            {
                Amount.VeryLow => 0,
                Amount.Low => 10,
                Amount.High => 50,
                Amount.VeryHigh => 80,
                _ => 25,
            };
            if (Toolbox.RandomInt(100) < chance)
                return TimeOfDay.Night;
            else
                return TimeOfDay.RandomDaytime;
        }

        private static int GetObjectiveCountForMission(Amount amount)
        {
            return amount switch
            {
                Amount.VeryLow => 1,
                Amount.Low => Toolbox.RandomFrom(1, 1, 2),
                Amount.High => Toolbox.RandomFrom(2, 3, 3, 4),
                Amount.VeryHigh => Toolbox.RandomFrom(3, 4, 4, 4, 5),
                // case Amount.Average:
                _ => Toolbox.RandomFrom(1, 2, 2, 2, 3),
            };
        }

        private static DateTime IncrementDate(DateTime dateTime) => dateTime.AddDays(Toolbox.RandomMinMax(1, 3));
    }
}
