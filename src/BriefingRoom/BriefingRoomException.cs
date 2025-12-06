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
using System.Diagnostics.CodeAnalysis;
using BriefingRoom4DCS.Data;

namespace BriefingRoom4DCS
{
    public class BriefingRoomException : BriefingRoomRawException
    {
        public BriefingRoomException(IDatabase database, string langKey, string message) : base(database.Language.Translate(langKey, message)) { }
        public BriefingRoomException(IDatabase database, string langKey, [StringSyntax("CompositeFormat")] String message, params object[] args) : base(database.Language.Translate(langKey, message, args)) { }
        public BriefingRoomException(IDatabase database, string langKey, string message, Exception innerException) : base(database.Language.Translate(langKey, message), innerException: innerException) { }
        public BriefingRoomException(IDatabase database, string langKey, [StringSyntax("CompositeFormat")] String message, Exception innerException, params object[] args) : base(database.Language.Translate(langKey, message, args), innerException: innerException) { }
    }
}

public class BriefingRoomRawException : Exception
{
    public BriefingRoomRawException(string message) : base(message) { }
    public BriefingRoomRawException(string message, Exception innerException) : base(message, innerException) { }

}
