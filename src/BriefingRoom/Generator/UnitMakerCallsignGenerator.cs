/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using System;
using System.Collections.Generic;

namespace BriefingRoom4DCS.Generator
{
    internal static class UnitMakerCallsignGenerator
    {

        private static readonly List<Country> NON_NATO_CALLSIGN_NATIONS = new() { Country.Russia, Country.Abkhazia, Country.Belarus, Country.China, Country.Insurgents, Country.SouthOssetia, Country.Ukraine, Country.USSR, Country.Yugoslavia };

        internal static UnitCallsign GetCallsign(ref DCSMission mission, DBEntryAircraft unitDB, Country country, Side side, bool isUsingSkynet, string overrideName, int overrideNumber)
        {

            if (NON_NATO_CALLSIGN_NATIONS.Contains(country))
                return GetRussianCallsign(ref mission, unitDB.Families[0], side, isUsingSkynet);

            if (!string.IsNullOrEmpty(overrideName))
                return GetOverriddenCallsign(ref mission, overrideName, overrideNumber);

            return GetNATOCallsign(ref mission, unitDB, country, side, isUsingSkynet);
        }

        private static UnitCallsign GetNATOCallsign(ref DCSMission mission, DBEntryAircraft unitDB, Country country, Side side, bool isUsingSkynet)
        {
            string groupName;
            int randomNumber;
            string[] callSignEnum;
            int tryCount = 0;
            do
            {
                callSignEnum = Toolbox.RandomFrom<string>(unitDB.CallSigns[country]).Split(":");
                randomNumber = Toolbox.RandomMinMax(1, 9);
                groupName = $"{callSignEnum[1]} {randomNumber}";

                tryCount++;
            } while (mission.NATOCallsigns.Contains(groupName) && tryCount < 100);
            mission.NATOCallsigns.Add(groupName);

            var unitName = groupName + " $INDEX$";
            var prefixedUnitName = unitName;
            if (isUsingSkynet && unitDB.Families[0] == UnitFamily.PlaneAWACS)
                prefixedUnitName = SetSkyNetPrefix(unitName, side);
            return new UnitCallsign(groupName, prefixedUnitName/*, onboardNum*/, new Dictionary<object, object> { { 1, callSignEnum[0] }, { 2, randomNumber }, { "name", unitName.Replace(" ", "") } });
        }


        private static UnitCallsign GetRussianCallsign(ref DCSMission mission,UnitFamily unitFamily, Side side, bool isUsingSkynet)
        {
            string fgName;
            int[] fgNumber = new int[2];
            int tryCount = 0;
            do
            {
                fgNumber[0] = Toolbox.RandomMinMax(1, 9);
                fgNumber[1] = Toolbox.RandomMinMax(0, 9);

                fgName = Toolbox.ValToString(fgNumber[0]) + Toolbox.ValToString(fgNumber[1]);

                tryCount++;
            } while (mission.RussianCallsigns.Contains(fgName)  && tryCount < 100);

            mission.RussianCallsigns.Add(fgName);

            string unitName = fgName + "$INDEX$";
            string oldUnitName = unitName;

            if (isUsingSkynet && unitFamily == UnitFamily.PlaneAWACS)
                unitName = SetSkyNetPrefix(unitName, side);

            return new UnitCallsign(fgName + "0", unitName, oldUnitName);
        }

        private static UnitCallsign GetOverriddenCallsign(ref DCSMission mission, string overrideName, int overrideNumber)
        {
            var splitOverrideName = overrideName.Split(":");
            var groupName = $"{splitOverrideName[1]} {overrideNumber}";
            var unitName = groupName + " $INDEX$";
            mission.NATOCallsigns.Add(groupName);
            return new UnitCallsign(groupName, unitName/*, onboardNum*/, new Dictionary<object, object> { { 1, int.Parse(splitOverrideName[0]) }, { 2, overrideNumber }, { "name", unitName.Replace(" ", "") } });
        }

        private static string SetSkyNetPrefix(string unitName, Side side)
        {
            var prefix = side == Side.Ally ? "BLUE-" : "";
            return $"{prefix}EW-{unitName}";
        }
    }
}
