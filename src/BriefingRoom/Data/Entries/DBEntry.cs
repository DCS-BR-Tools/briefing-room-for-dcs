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
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    public abstract class DBEntry
    {
        protected IDatabase Database { get; set; }

        internal string ID { get; set; }

        internal LanguageString UIDisplayName { get; set; }

        internal LanguageString UICategory { get; set; }

        internal LanguageString UIDescription { get; set; }

        internal DBEntry(IDatabase database)
        {
            Database = database;
        }

        internal DBEntry()
        {
        }

        internal virtual DatabaseEntryInfo GetDBEntryInfo()
        {
            return new DatabaseEntryInfo(ID, UIDisplayName, UICategory, UIDescription);
        }

        internal bool Load(IDatabase database, string id, string iniFilePath)
        {
            Database = database;
            ID = id;
            var ini = new INIFile(iniFilePath);
            var className = this.GetLanguageClassName();
            UIDisplayName = ini.GetLangStrings(Database.Language, className, ID, "GUI", "DisplayName");
            if (UIDisplayName.Count == 0) UIDisplayName = new LanguageString(Database.Language, className, ID, "DisplayName", ID);
            UICategory = ini.GetLangStrings(Database.Language, className, ID, "GUI", "Category");
            UIDescription = ini.GetLangStrings(Database.Language, className, ID, "GUI", "Description");

            return OnLoad(iniFilePath);
        }

        protected static void missingDCSDataWarnings<T>(Dictionary<string, T> supportData, Dictionary<string, DBEntry> itemMap, string Name)
        {
            var missingDCSData = supportData.Where(x => !itemMap.ContainsKey(x.Key)).Select(x => x.Key).ToList();
            if (missingDCSData.Count > 0)
            {
                BriefingRoom.PrintToLog($"{Name} DCS Data missing for: {string.Join(',', missingDCSData)}", LogMessageErrorLevel.Warning);
            }
        }

        protected abstract bool OnLoad(string iniFilePath);

        protected string[] GetValidDBEntryIDs<T>(string[] values, out string[] rejected) where T : DBEntry, new()
        {
            values ??= Array.Empty<string>(); // Make sure values is not null

            // Accepted values IDs are neither null nor empty AND exist in the database
            string[] accepted =
                (from string v in values
                 where !string.IsNullOrEmpty(v.Trim()) && Database.EntryExists<T>(v.Trim())
                 select v.Trim()).ToArray();

            // Accepted values IDs are neither null nor empty but DO NOT exist in the database
            rejected =
                (from string v in values
                 where !string.IsNullOrEmpty(v.Trim()) && !Database.EntryExists<T>(v.Trim())
                 select v.Trim()).ToArray();

            return accepted;
        }

        protected string[] GetValidDBEntryIDs<T>(List<string> values, out string[] rejected) where T : DBEntry, new()
        {
            return GetValidDBEntryIDs<T>(values.ToArray(), out rejected);
        }

        internal virtual void Merge(DBEntry entry)
        {
            throw new NotImplementedException();
        }

        public string GetLanguageClassName()
        {
            return DBEntry.GetLanguageClassName(this.GetType());
        }

        public static string GetLanguageClassName(Type type)
        {
            return type.Name.Replace("DBEntry", "");
        }
    }
}
