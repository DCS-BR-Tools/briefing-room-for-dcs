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

using System;

namespace BriefingRoom4DCS
{
    public readonly struct MinMaxD
    {
        internal readonly double Min;

        internal readonly double Max;

        internal MinMaxD(double min, double max) { Min = Math.Min(min, max); Max = Math.Max(min, max); }
        internal MinMaxD(double[] arr) { Min = Math.Min(arr[0], arr[1]); Max = Math.Max(arr[0], arr[1]); }

        internal MinMaxD(string minMaxString)
        {
            try
            {
                string[] minAndMaxString = minMaxString.Split(',');

                double val1 = Toolbox.StringToDouble(minAndMaxString[0]);
                double val2 = Toolbox.StringToDouble(minAndMaxString[1]);

                Min = Math.Min(val1, val2);
                Max = Math.Max(val1, val2);
            }
            catch (Exception)
            {
                Min = 0; Max = 0;
            }
        }

        internal double GetValue() { return Toolbox.RandomDouble(Min, Max); }

        public override string ToString() { return ToString(null); }

        internal string ToString(string stringFormat)
        { return Toolbox.ValToString(Min, stringFormat) + "," + Toolbox.ValToString(Max, stringFormat); }

        public static MinMaxD operator +(MinMaxD mm, double add) { return new MinMaxD(mm.Min + add, mm.Max + add); }
        public static MinMaxD operator -(MinMaxD mm, double sub) { return new MinMaxD(mm.Min - sub, mm.Max - sub); }
        public static MinMaxD operator *(MinMaxD mm, double mult) { return new MinMaxD(mm.Min * mult, mm.Max * mult); }
        public static MinMaxD operator /(MinMaxD mm, double mult) { return new MinMaxD(mm.Min / mult, mm.Max / mult); }

        internal bool Contains(double value) { return (value >= Min) && (value <= Max); }
    }
}
