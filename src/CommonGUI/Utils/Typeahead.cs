using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace BriefingRoom4DCS.GUI.Utils
{
    public class Typeahead
    {

        public static Task<List<DatabaseEntryInfo>> SearchDB(IBriefingRoom briefingRoom,  DatabaseEntryType entryType, string searchText, string parameter = "")
        {
            var list = briefingRoom.GetDatabaseEntriesInfo(entryType, parameter);
            return Task.FromResult(list.Where(x => x.Name.Get(briefingRoom.LanguageKey).ToLower().Contains(searchText.ToLower())).ToList());
        }

        public static string GetDBDisplayName(IBriefingRoom briefingRoom,  DatabaseEntryType entryType, string id)
        {
            if (String.IsNullOrEmpty(id))
                return briefingRoom.Translate("Random");
            return briefingRoom.GetDatabaseEntriesInfo(entryType).First(x => x.ID == id).Name.Get(briefingRoom.LanguageKey);
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