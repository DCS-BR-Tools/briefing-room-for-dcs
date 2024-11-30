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

using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryObjectiveTarget : DBEntry
    {
        internal LanguageString[] BriefingName { get; private set; }

        internal UnitCategory UnitCategory { get { return UnitFamilies[0].GetUnitCategory(); } }
        internal DCSUnitCategory DCSUnitCategory { get { return UnitFamilies[0].GetDCSUnitCategory(); } }

        internal MinMaxI[] UnitCount { get; private set; }

        internal UnitFamily[] UnitFamilies { get; private set; }

        internal SpawnPointType[] ValidSpawnPoints { get; private set; }

        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            var className = this.GetLanguageClassName();
            BriefingName = new LanguageString[2]
            {
                        ini.GetLangStrings(Database.Language, className, ID, "ObjectiveTarget", "Briefing.UnitName.Singular"),
                        ini.GetLangStrings(Database.Language, className, ID, "ObjectiveTarget", "Briefing.UnitName.Plural")
            };

            UnitFamilies = Toolbox.SetSingleCategoryFamilies(ini.GetValueArray<UnitFamily>("ObjectiveTarget", "Units.Families"));
            if (UnitFamilies.Length == 0)
            {
                BriefingRoom.PrintToLog($"No unit categories for objective target \"{ID}\"", LogMessageErrorLevel.Warning);
                return false;
            }

            UnitCount = new MinMaxI[Toolbox.EnumCount<Amount>()];
            foreach (Amount amount in Toolbox.GetEnumValues<Amount>())
                UnitCount[(int)amount] = ini.GetValue<MinMaxI>("ObjectiveTarget", $"Units.Count.{amount}");

            ValidSpawnPoints = DatabaseTools.CheckSpawnPoints(ini.GetValueArray<SpawnPointType>("ObjectiveTarget", "ValidSpawnPoints"));

            return true;
        }
    }
}
