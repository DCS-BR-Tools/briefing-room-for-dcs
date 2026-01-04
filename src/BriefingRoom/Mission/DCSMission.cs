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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Generator;
using BriefingRoom4DCS.Generator.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Mission
{
    public sealed class DCSMission : DCSMissionState
    {

        private const int MAX_VALUE_LENGTH_DISPLAY = 16;
        public string TheaterID { get { return GetValue("TheaterID"); } }

        internal Stack<DCSMissionState> PreviousStates;

        internal List<DCSMissionStrikePackage> StrikePackages { get; private set; }

        internal MissionTemplateRecord TemplateRecord { get; init; }

        //Generation Variables
        internal WaypointNameGenerator WaypointNameGenerator { get; private set; }
        internal List<Coordinates> ObjectiveCoordinates = [];
        internal List<UnitFamily> ObjectiveTargetUnitFamilies = [];
        internal Coordinates ObjectivesCenter { get; set; }
        internal DBEntryCoalition[] CoalitionsDB;
        internal DBEntryTheater TheaterDB { get; init; }
        internal DBEntrySituation SituationDB { get; set; }
        internal double WindSpeedAtSeaLevel { get; set; }
        internal double WindDirectionAtSeaLevel { get; set; }
        internal DBEntryAirbase PlayerAirbase { get; set; } 
        internal DBEntryAirbase PlayerAirbaseDestination { get; set; } 
        internal Coordinates AverageInitialPosition { get; set; }
        internal List<List<List<Waypoint>>> ObjectiveGroupedWaypoints { get; set; }
        internal Country[][] CoalitionsCountries { get; set; }
        internal bool InvertedCoalition { get { return TemplateRecord.OptionsMission.Contains("InvertCountriesCoalitions"); } }
        internal bool IsSinglePlayerMission { get { return TemplateRecord.GetPlayerSlotsCount() == 1 && TemplateRecord.AirbaseDynamicSpawn == DsAirbase.None; } }
        internal string LangKey { get; private set;}

        internal DCSMission(IDatabase database, string langKey, MissionTemplateRecord template)
        {
            LangKey = langKey;
            Airbases = [];
            Briefing = new(langKey);
            WaypointNameGenerator = new(database, langKey);
            ResetStrikePackages();
            MediaFiles = new(StringComparer.InvariantCultureIgnoreCase);
            Values = new(StringComparer.InvariantCultureIgnoreCase);
            SingletonSet = new(StringComparer.InvariantCultureIgnoreCase);
            MapData = new(StringComparer.InvariantCultureIgnoreCase);
            PopulatedAirbaseIds = new Dictionary<Coalition, List<int>>{
                    {Coalition.Blue, new List<int>()},
                    {Coalition.Red, new List<int>()},
                    {Coalition.Neutral, new List<int>()}
                };

            UniqueID = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()).ToLower();
            SetValue("MissionID", UniqueID);
            SetValue("ScriptMIST", Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_LUA, "MIST.lua")));
            SetValue("ScriptSingletons", "");
            TemplateRecord = template;
            PreviousStates = [];

            CoalitionsDB =
            [
                database.GetEntry<DBEntryCoalition>(template.ContextCoalitionBlue),
                database.GetEntry<DBEntryCoalition>(template.ContextCoalitionRed)
            ];
            Waypoints = [];
            TheaterDB = database.GetEntry<DBEntryTheater>(template.ContextTheater);

            LuaDrawings = [];
            LuaZones = [];
            FrontLine = [];
            AirbaseParkingSpots = [];
            SpawnPoints = [];
            UsedSpawnPoints = [];
            TemplateLocations = [];
            UsedTemplateLocations = [];

            GroupID = 1;
            UnitID = 1;

            NATOCallsigns = [];
            RussianCallsigns = [];
            CarrierDictionary = [];
            ModUnits = [];
            UnitLuaTables = [];
            AirbaseDB = [];
        }

        internal string ReplaceValues(string rawText, bool useHTMLBreaks = false)
        {
            if (rawText == null) return null;

            foreach (KeyValuePair<string, string> keyPair in Values) // Replace included scripts first so later replacements (objective count, player coalition, etc) will be applied to them too
            {
                if (!keyPair.Key.ToLower().StartsWith("script")) continue;
                rawText = rawText.Replace($"${keyPair.Key.ToUpper()}$", useHTMLBreaks ? keyPair.Value.Replace("\n", "<br/>") : keyPair.Value);
            }

            foreach (KeyValuePair<string, string> keyPair in Values) // Replace other values
                rawText = rawText.Replace($"${keyPair.Key.ToUpper()}$", useHTMLBreaks ? keyPair.Value.Replace("\n", "<br/>") : keyPair.Value);

            return rawText;
        }

        internal void SetAirbase(int airbaseID, Coalition airbaseCoalition)
        {
            if (Airbases.ContainsKey(airbaseID))
                Airbases[airbaseID] = airbaseCoalition;
            else
                Airbases.Add(airbaseID, airbaseCoalition);
        }

        internal void SaveStage(MissionStageName stageName)
        {
            PreviousStates.Push(new DCSMissionState(stageName, this));
        }

        internal void RevertStage(int stages)
        {

            while (stages > 1)
            {
                PreviousStates.Pop();
                stages--;
            };

            var prevState = PreviousStates.First();
            Briefing = new DCSMissionBriefing(prevState.Briefing);
            MapData = prevState.MapData.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            Values = prevState.Values.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            MediaFiles = prevState.MediaFiles.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            SingletonSet = prevState.SingletonSet.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
            Airbases = prevState.Airbases.ToDictionary(x => x.Key, x => x.Value);
            PopulatedAirbaseIds = prevState.PopulatedAirbaseIds.ToDictionary(x => x.Key, x => x.Value.ToList());
            LuaDrawings = prevState.LuaDrawings.ToList();
            LuaZones = prevState.LuaZones.ToList();
            CTLDZoneCount = prevState.CTLDZoneCount;
            PrevLaserCode = prevState.PrevLaserCode;
            TACANIndex = prevState.TACANIndex;
            AirbaseParkingSpots = prevState.AirbaseParkingSpots.ToDictionary(x => x.Key, x => x.Value.ToList());
            SpawnPoints = prevState.SpawnPoints.ToList();
            UsedSpawnPoints = prevState.UsedSpawnPoints.ToList();
            TemplateLocations = prevState.TemplateLocations.ToList();
            UsedTemplateLocations = prevState.UsedTemplateLocations.ToList();
            FrontLine = prevState.FrontLine.ToList();
            PlayerSideOfFrontLine = prevState.PlayerSideOfFrontLine;
            GroupID = prevState.GroupID;
            UnitID = prevState.UnitID;
            CarrierDictionary = prevState.CarrierDictionary.ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);
            ModUnits = prevState.ModUnits.ToList();
            UnitLuaTables = prevState.UnitLuaTables.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value.ToList()));
            Waypoints = prevState.Waypoints.ToList();
            AirbaseDB = prevState.AirbaseDB.ToList();
        }

        internal MissionStageName GetLastSavedStage()
        {
            return (MissionStageName)Enum.Parse(typeof(MissionStageName), PreviousStates.Last().UniqueID);
        }


        internal void ResetStrikePackages()
        {
            StrikePackages = new List<DCSMissionStrikePackage>();
        }


        internal void SetValue(string key, int value)
        {
            SetValue(key, value.ToString(NumberFormatInfo.InvariantInfo), false);
        }

        internal void SetValue(string key, double value)
        {
            SetValue(key, value.ToString(NumberFormatInfo.InvariantInfo), false);
        }

        internal void SetValue(string key, bool value)
        {
            SetValue(key, value.ToString(NumberFormatInfo.InvariantInfo).ToLower(), false);
        }

        internal void SetValue(string key, string value)
        {
            SetValue(key, value, false);
        }

        internal void AppendValue(string key, string value)
        {
            SetValue(key, value, true);
        }

        internal void AppendSingletonValue(string id, string key, string value)
        {
            if (SingletonSet.Contains(id))
                return;
            AppendValue(key, value);
            SingletonSet.Add(id);
        }

        private void SetValue(string key, string value, bool append)
        {
            if (string.IsNullOrEmpty(key)) return;
            key = key.ToUpper();
            value ??= "";
            value = value.Replace("\r\n", "\n");

            // string displayedValue = value.Replace("\n", " ");
            // if (displayedValue.Length > MAX_VALUE_LENGTH_DISPLAY) displayedValue = string.Concat(displayedValue.AsSpan(0, MAX_VALUE_LENGTH_DISPLAY), "...");
            // BriefingRoom.PrintToLog($"Mission parameter \"{key.ToLower()}\" {(append ? "appended with" : "set to")} \"{displayedValue}\"."); // Spammy so debug only

            if (!Values.ContainsKey(key))
                Values.Add(key, value);
            else
                Values[key] = append ? Values[key] + value : value;
        }

        internal string GetValue(string key)
        {
            return Values[key];
        }

        internal void AddMediaFile(string fileName, string sourceFilePath)
        {
            if (string.IsNullOrEmpty(fileName) || MediaFiles.ContainsKey(fileName)) return;
            if (!File.Exists(sourceFilePath)) return;
            MediaFiles.Add(fileName, sourceFilePath);
        }

        internal void AddMediaFolder(string folderName, string sourceFolderPath)
        {
            if (!Directory.Exists(sourceFolderPath)) return;
            foreach (string file in Directory.EnumerateFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories))
            {
                var filePath = file.Split(sourceFolderPath)[1];
                if (!MediaFiles.ContainsKey($"{folderName}{filePath}"))
                {
                    var endPath = $"{folderName}{filePath}".Replace("\\", "/");
                    MediaFiles.Add(endPath, sourceFolderPath + filePath);
                }
            }

        }

        internal void AddMediaFile(string fileName, byte[] mediaFileBytes)
        {
            if (string.IsNullOrEmpty(fileName) || MediaFiles.ContainsKey(fileName)) return;
            MediaFiles.Add(fileName, mediaFileBytes);
        }

        public async Task<bool> SaveToMizFile(IDatabase database, string mizFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(mizFilePath)) return false;
                if (!Toolbox.IsFilePathValid(mizFilePath)) return false;

                byte[] mizBytes = await SaveToMizBytes(database);
                if (mizBytes == null) return false; // Something went wrong

                if (File.Exists(mizFilePath)) File.Delete(mizFilePath);

                File.WriteAllBytes(mizFilePath, mizBytes);
                return true;
            }
            catch (Exception ex)
            {
                throw new(ex.Message);
            }
        }

        public async Task<byte[]> SaveToMizBytes(IDatabase database)
        {
            // Generate image files
            BriefingRoom.PrintToLog("Generating images...");
            await Imagery.GenerateTitleImage(database, this);
            if (!TemplateRecord.OptionsMission.Contains("DisableKneeboardImages"))
                await Imagery.GenerateKneeboardImagesAsync(database, this);

            return MizMaker.ExportToMizBytes(database, this, TemplateRecord.Template);
        }

        internal string[] GetMediaFileNames()
        {
            return MediaFiles.Keys.ToArray();
        }

        internal byte[] GetMediaFile(string mediaFileName)
        {
            if (!MediaFiles.ContainsKey(mediaFileName)) return null;
            if (MediaFiles[mediaFileName] is byte[] fileBytes) return fileBytes;
            if (MediaFiles[mediaFileName] is string filePath)
            {
                if (!File.Exists(filePath)) return null;
                return File.ReadAllBytes(filePath);
            }
            return null;
        }

        internal bool IsExtremeDistance(IBriefingRoom briefingRoom, MissionTemplate template, out double distance)
        {
            var objCenter = new Coordinates(
                double.Parse(GetValue("MissionCenterX"), CultureInfo.InvariantCulture),
                double.Parse(GetValue("MissionCenterY"), CultureInfo.InvariantCulture));
            var playerAirbase = new Coordinates(
                double.Parse(GetValue("MissionAirbaseX"), CultureInfo.InvariantCulture),
                double.Parse(GetValue("MissionAirbaseY"), CultureInfo.InvariantCulture));
            distance = objCenter.GetDistanceFrom(playerAirbase) * Toolbox.METERS_TO_NM;
            var extremeLimit = template.FlightPlanObjectiveDistanceMax * 1.7;
            var isTooFar = distance > extremeLimit;
            if (isTooFar)
                briefingRoom.PrintTranslatableWarning("DistanceTooFar", distance, extremeLimit);
            return isTooFar;
        }

        public Dictionary<string, List<double[]>> GetMapData()
        {
            return MapData;
        }


        internal int GetNextLaserCode()
        {
            var code = PrevLaserCode;
            code++;
            var digits = GetLaserDigits(code).ToList();
            if (digits.Last() == 9)
                code += 2;
            digits = GetLaserDigits(code).ToList();
            if (digits[2] == 9)
                code += 20;
            if (code >= 1788)
                code = 1511;
            PrevLaserCode = code;
            return code;
        }

        private static IEnumerable<int> GetLaserDigits(int source)
        {
            Stack<int> digits = new();
            while (source > 0)
            {
                var digit = source % 10;
                source /= 10;
                digits.Push(digit);
            }

            return digits;
        }


        internal string GetTACANSettingsFromFeature(DBEntryFeature featureDB, ref Dictionary<string, object> extraSettings)
        {
            if (featureDB.UnitGroupFlags.HasFlag(FeatureUnitGroupFlags.TACAN) && (featureDB.UnitGroupFamilies.Length > 0))
            {
                var callsign = (GetType() == typeof(FeaturesObjectives) && extraSettings.ContainsKey("ObjectiveName")) ? extraSettings["ObjectiveName"].ToString()[..3] : $"{GeneratorTools.GetTACANCallsign(featureDB.UnitGroupFamilies[0])}{TACANIndex}";
                if (extraSettings.ContainsKey("TACAN_NAME"))
                {
                    var tacanName = extraSettings["TACAN_NAME"].ToString();
                    callsign = tacanName.Length > 3 ? tacanName[..3] : tacanName;
                }
                var channel = ((GetType() == typeof(FeaturesObjectives)) ? 31 : 25) + TACANIndex;
                extraSettings.AddIfKeyUnused("TACANFrequency", 1108000000);
                extraSettings.AddIfKeyUnused("TACANCallsign", callsign);
                extraSettings.AddIfKeyUnused("TACANChannel", channel);
                if (TACANIndex < 9) TACANIndex++;
                return $",\n{channel}X (callsign {callsign})";
            }
            return "";
        }

        internal void SetFrontLine(List<Coordinates> frontLine, Coordinates playerAirbase, Coalition playerCoalition)
        {
            FrontLine = frontLine;
            PlayerSideOfFrontLine = ShapeManager.GetSideOfLine(playerAirbase, FrontLine);
        }
    }
}

