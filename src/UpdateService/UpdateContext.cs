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
using System.Reflection;

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Provides runtime context about the current application installation.
    /// </summary>
    public static class UpdateContext
    {
        private static string _installPath;
        private static string _executableName;

        /// <summary>
        /// Gets the installation directory path.
        /// </summary>
        public static string InstallPath
        {
            get
            {
                if (_installPath == null)
                {
                    // Get path from entry assembly location
                    var entryLocation = Assembly.GetEntryAssembly()?.Location;

                    if (!string.IsNullOrEmpty(entryLocation))
                    {
                        _installPath = Path.GetDirectoryName(entryLocation);
                    }
                    else
                    {
                        // Fallback for single-file apps
                        _installPath = AppContext.BaseDirectory;
                    }

                    // Normalize path
                    _installPath = Path.GetFullPath(_installPath).TrimEnd(Path.DirectorySeparatorChar);
                }

                return _installPath;
            }
        }

        /// <summary>
        /// Gets the name of the current executable.
        /// </summary>
        public static string ExecutableName
        {
            get
            {
                if (_executableName == null)
                {
                    var processPath = Environment.ProcessPath;

                    if (!string.IsNullOrEmpty(processPath))
                    {
                        _executableName = Path.GetFileName(processPath);
                    }
                    else
                    {
                        // Fallback - detect based on assembly name
                        var entryAssembly = Assembly.GetEntryAssembly();
                        var assemblyName = entryAssembly?.GetName().Name ?? "BriefingRoom";

                        _executableName = assemblyName.Contains("Desktop")
                            ? "BriefingRoom-Desktop.exe"
                            : "BriefingRoom-Web.exe";
                    }
                }

                return _executableName;
            }
        }

        /// <summary>
        /// Gets whether this is the Desktop application.
        /// </summary>
        public static bool IsDesktop =>
            ExecutableName.Contains("Desktop", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets whether this is the Web application.
        /// </summary>
        public static bool IsWeb =>
            ExecutableName.Contains("Web", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Creates an UpdateManager configured for the current application context.
        /// </summary>
        public static UpdateManager CreateUpdateManager(UpdateOptions options = null)
        {
            return new UpdateManager(InstallPath, ExecutableName, options);
        }
    }
}
