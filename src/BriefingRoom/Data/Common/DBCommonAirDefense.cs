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

namespace BriefingRoom4DCS.Data
{
    internal class DBCommonAirDefense
    {
        internal DBCommonAirDefenseLevel[] AirDefenseLevels { get; }

        internal MinMaxD[,] DistanceFromCenter { get; }

        internal double[,] MinDistanceFromOpposingPoint { get; }

        internal DBCommonAirDefense()
        {
            INIFile ini = new(Path.Combine(BRPaths.DATABASE, "AirDefense.ini"));
            AirDefenseLevels = new DBCommonAirDefenseLevel[Toolbox.EnumCount<AmountNR>()];
            for (var i = 0; i < Toolbox.EnumCount<AmountNR>(); i++)
                AirDefenseLevels[i] = new DBCommonAirDefenseLevel(ini, (AmountNR)i);

            DistanceFromCenter = new MinMaxD[2, Toolbox.EnumCount<AirDefenseRange>()];
            MinDistanceFromOpposingPoint = new double[2, Toolbox.EnumCount<AirDefenseRange>()];
            foreach (Side side in Toolbox.GetEnumValues<Side>())
            {
                if (side == Side.Neutral) continue;
                foreach (AirDefenseRange airDefenseRange in Toolbox.GetEnumValues<AirDefenseRange>())
                {
                    DistanceFromCenter[(int)side, (int)airDefenseRange] = ini.GetValue<MinMaxD>($"AirDefenseRange.{side}", $"{airDefenseRange}.DistanceFromCenter");
                    MinDistanceFromOpposingPoint[(int)side, (int)airDefenseRange] = ini.GetValue<double>($"AirDefenseRange.{side}", $"{airDefenseRange}.MinDistanceFromOpposingPoint");
                }
            }
        }


    }
}