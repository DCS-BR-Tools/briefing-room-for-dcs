﻿/*
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

using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryCoalition : DBEntry
    {

        internal Country[] Countries { get; private set; }

        internal (Decade start, Decade end) Operational { get; private set; } = new (Decade.Decade1940, Decade.Decade2020);

        internal string DefaultUnitList { get; private set; }

        private static readonly List<UnitFamily> SINGLE_TYPE_FAMILIES = new() { UnitFamily.VehicleMissile, UnitFamily.VehicleArtillery };


        protected override bool OnLoad(string iniFilePath)
        {
            //int i;

            var ini = new INIFile(iniFilePath);

            string[] badCountries = (from country in ini.GetValueArray<string>("Coalition", "Countries").Distinct() where !Enum.TryParse<Country>(country, true, out _) select country).ToArray();
            if (badCountries.Length > 0)
                BriefingRoom.PrintToLog($"Bad count {(badCountries.Length == 1 ? "y" : "ies")} in coalition \"{ID}\": {string.Join(", ", badCountries)}", LogMessageErrorLevel.Warning);

            Countries = ini.GetValueArray<Country>("Coalition", "Countries").Append(Country.ALL).Distinct().OrderBy(x => x).ToArray();
            if (Countries.Length == 0)
            {
                BriefingRoom.PrintToLog($"No country in coalition \"{ID}\", coalition was ignored.", LogMessageErrorLevel.Warning);
                return false;
            }

            var tempOperational = ini.GetValueArray<Decade>("Coalition", "Operational");
            if(tempOperational.Length > 0)
                Operational = new (tempOperational[0], tempOperational[1]);
            
            DefaultUnitList = ini.GetValue<string>("Coalition", "DefaultUnitList");
            if (!Database.EntryExists<DBEntryDefaultUnitList>(DefaultUnitList))
            {
                BriefingRoom.PrintToLog($"Default unit list \"{DefaultUnitList}\" required by coalition \"{ID}\" doesn't exist. Coalition was ignored.", LogMessageErrorLevel.Warning);
                return false;
            }

            return true;
        }

        internal Tuple<Country, DBEntryTemplate> GetRandomTemplate(List<UnitFamily> families, Decade decade, List<string> unitMods, List<string> unitBanList, MinMaxI? countMinMax = null,  Country[] allyCountries = null)
        {

            var validTemplates = new Dictionary<Country, List<DBEntryTemplate>>();
            var countryList = allyCountries is null ? Countries : allyCountries;
            foreach (var country in countryList)
            {
                validTemplates[country] = (
                        from template in Database.GetAllEntries<DBEntryTemplate>()
                        where families.Contains(template.Family) && template.Countries.ContainsKey(country)
                            && !template.Units.Any(x => unitBanList.Contains(x.DCSID))
                            && (string.IsNullOrEmpty(template.Module) || unitMods.Contains(template.Module, StringComparer.InvariantCultureIgnoreCase) || DBEntryDCSMod.CORE_MODS.Contains(template.Module, StringComparer.InvariantCultureIgnoreCase))
                            && (template.Countries[country].start <= decade) && (template.Countries[country].end >= decade)
                            && (!countMinMax.HasValue || countMinMax.Value.Contains(template.Units.Count))
                        select template
                    ).Distinct().ToList();
            }

            validTemplates = validTemplates.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

            if (validTemplates.Count == 0)
            {
                if(allyCountries is null)
                    return GetRandomTemplate(families, decade, unitMods, unitBanList, countMinMax,  GetAllyCountries(decade));
                BriefingRoom.PrintToLog($"No Templates of types {string.Join(", ", families)} found in coalition including {string.Join(", ", Countries.Where(x => x != Country.ALL))} including potential supplier allies {string.Join(", ", countryList.Where(x => x != Country.ALL))}");
                return null;
            }

            var selectedCountry = Toolbox.RandomFrom(validTemplates.Keys.ToList());
            return new(selectedCountry, Toolbox.RandomFrom(validTemplates[selectedCountry]));
        }



        internal Tuple<Country, List<string>> GetRandomUnits(List<UnitFamily> families, Decade decade, int count, List<string> unitMods, List<string> unitBanList, bool allowLowPolly, bool blockSuppliers, bool allowStatic, Country? requiredCountry = null, bool lowUnitVariation = false)
        {
            // Count is zero, return an empty array.
            if (count < 1) throw new BriefingRoomException("Asking for a zero unit list");
            if (families.Select(x => x.GetDCSUnitCategory()).Any(x => x != families.First().GetDCSUnitCategory())) throw new BriefingRoomException($"Cannot mix Categories in types {string.Join(", ", families)}");

            var category = families.First().GetDCSUnitCategory();
            bool allowDifferentUnitTypes = false;

            var validUnits = SelectValidUnits(families, decade, unitMods, unitBanList, allowLowPolly, blockSuppliers, allowStatic);

            if(validUnits is null)
                return new (Country.ALL, new List<string>());

            switch (category)
            {
                // Units are planes or helicopters, make sure unit count does not exceed the maximum flight group size
                case DCSUnitCategory.Helicopter:
                case DCSUnitCategory.Plane:
                    count = Toolbox.Clamp(count, 1, Toolbox.MAXIMUM_FLIGHT_GROUP_SIZE);
                    break;
                // Units are ships or static buildings, only one unit per group (that's the law in DCS World, buddy)
                case DCSUnitCategory.Ship:
                    count = 1;
                    break;
                case DCSUnitCategory.Static:
                    count = 1;
                    break;
                // Units are ground vehicles, allow multiple unit types in the group
                case DCSUnitCategory.Vehicle:
                    allowDifferentUnitTypes = families.Count > 1 || !SINGLE_TYPE_FAMILIES.Contains(families.First());
                    break;
            }


            var selectableUnits = new List<string>();
            var country = Toolbox.RandomFrom(validUnits.Keys.ToList());
            if (requiredCountry.HasValue)
                if (validUnits.ContainsKey(requiredCountry.Value))
                    country = requiredCountry.Value;
                else
                    BriefingRoom.PrintToLog($"Could not find suitable units for {requiredCountry.Value} using units from other coalition members.", LogMessageErrorLevel.Info);

            selectableUnits = validUnits[country];


            // Different unit types allowed in the group, pick a random type for each unit.
            if (allowDifferentUnitTypes)
            {
                if (lowUnitVariation)
                    selectableUnits = new List<string> { Toolbox.RandomFrom(selectableUnits), Toolbox.RandomFrom(selectableUnits) };
                List<string> selectedUnits = new();
                for (int i = 0; i < count; i++)
                    selectedUnits.Add(Toolbox.RandomFrom(selectableUnits));

                return new(country, selectedUnits.ToList());
            }

            // Different unit types NOT allowed in the group, pick a random type and fill the whole array with it.
            string unit = Toolbox.RandomFrom(selectableUnits);
            return new(country, Enumerable.Repeat(unit, count).ToList());
        }

        private Dictionary<Country, List<string>> SelectValidUnits(List<UnitFamily> families, Decade decade, List<string> unitMods, List<string> unitBanList, bool allowLowPolly, bool blockSuppliers,  bool allowStatic, Country[] allyCountries = null)
        {
            var validUnits = new Dictionary<Country, List<string>>();
            var countryList = allyCountries is null ? Countries : allyCountries;
            foreach (Country country in countryList)
                validUnits[country] = (
                        from unit in Database.GetAllEntries<DBEntryJSONUnit>()
                        where !unitBanList.Contains(unit.ID)
                            && unit.Families.Intersect(families).ToList().Count > 0 && unit.Operators.ContainsKey(country)
                            && (string.IsNullOrEmpty(unit.Module) || unitMods.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase) || DBEntryDCSMod.CORE_MODS.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase))
                            && (unit.Operators[country].start <= decade) && (unit.Operators[country].end >= decade)
                            && (!unit.LowPolly || allowLowPolly)
                            && (allowStatic || !unit.Immovable)
                        select unit.ID
                    ).Distinct().ToList();


            validUnits = validUnits.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);
            if (validUnits.Count == 0)
            {
                if(allyCountries is null && !blockSuppliers)
                    return SelectValidUnits(families, decade, unitMods, unitBanList, allowLowPolly, false, allowStatic, GetAllyCountries(decade));
                BriefingRoom.PrintToLog($"{UIDisplayName.Get()} No Units of types {string.Join(", ", families)} found in {decade} in coalition of {string.Join(", ", Countries.Where(x => x != Country.ALL))} {(!blockSuppliers ? $"including potential supplier allies ({countryList.Where(x => x != Country.ALL).Count()})" : ". Block Supplier Option turned on consider turning off.")}", LogMessageErrorLevel.Warning);

                return GetDefaultUnits(families, decade, unitMods);
            }
            if(allyCountries != null)
                validUnits = new Dictionary<Country, List<string>>{{Country.ALL, validUnits.SelectMany(x => x.Value).ToList()}};

            return validUnits;
        }

        private Country[] GetAllyCountries(Decade decade)
        {
            // Operates on the assumption that allies will supply them with arms. Most nations are in a coalition with the USA or Russia/USSR so is replacement for default unit lists.
            return Database.GetAllEntries<DBEntryCoalition>()
                .Where(x => x.Countries.Where(x => x != Country.ALL).Intersect(Countries).Any() && (x.Operational.start <= decade) && (x.Operational.end >= decade))
                .SelectMany(x =>x.Countries)
                .Where(x => !Countries.Contains(x))
                .ToArray();
        }

        private Dictionary<Country, List<string>> GetDefaultUnits(List<UnitFamily> families, Decade decade, List<string> unitMods)
        {
            var defaultDict = Database.GetEntry<DBEntryDefaultUnitList>(DefaultUnitList).DefaultUnits;
            var validUnits = new List<string>();
            foreach (var family in families)
            {
                if(!defaultDict[family].ContainsKey(decade))
                    continue;
                var options = defaultDict[family][decade].Where(id => 
                {
                    var unit = Database.GetEntry<DBEntryJSONUnit>(id);
                    return string.IsNullOrEmpty(unit.Module) || unitMods.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase) || DBEntryDCSMod.CORE_MODS.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase);
                });
                validUnits.AddRange(options);
            }
            if(validUnits.Count == 0)
                BriefingRoom.PrintToLog($"{UIDisplayName.Get()} No default units found in {decade} for types {string.Join(", ", families)}", LogMessageErrorLevel.Warning);
            else 
                BriefingRoom.PrintToLog($"{UIDisplayName.Get()} Needed to use default of {string.Join(", ", validUnits)} in {decade} for types {string.Join(", ", families)}", LogMessageErrorLevel.Warning);
            return new Dictionary<Country, List<string>> { { Country.ALL, validUnits } };
        }
    }
}
