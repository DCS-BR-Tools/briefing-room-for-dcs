using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace BriefingRoom4DCS.Data.JSON
{
    public class Template
    {
        public string _id { get; set; }

        [JsonProperty("@dcsversion")]
        public string dcsversion { get; set; }

        [JsonProperty("@created")]
        public DateTime created { get; set; }
        public string country { get; set; }
        public string name { get; set; }
        public bool sys { get; set; }
        public string type { get; set; }
        public List<TemplateUnit> units { get; set; }
    }

    public class TemplateUnit
    {
        public double dx { get; set; }
        public double dy { get; set; }
        public string name { get; set; }
        public string skill { get; set; }
        public double heading { get; set; }
    }

    public class TemplateBRInfo
    {
        public string type { get; set; }
        public string family { get; set; }
        public List<int> operational { get; set; } = new List<int>{1940, 2020};
        public Dictionary<string, List<int>> extraOperators { get; set; } = new Dictionary<string, List<int>>();
        public bool lowPolly { get; set; }
        public string module { get; set; }
    }
}
