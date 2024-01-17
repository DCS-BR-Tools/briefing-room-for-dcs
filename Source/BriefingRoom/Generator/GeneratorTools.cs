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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;


namespace BriefingRoom4DCS.Generator
{
    internal static class GeneratorTools
    {
        private static readonly int[] DAYS_PER_MONTH = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        internal static List<string> GetEmbeddedAirDefenseUnits(MissionTemplateRecord template, Side side, UnitCategory unitCategory, Country? country = null)
        {
            DBCommonAirDefenseLevel airDefenseInfo = (side == Side.Ally) ?
                 Database.Instance.Common.AirDefense.AirDefenseLevels[(int)template.SituationFriendlyAirDefense.Get()] :
                  Database.Instance.Common.AirDefense.AirDefenseLevels[(int)template.SituationEnemyAirDefense.Get()];

            DBEntryCoalition unitsCoalitionDB = Database.Instance.GetEntry<DBEntryCoalition>(template.GetCoalitionID(side));
            if (unitsCoalitionDB == null) return new List<string>();

            List<string> units = new();

            if (Toolbox.RandomDouble() >= airDefenseInfo.EmbeddedChance) return new List<string>();

            int airDefenseUnitsCount = airDefenseInfo.EmbeddedUnitCount.GetValue();

            var families = unitCategory switch
            {
                UnitCategory.Infantry => new List<UnitFamily> { UnitFamily.InfantryMANPADS },
                UnitCategory.Static => new List<UnitFamily> { UnitFamily.InfantryMANPADS, UnitFamily.VehicleAAA, UnitFamily.VehicleAAAStatic, UnitFamily.VehicleSAMShortIR, UnitFamily.VehicleSAMShortIR, UnitFamily.VehicleSAMShort },
                UnitCategory.Vehicle => new List<UnitFamily> { UnitFamily.VehicleAAA, UnitFamily.VehicleAAA, UnitFamily.VehicleSAMShortIR, UnitFamily.VehicleSAMShortIR, UnitFamily.VehicleSAMShort },
                _ => new List<UnitFamily>()
            };

            if (families.Count == 0)
                return units.ToList();

            var allowStatic = unitCategory == UnitCategory.Static;
            for (int i = 0; i < airDefenseUnitsCount; i++)
                units.AddRange(unitsCoalitionDB.GetRandomUnits(families, template.ContextDecade, 1, template.Mods, template.OptionsUnitBanList, template.OptionsMission.Contains("AllowLowPoly"),  template.OptionsMission.Contains("BlockSuppliers"),  allowStatic, country).Item2);

            return units.ToList();
        }
        
        internal static Tuple<Country, List<string>> GetNeutralRandomUnits(List<UnitFamily> families, List<Country> IgnoreCountries, Decade decade, int count, List<string> unitMods, bool allowLowPolly, List<string> unitBanList, Country? requiredCountry = null)
        {
            // Count is zero, return an empty array.
            if (count < 1) throw new BriefingRoomException("Asking for a zero unit list");
            if (families.Select(x => x.GetDCSUnitCategory()).Any(x => x != families.First().GetDCSUnitCategory())) throw new BriefingRoomException($"Cannot mix Categories in types {string.Join(", ", families)}");

            var category = families.First().GetDCSUnitCategory();
            bool allowDifferentUnitTypes = false;
            var validUnits = new Dictionary<Country, List<string>>();
            
            foreach (Country country in Enum.GetValues(typeof(Country)).Cast<Country>().Where(x => !IgnoreCountries.Contains(x)).ToList())
                validUnits[country] = (
                        from DBEntryJSONUnit unit in Database.Instance.GetAllEntries<DBEntryJSONUnit>()
                        where !unitBanList.Contains(unit.ID) && unit.Families.Intersect(families).ToList().Count > 0 && unit.Operators.ContainsKey(country) &&
                            (string.IsNullOrEmpty(unit.Module) || unitMods.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase) || DBEntryDCSMod.CORE_MODS.Contains(unit.Module, StringComparer.InvariantCultureIgnoreCase)) &&
                            (unit.Operators[country].start <= decade) && (unit.Operators[country].end >= decade) &&
                            (!unit.LowPolly || allowLowPolly)
                        select unit.ID
                    ).Distinct().ToList();

