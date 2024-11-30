using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class Car : Unit
    {
        public string category { get; set; }
        public Dictionary<string, List<string>> paintSchemes { get; set; }

    }
}