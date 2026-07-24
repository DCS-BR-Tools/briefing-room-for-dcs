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

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace BriefingRoom4DCS.Generator
{
    /// <summary>
    /// Manages the headless browser instance and page pool used for image generation.
    /// </summary>
    internal static class BrowserManager
    {
        private static IBrowser? _browser;
        private static readonly object _browserLock = new();
        private static bool _browserInitialized;

        // Page pool for reuse - avoids creating new pages for each render
        private static readonly ConcurrentBag<IPage> _pagePool = new();
        private const int MaxPooledPages = 4;
        private static readonly string BrowserCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BriefingRoom");
        private static readonly string LastWorkingBrowserPathFile = Path.Combine(BrowserCacheDirectory, "last-working-browser-path.txt");

        private static readonly string[] FirefoxBasedExecutableNames = ["firefox", "librewolf", "waterfox"];

        /// <summary>
        /// Initialize the browser for HTML rendering. Call this at application startup.
        /// </summary>
        internal static async Task InitializeAsync()
        {
            if (_browserInitialized) return;

            lock (_browserLock)
            {
                if (_browserInitialized) return;
                _browserInitialized = true;
            }

            var failures = new StringBuilder();
            var attemptedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var executablePath in GetInstalledBrowserCandidates().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                attemptedPaths.Add(executablePath);
                if (await TryLaunchBrowserAsync(executablePath, failures))
                    return;
            }

            // Final fallback: try downloading Chromium and launching it.
            try
            {
                BriefingRoom.PrintToLog("BrowserManager: no working installed browser found, downloading Chromium fallback...", LogMessageErrorLevel.Warning);
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                var downloadedPath = browserFetcher.GetInstalledBrowsers().First().GetExecutablePath();
                if (!attemptedPaths.Contains(downloadedPath))
                {
                    if (await TryLaunchBrowserAsync(downloadedPath, failures))
                        return;
                }
            }
            catch (Exception ex)
            {
                failures.AppendLine($"Chromium download fallback failed: {ex.GetType().Name} - {ex.Message}");
                BriefingRoom.PrintToLog($"BrowserManager: Chromium download fallback failed ({ex.Message})", LogMessageErrorLevel.Error);
            }

            throw new InvalidOperationException($"Failed to launch any supported browser for imagery generation.{Environment.NewLine}{failures}");
        }

        /// <summary>
        /// Shutdown the browser. Call this at application shutdown.
        /// </summary>
        internal static async Task ShutdownAsync()
        {
            // Clean up pooled pages
            while (_pagePool.TryTake(out var page))
            {
                page.Dispose();
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser.Dispose();
                _browser = null;
                _browserInitialized = false;
            }
        }

        internal static async Task<IPage> GetPooledPageAsync()
        {
            if (_pagePool.TryTake(out var page))
            {
                return page;
            }

            var browser = await GetBrowserAsync();
            return await browser.NewPageAsync();
        }

        internal static void ReturnPageToPool(IPage page)
        {
            if (_pagePool.Count < MaxPooledPages)
            {
                _pagePool.Add(page);
            }
            else
            {
                page.Dispose();
            }
        }

        private static async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null)
                await InitializeAsync();
            return _browser!;
        }

        private static async Task<bool> TryLaunchBrowserAsync(string executablePath, StringBuilder failures)
        {
            var isFirefox = IsFirefoxExecutable(executablePath);
            var browserArgs = GetBrowserArgs(isFirefox);
            try
            {
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executablePath,
                    Browser = isFirefox ? SupportedBrowser.Firefox : SupportedBrowser.Chrome,
                    Args = browserArgs,
                    Timeout = 60000
                });

                SaveLastWorkingBrowserPath(executablePath);
                BriefingRoom.PrintToLog($"BrowserManager: launched {(isFirefox ? "Firefox-based" : "Chromium-based")} browser '{executablePath}'.");
                return true;
            }
            catch (Exception ex)
            {
                InvalidateLastWorkingBrowserPath(executablePath);
                failures.AppendLine($"Browser launch failed for '{executablePath}': {ex.GetType().Name} - {ex.Message}");
                BriefingRoom.PrintToLog($"BrowserManager: failed to launch '{executablePath}' ({ex.Message})", LogMessageErrorLevel.Warning);
                return false;
            }
        }

        private static string[] GetBrowserArgs(bool isFirefox)
        {
            if (isFirefox)
            {
                // Firefox uses minimal args; headless mode is handled by PuppeteerSharp via Headless = true
                return BriefingRoom.RUNNING_IN_DOCKER
                    ? ["-safe-mode"]
                    : [];
            }

            // Chromium-based browser args
            var chromiumArgs = new List<string>
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-extensions",
                "--disable-background-networking",
                "--disable-sync",
                "--disable-translate",
                "--disable-default-apps",
                "--no-first-run"
            };

            if (BriefingRoom.RUNNING_IN_DOCKER)
            {
                // Disable GPU in Docker (no display/driver)
                chromiumArgs.Add("--disable-gpu");
                chromiumArgs.Add("--single-process");
            }
            else
            {
                // Enable GPU acceleration on desktop
                chromiumArgs.Add("--enable-gpu-rasterization");
                chromiumArgs.Add("--enable-accelerated-2d-canvas");
            }

            return chromiumArgs.ToArray();
        }

        private static IEnumerable<string> GetInstalledBrowserCandidates()
        {
            var lastWorkingPath = GetLastWorkingBrowserPath();
            if (!string.IsNullOrWhiteSpace(lastWorkingPath) && File.Exists(lastWorkingPath))
                yield return lastWorkingPath;

            // Check environment variable first
            var envPath = Environment.GetEnvironmentVariable("CHROME_PATH")
                       ?? Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH")
                       ?? Environment.GetEnvironmentVariable("FIREFOX_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                yield return envPath;

            string[] candidates = OperatingSystem.IsWindows() ?
            [
                // Chrome
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                // Edge
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                // Brave
                @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
                @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                // Opera
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Opera\opera.exe"),
                @"C:\Program Files\Opera\opera.exe",
                // Vivaldi
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Vivaldi\Application\vivaldi.exe"),
                @"C:\Program Files\Vivaldi\Application\vivaldi.exe",
                // Firefox
                @"C:\Program Files\Mozilla Firefox\firefox.exe",
                @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Mozilla Firefox\firefox.exe"),
                // LibreWolf (Firefox-based)
                @"C:\Program Files\LibreWolf\librewolf.exe",
                @"C:\Program Files (x86)\LibreWolf\librewolf.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"LibreWolf\librewolf.exe"),
                // Waterfox (Firefox-based)
                @"C:\Program Files\Waterfox\waterfox.exe",
                @"C:\Program Files (x86)\Waterfox\waterfox.exe",
            ] :
            [
                // Linux - check multiple paths as different distros install to different locations
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser",
                "/usr/lib/chromium/chromium",
                "/usr/lib/chromium-browser/chromium-browser",
                "/usr/bin/google-chrome",
                "/usr/bin/google-chrome-stable",
                "/usr/bin/brave-browser",
                "/snap/bin/brave",
                "/snap/bin/chromium",
                // Opera
                "/usr/bin/opera",
                "/snap/bin/opera",
                // Vivaldi
                "/usr/bin/vivaldi",
                "/usr/bin/vivaldi-stable",
                // Firefox
                "/usr/bin/firefox",
                "/usr/bin/firefox-esr",
                "/usr/lib/firefox/firefox",
                "/usr/lib/firefox-esr/firefox-esr",
                "/snap/bin/firefox",
                "/opt/firefox/firefox",
                // LibreWolf (Firefox-based)
                "/usr/bin/librewolf",
                "/snap/bin/librewolf",
                "/opt/librewolf/librewolf",
                // Waterfox (Firefox-based)
                "/usr/bin/waterfox",
                "/opt/waterfox/waterfox"
            ];

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    yield return candidate;
            }
        }

        private static string? GetLastWorkingBrowserPath()
        {
            try
            {
                if (!File.Exists(LastWorkingBrowserPathFile))
                    return null;

                var path = File.ReadAllText(LastWorkingBrowserPathFile).Trim();
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                return path;
            }
            catch (Exception ex)
            {
                BriefingRoom.PrintToLog($"BrowserManager: unable to read last-working browser path ({ex.Message})", LogMessageErrorLevel.Warning);
                return null;
            }
        }

        private static void SaveLastWorkingBrowserPath(string executablePath)
        {
            try
            {
                if (!Toolbox.CreateMissingDirectory(BrowserCacheDirectory))
                {
                    BriefingRoom.PrintToLog($"BrowserManager: unable to create cache directory '{BrowserCacheDirectory}'.", LogMessageErrorLevel.Warning);
                    return;
                }

                File.WriteAllText(LastWorkingBrowserPathFile, executablePath);
            }
            catch (Exception ex)
            {
                BriefingRoom.PrintToLog($"BrowserManager: unable to persist last-working browser path ({ex.Message})", LogMessageErrorLevel.Warning);
            }
        }

        private static void InvalidateLastWorkingBrowserPath(string executablePath)
        {
            try
            {
                if (!File.Exists(LastWorkingBrowserPathFile))
                    return;

                var cachedPath = File.ReadAllText(LastWorkingBrowserPathFile).Trim();
                if (!string.Equals(cachedPath, executablePath, StringComparison.OrdinalIgnoreCase))
                    return;

                File.Delete(LastWorkingBrowserPathFile);
            }
            catch (Exception ex)
            {
                BriefingRoom.PrintToLog($"BrowserManager: unable to invalidate last-working browser path ({ex.Message})", LogMessageErrorLevel.Warning);
            }
        }

        private static bool IsFirefoxExecutable(string? path) =>
            path != null && FirefoxBasedExecutableNames.Any(name =>
                Path.GetFileNameWithoutExtension(path).Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
