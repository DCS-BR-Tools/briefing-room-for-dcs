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
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.GUI
{
    public static class BriefingRoomGUITools
    {
        public static string GetEnumName(string langKey, object enumValue)
        {
            if (enumValue == null) return "";
            var enumStr = enumValue.ToString();
            if(enumStr.Equals("None"))
                enumStr = "NNone";
            return BriefingRoom.Translate(langKey, enumStr);
        }

        public static string GetEnumDescription(string langKey, object enumValue)
        {
            if (enumValue == null) return "";
            return BriefingRoom.Translate(langKey, $"{enumValue}Description");
        }
    }
}
