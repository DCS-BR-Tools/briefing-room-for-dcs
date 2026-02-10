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
using System.Reflection;

namespace BriefingRoom4DCS
{
    internal static class BRPaths
    {
        internal static string ROOT { get; } = FindRoot();

        internal static string DATABASE { get; } = Path.Combine(ROOT, "Database");

        internal static string DATABASEJSON { get; } = Path.Combine(ROOT, "DatabaseJSON");

        internal static string CUSTOMDATABASE { get; } = Path.Combine(ROOT, "CustomConfigs");

#if DEBUG
        internal static string DEBUGOUTPUT { get; } = Path.Combine(ROOT, "DebugOutput");
#endif

        internal static string INCLUDE { get; } = Path.Combine(ROOT, "Include");

        internal static string INCLUDE_HTML { get; } = Path.Combine(INCLUDE, "Html");

        internal static string INCLUDE_JPG { get; } = Path.Combine(INCLUDE, "Jpg");

        internal static string INCLUDE_LUA { get; } = Path.Combine(INCLUDE, "Lua");

        internal static string INCLUDE_MARKDOWN { get; } = Path.Combine(INCLUDE, "Markdown");

        internal static string INCLUDE_YAML { get; } = Path.Combine(INCLUDE, "Yaml");


        internal static string INCLUDE_LUA_MISSIONFEATURES { get; } = Path.Combine(INCLUDE_LUA, "MissionFeatures");

        internal static string INCLUDE_LUA_OPTIONSMISSION { get; } = Path.Combine(INCLUDE_LUA, "OptionsMission");

        internal static string INCLUDE_LUA_OBJECTIVEFEATURES { get; } = Path.Combine(INCLUDE_LUA, "ObjectiveFeatures");

        internal static string INCLUDE_LUA_OBJECTIVETRIGGERS { get; } = Path.Combine(INCLUDE_LUA, "ObjectiveTriggers");

        internal static string INCLUDE_LUA_MISSION { get; } = Path.Combine(INCLUDE_LUA, "Mission");

        internal static string INCLUDE_LUA_UNITS { get; } = Path.Combine(INCLUDE_LUA, "Units");

        internal static string INCLUDE_YAML_UNIT { get; } = Path.Combine(INCLUDE_YAML, "Unit");

        internal static string INCLUDE_YAML_GROUP { get; } = Path.Combine(INCLUDE_YAML, "Group");

        internal static string INCLUDE_OGG { get; } = Path.Combine(INCLUDE, "Ogg");

        internal static string MEDIA { get; } = Path.Combine(ROOT, "Media");

        internal static string MEDIA_ICONS16 { get; } = Path.Combine(MEDIA, "Icons16");

        internal static string MIZ_SCRIPTS = "l10n/DEFAULT/";
        internal static string MIZ_RESOURCES = $"{MIZ_SCRIPTS}resources/";
        internal static string MIZ_RESOURCES_OGG = $"{MIZ_RESOURCES}ogg/";

        private static string FindRoot(string path = "", int loop = 0)
        {
            if (string.IsNullOrEmpty(path))
                path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.GetFullPath(path);
            DirectoryInfo di = new(path);
            var directories = di.GetDirectories();
            
            // Check for Database directly in this folder (development layout)
            if (directories.Any(x => x.Name == "Database"))
                return Toolbox.NormalizeDirectoryPath(path);
            
            // Check for Database in bin/ subfolder (publish layout: exe at root, data in bin/)
            var binDir = directories.FirstOrDefault(x => x.Name == "bin");
            if (binDir != null && binDir.GetDirectories().Any(x => x.Name == "Database"))
                return Toolbox.NormalizeDirectoryPath(binDir.FullName);
            
            if (loop > 10)
                throw new Exception("Can't Find Database within 10 levels up on app");
            return FindRoot(Path.Combine(path, ".."), loop + 1);
        }
    }
}
