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

using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryObjectiveTargetBehavior : DBEntry
    {
        internal DBEntryObjectiveTargetBehaviorLocation Location { get; private set; }

        internal string[] GroupLua { get; private set; }
        internal string[] UnitLua { get; private set; }

        internal UnitCategory[] ValidUnitCategories { get; private set; }
        internal string[] InvalidTasks { get; private set; }
        internal bool IsStatic {get; set;}


        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            Location = ini.GetValue<DBEntryObjectiveTargetBehaviorLocation>("Behavior", "Location");
            IsStatic = ini.GetValue<bool>("Behavior", "IsStatic");

            GroupLua = new string[Toolbox.EnumCount<DCSUnitCategory>()];
            foreach (DCSUnitCategory unitCategory in Toolbox.GetEnumValues<DCSUnitCategory>())
                GroupLua[(int)unitCategory] = ini.GetValue<string>("Lua", $"Group.{unitCategory}");

            UnitLua = new string[Toolbox.EnumCount<DCSUnitCategory>()];
            foreach (DCSUnitCategory unitLua in Toolbox.GetEnumValues<DCSUnitCategory>())
                UnitLua[(int)unitLua] = ini.GetValue<string>("Lua", $"Unit.{unitLua}");

            ValidUnitCategories = ini.GetValueArray<UnitCategory>("Behavior", "ValidUnitCategories").Distinct().ToArray();
            if (ValidUnitCategories.Length == 0) ValidUnitCategories = Toolbox.GetEnumValues<UnitCategory>(); // No category means all categories

            InvalidTasks = Database.Instance.CheckIDs<DBEntryObjectiveTask>(ini.GetValueArray<string>("Behavior", "InvalidTasks").Distinct().ToArray());
            return true;
        }
    }
}
