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

using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using BriefingRoom4DCS.Data;
using System.Text.RegularExpressions;
using BriefingRoom4DCS.Mission;
using Microsoft.Maui.Graphics;

namespace BriefingRoom4DCS
{
    internal static partial class Toolbox
    {
        internal const int MAXIMUM_FLIGHT_GROUP_SIZE = 4;

        internal const double DEGREES_TO_RADIANS = 0.0174533;

        internal const double RADIANS_TO_DEGREES = 57.2958;

        internal const double METERS_TO_NM = 0.000539957;

        internal const double NM_TO_METERS = 1852.0;

        internal const int MINUTES_PER_DAY = 24 * 60;

        internal const int SECONDS_PER_DAY = MINUTES_PER_DAY * 60;

        internal const double FEET_TO_METERS = 0.3048;
        internal const double METERS_TO_FEET = 3.28084;

        internal const double KNOTS_TO_METERS_PER_SECOND = 0.514444;

        internal static readonly MinMaxD ANY_RANGE = new(1, 1000);
        internal static readonly MinMaxD HINT_RANGE = new(0, 10);

        internal static List<string> ALIASES = new()
        {
                "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliet", "Kilo",
                "Lima", "Mike", "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor",
                "Whiskey", "X-Ray", "Yankee", "Zulu"};

        internal static bool CreateMissingDirectory(string path)
        {
            if (Directory.Exists(path)) return true;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception) { return false; }

            return true;
        }

        internal static string GetAlias(int index) =>
            ALIASES[index % ALIASES.Count];


        internal const double METERS_PER_SECOND_TO_KNOTS = 1.94384;

        internal const double TWO_PI = Math.PI * 2;

        internal static readonly UnitFamily[] CARRIER_FAMILIES = new UnitFamily[] { UnitFamily.ShipCarrierCATOBAR, UnitFamily.ShipCarrierSTOBAR, UnitFamily.ShipCarrierSTOVL };

        private static readonly Random Rnd = new();

        internal static bool StringICompare(string string1, string string2)
        {
            if (string1 == null || string2 == null) return string1 == string2;

            return string1.ToLower() == string2.ToLower();
        }

