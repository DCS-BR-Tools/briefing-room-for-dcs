using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class Category
    {
        public string name { get; set; }
    }

    public class Ship : Unit
    {
        public List<Category> categories { get; set; }
        public int helicopterStorage { get; set; }
        public int planeStorage { get; set; }
    }
}
