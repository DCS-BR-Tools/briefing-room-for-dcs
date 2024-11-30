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
    internal readonly struct DBCommonCAPLevel
    {
        internal DCSSkillLevel[] SkillLevel { get; }

        internal MinMaxI UnitCount { get; }

        internal DBCommonCAPLevel(INIFile ini, AmountNR capLevel)
        {
            if ((capLevel == AmountNR.None) || (capLevel == AmountNR.Random))
            {
                SkillLevel = new DCSSkillLevel[] { DCSSkillLevel.Random };
                UnitCount = new MinMaxI(0, 0);
                return;
            }

            SkillLevel = ini.GetValueArray<DCSSkillLevel>("CAPLevels", $"{capLevel}.SkillLevel").Distinct().ToArray();
            if (SkillLevel.Length == 0) SkillLevel = new DCSSkillLevel[] { DCSSkillLevel.Average, DCSSkillLevel.Good, DCSSkillLevel.High, DCSSkillLevel.Excellent };
            UnitCount = ini.GetValue<MinMaxI>("CAPLevels", $"{capLevel}.UnitCount");
        }
    }
}
