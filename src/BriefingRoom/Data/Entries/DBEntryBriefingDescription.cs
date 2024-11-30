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

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryBriefingDescription : DBEntry
    {
        internal List<LanguageString> DescriptionText { get; private set; }


        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            var className = this.GetLanguageClassName();
            LanguageString defaultTexts = ini.GetLangStrings(Database.Language, className, ID, "BriefingDescription", "Description");

            DescriptionText = new List<LanguageString>();
            for (int i = 0; i < Toolbox.EnumCount<UnitFamily>(); i++)
            {
                DescriptionText.Add(defaultTexts);
                var data = ini.GetLangStrings(Database.Language, className, ID, "BriefingDescription", $"Description.{(UnitFamily)i}");
                if (data.Count > 0)
                    DescriptionText[i] = data;
            }

            return true;
        }
    }
}
