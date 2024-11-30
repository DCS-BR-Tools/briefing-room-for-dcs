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
using System.Globalization;
using System.Linq;
using System.Text;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS
{
    public class INIFile
    {
        private readonly Dictionary<string, INIFileSection> Sections = new(StringComparer.InvariantCultureIgnoreCase);

        internal INIFile() { Clear(); }

        private string FilePath { get; init; }
        internal INIFile(string filePath, Encoding encoding = null)
        {
            Clear();
            FilePath = filePath;
            string iniString = File.ReadAllText(filePath, encoding ?? Encoding.UTF8);
            ParseINIString(iniString);
        }


        public static INIFile CreateFromRawINIString(string iniString)
        {
            INIFile ini = new();
            ini.Clear();
            ini.ParseINIString(iniString);
            return ini;
        }

        internal void Clear()
        {
            foreach (string s in Sections.Keys)
                Sections[s].Clear();

            Sections.Clear();
        }

        internal bool SaveToFile(string filePath, Encoding encoding = null)
        {
            try
            {
                File.WriteAllText(filePath, GetFileData(), encoding ?? Encoding.UTF8);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal string GetFileData()
        {
            string fileContent = "";

            foreach (string s in Sections.Keys)
            {
                if (string.IsNullOrEmpty(s)) continue;
                fileContent += $"[{s}]\r\n";

                foreach (string v in Sections[s].Keys)
                {
                    if (string.IsNullOrEmpty(v)) continue;
                    if (string.IsNullOrEmpty(Sections[s][v])) continue;

                    fileContent += $"{v}={Sections[s][v]}\r\n";
                }

                fileContent += "\r\n";
            }
            return fileContent;
        }


        internal string[] GetSections(bool showAbstractSections = false)
        {
            if (showAbstractSections)
                return Sections.Keys.OrderBy(x => x).ToArray();

            return (from string s in Sections.Keys where !Sections[s].Abstract select s).OrderBy(x => x).ToArray();
        }

        public T GetValue<T>(string section, string key, T defaultValue = default)
        {
            if (!ValueExists(section, key))
            {
                if ((defaultValue == null) && (typeof(T) == typeof(string))) return (T)Convert.ChangeType("", typeof(T));
                return defaultValue;
            }

            string val = ReadValue(section, key) ?? "";

            try
            {
                if (CanConvertStringTo<T>())
                    return ConvertStringTo(val, defaultValue);
                else
                    return default;
            }
            catch (Exception e)
            {
                throw new BriefingRoomException("en", $"Failed to parse value {FilePath}: {typeof(T).FullName} => {string.Join(",", val)}", e);
            }
        }

        internal LanguageString GetLangStrings(string section, string key, string langKey)
        {
            var resultMap = new LanguageString();
            var value = GetValue<string>(section, $"{key}", "");
            if (!string.IsNullOrEmpty(value))
                resultMap.AddIfKeyUnused(langKey, value);
            return resultMap;
        }

        internal LanguageString GetLangStrings(DatabaseLanguage langDB, string classId, string id, string section, string key)
        {
            var resultMap = new LanguageString();
            foreach (var langKey in BriefingRoom.AvailableLanguagesMap.Keys)
            {
                var value = GetValue<string>(section, $"{key}{(langKey != "en" ? $".{langKey}" : "")}", "");
                if (!string.IsNullOrEmpty(value))
                    resultMap.AddIfKeyUnused(langKey, value);
            }
            resultMap.AddOverrides(langDB, classId, id, section, key);

            return resultMap;
        }


        internal LanguageString AddLangStrings(string section, string key, LanguageString resultMap, string langKey)
        {
            var value = GetValue<string>(section, key, "");
            if (!string.IsNullOrEmpty(value))
                resultMap.AddIfKeyUnused(langKey, value);
            return resultMap;
        }
        internal T[] GetValueArray<T>(string section, string key, char separator = ',')
        {
            if (string.IsNullOrEmpty(GetValue<string>(section, key))) return Array.Empty<T>();

            object val = ReadValue(section, key) ?? "";

            try
            {
                if (typeof(T) == typeof(string)) val = val.ToString().Split(separator);
                else if (typeof(T) == typeof(bool)) val = ConvertArray<bool>(val.ToString().Split(separator));
                else if (typeof(T) == typeof(int)) val = ConvertArray<int>(val.ToString().Split(separator));
                else if (typeof(T) == typeof(float)) val = ConvertArray<float>(val.ToString().Split(separator));
                else if (typeof(T) == typeof(double)) val = ConvertArray<double>(val.ToString().Split(separator));
                else if (typeof(T).IsEnum) val = ConvertArray<T>(val.ToString().Split(separator));
                //else if (CanConvertStringFrom<T>()) val = ConvertArray<T>(val.ToString().Split(separator));

                return (T[])Convert.ChangeType(val, typeof(T[]));
            }
            catch (Exception)
            {
                throw new BriefingRoomException("en", $"Failed to parse value {FilePath}: {typeof(T).FullName} => {string.Join(",", val)}");
            }
        }

        internal List<T> GetValueList<T>(string section, string key, char separator = ',') => GetValueArray<T>(section, key, separator).ToList();
        internal List<T> GetValueDistinctList<T>(string section, string key, char separator = ',') => GetValueArray<T>(section, key, separator).Distinct().ToList();

        internal T GetValueArrayAsEnumFlags<T>(string section, string key, char separator = ',') where T : Enum
        {
            T[] enumArray = GetValueArray<T>(section, key, separator).Distinct().ToArray();

            int enumVal = 0;
            foreach (T e in enumArray)
                enumVal |= Convert.ToInt32(e);

            return (T)(object)enumVal;
        }

        internal T[] GetValueArrayAsMinMaxEnum<T>(string section, string key) where T : Enum
        {
            T[] minMaxEnum = GetValueArray<T>(section, key);
            if (minMaxEnum.Length < 2) return new T[] { Toolbox.GetEnumValues<T>().First(), Toolbox.GetEnumValues<T>().Last() };
            return new T[] { (T)(object)Math.Min((int)(object)minMaxEnum[0], (int)(object)minMaxEnum[1]), (T)(object)Math.Max((int)(object)minMaxEnum[0], (int)(object)minMaxEnum[1]) };
        }

        internal void SetValue<T>(string section, string key, T value)
        {
            if (CanConvertStringFrom<T>())
                WriteValue(section, key, ConvertStringFrom(value));
            else
                WriteValue(section, key, "");
        }

        internal void SetValueArray<T>(string section, string key, T[] value, char separator = ',')
        {
            value ??= Array.Empty<T>();
            object oVal = value;

            if (typeof(T) == typeof(string))
                WriteValue(section, key, string.Join(separator.ToString(), (string[])oVal));
            else if (typeof(T).IsEnum)
                WriteValue(section, key, string.Join(separator.ToString(), from T e in (T[])oVal select e.ToString()));
            else
                WriteValue(section, key, value.ToString());
        }

        internal string this[string section, string key]
        {
            get
            {
                return ReadValue(section, key);
            }
            set
            {
                WriteValue(section, key, value);
            }
        }

        internal string[] GetKeysInSection(string section, bool ignoreLangs = false)
        {
            if (!Sections.ContainsKey(section)) return Array.Empty<string>();

            List<string> keys = new();
            keys.AddRange(Sections[section].Keys);

            List<string> checkedSections = new();

            while (
                !string.IsNullOrEmpty(Sections[section].ParentSection) &&
                !checkedSections.Contains(section.ToLower()) &&
                Sections.ContainsKey(Sections[section].ParentSection))
            {
                checkedSections.Add(section.ToLower()); // To avoid circular inheritances
                section = Sections[section].ParentSection;
                keys.AddRange(Sections[section].Keys);
            }
            if (ignoreLangs)
                keys = keys.Where(x => !BriefingRoom.AvailableLanguagesMap.Keys.Any(y => x.EndsWith($".{y}"))).ToList();
            return keys.Distinct().OrderBy(x => x).ToArray();
        }

        internal string[] GetTopLevelKeysInSection(string section)
        {
            string[] keys = GetKeysInSection(section);

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Contains('.'))
                    keys[i] = keys[i][..keys[i].IndexOf(".")];
            }

            return keys.Distinct().ToArray();
        }

        internal bool ValueExists(string section, string key)
        {
            return ReadValue(section, key) != null;
        }

        private string ReadValue(string section, string key)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key)) return null;

            List<string> checkedSections = new();

            while (Sections.ContainsKey(section))
            {
                if (Sections[section].ContainsKey(key))
                    return Sections[section][key];

                if (string.IsNullOrEmpty(Sections[section].ParentSection))
                    return null;

                checkedSections.Add(section.ToLower()); // To avoid circular inheritances
                if (checkedSections.Contains(Sections[section].ParentSection.ToLower()))
                    return null;

                section = Sections[section].ParentSection;
            }

            return null;
        }

        private bool WriteValue(string section, string key, string value)
        {
            section = (section ?? "").ToLower().Trim();
            key = (key ?? "").ToLower().Trim();
            value ??= "";
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key)) return false;

            if (!Sections.ContainsKey(section))
                Sections.Add(section, new INIFileSection());

            Sections[section][key] = value;

            return true;
        }

        private void ParseINIString(string iniString)
        {
            string[] lines = (iniString + "\n").Replace("\r\n", "\n").Split('\n');

            Clear();

            string section = null;

            foreach (string li in lines)
            {
                string l = li.Trim(' ', '\t'); // Trim line
                if (l.StartsWith(";")) continue; // Line is a comment

                if (l.StartsWith("[")) // found a new section
                {
                    // try to get section name, make sure it's valid
                    section = l.Trim('[', ']', ' ', '\t', ':').ToLower();
                    string parentSection = null;

                    if (section.Contains(':')) // Sections inherits another section, name declared in the [SECTION:PARENT_SECTION] format
                    {
                        try
                        {
                            string sectionWithParent = section;
                            section = sectionWithParent.Split(':')[0].Trim();
                            parentSection = sectionWithParent.Split(':')[1].Trim();
                        }
                        catch (Exception)
                        {
                            section = l.Trim('[', ']', ' ', '\t', ':').ToLower();
                            parentSection = null;
                        }
                    }

                    bool abstractSection = section.StartsWith("_");

                    if (string.IsNullOrEmpty(section)) { section = null; continue; }

                    if (!Sections.ContainsKey(section))
                        Sections.Add(section, new INIFileSection(abstractSection, parentSection));

                    continue;
                }

                if (l.Contains('=')) // The line contains an "equals" sign, it means we found a value
                {
                    if (section == null) continue; // we're not in a section, ignore

                    string[] v = l.Split(new char[] { '=' }, 2); // Split the line at the first "equal" sign: key = value
                    if (v.Length < 2) continue;

                    string key = v[0].Trim().Trim('"').ToLower();
                    string value = v[1].Trim().Trim('"');
                    WriteValue(section, key, value);
                }
            }
        }

        private static bool CanConvertStringTo<T>()
        {
            Type type = typeof(T);

            if (
                (type == typeof(bool)) ||
                (type == typeof(double)) ||
                (type == typeof(float)) ||
                (type == typeof(int)) ||
                (type == typeof(string)) ||
                (type == typeof(Coordinates)) ||
                (type == typeof(double[])) ||
                (type == typeof(MinMaxD)) ||
                (type == typeof(MinMaxI)) ||
                type.IsEnum)
                return true;

            return false;
        }

        private static bool CanConvertStringFrom<T>()
        {
            Type type = typeof(T);

            if (
                (type == typeof(bool)) ||
                (type == typeof(double)) ||
                (type == typeof(float)) ||
                (type == typeof(int)) ||
                (type == typeof(string)) ||
                (type == typeof(Coordinates)) ||
                (type == typeof(double[])) ||
                type.IsEnum)
                return true;

            return false;
        }

        private static T ConvertStringTo<T>(string value, T defaultValue = default)
        {
            Type type = typeof(T);

            object outObject = defaultValue;
            if (string.IsNullOrEmpty(value))
                return (T)outObject;
            if (type == typeof(bool))
                outObject = Convert.ToBoolean(value, NumberFormatInfo.InvariantInfo);
            else if (type == typeof(double))
                outObject = Convert.ToDouble(value, NumberFormatInfo.InvariantInfo);
            else if (type == typeof(float))
                outObject = Convert.ToSingle(value, NumberFormatInfo.InvariantInfo);
            else if (type == typeof(int))
                outObject = Convert.ToInt32(value, NumberFormatInfo.InvariantInfo);
            else if (type == typeof(string))
                outObject = value;
            else if (type.IsEnum)
                outObject = Enum.Parse(typeof(T), value, true);
            else if (type == typeof(Coordinates))
            {
                object obj = new Coordinates(value);
                return (T)obj;
            }
            else if (type == typeof(double[]))
            {
                object obj = new Coordinates(value).ToArray();
                return (T)obj;
            }
            else if (type == typeof(MinMaxD))
            {
                string[] minMaxStr = value.Split(',');
                object obj = new MinMaxD(Convert.ToDouble(minMaxStr[0].Trim(), NumberFormatInfo.InvariantInfo), Convert.ToDouble(minMaxStr[1].Trim(), NumberFormatInfo.InvariantInfo));
                return (T)obj;
            }
            else if (type == typeof(MinMaxI))
            {
                string[] minMaxStr = value.Split(',');
                object obj = new MinMaxI(Convert.ToInt32(minMaxStr[0].Trim(), NumberFormatInfo.InvariantInfo), Convert.ToInt32(minMaxStr[1].Trim(), NumberFormatInfo.InvariantInfo));
                return (T)obj;
            }

            return (T)outObject;
        }

        private static string ConvertStringFrom<T>(T value)
        {
            Type type = typeof(T);

            object inObject = value;
            string outString = value.ToString();

            if (type == typeof(bool))
                outString = ((bool)inObject).ToString(NumberFormatInfo.InvariantInfo);
            else if (type == typeof(double))
                outString = ((double)inObject).ToString(NumberFormatInfo.InvariantInfo);
            else if (type == typeof(float))
                outString = ((float)inObject).ToString(NumberFormatInfo.InvariantInfo);
            else if (type == typeof(int))
                outString = ((int)inObject).ToString(NumberFormatInfo.InvariantInfo);
            else if (type == typeof(Coordinates))
                outString = ((Coordinates)inObject).ToString();
            else if (type == typeof(double[]))
                outString = string.Join(",", ((double[])inObject).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)));

            return outString;
        }

        private T[] ConvertArray<T>(string[] sourceArray)
        {
            try
            {
                T[] arr = new T[sourceArray.Length];

                for (int i = 0; i < sourceArray.Length; i++)
                {
                    if (CanConvertStringTo<T>())
                        arr[i] = ConvertStringTo<T>(sourceArray[i]);
                    else
                        arr[i] = default;
                }

                return arr;
            }
            catch (Exception e)
            {
                throw new BriefingRoomException("en", $"Failed to parse value {FilePath}: {typeof(T).FullName} => {string.Join(",", sourceArray)}", e);
            }
        }
    }
}
