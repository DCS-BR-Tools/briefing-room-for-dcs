using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;


namespace BriefingRoom4DCS.Data
{
    public readonly struct DBEntryTheaterTemplateUnitLocation
    {
        public double Heading { get; init; }
        public Coordinates Coordinates { get; init; }
        public List<UnitFamily> UnitTypes { get; init; }
        public string SpecificType { get; init; }
        public bool IsScenery { get; init; }
    }

    public readonly struct DBEntryTheaterTemplateLocation
    {
        public static readonly List<UnitFamily> CRITICAL_SAM_FAMILIES = new() { UnitFamily.VehicleSAMsr, UnitFamily.VehicleSAMtr, UnitFamily.VehicleSAMCmd, UnitFamily.VehicleSAMLauncher };
        public Coordinates Coordinates { get; init; }
        public List<DBEntryTheaterTemplateUnitLocation> Locations { get; init; }
        public TheaterTemplateLocationType LocationType { get; init; }

        public DBEntryTheaterTemplateLocation(TheaterTemplateLocation TheaterTemplateLocation)
        {
            Coordinates = new Coordinates(TheaterTemplateLocation.coords[0], TheaterTemplateLocation.coords[1]);
            Locations = new List<DBEntryTheaterTemplateUnitLocation>();

            foreach (var unitLocation in TheaterTemplateLocation.locations)
            {
                var location = new DBEntryTheaterTemplateUnitLocation
                {
                    Heading = unitLocation.heading,
                    Coordinates = new Coordinates(unitLocation.coords[0], unitLocation.coords[1]),
                    UnitTypes = unitLocation.unitTypes.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToList(),
                    SpecificType = unitLocation.specificType,
                    IsScenery = unitLocation.isScenery
                };
                Locations.Add(location);
            }

            LocationType = (TheaterTemplateLocationType)Enum.Parse(typeof(TheaterTemplateLocationType), TheaterTemplateLocation.locationType, true);
        }

        public Dictionary<UnitFamily, List<string>> GetRequiredFamilyMap()
        {
            var familyMap = new Dictionary<UnitFamily, List<string>>();

            foreach (var unitLocation in Locations)
                foreach (var unitFamily in unitLocation.UnitTypes)
                    if (!familyMap.ContainsKey(unitFamily))
                        familyMap[unitFamily] = new List<string>();

            return familyMap;
        }

        public Tuple<List<string>, List<DBEntryTemplateUnit>> CreateTemplatePositionMap(Dictionary<UnitFamily, List<string>> familyMap, Boolean tryUseAll = false)
        {
            var positionMap = new List<DBEntryTemplateUnit>();
            var potentialMissingCriticalFamilies = true;
            var units = new List<string>();
            var sortedLocations = Locations.ToList();
            sortedLocations.Sort((a, b) => a.UnitTypes.Count(af => CRITICAL_SAM_FAMILIES.Contains(af)).CompareTo(b.UnitTypes.Count(bf => CRITICAL_SAM_FAMILIES.Contains(bf))));
            sortedLocations.Reverse();

            foreach (var unitLocation in sortedLocations)
            {
                if (unitLocation.SpecificType != null)
                {
                    var specificTemplateUnit = new DBEntryTemplateUnit
                    {
                        DCoordinates = unitLocation.Coordinates,
                        Heading = unitLocation.Heading,
                        DCSID = unitLocation.SpecificType,
                        IsScenery = unitLocation.IsScenery
                    };
                    positionMap.Add(specificTemplateUnit);
                    units.Add(unitLocation.SpecificType);
                    continue;
                }
                var familyOptions = unitLocation.UnitTypes.Intersect(familyMap.Keys).ToList();
                if (familyOptions.Count == 0)
                    throw new BriefingRoomRawException($"Unit type {string.Join(",", unitLocation.UnitTypes)} not found in family map.");
                if (tryUseAll && potentialMissingCriticalFamilies && !unitLocation.UnitTypes.Any(f => CRITICAL_SAM_FAMILIES.Contains(f)))
                {
                    var missingCriticalFamilies = CRITICAL_SAM_FAMILIES.Where(f => familyMap.ContainsKey(f) && familyMap[f].Count > 0 && !familyMap[f].All(id => units.Contains(id))).ToList();

                    if (missingCriticalFamilies.Count > 0)
                        familyOptions = missingCriticalFamilies;
                    else 
                        potentialMissingCriticalFamilies = false;
                }
                var options = familyOptions.SelectMany(x => familyMap[x]).ToList();
                if (options.Count == 0)
                    throw new BriefingRoomRawException($"Unit type {string.Join(",", unitLocation.UnitTypes)} has no DCSID in family map.");

                var unitID = Toolbox.RandomFrom(options);
                if (tryUseAll) // Remove the unitID from all families unless its the last one
                    familyMap.Keys.ToList().ForEach(x =>
                    {
                        if (familyMap[x].Count > 1)
                            familyMap[x].Remove(unitID);
                    });

                var templateUnit = new DBEntryTemplateUnit
                {
                    DCoordinates = unitLocation.Coordinates,
                    Heading = unitLocation.Heading,
                    DCSID = unitID,
                    IsScenery = unitLocation.IsScenery
                };
                positionMap.Add(templateUnit);
                units.Add(unitID);
            }

            return new Tuple<List<string>, List<DBEntryTemplateUnit>>(units, positionMap);
        }
    }
}
