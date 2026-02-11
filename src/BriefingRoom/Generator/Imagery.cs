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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.Generator
{
    public class Imagery
    {
        private static string UNKOWN_IMAGE_PATH = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"Unknown.png");
        private static IBrowser? _browser;
        private static readonly object _browserLock = new();
        private static bool _browserInitialized;
        
        // Page pool for reuse - avoids creating new pages for each render
        private static readonly ConcurrentBag<IPage> _pagePool = new();
        private const int MaxPooledPages = 4;

        /// <summary>
        /// Initialize the browser for HTML rendering. Call this at application startup.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (_browserInitialized) return;
            
            lock (_browserLock)
            {
                if (_browserInitialized) return;
                _browserInitialized = true;
            }

            var executablePath = FindInstalledBrowser();
            
            if (executablePath == null)
            {
                // Download Chromium if no browser found
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                executablePath = browserFetcher.GetInstalledBrowsers().First().GetExecutablePath();
            }

            // Build browser args - enable GPU acceleration on desktop, disable in Docker
            var browserArgs = new List<string>
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
                browserArgs.Add("--disable-gpu");
                browserArgs.Add("--single-process");
            }
            else
            {
                // Enable GPU acceleration on desktop
                browserArgs.Add("--enable-gpu-rasterization");
                browserArgs.Add("--enable-accelerated-2d-canvas");
            }

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = browserArgs.ToArray()
            });
        }

        private static async Task<IPage> GetPooledPageAsync()
        {
            if (_pagePool.TryTake(out var page))
            {
                return page;
            }
            
            var browser = await GetBrowserAsync();
            return await browser.NewPageAsync();
        }

        private static void ReturnPageToPool(IPage page)
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

        private static string? FindInstalledBrowser()
        {
            // Check environment variable first
            var envPath = Environment.GetEnvironmentVariable("CHROME_PATH") 
                       ?? Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

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
                "/snap/bin/chromium"
            ];

            return candidates.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Shutdown the browser. Call this at application shutdown.
        /// </summary>
        public static async Task ShutdownAsync()
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

        private static async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser == null)
                await InitializeAsync();
            return _browser!;
        }
        internal static async Task GenerateCampaignImages(IDatabase database, CampaignTemplate campaignTemplate, DCSCampaign campaign, string baseFileName)
        {
            string titleHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignTitleImage.html"));
            string winHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignWinImage.html"));
            string lossHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignLossImage.html"));
            string[] theaterImages = Directory.GetFiles(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters"), $"{database.GetEntry<DBEntryTheater>(campaignTemplate.ContextTheater).DCSID}*.*")
                .Where(x => x.EndsWith(".jpg") || x.EndsWith(".png")).ToArray();
            var backgroundImage = "_default.jpg";
            if (theaterImages.Length > 0)
                backgroundImage = Path.GetFileName(Toolbox.RandomFrom(theaterImages));

            GeneratorTools.ReplaceKey(ref titleHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters", backgroundImage)));
            GeneratorTools.ReplaceKey(ref winHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Sky.jpg")));
            GeneratorTools.ReplaceKey(ref lossHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Fire.jpg")));
            var playerFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{campaignTemplate.GetCoalitionID(campaignTemplate.ContextPlayerCoalition)}.png");
            if (!File.Exists(playerFlagPath))
                playerFlagPath = UNKOWN_IMAGE_PATH;
        
            GeneratorTools.ReplaceKey(ref titleHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
            GeneratorTools.ReplaceKey(ref winHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
            GeneratorTools.ReplaceKey(ref lossHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));

            var enemyFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{campaignTemplate.GetCoalitionID(campaignTemplate.ContextPlayerCoalition.GetEnemy())}.png");
            
            if (!File.Exists(enemyFlagPath))
                enemyFlagPath = UNKOWN_IMAGE_PATH;
            
            GeneratorTools.ReplaceKey(ref titleHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
            GeneratorTools.ReplaceKey(ref winHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
            GeneratorTools.ReplaceKey(ref lossHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
           

            GeneratorTools.ReplaceKey(ref titleHTML, "MissionName", campaign.Name);
            GeneratorTools.ReplaceKey(ref winHTML, "MissionName", campaign.Name);
            GeneratorTools.ReplaceKey(ref lossHTML, "MissionName", campaign.Name);

            GeneratorTools.ReplaceKey(ref titleHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));
            GeneratorTools.ReplaceKey(ref winHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));
            GeneratorTools.ReplaceKey(ref lossHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));

            var langKey = campaign.Missions[0].LangKey;
            
            // Render all 3 campaign images in parallel
            await Task.WhenAll(
                GenerateCampaignImageAsync(database, langKey, titleHTML, campaign, $"{baseFileName}_Title"),
                GenerateCampaignImageAsync(database, langKey, winHTML, campaign, $"{baseFileName}_Success"),
                GenerateCampaignImageAsync(database, langKey, lossHTML, campaign, $"{baseFileName}_Failure")
            );

        }

        internal static async Task GenerateTitleImage(IDatabase database, DCSMission mission)
        {
            string html = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "MissionTitleImage.html"));
            string[] theaterImages = Directory.GetFiles(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters"), $"{database.GetEntry<DBEntryTheater>(mission.TemplateRecord.ContextTheater).DCSID}*.*")
               .Where(x => x.EndsWith(".jpg") || x.EndsWith(".png")).ToArray();
            var backgroundImage = "_default.jpg";
            if (theaterImages.Length > 0)
                backgroundImage = Path.GetFileName(Toolbox.RandomFrom(theaterImages));
            GeneratorTools.ReplaceKey(ref html, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters", backgroundImage)));

            var playerFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{mission.TemplateRecord.GetCoalitionID(mission.TemplateRecord.ContextPlayerCoalition)}.png");
            if (!File.Exists(playerFlagPath))
                playerFlagPath = UNKOWN_IMAGE_PATH;
            GeneratorTools.ReplaceKey(ref html, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));

            var enemyFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{mission.TemplateRecord.GetCoalitionID(mission.TemplateRecord.ContextPlayerCoalition.GetEnemy())}.png");
            if (!File.Exists(enemyFlagPath))
                enemyFlagPath = UNKOWN_IMAGE_PATH;
            GeneratorTools.ReplaceKey(ref html, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
            
            GeneratorTools.ReplaceKey(ref html, "MissionName", mission.Briefing.Name);
            GeneratorTools.ReplaceKey(ref html, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));

            await GenerateTitleImageAsync(database, html, mission);
        }

        internal static async Task GenerateKneeboardImagesAsync(IDatabase database, DCSMission mission)
        {
            var html = mission.Briefing.GetBriefingKneeBoardTasksAndRemarksHTML(database, mission);
            await GenerateKneeboardImageAsync(database, html, "Tasks", mission);

            html = mission.Briefing.GetBriefingKneeBoardFlightsHTML(database, mission);
            await GenerateKneeboardImageAsync(database, html, "Flights", mission);

            html = mission.Briefing.GetBriefingKneeBoardGroundHTML(database, mission);
            await GenerateKneeboardImageAsync(database, html, "Ground", mission);

            foreach (var flight in mission.Briefing.FlightBriefings)
            {
                html = flight.GetFlightBriefingKneeBoardHTML(database, mission.LangKey);
                await GenerateKneeboardImageAsync(database, html, flight.Name, mission, flight.Type);
            }
        }



        public static async Task<List<byte[]>> GenerateKneeboardImageAsync(IDatabase database, string langKey, string html)
        {
            List<byte[]> output = new();
            try
            {
                var imagePaths = await GenerateKneeboardImagePaths(html);
                foreach (var path in imagePaths)
                {
                    var imgData = await File.ReadAllBytesAsync(path);
                    using var ms = new MemoryStream();
                    output.Add(imgData);
                    File.Delete(path);
                }
                return output;

            }
            catch (Exception e)
            {
                throw new BriefingRoomException(database, langKey, "FailedToCreateKneeboard", e);
            }

        }

        private static async Task<int> GenerateKneeboardImageAsync(IDatabase database, string html, string name, DCSMission mission, string aircraftID = "")
        {
            try
            {
                var midPath = !string.IsNullOrEmpty(aircraftID) ? $"{aircraftID}/" : "";
                var imagePaths = await GenerateKneeboardImagePaths(html);
                var multiImage = imagePaths.Count() > 1;
                var inc = 0;
                foreach (var path in imagePaths)
                {
                    var imgData = await File.ReadAllBytesAsync(path);
                    mission.AddMediaFile($"KNEEBOARD/{midPath}IMAGES/{name}{(multiImage ? inc : "")}.png", imgData);
                    File.Delete(path);
                    inc++;
                }
                mission.AddMediaFile($"KNEEBOARD_HTML/{midPath}IMAGES/{name}.html", Encoding.UTF8.GetBytes(html));

                return inc;

            }
            catch (Exception e)
            {
                throw new BriefingRoomException(database, mission.LangKey, "FailedToCreateKneeboard", e);
            }

        }

        private static async Task GenerateTitleImageAsync(IDatabase database, string html, DCSMission mission)
        {
            try
            {
                var imagePath = await GenerateTitleImagePath(html);
                var imgData = await File.ReadAllBytesAsync(imagePath);
                mission.AddMediaFile($"{BRPaths.MIZ_RESOURCES}title_{mission.UniqueID}.png", imgData);
                File.Delete(imagePath);
                mission.AddMediaFile($"{BRPaths.MIZ_RESOURCES}title_{mission.UniqueID}.html", Encoding.UTF8.GetBytes(html));
            }
            catch (Exception e)
            {
                throw new BriefingRoomException(database, mission.LangKey, "FailedToCreateTitleImage", e);
            }

        }

        private static async Task GenerateCampaignImageAsync(IDatabase database, string langKey, string html, DCSCampaign campaign, string fileName)
        {
            try
            {
                var imagePath = await GenerateTitleImagePath(html);
                var imgData = await File.ReadAllBytesAsync(imagePath);
                campaign.AddMediaFile($"{fileName}.png", imgData);
                File.Delete(imagePath);
                campaign.AddMediaFile($"{fileName}.html", Encoding.UTF8.GetBytes(html));
            }
            catch (Exception e)
            {
                throw new BriefingRoomException(database, langKey, "FailedToCreateTitleImage", e);
            }

        }

        private static async Task<string[]> GenerateKneeboardImagePaths(string html)
        {
            var iWidth = 768;
            var iHeight = 1024;
            
            var page = await GetPooledPageAsync();
            try
            {
                await page.SetViewportAsync(new ViewPortOptions { Width = iWidth, Height = iHeight });
                // Use DOMContentLoaded - faster since images are base64 embedded
                await page.SetContentAsync(html, new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });
                
                // Get full page height for multi-page kneeboards
                var bodyHeight = await page.EvaluateExpressionAsync<int>("document.body.scrollHeight");
                var pageCount = (int)Math.Ceiling((double)bodyHeight / iHeight);
                
                var imagePaths = new List<string>();
                for (int i = 0; i < pageCount; i++)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                    await page.EvaluateExpressionAsync($"window.scrollTo(0, {i * iHeight})");
                    await page.ScreenshotAsync(tempPath, new ScreenshotOptions
                    {
                        Clip = new PuppeteerSharp.Media.Clip
                        {
                            X = 0,
                            Y = 0,
                            Width = iWidth,
                            Height = iHeight
                        },
                        Type = ScreenshotType.Png
                    });
                    imagePaths.Add(tempPath);
                }

                return imagePaths.ToArray();
            }
            finally
            {
                ReturnPageToPool(page);
            }
        }

        private static async Task<string> GenerateTitleImagePath(string html)
        {
            var iWidth = 1024;
            var iHeight = 1024;
            
            var page = await GetPooledPageAsync();
            try
            {
                await page.SetViewportAsync(new ViewPortOptions { Width = iWidth, Height = iHeight });
                // Use DOMContentLoaded - faster since images are base64 embedded
                await page.SetContentAsync(html, new NavigationOptions { WaitUntil = [WaitUntilNavigation.DOMContentLoaded] });
                
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                await page.ScreenshotAsync(tempPath, new ScreenshotOptions
                {
                    Clip = new PuppeteerSharp.Media.Clip
                    {
                        X = 0,
                        Y = 0,
                        Width = iWidth,
                        Height = iHeight
                    },
                    Type = ScreenshotType.Png
                });

                return tempPath;
            }
            finally
            {
                ReturnPageToPool(page);
            }
        }

        private static string GetInternalImageHTMLBase64(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var fileBase64 = Convert.ToBase64String(bytes);
            return $"data:image/png;base64, {fileBase64}";
        }
    }
}