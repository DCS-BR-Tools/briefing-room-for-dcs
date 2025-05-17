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
using BriefingRoom4DCS.Data.JSON;
using Newtonsoft.Json;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryTheater : DBEntry
    {
        internal static readonly List<string> DESERT_MAPS = new() { "Nevada", "PersianGulf", "Syria", "SinaiMap", "Afghanistan" };
        private static readonly MinMaxI DEFAULT_DAYTIME = new(8 * 60, 19 * 60);

        internal Coordinates DefaultMapCenter { get; private set; }

        internal string DCSID { get; private set; }

        internal double MagneticDeclination { get; private set; }
        internal bool SouthernHemisphere { get; private set; }

        internal MinMaxI[] DayTime { get; private set; }

        internal List<List<Coordinates>> WaterCoordinates { get; private set; }

        internal List<List<Coordinates>> WaterExclusionCoordinates { get; private set; }

        internal DBEntryTheaterSpawnPoint[] SpawnPoints { get { return GetSpawnPoints(); } }
        internal DBEntryTheaterSpawnPoint[] _SpawnPoints { get; private set; }
       
        internal DBEntryTheaterTemplateLocation[] TemplateLocations { get; private set; }
        internal MinMaxI[] Temperature { get; private set; }


        protected override bool OnLoad(string iniFilePath)
        {
            int i;

            var ini = new INIFile(iniFilePath);

            // [Theater] section
            DCSID = ini.GetValue<string>("Theater", "DCSID");
            DefaultMapCenter = ini.GetValue<Coordinates>("Theater", "DefaultMapCenter");
            MagneticDeclination = ini.GetValue<double>("Theater", "MagneticDeclination");
            SouthernHemisphere = ini.GetValue<bool>("Theater", "SouthernHemisphere", false);

            // [Daytime] section
            DayTime = new MinMaxI[12];
            for (i = 0; i < 12; i++)
            {
                MinMaxI? dayTimeValue = ParseMinMaxTime(ini.GetValueArray<string>("Daytime", ((Month)i).ToString()));

                if (!dayTimeValue.HasValue) // Cast failed
                    BriefingRoom.PrintToLog(
                        $"Wrong format for daytime value for month {(Month)i} in theater {ID}, using default value",
                        LogMessageErrorLevel.Warning);

                DayTime[i] = dayTimeValue ?? DEFAULT_DAYTIME;
            }
            // Water Coordinates
            var boundsJsonFilePath = Path.Combine(BRPaths.DATABASEJSON, "TheaterTerrainBounds", $"{DCSID}.json");
            if (!File.Exists(boundsJsonFilePath))
                throw new BriefingRoomException("en", $"{DCSID} Missing Terrain Data. File not found: {boundsJsonFilePath}");

            var terrainData = JsonConvert.DeserializeObject<TerrainBounds>(File.ReadAllText(boundsJsonFilePath));
            WaterCoordinates = terrainData.waters.Select(x => x.Select(y => new Coordinates(y)).ToList()).ToList();
            WaterExclusionCoordinates = terrainData.landMasses.Select(x => x.Select(y => new Coordinates(y)).ToList()).ToList();

            var templateLocationFilePath = Path.Combine(BRPaths.DATABASEJSON, "TheaterTemplateLocations", $"{DCSID}.json");
            if (File.Exists(templateLocationFilePath))
            {
                TemplateLocations = JsonConvert.DeserializeObject<List<TheaterTemplateLocation>>(File.ReadAllText(templateLocationFilePath)).Select(x =>
                    new DBEntryTheaterTemplateLocation(x)
                ).ToArray();
                BriefingRoom.PrintToLog($"{DCSID} loaded {TemplateLocations.Length} Template Locations");
            } 
            else
            {
                TemplateLocations = Array.Empty<DBEntryTheaterTemplateLocation>();
                BriefingRoom.PrintToLog($"{DCSID} no Template Locations found", LogMessageErrorLevel.Warning);
            }

            // [Temperature] section
            Temperature = new MinMaxI[12];
            for (i = 0; i < 12; i++)
                Temperature[i] = ini.GetValue<MinMaxI>("Temperature", ((Month)i).ToString());
            return true;
        }

        internal double GetMagneticHeading(double heading)
        {
            var tunedHeading = Math.Round(heading + MagneticDeclination);
            if (tunedHeading >= 360)
                tunedHeading -= 360;
            if (tunedHeading < 0)
                tunedHeading += 360;
            return tunedHeading;
        }

        private DBEntryTheaterSpawnPoint[] GetSpawnPoints()
        {

            if (_SpawnPoints != null) return _SpawnPoints;
            var spawnPointsJsonFilePath = Path.Combine(BRPaths.DATABASEJSON, "TheaterSpawnPoints", $"{DCSID}.json");
            if (!File.Exists(spawnPointsJsonFilePath))
                throw new BriefingRoomException("en", $"{DCSID} Missing SpawnPoint JSON Data. File not found: {spawnPointsJsonFilePath}");

            _SpawnPoints = JsonConvert.DeserializeObject<List<SpawnPoint>>(File.ReadAllText(spawnPointsJsonFilePath)).Select(x =>
                new DBEntryTheaterSpawnPoint
                {
                    Coordinates = new Coordinates(x.coords),
                    PointType = (SpawnPointType)Enum.Parse(typeof(SpawnPointType), x.BRtype, true)
                }
            ).ToArray();
            BriefingRoom.PrintToLog($"{DCSID} loaded {_SpawnPoints.Length} spawn points");

            var spawnPointsGeneratedJsonFilePath = Path.Combine(BRPaths.DATABASEJSON, "TheaterSpawnPoints", $"{DCSID}_Generated.json");
            if (File.Exists(spawnPointsGeneratedJsonFilePath))
            {
                var generatedSpawnPoints = JsonConvert.DeserializeObject<List<SpawnPoint>>(File.ReadAllText(spawnPointsGeneratedJsonFilePath)).Select(x =>
                    new DBEntryTheaterSpawnPoint
                    {
                        Coordinates = new Coordinates(x.coords),
                        PointType = (SpawnPointType)Enum.Parse(typeof(SpawnPointType), x.BRtype, true)
                    }
                ).ToArray();
                BriefingRoom.PrintToLog($"{DCSID} loaded {generatedSpawnPoints.Length} generated spawn points");
                _SpawnPoints = _SpawnPoints.Concat(generatedSpawnPoints).ToArray();
                BriefingRoom.PrintToLog($"{DCSID} total {SpawnPoints.Length} spawn points");

            }
            return _SpawnPoints;

        }

        private static MinMaxI? ParseMinMaxTime(string[] timeValues)
        {
            if (timeValues.Length != 2) return null;

            int[] vals = new int[2];

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    double val = Convert.ToDouble(timeValues[i].Replace(':', '.').Trim(), NumberFormatInfo.InvariantInfo);
                    int hours = (int)val; // Hours are stored in the in the integer part
                    int minutes = Toolbox.Clamp((int)((val - hours) * 60.0), 0, 59); // Minutes are stores in the decimal part
                    hours = Toolbox.Clamp(hours, 0, 23);
                    vals[i] = hours * 60 + minutes;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            if (vals[0] > vals[1]) return null; // Min value > Max value. BAD!

            return new MinMaxI(vals[0], vals[1]);
        }
    }
}
