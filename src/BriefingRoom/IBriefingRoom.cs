using System;
using System.Collections.Generic;
using BriefingRoom4DCS;
using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS
{
    public interface IBriefingRoom
    {
        string LanguageKey { get; set; }
        IDatabase Database { get; }

        void SetLogHandler(BriefingRoom.LogHandler logHandler);

        List<string> GetUnitIdsByFamily(UnitFamily family);

        DatabaseEntryInfo[] GetDatabaseEntriesInfo(DatabaseEntryType entryType, string parameter = "");

        DatabaseEntryInfo? GetSingleDatabaseEntryInfo(DatabaseEntryType entryType, string id);

        List<Tuple<string, string>> GetAircraftLiveries(string aircraftID);

        List<string> GetAircraftCallsigns(string aircraftID, Country country);

        List<Tuple<string, Decade>> GetAircraftPayloads(string aircraftID);

        List<SpawnPoint> GetTheaterSpawnPoints(string theaterID);

        Tuple<List<List<double[]>>, List<List<double[]>>> GetTheaterWaterZones(string theaterID);

        string[] GetDatabaseEntriesIDs(DatabaseEntryType entryType, string parameter = "");

        DCSMission GenerateMission(string templateFilePath);

        DCSMission GenerateMission(MissionTemplate template);

        DCSCampaign GenerateCampaign(string templateFilePath);

        DCSCampaign GenerateCampaign(CampaignTemplate template);

        Dictionary<string, List<double[]>> GetMapSupportingMapData(MissionTemplate template);

        Dictionary<string, List<double[]>> GetAirbasesMapData(string mapID);

        string Translate(string key);

        string Translate(string key, params object[] args);

        void ReloadDatabase();

        bool SetDCSSaveGamePath(string path);

        void PrintTranslatableWarning(string key, params object[] args);

    }
}