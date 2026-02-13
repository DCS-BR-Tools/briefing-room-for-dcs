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
using BriefingRoom4DCS.Generator.Mission;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS
{
    public sealed class BriefingRoom : IBriefingRoom
    {
        public static bool RUNNING_IN_DOCKER = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public const string REPO_URL = "https://github.com/DCS-BR-Tools/briefing-room-for-dcs";

        public const string WEBSITE_URL = "https://DCS-BR-Tools.github.io/briefing-room-for-dcs/";

        public const string DISCORD_URL = "https://discord.gg/MvdFTYxkpx";

        public const string VERSION = "0.5.~RELEASE_VERSION~";

        public const string BUILD_VERSION = "20260213-171541";

        public const string TARGETED_DCS_WORLD_VERSION = "2.9.24.19998";

        public const int MAXFILESIZE = 50000000;

        public static string DCSSaveGamePath { get; private set; } = string.Empty;


        public delegate void LogHandler(string message, LogMessageErrorLevel errorLevel);
        public string LanguageKey { get; set; } = "en";
        public IDatabase Database { get; }

        private static event LogHandler OnMessageLogged;

        public BriefingRoom(IDatabase database, LogHandler logHandler = null)
        {
           

            getSaveGamePath();
            OnMessageLogged = logHandler;
            Database = database;

        }

        public void SetLogHandler(LogHandler logHandler)
        {
            OnMessageLogged = logHandler;
        }

        public List<string> GetUnitIdsByFamily(UnitFamily family)
        {
            return Database.GetAllEntries<DBEntryJSONUnit>().Where(x => x.Families.Contains(family)).Select(x => x.ID).ToList();
        }

        public DatabaseEntryInfo[] GetDatabaseEntriesInfo(DatabaseEntryType entryType, string parameter = "")
        {
            switch (entryType)
            {
                case DatabaseEntryType.Airbase:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return Array.Empty<DatabaseEntryInfo>();
                    else // A parameter was provided, return all airbases from specified theater
                        return (from DBEntryAirbase airbase in Database.GetAllEntries<DBEntryAirbase>() where airbase.Theater == parameter.ToLower() select airbase.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.Situation:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return Array.Empty<DatabaseEntryInfo>();
                    else // A parameter was provided, return all airbases from specified theater
                        return (from DBEntrySituation situation in Database.GetAllEntries<DBEntrySituation>() where situation.Theater == parameter.ToLower() select situation.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();
                case DatabaseEntryType.ObjectiveTarget:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return (from DBEntryObjectiveTarget objectiveTarget in Database.GetAllEntries<DBEntryObjectiveTarget>() select objectiveTarget.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();
                    else
                        return (from DBEntryObjectiveTarget objectiveTarget in Database.GetAllEntries<DBEntryObjectiveTarget>() where Database.GetEntry<DBEntryObjectiveTask>(parameter).ValidUnitCategories.Contains(objectiveTarget.UnitCategory) select objectiveTarget.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();
                case DatabaseEntryType.ObjectiveTargetBehavior:
                    if (string.IsNullOrEmpty(parameter)) // No parameter, return none
                        return (from DBEntryObjectiveTargetBehavior objectiveTargetBehavior in Database.GetAllEntries<DBEntryObjectiveTargetBehavior>() select objectiveTargetBehavior.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();
                    else
                    {
                        var paramList = parameter.Split(',');
                        var taskId = paramList[0];
                        var targetId = paramList[1];
                        return (from DBEntryObjectiveTargetBehavior objectiveTargetBehavior in Database.GetAllEntries<DBEntryObjectiveTargetBehavior>() where objectiveTargetBehavior.ValidUnitCategories.Contains(Database.GetEntry<DBEntryObjectiveTarget>(targetId).UnitCategory) && !objectiveTargetBehavior.InvalidTasks.Contains(taskId) select objectiveTargetBehavior.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                    }
                case DatabaseEntryType.Coalition:
                    return (from DBEntryCoalition coalition in Database.GetAllEntries<DBEntryCoalition>() select coalition.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.DCSMod:
                    return (from DBEntryDCSMod dcsMod in Database.GetAllEntries<DBEntryDCSMod>() select dcsMod.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.MissionFeature:
                    return (from DBEntryFeatureMission missionFeature in Database.GetAllEntries<DBEntryFeatureMission>() select missionFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.OptionsMission:
                    return (from DBEntryOptionsMission missionFeature in Database.GetAllEntries<DBEntryOptionsMission>() select missionFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.ObjectiveFeature:
                    return (from DBEntryFeatureObjective objectiveFeature in Database.GetAllEntries<DBEntryFeatureObjective>() select objectiveFeature.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.ObjectivePreset:
                    return (from DBEntryObjectivePreset objectivePreset in Database.GetAllEntries<DBEntryObjectivePreset>() select objectivePreset.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.ObjectiveTask:
                    return (from DBEntryObjectiveTask objectiveTask in Database.GetAllEntries<DBEntryObjectiveTask>() select objectiveTask.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.Theater:
                    return (from DBEntryTheater theater in Database.GetAllEntries<DBEntryTheater>() select theater.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.Unit:
                    var ModList = parameter.Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => Database.GetEntry<DBEntryDCSMod>(x).Module).ToList();
                    return Database.GetAllEntries<DBEntryJSONUnit>().Where(x => DBEntryDCSMod.CORE_MODS.Contains(x.Module) || string.IsNullOrEmpty(x.Module) || ModList.Contains(x.Module)).Select(x => x.GetDBEntryInfo()).OrderBy(x => x.Category.Get(LanguageKey)).ThenBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.UnitCarrier:
                    return Database.GetAllEntries<DBEntryJSONUnit>().Where(unitCarrier => Toolbox.CARRIER_FAMILIES.Intersect(unitCarrier.Families).Any()).Select(unitCarrier => unitCarrier.GetDBEntryInfo())
                    .Concat(Database.GetAllEntries<DBEntryTemplate>().Where(template => template.Type == "FOB").Select(template => template.GetDBEntryInfo()))
                    .OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.UnitFlyableAircraft:
                    return (from DBEntryAircraft unitFlyable in Database.GetAllEntries<DBEntryJSONUnit, DBEntryAircraft>() where unitFlyable.PlayerControllable select unitFlyable.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();

                case DatabaseEntryType.WeatherPreset:
                    return (from DBEntryWeatherPreset weatherPreset in Database.GetAllEntries<DBEntryWeatherPreset>() select weatherPreset.GetDBEntryInfo()).OrderBy(x => x.Name.Get(LanguageKey)).ToArray();
            }

            return null;
        }

        public DatabaseEntryInfo? GetSingleDatabaseEntryInfo(DatabaseEntryType entryType, string id)
        {
            return GetSingleDatabaseEntryInfo(LanguageKey, entryType, id);
        }

        public DatabaseEntryInfo? GetSingleDatabaseEntryInfo(string LanguageKey, DatabaseEntryType entryType, string id)
        {
            // Database entry ID doesn't exist
            if (!GetDatabaseEntriesIDs(entryType).Contains(id)) return null;

            DatabaseEntryInfo[] databaseEntryInfos = GetDatabaseEntriesInfo(entryType);
            return
                (from DatabaseEntryInfo databaseEntryInfo in databaseEntryInfos
                 where databaseEntryInfo.ID.ToLower() == id.ToLower()
                 select databaseEntryInfo).First();
        }

        public List<Tuple<string, string>> GetAircraftLiveries(string aircraftID) =>
            Database.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftID).Liveries
            .Select(x => x.Value)
            .Aggregate(new List<Tuple<string, string>>(), (acc, x) => { acc.AddRange(x); return acc; })
            .Distinct().Order().ToList();

        public List<string> GetAircraftCallsigns(string aircraftID, Country country) => Database.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftID).CallSigns[country].Select(x => x).Distinct().Order().ToList();

        public List<Tuple<string, Decade>> GetAircraftPayloads(string aircraftID) =>
            Database.GetEntry<DBEntryJSONUnit, DBEntryAircraft>(aircraftID).Payloads.Select(x => new Tuple<string, Decade>(x.name, x.decade)).Distinct().Order().ToList();

        public List<SpawnPoint> GetTheaterSpawnPoints(string theaterID) =>
           Database.GetEntry<DBEntryTheater>(theaterID).SpawnPoints.Select(x => x.ToSpawnPoint()).ToList();

        public Tuple<List<List<double[]>>, List<List<double[]>>> GetTheaterWaterZones(string theaterID)
        {
            var theater = Database.GetEntry<DBEntryTheater>(theaterID);
            return new Tuple<List<List<double[]>>, List<List<double[]>>>(
                theater.WaterCoordinates.Select(x => x.Select(y => y.ToArray()).ToList()).ToList(),
                theater.WaterExclusionCoordinates.Select(x => x.Select(y => y.ToArray()).ToList()).ToList()
                );
        }

        public static string GetAlias(int index) => Toolbox.GetAlias(index);

        public string[] GetDatabaseEntriesIDs(DatabaseEntryType entryType, string parameter = "")
        {
            return (from DatabaseEntryInfo entryInfo in GetDatabaseEntriesInfo(entryType, parameter) select entryInfo.ID).ToArray();
        }

        public DCSMission GenerateMission(string templateFilePath)
        {
            return MissionGenerator.GenerateRetryable(this, new MissionTemplate(Database, templateFilePath));
        }

        public DCSMission GenerateMission(MissionTemplate template)
        {
            return MissionGenerator.GenerateRetryable(this, template);
        }

        public DCSCampaign GenerateCampaign(string templateFilePath)
        {
            return CampaignGenerator.Generate(this, new CampaignTemplate(this, templateFilePath));
        }

        public DCSCampaign GenerateCampaign(CampaignTemplate template)
        {
            return CampaignGenerator.Generate(this, template);
        }

        public Dictionary<string, List<double[]>> GetMapSupportingMapData(MissionTemplate template)
        {
            return DrawingMaker.GetPreviewMapData(Database, template, LanguageKey);
        }

        public Dictionary<string, List<double[]>> GetAirbasesMapData(string mapID)
        {
            return DrawingMaker.GetBasicAirbasesMapData(Database, mapID, LanguageKey);
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

        public string Translate(string key)
        {
            if (Database.Language == null)
                return key;
            return Database.Language.Translate(LanguageKey, key);
        }
        public string Translate(string key, params object[] args)
        {
            if (Database.Language == null)
                return key;
            return Database.Language.Translate(LanguageKey, key, args);
        }


        public void PrintTranslatableWarning(string key, params object[] args)
        {
            PrintToLog(Translate(key, args), LogMessageErrorLevel.Warning);
        }


        public static void PrintToLog(string message, LogMessageErrorLevel errorLevel = LogMessageErrorLevel.Info)
        {
            OnMessageLogged?.Invoke(message, errorLevel);
            if (errorLevel == LogMessageErrorLevel.Warning || errorLevel == LogMessageErrorLevel.Error || System.Diagnostics.Debugger.IsAttached)
                Console.WriteLine($"{errorLevel}: {message}");
        }

        public void ReloadDatabase()
        {
            Database.Reset();
        }

        private void getSaveGamePath()
        {
            if (!string.IsNullOrEmpty(DCSSaveGamePath))
                return;
            var userPath = Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            if (Directory.Exists(Path.Join(userPath, "Saved Games", "DCS.openbeta")))
            {
                DCSSaveGamePath = Path.Join(userPath, "Saved Games", "DCS.openbeta");
                return;
            }
            if (Directory.Exists(Path.Join(userPath, "Saved Games", "DCS")))
            {
                DCSSaveGamePath = Path.Join(userPath, "Saved Games", "DCS");
                return;
            }

        }

        public bool SetDCSSaveGamePath(string path)
        {
            if (
                Directory.Exists(path) &&
                Directory.Exists(Path.Join(path, "MissionEditor", "UnitPayloads")) &&
                Directory.Exists(Path.Join(path, "Liveries")))
            {
                DCSSaveGamePath = path;
                Database.Reset();
                return true;
            }
            return false;
        }
    }
}