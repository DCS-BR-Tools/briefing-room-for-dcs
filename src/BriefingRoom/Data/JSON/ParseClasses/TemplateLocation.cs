using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class TheaterTemplateLocation
    {
        public List<double> coords { get; set; }
        public List<TemplateUnit> units { get; set; }
        public string locationType { get; set; }
    }
}
