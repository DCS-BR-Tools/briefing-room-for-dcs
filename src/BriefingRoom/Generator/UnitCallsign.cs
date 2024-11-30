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
using System.Linq;
using System.Collections.Generic;

namespace BriefingRoom4DCS.Generator
{
    internal readonly struct UnitCallsign
    {
        internal string GroupName { get; }

        private readonly string UnitName;

        private readonly string Lua;

        private readonly Dictionary<object, object> LuaObj;

        internal UnitCallsign(string groupName, string unitName, string lua)
        {
            GroupName = groupName;
            UnitName = unitName;
            Lua = lua;
            LuaObj = new Dictionary<object, object>();
        }

        internal UnitCallsign(string groupName, string unitName, Dictionary<object, object> luaObj)
        {
            GroupName = groupName;
            UnitName = unitName;
            LuaObj = luaObj;
            Lua = "";
        }

        internal dynamic GetLua(int unitIndex)
        {
            if (!string.IsNullOrEmpty(Lua))
                return Lua.Replace("$INDEX$", unitIndex.ToString());
            if (!LuaObj.ContainsKey(3))
                LuaObj.Add(3, unitIndex);
            else
                LuaObj[3] = unitIndex;
            return LuaObj.ToDictionary(x => x.Key, x => x.Value);
        }

        internal string GetUnitName(int unitIndex)
        {
            return UnitName.Replace("$INDEX$", unitIndex.ToString());
        }
    }
}