            // Ensures that only countries with units listed get returned
            validUnits = validUnits.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

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
                // Units are ground vehicles, allow multiple unit types in the group
                case DCSUnitCategory.Vehicle:
                    allowDifferentUnitTypes = true;
                    break;
            }
            var selectableUnits = new List<string>();
            var selectedCountry = Toolbox.RandomFrom(validUnits.Keys.ToList());
            if (requiredCountry.HasValue)
                if (validUnits.ContainsKey(requiredCountry.Value))
                    selectedCountry = requiredCountry.Value;
                else
                    BriefingRoom.PrintToLog($"Could not find suitable units for {requiredCountry.Value} using units from other coalition members.", LogMessageErrorLevel.Info);

            selectableUnits = validUnits[selectedCountry];


            // Different unit types allowed in the group, pick a random type for each unit.
            if (allowDifferentUnitTypes)
            {
                List<string> selectedUnits = new();
                for (int i = 0; i < count; i++)
                    selectedUnits.Add(Toolbox.RandomFrom(selectableUnits));

                return new(selectedCountry, selectedUnits.ToList());
            }

            // Different unit types NOT allowed in the group, pick a random type and fill the whole array with it.
            string unitString = Toolbox.RandomFrom(selectableUnits);
            return new(selectedCountry, Enumerable.Repeat(unitString, count).ToList());
        }

        internal static object LowercaseFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            if (str.Length == 1) return str.ToLower();
            return string.Concat(str[..1].ToLower(), str.AsSpan(1));
        }

        internal static string MakeHTMLList(List<string> listEntries)
        {
            string list = "";
            foreach (string listEntry in listEntries)
                list += $"<li>{listEntry}</li>\n";
            return list;
        }

        internal static string GenerateCampaignName(string desiredName)
        {
            // Try to get the provided custom mission name.
            string missionName = (desiredName ?? "").ReplaceAll("", "\r", "\n", "\t").Trim();

            // No custom name found, generate one.
            if (string.IsNullOrEmpty(missionName))
            {
                missionName = Database.Instance.Common.Names.CampaignNameTemplate.Get();
                for (int i = 0; i < DBCommonNames.MISSION_NAMES_PART_COUNT; i++)
                    missionName = missionName.Replace($"$P{i + 1}$", Toolbox.RandomFrom(Database.Instance.Common.Names.MissionNameParts[i].Get().Split(",")));
            }

            return missionName;
        }

        internal static string GenerateMissionName(string desiredName)
        {
            // Try to get the provided custom mission name.
            string missionName = (desiredName ?? "").ReplaceAll("", "\r", "\n", "\t").Trim();

            // No custom name found, generate one.
            if (string.IsNullOrEmpty(missionName))
            {
                missionName = Database.Instance.Common.Names.MissionNameTemplate.Get();
                for (int i = 0; i < DBCommonNames.MISSION_NAMES_PART_COUNT; i++)
                    missionName = missionName.Replace($"$P{i + 1}$", Toolbox.RandomFrom(Database.Instance.Common.Names.MissionNameParts[i].Get().Split(",")));
            }

            return missionName;
        }


        internal static string MakeRawTextList(List<string> listEntries, string newLine = "\n")
        {
            string list = "";
            foreach (string listEntry in listEntries)
                list += $"- {listEntry}{newLine}";
            return list;
        }

        internal static string MakeHTMLTable(List<string> tableEntries)
        {
            string table = "";
            foreach (string tableRow in tableEntries)
            {
                string[] rowCells = tableRow.Split('\t');
                table += "<tr>";
                foreach (string rowCell in rowCells)
                    table += $"<td>{rowCell.ReplaceAll("\n", "<br/>")}</td>";
                table += "</tr>\n";

            }
            return table;
        }

        internal static string GetTemplateCoalition(MissionTemplateRecord template, Coalition coalition)
        {
            if (coalition == Coalition.Red) return template.ContextCoalitionRed;
            return template.ContextCoalitionBlue;
        }

        internal static Coalition? GetSpawnPointCoalition(MissionTemplateRecord template, Side side, bool forceSide = false)
        {
            // No countries spawning restriction
            if (template.SpawnAnywhere && !forceSide) return null;
            if (side == Side.Neutral) return Coalition.Neutral;

            Coalition coalition = side == Side.Ally ? template.ContextPlayerCoalition : template.ContextPlayerCoalition.GetEnemy();

            return coalition;
        }

        internal static int GetDaysPerMonth(Month month, int year)
        {
            // Not February, return value stored in DAYS_PER_MONTH array
            if (month != Month.February) return DAYS_PER_MONTH[(int)month];

            bool leapYear = false;
            if ((year % 400) == 0) leapYear = true;
            else if ((year % 100) == 0) leapYear = false;
            else if ((year % 4) == 0) leapYear = true;

            return leapYear ? 29 : 28;
        }


        internal static object GetTACANCallsign(UnitFamily unitFamily)
        {
            return unitFamily switch
            {
                UnitFamily.ShipCarrierCATOBAR or UnitFamily.ShipCarrierSTOBAR or UnitFamily.ShipCarrierSTOVL => "CVN",
                UnitFamily.PlaneTankerBasket or UnitFamily.PlaneTankerBoom => "TKR",
                _ => "TCN",
            };
        }

        internal static DCSSkillLevel GetDefaultSkillLevel(MissionTemplateRecord template, Side side) => (Side.Ally == side ? template.SituationFriendlySkill : template.SituationEnemySkill) switch
        {
            AmountR.VeryLow => DCSSkillLevel.Average,
            AmountR.Low => Toolbox.RandomFrom(DCSSkillLevel.Average, DCSSkillLevel.Good),
            AmountR.Average => Toolbox.RandomFrom(DCSSkillLevel.Average, DCSSkillLevel.Good, DCSSkillLevel.High),
            AmountR.High => Toolbox.RandomFrom(DCSSkillLevel.Good, DCSSkillLevel.High),
            AmountR.VeryHigh => Toolbox.RandomFrom(DCSSkillLevel.High, DCSSkillLevel.Excellent),
            _ => DCSSkillLevel.Random,
        };

        internal static string FormatRadioFrequency(double radioFrequency)
        {
            return (radioFrequency > 10000 ? radioFrequency / 1000000.0 : radioFrequency).ToString("F1", NumberFormatInfo.InvariantInfo);
        }

        internal static void ReplaceKey(ref string lua, string key, object value)
        {
            string valueStr = Toolbox.ValToString(value);
            if (value is bool) valueStr = valueStr.ToLower();

            lua = lua.Replace($"${key.ToUpper()}$", valueStr);
        }

        internal static void ReplaceKey(ref string lua, string key, object arrayValue, int arrayIndex)
        {
            try
            {
                object value = ((IEnumerable)arrayValue).Cast<object>().Select(x => x).ToArray()[arrayIndex];
                ReplaceKey(ref lua, key, value);
            }
            catch (Exception)
            {
                ReplaceKey(ref lua, key, "");
            }
        }

        internal static int GetTACANFrequency(int channel, char channelMode, bool AA)
        {
            int res;

            if (!AA && channelMode == 'X')
            {
                if (channel < 64)
                {
                    res = 962 + channel - 1;
                }
                else
                {
                    res = 1151 + channel - 64;
                }
            }
            else
            {
                if (channel < 64)
                {
                    res = 1088 + channel - 1;
                }
                else
                {
                    res = 1025 + channel - 64;
                }
            }

            return GetRadioFrequency(res);
        }

        internal static int GetRadioFrequency(double radioFrequency)
        {
            return (int)(radioFrequency * 1000000.0);
        }

        internal static void CheckDBForMissingEntry<T>(string id, bool allowEmpty = false) where T : DBEntry
        {
            if (string.IsNullOrEmpty(id) && allowEmpty) return;
            if (!Database.Instance.EntryExists<T>(id)) throw new BriefingRoomException($"Database entry {typeof(T).Name} with ID \"{id}\" not found.");
        }

        internal static string ParseRandomString(string randomString, DCSMission mission = null)
        {
            while (randomString.Contains('{') && randomString.Contains('{'))
            {
                int start = randomString.LastIndexOf("{");
                string stringLeft = randomString[start..];
                if (!stringLeft.Contains('}')) break;
                int end = stringLeft.IndexOf("}") + 1;

                string segment = randomString.Substring(start, end);
                string parsedSegment = segment.Replace("{", "").Replace("}", "").Trim();
                string[] items = parsedSegment.Split('|');
                string selItem = Toolbox.RandomFrom(items);

                randomString = randomString.Replace(segment, selItem);
            }

            var responseString = randomString.Replace("{", "").Replace("}", "").Trim();
            if (mission != null)
                return mission.ReplaceValues(responseString);
            return responseString;
        }

        private static string SetSkyNetPrefix(string groupName, UnitFamily unitFamily, Side side)
        {
            var ewrTypes = new List<UnitFamily> {
                UnitFamily.VehicleEWR,
                UnitFamily.ShipCarrierCATOBAR,
                UnitFamily.ShipCarrierSTOBAR,
                UnitFamily.ShipCarrierSTOVL,
                UnitFamily.ShipCruiser,
                UnitFamily.ShipFrigate};
            var samTypes = new List<UnitFamily> {
                UnitFamily.VehicleSAMMedium,
                UnitFamily.VehicleSAMLong
                };
            var isEWR = ewrTypes.Contains(unitFamily);
            if (isEWR || samTypes.Contains(unitFamily))
            {
                var prefix = isEWR ? "EW" : "SAM";
                var newGroupName = $"{prefix}-{groupName}";
                if (side == Side.Ally)
                    newGroupName = $"BLUE-{newGroupName}";
                return newGroupName;
            }
            return groupName;
        }

        internal static string GetGroupName(int groupID, UnitFamily family, Side side, bool isUsingSkynet)
        {
            string name = ParseRandomString(Database.Instance.Common.Names.UnitGroups[(int)family].Get());

            int fakeGroupNumber = groupID * 10 + Toolbox.RandomInt(1, 10);
            name = name.Replace("$N$", fakeGroupNumber.ToString(NumberFormatInfo.InvariantInfo));
            name = name.Replace("$NTH$", Toolbox.GetOrdinalAdjective(fakeGroupNumber));
            if (isUsingSkynet)
                return SetSkyNetPrefix(name, family, side);
            if (string.IsNullOrEmpty(name))
                throw new BriefingRoomException($"{family} cannot have empty group name");
            return name;
        }


        internal static string GetPlayerStartingAction(PlayerStartLocation playerStartLocation)
        {
            return playerStartLocation switch
            {
                PlayerStartLocation.ParkingHot => "From Parking Area Hot",
                PlayerStartLocation.Runway => "From Runway",
                PlayerStartLocation.Air => "Turning Point",
                // case PlayerStartLocation.ParkingCold
                _ => "From Parking Area",
            };
        }

        internal static string GetPlayerStartingType(PlayerStartLocation playerStartLocation)
        {
            return playerStartLocation switch
            {
                PlayerStartLocation.ParkingHot => "TakeOffParkingHot",
                PlayerStartLocation.Runway => "TakeOff",
                PlayerStartLocation.Air => "Turning Point",
                // case PlayerStartLocation.ParkingCold
                _ => "TakeOffParking",
            };
        }
    }
}
