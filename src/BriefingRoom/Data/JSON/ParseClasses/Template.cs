using System.Collections.Generic;

namespace BriefingRoom4DCS.Data.JSON
{
    public class Template
    {
        public string name { get; set; }
        public List<TemplateGroup> groups { get; set; }
        public bool immovable { get; set; } = false;
    }

    public class TemplateGroup
    {
        public List<double> coords { get; set; }
        public List<TemplateUnit> units { get; set; } = new List<TemplateUnit>();
    }

    public class TemplateUnit
    {
        public double heading { get; set; }
        public List<double> coords { get; set; }
        public List<string> unitFamilies { get; set; } = new List<string>();
        public string originalType { get; set; }
        public bool isScenery { get; set; } = false;
        public bool isSpecificType { get; set; } = false;
    }
}
