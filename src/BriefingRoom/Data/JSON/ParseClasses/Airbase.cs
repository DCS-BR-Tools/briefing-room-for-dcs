using System.Collections.Generic;


namespace BriefingRoom4DCS.Data.JSON
{
    public class AirdromeData
    {
        public List<string> runways { get; set; }
        public List<int> ATC { get; set; }
        public List<int> TACAN { get; set; }
        public List<int> ILS { get; set; }
    }



    public class Parking
    {
        public int Term_Index { get; set; }
        public Pos pos { get; set; }
        public string Term_Type_Name { get; set; }
        public VTerminalPos vTerminalPos { get; set; }
        public bool TO_AC { get; set; }
        public int Term_Index_0 { get; set; }
        public int Term_Type { get; set; }
        public double fDistToRW { get; set; }
    }



    public class Position
    {
        public double y { get; set; }
        public double x { get; set; }
        public double z { get; set; }
    }

    public class Airbase : MongoBase
    {
        public int ID { get; set; }
        public string displayName { get; set; }
        public List<Parking> parking { get; set; }
        public Pos pos { get; set; }
        public List<Runway> runways { get; set; }
        public string theatre { get; set; }
        public string typeName { get; set; }
        public AirdromeData airdromeData { get; set; }
        public List<Stand> stands { get; set; } = new List<Stand>();
    }

    public class Runway
    {
        public double course { get; set; }
        public int Name { get; set; }
        public Position position { get; set; }
        public double length { get; set; }
        public int width { get; set; }
        public string name { get; set; }
    }

    public class VTerminalPos
    {
        public double y { get; set; }
        public double x { get; set; }
        public double z { get; set; }
    }

    public class StandParams
    {
        public string SHELTER { get; set; }
        public string FOR_HELICOPTERS { get; set; }
        public string LENGTH { get; set; }
        public string HEIGHT { get; set; }
        public string WIDTH { get; set; }
        public string FOR_AIRPLANES { get; set; }
    }

    public class Stand
    {
        public double y { get; set; }
        public double x { get; set; }
        public string name { get; set; }
        public int flag { get; set; }
        public int crossroad_index { get; set; }
        public StandParams @params { get; set; }
    }




}
