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
    public readonly struct MinMaxI
    {
        internal readonly int Min;

        internal readonly int Max;

        internal MinMaxI(int min, int max) { Min = Math.Min(min, max); Max = Math.Max(min, max); }

        internal MinMaxI(string minMaxString)
        {
            try
            {
                string[] minAndMaxString = minMaxString.Split(',');

                int val1 = Toolbox.StringToInt(minAndMaxString[0]);
                int val2 = Toolbox.StringToInt(minAndMaxString[1]);

                Min = Math.Min(val1, val2);
                Max = Math.Max(val1, val2);
            }
            catch (Exception)
            {
                Min = 0; Max = 0;
            }
        }

        internal int GetValue() { return Toolbox.RandomInt(Min, Max + 1); }

        public static MinMaxI operator *(MinMaxI mm, int mult) { return new MinMaxI(mm.Min * mult, mm.Max * mult); }
        public static MinMaxI operator /(MinMaxI mm, int mult) { return new MinMaxI(mm.Min / mult, mm.Max / mult); }
        public static MinMaxD operator *(MinMaxI mm, double mult) { return new MinMaxD(mm.Min * mult, mm.Max * mult); }
        public static MinMaxD operator /(MinMaxI mm, double mult) { return new MinMaxD(mm.Min / mult, mm.Max / mult); }

        public override string ToString() { return ToString(null); }

        internal string ToString(string stringFormat)
        { return Toolbox.ValToString(Min, stringFormat) + "," + Toolbox.ValToString(Max, stringFormat); }

        internal bool Contains(int value) { return (value >= Min) && (value <= Max); }
    }
}
