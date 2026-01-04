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
using BriefingRoom4DCS.Template;

namespace BriefingRoom4DCS.Data
{
    internal abstract class DBEntryFeature : DBEntry
    {
        internal abstract string SourceLuaDirectory { get; }

        internal List<LanguageString> BriefingRemarks { get; private set; }

        internal string[] IncludeLua { get; private set; }

        internal string[] IncludeOgg { get; private set; }

        internal string IncludeOggFolder { get; private set; }

        internal UnitFamily[] UnitGroupFamilies { get; private set; }

        internal FeatureUnitGroupFlags UnitGroupFlags { get; private set; }

        internal string UnitGroupLuaGroup { get; set; }

        internal string UnitGroupLuaUnit { get; private set; }

        internal MinMaxI UnitGroupSize { get; private set; }

        internal MinMaxI ExtraGroups { get; private set; }

        internal double UnitGroupSpawnDistance { get; private set; }

        internal DCSTask UnitGroupTask { get; private set; }
        internal bool UnitGroupAllowStatic {get; set;}

        internal SpawnPointType[] UnitGroupValidSpawnPoints { get; private set; }



        protected override bool OnLoad(string iniFilePath)
        {
            var ini = new INIFile(iniFilePath);
            var className = this.GetLanguageClassName();
            BriefingRemarks = new List<LanguageString>
            {
                ini.GetLangStrings(Database.Language, className, ID,"Briefing", "Remarks"),
                ini.GetLangStrings(Database.Language, className, ID,"Briefing", "Remarks.Enemy")
            };

            // Included files
            IncludeLua = Toolbox.AddMissingFileExtensions(ini.GetValueArray<string>("Include", "Lua"), ".lua");
            IncludeOgg = Toolbox.AddMissingFileExtensions(ini.GetValueArray<string>("Include", "Ogg"), ".ogg");
            IncludeOggFolder = ini.GetValue<string>("Include", "OggFolder");

            foreach (string f in IncludeLua)
                if (!File.Exists(Path.Combine(SourceLuaDirectory, f)))
                    BriefingRoom.PrintToLog($"File \"{SourceLuaDirectory}{f}\", required by feature \"{ID}\", doesn't exist.", LogMessageErrorLevel.Warning);

            foreach (string f in IncludeOgg)
                if (!File.Exists(Path.Combine(BRPaths.INCLUDE_OGG, f)))
                    BriefingRoom.PrintToLog($"File \"{BRPaths.INCLUDE_OGG}{f}\", required by feature \"{ID}\", doesn't exist.", LogMessageErrorLevel.Warning);

            // Unit group
            UnitGroupFamilies = ini.GetValueArray<UnitFamily>("UnitGroup", "Families");
            UnitGroupFlags = ini.GetValueArrayAsEnumFlags<FeatureUnitGroupFlags>("UnitGroup", "Flags");
            UnitGroupLuaGroup = ini.GetValue<string>("UnitGroup", "Lua.Group");
            UnitGroupLuaUnit = ini.GetValue<string>("UnitGroup", "Lua.Unit");
            UnitGroupSize = ini.GetValue<MinMaxI>("UnitGroup", "Size");
            ExtraGroups = ini.GetValue<MinMaxI>("UnitGroup", "ExtraGroups");
            UnitGroupSpawnDistance = ini.GetValue<double>("UnitGroup", "SpawnDistance");
            UnitGroupTask = ini.GetValue<DCSTask>("UnitGroup", "Task", DCSTask.Nothing);
            UnitGroupValidSpawnPoints = DatabaseTools.CheckSpawnPoints(ini.GetValueArray<SpawnPointType>("UnitGroup", "ValidSpawnPoints"));
            UnitGroupAllowStatic = ini.GetValue<bool>("UnitGroup", "AllowStatic");

            return true;
        }
    }
}
