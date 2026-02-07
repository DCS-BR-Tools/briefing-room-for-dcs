using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class TheaterTemplateUnitLocation
    {
        public double heading { get; set; }
        public List<double> coords { get; set; }
        public List<string> unitTypes { get; set; }
        public string originalType { get; set; }
        public string specificType { get; set; }
        public bool isScenery { get; set; } = false;
    }

    public class TheaterTemplateLocation
    {
        public List<double> coords { get; set; }
        public List<TheaterTemplateUnitLocation> locations { get; set; }
        public string locationType { get; set; }
    }
}
