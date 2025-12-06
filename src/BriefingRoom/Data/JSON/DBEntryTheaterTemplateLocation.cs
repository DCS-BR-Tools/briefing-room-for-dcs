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
    }

    public readonly struct DBEntryTheaterTemplateLocation
    {
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
                    UnitTypes = unitLocation.unitTypes.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToList()
                };

                Locations.Add(location);
            }

            LocationType = (TheaterTemplateLocationType)Enum.Parse(typeof(TheaterTemplateLocationType), TheaterTemplateLocation.locationType, true);
        }

        public Dictionary<UnitFamily, List<string>> GetRequiredFamilyMap()
        {
            var familyMap = new Dictionary<UnitFamily, List<string>>();

            foreach (var unitLocation in Locations)
            {
                var unitFamily = Toolbox.RandomFrom(unitLocation.UnitTypes);
                if (!familyMap.ContainsKey(unitFamily))
                {
                    familyMap[unitFamily] = new List<string>();
                }
            }

            return familyMap;
        }

        public Tuple<List<string>, List<DBEntryTemplateUnit>> CreateTemplatePositionMap(Dictionary<UnitFamily, List<string>> familyMap, Boolean tryUseAll = false)
        {
            var positionMap = new List<DBEntryTemplateUnit>();
            var units = new List<string>();
            foreach (var unitLocation in Locations)
            {
                var familyOptions = unitLocation.UnitTypes.Intersect(familyMap.Keys).ToList();
                if (familyOptions.Count == 0)
                {
                    throw new BriefingRoomRawException($"Unit type {string.Join(",", unitLocation.UnitTypes)} not found in family map.");
                }
                var options = familyOptions.SelectMany(x => familyMap[x]).ToList();
                if (options.Count == 0)
                {
                    throw new BriefingRoomRawException($"Unit type {string.Join(",", unitLocation.UnitTypes)} has no DCSID in family map.");
                }

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
                    DCSID = unitID
                };
                positionMap.Add(templateUnit);
                units.Add(unitID);
            }

            return new Tuple<List<string>, List<DBEntryTemplateUnit>>(units, positionMap);
        }
    }
}
