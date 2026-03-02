using System.Collections.Generic;
using System.Linq;


namespace BriefingRoom4DCS.Data.JSON
{
    public class Car : Unit
    {
        public string category { get; set; }
        public Dictionary<string, List<List<string>>> paintSchemes { get; set; }

    }

    public class CarLegacy : Car
    {
        new public Dictionary<string, List<string>> paintSchemes { get; set; }
         
        public Car getCar()
        {
            return new Car
            {
                type = this.type,
                displayName = this.displayName,
                module = this.module,
                shape = this.shape,
                category = this.category,
                paintSchemes = this.paintSchemes.ToDictionary(pair => pair.Key, pair => pair.Value.Select(x => new List<string> { x, x }).ToList()),
                Operators = this.Operators
            };
        }
    }
}