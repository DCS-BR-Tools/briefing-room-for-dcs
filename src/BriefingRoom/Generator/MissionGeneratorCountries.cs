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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS.Generator
{
    internal class MissionGeneratorCountries
    {
        private static readonly Country[] DEFAULT_COUNTRIES = new Country[] { Country.CombinedJointTaskForcesBlue, Country.CombinedJointTaskForcesRed };

        internal static Country[][] GenerateCountries(ref DCSMission mission)
        {
            int i;

            List<Country>[] countries = new List<Country>[] { new List<Country>(), new List<Country>(), new List<Country>() };

            // Add default country for each coalition
            for (i = 0; i < 2; i++)
                countries[i].Add(DEFAULT_COUNTRIES[i]);

            // Add countries for player FGs to player coalition
            foreach (MissionTemplateFlightGroupRecord flightGroup in mission.TemplateRecord.PlayerFlightGroups)
            {
                var group = flightGroup.Hostile ? mission.TemplateRecord.ContextPlayerCoalition.GetEnemy() : mission.TemplateRecord.ContextPlayerCoalition;
                countries[(int)group].Add(flightGroup.Country);
            }


            countries[(int)Coalition.Blue].AddRange(Database.Instance.GetEntry<DBEntryCoalition>(mission.TemplateRecord.ContextCoalitionBlue).Countries);
            countries[(int)Coalition.Red].AddRange(Database.Instance.GetEntry<DBEntryCoalition>(mission.TemplateRecord.ContextCoalitionRed).Countries);

            // Add all non-aligned countries to the list of neutral countries
            List<Country> neutralCountries = new(Toolbox.GetEnumValues<Country>());
            for (i = 0; i < 2; i++) neutralCountries = neutralCountries.Except(countries[i]).ToList();
            countries[(int)Coalition.Neutral].AddRange(neutralCountries);

            // Make sure each country doesn't contain the other's coalition default country
            for (i = 0; i < 3; i++)
            {
                countries[i].Remove(Country.ALL);
                countries[i] = countries[i].Distinct().ToList();
            }

            var intersect = countries[(int)Coalition.Blue].Intersect(countries[(int)Coalition.Red]).ToList();
            if (intersect.Count > 0)
                throw new BriefingRoomException(mission.LangKey, "DuelSideCountry", string.Join(",", intersect));


            mission.SetValue("CoalitionNeutral", GetCountriesLuaTable(neutralCountries));
            mission.SetValue("CoalitionBlue", GetCountriesLuaTable(countries[(int)Coalition.Blue]));
            mission.SetValue("CoalitionRed", GetCountriesLuaTable(countries[(int)Coalition.Red]));

            return new Country[][] { countries[0].ToArray(), countries[1].ToArray(), countries[2].ToArray() };
        }

        private static string GetCountriesLuaTable(List<Country> countries)
        {
            string luaTable = "{ ";
            for (int i = 0; i < countries.Count; i++)
                luaTable += $"[{i + 1}] = {(int)countries[i]}{(i < countries.Count - 1 ? ", " : "")}";
            luaTable += " }";

            return luaTable;
        }
    }
}
