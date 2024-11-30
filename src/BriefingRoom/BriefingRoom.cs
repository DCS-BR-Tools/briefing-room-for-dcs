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
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Generator;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS
{
    public sealed class BriefingRoom
    {
        public static string TARGETED_DCS_WORLD_VERSION { get; private set; }
        public static Dictionary<string, string> AvailableLanguagesMap { get; private set; }
        public static bool RUNNING_IN_DOCKER = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static DatabaseLanguage LanguageDB { get; private set; }

        public const string REPO_URL = "https://github.com/akaAgar/briefing-room-for-dcs";

        public const string WEBSITE_URL = "https://akaagar.github.io/briefing-room-for-dcs/";

        public const string DISCORD_URL = "https://discord.gg/MvdFTYxkpx";

        public const string VERSION = "0.5.~RELEASE_VERSION~";

        public const string BUILD_VERSION = "~BUILD_VERSION~";

        public const int MAXFILESIZE = 50000000;



        public delegate void LogHandler(string message, LogMessageErrorLevel errorLevel);
        public string LanguageKey { get; set; } = "en";

        private static event LogHandler OnMessageLogged;

        public BriefingRoom(LogHandler logHandler = null, bool nukeDB = false)
        {
            INIFile ini = new(Path.Combine(BRPaths.DATABASE, "Common.ini"));
            TARGETED_DCS_WORLD_VERSION = ini.GetValue("Versions", "DCSVersion", "2.9.2");


            AvailableLanguagesMap = new Dictionary<string, string> { { "en", "English" } };
            foreach (var key in ini.GetKeysInSection("Languages"))
                AvailableLanguagesMap.AddIfKeyUnused(key, ini.GetValue<string>("Languages", key));


            OnMessageLogged = logHandler;
            if (nukeDB) 
            {
                Database.Reset();
            } else {
                Database.Instance.Initialize();
            }
            LanguageDB = Database.Instance.Language;
        }

        public void SetLogHandler(LogHandler logHandler)
        {
            OnMessageLogged = logHandler;
        }

        public List<string> GetUnitIdsByFamily(UnitFamily family)
        {
            return Database.Instance.GetAllEntries<DBEntryJSONUnit>().Where(x => x.Families.Contains(family)).Select(x => x.ID).ToList();
        }

        public DatabaseEntryInfo[] GetDatabaseEntriesInfo(DatabaseEntryType entryType, string parameter = "") {
            return GetDatabaseEntriesInfo(LanguageKey, entryType, parameter);
        }

        public static DatabaseEntryInfo[] GetDatabaseEntriesInfo(string langKey, DatabaseEntryType entryType, string parameter = "")
        {
            switch (entryType)
            {
                case DatabaseEntryType.Airbase:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return Array.Empty<DatabaseEntryInfo>();
                    else // A parameter was provided, return all airbases from specified theater
                        return (from DBEntryAirbase airbase in Database.Instance.GetAllEntries<DBEntryAirbase>() where airbase.Theater == parameter.ToLower() select airbase.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.Situation:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return Array.Empty<DatabaseEntryInfo>();
                    else // A parameter was provided, return all airbases from specified theater
                        return (from DBEntrySituation situation in Database.Instance.GetAllEntries<DBEntrySituation>() where situation.Theater == parameter.ToLower() select situation.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();
                case DatabaseEntryType.ObjectiveTarget:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return (from DBEntryObjectiveTarget objectiveTarget in Database.Instance.GetAllEntries<DBEntryObjectiveTarget>() select objectiveTarget.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();
                    else
                        return (from DBEntryObjectiveTarget objectiveTarget in Database.Instance.GetAllEntries<DBEntryObjectiveTarget>() where Database.Instance.GetEntry<DBEntryObjectiveTask>(parameter).ValidUnitCategories.Contains(objectiveTarget.UnitCategory) select objectiveTarget.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();
                case DatabaseEntryType.ObjectiveTargetBehavior:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return (from DBEntryObjectiveTargetBehavior objectiveTargetBehavior in Database.Instance.GetAllEntries<DBEntryObjectiveTargetBehavior>() select objectiveTargetBehavior.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();
                    else
                    {
                        var paramList = parameter.Split(',');
                        var taskId = paramList[0];
                        var targetId = paramList[1];
                        return (from DBEntryObjectiveTargetBehavior objectiveTargetBehavior in Database.Instance.GetAllEntries<DBEntryObjectiveTargetBehavior>() where objectiveTargetBehavior.ValidUnitCategories.Contains(Database.Instance.GetEntry<DBEntryObjectiveTarget>(targetId).UnitCategory) && !objectiveTargetBehavior.InvalidTasks.Contains(taskId) select objectiveTargetBehavior.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                    }
                case DatabaseEntryType.Coalition:
                    return (from DBEntryCoalition coalition in Database.Instance.GetAllEntries<DBEntryCoalition>() select coalition.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.DCSMod:
                    return (from DBEntryDCSMod dcsMod in Database.Instance.GetAllEntries<DBEntryDCSMod>() select dcsMod.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.MissionFeature:
                    return (from DBEntryFeatureMission missionFeature in Database.Instance.GetAllEntries<DBEntryFeatureMission>() select missionFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.OptionsMission:
                    return (from DBEntryOptionsMission missionFeature in Database.Instance.GetAllEntries<DBEntryOptionsMission>() select missionFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.ObjectiveFeature:
                    return (from DBEntryFeatureObjective objectiveFeature in Database.Instance.GetAllEntries<DBEntryFeatureObjective>() select objectiveFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.ObjectivePreset:
                    return (from DBEntryObjectivePreset objectivePreset in Database.Instance.GetAllEntries<DBEntryObjectivePreset>() select objectivePreset.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.ObjectiveTask:
                    return (from DBEntryObjectiveTask objectiveTask in Database.Instance.GetAllEntries<DBEntryObjectiveTask>() select objectiveTask.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.Theater:
                    return (from DBEntryTheater theater in Database.Instance.GetAllEntries<DBEntryTheater>() select theater.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.Unit:
                    var ModList = parameter.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => Database.Instance.GetEntry<DBEntryDCSMod>(x).Module).ToList();
                    return Database.Instance.GetAllEntries<DBEntryJSONUnit>().Where(x => DBEntryDCSMod.CORE_MODS.Contains(x.Module) || string.IsNullOrEmpty(x.Module) || ModList.Contains(x.Module)).Select(x => x.GetDBEntryInfo()).OrderBy(x => x.Category.Get(langKey)).ThenBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.UnitCarrier:
                    return Database.Instance.GetAllEntries<DBEntryJSONUnit>().Where(unitCarrier => Toolbox.CARRIER_FAMILIES.Intersect(unitCarrier.Families).Any()).Select(unitCarrier => unitCarrier.GetDBEntryInfo())
                    .Concat(Database.Instance.GetAllEntries<DBEntryTemplate>().Where(template => template.Type == "FOB").Select(template => template.GetDBEntryInfo()))
                    .OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.UnitFlyableAircraft:
                    return (from DBEntryAircraft unitFlyable in Database.Instance.GetAllEntries<DBEntryJSONUnit, DBEntryAircraft>() where unitFlyable.PlayerControllable select unitFlyable.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();

                case DatabaseEntryType.WeatherPreset:
                    return (from DBEntryWeatherPreset weatherPreset in Database.Instance.GetAllEntries<DBEntryWeatherPreset>() select weatherPreset.GetDBEntryInfo()).OrderBy(x => x.Name.Get(langKey)).ToArray();
            }

            return null;
        }

         public DatabaseEntryInfo? GetSingleDatabaseEntryInfo(DatabaseEntryType entryType, string id) {
            return GetSingleDatabaseEntryInfo(LanguageKey, entryType, id);
         }

        public static DatabaseEntryInfo? GetSingleDatabaseEntryInfo(string langKey, DatabaseEntryType entryType, string id)
        {
            // Database entry ID doesn't exist
            if (!GetDatabaseEntriesIDs(entryType).Contains(id)) return null;

            DatabaseEntryInfo[] databaseEntryInfos = GetDatabaseEntriesInfo(langKey, entryType);
            return
                (from DatabaseEntryInfo databaseEntryInfo in databaseEntryInfos
                 where databaseEntryInfo.ID.ToLower() == id.ToLower()
                 select databaseEntryInfo).First();
        }

        public static List<string> GetAircraftLiveries(string aircraftID) =>
            Database.Instance.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftID).Liveries
            .Select(x => x.Value)
            .Aggregate(new List<string>(), (acc, x) => { acc.AddRange(x); return acc; })
            .Distinct().Order().ToList();

        public static List<string> GetAircraftCallsigns(string aircraft) => new();


        public static List<string> GetAircraftPayloads(string aircraftID) =>
            Database.Instance.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftID).Payloads.Select(x => x.name).Distinct().Order().ToList();

         public static List<SpawnPoint> GetTheaterSpawnPoints(string theaterID) =>
            Database.Instance.GetEntry<DBEntryTheater>(theaterID).SpawnPoints.Select(x => x.ToSpawnPoint()).ToList();


        public static string GetAlias(int index) => Toolbox.GetAlias(index);

        public static string[] GetDatabaseEntriesIDs(DatabaseEntryType entryType, string parameter = "")
        {
            return (from DatabaseEntryInfo entryInfo in GetDatabaseEntriesInfo("en", entryType, parameter) select entryInfo.ID).ToArray();
        }

        public DCSMission GenerateMission(string templateFilePath)
        {
            return MissionGenerator.GenerateRetryable(LanguageKey, new MissionTemplate(templateFilePath));
        }

        public DCSMission GenerateMission(MissionTemplate template)
        {
            return MissionGenerator.GenerateRetryable(LanguageKey, template);
        }

        public DCSCampaign GenerateCampaign(string templateFilePath)
        {
            return CampaignGenerator.Generate(LanguageKey, new CampaignTemplate(templateFilePath));
        }

        public  DCSCampaign GenerateCampaign(CampaignTemplate template)
        {
            return CampaignGenerator.Generate(LanguageKey, template);
        }

        public static string GetBriefingRoomRootPath() { return BRPaths.ROOT; }

        public static string GetBriefingRoomMarkdownPath() { return BRPaths.INCLUDE_MARKDOWN; }

        public static string GetDCSMissionPath()
        {
            string[] possibleDCSPaths = new string[] { "DCS.earlyaccess", "DCS.openbeta", "DCS" };

            for (int i = 0; i < possibleDCSPaths.Length; i++)
            {
                string dcsPath = Path.Combine(Toolbox.PATH_USER, "Saved Games", possibleDCSPaths[i], "Missions");
                if (Directory.Exists(dcsPath)) return dcsPath;
            }

            return Toolbox.PATH_USER_DOCS;
        }

        public static string GetDCSCampaignPath()
        {
            string campaignPath = Path.Combine(GetDCSMissionPath(), "Campaigns", "multilang");

            if (Directory.Exists(campaignPath)) return campaignPath;

            return Toolbox.PATH_USER_DOCS;
        }

        public string Translate(string key){
                if(LanguageDB == null)
                    return key;
                return LanguageDB.Translate(LanguageKey, key);
            }
        public string Translate(string key, params object[] args) {
            if(LanguageDB == null)
                    return key;
            var template = LanguageDB.Translate(LanguageKey, key);
            return string.Format(template, args);
        }

        public static string Translate(string langKey, string key){
                if(LanguageDB == null)
                    return key;
                return LanguageDB.Translate(langKey, key);
            }
        public static string Translate(string langKey, string key, params object[] args) {
            if(LanguageDB == null)
                    return key;
            var template = LanguageDB.Translate(langKey, key);
            return string.Format(template, args);
        }

        internal static void PrintTranslatableWarning(string langKey,string key, params object[] args)
        {
           PrintToLog(Translate(langKey, key, args), LogMessageErrorLevel.Warning);
        }


        internal static void PrintToLog(string message, LogMessageErrorLevel errorLevel = LogMessageErrorLevel.Info)
        {
            OnMessageLogged?.Invoke(message, errorLevel);
            if (errorLevel == LogMessageErrorLevel.Warning || errorLevel == LogMessageErrorLevel.Error || System.Diagnostics.Debugger.IsAttached)
                Console.WriteLine($"{errorLevel}: {message}");
        }

        public static void ReloadDatabase()
        {
            Database.Reset();
        }
    }
}
