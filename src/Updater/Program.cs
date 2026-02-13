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
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;

namespace BriefingRoom4DCS.Updater
{
    /// <summary>
    /// Standalone updater executable that replaces application files after the main app exits.
    /// 
    /// Usage: Updater.exe --source &lt;temp_path&gt; --target &lt;install_path&gt; --exe &lt;app.exe&gt; --backup &lt;backup_path&gt; --skip &lt;patterns&gt;
    /// </summary>
    internal class Program
    {
        private static string _logFile;

        static int Main(string[] args)
        {
            try
            {
                var options = ParseArguments(args);
                if (options == null)
                {
                    ShowUsage();
                    return 1;
                }

                _logFile = Path.Combine(options.TargetPath, "update.log");
                Log($"Updater started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Log($"Source: {options.SourcePath}");
                Log($"Target: {options.TargetPath}");
                Log($"Executable: {options.ExecutableName}");
                Log($"Backup: {options.BackupPath}");
                Log($"Skip patterns: {string.Join(", ", options.SkipPatterns)}");

                // Wait for main application to exit
                WaitForProcessToExit(options.ExecutableName);

                // Perform backup of user-modified files
                if (!string.IsNullOrEmpty(options.BackupPath))
                {
                    BackupUserFiles(options);
                }

                // Copy new files
                CopyFiles(options);

                // Launch the updated application
                LaunchApplication(options);

                // Clean up temp folder
                CleanupTempFolder(options.SourcePath);

                Log("Update completed successfully");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return 0;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                Log(ex.StackTrace);
                Console.WriteLine($"Update failed: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return 1;
            }
        }

        private static UpdaterOptions ParseArguments(string[] args)
        {
            Option<FileInfo> fileOption = new("--source")
            {
                Description = "Path to extracted update files",
                Required = true
            };
            Option<DirectoryInfo> targetOption = new("--target")
            {
                Description = "Path to application installation",
                Required = true
            };
            Option<string> exeOption = new("--exe")
            {
                Description = "Name of main executable (e.g., BriefingRoom-Desktop.exe)",
                Required = true
            };
            Option<DirectoryInfo> backupOption = new("--backup")
            {
                Description = "Path to store backup of user-modified files (optional)",
                Required = false
            };
            Option<string> skipOption = new("--skip")
            {
                Description = "Semicolon-separated patterns to skip (e.g., CustomConfigs;*.brt)",
                Required = false
            };

            RootCommand rootCommand = new("BriefingRoom Updater");
            rootCommand.Options.Add(fileOption);
            rootCommand.Options.Add(targetOption);
            rootCommand.Options.Add(exeOption);
            rootCommand.Options.Add(backupOption);
            rootCommand.Options.Add(skipOption);

            ParseResult parseResult = rootCommand.Parse(args);
            if (parseResult.Errors.Count == 0 && parseResult.GetValue(fileOption) is FileInfo parsedFile)
            {
                var options = new UpdaterOptions
                {
                    SourcePath = parsedFile.FullName,
                    TargetPath = parseResult.GetValue(targetOption)?.FullName,
                    ExecutableName = parseResult.GetValue(exeOption),
                    BackupPath = parseResult.GetValue(backupOption)?.FullName,
                    SkipPatterns = parseResult.GetValue(skipOption)?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()
                };
                return options;
            }
            foreach (ParseError parseError in parseResult.Errors)
            {
                Console.Error.WriteLine(parseError.Message);
            }
            return null;
        }

        private static void ShowUsage()
        {
            Console.WriteLine("BriefingRoom Updater");
            Console.WriteLine();
            Console.WriteLine("Usage: Updater.exe --source <path> --target <path> --exe <name> [--backup <path>] [--skip <patterns>]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --source  Path to extracted update files");
            Console.WriteLine("  --target  Path to application installation");
            Console.WriteLine("  --exe     Name of main executable (e.g., BriefingRoom-Desktop.exe)");
            Console.WriteLine("  --backup  Path to store backup of user-modified files (optional)");
            Console.WriteLine("  --skip    Semicolon-separated patterns to skip (e.g., CustomConfigs;*.brt)");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void WaitForProcessToExit(string executableName)
        {
            var processName = Path.GetFileNameWithoutExtension(executableName);
            Log($"Waiting for {processName} to exit...");

            int waitCount = 0;
            while (Process.GetProcessesByName(processName).Length > 0)
            {
                Thread.Sleep(500);
                waitCount++;
                if (waitCount > 60) // 30 second timeout
                {
                    Log("Timeout waiting for application to exit");
                    throw new TimeoutException($"Application {processName} did not exit within 30 seconds");
                }
            }

            // Small delay to ensure file handles are released
            Thread.Sleep(1000);
            Log("Application has exited");
        }

        private static void BackupUserFiles(UpdaterOptions options)
        {
            Log("Checking for user-modified files to backup...");

            var exePath = Path.Combine(options.TargetPath, options.ExecutableName);
            if (!File.Exists(exePath))
            {
                Log("Executable not found, skipping backup");
                return;
            }

            var exeTime = File.GetLastWriteTimeUtc(exePath);
            var backupDir = options.BackupPath;

            Directory.CreateDirectory(backupDir);

            var files = Directory.GetFiles(options.TargetPath, "*", SearchOption.AllDirectories);
            int backupCount = 0;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(options.TargetPath, file);

                // Skip system files
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext == ".exe" || ext == ".dll" || ext == ".pdb" || ext == ".deps" || ext == ".runtimeconfig")
                    continue;

                // Skip if in skip patterns
                if (ShouldSkipFile(relativePath, options.SkipPatterns))
                    continue;

                // Check if modified after exe was created
                var fileTime = File.GetLastWriteTimeUtc(file);
                if (fileTime > exeTime)
                {
                    var backupPath = Path.Combine(backupDir, relativePath);
                    var backupFileDir = Path.GetDirectoryName(backupPath);
                    Directory.CreateDirectory(backupFileDir);

                    File.Copy(file, backupPath, overwrite: true);
                    Log($"Backed up: {relativePath}");
                    backupCount++;
                }
            }

            Log($"Backed up {backupCount} user-modified files");
        }

        private static void CopyFiles(UpdaterOptions options)
        {
            Log("Copying update files...");

            var sourceFiles = Directory.GetFiles(options.SourcePath, "*", SearchOption.AllDirectories);
            int copyCount = 0;
            int skipCount = 0;

            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(options.SourcePath, sourceFile);

                // Check skip patterns
                if (ShouldSkipFile(relativePath, options.SkipPatterns))
                {
                    var targetPath = Path.Combine(options.TargetPath, relativePath);
                    if (File.Exists(targetPath))
                    {
                        Log($"Skipped (user config): {relativePath}");
                        skipCount++;
                        continue;
                    }
                }

                var destFile = Path.Combine(options.TargetPath, relativePath);
                var destDir = Path.GetDirectoryName(destFile);
                Directory.CreateDirectory(destDir);

                // Retry logic for locked files
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        File.Copy(sourceFile, destFile, overwrite: true);
                        copyCount++;
                        break;
                    }
                    catch (IOException) when (retries > 1)
                    {
                        retries--;
                        Log($"File locked, retrying: {relativePath}");
                        Thread.Sleep(1000);
                    }
                }
            }

