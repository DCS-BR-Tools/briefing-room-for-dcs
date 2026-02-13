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

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Configuration options for the update process.
    /// </summary>
    public class UpdateOptions
    {
        /// <summary>
        /// Patterns for files/folders to skip during update to preserve user customizations.
        /// </summary>
        public string[] SkipPatterns { get; set; } = new[]
        {
            "CustomConfigs",
            "*.brt",
            "*.cbrt"
        };

        /// <summary>
        /// Whether to create a backup of user-modified files before updating.
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Name of the backup folder (created in installation directory).
        /// </summary>
        public string BackupFolderName { get; set; } = ".backup";

        /// <summary>
        /// GitHub repository owner.
        /// </summary>
        public string GitHubOwner { get; set; } = "DCS-BR-Tools";

        /// <summary>
        /// GitHub repository name.
        /// </summary>
        public string GitHubRepo { get; set; } = "briefing-room-for-dcs";
    }
}
