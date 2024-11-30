using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{

    public class Layout
    {
        public List<string> categories { get; set; }
        public string name { get; set; }
        public int minUnits { get; set; }
        public List<LayoutUnit> units { get; set; }
    }

    public class LayoutUnit
    {
        public double dx { get; set; }
        public double dy { get; set; }
        public double heading { get; set; }
    }


}