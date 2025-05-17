using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;


namespace BriefingRoom4DCS.Data
{
    public readonly struct DBEntryTemplateUnitLocation
    {
        public double Heading { get; init; }
        public Coordinates Coordinates { get; init; }
        public List<UnitFamily> UnitTypes { get; init; }
    }

    public readonly struct DBEntryTemplateLocation
    {
        public Coordinates Coordinates { get; init; }
        public List<DBEntryTemplateUnitLocation> Locations { get; init; }
        public DBEntryTemplateLocationType LocationType { get; init; }

        public DBEntryTemplateLocation(TemplateLocation templateLocation)
        {
            Coordinates = new Coordinates(templateLocation.coords[0], templateLocation.coords[1]);
            Locations = new List<DBEntryTemplateUnitLocation>();

            foreach (var unitLocation in templateLocation.locations)
            {
                var location = new DBEntryTemplateUnitLocation
                {
                    Heading = unitLocation.heading,
                    Coordinates = new Coordinates(unitLocation.coords[0], unitLocation.coords[1]),
                    UnitTypes = unitLocation.unitTypes.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToList()
                };

                Locations.Add(location);
            }

            LocationType = (DBEntryTemplateLocationType)Enum.Parse(typeof(DBEntryTemplateLocationType), templateLocation.locationType, true);
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

        public Tuple<List<string>,List<DBEntryTemplateUnit>> CreateTemplatePositionMap(Dictionary<UnitFamily, List<string>> familyMap)
        {
            var positionMap = new List<DBEntryTemplateUnit>();
            var units = new List<string>();
            foreach (var unitLocation in Locations)
            {
                var familyOptions = unitLocation.UnitTypes.Intersect(familyMap.Keys).ToList();
                if (familyOptions.Count == 0)
                {
                    throw new BriefingRoomException("en", $"Unit type {unitLocation.UnitTypes} not found in family map.");
                }
                var options = familyOptions.SelectMany(x => familyMap[x]).ToList();
                if (options.Count == 0)
                {
                    throw new BriefingRoomException("en", $"Unit type {unitLocation.UnitTypes} has no DCSID in family map.");
                }

                var unitID = Toolbox.RandomFrom(options);
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
