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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Manages backup of user-modified files before update.
    /// </summary>
    public class BackupManager
    {
        private readonly string _installPath;
        private readonly UpdateOptions _options;

        public BackupManager(string installPath, UpdateOptions options = null)
        {
            _installPath = installPath ?? throw new ArgumentNullException(nameof(installPath));
            _options = options ?? new UpdateOptions();
        }

        /// <summary>
        /// Gets list of files that have been modified since the application was installed.
        /// Files are considered modified if their last write time is after the main executable's timestamp.
        /// </summary>
        public IReadOnlyList<string> GetUserModifiedFiles(string executableName)
        {
            var exePath = Path.Combine(_installPath, executableName);
            if (!File.Exists(exePath))
                return Array.Empty<string>();

            var exeTime = File.GetLastWriteTimeUtc(exePath);
            var modifiedFiles = new List<string>();

            var allFiles = Directory.GetFiles(_installPath, "*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                var relativePath = Path.GetRelativePath(_installPath, file);

                // Skip system/binary files
                if (IsSystemFile(file))
                    continue;

                // Skip files in skip patterns (they'll be preserved anyway)
                if (IsInSkipPattern(relativePath))
                    continue;

                // Check if modified after exe was created
                var fileTime = File.GetLastWriteTimeUtc(file);
                if (fileTime > exeTime)
                {
                    modifiedFiles.Add(relativePath);
                }
            }

            return modifiedFiles;
        }

        /// <summary>
        /// Creates a backup folder path with timestamp.
        /// </summary>
        public string CreateBackupPath()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            return Path.Combine(_installPath, _options.BackupFolderName, timestamp);
        }

        /// <summary>
        /// Checks if a file should be skipped based on configured patterns.
        /// </summary>
        private bool IsInSkipPattern(string relativePath)
        {
            if (_options.SkipPatterns == null || _options.SkipPatterns.Length == 0)
                return false;

            relativePath = relativePath.Replace('\\', '/');

            foreach (var pattern in _options.SkipPatterns)
            {
                var normalizedPattern = pattern.Replace('\\', '/');

                // Directory pattern
                if (!normalizedPattern.Contains('*') && !normalizedPattern.Contains('.'))
                {
                    if (relativePath.StartsWith(normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // Extension pattern at root
                else if (normalizedPattern.StartsWith("*."))
                {
                    var ext = normalizedPattern.Substring(1);
                    if (relativePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase) && !relativePath.Contains('/'))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a file is a system/binary file that shouldn't be backed up.
        /// </summary>
        private static bool IsSystemFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var systemExtensions = new[] { ".exe", ".dll", ".pdb", ".deps", ".runtimeconfig" };
            return systemExtensions.Contains(ext);
        }
    }
}
