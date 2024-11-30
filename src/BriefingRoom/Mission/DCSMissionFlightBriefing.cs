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


using System.Collections.Generic;
using System.IO;
using BriefingRoom4DCS.Generator;

namespace BriefingRoom4DCS.Mission
{
    public class DCSMissionFlightBriefing
    {
        internal string Type { get; set; }
        public string Name { get; set; }
        internal List<string> Waypoints { get; set; }

        public string GetFlightBriefingKneeBoardHTML(string langKey)
        {
            string html = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "KneeboardHeader.html")) +
                Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "KneeboardFlight.html")) +
                Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "BriefingFooter.html"));
            html = BriefingRoom.LanguageDB.ReplaceValues(langKey, html);
            GeneratorTools.ReplaceKey(ref html, "BriefingFlightName", Name);
            GeneratorTools.ReplaceKey(ref html, "BriefingWaypoints", GeneratorTools.MakeHTMLTable(Waypoints));
            return html;
        }
    }
}
