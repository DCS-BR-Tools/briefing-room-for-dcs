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
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.Data
{
    internal class DBCommonNames
    {
        internal const int MISSION_NAMES_PART_COUNT = 4;

        internal LanguageString[] MissionNameParts { get; } = new LanguageString[MISSION_NAMES_PART_COUNT];

        internal LanguageString MissionNameTemplate { get; }

        internal LanguageString CampaignNameTemplate { get; }

        internal LanguageString[] UnitFamilies { get; } = new LanguageString[Toolbox.EnumCount<UnitFamily>()];

        internal LanguageString[] UnitGroups { get; } = new LanguageString[Toolbox.EnumCount<UnitFamily>()];

        internal LanguageString WPEgressName { get; }

        internal LanguageString WPFinalName { get; }

        internal LanguageString WPIngressName { get; }

        internal LanguageString WPBullseyeName { get; }

        internal LanguageString WPInitialName { get; }

        internal LanguageString WPObjectivesNames { get; }

        public DBCommonNames(DatabaseLanguage LangDB)
        {
            int i;

            BriefingRoom.PrintToLog("Loading common global settings...");
            INIFile ini = new(Path.Combine(BRPaths.DATABASE, "Names.ini"));
            var className = DBEntry.GetLanguageClassName(typeof(DBCommonNames));
            CampaignNameTemplate = ini.GetLangStrings(LangDB, className, "Name", "Campaign", "Template");
            MissionNameTemplate = ini.GetLangStrings(LangDB, className, "Name", "Mission", "Template");
            for (i = 0; i < MISSION_NAMES_PART_COUNT; i++)
                MissionNameParts[i] = ini.GetLangStrings(LangDB, className, "Name", "Mission", $"Part{i + 1}");

            for (i = 0; i < Toolbox.EnumCount<UnitFamily>(); i++)
            {
                var unitFamily = ((UnitFamily)i).ToString();
                var unitFamilyNames = ini.GetLangStrings(LangDB, className, "Name", "UnitFamilies", unitFamily);
                if (unitFamilyNames.Count() == 0)
                    throw new BriefingRoomException("en", $"No UnitFamilies name for {unitFamily}.");
                UnitFamilies[i] = unitFamilyNames;

                var unitGroupNames = ini.GetLangStrings(LangDB, className, "Name", "UnitGroups", unitFamily);
                if (unitGroupNames.Count() == 0)
                    throw new BriefingRoomException("en", $"No UnitGroups name for {unitFamily}.");
                UnitGroups[i] = unitGroupNames;
            }

            WPEgressName = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Egress");
            WPFinalName = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Final");
            WPIngressName = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Ingress");
            WPBullseyeName = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Bullseye");
            WPInitialName = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Initial");
            WPObjectivesNames = ini.GetLangStrings(LangDB, className, "Name", "Waypoints", "Objectives");
        }
    }
}