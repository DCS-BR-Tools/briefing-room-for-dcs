using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace BriefingRoom4DCS.Data.JSON
{
    public class Template
    {
        public string name { get; set; }
        public List<int> operational { get; set; }
        public Dictionary<string, List<int>> Operators { get; set; } = new Dictionary<string, List<int>>();
        public bool lowPolly { get; set; }
        public List<string> requiredModules { get; set; }
        public List<TemplateUnit> units { get; set; }
    }


    public class TemplateUnit
    {
        public double heading { get; set; }
        public List<double> coords { get; set; }
        public List<string> unitFamilies { get; set; }
        public string originalType { get; set; }
        public bool isScenery { get; set; } = false;
        public bool isSpecificType { get; set; } = false;
    }
}
