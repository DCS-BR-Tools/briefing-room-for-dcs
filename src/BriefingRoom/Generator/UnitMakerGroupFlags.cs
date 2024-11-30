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

namespace BriefingRoom4DCS.Generator
{
    [Flags]
    internal enum UnitMakerGroupFlags
    {
        AlwaysHidden = 1,

        FirstUnitIsClient = 2,

        ImmediateAircraftSpawn = 4,

        NeverHidden = 8,

        EmbeddedAirDefense = 16,

        RadioAircraftSpawn = 32,
        Immortal = 64,
        Inert = 128,
        Invisible = 256,
        ScrambleStart = 512,
        StaticAircraft = 1024,
        LowUnitVariation = 2048,
        TimedAircraftSpawn = 4096,
        ProgressionAircraftSpawn = 8192,
    }
}