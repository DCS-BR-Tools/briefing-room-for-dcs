using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace BriefingRoom4DCS.GUI.Utils
{
    public class Typeahead
    {

        public static Task<List<DatabaseEntryInfo>> SearchDB(string langKey, DatabaseEntryType entryType, string searchText, string parameter = "")
        {
            var list = BriefingRoom4DCS.BriefingRoom.GetDatabaseEntriesInfo(langKey, entryType, parameter);
            return Task.FromResult(list.Where(x => x.Name.Get(langKey).ToLower().Contains(searchText.ToLower())).ToList());
        }

        public static string GetDBDisplayName(string langKey, DatabaseEntryType entryType, string id)
        {
            if (String.IsNullOrEmpty(id))
                return BriefingRoom4DCS.BriefingRoom.Translate(langKey, "Random");
            return BriefingRoom4DCS.BriefingRoom.GetDatabaseEntriesInfo(langKey, entryType).First(x => x.ID == id).Name.Get(langKey);
        }

        public static string ConvertDB(DatabaseEntryInfo entry) => entry.ID;
        public static string ConvertDBL(DatabaseEntryInfo entry, List<int> ids) => "";

        public static async Task<List<T>> SearchEnum<T>(string searchText)
        {
            var list = new List<T>((T[])Enum.GetValues(typeof(T)));
            return await Task.FromResult(list.Where(x => x.ToString().ToLower().Contains(searchText.ToLower())).ToList());
        }
    }
}