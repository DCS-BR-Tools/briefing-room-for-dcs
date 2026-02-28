using System.Collections.Generic;

namespace BriefingRoom4DCS.Data.JSON
{
    public class HintResponse
    {
        public Dictionary<string, double[]> hints { get; set; }
        public List<List<List<double>>> combatZones { get; set; } = new List<List<List<double>>>();
        public List<List<double>> frontLine { get; set; } = new List<List<double>>();
    }

}
