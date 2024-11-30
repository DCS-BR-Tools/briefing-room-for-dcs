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

using BriefingRoom4DCS.Data;
using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using IronPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BriefingRoom4DCS.Generator
{
    public class MissionGeneratorImages
    {
        internal static async Task GenerateCampaignImages(CampaignTemplate campaignTemplate, DCSCampaign campaign, string baseFileName)
        {
            string titleHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignTitleImage.html"));
            string winHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignWinImage.html"));
            string lossHTML = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "CampaignLossImage.html"));
            string[] theaterImages = Directory.GetFiles(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters"), $"{Database.Instance.GetEntry<DBEntryTheater>(campaignTemplate.ContextTheater).DCSID}*.*")
                .Where(x => x.EndsWith(".jpg") || x.EndsWith(".png")).ToArray();
            var backgroundImage = "_default.jpg";
            if (theaterImages.Length > 0)
                backgroundImage = Path.GetFileName(Toolbox.RandomFrom(theaterImages));

            GeneratorTools.ReplaceKey(ref titleHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters", backgroundImage)));
            GeneratorTools.ReplaceKey(ref winHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Sky.jpg")));
            GeneratorTools.ReplaceKey(ref lossHTML, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Fire.jpg")));

            var playerFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{campaignTemplate.GetCoalitionID(campaignTemplate.ContextPlayerCoalition)}.png");
            if (File.Exists(playerFlagPath))
            {
                GeneratorTools.ReplaceKey(ref titleHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
                GeneratorTools.ReplaceKey(ref winHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
                GeneratorTools.ReplaceKey(ref lossHTML, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
            }

            var enemyFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{campaignTemplate.GetCoalitionID(campaignTemplate.ContextPlayerCoalition.GetEnemy())}.png");
            if (File.Exists(enemyFlagPath))
            {
                GeneratorTools.ReplaceKey(ref titleHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
                GeneratorTools.ReplaceKey(ref winHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
                GeneratorTools.ReplaceKey(ref lossHTML, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));
            }


            GeneratorTools.ReplaceKey(ref titleHTML, "MissionName", campaign.Name);
            GeneratorTools.ReplaceKey(ref winHTML, "MissionName", campaign.Name);
            GeneratorTools.ReplaceKey(ref lossHTML, "MissionName", campaign.Name);

            GeneratorTools.ReplaceKey(ref titleHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));
            GeneratorTools.ReplaceKey(ref winHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));
            GeneratorTools.ReplaceKey(ref lossHTML, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));

            var langKey = campaign.Missions[0].LangKey;
            await GenerateCampaignImageAsync(langKey, titleHTML, campaign, $"{baseFileName}_Title");
            await GenerateCampaignImageAsync(langKey, winHTML, campaign, $"{baseFileName}_Success");
            await GenerateCampaignImageAsync(langKey, lossHTML, campaign, $"{baseFileName}_Failure");

        }

        internal static async Task GenerateTitleImage(DCSMission mission)
        {
            string html = Toolbox.ReadAllTextIfFileExists(Path.Combine(BRPaths.INCLUDE_HTML, "MissionTitleImage.html"));
            string[] theaterImages = Directory.GetFiles(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters"), $"{Database.Instance.GetEntry<DBEntryTheater>(mission.TemplateRecord.ContextTheater).DCSID}*.*")
               .Where(x => x.EndsWith(".jpg") || x.EndsWith(".png")).ToArray();
            var backgroundImage = "_default.jpg";
            if (theaterImages.Length > 0)
                backgroundImage = Path.GetFileName(Toolbox.RandomFrom(theaterImages));
            GeneratorTools.ReplaceKey(ref html, "BackgroundImage", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "Theaters", backgroundImage)));

            var playerFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{mission.TemplateRecord.GetCoalitionID(mission.TemplateRecord.ContextPlayerCoalition)}.png");
            if (File.Exists(playerFlagPath))
                GeneratorTools.ReplaceKey(ref html, "PlayerFlag", GetInternalImageHTMLBase64(playerFlagPath));
            var enemyFlagPath = Path.Combine(BRPaths.INCLUDE_JPG, "Flags", $"{mission.TemplateRecord.GetCoalitionID(mission.TemplateRecord.ContextPlayerCoalition.GetEnemy())}.png");
            if (File.Exists(enemyFlagPath))
                GeneratorTools.ReplaceKey(ref html, "EnemyFlag", GetInternalImageHTMLBase64(enemyFlagPath));

            GeneratorTools.ReplaceKey(ref html, "MissionName", mission.Briefing.Name);
            GeneratorTools.ReplaceKey(ref html, "Watermark", GetInternalImageHTMLBase64(Path.Combine(BRPaths.INCLUDE_JPG, "IconSlim.png")));

            await GenerateTitleImageAsync(html, mission);
        }

        internal static async Task GenerateKneeboardImagesAsync(DCSMission mission)
        {
            var html = mission.Briefing.GetBriefingKneeBoardTasksAndRemarksHTML(mission);
            await GenerateKneeboardImageAsync(html, "Tasks", mission);

            html = mission.Briefing.GetBriefingKneeBoardFlightsHTML(mission);
            await GenerateKneeboardImageAsync(html, "Flights", mission);

            html = mission.Briefing.GetBriefingKneeBoardGroundHTML(mission);
            await GenerateKneeboardImageAsync(html, "Ground", mission);

            foreach (var flight in mission.Briefing.FlightBriefings)
            {
                html = flight.GetFlightBriefingKneeBoardHTML(mission.LangKey);
                await GenerateKneeboardImageAsync(html, flight.Name, mission, flight.Type);
            }
        }



        public static async Task<List<byte[]>> GenerateKneeboardImageAsync(string langKey, string html)
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
                throw new BriefingRoomException(langKey, "FailedToCreateKneeboard", e);
            }

        }

        private static async Task<int> GenerateKneeboardImageAsync(string html, string name, DCSMission mission, string aircraftID = "")
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
                throw new BriefingRoomException(mission.LangKey, "FailedToCreateKneeboard", e);
            }

        }

        private static async Task GenerateTitleImageAsync(string html, DCSMission mission)
        {
            try
            {
                var imagePath = await GenerateTitleImagePath(html);
                var imgData = await File.ReadAllBytesAsync(imagePath);
                mission.AddMediaFile($"l10n/DEFAULT/title_{mission.UniqueID}.png", imgData);
                File.Delete(imagePath);
                mission.AddMediaFile($"l10n/DEFAULT/title_{mission.UniqueID}.html", Encoding.UTF8.GetBytes(html));
            }
            catch (Exception e)
            {
                throw new BriefingRoomException(mission.LangKey, "FailedToCreateTitleImage", e);
            }

        }

        private static async Task GenerateCampaignImageAsync(string langKey, string html, DCSCampaign campaign, string fileName)
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
                throw new BriefingRoomException(langKey, "FailedToCreateTitleImage", e);
            }

        }

        private static async Task<string[]> GenerateKneeboardImagePaths(string html)
        {
            var iWidth = 768;
            var iHeight = 1024;
            string tempRenderPath = Path.ChangeExtension(Path.GetTempFileName(), ".png").Replace(".png", "_*.png");
            ChromePdfRenderer renderer = new();
            renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.Custom;
            renderer.RenderingOptions.SetCustomPaperSizeinPixelsOrPoints(iWidth, iHeight);
            renderer.RenderingOptions.MarginTop = 0;
            renderer.RenderingOptions.MarginLeft = 0;
            renderer.RenderingOptions.MarginRight = 0;
            renderer.RenderingOptions.MarginBottom = 0;
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(html);
            var imagePaths = pdf.ToPngImages(tempRenderPath, iWidth, iHeight);

            return imagePaths;
        }

        private static async Task<string> GenerateTitleImagePath(string html)
        {
            var iWidth = 1024;
            var iHeight = 1024;
            string tempRenderPath = Path.ChangeExtension(Path.GetTempFileName(), ".png").Replace(".png", "_*.png");
            ChromePdfRenderer renderer = new();
            renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.Custom;
            renderer.RenderingOptions.SetCustomPaperSizeinPixelsOrPoints(iWidth, iHeight);
            renderer.RenderingOptions.MarginTop = 0;
            renderer.RenderingOptions.MarginLeft = 0;
            renderer.RenderingOptions.MarginRight = 0;
            renderer.RenderingOptions.MarginBottom = 0;
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(html);
            var imagePaths = pdf.ToPngImages(tempRenderPath, iWidth, iHeight);

            return imagePaths[0];
        }

        private static string GetInternalImageHTMLBase64(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            var fileBase64 = Convert.ToBase64String(bytes);
            return $"data:image/png;base64, {fileBase64}";
        }
    }
}