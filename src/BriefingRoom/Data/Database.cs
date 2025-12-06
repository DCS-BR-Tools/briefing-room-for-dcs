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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace BriefingRoom4DCS.Data
{
    public class Database: IDatabase
    {

        public DatabaseCommon Common { get; set; }
        public DatabaseLanguage Language { get; set; }

        private readonly Dictionary<Type, Dictionary<string, DBEntry>> DBEntries;
        private readonly Dictionary<Type, Tuple<string, string>> UnloadedEntries = new();

        private bool Initialized = false;
        private bool LoadingInProgress = false;

        public Database()
        {
            Language = new DatabaseLanguage();
            Common = new DatabaseCommon();
            DBEntries = new Dictionary<Type, Dictionary<string, DBEntry>>();
            this.Initialize();
        }

        public void Reset()
        {
            this.Initialize();
        }

        internal void Initialize()
        {
            if (Initialized) return;
            BriefingRoom.PrintToLog("---------------> Initializing database...", LogMessageErrorLevel.Warning);
            if (LoadingInProgress)
            {
                BriefingRoom.PrintToLog("Database loading already in progress, cancelling...", LogMessageErrorLevel.Warning);
                return;
            }
            LoadingInProgress = true;
            Language.Load();
            Common.Load(Language);

            // Load entries into the database
            DBEntries.Clear();
            PrepLoadEntries<DBEntryBriefingDescription>("LoadEntries", "BriefingDescriptions");
            PrepLoadEntries<DBEntryFeatureMission>("LoadEntries", "MissionFeatures");
            PrepLoadEntries<DBEntryOptionsMission>("LoadEntries", "OptionsMission");
            PrepLoadEntries<DBEntryFeatureObjective>("LoadEntries", "ObjectiveFeatures");
            PrepLoadEntries<DBEntryObjectiveTarget>("LoadEntries", "ObjectiveTargets");
            PrepLoadEntries<DBEntryObjectiveTask>("LoadEntries", "ObjectiveTasks");
            PrepLoadEntries<DBEntryObjectiveTargetBehavior>("LoadEntries", "ObjectiveTargetsBehaviors");
            PrepLoadEntries<DBEntryObjectivePreset>("LoadEntries", "ObjectivePresets"); // Must be loaded after other DBEntryObjective*, as it depends on them
            PrepLoadEntries<DBEntryTheater>("LoadEntries", "Theaters");
            PrepLoadEntries<DBEntryAirbase>("LoadJSONEntries", "TheatersAirbases");
            PrepLoadEntries<DBEntrySituation>("LoadJSONFolderEntries", "Situations");
            PrepLoadEntries<DBEntryDCSMod>("LoadEntries", "DCSMods");
            LoadJSONEntries<DBEntryWeaponByDecade>("WeaponsByDate");
            LoadJSONEntries<DBEntryWeaponByDecade>("WeaponsByDate_custom");
            LoadJSONModEntries<DBEntryWeaponByDecade>("WeaponsByDate", false);
            LoadJSONEntries<DBEntryCar>("UnitCars", true);
            LoadJSONModEntries<DBEntryCar>("UnitCars", true);
            LoadJSONEntries<DBEntryAircraft>("UnitPlanes", true);
            LoadJSONModEntries<DBEntryAircraft>("UnitPlanes", true);
            LoadJSONEntries<DBEntryAircraft>("UnitHelicopters", true);
            LoadJSONModEntries<DBEntryAircraft>("UnitHelicopters", true);
            LoadJSONEntries<DBEntryShip>("UnitShips", true);
            LoadJSONModEntries<DBEntryShip>("UnitShips", true);
            LoadJSONEntries<DBEntryStatic>("UnitWarehouses", true);
            LoadJSONEntries<DBEntryStatic>("UnitFortifications", true);
            LoadJSONEntries<DBEntryStatic>("UnitCargo", true);
            LoadJSONModEntries<DBEntryStatic>("UnitCargo", true);
            LoadJSONEntries<DBEntryStatic>("UnitHeliports", true);
            PrepLoadEntries<DBEntryTemplate>("LoadJSONEntries", "Templates");
            PrepLoadEntries<DBEntryTemplate>("LoadJSONEntries", "TemplatesCustom");
            PrepLoadEntries<DBEntryLayout>("LoadJSONEntries", "Layouts");
            PrepLoadEntries<DBEntryDefaultUnitList>("LoadEntries", "DefaultUnitLists");
            LoadEntries<DBEntryCoalition>("Coalitions");
            LoadCustomUnitEntries<DBEntryCoalition>("Coalitions");
            PrepLoadEntries<DBEntryWeatherPreset>("LoadEntries", "WeatherPresets");


            // Can't start without at least one player-controllable aircraft
            if (!GetAllEntries<DBEntryJSONUnit>().Any(x => typeof(DBEntryAircraft).Equals(x.GetType()) && ((DBEntryAircraft)x).PlayerControllable))
                throw new BriefingRoomRawException("No player-controllable aircraft found.");

            Initialized = true;
            BriefingRoom.PrintToLog("---------------> Database initialized.", LogMessageErrorLevel.Warning);
        }

        private void PrepLoadEntries<T>(string type, string subDirectory) where T : DBEntry, new()
        {

            Type dbType = typeof(T);
            if (!DBEntries.ContainsKey(dbType))
                DBEntries.TryAdd(dbType, new Dictionary<string, DBEntry>(StringComparer.InvariantCultureIgnoreCase));
            DBEntries[dbType].Clear();
            UnloadedEntries[dbType] = new(subDirectory, type);
        }

        private void CheckAndLoadEntries<T>() where T : DBEntry, new()
        {
            Type dbType = typeof(T);
            if (!UnloadedEntries.ContainsKey(dbType)) return;

            var entry = UnloadedEntries[dbType];
            UnloadedEntries.Remove(dbType);

            string subDirectory = entry.Item1;
            string type = entry.Item2;
            switch (type)
            {
                case "LoadEntries":
                    LoadEntries<T>(subDirectory);
                    break;
                case "LoadJSONEntries":
                    LoadJSONEntries<T>(subDirectory);
                    break;
                case "LoadJSONFolderEntries":
                    LoadJSONFolderEntries<T>(subDirectory);
                    break;
                case "LoadCustomUnitEntries":
                    LoadCustomUnitEntries<T>(subDirectory);
                    break;
                default:
                    throw new BriefingRoomRawException($"Unknown database type {type}");
            }
        }

        private void LoadEntries<T>(string subDirectory) where T : DBEntry, new()
        {
            BriefingRoom.PrintToLog($"Loading {subDirectory.ToLower()}...");

            string directory = Path.Combine(BRPaths.DATABASE, subDirectory);
            if (!Directory.Exists(directory))
                throw new Exception($"Directory {directory} not found.");

            Type dbType = typeof(T);
            string shortTypeName = dbType.Name[7..].ToLower();

            if (!DBEntries.ContainsKey(dbType))
                DBEntries.TryAdd(dbType, new Dictionary<string, DBEntry>(StringComparer.InvariantCultureIgnoreCase));

            DBEntries[dbType].Clear();

            foreach (string filePath in Directory.EnumerateFiles(directory, "*.ini", SearchOption.AllDirectories))
            {
                string id = Path.GetFileNameWithoutExtension(filePath).Replace(",", "").Trim(); // No commas in file names, so we don't break comma-separated arrays

                if (DBEntries[dbType].ContainsKey(id)) continue;
                T entry = new();
                if (!entry.Load(this, id, filePath)) continue;
                DBEntries[dbType].TryAdd(id, entry);
                BriefingRoom.PrintToLog($"Loaded {shortTypeName} \"{id}\"");
            }
            BriefingRoom.PrintToLog($"Found {DBEntries[dbType].Count} database entries of type \"{typeof(T).Name}\"");

            bool mustHaveAtLeastOneEntry = true;
            if ((dbType == typeof(DBEntryFeatureMission)) ||
                (dbType == typeof(DBEntryFeatureObjective)))
                mustHaveAtLeastOneEntry = false;

            // If a required database type has no entries, raise an error.
            if ((DBEntries[dbType].Count == 0) && mustHaveAtLeastOneEntry)
                throw new BriefingRoomRawException($"No valid database entries found in the \"{subDirectory}\" directory");
        }

        private void LoadJSONEntries<T>(string subDirectory, bool unitType = false) where T : DBEntry, new()
        {
            BriefingRoom.PrintToLog($"Loading {subDirectory.Replace(BRPaths.DATABASEJSON, "").ToLower()}...");

            string filePath = Path.Combine(BRPaths.DATABASEJSON, subDirectory + (Path.HasExtension(subDirectory) ? "" : ".json"));
            if (!File.Exists(filePath))
                throw new Exception($"File {filePath} not found.");

            Type dbType = typeof(T);
            if (unitType)
                dbType = typeof(DBEntryJSONUnit);


            if (!DBEntries.ContainsKey(dbType))
                DBEntries.TryAdd(dbType, new Dictionary<string, DBEntry>(StringComparer.InvariantCultureIgnoreCase));
            Dictionary<string, DBEntry> entries = new T() switch
            {
                DBEntryAirbase a => DBEntries[dbType].Concat(DBEntryAirbase.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryCar a => DBEntries[dbType].Concat(DBEntryCar.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryAircraft a => DBEntries[dbType].Concat(DBEntryAircraft.LoadJSON(this, filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryShip a => DBEntries[dbType].Concat(DBEntryShip.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryStatic a => DBEntries[dbType].Concat(DBEntryStatic.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryCargo a => DBEntries[dbType].Concat(DBEntryCargo.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryTemplate a => DBEntries[dbType].Concat(DBEntryTemplate.LoadJSON(this, filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryLayout a => DBEntries[dbType].Concat(DBEntryLayout.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntrySituation a => DBEntries[dbType].Concat(DBEntrySituation.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                DBEntryWeaponByDecade a => DBEntries[dbType].Concat(DBEntryWeaponByDecade.LoadJSON(filePath, Language)).ToDictionary(pair => pair.Key, pair => pair.Value),
                _ => throw new BriefingRoomRawException($"JSON type {dbType} not implemented."),
            };
            DBEntries[dbType] = entries;
            BriefingRoom.PrintToLog($"Found {DBEntries[dbType].Count} database entries of type \"{typeof(T).Name}\"");


            bool mustHaveAtLeastOneEntry = true;
            if ((dbType == typeof(DBEntryFeatureMission)) ||
                (dbType == typeof(DBEntryFeatureObjective)))
                mustHaveAtLeastOneEntry = false;

            // If a required database type has no entries, raise an error.
            if ((DBEntries[dbType].Count == 0) && mustHaveAtLeastOneEntry)
                throw new BriefingRoomRawException($"No valid database entries found in the \"{subDirectory}\" directory");
        }

        private void LoadJSONFolderEntries<T>(string subDirectory) where T : DBEntry, new()
        {
            BriefingRoom.PrintToLog($"Loading {subDirectory.ToLower()}...");

            string folderPath = Path.Combine(BRPaths.DATABASEJSON, subDirectory);
            if (!Directory.Exists(folderPath))
                throw new Exception($"Folder {folderPath} not found.");

            foreach (var filePath in Directory.GetFiles(folderPath))
            {
                LoadJSONEntries<T>(filePath);
            }
        }

        private void LoadJSONModEntries<T>(string subDirectory, bool unitType) where T : DBEntry, new()
        {
            BriefingRoom.PrintToLog($"Loading {subDirectory.ToLower()} Mods...");

            string modFolderPath = Path.Combine(BRPaths.DATABASEJSON, "Mods");
            if (!Directory.Exists(modFolderPath))
                throw new Exception($"Folder {modFolderPath} not found.");

            foreach (var folderPath in Directory.GetDirectories(modFolderPath))
            {
                var filePath = Path.Combine(folderPath, subDirectory);
                var searchPath = $"{filePath}.json";
                if (!File.Exists(searchPath))
                    continue;

                LoadJSONEntries<T>(filePath, unitType);
            }
        }

        private void LoadCustomUnitEntries<T>(string subDirectory) where T : DBEntry, new()
        {
            BriefingRoom.PrintToLog($"Custom Loading {subDirectory.ToLower()}...");

            string directory = Path.Combine(BRPaths.CUSTOMDATABASE, subDirectory);
            if (!Directory.Exists(directory))
                return;

            Type dbType = typeof(T);
            string shortTypeName = dbType.Name[7..].ToLower();

            foreach (string filePath in Directory.EnumerateFiles(directory, "*.ini", SearchOption.AllDirectories))
            {
                string id = Path.GetFileNameWithoutExtension(filePath).Replace(",", "").Trim(); // No commas in file names, so we don't break comma-separated arrays

                var entry = new T();
                if (!entry.Load(this, id, filePath)) continue;
                if (DBEntries[dbType].ContainsKey(id))
                {
                    GetEntry<T>(id).Merge(entry);
                    BriefingRoom.PrintToLog($"Updated {shortTypeName} \"{id}\"");

                }
                else
                {
                    DBEntries[dbType].TryAdd(id, entry);
                    BriefingRoom.PrintToLog($"Loaded {shortTypeName} \"{id}\"");
                }
            }
            BriefingRoom.PrintToLog($"Found {DBEntries[dbType].Count} custom database entries of type \"{typeof(T).Name}\"");

            bool mustHaveAtLeastOneEntry = true;
            if ((dbType == typeof(DBEntryFeatureMission)) ||
                (dbType == typeof(DBEntryFeatureObjective)))
                mustHaveAtLeastOneEntry = false;

            // If a required database type has no entries, raise an error.
            if ((DBEntries[dbType].Count == 0) && mustHaveAtLeastOneEntry)
                throw new BriefingRoomRawException($"No valid database entries found in the \"{subDirectory}\" directory");
        }


        public string CheckID<T>(string id, string defaultID = null, bool allowEmptyStr = false, List<string> allowedValues = null) where T : DBEntry, new()
        {

            if (string.IsNullOrEmpty(id) && allowEmptyStr) return "";
            if (allowedValues != null && allowedValues.Contains(id)) return id;
            CheckAndLoadEntries<T>();
            if (EntryExists<T>(id)) return id;
            if (!string.IsNullOrEmpty(defaultID) && EntryExists<T>(defaultID)) return CheckID<T>(defaultID);
            if (allowEmptyStr || GetAllEntriesIDs<T>().Length == 0) return "";
            return GetAllEntriesIDs<T>()[0];
        }

        public string[] CheckIDs<T>(params string[] ids) where T : DBEntry, new()
        {
            return ids.Intersect(GetAllEntriesIDs<T>(), StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }

        public bool EntryExists<T>(string id) where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            return DBEntries[typeof(T)].ContainsKey(id ?? "");
        }

        public T[] GetAllEntries<T>() where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            return (from entry in DBEntries[typeof(T)].Values select (T)entry).ToArray();
        }

        public ST[] GetAllEntries<T, ST>()
            where T : DBEntry, new()
            where ST : DBEntry
        {
            CheckAndLoadEntries<T>();
            return (from entry in DBEntries[typeof(T)].Values where typeof(ST).Equals(entry.GetType()) select (ST)entry).ToArray();
        }

        public string[] GetAllEntriesIDs<T>() where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            if (!DBEntries.ContainsKey(typeof(T))) return null;
            return DBEntries[typeof(T)].Keys.ToArray();
        }

        public T GetEntry<T>(string id) where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            id ??= "";
            if (!DBEntries[typeof(T)].ContainsKey(id)) return null;
            return (T)DBEntries[typeof(T)][id];
        }

        public ST GetEntry<T, ST>(string id)
            where T : DBEntry, new()
            where ST : DBEntry
        {
            CheckAndLoadEntries<T>();
            id ??= "";
            if (!DBEntries[typeof(T)].ContainsKey(id)) return null;
            return (ST)DBEntries[typeof(T)][id];
        }

        public T[] GetEntries<T>(params string[] ids) where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            return (from T entry in GetAllEntries<T>() where ids.Distinct().OrderBy(x => x).Contains(entry.ID) select entry).ToArray();
        }

        public List<T> GetEntries<T>(List<string> ids) where T : DBEntry, new()
        {
            CheckAndLoadEntries<T>();
            return (from T entry in GetAllEntries<T>() where ids.Distinct().OrderBy(x => x).Contains(entry.ID) select entry).ToList();
        }
    }
}
