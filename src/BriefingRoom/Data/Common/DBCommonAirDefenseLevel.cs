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
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal readonly struct DBCommonAirDefenseLevel
    {
        internal double EmbeddedChance { get; }

        internal MinMaxI EmbeddedUnitCount { get; }

        internal MinMaxI[] GroupsInArea { get; }

        internal DCSSkillLevel[] SkillLevel { get; }

        internal DBCommonAirDefenseLevel(INIFile ini, AmountNR airDefenseLevel)
        {
            int i;
            GroupsInArea = new MinMaxI[Toolbox.EnumCount<AirDefenseRange>()];

            if ((airDefenseLevel == AmountNR.None) || (airDefenseLevel == AmountNR.Random))
            {
                EmbeddedChance = 0;
                EmbeddedUnitCount = new MinMaxI(0, 0);
                for (i = 0; i < Toolbox.EnumCount<AirDefenseRange>(); i++)
                    GroupsInArea[i] = new MinMaxI(0, 0);
                SkillLevel = new DCSSkillLevel[] { DCSSkillLevel.Average };

                return;
            }

            EmbeddedChance = Toolbox.Clamp(ini.GetValue<int>("AirDefense", $"{airDefenseLevel}.Embedded.Chance"), 0, 100) / 100.0;
            EmbeddedUnitCount = ini.GetValue<MinMaxI>("AirDefense", $"{airDefenseLevel}.Embedded.UnitCount");

            for (i = 0; i < Toolbox.EnumCount<AirDefenseRange>(); i++)
                GroupsInArea[i] = ini.GetValue<MinMaxI>("AirDefense", $"{airDefenseLevel}.GroupsInArea.{(AirDefenseRange)i}");

            SkillLevel = ini.GetValueArray<DCSSkillLevel>("AirDefense", $"{airDefenseLevel}.SkillLevel").Distinct().ToArray();
            if (SkillLevel.Length == 0) SkillLevel = new DCSSkillLevel[] { DCSSkillLevel.Average, DCSSkillLevel.Good, DCSSkillLevel.High, DCSSkillLevel.Excellent };
        }
    }
}
