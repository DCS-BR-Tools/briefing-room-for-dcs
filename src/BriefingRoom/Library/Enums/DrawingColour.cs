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
    public enum DrawingColour
    {
        Red,
        Black,
        RedFill,
        Blue,
        BlueFill,
        GreenFill,
        White,
        WhiteBackground,
        Clear,
    }

    public static class DrawingColourExtensions
    {
        public static string ToValue(this DrawingColour colour) => colour switch
        {
            DrawingColour.Black => "0x00000088",
            DrawingColour.Red => "0xff0000ff",
            DrawingColour.RedFill => "0xff000022",
            DrawingColour.Blue => "0x0000ffff",
            DrawingColour.BlueFill => "0x0000ff22",
            DrawingColour.Clear => "0xff000000",
            DrawingColour.GreenFill => "0x00800022",
            DrawingColour.White => "0xffffffff",
            DrawingColour.WhiteBackground => "0xffffff88",
            _ => throw new ArgumentOutOfRangeException(nameof(colour), $"Not expected colour value: {colour}"),
        };
    }
}
