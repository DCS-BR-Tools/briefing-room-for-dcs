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

using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Data
{
    internal class DBEntryObjectivePreset : DBEntry
    {
        internal string[] Features { get; private set; }

        internal ObjectiveOption[] Options { get; private set; }


        internal string[] Targets { get; private set; }

        internal string[] TargetsBehaviors { get; private set; }

        internal string Task { get; private set; }

        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            Features = Database.CheckIDs<DBEntryFeatureObjective>(ini.GetValueArray<string>("ObjectivePreset", "Features"));
            Options = ini.GetValueArray<ObjectiveOption>("ObjectivePreset", "Options");
            Targets = Database.CheckIDs<DBEntryObjectiveTarget>(ini.GetValueArray<string>("ObjectivePreset", "Targets"));
            if (Targets.Length == 0) { BriefingRoom.PrintToLog($"No valid targets for objective preset \"{ID}\"", LogMessageErrorLevel.Warning); return false; }
            TargetsBehaviors = Database.CheckIDs<DBEntryObjectiveTargetBehavior>(ini.GetValueArray<string>("ObjectivePreset", "TargetsBehaviors"));
            if (TargetsBehaviors.Length == 0) { BriefingRoom.PrintToLog($"No valid target behaviors for objective preset \"{ID}\"", LogMessageErrorLevel.Warning); return false; }
            Task = Database.CheckID<DBEntryObjectiveTask>(ini.GetValue<string>("ObjectivePreset", "Task"));
            if (string.IsNullOrEmpty(Task)) { BriefingRoom.PrintToLog($"No valid task for objective preset \"{ID}\"", LogMessageErrorLevel.Warning); return false; }

            return true;
        }
    }
}