            Log($"Copied {copyCount} files, skipped {skipCount} user configs");
        }

        private static bool ShouldSkipFile(string relativePath, string[] skipPatterns)
        {
            if (skipPatterns == null || skipPatterns.Length == 0)
                return false;

            relativePath = relativePath.Replace('\\', '/');

            foreach (var pattern in skipPatterns)
            {
                var normalizedPattern = pattern.Replace('\\', '/');

                // Directory pattern (e.g., "CustomConfigs")
                if (!normalizedPattern.Contains('*') && !normalizedPattern.Contains('.'))
                {
                    if (relativePath.StartsWith(normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // Extension pattern (e.g., "*.brt")
                else if (normalizedPattern.StartsWith("*."))
                {
                    var ext = normalizedPattern.Substring(1); // ".brt"
                    if (relativePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        // Only skip if at root level
                        if (!relativePath.Contains('/'))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void LaunchApplication(UpdaterOptions options)
        {
            var exePath = Path.Combine(options.TargetPath, options.ExecutableName);
            Log($"Launching {exePath}...");

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = options.TargetPath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private static void CleanupTempFolder(string tempPath)
        {
            try
            {
                Log($"Cleaning up temp folder: {tempPath}");
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Log($"Warning: Failed to cleanup temp folder: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(line);

            if (!string.IsNullOrEmpty(_logFile))
            {
                try
                {
                    File.AppendAllText(_logFile, line + Environment.NewLine);
                }
                catch
                {
                    // Ignore logging errors
                }
            }
        }
    }

    internal class UpdaterOptions
    {
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
        public string ExecutableName { get; set; }
        public string BackupPath { get; set; }
        public string[] SkipPatterns { get; set; } = Array.Empty<string>();
    }
}
