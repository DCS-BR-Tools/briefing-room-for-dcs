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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace BriefingRoom4DCS.Generator
{
    /// <summary>
    /// Manages the headless browser instance used for image generation.
    /// </summary>
    internal static class BrowserManager
    {
        private static IBrowser? _browser;
        private static readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private static bool _browserInitialized;

        private static readonly string BrowserCacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BriefingRoom");
        private static readonly string LastWorkingBrowserPathFile = Path.Combine(BrowserCacheDirectory, "last-working-browser-path.txt");

        private static readonly string[] FirefoxBasedExecutableNames = ["firefox", "librewolf", "waterfox"];

        /// <summary>
        /// Initialize the browser for HTML rendering. Call this at application startup.
        /// </summary>
        internal static async Task InitializeAsync()
        {
            if (Volatile.Read(ref _browserInitialized)) return;

            // SemaphoreSlim(1,1) gives async-safe exclusive access so concurrent callers
            // wait rather than racing past each other. The flag is only set on success,
            // so a failed attempt releases the semaphore and allows a future retry.
            await _initSemaphore.WaitAsync();
            try
            {
                if (Volatile.Read(ref _browserInitialized)) return;

                var failures = new StringBuilder();
                var attemptedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var candidateCount = 0;

                foreach (var executablePath in GetInstalledBrowserCandidates().Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    candidateCount++;
                    attemptedPaths.Add(executablePath);
                    if (await TryLaunchBrowserAsync(executablePath, failures))
                    {
                        Volatile.Write(ref _browserInitialized, true);
                        return;
                    }
                }

                BriefingRoom.PrintToLog($"BrowserManager: all {candidateCount} installed browser candidate(s) failed. Attempting Chromium download fallback...", LogMessageErrorLevel.Warning);

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
                        {
                            Volatile.Write(ref _browserInitialized, true);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures.AppendLine($"Chromium download fallback failed: {ex.GetType().Name} - {ex.Message}");
                    BriefingRoom.PrintToLog($"BrowserManager: Chromium download fallback failed ({ex.Message})", LogMessageErrorLevel.Error);
                }

                throw new InvalidOperationException($"Failed to launch any supported browser for imagery generation.{Environment.NewLine}{failures}");
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        /// <summary>
        /// Shutdown the browser. Call this at application shutdown.
        /// </summary>
        internal static async Task ShutdownAsync()
        {
            await _initSemaphore.WaitAsync();
            try
            {
                var browserToClose = _browser;
                _browser = null;
                Volatile.Write(ref _browserInitialized, false);

                if (browserToClose != null)
                {
                    await browserToClose.CloseAsync();
                    browserToClose.Dispose();
                }
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        internal static async Task<IPage> GetPooledPageAsync()
        {
            var browser = await GetBrowserAsync();
            return await browser.NewPageAsync();
        }

        internal static async Task<IPage> GetFreshPageAsync()
        {
            var browser = await GetBrowserAsync();
            return await browser.NewPageAsync();
        }

        internal static void ReturnPageToPool(IPage page)
        {
            try
            {
                page.Dispose();
            }
            catch (Exception ex)
            {
                BriefingRoom.PrintToLog($"BrowserManager: failed to dispose page ({ex.Message})", LogMessageErrorLevel.Warning);
            }
        }

        internal static void ClearPagePool()
        {
            // No-op: page pooling has been removed.
        }

        private static async Task<IBrowser> GetBrowserAsync()
        {
            if (!Volatile.Read(ref _browserInitialized) || _browser == null)
                await InitializeAsync();

            var browser = _browser;
            if (browser == null)
                throw new InvalidOperationException("BrowserManager initialization completed without a browser instance.");

            return browser;
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
                // Canary test: verify the browser can actually render and screenshot.
                // This catches issues that only appear at render-time (e.g. display server issues, missing libs).
                if (!await CanaryTestBrowserAsync(_browser, failures))
                {
                    BriefingRoom.PrintToLog($"BrowserManager: canary test FAILED for '{Path.GetFileName(executablePath)}'.", LogMessageErrorLevel.Warning);
                    InvalidateLastWorkingBrowserPath(executablePath);
                    _browser.Dispose();
                    _browser = null;
                    return false;
                }

                SaveLastWorkingBrowserPath(executablePath);
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

        private static async Task<bool> CanaryTestBrowserAsync(IBrowser browser, StringBuilder failures)
        {
            IPage? testPage = null;
            try
            {
                testPage = await browser.NewPageAsync();
                await testPage.SetViewportAsync(new ViewPortOptions { Width = 256, Height = 256 });

                // Load minimal HTML to test rendering and JS execution
                var testHtml = "<html><body>Canary</body></html>";
                await testPage.SetContentAsync(testHtml, new SetContentOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });

                // Verify JS execution works by reading a simple property
                var bodyText = await testPage.EvaluateExpressionAsync<string>("document.body.textContent");
                if (string.IsNullOrEmpty(bodyText) || !bodyText.Contains("Canary"))
                {
                    BriefingRoom.PrintToLog($"BrowserManager: canary test: JS evaluation returned '{bodyText}' (expected 'Canary')", LogMessageErrorLevel.Warning);
                    failures.AppendLine($"Browser canary test failed: JS evaluation did not return expected content (got '{bodyText}')");
                    return false;
                }
                // Test screenshot capability
                var tempPath = Path.Combine(Path.GetTempPath(), $"br-canary-{Guid.NewGuid()}.png");
                await testPage.ScreenshotAsync(tempPath, new ScreenshotOptions { Type = ScreenshotType.Png });

                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
                {
                    BriefingRoom.PrintToLog($"BrowserManager: canary test: screenshot failed (file missing or empty)", LogMessageErrorLevel.Warning);
                    failures.AppendLine($"Browser canary test failed: screenshot produced no valid file");
                    return false;
                }
                File.Delete(tempPath);
                return true;
            }
            catch (Exception ex)
            {
                BriefingRoom.PrintToLog($"BrowserManager: canary test exception: {ex.GetType().Name}: {ex.Message}", LogMessageErrorLevel.Warning);
                failures.AppendLine($"Browser canary test failed for: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
            finally
            {
                if (testPage != null)
                {
                    try
                    {
                        await testPage.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        BriefingRoom.PrintToLog($"BrowserManager: canary test: failed to close test page ({ex.Message})", LogMessageErrorLevel.Warning);
                    }
                }
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

            // Disable GPU: headless screenshot rendering does not benefit from GPU
            // acceleration, and GPU can cause renderer hangs or crashes on machines
            // without a proper display driver (common in headless/server environments).
            chromiumArgs.Add("--disable-gpu");

            if (BriefingRoom.RUNNING_IN_DOCKER)
                chromiumArgs.Add("--single-process");

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
