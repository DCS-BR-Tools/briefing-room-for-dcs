using System;
using System.Collections.Generic;
using System.Linq;
using BriefingRoom4DCS.Data.JSON;


namespace BriefingRoom4DCS.Data
{

    public readonly struct DBEntryTheaterTemplateLocation
    {
        public Coordinates Coordinates { get; init; }
        public List<DBEntryTemplateUnit> Units { get; init; }
        public TheaterTemplateLocationType LocationType { get; init; }

        public DBEntryTheaterTemplateLocation(TheaterTemplateLocation TheaterTemplateLocation)
        {
            Coordinates = new Coordinates(TheaterTemplateLocation.coords[0], TheaterTemplateLocation.coords[1]);
            Units = new List<DBEntryTemplateUnit>();

            foreach (var unitLocation in TheaterTemplateLocation.units)
            {
                var location = new DBEntryTemplateUnit
                {
                    Heading = unitLocation.heading,
                    Coordinates = new Coordinates(unitLocation.coords[0], unitLocation.coords[1]),
                    UnitFamilies = unitLocation.unitFamilies.Select(x => (UnitFamily)Enum.Parse(typeof(UnitFamily), x, true)).ToList(),
                    IsScenery = unitLocation.isScenery,
                    DCSID = unitLocation.isSpecificType ? unitLocation.originalType : null
                };

                Units.Add(location);
            }

            LocationType = (TheaterTemplateLocationType)Enum.Parse(typeof(TheaterTemplateLocationType), TheaterTemplateLocation.locationType, true);
        }

        public Dictionary<UnitFamily, List<string>> GetRequiredFamilyMap()
        {
            var familyMap = new Dictionary<UnitFamily, List<string>>();

            foreach (var unitLocation in Units)
            {
                var unitFamily = Toolbox.RandomFrom(unitLocation.UnitFamilies);
                if (!familyMap.ContainsKey(unitFamily))
                {
                    familyMap[unitFamily] = new List<string>();
                }
            }

            return familyMap;
        }

        public Tuple<List<string>, List<DBEntryDCSTemplateUnit>> CreateTemplatePositionMap(Dictionary<UnitFamily, List<string>> familyMap, Boolean tryUseAll = false)
        {
            var positionMap = new List<DBEntryDCSTemplateUnit>();
            var units = new List<string>();
            foreach (var unitLocation in Units)
            {
                var familyOptions = unitLocation.UnitFamilies.Intersect(familyMap.Keys).ToList();
                if (familyOptions.Count == 0)
                {
                    throw new BriefingRoomException("en", $"Unit type {string.Join(",", unitLocation.UnitFamilies)} not found in family map.");
                }
                var options = familyOptions.SelectMany(x => familyMap[x]).ToList();
                if (options.Count == 0)
                {
                    throw new BriefingRoomException("en", $"Unit type {string.Join(",", unitLocation.UnitFamilies)} has no DCSID in family map.");
                }

                var unitID = Toolbox.RandomFrom(options);
                if (tryUseAll) // Remove the unitID from all families unless its the last one
                    familyMap.Keys.ToList().ForEach(x =>
                    {
                        if (familyMap[x].Count > 1)
                            familyMap[x].Remove(unitID);
                    });

                var templateUnit = new DBEntryDCSTemplateUnit
                {
                    DCoordinates = unitLocation.Coordinates,
                    Heading = unitLocation.Heading,
                    DCSID = unitID
                };
                positionMap.Add(templateUnit);
                units.Add(unitID);
            }

            return new Tuple<List<string>, List<DBEntryDCSTemplateUnit>>(units, positionMap);
        }
    }
}
