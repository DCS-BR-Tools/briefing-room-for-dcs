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

using System.Collections.Generic;

namespace BriefingRoom4DCS.Data
{
    public interface IDatabase
    {
        DatabaseCommon Common { get; set; }
        DatabaseLanguage Language { get; set; }

        void Reset();

        bool EntryExists<T>(string id) where T : DBEntry, new();

        string[] GetAllEntriesIDs<T>() where T : DBEntry, new();

        T[] GetAllEntries<T>() where T : DBEntry, new();

        ST[] GetAllEntries<T, ST>()
            where T : DBEntry, new()
            where ST : DBEntry;

        T GetEntry<T>(string id) where T : DBEntry, new();

        ST GetEntry<T, ST>(string id)
            where T : DBEntry, new()
            where ST : DBEntry;
        T[] GetEntries<T>(params string[] ids) where T : DBEntry, new();
        List<T> GetEntries<T>(List<string> ids) where T : DBEntry, new();

        string CheckID<T>(string id, string defaultID = null, bool allowEmptyStr = false, List<string> allowedValues = null) where T : DBEntry, new();
        string[] CheckIDs<T>(params string[] ids) where T : DBEntry, new();
    }
}
