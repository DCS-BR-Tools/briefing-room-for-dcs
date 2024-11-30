using System;
using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class TerrainBounds
    {
        public List<List<List<double>>> waters { get; set; }
        public List<List<List<double>>> landMasses { get; set; }
    }

}
