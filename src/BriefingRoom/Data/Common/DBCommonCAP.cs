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
using System;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBCommonCAP
    {
        internal MinMaxD DistanceFromCenter { get; }

        internal MinMaxD FlightPathLength { get; }

        internal int[] GroupSize { get; }

        internal double MinDistanceFromOpposingPoint { get; }

        internal string LuaGroup { get; }

        internal string LuaUnit { get; }

        internal UnitFamily[] UnitFamilies { get; }

        internal DBCommonCAPLevel[] CAPLevels { get; }

        internal DBCommonCAP()
        {
            INIFile ini = new(Path.Combine(BRPaths.DATABASE, "CAP.ini"));
            DistanceFromCenter = ini.GetValue<MinMaxD>("CAP", "DistanceFromCenter");
            GroupSize = (from int groupSize in ini.GetValueArray<int>("CAP", "GroupSize") where groupSize > 0 select groupSize).ToArray();
            if (GroupSize.Length == 0) GroupSize = new int[] { 1 };
            FlightPathLength = ini.GetValue<MinMaxD>("CAP", "FlightPathLength") * Toolbox.NM_TO_METERS;
            LuaGroup = ini.GetValue<string>("CAP", "Lua.Group");
            LuaUnit = ini.GetValue<string>("CAP", "Lua.Unit");
            MinDistanceFromOpposingPoint = ini.GetValue<double>("CAP", "MinDistanceFromOpposingPoint");
            UnitFamilies = (from UnitFamily unitFamily in ini.GetValueArray<UnitFamily>("CAP", "UnitFamilies") where unitFamily.GetUnitCategory() == UnitCategory.Plane select unitFamily).ToArray();
            if (UnitFamilies.Length == 0) UnitFamilies = new UnitFamily[] { UnitFamily.PlaneFighter };

            CAPLevels = new DBCommonCAPLevel[Toolbox.EnumCount<AmountNR>()];
            for (int i = 0; i < Toolbox.EnumCount<AmountNR>(); i++)
                CAPLevels[i] = new DBCommonCAPLevel(ini, (AmountNR)i);
        }


    }
}