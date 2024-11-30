/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Data
{
    public struct DBEntryAirbaseParkingSpot
    {
        internal int DCSID { get; private set; }
        internal Coordinates Coordinates { get; private set; }
        internal ParkingSpotType ParkingType { get; private set; }
        internal double Height { get; private set; }
        internal double Length { get; private set; }
        internal double Width { get; private set; }

        public DBEntryAirbaseParkingSpot() { }


        internal static DBEntryAirbaseParkingSpot[] LoadJSON(List<Parking> data, string airbaseId)
        {
            return data.Select(p =>
            {
                var parkingSpotType = ParkingSpotType.Unknown;
                try
                {
                    parkingSpotType = (ParkingSpotType)Enum.Parse(typeof(ParkingSpotType), p.Term_Type_Name, true);
                }
                catch (System.Exception)
                {
                    BriefingRoom.PrintToLog($"Failed to parse parking type: {p.Term_Type_Name} (airbase: {airbaseId}, id: {p.Term_Index})", LogMessageErrorLevel.Warning);
                }
                return new DBEntryAirbaseParkingSpot
                {
                    DCSID = p.Term_Index,
                    Coordinates = new Coordinates(p.pos.DCS.x, p.pos.DCS.z),
                    ParkingType = parkingSpotType
                };
            }).ToArray();
        }

        internal static DBEntryAirbaseParkingSpot[] LoadJSON(List<Stand> data, string airbaseId)
        {
            return data.Select(stand =>
            {
                var parkingSpotType = ParkingSpotType.Unknown;
                try
                {
                    if (stand.@params.SHELTER == "1")
                        parkingSpotType = ParkingSpotType.HardenedAirShelter;
                    else if (stand.@params.FOR_HELICOPTERS == "1" && stand.@params.FOR_AIRPLANES == "1")
                        parkingSpotType = ParkingSpotType.OpenAirSpawn;
                    else if (stand.@params.FOR_HELICOPTERS == "1")
                        parkingSpotType = ParkingSpotType.HelicopterOnly;
                    else if (stand.@params.FOR_AIRPLANES == "1")
                        parkingSpotType = ParkingSpotType.AirplaneOnly;

                }
                catch (System.Exception)
                {
                    BriefingRoom.PrintToLog($"Failed to parse parking type: {stand.name} (airbase: {airbaseId}, id: {stand.crossroad_index})", LogMessageErrorLevel.Warning);
                }
                return new DBEntryAirbaseParkingSpot
                {
                    DCSID = stand.crossroad_index,
                    Coordinates = new Coordinates(stand.x, stand.y),
                    ParkingType = parkingSpotType,
                    Height = string.IsNullOrEmpty(stand.@params.HEIGHT) ? 500 : Double.Parse(stand.@params.HEIGHT),
                    Length = Double.Parse(stand.@params.LENGTH),
                    Width = Double.Parse(stand.@params.WIDTH)
                };
            }).ToArray();
        }
    }


}
