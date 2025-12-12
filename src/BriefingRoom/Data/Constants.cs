using System.Collections.Generic;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS.Data
{
    internal class Constants
    {
        internal static readonly List<UnitFamily> SINGLE_TYPE_FAMILIES = new() { UnitFamily.VehicleMissile, UnitFamily.VehicleArtillery };
        internal static readonly List<DBEntryObjectiveTargetBehaviorLocation> AIRBASE_LOCATIONS = new()
        {
            DBEntryObjectiveTargetBehaviorLocation.Airbase,
            DBEntryObjectiveTargetBehaviorLocation.AirbaseParking,
            DBEntryObjectiveTargetBehaviorLocation.AirbaseParkingNoHardenedShelter,
        };

        internal static readonly List<DBEntryObjectiveTargetBehaviorLocation> AIR_ON_GROUND_LOCATIONS = new()
        {
            DBEntryObjectiveTargetBehaviorLocation.AirbaseParking,
            DBEntryObjectiveTargetBehaviorLocation.AirbaseParkingNoHardenedShelter
        };

        internal static readonly List<SpawnPointType> LAND_SPAWNS = new()
        {
            SpawnPointType.LandSmall,
            SpawnPointType.LandMedium,
            SpawnPointType.LandLarge,
        };

        internal static readonly List<List<UnitFamily>> MIXED_INFANTRY_SETS = new()
        {
            new List<UnitFamily> {UnitFamily.Infantry},
            new List<UnitFamily> {UnitFamily.Infantry},
            new List<UnitFamily> {UnitFamily.Infantry},
            new List<UnitFamily> {UnitFamily.Infantry, UnitFamily.Infantry, UnitFamily.InfantryMANPADS},
            new List<UnitFamily> {UnitFamily.Infantry, UnitFamily.Infantry, UnitFamily.InfantryMANPADS},
            new List<UnitFamily> {UnitFamily.Infantry, UnitFamily.Infantry, UnitFamily.InfantryMANPADS},
            new List<UnitFamily> {UnitFamily.Infantry, UnitFamily.InfantryMANPADS},
            new List<UnitFamily> {UnitFamily.Infantry, UnitFamily.InfantryMANPADS, UnitFamily.InfantryMANPADS},
            new List<UnitFamily> {UnitFamily.InfantryMANPADS},
        };

        internal static readonly List<List<UnitFamily>> MIXED_VEHICLE_SETS = new()
        {
            new List<UnitFamily> {UnitFamily.VehicleAPC},
            new List<UnitFamily> {UnitFamily.VehicleAPC, UnitFamily.VehicleAPC, UnitFamily.VehicleMBT},
            new List<UnitFamily> {UnitFamily.VehicleAPC, UnitFamily.VehicleAPC, UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleArtillery},
            new List<UnitFamily> {UnitFamily.VehicleArtillery,UnitFamily.VehicleArtillery,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleMBT},
            new List<UnitFamily> {UnitFamily.VehicleMBT,UnitFamily.VehicleMBT,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleMissile},
            new List<UnitFamily> {UnitFamily.VehicleMissile,UnitFamily.VehicleMissile,UnitFamily.VehicleAPC,UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleTransport},
            new List<UnitFamily> {UnitFamily.VehicleTransport,UnitFamily.VehicleTransport,UnitFamily.VehicleAPC,},
        };

        internal static readonly Dictionary<UnitFamily, TheaterTemplateLocationType> THEATER_TEMPLATE_LOCATION_MAP = new()
        {
            {UnitFamily.VehicleSAMMedium, TheaterTemplateLocationType.SAM},
            {UnitFamily.VehicleSAMLong, TheaterTemplateLocationType.SAM},
            {UnitFamily.VehicleStatic, TheaterTemplateLocationType.BASE},
        };

        internal static readonly List<UnitFamily> TEMPLATE_PREFERENCE_FAMILIES = new()
        {
            UnitFamily.StaticStructureMilitary,
            UnitFamily.StaticStructureProduction,
            UnitFamily.VehicleSAMLong,
            UnitFamily.VehicleSAMMedium
        };

        internal static readonly List<UnitFamily> TEMPLATE_ALWAYS_FAMILIES = new()
        {
            UnitFamily.VehicleSAMLong,
            UnitFamily.VehicleSAMMedium
        };

        internal static readonly List<UnitCategory> NEAR_FRONT_LINE_CATEGORIES = new() { UnitCategory.Static, UnitCategory.Vehicle, UnitCategory.Infantry };
        internal static readonly List<UnitFamily> LARGE_AIRCRAFT = new()
        {
            UnitFamily.PlaneAWACS,
            UnitFamily.PlaneTankerBasket,
            UnitFamily.PlaneTankerBoom,
            UnitFamily.PlaneTransport,
            UnitFamily.PlaneBomber,
        };

        internal static readonly string HTML_SLASH_BREAK = "/&#8203;";
    }
}