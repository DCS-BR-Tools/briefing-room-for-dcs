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
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.UpdateService
{
    /// <summary>
    /// Manages the update process for BriefingRoom applications.
    /// </summary>
    public class UpdateManager : IDisposable
    {
        private readonly GitHubReleaseClient _releaseClient;
        private readonly BackupManager _backupManager;
        private readonly UpdateOptions _options;
        private readonly string _installPath;
        private readonly string _executableName;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        /// <summary>
        /// Event raised during download to report progress (0-100).
        /// </summary>
        public event Action<int> DownloadProgressChanged;

        /// <summary>
        /// Event raised to report status messages.
        /// </summary>
        public event Action<string> StatusChanged;


        /// <summary>
        /// Creates a new UpdateManager instance.
        /// </summary>
        /// <param name="installPath">Path to the application installation directory.</param>
        /// <param name="executableName">Name of the main executable (e.g., "BriefingRoom-Desktop.exe").</param>
        /// <param name="options">Update configuration options.</param>
        public UpdateManager(string installPath, string executableName, UpdateOptions options = null)
        {
            _installPath = installPath ?? throw new ArgumentNullException(nameof(installPath));
            _executableName = executableName ?? throw new ArgumentNullException(nameof(executableName));
            _options = options ?? new UpdateOptions();

            _releaseClient = new GitHubReleaseClient(_options);
            _backupManager = new BackupManager(_installPath, _options);
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Checks if an update is available.
        /// </summary>
        /// <returns>The latest release info if an update is available, null otherwise.</returns>
        public async Task<Tuple<ReleaseInfo, ReleaseInfo>> GetLatestVersions(CancellationToken cancellationToken = default)
        {
            var releases = await _releaseClient.GetReleasesAsync(cancellationToken);
            releases = releases.Where(r => !r.IsDraft).ToArray(); // Exclude drafts
            releases = releases.OrderByDescending(r => r.PublishedAt).ToArray(); // Sort by publish date
            var latestBetaRelease = releases.FirstOrDefault(r => r.IsPrerelease);
            var latestStableRelease = releases.FirstOrDefault(r => !r.IsPrerelease);

            if (latestBetaRelease == null && latestStableRelease == null)
            {
                return null;
            }

            var currentVersionDate = GetCurrentBuildVersionDate();
            BriefingRoom.PrintToLog($"Current build version date: {currentVersionDate:yyyy-MM-dd HH:mm:ss}", LogMessageErrorLevel.Warning);
            var betaNewerThanStable = DateTime.Compare(latestBetaRelease.PublishedAt, latestStableRelease.PublishedAt) > 0;
            var newerStable = DateTime.Compare(latestStableRelease.PublishedAt, currentVersionDate) > 0;
            var newerBeta = DateTime.Compare(latestBetaRelease.PublishedAt, currentVersionDate) > 0;
            if (!newerStable && !newerBeta)
            {
                return null;
            }
            if (newerStable & !betaNewerThanStable)
            {
                return new Tuple<ReleaseInfo, ReleaseInfo>(latestStableRelease, null);
            }
            if (newerStable & betaNewerThanStable)
            {
                return new Tuple<ReleaseInfo, ReleaseInfo>(latestStableRelease, latestBetaRelease);
            }
            return new Tuple<ReleaseInfo, ReleaseInfo>(null, latestBetaRelease);
        }

        private DateTime GetCurrentBuildVersionDate()
        {
            if (DateTime.TryParseExact(BriefingRoom.BUILD_VERSION, "yyyyMMdd-HHmmss", null, System.Globalization.DateTimeStyles.None, out var date))
                return date;

            return DateTime.MinValue;
        }

        /// <summary>
        /// Downloads and extracts the update to a temporary folder.
        /// </summary>
        /// <returns>Path to the extracted update files.</returns>
        public async Task<string> DownloadUpdateAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
        {
            if (release?.Assets == null || release.Assets.Length == 0)
                throw new InvalidOperationException("Release has no downloadable assets");

            // Find the ZIP asset
            var zipAsset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (zipAsset == null)
                throw new InvalidOperationException("Release has no ZIP download");

            StatusChanged?.Invoke($"Downloading {zipAsset.Name}...");

            // Create temp folder
            var tempPath = Path.Combine(Path.GetTempPath(), "BriefingRoom-Update", Guid.NewGuid().ToString("N"));
            var zipPath = Path.Combine(tempPath, zipAsset.Name);
            var extractPath = Path.Combine(tempPath, "extracted");

            Directory.CreateDirectory(tempPath);
            BriefingRoom.PrintToLog($"Created temporary update folder: {tempPath}", LogMessageErrorLevel.Warning);
            Directory.CreateDirectory(extractPath);
            BriefingRoom.PrintToLog($"Created temporary extracted folder: {extractPath}", LogMessageErrorLevel.Warning);

            try
            {
                // Download with progress
                using var response = await _httpClient.GetAsync(zipAsset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? zipAsset.Size;

                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        totalRead += bytesRead;

                        var progress = (int)(totalRead * 100 / totalBytes);
                        DownloadProgressChanged?.Invoke(progress);
                    }
                }

                StatusChanged?.Invoke("Extracting update...");

                // Extract ZIP
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                BriefingRoom.PrintToLog($"Extracted update from {zipPath} to {extractPath}", LogMessageErrorLevel.Warning);

                // Clean up ZIP file
                File.Delete(zipPath);
                BriefingRoom.PrintToLog($"Deleted temporary ZIP file: {zipPath}", LogMessageErrorLevel.Warning);

                return extractPath;
            }
            catch
            {
                // Clean up on failure
                try { Directory.Delete(tempPath, recursive: true); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Launches the updater executable and exits the current application.
        /// </summary>
        /// <param name="extractedPath">Path to the extracted update files.</param>
        public void LaunchUpdaterAndExit(string extractedPath)
        {
            // Find Updater.exe in the extracted files
            var updaterPath = FindExe(extractedPath, "Updater.exe");
            var sourcePath = FindExe(extractedPath, _executableName).Replace(_executableName, "").TrimEnd(Path.DirectorySeparatorChar); // Get the folder containing the executable
            if (updaterPath == null)
                throw new FileNotFoundException($"Updater.exe not found in update package. Searched in: {extractedPath}");

            // Prepare backup path
            string backupPath = null;
            if (_options.CreateBackup)
            {
                backupPath = _backupManager.CreateBackupPath();
                BriefingRoom.PrintToLog($"Backup enabled. Backup will be created at: {backupPath}", LogMessageErrorLevel.Warning);
            }

            // Build arguments
            var skipPatterns = string.Join(";", _options.SkipPatterns);
            var args = $"--source \"{sourcePath}\" --target \"{_installPath}\" --exe \"{_executableName}\"";

            if (!string.IsNullOrEmpty(backupPath))
            {
                args += $" --backup \"{backupPath}\"";
            }

            if (!string.IsNullOrEmpty(skipPatterns))
            {
                args += $" --skip \"{skipPatterns}\"";
            }

            StatusChanged?.Invoke("Launching updater...");
            BriefingRoom.PrintToLog($"Launching updater: {updaterPath} with arguments: {args}", LogMessageErrorLevel.Warning);

            var startInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = args,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(updaterPath)
            };

            Process.Start(startInfo);

            // Exit the current application
            Environment.Exit(0);
        }

        /// <summary>
        /// Gets the list of user-modified files that will be backed up.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> GetUserModifiedFiles()
        {
            return _backupManager.GetUserModifiedFiles(_executableName);
        }


        /// <summary>
        /// Finds the Updater.exe in the extracted update folder.
        /// </summary>
        private static string FindExe(string extractedPath, string executableName)
        {
            // Check root
            var updaterPath = Path.Combine(extractedPath, executableName);
            if (File.Exists(updaterPath))
                return updaterPath;
    
            // Check bin folder
            updaterPath = Path.Combine(extractedPath, "bin", executableName);
            if (File.Exists(updaterPath))
                return updaterPath;

            // Search recursively
            var found = Directory.GetFiles(extractedPath, executableName, SearchOption.AllDirectories);
            return found.Length > 0 ? found[0] : null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _releaseClient?.Dispose();
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
        /// <summary>
        /// Deletes all contents in the BriefingRoom-Update temp directory.
        /// </summary>
        public static void CleanUpTempUpdateFolder()
        {
            var tempUpdatePath = Path.Combine(Path.GetTempPath(), "BriefingRoom-Update");
            if (Directory.Exists(tempUpdatePath))
            {
                try
                {
                    Directory.Delete(tempUpdatePath, recursive: true);
                    BriefingRoom.PrintToLog($"Deleted temp update folder: {tempUpdatePath}", LogMessageErrorLevel.Warning);
                }
                catch (Exception ex)
                {
                    BriefingRoom.PrintToLog($"Failed to delete temp update folder: {tempUpdatePath}. Error: {ex.Message}", LogMessageErrorLevel.Error);
                }
            }
        }
    }
}
