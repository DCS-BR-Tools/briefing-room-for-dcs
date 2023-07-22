using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class Situation
    {   
        public Dictionary<string,string> DisplayName { get; set; } = new Dictionary<string,string>{{"EN", "<NAME HERE>"}};
        public string Theater { get; set; }
        public List<Dictionary<string,string>> BriefingDescriptions { get; set; } = new List<Dictionary<string,string>>();
        public List<string> RelatedSituations { get; set; } = new List<string>();
        public List<List<List<double>>> redZones { get; set; }
        public List<List<List<double>>> blueZones { get; set; }
        public List<List<List<double>>> noSpawnZones { get; set; }
    }
}