        internal static string ToInvariantString(this double value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        internal static Dictionary<string, object> ToDictionaryObject(string dataStr)
        {
            var dict = new Dictionary<string, object>();
            var regex = LuaDictRegex();
            if (string.IsNullOrEmpty(dataStr))
                return dict;
            foreach (string str in dataStr.Split(","))
            {
                if (!regex.IsMatch(str))
                    throw new Exception($"Invalid Lua {dataStr}");
                var match = regex.Match(str);
                dict.Add(match.Groups[1].Value, DeterminType(match.Groups[2].Value));
            }
            return dict;
        }

        internal static object DeterminType(object value)
        {
            if (value is IDictionary<object, object>)
            {
                return ((Dictionary<object, object>)value).ToDictionary(x => x.Key.ToString(), x => DeterminType(x.Value));
            }
            if (value is not string)
                return value;
            var strVal = value as string;
            if (bool.TryParse(strVal, out bool boolVal))
                return boolVal;
            if (int.TryParse(strVal, out int intVal))
                return intVal;
            if (float.TryParse(strVal, out float floatVal))
                return floatVal;
            return strVal;
        }

        internal static string ToLuaName(this UnitCategory unitCategory)
        {
            return unitCategory switch
            {
                UnitCategory.Helicopter => "HELICOPTER",
                UnitCategory.Plane => "AIRPLANE",
                UnitCategory.Ship => "SHIP",
                UnitCategory.Cargo or UnitCategory.Static => "STRUCTURE",
                _ => "GROUND_UNIT",// case UnitCategory.Vehicle
            };
        }

        internal static string RemoveInvalidPathCharacters(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "_";
            return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        }


        internal static double RandomAngle()
        {
            return RandomDouble(TWO_PI);
        }

        internal static bool IsFilePathValid(string path, bool allowRelativePaths = false)
        {
            bool isValid;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '/' })) == false;
                }
            }
            catch (Exception)
            {
                isValid = false;
            }

            return isValid;
        }
        internal static T ElementAtOrDefault<T>(this IList<T> list, int index, T @default)
        {
            return index >= 0 && index < list.Count ? list[index] : @default;
        }

        internal static T[] ShuffleArray<T>(T[] array)
        {
            return array.OrderBy(x => Rnd.Next()).ToArray();
        }

        internal static List<T> ShuffleList<T>(List<T> array)
        {
            return array.OrderBy(x => Rnd.Next()).ToList();
        }

        internal static KeyValuePair<string, object> ToKeyValuePair(this string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        internal static void AddIfKeyUnused<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, T2 value)
        {
            if (dictionary.ContainsKey(key))
                return;
            try {
                dictionary.TryAdd(key, value);
            } catch (Exception e) {
                BriefingRoom.PrintToLog($"Failed to add key {key} to dictionary {e}", LogMessageErrorLevel.Warning);
            }
        }

        internal static string ReplaceAll(this string str, string replaceTo, params string[] replaceFrom)
        {
            StringBuilder sb = new(str);

            foreach (string r in replaceFrom)
                sb.Replace(r, replaceTo);

            return sb.ToString();
        }

        internal static Coalition GetEnemy(this Coalition coalition)
        {
            return coalition == Coalition.Blue ? Coalition.Red : Coalition.Blue;
        }

        internal static string ReadAllTextIfFileExists(string filePath)
        {
            if (!File.Exists(filePath)) return "";
            return File.ReadAllText(filePath);
        }

        internal static Point Add(this Point point, Point other)
        {
            return new Point(point.X + other.X, point.Y + other.Y);
        }

        internal static string AddMissingFileExtension(string filePath, string extension)
        {
            if (filePath == null) return null;
            if (!filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                return $"{filePath}{extension}";

            return filePath;
        }

        internal static string[] AddMissingFileExtensions(string[] filePaths, string extension)
        {
            if (filePaths == null) return null;

            return (from string filePath in filePaths select AddMissingFileExtension(filePath, extension)).ToArray();
        }

        internal static UnitFamily[] SetSingleCategoryFamilies(UnitFamily[] unitFamilies)
        {
            if (unitFamilies == null || unitFamilies.Length == 0) return Array.Empty<UnitFamily>();

            return (from UnitFamily unitFamily in unitFamilies
                    where unitFamily.GetUnitCategory() == unitFamilies[0].GetUnitCategory()
                    select unitFamily).Distinct().ToArray();
        }

        internal static int GetRandomYearFromDecade(Decade decade)
        {
            return decade switch
            {
                Decade.Decade1940 => RandomInt(1942, 1945),// WW2 only
                Decade.Decade1950 => RandomInt(1950, 1960),
                Decade.Decade1960 => RandomInt(1960, 1970),
                Decade.Decade1970 => RandomInt(1970, 1980),
                Decade.Decade1980 => RandomInt(1980, 1990),
                Decade.Decade1990 => RandomInt(1990, 2000),
                Decade.Decade2010 => RandomInt(2010, 2020),
                Decade.Decade2020 => RandomInt(2020, 2030),
                _ => RandomInt(2000, 2010),// case Decade.Decade2000
            };
        }

        internal static Season GetSeasonFromMonth(int monthInt)
        {
            var month = (Month)(monthInt - 1);
            return month switch
            {
                Month.January => Season.Winter,
                Month.February => Season.Winter,
                Month.March => Season.Spring,
                Month.April => Season.Spring,
                Month.May => Season.Spring,
                Month.June => Season.Summer,
                Month.July => Season.Summer,
                Month.August => Season.Summer,
                Month.September => Season.Fall,
                Month.October => Season.Fall,
                Month.November => Season.Fall,
                Month.December => Season.Winter,
                _ => Season.Summer
            };
        }


        internal static string PATH_USER { get; } = NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        internal static string PATH_USER_DOCS { get; } = NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));


        internal static T[] GetEnumValues<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        internal static AmountNR Get(this AmountNR amountNR)
        {
            return AmountNR.Random == amountNR ? (AmountNR)(RandomInt((int)AmountNR.VeryHigh) + 1) : amountNR;
        }

        internal static bool RollChance(this AmountNR amountN)
        {
            int chance;
            switch (amountN.Get())
            {
                case AmountNR.None:
                    return false;
                case AmountNR.VeryLow:
                    chance = 90;
                    break;
                case AmountNR.Low:
                    chance = 75;
                    break;
                case AmountNR.High:
                    chance = 25;
                    break;
                case AmountNR.VeryHigh:
                    chance = 10;
                    break;
                default:
                    chance = 50;
                    break;
            }
            return RandomMinMax(1, 100) > chance;
        }

        internal static T[] ParseEnumString<T>(string enumString, char separator = ',', string prefix = "") where T : struct
        {
            if (enumString == null || enumString.Length == 0) return Array.Empty<T>();

            string[] strParts = enumString.Split(separator);

            List<T> enumValues = new();
            foreach (string s in strParts)
            {
                if (Enum.TryParse(prefix + s, true, out T e))
                    enumValues.Add(e);
            }

            return enumValues.ToArray();
        }

        internal static double ClampAngle(double angle)
        {
            int angleDeg = (int)(angle * RADIANS_TO_DEGREES);
            while (angleDeg < 0) { angleDeg += 360; }
            angleDeg %= 360;
            return angleDeg * DEGREES_TO_RADIANS;
        }

        internal static int EnumCount<T>() where T : struct
        {
            return Enum.GetValues(typeof(T)).Length;
        }

        internal static UnitCategory GetUnitCategory(this UnitFamily unitFamily)
        {
            return unitFamily switch
            {
                UnitFamily.HelicopterAttack or UnitFamily.HelicopterTransport or UnitFamily.HelicopterUtility => UnitCategory.Helicopter,
                UnitFamily.PlaneAttack or UnitFamily.PlaneAWACS or UnitFamily.PlaneBomber or UnitFamily.PlaneDrone or UnitFamily.PlaneFighter or UnitFamily.PlaneInterceptor or UnitFamily.PlaneSEAD or UnitFamily.PlaneStrike or UnitFamily.PlaneTankerBasket or UnitFamily.PlaneTankerBoom or UnitFamily.PlaneTransport or UnitFamily.PlaneCATOBAR or UnitFamily.PlaneSTOBAR or UnitFamily.PlaneSTOVL => UnitCategory.Plane,
                UnitFamily.ShipCarrierCATOBAR or UnitFamily.ShipCarrierSTOBAR or UnitFamily.ShipCarrierSTOVL or UnitFamily.ShipCruiser or UnitFamily.ShipFrigate or UnitFamily.ShipSpeedboat or UnitFamily.ShipSubmarine or UnitFamily.ShipTransport => UnitCategory.Ship,
                UnitFamily.StaticStructureMilitary or UnitFamily.StaticStructureProduction or UnitFamily.FOB or UnitFamily.StaticStructureOffshore or UnitFamily.StaticStructureCivilian or UnitFamily.StaticDecorative => UnitCategory.Static,
                UnitFamily.Cargo => UnitCategory.Cargo,
                UnitFamily.Infantry or UnitFamily.InfantryMANPADS => UnitCategory.Infantry,
                _ => UnitCategory.Vehicle,
            };
        }

        internal static DCSUnitCategory GetDCSUnitCategory(this UnitFamily unitFamily)
        {
            return unitFamily switch
            {
                UnitFamily.HelicopterAttack or UnitFamily.HelicopterTransport or UnitFamily.HelicopterUtility => DCSUnitCategory.Helicopter,
                UnitFamily.PlaneAttack or UnitFamily.PlaneAWACS or UnitFamily.PlaneBomber or UnitFamily.PlaneDrone or UnitFamily.PlaneFighter or UnitFamily.PlaneInterceptor or UnitFamily.PlaneSEAD or UnitFamily.PlaneStrike or UnitFamily.PlaneTankerBasket or UnitFamily.PlaneTankerBoom or UnitFamily.PlaneTransport or UnitFamily.PlaneCATOBAR or UnitFamily.PlaneSTOBAR or UnitFamily.PlaneSTOVL => DCSUnitCategory.Plane,
                UnitFamily.ShipCarrierCATOBAR or UnitFamily.ShipCarrierSTOBAR or UnitFamily.ShipCarrierSTOVL or UnitFamily.ShipCruiser or UnitFamily.ShipFrigate or UnitFamily.ShipSpeedboat or UnitFamily.ShipSubmarine or UnitFamily.ShipTransport => DCSUnitCategory.Ship,
                UnitFamily.StaticStructureMilitary or UnitFamily.StaticStructureProduction or UnitFamily.FOB or UnitFamily.StaticStructureOffshore or UnitFamily.StaticStructureCivilian or UnitFamily.StaticDecorative => DCSUnitCategory.Static,
                UnitFamily.Cargo => DCSUnitCategory.Cargo,
                _ => DCSUnitCategory.Vehicle,
            };
        }

        internal static bool IsAircraft(this UnitCategory unitCategory)
        {
            return unitCategory == UnitCategory.Helicopter || unitCategory == UnitCategory.Plane;
        }

         internal static bool IsGroundMoving(this UnitCategory unitCategory)
        {
            return unitCategory == UnitCategory.Vehicle || unitCategory == UnitCategory.Infantry;
        }

        internal static bool IsAirDefense(this UnitFamily unitFamily)
        {
            return unitFamily switch
            {
                UnitFamily.VehicleAAA or UnitFamily.VehicleAAAStatic or UnitFamily.InfantryMANPADS or UnitFamily.VehicleSAMLong or UnitFamily.VehicleSAMMedium or UnitFamily.VehicleSAMShort or UnitFamily.VehicleSAMShortIR => true,
                _ => false,
            };
        }

        internal static bool IsCarrier(this UnitFamily unitFamily)
        {
            return unitFamily switch
            {
                UnitFamily.ShipCarrierCATOBAR or UnitFamily.ShipCarrierSTOBAR or UnitFamily.ShipCarrierSTOVL => true,
                _ => false,
            };
        }

        internal static string ValToString(object value, string stringFormat = "", string langKey = "em")
        {
            if (value == null) return "";
            if (value is string v) return v;
            if (value is bool v1) return v1.ToString(NumberFormatInfo.InvariantInfo);
            if (value is int v2) return v2.ToString(stringFormat, NumberFormatInfo.InvariantInfo);
            if (value is float v3) return v3.ToString(stringFormat, NumberFormatInfo.InvariantInfo);
            if (value is double v4) return v4.ToString(stringFormat, NumberFormatInfo.InvariantInfo);
            if (value is LanguageString @string) return @string.Get(langKey);
            return value.ToString();
        }

        internal static double StringToDouble(string stringValue, double defaultValue = 0.0)
        {
            try { return Convert.ToDouble(stringValue.Trim(), NumberFormatInfo.InvariantInfo); }
            catch (Exception) { return defaultValue; }
        }

        internal static int StringToInt(string stringValue, int defaultValue = 0)
        {
            try { return Convert.ToInt32(stringValue.Trim(), NumberFormatInfo.InvariantInfo); }
            catch (Exception) { return defaultValue; }
        }

        internal static string NormalizeDirectoryPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            return path;
        }

        internal static double Lerp(double value1, double value2, double linearInterpolation)
        {
            return value1 * (1 - linearInterpolation) + value2 * linearInterpolation;
        }

        internal static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        internal static T RandomFrom<T>(params T[] array)
        {
            if (array == null || array.Length == 0) return default;
            return array[Rnd.Next(array.Length)];
        }

        internal static T RandomFrom<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[Rnd.Next(list.Count)];
        }

        internal static bool RandomChance(int oneOutOf)
        { return Rnd.Next(oneOutOf) == 0; }

        internal static int RandomInt()
        { return Rnd.Next(); }

        internal static int RandomInt(int max)
        { return Rnd.Next(max); }

        internal static int RandomInt(int min, int max)
        { return Rnd.Next(min, max); }

        internal static int RandomMinMax(int min, int max)
        { return Rnd.Next(min, max + 1); }

        internal static double RandomDouble()
        { return Rnd.NextDouble(); }

        internal static double RandomDouble(double max)
        { return Rnd.NextDouble() * max; }

        internal static double RandomDouble(double min, double max)
        { return Rnd.NextDouble() * (max - min) + min; }

        internal static string GetOrdinalAdjective(int number)
        {
            string numberStr = ValToString(number);

            if (numberStr.EndsWith("11") || numberStr.EndsWith("12") || numberStr.EndsWith("13")) return $"{number}th";
            if (numberStr.EndsWith("3")) return $"{number}rd";
            if (numberStr.EndsWith("2")) return $"{number}nd";
            if (numberStr.EndsWith("1")) return $"{number}st";

            return $"{number}th";
        }

        internal static byte[] ZipData(IDatabase database, string langKey, Dictionary<string, byte[]> FileEntries)
        {
            byte[] mizBytes;

            try
            {
                using MemoryStream ms = new();
                using (ZipArchive zip = new(ms, ZipArchiveMode.Update))
                {
                    foreach (string entryKey in FileEntries.Keys)
                    {
                        ZipArchiveEntry entry = zip.CreateEntry(entryKey, CompressionLevel.Optimal);
                        using BinaryWriter writer = new(entry.Open());
                        writer.Write(FileEntries[entryKey]);
                    }
                }

                mizBytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                throw new BriefingRoomException(database, langKey, "FailedToZip", ex);
            }

            return mizBytes;
        }

        internal static void SetMinMaxTheaterCoords(IDatabase database, ref DCSMission mission)
        {
            double minX = 123;
            double minY = 123;
            double maxX = 123;
            double maxY = 123;

            foreach (var area in mission.TheaterDB.WaterCoordinates)
            {
                foreach (var coord in area)
                {
                    GetMinMaxCoords(coord, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
            foreach (var area in mission.TheaterDB.WaterExclusionCoordinates)
            {
                foreach (var coord in area)
                {
                    GetMinMaxCoords(coord, ref minX, ref minY, ref maxX, ref maxY);
                }
            }
            foreach (var sp in mission.TheaterDB.SpawnPoints)
            {
                GetMinMaxCoords(sp.Coordinates, ref minX, ref minY, ref maxX, ref maxY);
            }
            var theaterDCSID = mission.TheaterDB.DCSID.ToLower();
            var situations = database.GetAllEntries<DBEntrySituation>()
                    .Where(x => x.Theater == theaterDCSID)
                    .ToList();
            foreach (var situation in situations)
            {
                foreach (var zones in situation.RedZones.Concat(situation.BlueZones).Concat(situation.NoSpawnZones))
                {
                    foreach (var coord in zones)
                    {  
                        GetMinMaxCoords(coord, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }
            }
            mission.SetValue("TheaterMinX", Math.Floor(minX / 1000) * 1000);
            mission.SetValue("TheaterMinY", Math.Floor(minY / 1000) * 1000);
            mission.SetValue("TheaterMaxX", Math.Ceiling(maxX / 1000) * 1000);
            mission.SetValue("TheaterMaxY", Math.Ceiling(maxY / 1000) * 1000);
        }

        private static void GetMinMaxCoords(Coordinates coord, ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (minX == 123)
            {
                minX = coord.X;
                minY = coord.Y;
                maxX = coord.X;
                maxY = coord.Y;
            }
            if (coord.X < minX)
                minX = coord.X;
            if (coord.Y < minY)
                minY = coord.Y;
            if (coord.X > maxX)
                maxX = coord.X;
            if (coord.Y > maxY)
                maxY = coord.Y;
        }

        internal static void CheckObjectiveProgressionLogic(IDatabase database, MissionTemplateRecord template, string langKey)
        {
            var tasks = new List<MissionTemplateSubTaskRecord>();
            template.Objectives.ForEach(x => { tasks.Add(x); tasks.AddRange(x.SubTasks);});
            tasks.Select((value, i) => new { i, value }).ToList().ForEach(x => CheckProgressionLogic(database, x.value, x.i + 1, tasks, langKey));
        }

        internal static void CheckProgressionLogic(IDatabase database, MissionTemplateSubTaskRecord task, int taskIndex,  List<MissionTemplateSubTaskRecord> tasks,  string langKey)
        {
            if(!task.ProgressionActivation)
                return;
            if(!string.IsNullOrEmpty(task.ProgressionOverrideCondition) && !Regex.IsMatch(task.ProgressionOverrideCondition, @"^([\(\)\d+]| and | or )+$")) 
                throw new BriefingRoomException(database, langKey, "InvalidProgressionOverrideCondition", taskIndex , task.ProgressionOverrideCondition);
            CheckProgressionDeps(database, tasks, task, taskIndex, taskIndex, langKey);
        }

        internal static void CheckProgressionDeps(IDatabase database, List<MissionTemplateSubTaskRecord> tasks, MissionTemplateSubTaskRecord task, int taskIndex, int searchIndex, string langKey, List<int> seenTaskIndexes = null)
        {
            if(seenTaskIndexes is null)
                seenTaskIndexes = new List<int>();
            if(seenTaskIndexes.Contains(taskIndex))
                return; // Skip paths we have already seen

            var dependentTasks = string.IsNullOrEmpty(task.ProgressionOverrideCondition) ? task.ProgressionDependentTasks.Select(x => x + 1).ToList() : Regex.Matches(task.ProgressionOverrideCondition, @"\d+").Select(x => int.Parse(x.Value)).ToList();

            if(dependentTasks.Count == 0)
                return;
            if(dependentTasks.Contains(searchIndex))
            {
                throw new BriefingRoomException(database, langKey, "InvalidProgressionDependentTasks", searchIndex , taskIndex);
            }

            seenTaskIndexes.Add(taskIndex);
            dependentTasks.ForEach(x => CheckProgressionDeps(database, tasks, tasks[x -1], x, searchIndex, langKey, seenTaskIndexes));
        }

        [GeneratedRegex("\\[\\\"(.*?)\\\"\\] ?= ?(.*)")]
        private static partial Regex LuaDictRegex();
    }
}
